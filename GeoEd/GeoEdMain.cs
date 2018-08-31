using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Common;
using GeoEd.Data;
using GeoEd.Games;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using SysTimer = System.Timers.Timer;

namespace GeoEd
{
    public struct Vertex
    {
        public const int Size = (4 + 4) * 4; // size of struct in bytes

        private readonly Vector4 _position;
        private readonly Color4 _color;

        public Vertex(Vector4 position, Color4 color)
        {
            _position = position;
            _color = color;
        }
    }

    public class RenderObject : IDisposable
    {
        private bool _initialized;
        private readonly int _vertexArray;
        private readonly int _buffer;
        private readonly int _verticeCount;
        public RenderObject(Vertex[] vertices)
        {
            _verticeCount = vertices.Length;
            _vertexArray = GL.GenVertexArray();
            _buffer = GL.GenBuffer();

            GL.BindVertexArray(_vertexArray);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexArray);

            // create first buffer: vertex
            GL.NamedBufferStorage(
                _buffer,
                Vertex.Size * vertices.Length,        // the size needed by this buffer
                vertices,                           // data to initialize with
                BufferStorageFlags.MapWriteBit);    // at this point we will only write to the buffer


            GL.VertexArrayAttribBinding(_vertexArray, 0, 0);
            GL.EnableVertexArrayAttrib(_vertexArray, 0);
            GL.VertexArrayAttribFormat(
                _vertexArray,
                0,                      // attribute index, from the shader location = 0
                4,                      // size of attribute, vec4
                VertexAttribType.Float, // contains floats
                false,                  // does not need to be normalized as it is already, floats ignore this flag anyway
                0);                     // relative offset, first item


            GL.VertexArrayAttribBinding(_vertexArray, 1, 0);
            GL.EnableVertexArrayAttrib(_vertexArray, 1);
            GL.VertexArrayAttribFormat(
                _vertexArray,
                1,                      // attribute index, from the shader location = 1
                4,                      // size of attribute, vec4
                VertexAttribType.Float, // contains floats
                false,                  // does not need to be normalized as it is already, floats ignore this flag anyway
                16);                     // relative offset after a vec4

            // link the vertex array and buffer and provide the stride as size of Vertex
            GL.VertexArrayVertexBuffer(_vertexArray, 0, _buffer, IntPtr.Zero, Vertex.Size);
            _initialized = true;
        }
        public void Bind()
        {
            GL.BindVertexArray(_vertexArray);
        }
        public void Render()
        {
            GL.DrawArrays(PrimitiveType.Triangles, 0, _verticeCount);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_initialized)
                {
                    GL.DeleteVertexArray(_vertexArray);
                    GL.DeleteBuffer(_buffer);
                    _initialized = false;
                }
            }
        }
    }

    public partial class GeoEdMain : Form
    {
        private readonly object _renderLock = new object();

        private readonly SysTimer _timer;

        private readonly List<RenderObject> _renderObjects = new List<RenderObject>();

        private int _program;

        public GeoEdMain()
        {
            InitializeComponent();

            MessageUtil.SetAppName("GeoEd");

            openFileDialog.Filter = "Bundle Files (*.bin, *.bun)|*.bin;*.bun";
            glControl1.Resize += glControl1_Resize;
            glControl1.Paint += glControl1_Paint;

            _timer = new SysTimer(1000.0 / 60.0); // 60FPS target
            _timer.Elapsed += (_, __) =>
            {
                glControl1.Invalidate();
            };

            _timer.Start();
        }

        private int CompileShader(ShaderType type, string path)
        {
            var shader = GL.CreateShader(type);
            var src = File.ReadAllText(path);
            GL.ShaderSource(shader, src);
            GL.CompileShader(shader);
            var info = GL.GetShaderInfoLog(shader);
            if (!string.IsNullOrWhiteSpace(info))
                Debug.WriteLine($"GL.CompileShader [{type}] had info log: {info}");
            return shader;
        }

        private int CreateProgram()
        {
            var program = GL.CreateProgram();
            var shaders = new List<int>();
            shaders.Add(CompileShader(ShaderType.VertexShader, @"Shaders\vertexShader.vs"));
            shaders.Add(CompileShader(ShaderType.FragmentShader, @"Shaders\fragShader.fs"));

            foreach (var shader in shaders)
                GL.AttachShader(program, shader);
            GL.LinkProgram(program);
            var info = GL.GetProgramInfoLog(program);
            if (!string.IsNullOrWhiteSpace(info))
                Debug.WriteLine($"GL.LinkProgram had info log: {info}");

            foreach (var shader in shaders)
            {
                GL.DetachShader(program, shader);
                GL.DeleteShader(shader);
            }
            return program;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                var fileName = openFileDialog.FileName;
                var fullDirectory = Path.GetDirectoryName(fileName);

                if (fullDirectory == null)
                {
                    throw new NullReferenceException("fullDirectory == null");
                }

                messageLabel.Text = $"Loading: {fileName}...";

                var parentDirectory = Directory.GetParent(fullDirectory);

                if (string.Equals("CARS", parentDirectory.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    parentDirectory = Directory.GetParent(parentDirectory.FullName);
                }
                else if (string.Equals("FRONTEND", parentDirectory.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    parentDirectory = Directory.GetParent(parentDirectory.FullName);
                }

                var detectedGame = GameDetector.DetectGame(parentDirectory.FullName);

                if (detectedGame != GameDetector.Game.MostWanted)
                {
                    MessageUtil.ShowError($"Unsupported game: {detectedGame}");

                    return;
                }

                SetWindowTitle(detectedGame);

                Directory.CreateDirectory(Path.Combine(fullDirectory, "models"));

                var chunkManager = new ChunkManager();
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                switch (detectedGame)
                {
                    case GameDetector.Game.MostWanted:
                        {
                            chunkManager.Read<MostWantedSolids>(fileName);
                            break;
                        }
                    default: break;
                }

                stopwatch.Stop();

                messageLabel.Text = $"Loaded {fileName} [{stopwatch.ElapsedMilliseconds}ms]";

                treeView1.Nodes.Clear();

                lock (_renderLock)
                {
                    _renderObjects.Clear();
                }

                foreach (var solidList in chunkManager.SolidLists)
                {
                    var rootNode = treeView1.Nodes.Add(solidList.PipelinePath);

                    rootNode.Tag = solidList;

                    foreach (var groupedObjects in solidList.Objects.GroupBy(o =>
                        o.Name.Substring(0, o.Name.Contains('_') ? o.Name.LastIndexOf('_') : o.Name.Length)))
                    {
                        var baseObjNode = rootNode.Nodes.Add(groupedObjects.Key);

                        baseObjNode.Tag = groupedObjects;

                        foreach (var groupObj in groupedObjects)
                        {
                            var objNode = baseObjNode.Nodes.Add(groupObj.Name);
                            objNode.Tag = groupObj;
                        }
                    }

                    foreach (var solidObject in solidList.Objects)
                    {
                        // Set up vertices

                        if (solidObject.Materials.Any(o => (o.Flags & 0x000040) == 0x000040)) continue;

                        var bufCountMap = new Dictionary<int, long>();

                        foreach (var grouping in solidObject.Materials.GroupBy(m => m.VertexStreamIndex))
                        {
                            bufCountMap[grouping.Key] = grouping.Sum(m => m.NumVerts);
                        }

                        //using (var fs = File.OpenWrite(Path.Combine(fullDirectory, "models", $"{solidObject.Name}.obj")))
                        //using (var sw = new StreamWriter(fs))
                        //{
                        //sw.WriteLine($"obj {solidObject.Name}");

                        var numVerts = 0u;

                        if (solidObject.MeshDescriptor.NumVerts == 0) continue;

                        foreach (var solidObjectMaterial in solidObject.Materials)
                        {
                            var vb = solidObject.VertexBuffers[solidObjectMaterial.VertexStreamIndex];

                            if (vb.Data.Count == 0) continue;

                            var stride = (int)(vb.Data.Count / bufCountMap[solidObjectMaterial.VertexStreamIndex]);
                            var shift = numVerts - vb.Position / stride;
                            var normBase = solidObjectMaterial.Unknown1 == 4 ? 3 : 6;
                            var uvBase = solidObjectMaterial.Unknown1 == 4 ? 7 : 3;

                            for (var j = 0; j < solidObjectMaterial.NumVerts; j++)
                            {
                                var vertex = new SolidMeshVertex
                                {
                                    Color = 0xeeeeee,
                                    X = vb.Data[vb.Position],
                                    Y = vb.Data[vb.Position + 2],
                                    Z = vb.Data[vb.Position + 1]
                                };
                                solidObject.Vertices.Add(vertex);

                                vb.Position += stride;

                                numVerts++;

                                //sw.WriteLine($"v {BinaryUtil.FullPrecisionFloat(vertex.X)} {BinaryUtil.FullPrecisionFloat(vertex.Y)} {BinaryUtil.FullPrecisionFloat(vertex.Z)}");
                            }

                            for (var j = (int)(solidObjectMaterial.TriOffset / 3);
                                j < solidObjectMaterial.TriOffset / 3 + solidObjectMaterial.NumTris / 3;
                                j++)
                            {
                                if (solidObject.Faces[j].Vtx1 != solidObject.Faces[j].Vtx2
                                    && solidObject.Faces[j].Vtx1 != solidObject.Faces[j].Vtx3
                                    && solidObject.Faces[j].Vtx2 != solidObject.Faces[j].Vtx3)
                                {
                                    var origFace = new[]
                                    {
                                            solidObject.Faces[j].Vtx1,
                                            solidObject.Faces[j].Vtx2,
                                            solidObject.Faces[j].Vtx3
                                        };

                                    solidObject.Faces[j].Vtx1 = (ushort)(shift + origFace[0]);
                                    solidObject.Faces[j].Vtx2 = (ushort)(shift + origFace[2]);
                                    solidObject.Faces[j].Vtx3 = (ushort)(shift + origFace[1]);
                                }

                                //sw.WriteLine($"f {solidObject.Faces[j].Vtx1 + 1} {solidObject.Faces[j].Vtx2 + 1} {solidObject.Faces[j].Vtx3 + 1}");
                            }
                        }
                        //}

                        if (solidObject.Name.EndsWith("_A"))
                        {
                            lock (_renderLock)
                            {
                                _renderObjects.Add(new RenderObject(
                                    solidObject.Vertices.Select(v => new Vertex(
                                        new Vector4(v.X, v.Y, v.Z, 1.0f), Color4.AliceBlue)).ToArray()
                                ));
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("cancelled, doing nothing");
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            lock (_renderLock)
            {
                _program = CreateProgram();
                glControl1_Resize(this, EventArgs.Empty);   // Ensure the Viewport is set up correctly
                GL.ClearColor(Color.Crimson);

                SetWindowTitle(GameDetector.Game.Unknown);
            }

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.PatchParameter(PatchParameterInt.PatchVertices, 3);

            //Text += $" - OpenGL: {GL.GetString(StringName.Version)}";
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            glControl1.MakeCurrent();

            GL.ClearColor(Color.Crimson);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(_program);

            lock (_renderLock)
            {
                foreach (var renderObject in _renderObjects)
                {
                    renderObject.Bind();

                    renderObject.Render();
                }
            }

            glControl1.SwapBuffers();
        }

        private void glControl1_Resize(object sender, EventArgs e)
        {
            if (glControl1.ClientSize.Height == 0)
                glControl1.ClientSize = new Size(glControl1.ClientSize.Width, 1);

            GL.Viewport(0, 0, glControl1.ClientSize.Width, glControl1.ClientSize.Height);
        }

        private void SetWindowTitle(GameDetector.Game game)
        {
            var title = "GeoEd v0.0.1 by heyitsleo";

            try
            {
                title += $" - OpenGL: {GL.GetString(StringName.Version)}";
            }
            catch (Exception)
            {
                // ignored
            }

            if (game != GameDetector.Game.Unknown)
            {
                title += $" | {game}";
            }

            Text = title;
        }
    }
}

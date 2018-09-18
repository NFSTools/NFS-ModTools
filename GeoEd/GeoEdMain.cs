using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Common;
using Common.Geometry;
using Common.Geometry.Data;
using ObjLoader.Loader.Data.DataStore;
using ObjLoader.Loader.Loaders;
using ObjLoader.Loader.TypeParsers;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using SysTimer = System.Timers.Timer;

namespace GeoEd
{
    public partial class GeoEdMain : Form
    {
        private readonly object _renderLock = new object();

        private readonly SysTimer _timer;

        private int _program;
        private ChunkManager _chunkManager;
        private string _currentFileName;
        private GameDetector.Game _currentGame;
        private int _selectedSolidList;

        private readonly Dictionary<uint, RenderObject> _objectsToRender = new Dictionary<uint, RenderObject>();

        public GeoEdMain()
        {
            InitializeComponent();

            MessageUtil.SetAppName("GeoEd");

            openBundleDialog.Filter = "Bundle Files (*.bin, *.bun)|*.bin;*.bun|All Files (*.*)|*.*";
            openModelDialog.Filter = "Wavefront Obj Files (*.obj)|*.obj";

            glControl1.Resize += glControl1_Resize;
            glControl1.Paint += glControl1_Paint;

            _timer = new SysTimer(1000.0 / 60.0); // 60FPS target
            _timer.Elapsed += (_, __) =>
            {
                glControl1.Invalidate();
            };

            _timer.Start();

            treeView1.AfterSelect += TreeView1_OnAfterSelect;
            treeView1.AfterCheck += TreeView1_OnAfterCheck;
        }

        private void TreeView1_OnAfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Parent == null || !(e.Node.Tag is int modelIdx)) return;

            var parentChunkIdx = (int)e.Node.Parent.Parent.Tag;
            var chunk = _chunkManager.Chunks[parentChunkIdx];

            if (!(chunk.Resource is SolidList solidList)) return;

            var solidListObject = solidList.Objects[modelIdx];
            var vertices = new List<Vertex>();
            var vertexIndices = new List<ushort>();

            foreach (var face in solidListObject.Faces)
            {
                if (!vertexIndices.Contains(face.Vtx1))
                {
                    vertices.Add(new Vertex(
                        new Vector4(solidListObject.Vertices[face.Vtx1].X, solidListObject.Vertices[face.Vtx1].Y, solidListObject.Vertices[face.Vtx1].Z, 1.0f),
                        Color4.Black
                    ));

                    vertexIndices.Add(face.Vtx1);
                }

                if (!vertexIndices.Contains(face.Vtx2))
                {
                    vertices.Add(new Vertex(
                        new Vector4(solidListObject.Vertices[face.Vtx2].X, solidListObject.Vertices[face.Vtx2].Y, solidListObject.Vertices[face.Vtx2].Z, 1.0f),
                        Color4.Black
                    ));

                    vertexIndices.Add(face.Vtx2);
                }

                if (!vertexIndices.Contains(face.Vtx3))
                {
                    vertices.Add(new Vertex(
                        new Vector4(solidListObject.Vertices[face.Vtx3].X, solidListObject.Vertices[face.Vtx3].Y, solidListObject.Vertices[face.Vtx3].Z, 1.0f),
                        Color4.Black
                    ));

                    vertexIndices.Add(face.Vtx3);
                }
            }

            var renderObj = new RenderObject(vertices.ToArray());

            if (e.Node.Checked)
            {
                _objectsToRender.Add(solidListObject.Hash, renderObj);
            }
            else
            {
                _objectsToRender.Remove(solidListObject.Hash);
            }
        }

        private void TreeView1_OnAfterSelect(object sender, TreeViewEventArgs e)
        {
            if (treeView1.SelectedNode != null
                && treeView1.SelectedNode.Parent == null)
            {
                _selectedSolidList = (int)treeView1.SelectedNode?.Tag;
                importModelToolStripMenuItem.Enabled = true;
            }
            else
            {
                importModelToolStripMenuItem.Enabled = false;
            }
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
            var shaders = new List<int>
            {
                CompileShader(ShaderType.VertexShader, @"Shaders\vertexShader.vs"),
                CompileShader(ShaderType.FragmentShader, @"Shaders\fragShader.fs")
            };

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
            if (openBundleDialog.ShowDialog(this) == DialogResult.OK)
            {
                _currentFileName = openBundleDialog.FileName;
                var fullDirectory = Path.GetDirectoryName(_currentFileName);

                if (fullDirectory == null)
                {
                    throw new NullReferenceException("fullDirectory == null");
                }

                importModelToolStripMenuItem.Enabled = false;
                saveToolStripMenuItem.Enabled = false;
                messageLabel.Text = $"Loading: {_currentFileName}...";

                var parentDirectory = Directory.GetParent(fullDirectory);

                if (string.Equals("CARS", parentDirectory.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    parentDirectory = Directory.GetParent(parentDirectory.FullName);
                }
                else if (string.Equals("FRONTEND", parentDirectory.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    parentDirectory = Directory.GetParent(parentDirectory.FullName);
                }

                _currentGame = GameDetector.DetectGame(parentDirectory.FullName);

                if (_currentGame != GameDetector.Game.MostWanted 
                    && _currentGame != GameDetector.Game.World
                    && _currentGame != GameDetector.Game.ProStreet)
                {
                    MessageUtil.ShowError($"Unsupported game: {_currentGame}");

                    return;
                }

                SetWindowTitle(_currentGame);

                Directory.CreateDirectory(Path.Combine(fullDirectory, "models"));

                _chunkManager = new ChunkManager(_currentGame);
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                _chunkManager.Read(_currentFileName);
                stopwatch.Stop();

                messageLabel.Text = $"Loaded {_currentFileName} [{stopwatch.ElapsedMilliseconds}ms]";

                saveToolStripMenuItem.Enabled = true;

                treeView1.Nodes.Clear();

                lock (_renderLock)
                {
                    _objectsToRender.Clear();
                }

                var chunks = _chunkManager.Chunks;

                for (var index = 0; index < chunks.Count; index++)
                {
                    var chunk = chunks[index];

                    if (!(chunk.Resource is SolidList solidList)) continue;

                    var rootNode = treeView1.Nodes.Add(solidList.PipelinePath);
                    rootNode.Tag = index;

                    foreach (var groupedObjects in solidList.Objects.GroupBy(o =>
                        o.Name.Substring(0, o.Name.Contains('_') ? o.Name.LastIndexOf('_') : o.Name.Length)).ToList())
                    {
                        var baseObjNode = rootNode.Nodes.Add(groupedObjects.Key);

                        var groupList = groupedObjects.ToList();

                        for (var j = 0; j < groupList.Count; j++)
                        {
                            var groupObj = groupList[j];
                            var objNode = baseObjNode.Nodes.Add(groupObj.Name);
                            objNode.Tag = j;
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
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            glControl1.MakeCurrent();

            GL.ClearColor(Color.Crimson);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(_program);

            lock (_renderLock)
            {
                foreach (var renderObject in _objectsToRender)
                {
                    renderObject.Value.Bind();
                    renderObject.Value.Render();
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

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_currentGame != GameDetector.Game.World)
            {
                MessageUtil.ShowError("Saving is not supported for this game.");
                return;
            }

            if (!File.Exists(_currentFileName + ".bak"))
            {
                File.Copy(_currentFileName, _currentFileName + ".bak");
            }

            using (var fs = new FileStream(_currentFileName, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                var chunkStream = new ChunkStream(bw);

                SolidListManager slm;

                switch (_currentGame)
                {
                    case GameDetector.Game.World:
                        slm = new World15Solids();
                        break;
                    default: throw new Exception("nah");
                }

                var solidListIdx = -1;
                var numSolidLists = _chunkManager.Chunks.Count(c => c.Resource is SolidList);

                foreach (var chunk in _chunkManager.Chunks)
                {
                    if (chunk.Resource is SolidList sl)
                    {
                        slm.WriteSolidList(chunkStream, sl);

                        if (++solidListIdx != numSolidLists)
                        {
                            chunkStream.PaddingAlignment(0x80);
                        }
                    }
                    else
                    {
                        chunkStream.BeginChunk(chunk.Id);
                        bw.Write(chunk.Data);
                        chunkStream.EndChunk();
                        chunkStream.PaddingAlignment(0x10);
                    }
                }
            }

            MessageUtil.ShowInfo("Saved!");
        }

        private void importModelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openModelDialog.ShowDialog(this) == DialogResult.OK)
            {
                var objLoader = new ObjLoaderFactory().Create();

                var solidObj = new SolidObject
                {
                    Name = Path.GetFileNameWithoutExtension(openModelDialog.FileName),
                    MinPoint = new SimpleVector4(0.0f, 0.0f, 0.0f, 0.0f),
                    MaxPoint = new SimpleVector4(0.0f, 0.0f, 0.0f, 0.0f),
                    Transform = new SimpleMatrix
                    {
                        Data = new float[4, 4]
                    }
                };

                var matrix = new float[16];
                matrix[0] = 1.0f;
                matrix[5] = 1.0f;
                matrix[10] = 1.0f;
                matrix[15] = 1.0f;

                var matrixIdx = 0;

                for (var i = 0; i < 4; i++)
                {
                    for (var j = 0; j < 4; j++)
                    {
                        solidObj.Transform[i, j] = matrix[matrixIdx++];
                    }
                }

                solidObj.Hash = Hasher.BinHash(solidObj.Name);

                using (var ms = new MemoryStream(
                    Encoding.ASCII.GetBytes(File.ReadAllText(openModelDialog.FileName))))
                {
                    var result = objLoader.Load(ms);

                    // group: face list->material
                    foreach (var group in result.Groups)
                    {
                        var solidMaterial = new SolidObjectMaterial
                        {
                            Name = group.Material.Name,
                            Flags = 0x00224000,
                            MinPoint = new SimpleVector3(0, 0, 0),
                            MaxPoint = new SimpleVector3(0, 0, 0),
                            TextureHash =
                                Hasher.BinHash(Path.GetFileNameWithoutExtension(group.Material.AmbientTextureMap)),
                            NumTris = (uint)group.Faces.Count
                        };

                        solidMaterial.Hash = Hasher.BinHash(solidMaterial.Name);
                        solidMaterial.NumVerts = 0;
                        solidMaterial.NumIndices = solidMaterial.NumTris * 3;

                        var matVerts = new List<int>();

                        foreach (var face in group.Faces)
                        {
                            if (!matVerts.Contains(face[0].VertexIndex))
                            {
                                matVerts.Add(face[0].VertexIndex);
                                solidMaterial.NumVerts++;
                            }

                            if (!matVerts.Contains(face[1].VertexIndex))
                            {
                                matVerts.Add(face[1].VertexIndex);
                                solidMaterial.NumVerts++;
                            }

                            if (!matVerts.Contains(face[2].VertexIndex))
                            {
                                matVerts.Add(face[2].VertexIndex);
                                solidMaterial.NumVerts++;
                            }
                        }

                        foreach (var vertIdx in matVerts)
                        {
                            var vertex = result.Vertices[vertIdx - 1];

                            // min
                            if (vertex.X < solidMaterial.MinPoint.X)
                            {
                                solidMaterial.MinPoint.X = vertex.X;
                            }

                            if (vertex.Y < solidMaterial.MinPoint.Y)
                            {
                                solidMaterial.MinPoint.Y = vertex.Y;
                            }

                            if (vertex.Z < solidMaterial.MinPoint.Z)
                            {
                                solidMaterial.MinPoint.Z = vertex.Z;
                            }

                            // max
                            if (vertex.X > solidMaterial.MaxPoint.X)
                            {
                                solidMaterial.MaxPoint.X = vertex.X;
                            }

                            if (vertex.Y > solidMaterial.MaxPoint.Y)
                            {
                                solidMaterial.MaxPoint.Y = vertex.Y;
                            }

                            if (vertex.Z > solidMaterial.MaxPoint.Z)
                            {
                                solidMaterial.MaxPoint.Z = vertex.Z;
                            }
                        }

                        foreach (var face in group.Faces)
                        {
                            solidObj.Faces.Add(new SolidMeshFace
                            {
                                Vtx1 = (ushort)(face[0].VertexIndex - 1),
                                Vtx2 = (ushort)(face[1].VertexIndex - 1),
                                Vtx3 = (ushort)(face[2].VertexIndex - 1)
                            });
                        }

                        solidObj.Materials.Add(solidMaterial);
                    }

                    for (var j = 0; j < result.Vertices.Count; j++)
                    {
                        var vertex = result.Vertices[j];
                        var tv = result.Textures[j];

                        // min
                        if (vertex.X < solidObj.MinPoint.X)
                        {
                            solidObj.MinPoint.X = vertex.X;
                        }

                        if (vertex.Y < solidObj.MinPoint.Y)
                        {
                            solidObj.MinPoint.Y = vertex.Y;
                        }

                        if (vertex.Z < solidObj.MinPoint.Z)
                        {
                            solidObj.MinPoint.Z = vertex.Z;
                        }

                        // max
                        if (vertex.X > solidObj.MaxPoint.X)
                        {
                            solidObj.MaxPoint.X = vertex.X;
                        }

                        if (vertex.Y > solidObj.MaxPoint.Y)
                        {
                            solidObj.MaxPoint.Y = vertex.Y;
                        }

                        if (vertex.Z > solidObj.MaxPoint.Z)
                        {
                            solidObj.MaxPoint.Z = vertex.Z;
                        }

                        solidObj.Vertices.Add(new SolidMeshVertex
                        {
                            U = tv.X,
                            V = -tv.Y,
                            X = vertex.X,
                            Y = vertex.Y,
                            Z = vertex.Z
                        });
                    }
                }

                ((SolidList)_chunkManager.Chunks[_selectedSolidList].Resource).Objects.Add(solidObj);
                ((SolidList)_chunkManager.Chunks[_selectedSolidList].Resource).ObjectCount++;

                MessageUtil.ShowInfo("Imported object!");
            }
        }
    }
}

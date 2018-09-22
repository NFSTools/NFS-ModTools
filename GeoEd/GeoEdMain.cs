using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Common;
using Common.Geometry;
using Common.Geometry.Data;
using ObjLoader.Loader.Loaders;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using SharpHelper;

namespace GeoEd
{
    public partial class GeoEdMain : Form
    {
        private readonly object _renderLock = new object();

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

            treeView1.AfterSelect += TreeView1_OnAfterSelect;
            treeView1.AfterCheck += TreeView1_OnAfterCheck;

            Load += OnLoad;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            int[] indices = {
                            0,1,2,0,2,3,
                            4,6,5,4,7,6,
                            8,9,10,8,10,11,
                            12,14,13,12,15,14,
                            16,18,17,16,19,18,
                            20,21,22,20,22,23
            };


            ColoredVertex[] vertices = {
                ////TOP
                new ColoredVertex(new Vector3(-5,5,5),new Vector4(0,1,0,0)),
                new ColoredVertex(new Vector3(5,5,5),new Vector4(0,1,0,0)),
                new ColoredVertex(new Vector3(5,5,-5),new Vector4(0,1,0,0)),
                new ColoredVertex(new Vector3(-5,5,-5),new Vector4(0,1,0,0)),
                //BOTTOM
                new ColoredVertex(new Vector3(-5,-5,5),new Vector4(1,0,1,1)),
                new ColoredVertex(new Vector3(5,-5,5),new Vector4(1,0,1,1)),
                new ColoredVertex(new Vector3(5,-5,-5),new Vector4(1,0,1,1)),
                new ColoredVertex(new Vector3(-5,-5,-5),new Vector4(1,0,1,1)),
                //LEFT
                new ColoredVertex(new Vector3(-5,-5,5),new Vector4(1,0,0,1)),
                new ColoredVertex(new Vector3(-5,5,5),new Vector4(1,0,0,1)),
                new ColoredVertex(new Vector3(-5,5,-5),new Vector4(1,0,0,1)),
                new ColoredVertex(new Vector3(-5,-5,-5),new Vector4(1,0,0,1)),
                //RIGHT
                new ColoredVertex(new Vector3(5,-5,5),new Vector4(1,1,0,1)),
                new ColoredVertex(new Vector3(5,5,5),new Vector4(1,1,0,1)),
                new ColoredVertex(new Vector3(5,5,-5),new Vector4(1,1,0,1)),
                new ColoredVertex(new Vector3(5,-5,-5),new Vector4(1,1,0,1)),
                //FRONT
                new ColoredVertex(new Vector3(-5,5,5),new Vector4(0,1,1,1)),
                new ColoredVertex(new Vector3(5,5,5),new Vector4(0,1,1,1)),
                new ColoredVertex(new Vector3(5,-5,5),new Vector4(0,1,1,1)),
                new ColoredVertex(new Vector3(-5,-5,5),new Vector4(0,1,1,1)),
                //BACK
                new ColoredVertex(new Vector3(-5,5,-5),new Vector4(0,0,1,1)),
                new ColoredVertex(new Vector3(5,5,-5),new Vector4(0,0,1,1)),
                new ColoredVertex(new Vector3(5,-5,-5),new Vector4(0,0,1,1)),
                new ColoredVertex(new Vector3(-5,-5,-5),new Vector4(0,0,1,1))
            };

            //Help to count Frame Per Seconds
            var fpsCounter = new SharpFPS();

            var form = new RenderForm();

            using (var device = new SharpDevice(form))
            {
                //Init Mesh
                var mesh = SharpMesh.Create(device, vertices, indices);

                //Create Shader From File and Create Input Layout
                var shader = new SharpShader(device, "HLSL.txt",
                    new SharpShaderDescription { VertexShaderFunction = "VS", PixelShaderFunction = "PS" },
                    new[] {
                        new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                        new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0)
                    });

                //create constant buffer
                var buffer = shader.CreateBuffer<Matrix>();

                fpsCounter.Reset();

                //main loop
                RenderLoop.Run(panel1, () =>
                {
                    //Resizing
                    if (device.MustResize)
                    {
                        device.Resize();
                    }

                    //apply states
                    device.UpdateAllStates();

                    //clear color
                    device.Clear(Color.CornflowerBlue);

                    //Set matrices
                    var ratio = form.ClientRectangle.Width / (float)form.ClientRectangle.Height;
                    var projection = Matrix.PerspectiveFovLH(3.14F / 3.0F, ratio, 1, 1000);
                    var view = Matrix.LookAtLH(new Vector3(0, 10, -50), new Vector3(), Vector3.UnitY);
                    var world = Matrix.RotationY(Environment.TickCount / 1000.0F);
                    var WVP = world * view * projection;

                    //update constant buffer
                    device.UpdateData(buffer, WVP);

                    //pass constant buffer to shader
                    device.DeviceContext.VertexShader.SetConstantBuffer(0, buffer);

                    //apply shader
                    shader.Apply();

                    //draw mesh
                    mesh.Draw();

                    //begin drawing text
                    device.Font.Begin();

                    //draw string
                    fpsCounter.Update();
                    device.Font.DrawString("FPS: " + fpsCounter.FPS, 0, 0);

                    //flush text to view
                    device.Font.End();
                    //present
                    device.Present();
                });

                //release resources
                mesh.Dispose();
                buffer.Dispose();
            }
        }

        private void TreeView1_OnAfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Parent == null || !(e.Node.Tag is int modelIdx)) return;

            var parentChunkIdx = (int)e.Node.Parent.Parent.Tag;
            var chunk = _chunkManager.Chunks[parentChunkIdx];

            if (!(chunk.Resource is SolidList solidList)) return;

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

        private void SetWindowTitle(GameDetector.Game game)
        {
            var title = "GeoEd v0.0.1 by heyitsleo";

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

                var solidObj = new World15Object
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

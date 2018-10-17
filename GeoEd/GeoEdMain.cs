using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Common;
using Common.Geometry;
using Common.Geometry.Data;
using ObjLoader.Loader.Loaders;

namespace GeoEd
{
    public partial class GeoEdMain : Form
    {
        private ChunkManager _chunkManager;
        private string _currentFileName;
        private GameDetector.Game _currentGame;
        private int _selectedSolidList;

        public GeoEdMain()
        {
            InitializeComponent();

            MessageUtil.SetAppName("GeoEd");

            openBundleDialog.Filter = "Bundle Files (*.bin, *.bun)|*.bin;*.bun|All Files (*.*)|*.*";
            openModelDialog.Filter = "Wavefront Obj Files (*.obj)|*.obj";

            treeView1.AfterSelect += TreeView1_OnAfterSelect;
            treeView1.AfterCheck += TreeView1_OnAfterCheck;
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

                foreach (var chunk in _chunkManager.Chunks)
                {
                    if (chunk.Resource is SolidList sl)
                    {
                        slm.WriteSolidList(chunkStream, sl);
                    }
                    else
                    {
                        chunkStream.PaddingAlignment((chunk.Id & 0x80000000) == 0x80000000 ? 0x80 : 0x10);
                        chunkStream.BeginChunk(chunk.Id);
                        bw.Write(chunk.Data);
                        chunkStream.EndChunk();
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
                    var totalTris = result.Groups.Sum(g => g.Faces.Count);
                    var curTriIdx = 0;
                    var curVertIdx = 0;

                    Array.Resize(ref solidObj.Faces, totalTris);

                    // group: face list->material
                    foreach (var group in result.Groups)
                    {
                        var ambientFileName = Path.GetFileNameWithoutExtension(group.Material.AmbientTextureMap);

                        Debug.Assert(ambientFileName != null);

                        var textureHash = ambientFileName.StartsWith("0x")
                            ? uint.Parse(ambientFileName.Substring(2), NumberStyles.AllowHexSpecifier)
                            : Hasher.BinHash(ambientFileName);

                        if (!solidObj.TextureHashes.Contains(textureHash))
                        {
                            solidObj.TextureHashes.Add(textureHash);
                        }

                        var solidMaterial = new SolidObjectMaterial
                        {
                            Name = group.Material.Name,
                            Flags = 0x00224000,
                            MinPoint = new SimpleVector3(0, 0, 0),
                            MaxPoint = new SimpleVector3(0, 0, 0),
                            TextureHash = textureHash,
                            NumTris = (uint)group.Faces.Count,
                            TextureIndex = (byte) solidObj.TextureHashes.IndexOf(textureHash)
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
                            solidObj.Faces[curTriIdx++] = new SolidMeshFace
                            {
                                Vtx1 = (ushort)(face[0].VertexIndex - 1),
                                Vtx2 = (ushort)(face[1].VertexIndex - 1),
                                Vtx3 = (ushort)(face[2].VertexIndex - 1)
                            };
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

                        solidObj.Vertices[curVertIdx++] = new SolidMeshVertex
                        {
                            U = tv.X,
                            V = -tv.Y,
                            X = vertex.X,
                            Y = vertex.Y,
                            Z = vertex.Z
                        };
                    }
                }

                ((SolidList)_chunkManager.Chunks[_selectedSolidList].Resource).Objects.Add(solidObj);
                ((SolidList)_chunkManager.Chunks[_selectedSolidList].Resource).ObjectCount++;

                MessageUtil.ShowInfo("Imported object!");
            }
        }
    }
}

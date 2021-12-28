using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Be.Windows.Forms;
using Common;

namespace ChunkView
{
    public partial class ChunkViewMain : Form
    {
        private const string WindowTitle = "ChunkView v0.0.2 by heyitsleo";
        private const string RecentFilesPath = "RecentFiles.txt";

        private readonly Dictionary<uint, string> _chunkIdDictionary = new Dictionary<uint, string>();
        private List<Chunk> _chunks = new List<Chunk>();
        private string _currentPath;
        private List<string> _recentFiles = new List<string>();

        public ChunkViewMain()
        {
            InitializeComponent();

            treeView1.AfterSelect += TreeView1_OnAfterSelect;
            treeView1.ShowNodeToolTips = true;
            treeView1.ShowLines = false;

            hexBox1.Font = new Font("Consolas", 10.25F);
            hexBox1.ReadOnly = true;
            hexBox1.LineInfoVisible = true;
            hexBox1.ShadowSelectionColor = Color.FromArgb(100, 60, 188, 255);
            hexBox1.StringViewVisible = true;
            hexBox1.UseFixedBytesPerLine = true;
            hexBox1.VScrollBarVisible = true;
            hexBox1.ColumnInfoVisible = true;
            hexBox1.SelectionBackColor = Color.LightGreen;

            var chunkDef = ReadEmbeddedFile("ChunkView.Resources.ChunkDef.txt");

            foreach (var line in chunkDef.Split('\n'))
            {
                var split = line.Split(new[] { ' ' }, 2);

                _chunkIdDictionary.Add(uint.Parse(split[0].Substring(2), NumberStyles.HexNumber), split[1].Trim());
            }

            if (File.Exists(RecentFilesPath))
            {
                _recentFiles.AddRange(File.ReadAllLines(RecentFilesPath).Take(10));

                foreach (var recentFile in _recentFiles)
                {
                    AddRecentFileToMenu(recentFile);
                }

                if (_recentFiles.Count > 0)
                {
                    LoadFile(_recentFiles[0]);
                }
            }
        }

        private void AddRecentFile(string path)
        {
            _recentFiles.Remove(path);
            _recentFiles.Insert(0, path);

            if (_recentFiles.Count > 10)
            {
                _recentFiles.RemoveRange(10, _recentFiles.Count - 10);
            }

            File.WriteAllLines(RecentFilesPath, _recentFiles);
            AddRecentFileToMenu(path);
        }

        private void AddRecentFileToMenu(string path)
        {
            recentFilesToolStripMenuItem.DropDownItems.RemoveByKey(path);
            recentFilesToolStripMenuItem.Enabled = true;

            var recentFileItem = new ToolStripMenuItem(path);
            recentFileItem.Name = path;
            recentFilesToolStripMenuItem.DropDownItems.Insert(0, recentFileItem);
            recentFileItem.Click += delegate { LoadFile(path); };
        }

        private void TreeView1_OnAfterSelect(object sender, TreeViewEventArgs e)
        {
            if (treeView1.SelectedNode?.Tag is Chunk chunk)
            {
                hexBox1.ByteProvider = new DynamicByteProvider(chunk.Data);
            }
            else
            {
                hexBox1.ByteProvider = new DynamicByteProvider(Array.Empty<byte>());
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                LoadFile(openFileDialog.FileName);
            }
        }

        private void LoadFile(string filename)
        {
            messageLabel.Text = $"Loading: {filename}...";
            var stopwatch = new Stopwatch();
            exportToolStripMenuItem.Enabled = false;
            reloadToolStripMenuItem.Enabled = false;

            hexBox1.ByteProvider = new DynamicByteProvider(Array.Empty<byte>());
            treeView1.Nodes.Clear();
            _chunks.Clear();

            using (var br = new BinaryReader(File.OpenRead(filename)))
            {
                stopwatch.Start();

                if (br.BaseStream.Length >= 8)
                {
                    var head = br.ReadUInt32();
                    br.BaseStream.Position -= 4;

                    if (head == 0x5a4c444a)
                    {
                        br.BaseStream.Position += 8;
                        br.ReadUInt32();
                        var compSize = br.ReadUInt32();

                        br.BaseStream.Position = 0;
                        var compressedData = new byte[compSize];

                        if (br.Read(compressedData, 0, compressedData.Length) != compressedData.Length)
                        {
                            throw new Exception(
                                $"failed to read {compressedData.Length} bytes of compressed chunk data");
                        }

                        var decompressedData = Compression.Decompress(compressedData).ToArray();

                        using var decompressedReader = new BinaryReader(new MemoryStream(decompressedData));
                        _chunks = ScanChunks(decompressedReader, decompressedReader.BaseStream.Length);
                    }
                    else
                    {
                        _chunks = ScanChunks(br, br.BaseStream.Length);
                    }

                    stopwatch.Stop();
                }
                else
                {
                    stopwatch.Stop();
                    MessageBox.Show("File is too small.", "ChunkView", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            SuspendLayout();

            messageLabel.Text = $"Loaded {_chunks.Count} chunks from [{filename}] in {stopwatch.ElapsedMilliseconds}ms";
            Text = $"{WindowTitle} - {filename}";
            _currentPath = filename;
            exportToolStripMenuItem.Enabled = true;
            reloadToolStripMenuItem.Enabled = true;

            PopulateChunks(_chunks, treeView1.Nodes.Add(Path.GetFileName(filename)));
            AddRecentFile(filename);
            ResumeLayout(true);
        }

        private void PopulateChunks(IReadOnlyList<Chunk> chunks, TreeNode baseNode)
        {
            for (var index = 0; index < chunks.Count; index++)
            {
                var chunk = chunks[index];
                var nodeText = $"#{index + 1}: ";

                if (_chunkIdDictionary.ContainsKey(chunk.Id))
                {
                    nodeText += _chunkIdDictionary[chunk.Id];
                }
                else if (chunk.Id == 0)
                {
                    nodeText += "(Padding)";
                }
                else
                {
                    nodeText += $"0x{chunk.Id:X8}";
                }

                nodeText += $" @ 0x{chunk.Offset:X8}";

                var chunkNode = baseNode.Nodes.Add(nodeText);
                var infoParts = new List<string>();

                if (chunk.Id != 0)
                {
                    infoParts.Add($"ID: 0x{chunk.Id:X8}");
                }

                infoParts.Add($"Size: {chunk.Size}");

                if (chunk.SubChunks.Count > 0)
                {
                    infoParts.Add($"Children: {chunk.SubChunks.Count}");
                }

                chunkNode.ToolTipText = string.Join(" | ", infoParts);
                chunkNode.Tag = chunk;

                if (chunk.SubChunks.Count > 0)
                {
                    PopulateChunks(chunk.SubChunks, chunkNode);
                }
            }
        }

        private static List<Chunk> ScanChunks(BinaryReader br, long sizeLimit)
        {
            var chunks = new List<Chunk>();
            var endPos = br.BaseStream.Position + sizeLimit;

            while (br.BaseStream.Position < endPos)
            {
                var chunkId = br.ReadUInt32();
                var chunkSize = br.ReadUInt32();

                var chunkOffset = br.BaseStream.Position;
                var chunkEndPos = br.BaseStream.Position + chunkSize;
                var data = new byte[chunkSize];

                if (br.Read(data, 0, data.Length) != data.Length)
                {
                    throw new Exception($"failed to read {data.Length} bytes of chunk data");
                }

                br.BaseStream.Position = chunkOffset;

                // compressed FNG
                if (chunkId == 0x00030210)
                {
                    br.ReadUInt32(); // FNG file name hash

                    // Read compressed data
                    var compressedData = new byte[chunkSize - 4];

                    if (br.Read(compressedData, 0, compressedData.Length) != compressedData.Length)
                    {
                        throw new Exception($"failed to read {compressedData.Length} bytes of compressed FNG data");
                    }

                    var decompressedData = Compression.Decompress(compressedData);
                    var fullDecompressedData = new Span<byte>(new byte[decompressedData.Length + 8]);

                    BitConverter.GetBytes(0x30203).CopyTo(fullDecompressedData[..4]);
                    BitConverter.GetBytes(decompressedData.Length).CopyTo(fullDecompressedData[4..8]);
                    decompressedData.CopyTo(fullDecompressedData[8..]);

                    using var decompressedReader = new BinaryReader(new MemoryStream(fullDecompressedData.ToArray()));
                    chunks.AddRange(ScanChunks(decompressedReader, decompressedReader.BaseStream.Length));
                }
                // container chunk
                else if ((chunkId & 0x80000000) == 0x80000000 || chunkId == 0x00030203)
                {
                    chunks.Add(new Chunk
                    {
                        Id = chunkId,
                        Offset = chunkOffset,
                        Size = chunkSize,
                        SubChunks = ScanChunks(br, chunkSize),
                        IsParent = true,
                        Data = data
                    });
                }
                else
                {
                    chunks.Add(new Chunk
                    {
                        Id = chunkId,
                        Offset = chunkOffset,
                        Size = chunkSize,
                        Data = data
                    });
                }

                br.BaseStream.Position = chunkEndPos;
            }

            return chunks;
        }

        private string ReadEmbeddedFile(string path)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(path);
            using var reader = new StreamReader(stream ?? throw new InvalidOperationException());
            return reader.ReadToEnd();
        }

        internal class Chunk
        {
            public uint Id;

            public long Offset;

            public uint Size;

            public byte[] Data;

            public bool IsParent;

            public List<Chunk> SubChunks = new List<Chunk>();
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var rootNode = treeView1.Nodes[0];
            var chunks = new List<Chunk>();
            foreach (TreeNode node in rootNode.Nodes)
            {
                if (node.Tag is Chunk chunk)
                {
                    chunks.Add(chunk);
                }
            }

            var ofd = new FolderBrowserDialog();
            ofd.Description = "Select output folder";
            ofd.RootFolder = Environment.SpecialFolder.Desktop;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                ExportChunks("root", ofd.SelectedPath, null, chunks);
            }
        }

        private void ExportChunks(string parentName, string directory, Chunk parentChunk, List<Chunk> chunks)
        {
            var chunkCounter = 0;
            if (parentChunk == null)
            {
                foreach (var chunk in chunks)
                {
                    if (chunk.IsParent)
                    {
                        ExportChunks(Path.Combine(directory, $"{parentName}-{chunk.Id:X8}"), directory, chunk,
                            chunk.SubChunks);
                    }
                    else
                    {
                        File.WriteAllBytes(
                            Path.Combine(directory,
                                $"{parentName}-{chunk.Id:X8}-{chunk.Offset:X8}-{++chunkCounter}.bin"), chunk.Data);
                    }
                }
            }
            else
            {
                foreach (var chunk in chunks)
                {
                    if (chunk.IsParent)
                    {
                        ExportChunks(Path.Combine(directory, $"{parentName}-{chunk.Id:X8}"), directory, chunk,
                            chunk.SubChunks);
                    }
                    else
                    {
                        File.WriteAllBytes(
                            Path.Combine(directory,
                                $"{parentName}-{chunk.Id:X8}-{chunk.Offset:X8}-{++chunkCounter}.bin"), chunk.Data);
                    }
                }
            }
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadFile(_currentPath);
        }
    }
}
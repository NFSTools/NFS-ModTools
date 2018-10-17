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
        private const string WindowTitle = "ChunkView v0.0.1 by heyitsleo";

        private readonly Dictionary<uint, string> _chunkIdDictionary = new Dictionary<uint, string>();
        private List<Chunk> _chunks = new List<Chunk>();

        private bool _updatingSelection = false;
        private bool _comparing = false;

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

                _chunkIdDictionary[uint.Parse(split[0].Substring(2), NumberStyles.HexNumber)] = split[1].Trim();
            }
        }

        private void TreeView1_OnAfterSelect(object sender, TreeViewEventArgs e)
        {
            if (treeView1.SelectedNode?.Tag is Chunk chunk)
            {
                if (_comparing)
                {
                    MessageUtil.ShowInfo("Running comparison...");
                    _comparing = false;
                }

                if (treeView1.SelectedNode?.Parent?.Tag is Chunk parentChunk)
                {
                    hexBox1.ByteProvider = new DynamicByteProvider(parentChunk.Data);
                    var scrollOffset = chunk.Offset - parentChunk.Offset - 8;

                    if (scrollOffset >= 0x80)
                    {
                        scrollOffset += (0x80 - scrollOffset % 0x80) + 0x10;
                    }

                    hexBox1.ScrollByteIntoView(scrollOffset);
                    hexBox1.Select(chunk.Offset - parentChunk.Offset - 8, chunk.Size + 8);
                }
                else
                {
                    hexBox1.ByteProvider = new DynamicByteProvider(chunk.Data);
                }

                compareToolStripMenuItem.Enabled = true;
            }
            else
            {
                hexBox1.ByteProvider = new DynamicByteProvider(new byte[0]);
                compareToolStripMenuItem.Enabled = false;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                messageLabel.Text = $"Loading: {openFileDialog.FileName}...";
                var stopwatch = new Stopwatch();

                compareToolStripMenuItem.Enabled = false;
                hexBox1.ByteProvider = new DynamicByteProvider(new byte[0]);
                treeView1.Nodes.Clear();
                _chunks.Clear();
                _chunks = new List<Chunk>();

                GC.Collect();

                using (var fs = File.OpenRead(openFileDialog.FileName))
                using (var br = new BinaryReader(fs))
                {
                    stopwatch.Start();

                    if (br.ReadUInt32() == 0x5a4c444a)
                    {
                        br.BaseStream.Position -= 4;

                        br.BaseStream.Position += 0x8;

                        var outSize = br.ReadUInt32();
                        var compSize = br.ReadUInt32();
                        var outData = new byte[outSize];

                        br.BaseStream.Position = 0;
                        var inData = br.ReadBytes((int)compSize);

                        Compression.Decompress(inData, outData);

                        using (var br2 = new BinaryReader(new MemoryStream(outData)))
                        {
                            _chunks.AddRange(ScanChunks(br2, (uint)br2.BaseStream.Length));
                        }

                        outData = null;
                        inData = null;
                        GC.Collect();
                    }
                    else
                    {
                        br.BaseStream.Position -= 4;
                        _chunks.AddRange(ScanChunks(br, (uint)br.BaseStream.Length));
                    }
                }

                stopwatch.Stop();

                compareToolStripMenuItem.Enabled = true;

                SuspendLayout();
                PopulateChunks(_chunks, treeView1.Nodes.Add(Path.GetFileName(openFileDialog.FileName)));
                ResumeLayout(true);

                messageLabel.Text = $"Loaded {_chunks.Count} chunks from [{openFileDialog.FileName}] in {stopwatch.ElapsedMilliseconds}ms";

                Text = $"{WindowTitle} - {openFileDialog.FileName}";
            }
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
                chunkNode.ToolTipText = $"Size: {chunk.Size} | Children: {chunk.SubChunks.Count}";
                //var chunkNode = baseNode.Nodes.Add($"#{index + 1}: {_chunkIdDictionary.ContainsKey(chunk.Id) ? _chunkIdDictionary[chunk.Id]} @ 0x{chunk.Offset:X8}");
                chunkNode.Tag = chunk;

                if (chunk.SubChunks.Count > 0)
                {
                    PopulateChunks(chunk.SubChunks, chunkNode);
                }
            }
        }

        private List<Chunk> ScanChunks(BinaryReader br, uint sizeLimit)
        {
            var chunks = new List<Chunk>();
            var endPos = br.BaseStream.Position + sizeLimit;

            while (br.BaseStream.Position < endPos)
            {
                var chunkId = br.ReadUInt32();
                var chunkSize = br.ReadUInt32();

                var chunkOffset = (uint)br.BaseStream.Position;
                var chunkEndPos = br.BaseStream.Position + chunkSize;
                var data = br.ReadBytes((int)chunkSize);

                br.BaseStream.Position = chunkOffset;

                // compressed FNG
                if (chunkId == 0x00030210)
                {
                    br.BaseStream.Position += 4;

                    var flag = br.ReadUInt32();
                    var outSize = 0u;

                    if (flag == 0x5a4c444a) // JDLZ
                    {
                        br.BaseStream.Position -= 4;

                        br.BaseStream.Position += 0x8;

                        outSize = br.ReadUInt32();
                    } else if (flag == 0x46465548) // HUFF
                    {
                        br.BaseStream.Position += 4;

                        outSize = br.ReadUInt32();

                        br.BaseStream.Position += 4;
                    }

                    br.BaseStream.Position = chunkOffset + 4;
                    var compData = br.ReadBytes((int) (chunkSize - 4));
                    var outData = new byte[outSize];

                    Compression.Decompress(compData, outData);

                    var ms = new MemoryStream();

                    using (var bw = new BinaryWriter(ms, Encoding.Default, true))
                    {
                        bw.Write(0x00030203);
                        bw.Write(outSize);
                        bw.Write(outData);
                    }

                    ms.Position = 0;

                    using (var br2 = new BinaryReader(ms))
                    {
                        chunks.AddRange(ScanChunks(br2, (uint)br2.BaseStream.Length));
                    }

                    ms.Dispose();

                    outData = null;
                    compData = null;
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

            using (var stream = assembly.GetManifestResourceStream(path))
            using (var reader = new StreamReader(stream ?? throw new InvalidOperationException()))
            {
                return reader.ReadToEnd();
            }
        }

        internal class Chunk
        {
            public uint Id;

            public uint Offset;

            public uint Size;

            public byte[] Data;

            public bool IsParent;

            public List<Chunk> SubChunks = new List<Chunk>();
        }

        private void compareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _comparing = true;
        }
    }
}

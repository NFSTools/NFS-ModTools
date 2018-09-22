using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Common;
using Common.Geometry.Data;
using Common.Stream;
using Common.Stream.Data;
using Common.Textures.Data;
using Pfim;
using SysPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace MapEd
{
    public sealed partial class MapEdMain : Form
    {
        internal class SearchResult<T> where T : ChunkManager.BasicResource
        {
            public List<uint> SectionIds { get; } = new List<uint>();

            public List<TreeNode> Nodes { get; } = new List<TreeNode>();

            public List<T> Resources { get; } = new List<T>();
        }

        private readonly string _baseTitle;
        private readonly PictureBox _pictureBox;

        private readonly List<ChunkManager.Chunk> _chunks;
        private readonly Dictionary<uint, List<int>> _sectionChunksDictionary;

        private GameBundleManager _bundleManager;
        private SolidObject _selectedSolidObject;

        public MapEdMain()
        {
            InitializeComponent();

            _baseTitle = Text;
            _sectionChunksDictionary = new Dictionary<uint, List<int>>();
            _chunks = new List<ChunkManager.Chunk>();
            _pictureBox = new PictureBox { Size = panel1.Size };

            baseTree.ShowNodeToolTips = true;
            subTree.ShowNodeToolTips = true;

            baseTree.AfterSelect += BaseTree_OnAfterSelect;
            subTree.AfterSelect += SubTree_OnAfterSelect;

            MessageUtil.SetAppName("MapEd");
        }

        private void SubTree_OnAfterSelect(object sender, TreeViewEventArgs e)
        {
            exportModelToolStripMenuItem.Enabled = false;
            _selectedSolidObject = null;

            if (subTree.SelectedNode != null)
            {
                var effectiveNode = subTree.SelectedNode;

                if (effectiveNode.Parent != null)
                {
                    effectiveNode = effectiveNode.Parent;
                }

                switch (effectiveNode.Tag)
                {
                    case Texture texture:
                        panel1.Controls.Clear();

                        if (texture.CompressionType == TextureCompression.P8
                            || texture.CompressionType == TextureCompression.A8R8G8B8
                            || texture.CompressionType == TextureCompression.Ati1
                            || texture.CompressionType == TextureCompression.Unknown)
                        {
                            Console.WriteLine(texture.CompressionType + $" {(int)texture.CompressionType:X8}");
                            MessageUtil.ShowError("Preview is unavailable for this format: " + texture.CompressionType);
                        }
                        else
                        {
                            using (var ms = new MemoryStream())
                            {
                                var ddsHeader = new DDSHeader();
                                ddsHeader.Init(texture);

                                using (var bw = new BinaryWriter(ms))
                                {
                                    BinaryUtil.WriteStruct(bw, ddsHeader);
                                    bw.Write(texture.Data, 0, texture.Data.Length);
                                }

                                var dds = Dds.Create(ms.ToArray(), new PfimConfig());

                                SysPixelFormat format;

                                switch (dds.Format)
                                {
                                    case ImageFormat.Rgb24:
                                        format = SysPixelFormat.Format24bppRgb;
                                        break;

                                    case ImageFormat.Rgba32:
                                        format = SysPixelFormat.Format32bppArgb;
                                        break;

                                    default:
                                        throw new Exception("Format not recognized");
                                }

                                unsafe
                                {
                                    fixed (byte* p = dds.Data)
                                    {
                                        var bitmap = new Bitmap((int)texture.Width, (int)texture.Height, dds.Stride,
                                            format, (IntPtr)p);
                                        _pictureBox.Image = bitmap;
                                    }
                                }
                            }

                            panel1.Controls.Add(_pictureBox);
                        }

                        break;
                    case SolidObject solidObject:
                        exportModelToolStripMenuItem.Enabled = true;

                        _selectedSolidObject = solidObject;
                        break;
                }
            }
        }

        private void BaseTree_OnAfterSelect(object sender, TreeViewEventArgs e)
        {
            subTree.Nodes.Clear();
            panel1.Controls.Clear();
            subTree.Visible = false;

            if (baseTree.SelectedNode == null) return;

            switch (baseTree.SelectedNode.Tag)
            {
                case TexturePack tpk:
                    foreach (var texture in tpk.Textures)
                    {
                        var textureNode = subTree.Nodes.Add(texture.Name);

                        textureNode.Tag = texture;
                        textureNode.ToolTipText = texture.Dimensions + $"- Mipmaps: {texture.MipMapCount} - Format: {texture.CompressionType.ToString().ToUpper()}";
                    }

                    subTree.Visible = true;
                    break;
                case SolidList sl:
                    foreach (var solidObject in sl.Objects)
                    {
                        var solidObjectNode = subTree.Nodes.Add(solidObject.Name);

                        solidObjectNode.Tag = solidObject;
                        solidObjectNode.ToolTipText = $"Materials: {solidObject.Materials.Count} - Faces: {solidObject.Faces.Count}";

                        foreach (var material in solidObject.Materials)
                        {
                            var materialNode = solidObjectNode.Nodes.Add(material.Name);
                            materialNode.ToolTipText = $"Texture: 0x{material.TextureHash:X8}";
                        }
                    }

                    subTree.Visible = true;
                    break;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
            {
                var game = GameDetector.DetectGame(folderBrowserDialog.SelectedPath);

                if (game == GameDetector.Game.Unknown)
                {
                    MessageUtil.ShowError("Cannot detect game.");
                    return;
                }

                if (game != GameDetector.Game.World
                    && game != GameDetector.Game.ProStreet)
                {
                    MessageUtil.ShowError($"Unsupported game: {game}");
                    return;
                }

                switch (game)
                {
                    case GameDetector.Game.World:
                        {
                            _bundleManager = new World15Manager();
                            break;
                        }
                    case GameDetector.Game.ProStreet:
                        {
                            _bundleManager = new ProStreetManager();
                            break;
                        }
                    //case GameDetector.Game.MostWanted:
                    //    {
                    //        _bundleManager = new MostWantedManager();
                    //        break;
                    //    }
                    case GameDetector.Game.Unknown:
                    case GameDetector.Game.Carbon:
                        goto default;
                    default: throw new Exception("Shouldn't ever get here");
                }

                messageLabel.Text = $"Loading: {folderBrowserDialog.SelectedPath}";
                var stopwatch = new Stopwatch();

                var tracksHighExists = game == GameDetector.Game.World &&
                                       Directory.Exists(Path.Combine(folderBrowserDialog.SelectedPath, "TracksHigh"));

                _sectionChunksDictionary.Clear();
                _chunks.Clear();

                subTree.Nodes.Clear();
                baseTree.Nodes.Clear();
                panel1.Controls.Clear();

                texturePackToolStripMenuItem.Enabled = true;
                textureToolStripMenuItem.Enabled = true;
                sectionToolStripMenuItem.Enabled = true;
                modelToolStripMenuItem.Enabled = false;
                subTree.Visible = false;

                stopwatch.Start();
#if !DEBUG
                try
                {
#endif
                _bundleManager.ReadFrom(folderBrowserDialog.SelectedPath);

                if (_bundleManager.Bundles.Count == 0)
                {
                    MessageUtil.ShowError("No bundles were found.");
                    return;
                }

                var cm = new ChunkManager(game);

                SuspendLayout();

                foreach (var bundleManagerBundle in _bundleManager.Bundles)
                {
                    FileStream bundleMasterStream = null;
                    var masterStreamPath = Path.Combine(folderBrowserDialog.SelectedPath, "TRACKS",
                        $"STREAM{bundleManagerBundle.Name}.BUN");

                    if (File.Exists(masterStreamPath))
                    {
                        bundleMasterStream = File.OpenRead(masterStreamPath);
                    }

                    var sectionData = new byte[0];

                    foreach (var streamSection in bundleManagerBundle.Sections)
                    {
                        BinaryReader br = null;
                        MemoryStream ms = null;

                        if (streamSection is WorldStreamSection wss)
                        {
                            var filePath = Path.Combine(
                                folderBrowserDialog.SelectedPath,
                                tracksHighExists ? "TracksHigh" : "Tracks",
                                $"STREAM{bundleManagerBundle.Name}_" + (wss.FragmentFileId == 0
                                    ? wss.Number.ToString()
                                    : $"0x{wss.FragmentFileId:X8}") + ".BUN");
                            br = new BinaryReader(File.OpenRead(filePath));
                        }
                        else
                        {
                            if (bundleMasterStream != null)
                            {
                                bundleMasterStream.Position = streamSection.Offset;

                                Array.Resize(ref sectionData, (int)streamSection.Size);
                                bundleMasterStream.Read(sectionData, 0, sectionData.Length);

                                ms = new MemoryStream(sectionData);
                                br = new BinaryReader(ms);
                            }
                        }

                        cm.Reset();
                        cm.Read(br);

                        _sectionChunksDictionary[streamSection.Number] = new List<int>();

                        foreach (var chunk in cm.Chunks)
                        {
                            _chunks.Add(chunk);
                            _sectionChunksDictionary[streamSection.Number].Add(_chunks.Count - 1);
                        }

                        br?.Dispose();
                        ms?.Dispose();
                    }
                }
                
                texturePackToolStripMenuItem.Enabled = true;
                textureToolStripMenuItem.Enabled = true;
                sectionToolStripMenuItem.Enabled = true;
                modelToolStripMenuItem.Enabled = true;

                stopwatch.Stop();

                foreach (var bundle in _bundleManager.Bundles)
                {
                    var bundleNode = baseTree.Nodes.Add(bundle.Name);

                    foreach (var streamSection in bundle.Sections)
                    {
                        var sectionNodeText = $"{streamSection.Name} ({streamSection.Number})";
                        var sectionNode = bundleNode.Nodes.Add(sectionNodeText);
                        sectionNode.Tag = streamSection.Number;

                        sectionNode.ToolTipText =
                            $"Position: {streamSection.Position} - Size: {streamSection.Size}";

                        foreach (var chunkIndex in _sectionChunksDictionary[streamSection.Number])
                        {
                            var chunk = _chunks[chunkIndex];
                            switch (chunk.Resource)
                            {
                                case TexturePack tpk:
                                    var tpkNode = sectionNode.Nodes.Add(tpk.PipelinePath);
                                    tpkNode.Tag = tpk;

                                    break;
                                case SolidList solidList:
                                    var solidListNode = sectionNode.Nodes.Add(solidList.PipelinePath);
                                    solidListNode.Tag = solidList;
                                    break;
                            }
                        }
                    }
                }

                Text = $"{_baseTitle} - {game}";
                messageLabel.Text =
                    $"Loaded {_bundleManager.Bundles.Count} bundles from {folderBrowserDialog.SelectedPath}. Time: {stopwatch.ElapsedMilliseconds}ms";
#if !DEBUG
            }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    MessageUtil.ShowError(ex.Message);
                }
#endif
            }
        }

        private SearchResult<TexturePack> SearchTexturePack(string path)
        {
            var result = new SearchResult<TexturePack>();

            foreach (TreeNode bundleNode in baseTree.Nodes)
            {
                foreach (TreeNode sectionNode in bundleNode.Nodes)
                {
                    foreach (TreeNode subNode in sectionNode.Nodes)
                    {
                        if (!(subNode.Tag is TexturePack tpk)) continue;

                        if (tpk.PipelinePath.ToLowerInvariant().Contains(path.ToLowerInvariant()))
                        {
                            result.Resources.Add(tpk);
                            result.Nodes.Add(subNode);
                            result.SectionIds.Add((uint)sectionNode.Tag);
                        }
                    }
                }
            }

            return result;
        }

        private SearchResult<SolidList> SearchSolidObject(string name)
        {
            var result = new SearchResult<SolidList>();

            foreach (TreeNode bundleNode in baseTree.Nodes)
            {
                foreach (TreeNode sectionNode in bundleNode.Nodes)
                {
                    foreach (TreeNode subNode in sectionNode.Nodes)
                    {
                        if (!(subNode.Tag is SolidList solidList)) continue;
                        if (!solidList.Objects.Any(o => o.Name.ToLowerInvariant().Contains(name.ToLowerInvariant())))
                            continue;
                        result.Nodes.Add(subNode);
                        result.SectionIds.Add((uint)sectionNode.Tag);
                        result.Resources.Add(solidList);
                    }
                }
            }

            return result;
        }

        private SearchResult<TexturePack> RealTextureSearch(string name)
        {
            var result = new SearchResult<TexturePack>();

            foreach (TreeNode bundleNode in baseTree.Nodes)
            {
                foreach (TreeNode sectionNode in bundleNode.Nodes)
                {
                    foreach (TreeNode subNode in sectionNode.Nodes)
                    {
                        if (!(subNode.Tag is TexturePack tpk)) continue;

                        var find = tpk.Find(name);

                        if (find == null) continue;

                        result.Nodes.Add(subNode);
                        result.SectionIds.Add((uint)sectionNode.Tag);
                        result.Resources.Add(tpk);
                    }
                }
            }

            return result;
        }

        private Texture SearchTexture(uint hash)
        {
            foreach (var sectionGroup in _sectionChunksDictionary)
            {
                foreach (var chunkIndex in sectionGroup.Value)
                {
                    var chunk = _chunks[chunkIndex];

                    if (!(chunk.Resource is TexturePack tpk)) continue;

                    var search = tpk.Find(hash);

                    if (search != null)
                    {
                        return search;
                    }
                }
            }

            return null;
        }

        private void ExportTexture(Texture texture, string path)
        {
            using (var fs = File.OpenWrite(path))
            using (var bw = new BinaryWriter(fs))
            {
                var ddsHeader = new DDSHeader();
                ddsHeader.Init(texture);

                BinaryUtil.WriteStruct(bw, ddsHeader);
                bw.Write(texture.Data);
            }
        }

        private void RecursiveClearColor(IEnumerable nodes)
        {
            foreach (TreeNode treeNode in nodes)
            {
                treeNode.BackColor = default(Color);

                if (treeNode.Nodes.Count > 0)
                {
                    RecursiveClearColor(treeNode.Nodes);
                }
            }
        }

        private void ProcessSearchResults<T>(SearchResult<T> results) where T : ChunkManager.BasicResource
        {
            if (results.Resources.Count == 0)
            {
                MessageUtil.ShowError("No results found.");
            }
            else
            {
                if (results.Nodes.Count > 0)
                {
                    baseTree.SelectedNode = results.Nodes[0];

                    foreach (var t in results.Nodes)
                    {
                        t.Parent.BackColor = Color.ForestGreen;
                        t.BackColor = Color.ForestGreen;
                        t.EnsureVisible();
                    }
                }
            }
        }

        private void exportModelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_selectedSolidObject != null)
            {
                saveFileDialog.FileName = _selectedSolidObject.Name + ".obj";

                if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    var mtlFileName = Path.GetFileNameWithoutExtension(saveFileDialog.FileName) + ".mtl";
                    var materialLibPath = Path.Combine(
                        Path.GetDirectoryName(saveFileDialog.FileName),
                        mtlFileName);

                    using (var ms = new FileStream(materialLibPath, FileMode.Create))
                    using (var sw = new StreamWriter(ms))
                    {
                        sw.WriteLine("# Generated by MapEd v0.0.1 by heyitsleo");

                        var hashes = _selectedSolidObject.Materials.Select(m => m.TextureHash)
                            .Distinct()
                            .ToList();
                        var hashMap = new Dictionary<uint, Texture>();
                        var pathMap = new Dictionary<uint, string>();

                        foreach (var hash in hashes)
                        {
                            hashMap[hash] = SearchTexture(hash);

                            if (hashMap[hash] != null)
                            {
                                var texPath = pathMap[hash] = Path.Combine(
                                    Path.GetDirectoryName(saveFileDialog.FileName), $"{hashMap[hash].Name}.dds");
                                ExportTexture(hashMap[hash], texPath);
                            }
                        }

                        foreach (var material in _selectedSolidObject.Materials.ToList())
                        {
                            var repCount = 0;
                            for (var j = 0; j < _selectedSolidObject.Materials.Count; j++)
                            {
                                if (string.Equals(_selectedSolidObject.Materials[j].Name, material.Name))
                                {
                                    var mat = _selectedSolidObject.Materials[j];

                                    if (repCount > 0)
                                    {
                                        mat.Name += $"_{repCount + 1}";
                                    }

                                    _selectedSolidObject.Materials[j] = mat;

                                    repCount++;
                                }
                            }
                        }

                        foreach (var material in _selectedSolidObject.Materials)
                        {
                            sw.WriteLine($"newmtl {material.Name.Replace(' ', '_')}");
                            sw.WriteLine("Ka 255 255 255");
                            sw.WriteLine("Kd 255 255 255");
                            sw.WriteLine("Ks 255 255 255");

                            if (hashMap[material.TextureHash] != null)
                            {
                                sw.WriteLine($"map_Ka {pathMap[material.TextureHash]}");
                                sw.WriteLine($"map_Kd {pathMap[material.TextureHash]}");
                                sw.WriteLine($"map_Ks {pathMap[material.TextureHash]}");
                            }
                            else
                            {
                                sw.WriteLine($"# Unknown texture: 0x{material.TextureHash:X8}");
                            }

                            //sw.WriteLine($"map_Ka 0x{_materials[m].TextureHash:X8}.png");
                            //sw.WriteLine($"map_Kd 0x{_materials[m].TextureHash:X8}.png");
                            //sw.WriteLine($"map_Ks 0x{_materials[m].TextureHash:X8}.png");
                            sw.WriteLine();
                        }
                    }

                    using (var fs = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    using (var sw = new StreamWriter(fs))
                    {
                        sw.WriteLine("# Generated by MapEd v0.0.1 by heyitsleo");
                        sw.WriteLine($"mtllib {mtlFileName}");
                        sw.WriteLine($"obj {_selectedSolidObject.Name}");

                        foreach (var vertex in _selectedSolidObject.Vertices)
                        {
                            sw.WriteLine($"vt {BinaryUtil.FullPrecisionFloat(vertex.U)} {BinaryUtil.FullPrecisionFloat(vertex.V)}");
                        }

                        foreach (var vertex in _selectedSolidObject.Vertices)
                        {
                            sw.WriteLine($"v {BinaryUtil.FullPrecisionFloat(vertex.X)} {BinaryUtil.FullPrecisionFloat(vertex.Y)} {BinaryUtil.FullPrecisionFloat(vertex.Z)}");
                        }

                        var lastMaterial = -1;

                        foreach (var face in _selectedSolidObject.Faces)
                        {
                            if (_selectedSolidObject.MaterialFaces.Any(p => p.Value.Any(f => f.SequenceEqual(face.ShiftedArray()))))
                            {
                                var matIndex = _selectedSolidObject.MaterialFaces.First(p => p.Value.Any(f => f.SequenceEqual(face.ShiftedArray()))).Key;

                                if (matIndex != lastMaterial)
                                {
                                    lastMaterial = matIndex;
                                    sw.WriteLine($"usemtl {_selectedSolidObject.Materials[matIndex].Name.Replace(" ", "_")}");
                                }
                            }
                            else
                            {
                                lastMaterial = -1;
                                sw.WriteLine("usemtl EMPTY");
                            }

                            if (face.Vtx1 >= _selectedSolidObject.MeshDescriptor.NumVerts
                                || face.Vtx2 >= _selectedSolidObject.MeshDescriptor.NumVerts
                                || face.Vtx3 >= _selectedSolidObject.MeshDescriptor.NumVerts) break;
                            sw.WriteLine($"f {face.Shift1 + 1}/{face.Shift1 + 1} {face.Shift2 + 1}/{face.Shift2 + 1} {face.Shift3 + 1}/{face.Shift3 + 1}");
                        }
                    }

                    MessageUtil.ShowInfo("Exported!");
                    Process.Start(Path.GetDirectoryName(saveFileDialog.FileName));
                }
            }
        }

        private void texturePackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RecursiveClearColor(baseTree.Nodes);

            var dialog = new MapEdSearch();

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                if (!string.IsNullOrWhiteSpace(dialog.Query))
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var results = SearchTexturePack(dialog.Query);
                    stopwatch.Stop();

                    messageLabel.Text =
                        $"Completed search for TPK [{dialog.Query}] in {stopwatch.ElapsedMilliseconds}ms";

                    ProcessSearchResults(results);
                }
            }
        }

        private void modelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RecursiveClearColor(baseTree.Nodes);

            var dialog = new MapEdSearch();

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                if (!string.IsNullOrWhiteSpace(dialog.Query))
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var results = SearchSolidObject(dialog.Query);
                    stopwatch.Stop();

                    messageLabel.Text =
                        $"Completed search for object [{dialog.Query}] in {stopwatch.ElapsedMilliseconds}ms";

                    ProcessSearchResults(results);
                }
            }
        }

        private void textureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RecursiveClearColor(baseTree.Nodes);

            var dialog = new MapEdSearch();

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                if (!string.IsNullOrWhiteSpace(dialog.Query))
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var results = RealTextureSearch(dialog.Query);
                    stopwatch.Stop();

                    messageLabel.Text =
                        $"Completed search for texture [{dialog.Query}] in {stopwatch.ElapsedMilliseconds}ms";

                    ProcessSearchResults(results);
                }
            }
        }
    }
}

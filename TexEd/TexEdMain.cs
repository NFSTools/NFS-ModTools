using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Common;
using Common.Textures.Data;
using Pfim;
using SysPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace TexEd
{
    public partial class TexEdMain : Form
    {
        private BindingList<TexturePack> _texturePacks;

        private BindingList<Texture> _textures;

        private readonly BindingSource _tpkSource;

        private readonly BindingSource _textureSource;

        private ChunkManager _chunkManager;

        public TexEdMain()
        {
            InitializeComponent();
            MessageUtil.SetAppName("TexEd");

            tpkDataGrid.AutoGenerateColumns = false;
            textureDataGrid.AutoGenerateColumns = false;

            typeof(DataGridView).InvokeMember(
                "DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null,
                tpkDataGrid,
                new object[] { true });
            typeof(DataGridView).InvokeMember(
                "DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null,
                textureDataGrid,
                new object[] { true });

            _texturePacks = new BindingList<TexturePack>();
            _textures = new BindingList<Texture>();
            _tpkSource = new BindingSource { DataSource = _texturePacks };
            _textureSource = new BindingSource { DataSource = _textures };

            tpkDataGrid.DataSource = _tpkSource;
            textureDataGrid.DataSource = _textureSource;

            openFileDialog.Filter = "Bundle Files (*.BUN, *.BIN)|*.BUN;*.BIN;*.bun;*.bin|All files (*.*)|*.*";

            tpkDataGrid.SelectionChanged += TpkDataGrid_OnSelectionChanged;
            textureDataGrid.SelectionChanged += TextureDataGrid_OnSelectionChanged;
        }

        private void TextureDataGrid_OnSelectionChanged(object sender, EventArgs e)
        {
            if (textureDataGrid.SelectedRows.Count > 0)
            {
                if (textureDataGrid.SelectedRows[0].DataBoundItem is Texture texture)
                {
                    pictureBox1.Image = null;

                    GC.Collect();

                    if (texture.CompressionType == TextureCompression.P8
                        || texture.CompressionType == TextureCompression.A8R8G8B8)
                    {
                        pictureBox1.Image = new Bitmap(Assembly.GetEntryAssembly().
                                                           GetManifestResourceStream("TexEd.Resources.texed-nopreview.png") 
                                                       ?? throw new InvalidOperationException());
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
                                    var bitmap = new Bitmap((int)texture.Width, (int)texture.Height, dds.Stride, format, (IntPtr)p);
                                    pictureBox1.Image = bitmap;
                                }
                            }
                        }
                    }
                }
                else
                {
                    GC.Collect();
                }
            }
            else
            {
                GC.Collect();
                pictureBox1.Image = null;
            }
        }

        private void TpkDataGrid_OnSelectionChanged(object sender, EventArgs e)
        {
            if (tpkDataGrid.SelectedRows.Count > 0)
            {
                textureDataGrid.ClearSelection();

                if (tpkDataGrid.SelectedRows[0].DataBoundItem is TexturePack tpk)
                {
                    _textures = new BindingList<Texture>(tpk.Textures);
                    _textureSource.DataSource = null;
                    _textureSource.DataSource = _textures;
                }
                else
                {
                    _textures = new BindingList<Texture>();
                    _textureSource.DataSource = null;
                    _textureSource.DataSource = _textures;
                }
            }
            else
            {
                _textures = new BindingList<Texture>();
                _textureSource.DataSource = null;
                _textureSource.DataSource = _textures;
                textureDataGrid.ClearSelection();
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageUtil.ShowInfo("Developed by heyitsleo");
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                var fileName = openFileDialog.FileName;

                _chunkManager = null;
                GC.Collect();
                _chunkManager = new ChunkManager(GameDetector.Game.Unknown);

                try
                {
                    messageLabel.Text = $"Loading: {fileName}";

                    _texturePacks.Clear();
                    _textures.Clear();

                    GC.Collect();

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    _chunkManager.Read(fileName);
                    stopwatch.Stop();
                    messageLabel.Text = $"Loaded {fileName} [{stopwatch.ElapsedMilliseconds}ms]";

                    _texturePacks = new BindingList<TexturePack>(
                        _chunkManager.Chunks
                            .Select(c => c.Resource)
                            .Where(r => r is TexturePack)
                            .Cast<TexturePack>()
                            .ToList());
                    _tpkSource.DataSource = null;
                    _tpkSource.DataSource = _texturePacks;

                    if (_texturePacks.Count > 0)
                    {
                        tpkDataGrid.Rows[0].Selected = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    MessageUtil.ShowError(ex.Message);
                }
            }
        }
    }
}

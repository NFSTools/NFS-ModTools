using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Common;
using Common.Geometry;
using Common.Geometry.Data;
using Common.Textures;
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
        private string _fileName;
        private GameDetector.Game _currentGame = GameDetector.Game.Unknown;

        private Texture _previousTexture;

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

                    try
                    {
                        pictureBox1.Image = new DDSImage(texture.GenerateImage()).BitmapImage;
                    }
                    catch (Exception)
                    {
                        MessageUtil.ShowError("Could not display image preview.");
                    }

                    _previousTexture = texture;
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

#if !DEBUG
                try
                {
#endif
                _fileName = fileName;

                var fullDirectory = Path.GetDirectoryName(_fileName);

                if (fullDirectory == null)
                {
                    throw new NullReferenceException("fullDirectory == null");
                }

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
                    && _currentGame != GameDetector.Game.Carbon
                    && _currentGame != GameDetector.Game.Undercover
                    && _currentGame != GameDetector.Game.ProStreet)
                {
                    MessageUtil.ShowError("Unsupported game.");

                    return;
                }

                _chunkManager = new ChunkManager(_currentGame, ChunkManager.ChunkManagerOptions.IgnoreUnknownChunks | ChunkManager.ChunkManagerOptions.SkipNull);

                messageLabel.Text = $"Loading: {_fileName}";

                saveToolStripMenuItem.Enabled = false;
                _texturePacks.Clear();
                _textures.Clear();

                GC.Collect();

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                _chunkManager.Read(_fileName);
                stopwatch.Stop();
                saveToolStripMenuItem.Enabled = true;
                label1.Text = "Packs: 0 - Textures: 0";

                messageLabel.Text = $"Loaded {_fileName} [{stopwatch.ElapsedMilliseconds}ms]";

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

                label1.Text = $"Packs: {_texturePacks.Count} - Textures: {_texturePacks.Sum(tpk => tpk.NumTextures)}";
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

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!File.Exists(_fileName + ".bak"))
            {
                File.Copy(_fileName, _fileName + ".bak");
            }

            SolidListManager slm;

            switch (_currentGame)
            {
                case GameDetector.Game.World:
                    slm = new World15Solids();
                    break;
                default: throw new Exception("nah");
            }

            using (var outputFile = new FileStream(_fileName, FileMode.Create))
            {
                var chunkStream = new ChunkStream(outputFile);

                foreach (var chunk in _chunkManager.Chunks)
                {
                    if (chunk.Resource is TexturePack tpk)
                    {
                        chunkStream.PaddingAlignment(0x80);
                        new DelegateTpk().WriteTexturePack(chunkStream, tpk);
                    }
                    else
                    {
                        chunkStream.BeginChunk(chunk.Id);
                        chunkStream.Write(chunk.Data);
                        chunkStream.EndChunk();
                        chunkStream.PaddingAlignment(0x10);
                    }
                }
            }

            MessageUtil.ShowInfo("Done!");
        }
    }
}

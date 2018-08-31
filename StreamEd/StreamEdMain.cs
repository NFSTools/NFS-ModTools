﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using Common;
using StreamEd.Data;
using StreamEd.Games;

namespace StreamEd
{
    public sealed partial class StreamEdMain : Form
    {
        private GameBundleManager _bundleManager;

        private BindingList<LocationBundle> _bundles;

        private BindingList<StreamSection> _sections;

        private readonly BindingSource _bundlesSource;

        private readonly BindingSource _sectionsSource;

        private const string WindowTitle = "StreamEd v0.0.1 by heyitsleo";

        public StreamEdMain()
        {
            InitializeComponent();

            _bundles = new BindingList<LocationBundle>();
            _sections = new BindingList<StreamSection>();

            _bundlesSource = new BindingSource { DataSource = _bundles };
            _sectionsSource = new BindingSource { DataSource = _sections };

            sectionsDataGrid.AutoGenerateColumns = false;
            sectionsDataGrid.DataSource = _sectionsSource;

            bundleSelectionBox.DataSource = _bundlesSource;
            bundleSelectionBox.SelectedIndexChanged += BundleSelectionBox_OnSelectedIndexChanged;

            typeof(DataGridView).InvokeMember(
                "DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null,
                sectionsDataGrid,
                new object[] { true });

            MessageUtil.SetAppName("StreamEd");

            this.Text = WindowTitle;
        }

        private void BundleSelectionBox_OnSelectedIndexChanged(object sender, EventArgs e)
        {
            Console.WriteLine("Selected index changed");
            if (bundleSelectionBox.SelectedItem is LocationBundle lb)
            {
                _sections = new BindingList<StreamSection>(lb.Sections);
                _sectionsSource.DataSource = null;
                _sectionsSource.DataSource = _sections;
            }
            else
            {
                _sections = new BindingList<StreamSection>();
                _sectionsSource.DataSource = null;
                _sectionsSource.DataSource = _sections;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (gameFolderBrowser.ShowDialog(this) == DialogResult.OK)
            {
                bundleSelectionBox.SelectedIndex = -1;
                saveToolStripMenuItem.Enabled = false;
                exportSectionsToolStripMenuItem.Enabled = false;
                bundleSelectionBox.Enabled = false;

                var game = GameDetector.DetectGame(gameFolderBrowser.SelectedPath);

                if (game != GameDetector.Game.MostWanted)
                {
                    MessageUtil.ShowError("Unsupported game directory.");
                    return;
                }

                messageLabel.Text = $"Loading: {gameFolderBrowser.SelectedPath}";

                switch (game)
                {
                    case GameDetector.Game.MostWanted:
                        {
                            _bundleManager = new MostWantedManager();
                            break;
                        }
                    case GameDetector.Game.Unknown:
                        goto default;
                    default: throw new Exception("Shouldn't ever get here");
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                try
                {
                    _bundleManager.ReadFrom(gameFolderBrowser.SelectedPath);

                    if (_bundleManager.Bundles.Count == 0)
                    {
                        MessageUtil.ShowError("No bundles were found.");
                        return;
                    }

                    stopwatch.Stop();

                    _bundles = new BindingList<LocationBundle>(_bundleManager.Bundles);
                    _bundlesSource.DataSource = _bundles;

                    saveToolStripMenuItem.Enabled = true;
                    exportSectionsToolStripMenuItem.Enabled = true;
                    bundleSelectionBox.Enabled = true;
                    bundleSelectionBox.SelectedIndex = 0;
                    BundleSelectionBox_OnSelectedIndexChanged(null, null);
                    Text = $"{WindowTitle} - {game}";
                    messageLabel.Text =
                        $"Loaded {_bundleManager.Bundles.Count} bundles from {gameFolderBrowser.SelectedPath}. Time: {stopwatch.ElapsedMilliseconds}ms";
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    MessageUtil.ShowError(ex.Message);
                }
                //gameFolderBrowser.SelectedPath
            }
            else
            {
                MessageUtil.ShowInfo("Doing nothing, since the dialog was closed.");
            }
        }

        private void exportSectionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var currentBundle = _bundles[bundleSelectionBox.SelectedIndex];

            if (gameFolderBrowser.ShowDialog(this) == DialogResult.OK)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                _bundleManager.ExtractBundleSections(currentBundle, gameFolderBrowser.SelectedPath);
                stopwatch.Stop();

                MessageUtil.ShowInfo($"Extracted in {stopwatch.ElapsedMilliseconds}ms");
            }
            else
            {
                MessageUtil.ShowInfo("Doing nothing, since the dialog was closed.");
            }

            //_bundleManager.ExtractBundleSections(currentBundle);
        }
    }
}

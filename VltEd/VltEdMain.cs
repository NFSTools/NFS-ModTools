using System;
using System.Windows.Forms;
using Common;
using Common.Windows;

namespace VltEd
{
    public partial class VltEdMain : Form
    {
        private const string WindowTitle = "VltEd v0.0.1 by heyitsleo";

        public VltEdMain()
        {
            InitializeComponent();

            MessageUtil.SetAppName("VltEd");
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
            {
                var gameDirectory = folderBrowserDialog.SelectedPath;
                var detectedGame = GameDetector.DetectGame(gameDirectory);

                if (detectedGame == GameDetector.Game.Unknown)
                {
                    MessageUtil.ShowError("Cannot determine installed game.");
                    return;
                }

                if (detectedGame != GameDetector.Game.MostWanted)
                {
                    MessageUtil.ShowError($"Unsupported game: {detectedGame}");
                    return;
                }

                LoadGame(gameDirectory, detectedGame);

                Text = $"{WindowTitle} - {detectedGame}";
            }
        }

        /// <summary>
        /// Load VLT data from the given game folder.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="game"></param>
        private void LoadGame(string path, GameDetector.Game game)
        {
            //vltPropertyGrid.SelectedObject

            var test = new CustomObjectType {Name = "Whatever"};
            test.Properties.Add(new CustomProperty
            {
                Category = "Test",
                Desc = "Blah blah",
                Name = "Something",
                Type = typeof(int)
            });

            test["Something"] = 123;

            vltPropertyGrid.SelectedObject = test;
        }
    }
}

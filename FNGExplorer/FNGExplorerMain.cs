using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Common;
using Common.Windows;

namespace FNGExplorer
{
    // ReSharper disable once InconsistentNaming
    public partial class FNGExplorerMain : Form
    {
        public FNGExplorerMain()
        {
            InitializeComponent();

            MessageUtil.SetAppName("FNG-Explorer");
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                var fileName = openFileDialog.FileName;

                messageLabel.Text = $"Loading [{fileName}]...";

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                try
                {
                    var packages = ProcessFile(fileName);

                    stopwatch.Stop();
                    messageLabel.Text = $"Loaded [{fileName}] in {stopwatch.ElapsedMilliseconds}ms";

                    foreach (var package in packages)
                    {
                        var pkgNode = treeView1.Nodes.Add($"{package.Name} [{package.Path}]");
                    }
                }
                catch (Exception exception)
                {
                    stopwatch.Stop();
                    MessageUtil.ShowError(exception.Message);
                }
            }
        }

        private IEnumerable<FNGPackage> ProcessFile(string file)
        {
            var packages = new List<FNGPackage>();

            using (var br = new BinaryReader(File.OpenRead(file)))
            {
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    var chunkId = br.ReadUInt32();
                    var chunkSize = br.ReadUInt32();
                    var chunkEnd = br.BaseStream.Position + chunkSize;

                    if (chunkId == 0x00030203)
                    {
                        packages.Add(ReadPackageChunks(br, chunkSize, null));
                    }

                    br.BaseStream.Position = chunkEnd;
                }
            }

            return packages;
        }

        private FNGPackage ReadPackageChunks(BinaryReader br, uint byteLimit, FNGPackage package)
        {
            if (package == null)
            {
                package = new FNGPackage();
            }

            var endOffset = br.BaseStream.Position + byteLimit;

            while (br.BaseStream.Position < endOffset)
            {
                var chunkId = br.ReadUInt32();
                var chunkSize = br.ReadUInt32();
                var chunkEnd = br.BaseStream.Position + chunkSize;

                Console.WriteLine($"0x{chunkId:X8} ({chunkSize} bytes @ {br.BaseStream.Position:X8}):");

                switch (chunkId)
                {
                    case 0xe76e4546: // FEN
                    case 0xcc736552: // Res
                    case 0xcc6a624f: // Obj
                    case 0xea624f46: // FOb
                    {
                        package = ReadPackageChunks(br, chunkSize, package);
                        break;
                    }
                    case 0x64486B50: // PKHd
                    {
                        br.BaseStream.Position += 16;

                        var nameLen = br.ReadInt32();
                        var pathLen = br.ReadInt32();

                        package.Name = new string(br.ReadChars(nameLen)).Trim('\0');
                        package.Path = new string(br.ReadChars(pathLen)).Trim('\0');
                        
                        break;
                    }
                    default:
                        Console.WriteLine(BinaryUtil.HexDump(br.ReadBytes(chunkSize)));
                        break;
                }

                br.BaseStream.Position = chunkEnd;
            }

            return package;
        }

        // ReSharper disable once InconsistentNaming
        internal class FNGPackage
        {
            public string Name { get; set; }

            public string Path { get; set; }

            public List<FNGObject> Objects { get; set; } = new List<FNGObject>();
        }

        // ReSharper disable once InconsistentNaming
        internal class FNGObject
        {

        }
    }
}

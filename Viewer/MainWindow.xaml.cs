using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Common;
using Common.Geometry.Data;
using Common.Textures.Data;
using Common.Windows;
using Microsoft.Win32;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;

namespace Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public class LogModel
        {
            public enum LogLevel
            {
                Info,
                Warning,
                Error
            }

            public class LogEntry
            {
                public LogLevel Level { get; set; }

                public DateTime Timestamp { get; set; }

                public ObservableCollection<Run> Message { get; set; }
            }

            public ObservableCollection<LogEntry> Entries { get; } = new ObservableCollection<LogEntry>();

            public void PushSimple(LogLevel level, string message)
            {
                Brush color;

                switch (level)
                {
                    case LogLevel.Info:
                        color = Brushes.LightSkyBlue;
                        break;
                    case LogLevel.Error:
                        color = Brushes.Red;
                        break;
                    case LogLevel.Warning:
                        color = Brushes.DarkOrange;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(level), level, null);
                }

                Entries.Insert(0, new LogEntry
                {
                    Level = level,
                    Timestamp = DateTime.Now,
                    Message = new ObservableCollection<Run>
                    {
                        new Run(message)
                        {
                            Foreground = color
                        }
                    }
                });
            }
        }

        private GameDetector.Game _game;
        private ICommand _modelCheckCommand;
        public RenderManager RenderManager { get; }

        public ICommand ModelCheckCommand
        {
            get
            {
                return _modelCheckCommand ?? (_modelCheckCommand = new CommandHandler<SolidObjectAsset>(asset =>
                {
                    if (asset.IsSelected != null && (bool)asset.IsSelected)
                    {
                        RenderManager.EnableSolid(asset.Resource);
                    }
                    else
                    {
                        RenderManager.DisableSolid(asset.Resource);
                    }
                }, true));
            }
        }

        private ICommand _textureCheckCommand;

        public ICommand TextureCheckCommand
        {
            get
            {
                return _textureCheckCommand ?? (_textureCheckCommand = new CommandHandler<TextureAsset>(asset =>
                {
                    if (asset.IsSelected != null && (bool)asset.IsSelected)
                    {
                        RenderManager.EnableTexture(asset.Resource);
                    }
                    else
                    {
                        RenderManager.DisableTexture(asset.Resource);
                    }
                }, true));
            }
        }

        private ICommand _texturePackCheckCommand;

        public ICommand TexturePackCheckCommand
        {
            get
            {
                return _texturePackCheckCommand ?? (_texturePackCheckCommand = new CommandHandler<TexturePackAsset>(
                    asset =>
                    {
                        if (asset.IsSelected != null && (bool)asset.IsSelected)
                        {
                            foreach (var fileAssetContainer in asset.SubAssets)
                            {
                                fileAssetContainer.IsSelected = true;
                                RenderManager.EnableTexture(((TextureAsset)fileAssetContainer).Resource);
                            }
                        }
                        else
                        {
                            foreach (var fileAssetContainer in asset.SubAssets)
                            {
                                fileAssetContainer.IsSelected = false;
                                RenderManager.DisableTexture(((TextureAsset)fileAssetContainer).Resource);
                            }
                        }
                    }, true));
            }
        }

        private ICommand _modelPackCheckCommand;

        public ICommand ModelPackCheckCommand
        {
            get
            {
                return _modelPackCheckCommand ?? (_modelPackCheckCommand = new CommandHandler<SolidListAsset>(asset =>
                {
                    if (asset.IsSelected != null && (bool)asset.IsSelected)
                    {
                        foreach (var fileAssetContainer in asset.SubAssets)
                        {
                            var solidObject = ((SolidObjectAsset)fileAssetContainer).Resource;
                            fileAssetContainer.IsSelected = true;
                            RenderManager.EnableSolid(solidObject);
                        }
                    }
                    else
                    {
                        foreach (var fileAssetContainer in asset.SubAssets)
                        {
                            var solidObject = ((SolidObjectAsset)fileAssetContainer).Resource;
                            fileAssetContainer.IsSelected = false;
                            RenderManager.DisableSolid(solidObject);
                        }
                    }
                }, true));
            }
        }

        private ICommand _sectionCheckCommand;

        private ICommand _gotoModelCommand;

        public ICommand GotoModelCommand
        {
            get
            {
                return _gotoModelCommand ?? (_gotoModelCommand = new CommandHandler<SolidObjectAsset>(asset =>
                {
                    //if (_renderObjects[asset.Resource.Hash].EnableTransform)
                    //{
                    //    Viewport.Camera.Position = new Point3D(
                    //        asset.Resource.Transform[3, 0],
                    //        asset.Resource.Transform[3, 1], 
                    //        asset.Resource.Transform[3, 2]);
                    //}
                }, true));
            }
        }

        public ObservableCollection<FileContainer> Containers { get; } = new ObservableCollection<FileContainer>();

        public LogModel Log { get; } = new LogModel();

        public PerspectiveCamera Camera { get; } = new PerspectiveCamera();

        public MainWindow()
        {
            InitializeComponent();

            MessageUtil.SetAppName("NFS Viewer");

            // RenderManager.Instance.SetModelVisualManager(MainVisual);

            Title = $"NFS Viewer v{Assembly.GetExecutingAssembly().GetName().Version}";
            Log.PushSimple(LogModel.LogLevel.Info, "Waiting...");
            RenderManager = new RenderManager();
            
            ModelViewport.ModelUpDirection = new Vector3D(0, 0, 1);
        }

        private void OpenItem_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog()
            {
                Filter = "Bundle Files|*.BUN;*.BIN|All Files|*.*",
                Multiselect = true,
                RestoreDirectory = true,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (ofd.ShowDialog() == true)
            {
                _game = GameDetector.Game.Unknown;
                var canContinue = true;
                var fileList = ofd.FileNames.ToList();

                Title = $"NFS Viewer v{Assembly.GetExecutingAssembly().GetName().Version}";

                foreach (var fileName in fileList.ToList())
                {
                    var fullDirectory = Path.GetDirectoryName(fileName);

                    if (fullDirectory == null)
                    {
                        throw new NullReferenceException("fullDirectory == null");
                    }

                    var parentDirectory = Directory.GetParent(fullDirectory);

                    if (string.Equals("CARS", parentDirectory.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        var texPath = Path.Combine(parentDirectory.FullName, "TEXTURES.BIN");
                        if (!fileList.Contains(texPath))
                            fileList.Add(texPath);
                        parentDirectory = Directory.GetParent(parentDirectory.FullName);
                    }
                    else if (string.Equals("FRONTEND", parentDirectory.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        parentDirectory = Directory.GetParent(parentDirectory.FullName);
                    }
                    else if (string.Equals("TRACKS", parentDirectory.Name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        parentDirectory = Directory.GetParent(parentDirectory.FullName);
                    }

                    var detectedGame = GameDetector.DetectGame(parentDirectory.FullName);

                    if (detectedGame != GameDetector.Game.World
                        && detectedGame != GameDetector.Game.MostWanted
                        && detectedGame != GameDetector.Game.Carbon
                        && detectedGame != GameDetector.Game.ProStreet
                        && detectedGame != GameDetector.Game.Underground
                        && detectedGame != GameDetector.Game.Underground2
                        && detectedGame != GameDetector.Game.Undercover)
                    {
                        MessageUtil.ShowError("Unsupported game!");
                        canContinue = false;
                        break;
                    }

                    if (_game != GameDetector.Game.Unknown && _game != detectedGame)
                    {
                        MessageUtil.ShowError("Don't mess with me like this");
                        canContinue = false;
                        break;
                    }

                    _game = detectedGame;
                }

                if (!canContinue)
                {
                    MessageUtil.ShowError("Cannot open files.");
                    return;
                }

                Log.PushSimple(LogModel.LogLevel.Info, $"Loading {fileList.Count} file(s) in {_game} mode");

                // Data cleanup
                Containers.Clear();
                RenderManager.Reset();
                // RenderManager.Instance.Reset();
                AssetRegistry.Instance.Reset();

                foreach (var file in fileList)
                {
                    var stopwatch = new Stopwatch();
                    var cm = new ChunkManager(_game);

                    stopwatch.Start();
                    cm.Read(file);
                    stopwatch.Stop();

                    Log.PushSimple(LogModel.LogLevel.Info, $"Loaded [{file}] in {stopwatch.ElapsedMilliseconds}ms");

                    var resources = cm.Chunks.Where(c => c.Resource != null).Select(c => c.Resource).ToList();

                    var container = new FileContainer
                    {
                        FileName = Path.GetFileName(file),
                        InnerFiles = new ObservableCollection<FileAssetContainer>(),
                        Assets = new ObservableCollection<FileAssetContainer>(resources.Select(r =>
                        {
                            FileAssetContainer fac = new NullAsset();

                            switch (r)
                            {
                                case TexturePack tpk:
                                    fac = new TexturePackAsset
                                    {
                                        Resource = tpk,
                                        SubAssets = new ObservableCollection<FileAssetContainer>()
                                    };

                                    foreach (var texture in tpk.Textures.OrderBy(t => t.Name))
                                    {
                                        fac.SubAssets.Add(new TextureAsset
                                        {
                                            Resource = texture,
                                            IsSelected = false
                                        });
                                    }

                                    break;
                                case SolidList solidList:
                                    fac = new SolidListAsset
                                    {
                                        Resource = solidList,
                                        SubAssets = new ObservableCollection<FileAssetContainer>()
                                    };

                                    foreach (var solidObject in solidList.Objects.OrderBy(o => o.Name))
                                    {
                                        fac.SubAssets.Add(new SolidObjectAsset
                                        {
                                            Resource = solidObject,
                                            IsSelected = false
                                        });
                                    }


                                    break;
                            }

                            return fac;
                        }))
                    };

                    Containers.Add(container);
                }
            }
        }

        private void ResetCameraItem_OnClick(object sender, RoutedEventArgs e)
        {
        }
    }

    public class CommandHandler<T> : ICommand
    {
        private Action<T> _action;
        private bool _canExecute;

        public CommandHandler(Action<T> action, bool canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _action((T)parameter);
        }
    }
}
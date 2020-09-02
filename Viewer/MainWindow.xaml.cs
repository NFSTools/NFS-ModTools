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
using Common;
using Common.Geometry.Data;
using Common.Textures.Data;
using Common.TrackStream;
using Common.TrackStream.Data;
using Microsoft.WindowsAPICodePack.Dialogs;
using SharpDX;
using SharpGL;
using SharpGL.SceneGraph;

namespace Viewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public class ObjectRenderInfo
        {
            public SolidObject SolidObject { get; set; }

            public bool EnableTransform { get; set; }
        }

        public class LogModel : PropertyNotifier
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
        public ICommand ModelCheckCommand
        {
            get
            {
                return _modelCheckCommand ?? (_modelCheckCommand = new CommandHandler<SolidObjectAsset>(asset =>
                           {
                               if (asset.IsSelected != null && (bool) asset.IsSelected)
                               {
                                   RenderManager.Instance.AddRenderObject(asset.Resource);
                                   RenderManager.Instance.UpdateScene();
                               }
                               else
                               {
                                   RenderManager.Instance.RemoveRenderObject(asset.Resource);
                                   RenderManager.Instance.UpdateScene();
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
                    if (asset.IsSelected != null && (bool) asset.IsSelected)
                    {
                        RenderManager.Instance.AddRenderTexture(asset.Resource);
                        RenderManager.Instance.UpdateScene();
                    }
                    else
                    {
                        RenderManager.Instance.RemoveRenderTexture(asset.Resource);
                        RenderManager.Instance.UpdateScene();
                    }
                }, true));
            }
        }

        private ICommand _texturePackCheckCommand;
        public ICommand TexturePackCheckCommand
        {
            get
            {
                return _texturePackCheckCommand ?? (_texturePackCheckCommand = new CommandHandler<TexturePackAsset>(asset =>
                {
                    if (asset.IsSelected != null && (bool) asset.IsSelected)
                    {
                        foreach (var fileAssetContainer in asset.SubAssets)
                        {
                            fileAssetContainer.IsSelected = true;
                            RenderManager.Instance.AddRenderTexture(((TextureAsset)fileAssetContainer).Resource);
                        }

                        RenderManager.Instance.UpdateScene();
                    }
                    else
                    {
                        foreach (var fileAssetContainer in asset.SubAssets)
                        {
                            fileAssetContainer.IsSelected = false;
                            RenderManager.Instance.RemoveRenderTexture(((TextureAsset)fileAssetContainer).Resource);
                        }

                        RenderManager.Instance.UpdateScene();
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
                    if (asset.IsSelected != null && (bool) asset.IsSelected)
                    {
                        foreach (var fileAssetContainer in asset.SubAssets)
                        {
                            var solidObject = ((SolidObjectAsset)fileAssetContainer).Resource;
                            //var nameSplit = solidObject.Name.Split('_');

                            //// check for LOD
                            //if (nameSplit.Length >= 3)
                            //{
                            //    var lodMatch = false;
                            //    var lod = "";

                            //    for (var j = nameSplit.Length - 1; j >= 0; j--)
                            //    {
                            //        var part = nameSplit[j].ToLower().Trim();

                            //        if (part.Length == 0)
                            //            continue;

                            //        if (part.Length == 1)
                            //        {
                            //            if (part[0] >= 'a' && part[0] <= 'z')
                            //            {
                            //                if (part[0] == 'a')
                            //                {
                            //                    lodMatch = true;
                            //                }
                            //            }
                            //        } else if (part.Length == 2)
                            //        {
                            //            if (uint.TryParse(part.Substring(0, 1), out _))
                            //            {
                            //                if (part[1] >= 'a' && part[1] <= 'z')
                            //                {
                            //                    if (part[1] == 'a')
                            //                    {
                            //                        lodMatch = true;
                            //                    }
                            //                }
                            //            }
                            //        }
                            //    }

                            //    if (!lodMatch)
                            //    {
                            //        continue;
                            //    }
                            //}

                            RenderManager.Instance.AddRenderObject(solidObject);
                            fileAssetContainer.IsSelected = true;
                        }

                        RenderManager.Instance.UpdateScene();
                    }
                    else
                    {
                        foreach (var fileAssetContainer in asset.SubAssets)
                        {
                            fileAssetContainer.IsSelected = false;

                            RenderManager.Instance.RemoveRenderObject(((SolidObjectAsset)fileAssetContainer).Resource);
                        }

                        RenderManager.Instance.UpdateScene();
                    }
                }, true));
            }
        }

        private ICommand _sectionCheckCommand;

        public ICommand SectionCheckCommand
        {
            get
            {
                return _sectionCheckCommand ?? (_sectionCheckCommand = new CommandHandler<SectionContainer>(asset =>
                {
                    if (asset.IsSelected != null && (bool) asset.IsSelected)
                    {
                        foreach (var subAsset in asset.SubAssets)
                        {
                            subAsset.IsSelected = true;

                            if (subAsset is SolidListAsset solidList)
                            {
                                ModelPackCheckCommand.Execute(solidList);
                            } else if (subAsset is TexturePackAsset tpk)
                            {
                                TexturePackCheckCommand.Execute(tpk);
                            }
                            else
                            {
                                asset.IsSelected = null;
                            }
                        }
                    }
                    else
                    {
                        foreach (var subAsset in asset.SubAssets)
                        {
                            subAsset.IsSelected = false;

                            if (subAsset is SolidListAsset solidList)
                            {
                                ModelPackCheckCommand.Execute(solidList);
                            }
                            else if (subAsset is TexturePackAsset tpk)
                            {
                                TexturePackCheckCommand.Execute(tpk);
                            }
                        }
                    }

                    RenderManager.Instance.UpdateScene();
                }, true));
            }
        }

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

        public MainWindow()
        {
            InitializeComponent();

            MessageUtil.SetAppName("NFS Viewer");

            RenderManager.Instance.SetViewport(Viewport);
            RenderManager.Instance.SetModelVisualManager(MainVisual);

            Title = $"NFS Viewer v{Assembly.GetExecutingAssembly().GetName().Version}";
            Log.PushSimple(LogModel.LogLevel.Info, "Waiting...");
        }

        private void OpenMapItem_OnClick(object sender, RoutedEventArgs e)
        {
            var openFolder = new CommonOpenFileDialog
            {
                Filters =
                {
                    new CommonFileDialogFilter("Bundle Files", "*.BUN;*.BIN"),
                    new CommonFileDialogFilter("All Files", "*.*")
                },
                Multiselect = true,
                RestoreDirectory = true,
                EnsureFileExists = true,
                EnsurePathExists = true,
            };

            if (openFolder.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var fileName = openFolder.FileName;
                var fullDirectory = new DirectoryInfo(fileName).Parent;

                if (fullDirectory?.Name.ToLower() != "tracks"
                    && fullDirectory?.Name.ToLower() != "trackshigh")
                {
                    MessageUtil.ShowError("Looks like you're using the wrong menu command...");
                    return;
                }

                var parentDirectory = fullDirectory.Parent;

                if (parentDirectory != null)
                {
                    var game = _game = GameDetector.DetectGame(parentDirectory.FullName);

                    if (game == GameDetector.Game.Unknown)
                    {
                        MessageUtil.ShowError("Cannot determine installed game.");
                        return;
                    }

                    Log.PushSimple(LogModel.LogLevel.Info, $"Loading [{fileName}] in {game} mode");

                    GameBundleManager gbm;

                    switch (game)
                    {
                        case GameDetector.Game.MostWanted:
                            {
                                gbm = new MostWantedManager();
                                break;
                            }
                        case GameDetector.Game.Carbon:
                            {
                                gbm = new CarbonManager();
                                break;
                            }
                        case GameDetector.Game.ProStreet:
                        case GameDetector.Game.ProStreetTest:
                            {
                                gbm = new ProStreetManager();
                                break;
                            }
                        case GameDetector.Game.Undercover:
                            {
                                gbm = new UndercoverManager();
                                break;
                            }
                        case GameDetector.Game.World:
                            {
                                gbm = new World15Manager();
                                break;
                            }
                        default:
                            throw new ArgumentOutOfRangeException(nameof(game), game, "Invalid game!");
                    }

                    Containers.Clear();
                    //MainVisual.Children.Clear();
                    RenderManager.Instance.Reset();
                    AssetRegistry.Instance.Reset();

                    LocationBundle bundle;

                    try
                    {
                        bundle = gbm.ReadLocationBundle(fileName);
                    }
                    catch (Exception)
                    {
                        MessageUtil.ShowError("This is not a map file.");
                        return;
                    }

                    //var bundle = gbm.ReadLocationBundle(fileName);
                    var rootContainer = new FileContainer
                    {
                        FileName = fileName,
                        Assets = new ObservableCollection<FileAssetContainer>()
                    };

                    BinaryReader masterStream = null;

                    if (game != GameDetector.Game.World)
                    {
                        masterStream = new BinaryReader(
                            File.OpenRead(
                                Path.Combine(fullDirectory.FullName, $"STREAM{bundle.Name}.BUN")));
                    }

                    if (game == GameDetector.Game.ProStreet
                        && (string.Equals(bundle.Name.ToLower(), "l6r_lodtest")
                            || string.Equals(bundle.Name.ToLower(), "l6r_lighting")
                            || string.Equals(bundle.Name.ToLower(), "l6r_nis")
                            || string.Equals(bundle.Name.ToLower(), "l6r_testtrack")))
                    {
                        game = GameDetector.Game.ProStreetTest;
                    }

                    var stopwatch = new Stopwatch();
                    var cm = new ChunkManager(game,
                        ChunkManager.ChunkManagerOptions.IgnoreUnknownChunks | ChunkManager.ChunkManagerOptions.SkipNull);
                    stopwatch.Start();

                    foreach (var section in bundle.Sections.OrderBy(s => s.Number))
                    {
                        cm.Reset();
                        
                        var sectionContainer = new SectionContainer
                        {
                            Section = section,
                            SubAssets = new ObservableCollection<FileAssetContainer>()
                        };

                        // NFS:World loading requires creation of independent file streams
                        if (section is WorldStreamSection wss)
                        {
                            var computedPath = Path.Combine(fullDirectory.FullName,
                                wss.FragmentFileId == 0
                                    ? $"STREAM{bundle.Name}_{wss.Number}.BUN"
                                    : $"STREAM{bundle.Name}_0x{wss.FragmentFileId:X8}.BUN");
                            cm.Read(computedPath);
                        }
                        else
                        {
                            if (masterStream != null)
                            {
                                masterStream.BaseStream.Position = section.Offset;

                                var sectionData = masterStream.ReadBytes(section.Size);

                                using (var br = new BinaryReader(new MemoryStream(sectionData)))
                                {
                                    cm.Read(br);
                                }
                            }
                        }

                        var resources = cm.Chunks.Where(c => c.Resource != null).Select(c => c.Resource).ToList();

                        sectionContainer.SubAssets = new ObservableCollection<FileAssetContainer>(resources.Select(r =>
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

                                    foreach (var solidObj in solidList.Objects.OrderBy(o => o.Name))
                                    {
                                        fac.SubAssets.Add(new SolidObjectAsset
                                        {
                                            Resource = solidObj,
                                            IsSelected = false
                                        });
                                    }

                                    break;
                            }

                            return fac;
                        }));

                        rootContainer.Assets.Add(sectionContainer);
                    }

                    stopwatch.Stop();

                    Log.PushSimple(LogModel.LogLevel.Info, $"Loaded {fileName} [{stopwatch.ElapsedMilliseconds}ms]");

                    Containers.Add(rootContainer);

                    masterStream?.Dispose();

                    Title = $"NFS Viewer v{Assembly.GetExecutingAssembly().GetName().Version} - [{fileName} ({game})]";
                }
            }
        }

        private void OpenItem_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new CommonOpenFileDialog
            {
                Filters =
                {
                    new CommonFileDialogFilter("Bundle Files", "*.BUN;*.BIN"),
                    new CommonFileDialogFilter("All Files", "*.*")
                },
                Multiselect = true,
                RestoreDirectory = true,
                EnsureFileExists = true,
                EnsurePathExists = true,
            };

            if (ofd.ShowDialog() == CommonFileDialogResult.Ok)
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
                RenderManager.Instance.Reset();
                AssetRegistry.Instance.Reset();

                foreach (var file in fileList)
                {
                    var stopwatch = new Stopwatch();
                    var cm = new ChunkManager(_game, ChunkManager.ChunkManagerOptions.IgnoreUnknownChunks | ChunkManager.ChunkManagerOptions.SkipNull);

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

                                    // DEFAULT + compression = car
                                    if (solidList.ClassType == "DEFAULT" 
                                        && solidList.Objects.Any(o => o.IsCompressed)
                                        && solidList.Objects.Any(o => o.Name.Contains("KIT")))
                                    {
                                        foreach (var grouping in solidList.Objects.GroupBy(o =>
                                        {
                                            if (o.Name.Contains("_KIT"))
                                            {
                                                var stripped = o.Name
                                                    .Substring(o.Name.IndexOf("KIT", StringComparison.Ordinal) + 3)
                                                    .Substring(0, o.Name.Contains("KITW") ? 3 : 2);

                                                return $"Kit {stripped.ToUpper()}";
                                            }

                                            return "Root";
                                        }))
                                        {
                                            var sfac = new SolidListAsset
                                            {
                                                IsSelected = false,
                                                SubAssets = new ObservableCollection<FileAssetContainer>(),
                                                Resource = new SolidList
                                                {
                                                    PipelinePath = grouping.Key,
                                                    ClassType = "KIT"
                                                }
                                            };

                                            sfac.Resource.Objects.AddRange(grouping);

                                            foreach (var solidObject in grouping.OrderBy(o => o.Name))
                                            {
                                                sfac.SubAssets.Add(new SolidObjectAsset
                                                {
                                                    Resource = solidObject,
                                                    IsSelected = false
                                                });
                                            }

                                            fac.SubAssets.Add(sfac);
                                        }
                                    }
                                    else
                                    {
                                        foreach (var solidObject in solidList.Objects.OrderBy(o => o.Name))
                                        {
                                            fac.SubAssets.Add(new SolidObjectAsset
                                            {
                                                Resource = solidObject,
                                                IsSelected = false
                                            });
                                        }
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

        private void OpenGLControl_OpenGLDraw(object sender, OpenGLEventArgs args)
        {
        }

        private void OpenGLControl_OnOpenGLInitialized(object sender, OpenGLEventArgs args)
        {
            return;
            args.OpenGL.Enable(OpenGL.GL_DEPTH_TEST);

            args.OpenGL.GenVertexArrays(1, _vertexArrays);
            args.OpenGL.BindVertexArray(_vertexArrays[0]);

            args.OpenGL.GenBuffers(1, _vertexBuffers);

            args.OpenGL.BindBuffer(OpenGL.GL_ARRAY_BUFFER, _vertexBuffers[0]);
            args.OpenGL.BufferData(OpenGL.GL_ARRAY_BUFFER, new float[]
            {
                -0.5f,  0.5f, 1.0f, 0.0f, 0.0f, // Top-left
                0.5f,  0.5f, 0.0f, 1.0f, 0.0f, // Top-right
                0.5f, -0.5f, 0.0f, 0.0f, 1.0f, // Bottom-right
                -0.5f, -0.5f, 1.0f, 1.0f, 1.0f  // Bottom-left
            }, OpenGL.GL_STATIC_DRAW);

            args.OpenGL.GenBuffers(1, _elementArrays);

            ushort[] elements =
            {
                0, 1, 2,
                2, 3, 0
            };

            args.OpenGL.BindBuffer(OpenGL.GL_ELEMENT_ARRAY_BUFFER, _elementArrays[0]);
            args.OpenGL.BufferData(OpenGL.GL_ELEMENT_ARRAY_BUFFER, elements, OpenGL.GL_STATIC_DRAW);


        }

        private readonly uint[] _vertexArrays = new uint[1];
        private readonly uint[] _vertexBuffers = new uint[1];
        private readonly uint[] _elementArrays = new uint[1];
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Geometry.Data;
using Common.Textures.Data;
using Common.TrackStream.Data;

namespace Viewer
{
    /// <summary>
    /// Keeps track of texture packs and object packs, and allows for quick lookups.
    /// </summary>
    public class AssetRegistry
    {
        private readonly Dictionary<string, Dictionary<uint, FileAssetContainer>> _sectionAssets = new Dictionary<string, Dictionary<uint, FileAssetContainer>>();

        private AssetRegistry() { }

        private static AssetRegistry _instance;
        private static readonly object InstanceLock = new object();

        public static AssetRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new AssetRegistry();
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Add a section to the registry.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="assets"></param>
        public void CreateSection(string key, List<FileAssetContainer> assets = null)
        {
            _sectionAssets[key] = new Dictionary<uint, FileAssetContainer>();

            if (assets == null) return;

            foreach (var assetContainer in assets)
            {
                switch (assetContainer)
                {
                    case SolidListAsset sla:
                    {
                        _sectionAssets[key][Hasher.BinHash(sla.Resource.PipelinePath)] = sla;
                        break;
                    }
                    case TexturePackAsset tpk:
                    {
                        _sectionAssets[key][tpk.Resource.Hash] = tpk;
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(assetContainer), $"No hash provider for: {assetContainer.GetType()}");
                }
            }
        }

        public TexturePackAsset FindTexturePack(string sectionKey, uint hash)
        {
            return _sectionAssets[sectionKey].First(a => a.Key == hash).Value as TexturePackAsset;
        }

        public SolidListAsset FindSolidList(string sectionKey, uint hash)
        {
            return _sectionAssets[sectionKey].First(a => a.Key == hash).Value as SolidListAsset;
        }

        public TexturePackAsset FindTexturePack(uint hash)
        {
            foreach (var section in _sectionAssets)
            {
                var result = FindTexturePack(section.Key, hash);

                if (result != null)
                    return result;
            }

            return null;
        }

        public Texture FindTexture(uint hash)
        {
            foreach (var section in _sectionAssets)
            {
                foreach (var fileAssetContainer in section.Value)
                {
                    if (fileAssetContainer.Value is TexturePackAsset tpk)
                    {
                        var texture = tpk.Resource.Textures.Find(t => t.TexHash == hash);

                        if (texture != null)
                        {
                            return texture;
                        }
                    }
                }
            }

            return null;
        }

        public SolidListAsset FindSolidList(uint hash)
        {
            foreach (var section in _sectionAssets)
            {
                var result = FindSolidList(section.Key, hash);

                if (result != null)
                    return result;
            }

            return null;
        }

        public void Reset()
        {
            _sectionAssets.Clear();
        }
    }

    public abstract class FileAssetContainer : INotifyPropertyChanged
    {
        private bool? _isSelected = false;

        public ObservableCollection<FileAssetContainer> SubAssets { get; set; }

        public bool? IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SimpleAssetGroup : FileAssetContainer
    {
        public string Name { get; set; }
    }

    public class SolidListAsset : FileAssetContainer
    {
        private SolidList _resource;

        public SolidList Resource
        {
            get => _resource;
            set
            {
                _resource = value;
                NotifyPropertyChanged();
            }
        }
    }

    public class TexturePackAsset : FileAssetContainer
    {
        private TexturePack _resource;

        public TexturePack Resource
        {
            get => _resource;
            set
            {
                _resource = value;
                NotifyPropertyChanged();
            }
        }
    }

    public class SolidObjectAsset : FileAssetContainer
    {
        private SolidObject _resource;

        public SolidObject Resource
        {
            get => _resource;
            set
            {
                _resource = value;
                NotifyPropertyChanged();
            }
        }
    }

    public class TextureAsset : FileAssetContainer
    {
        private Texture _resource;

        public Texture Resource
        {
            get => _resource;
            set
            {
                _resource = value;
                NotifyPropertyChanged();
            }
        }
    }

    public class NullAsset : FileAssetContainer { }

    public class FileContainer
    {
        public string FileName { get; set; }

        public ObservableCollection<FileAssetContainer> InnerFiles { get; set; } = new ObservableCollection<FileAssetContainer>();

        public ObservableCollection<FileAssetContainer> Assets { get; set; } = new ObservableCollection<FileAssetContainer>();
    }

    public class SectionContainer : FileAssetContainer
    {
        public StreamSection Section { get; set; }
    }
}

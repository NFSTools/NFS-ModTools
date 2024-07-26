using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using FBXSharp.Core;
using ICSharpCode.SharpZipLib.Zip.Compression;

namespace FBXSharp
{
    public class FBXExporter7400
    {
        public struct Options
        {
            public string Creator { get; set; }
            public string AppVendor { get; set; }
            public string AppName { get; set; }
            public string AppVersion { get; set; }
            public DateTime SaveTime { get; set; }
            public bool CompressData { get; set; }
        }

        private readonly IScene m_scene;

        public FBXExporter7400(IScene scene)
        {
            if (scene is null) throw new ArgumentNullException("Scene provided cannot be null");

            m_scene = scene;
        }

        private static byte[] GetFileID(in DateTime time)
        {
            // https://github.com/hamish-milne/FbxWriter/blob/master/Fbx/FbxBinary.cs

            var strinc = time.ToString("ssMMHHddffyyyymm");

            var result = new byte[0x10]
            {
                0x58, 0xAB, 0xA9, 0xF0,
                0x6C, 0xA2, 0xD8, 0x3F,
                0x4D, 0x47, 0x49, 0xA3,
                0xB4, 0xB2, 0xE7, 0x3D
            };

            for (byte i = 0, k = 0x40; i < 0x10; ++i) k = result[i] ^= (byte)(k ^ (byte)strinc[i]);

            return result;
        }

        private static byte[] GetEncryption(in DateTime time)
        {
            var strinc = time.ToString("ssMMHHddffyyyymm");

            var crypto = new byte[0x10]
            {
                0xE2, 0x4F, 0x7B, 0x5F,
                0xCD, 0xE4, 0xC8, 0x6D,
                0xDB, 0xD8, 0xFB, 0xD7,
                0x40, 0x58, 0xC6, 0x78
            };

            var result = new byte[0x10]
            {
                0x58, 0xAB, 0xA9, 0xF0,
                0x6C, 0xA2, 0xD8, 0x3F,
                0x4D, 0x47, 0x49, 0xA3,
                0xB4, 0xB2, 0xE7, 0x3D
            };

            for (byte i = 0, k = 0x40; i < 0x10; ++i) k = result[i] ^= (byte)(k ^ (byte)strinc[i]);

            for (byte i = 0, k = 0x40; i < 0x10; ++i) k = result[i] ^= (byte)(k ^ crypto[i]);

            for (byte i = 0, k = 0x40; i < 0x10; ++i) k = result[i] ^= (byte)(k ^ (byte)strinc[i]);

            return result;
        }

        private static IElement GetProperties70(IReadOnlyList<IElementProperty> properties)
        {
            var children = new IElement[properties.Count];

            for (var i = 0; i < children.Length; ++i) children[i] = PropertyFactory.AsElement(properties[i]);

            return new Element("Properties70", children, null);
        }

        private static IElement GetDefaultExtensionProperties(in Options options)
        {
            var properties = new IElementProperty[13];

            properties[0] = new FBXProperty<string>("KString", "Url", "DocumentUrl", IElementPropertyFlags.None,
                "/foobar.fbx");
            properties[1] = new FBXProperty<string>("KString", "Url", "SrcDocumentUrl", IElementPropertyFlags.None,
                "/foobar.fbx");
            properties[2] = new FBXProperty<object>("Compound", string.Empty, "Original");
            properties[3] = new FBXProperty<string>("KString", string.Empty, "Original|ApplicationVendor",
                IElementPropertyFlags.None, options.AppVendor);
            properties[4] = new FBXProperty<string>("KString", string.Empty, "Original|ApplicationName",
                IElementPropertyFlags.None, options.AppName);
            properties[5] = new FBXProperty<string>("KString", string.Empty, "Original|ApplicationVersion",
                IElementPropertyFlags.None, options.AppVersion);
            properties[6] = new FBXProperty<DateTime>("DateTime", string.Empty, "Original|DateTime_GMT",
                IElementPropertyFlags.None, options.SaveTime);
            properties[7] = new FBXProperty<string>("KString", string.Empty, "Original|FileName",
                IElementPropertyFlags.None, "/foobar.fbx");
            properties[8] = new FBXProperty<object>("Compound", string.Empty, "LastSaved");
            properties[9] = new FBXProperty<string>("KString", string.Empty, "LastSaved|ApplicationVendor",
                IElementPropertyFlags.None, options.AppVendor);
            properties[10] = new FBXProperty<string>("KString", string.Empty, "LastSaved|ApplicationName",
                IElementPropertyFlags.None, options.AppName);
            properties[11] = new FBXProperty<string>("KString", string.Empty, "LastSaved|ApplicationVersion",
                IElementPropertyFlags.None, options.AppVersion);
            properties[12] = new FBXProperty<DateTime>("DateTime", string.Empty, "LastSaved|DateTime_GMT",
                IElementPropertyFlags.None, options.SaveTime);

            return GetProperties70(properties);
        }

        private static IElement GetDefaultDocumentProperties()
        {
            var properties = new IElementProperty[2];

            properties[0] = new FBXProperty<object>("object", string.Empty, "SourceObject");
            properties[1] = new FBXProperty<string>("KString", string.Empty, "ActiveAnimStackName",
                IElementPropertyFlags.None, string.Empty);

            return GetProperties70(properties);
        }

        private IElement GetFBXHeaderExtension(in Options options)
        {
            var children = new IElement[6];

            children[0] = Element.WithAttribute("FBXHeaderVersion", ElementaryFactory.GetElementAttribute(1003));
            children[1] = Element.WithAttribute("FBXVersion", ElementaryFactory.GetElementAttribute(7400));
            children[2] = Element.WithAttribute("EncryptionType", ElementaryFactory.GetElementAttribute(0));
            children[4] = Element.WithAttribute("Creator", ElementaryFactory.GetElementAttribute(options.Creator));

            children[3] = new Element("CreationTimeStamp", new IElement[]
            {
                Element.WithAttribute("Version", ElementaryFactory.GetElementAttribute(1000)),
                Element.WithAttribute("Year", ElementaryFactory.GetElementAttribute(options.SaveTime.Year)),
                Element.WithAttribute("Month", ElementaryFactory.GetElementAttribute(options.SaveTime.Month)),
                Element.WithAttribute("Day", ElementaryFactory.GetElementAttribute(options.SaveTime.Day)),
                Element.WithAttribute("Hour", ElementaryFactory.GetElementAttribute(options.SaveTime.Hour)),
                Element.WithAttribute("Minute", ElementaryFactory.GetElementAttribute(options.SaveTime.Minute)),
                Element.WithAttribute("Second", ElementaryFactory.GetElementAttribute(options.SaveTime.Second)),
                Element.WithAttribute("Millisecond",
                    ElementaryFactory.GetElementAttribute(options.SaveTime.Millisecond))
            }, null);

            children[5] = new Element
            (
                "SceneInfo",
                new[]
                {
                    Element.WithAttribute("Type", ElementaryFactory.GetElementAttribute("UserData")),
                    Element.WithAttribute("Version", ElementaryFactory.GetElementAttribute(100)),
                    new Element("MetaData", new IElement[]
                    {
                        Element.WithAttribute("Version", ElementaryFactory.GetElementAttribute(100)),
                        Element.WithAttribute("Title", ElementaryFactory.GetElementAttribute(string.Empty)),
                        Element.WithAttribute("Subject", ElementaryFactory.GetElementAttribute(string.Empty)),
                        Element.WithAttribute("Author", ElementaryFactory.GetElementAttribute(string.Empty)),
                        Element.WithAttribute("Keywords", ElementaryFactory.GetElementAttribute(string.Empty)),
                        Element.WithAttribute("Revision", ElementaryFactory.GetElementAttribute(string.Empty)),
                        Element.WithAttribute("Comment", ElementaryFactory.GetElementAttribute(string.Empty))
                    }, null),
                    GetDefaultExtensionProperties(options)
                },
                new[]
                {
                    ElementaryFactory.GetElementAttribute("GlobalInfo::SceneInfo"),
                    ElementaryFactory.GetElementAttribute("UserData")
                }
            );

            return new Element("FBXHeaderExtension", children, null);
        }

        private IElement GetFileId(in Options options)
        {
            return Element.WithAttribute("FileId", ElementaryFactory.GetElementAttribute(GetFileID(options.SaveTime)));
        }

        private IElement GetCreationTime(in Options options)
        {
            return Element.WithAttribute("CreationTime",
                ElementaryFactory.GetElementAttribute(options.SaveTime.ToString("yyyy-MM-dd HH:mm:ss:fff")));
        }

        private IElement GetCreator(in Options options)
        {
            return Element.WithAttribute("Creator", ElementaryFactory.GetElementAttribute(options.Creator));
        }

        private IElement GetGlobalSettings()
        {
            return new Element("GlobalSettings", new[]
            {
                Element.WithAttribute("Version", ElementaryFactory.GetElementAttribute(1000)),
                GetProperties70(m_scene.Settings.Properties)
            }, null);
        }

        private IElement GetDocuments()
        {
            return new Element("Documents", new IElement[]
            {
                Element.WithAttribute("Count", ElementaryFactory.GetElementAttribute(1)),
                new Element
                (
                    "Document",
                    new[]
                    {
                        GetDefaultDocumentProperties(),
                        Element.WithAttribute("RootNode", ElementaryFactory.GetElementAttribute(0L))
                    },
                    new[]
                    {
                        ElementaryFactory.GetElementAttribute(m_scene.GetHashCode()),
                        ElementaryFactory.GetElementAttribute("Scene"),
                        ElementaryFactory.GetElementAttribute("Scene")
                    }
                )
            }, null);
        }

        private IElement GetReferences()
        {
            return new Element("References", null, null);
        }

        private IElement GetDefinitions()
        {
            var length = 0;
            var mapper = new int[(int)FBXClassType.Count];

            for (var i = 0; i < m_scene.Objects.Count; ++i) ++mapper[(int)m_scene.Objects[i].Class];

            for (var i = 0; i < mapper.Length; ++i)
                if (mapper[i] != 0)
                    ++length;

            var templates = new IElement[length + 3];
            var tempindex = 3;

            templates[0] = Element.WithAttribute("Version", ElementaryFactory.GetElementAttribute(100));
            templates[1] =
                Element.WithAttribute("Count", ElementaryFactory.GetElementAttribute(m_scene.Objects.Count + 1));
            templates[2] = new Element("ObjectType", new IElement[]
            {
                Element.WithAttribute("Count", ElementaryFactory.GetElementAttribute(1))
            }, new[] { ElementaryFactory.GetElementAttribute(FBXClassType.GlobalSettings.ToString()) });

            for (var type = FBXClassType.Video; type < FBXClassType.Count; ++type)
            {
                var count = mapper[(int)type];

                if (count == 0) continue;

                var attributes = new[]
                {
                    ElementaryFactory.GetElementAttribute(type.ToString())
                };

                var template = m_scene.GetTemplateObject(type);

                if (template is null)
                {
                    var elements = new IElement[]
                    {
                        Element.WithAttribute("Count", ElementaryFactory.GetElementAttribute(count))
                    };

                    templates[tempindex++] = new Element("ObjectType", elements, attributes);
                }
                else
                {
                    var elements = new IElement[]
                    {
                        Element.WithAttribute("Count", ElementaryFactory.GetElementAttribute(count)),
                        new Element("PropertyTemplate", new[]
                        {
                            GetProperties70(template.Properties)
                        }, new[] { ElementaryFactory.GetElementAttribute(template.Name) })
                    };

                    templates[tempindex++] = new Element("ObjectType", elements, attributes);
                }
            }

            return new Element("Definitions", templates, null);
        }

        private IElement GetObjects()
        {
            var elements = new IElement[m_scene.Objects.Count];

            for (var i = 0; i < elements.Length; ++i) elements[i] = m_scene.Objects[i].AsElement(true);

            return new Element("Objects", elements, null);
        }

        private IElement GetConnections()
        {
            var connections = new List<Connection>(m_scene.Root.GetConnections());

            foreach (var @object in m_scene.Objects)
            {
                var addon = @object.GetConnections();

                if (addon.Length != @object.NumConnections)
                {
                    var fucker = 0;
                }

                connections.AddRange(addon);
            }

            var elements = new IElement[connections.Count];

            for (var i = 0; i < elements.Length; ++i)
            {
                var connection = connections[i];

                switch (connection.Type)
                {
                    case Connection.ConnectionType.Object:
                    {
                        elements[i] = new Element("C", null, new[]
                        {
                            ElementaryFactory.GetElementAttribute("OO"),
                            ElementaryFactory.GetElementAttribute(connection.Source),
                            ElementaryFactory.GetElementAttribute(connection.Destination)
                        });

                        break;
                    }

                    case Connection.ConnectionType.Property:
                    {
                        elements[i] = new Element("C", null, new[]
                        {
                            ElementaryFactory.GetElementAttribute("OP"),
                            ElementaryFactory.GetElementAttribute(connection.Source),
                            ElementaryFactory.GetElementAttribute(connection.Destination),
                            connection.Property
                        });

                        break;
                    }

                    default:
                    {
                        elements[i] = new Element("C", null, new[]
                        {
                            ElementaryFactory.GetElementAttribute(string.Empty),
                            ElementaryFactory.GetElementAttribute(connection.Source),
                            ElementaryFactory.GetElementAttribute(connection.Destination)
                        });

                        break;
                    }
                }
            }

            return new Element("Connections", elements, null);
        }

        private IElement GetTakes()
        {
            var elements = new IElement[m_scene.TakeInfos.Count + 1];

            elements[0] = Element.WithAttribute("Current", ElementaryFactory.GetElementAttribute(string.Empty));

            for (var i = 0; i < m_scene.TakeInfos.Count; ++i)
            {
                var takeInfo = m_scene.TakeInfos[i];

                elements[i + 1] = new Element("Take", new IElement[]
                {
                    new Element("FileName", null, new[]
                    {
                        ElementaryFactory.GetElementAttribute(takeInfo.Filename)
                    }),
                    new Element("LocalTime", null, new[]
                    {
                        ElementaryFactory.GetElementAttribute(MathExtensions.SecondsToFBXTime(takeInfo.LocalTimeFrom)),
                        ElementaryFactory.GetElementAttribute(MathExtensions.SecondsToFBXTime(takeInfo.LocalTimeTo))
                    }),
                    new Element("ReferenceTime", null, new[]
                    {
                        ElementaryFactory.GetElementAttribute(
                            MathExtensions.SecondsToFBXTime(takeInfo.ReferenceTimeFrom)),
                        ElementaryFactory.GetElementAttribute(MathExtensions.SecondsToFBXTime(takeInfo.ReferenceTimeTo))
                    })
                }, new[] { ElementaryFactory.GetElementAttribute(takeInfo.Name) });
            }

            return new Element("Takes", elements, null);
        }

        private static void WriteArrayAttribute<T>(BinaryWriter bw, T[] array, bool compress) where T : unmanaged
        {
            if (array.Length == 0)
            {
                bw.Write(0); // array length
                bw.Write(0); // decompressed
                bw.Write(0); // buffer length

                return;
            }

            if (compress)
                unsafe
                {
                    var buffer = new byte[sizeof(T) * array.Length];
                    var output = new byte[buffer.Length << 1];

                    fixed (T* ptr = &array[0])
                    {
                        Marshal.Copy(new IntPtr(ptr), buffer, 0, buffer.Length);
                    }

                    var deflater = new Deflater();

                    deflater.SetInput(buffer);

                    deflater.Finish();

                    var outlen = deflater.Deflate(output);

                    bw.Write(array.Length);
                    bw.Write(1);
                    bw.Write(outlen);
                    bw.BaseStream.Write(output, 0, outlen);
                }
            else
                unsafe
                {
                    var buffer = new byte[sizeof(T) * array.Length];

                    fixed (T* ptr = &array[0])
                    {
                        Marshal.Copy(new IntPtr(ptr), buffer, 0, buffer.Length);
                    }

                    bw.Write(array.Length);
                    bw.Write(0);
                    bw.Write(buffer.Length);
                    bw.Write(buffer);
                }
        }

        private static void WriteAttribute(BinaryWriter bw, IElementAttribute attribute, bool compress)
        {
            bw.Write((byte)attribute.Type);

            switch (attribute.Type)
            {
                case IElementAttributeType.Byte:
                    bw.Write((byte)attribute.GetElementValue());
                    break;
                case IElementAttributeType.Int16:
                    bw.Write((short)attribute.GetElementValue());
                    break;
                case IElementAttributeType.Int32:
                    bw.Write((int)attribute.GetElementValue());
                    break;
                case IElementAttributeType.Int64:
                    bw.Write((long)attribute.GetElementValue());
                    break;
                case IElementAttributeType.Single:
                    bw.Write((float)attribute.GetElementValue());
                    break;
                case IElementAttributeType.Double:
                    bw.Write((double)attribute.GetElementValue());
                    break;
                case IElementAttributeType.String:
                    bw.WriteStringPrefixInt(attribute.GetElementValue().ToString());
                    break;
                case IElementAttributeType.Binary:
                    bw.WriteBytesPrefixed(attribute.GetElementValue() as byte[]);
                    break;
                case IElementAttributeType.ArrayBoolean:
                    WriteArrayAttribute(bw, attribute.GetElementValue() as byte[], compress);
                    break;
                case IElementAttributeType.ArrayInt32:
                    WriteArrayAttribute(bw, attribute.GetElementValue() as int[], compress);
                    break;
                case IElementAttributeType.ArrayInt64:
                    WriteArrayAttribute(bw, attribute.GetElementValue() as long[], compress);
                    break;
                case IElementAttributeType.ArraySingle:
                    WriteArrayAttribute(bw, attribute.GetElementValue() as float[], compress);
                    break;
                case IElementAttributeType.ArrayDouble:
                    WriteArrayAttribute(bw, attribute.GetElementValue() as double[], compress);
                    break;
            }
        }

        private static void WriteElements(BinaryWriter bw, IElement[] elements, long start, bool compress)
        {
            for (var i = 0; i < elements.Length; ++i)
            {
                var element = elements[i];
                var current = bw.BaseStream.Position;

                bw.Write(0); // temporary offset
                bw.Write(0); // temporary count
                bw.Write(0); // temporary length

                bw.WriteStringPrefixByte(element.Name);

                var alength = bw.BaseStream.Position;

                for (var k = 0; k < element.Attributes.Length; ++k) WriteAttribute(bw, element.Attributes[k], compress);

                var blength = bw.BaseStream.Position;

                if (element.Children.Length != 0) WriteElements(bw, element.Children, start, compress);

                var tailpos = bw.BaseStream.Position;

                bw.BaseStream.Position = current;

                bw.Write((int)(tailpos - start));
                bw.Write(element.Attributes.Length);
                bw.Write((int)(blength - alength));

                bw.BaseStream.Position = tailpos;
            }

            bw.Write(0);
            bw.Write(0);
            bw.Write(0);
            bw.Write((byte)0);
        }

        public void Save(Stream stream, in Options options)
        {
            if (!stream.CanWrite) throw new Exception("Cannot write to stream provided");

            var main = new IElement[11];

            main[0] = GetFBXHeaderExtension(options);
            main[1] = GetFileId(options);
            main[2] = GetCreationTime(options);
            main[3] = GetCreator(options);
            main[4] = GetGlobalSettings();
            main[5] = GetDocuments();
            main[6] = GetReferences();
            main[7] = GetDefinitions();
            main[8] = GetObjects();
            main[9] = GetConnections();
            main[10] = GetTakes();

            using (var bw = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                var start = stream.Position;

                bw.WriteManaged(Header.CreateNew(7400));

                WriteElements(bw, main, start, options.CompressData);

                bw.Write(GetEncryption(options.SaveTime));

                bw.FillBuffer(0x10);

                bw.WriteManaged(Footer.CreateNew(7400));
            }
        }
    }
}
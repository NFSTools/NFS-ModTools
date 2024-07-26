using System;
using System.Collections.Generic;
using FBXSharp.Core;

namespace FBXSharp.Objective
{
    public class Mesh : Model
    {
        private readonly List<Material> m_materials;
        private Geometry m_geometry;

        public static readonly FBXObjectType FType = FBXObjectType.Mesh;

        public override FBXObjectType Type => FType;

        public override bool SupportsAttribute => false;

        public Geometry Geometry
        {
            get => m_geometry;
            set => InternalSetGeometry(value);
        }

        public IReadOnlyList<Material> Materials => m_materials;

        internal Mesh(IElement element, IScene scene) : base(element, scene)
        {
            m_materials = new List<Material>();
        }

        private void InternalSetGeometry(Geometry geometry)
        {
            if (geometry is null)
            {
                m_geometry = null;
                return;
            }

            if (geometry.Scene != Scene) throw new Exception("Geometry should share same scene with model");

            m_geometry = geometry;
        }

        public void AddMaterial(Material material)
        {
            if (material is null) return;

            if (material.Scene != Scene) throw new Exception("Material should share same scene with model");

            m_materials.Add(material);
        }

        public void RemoveMaterial(Material material)
        {
            if (material is null || material.Scene != Scene) return;

            _ = m_materials.Remove(material);
        }

        public void AddMaterialAt(Material material, int index)
        {
            if (material is null) return;

            if (material.Scene != Scene) throw new Exception("Material should share same scene with model");

            if (index < 0 || index > m_materials.Count)
                throw new ArgumentOutOfRangeException("Index should be in range 0 to material count inclusively");

            m_materials.Insert(index, material);
        }

        public void RemoveMaterialAt(int index)
        {
            if (index < 0 || index >= m_materials.Count)
                throw new ArgumentOutOfRangeException("Index should be in 0 to material count range");

            m_materials.RemoveAt(index);
        }

        public override Connection[] GetConnections()
        {
            var counter = 0;
            var indexer = 0;

            counter += m_geometry is null ? 0 : 1;
            counter += m_materials.Count;
            counter += Children.Count;

            if (counter == 0) return Array.Empty<Connection>();

            var connections = new Connection[counter];

            if (!(m_geometry is null))
                connections[indexer++] = new Connection(Connection.ConnectionType.Object, m_geometry.GetHashCode(),
                    GetHashCode());

            for (var i = 0; i < m_materials.Count; ++i)
                connections[indexer++] = new Connection(Connection.ConnectionType.Object, m_materials[i].GetHashCode(),
                    GetHashCode());

            for (var i = 0; i < Children.Count; ++i)
                connections[indexer++] = new Connection(Connection.ConnectionType.Object, Children[i].GetHashCode(),
                    GetHashCode());

            return connections;
        }

        public override void ResolveLink(FBXObject linker, IElementAttribute attribute)
        {
            if (linker.Class == FBXClassType.Model)
            {
                AddChild(linker as Model);

                return;
            }

            if (linker.Class == FBXClassType.Geometry)
                if (linker.Type == FBXObjectType.Mesh)
                {
                    InternalSetGeometry(linker as Geometry);

                    return;
                }

            if (linker.Class == FBXClassType.Material)
            {
                AddMaterial(linker as Material);
            }
        }

        public override IElement AsElement(bool binary)
        {
            return MakeElement("Model", binary);
        }
    }
}
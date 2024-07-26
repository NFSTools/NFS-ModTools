using System;
using System.Collections.Generic;
using FBXSharp.Core;

namespace FBXSharp.Objective
{
    public class Skin : FBXObject
    {
        private readonly List<Cluster> m_clusters;

        public static readonly FBXObjectType FType = FBXObjectType.Skin;

        public static readonly FBXClassType FClass = FBXClassType.Deformer;

        public override FBXObjectType Type => FType;

        public override FBXClassType Class => FClass;

        public double DeformAccuracy { get; set; }

        public IReadOnlyList<Cluster> Clusters => m_clusters;

        internal Skin(IElement element, IScene scene) : base(element, scene)
        {
            m_clusters = new List<Cluster>();

            if (element is null) return;

            var deformAccuracy = element.FindChild("Link_DeformAcuracy");

            if (deformAccuracy is null || deformAccuracy.Attributes.Length == 0 ||
                deformAccuracy.Attributes[0].Type != IElementAttributeType.Double) return;

            DeformAccuracy = Convert.ToDouble(deformAccuracy.Attributes[0].GetElementValue());
        }

        public void AddCluster(Cluster cluster)
        {
            AddClusterAt(cluster, m_clusters.Count);
        }

        public void RemoveCluster(Cluster cluster)
        {
            if (cluster is null || cluster.Scene != Scene) return;

            _ = m_clusters.Remove(cluster);
        }

        public void AddClusterAt(Cluster cluster, int index)
        {
            if (cluster is null) return;

            if (cluster.Scene != Scene) throw new Exception("Cluster should share same scene with skin");

            if (index < 0 || index > m_clusters.Count)
                throw new ArgumentOutOfRangeException("Index should be in range 0 to cluster count inclusively");

            m_clusters.Insert(index, cluster);
        }

        public void RemoveClusterAt(int index)
        {
            if (index < 0 || index >= m_clusters.Count)
                throw new ArgumentOutOfRangeException("Index should be in 0 to cluster count range");

            m_clusters.RemoveAt(index);
        }

        public override Connection[] GetConnections()
        {
            if (m_clusters.Count == 0) return Array.Empty<Connection>();

            var thisHashKey = GetHashCode();
            var connections = new Connection[m_clusters.Count];

            for (var i = 0; i < connections.Length; ++i)
                connections[i] = new Connection(Connection.ConnectionType.Object, m_clusters[i].GetHashCode(),
                    thisHashKey);

            return connections;
        }

        public override void ResolveLink(FBXObject linker, IElementAttribute attribute)
        {
            if (linker.Class == FBXClassType.Deformer && linker.Type == FBXObjectType.Cluster)
                AddCluster(linker as Cluster);
        }

        public override IElement AsElement(bool binary)
        {
            var hasAnyProperties = Properties.Count != 0;

            var elements = new IElement[2 + (hasAnyProperties ? 1 : 0)];

            elements[0] = Element.WithAttribute("Version", ElementaryFactory.GetElementAttribute(101));
            elements[1] = Element.WithAttribute("Link_DeformAcuracy",
                ElementaryFactory.GetElementAttribute(DeformAccuracy));

            if (hasAnyProperties) elements[2] = BuildProperties70();

            return new Element(Class.ToString(), elements, BuildAttributes("Deformer", Type.ToString(), binary));
        }
    }
}
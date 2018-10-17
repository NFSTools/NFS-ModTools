using System.Drawing;
using Microsoft.DirectX.Direct3D;

namespace EveAgain.SceneObjects
{
	public class ObjectTriangle : ObjectBase
	{
		protected short[] indices;
		protected CustomVertex.PositionColored[] vertices;

		public ObjectTriangle()
		{
			Name = "Triangle Object";
			FVF = CustomVertex.PositionColored.Format;
			PrimitiveType = PrimitiveType.TriangleList;
			VertexCount = 3;
			IndexCount = 3;
			PrimitiveCount = 1;

			indices = new short[3];
			indices[0] = 2;
			indices[1] = 1;
			indices[2] = 0;

			vertices = new CustomVertex.PositionColored[3];
			vertices[0] = CreateVertex(-1,0,0, Color.Red);
			vertices[1] = CreateVertex(0,1,0, Color.Green);
			vertices[2] = CreateVertex(1,0,0.5f, Color.Blue);
		}

		private CustomVertex.PositionColored CreateVertex(float x, float y, float z, Color color) 
		{
			CustomVertex.PositionColored v;
			v.X = x;
			v.Y = y;
			v.Z = z;
			v.Color = color.ToArgb();
			return v;
		}

		public override object Indices => indices;
	    public override object Vertices => vertices;

	    public override Texture Texture => null;

	    public override void Render() {}
	}
}

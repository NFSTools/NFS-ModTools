using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace EveAgain.SceneObjects
{
	public class ObjectCube : ObjectBase
	{
		protected short[] indices;
		protected CustomVertex.PositionColored[] vertices;

		public ObjectCube()
		{
			Name = "Cube Object";
			FVF = CustomVertex.PositionColored.Format;
			PrimitiveType = PrimitiveType.TriangleList;
			VertexCount = 8;
			IndexCount = 36;
			PrimitiveCount = IndexCount/3;

			indices = new short[IndexCount];
			indices[0] = 0;
			indices[1] = 1;
			indices[2] = 2;
			indices[3] = 1;
			indices[4] = 3;
			indices[5] = 2;
            indices[6] = 2;
            indices[7] = 3;
            indices[8] = 5;
            indices[9] = 4;
            indices[10] = 5;
            indices[11] = 3;
            indices[12] = 7;
            indices[13] = 5;
            indices[14] = 4;
            indices[15] = 7;
            indices[16] = 4;
            indices[17] = 6;
            indices[18] = 0;
            indices[19] = 6;
            indices[20] = 1;
            indices[21] = 6;
            indices[22] = 0;
            indices[23] = 7;
            indices[24] = 5;
            indices[25] = 0;
            indices[26] = 2;
            indices[27] = 5;
            indices[28] = 7;
            indices[29] = 0;
            indices[30] = 1;
            indices[31] = 6;
            indices[32] = 3;
            indices[33] = 6;
            indices[34] = 4;
            indices[35] = 3;

			vertices = new CustomVertex.PositionColored[VertexCount];
			vertices[0] = CreateVertex(0,0,0, Color.Red);
			vertices[1] = CreateVertex(1,0,0, Color.Green);
			vertices[2] = CreateVertex(0,1,0, Color.Blue);
			vertices[3] = CreateVertex(1,1,0, Color.LimeGreen);
			vertices[4] = CreateVertex(1,1,1, Color.Gold);
			vertices[5] = CreateVertex(0,1,1, Color.Brown);
			vertices[6] = CreateVertex(1,0,1, Color.MidnightBlue);
			vertices[7] = CreateVertex(0,0,1, Color.SeaGreen);

			Transform = Matrix.Translation(-0.5f, -0.5f, -0.5f);
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

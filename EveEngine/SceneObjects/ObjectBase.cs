using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace EveAgain.SceneObjects
{
	public abstract class ObjectBase: ISceneObject
	{
	    public ObjectBase()
		{
			Name = "Unnamed Object";
			Transform = Matrix.Identity;
			CullMode = Cull.None;
			FillMode = FillMode.Solid;
			Lighting = false;
			
			Selectable = true;
			Movable = true;
			VertexSelectable = true;
			VertexMovable = true;
		}

		public string Name { get; set; }

	    public bool Selected { get; set; }

	    public Matrix Transform { get; set; }

	    public Cull CullMode { get; set; }

	    public FillMode FillMode { get; set; }

	    public bool Lighting { get; set; }

	    public PrimitiveType PrimitiveType { get; protected set; }
	    public VertexFormats FVF { get; protected set; }
	    public int VertexCount { get; protected set; }
	    public int IndexCount { get; protected set; }
	    public int PrimitiveCount { get; protected set; }
	    public abstract object Indices { get; }
		public abstract object Vertices { get; }		
		public abstract Texture Texture { get; }

		public bool PublicViewable { get; }

	    public bool Selectable { get; }

	    public bool Movable { get; }

	    public bool VertexSelectable { get; }

	    public bool VertexMovable { get; }

	    public bool OverrideRender { get; }

	    public abstract void Render();

	}
}

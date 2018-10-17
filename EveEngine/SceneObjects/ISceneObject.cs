using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace EveAgain.SceneObjects
{
	public interface ISceneObject
	{
		string Name { get; set; }
		bool Selected { get; set; }
		Matrix Transform { get; set; }
		
		Cull CullMode { get; set; }
		FillMode FillMode { get; set; }
		
		bool Lighting { get; set; }
		PrimitiveType PrimitiveType { get; }
		VertexFormats FVF { get; }
		int VertexCount { get; }
		int IndexCount { get; }
		int PrimitiveCount { get; }
		object Indices { get; }
		object Vertices { get; }		

		bool PublicViewable { get; }
		bool Selectable { get; }
		bool Movable { get; }
		bool VertexSelectable { get; }
		bool VertexMovable { get; }

		Texture Texture { get; }
		
		bool OverrideRender { get; }
		void Render();

	}

	public interface IVertexSelectable 
	{
	}

	public interface IVertexMovable
	{
	}
}


using EveAgain.Render;
using Microsoft.DirectX;

namespace EveAgain.Cameras
{
	public class CameraOrtho : Camera
	{
		public CameraOrtho()
		{
			// some default values
			zoomOrtho = 4.0f;
			nearPlane = -100;
			farPlane = 100;
		}
		
		public override void RecreateProjection() 
		{
			projection = Matrix.OrthoLH(aspectRatio * zoomOrtho, zoomOrtho, nearPlane, farPlane);
		}

		public override void DrawGrid(Microsoft.DirectX.Direct3D.Device device) {}
		public override void ProjectCoordinates( ref int x, ref int y, int width, int height, Vector3 pickRayOrigin) {}
		public override void UnProjectCoordinates( int x, int y, int width, int height, ref Vector3 pickRayOrigin, ref Vector3 pickRayDirection) {}

		public override Vector3 ScreenToWorld(Vector2 vec, IRenderSurface surface) 
		{
			Vector3 v = new Vector3();
			v.X = ( ( (2.0f * vec.X) / surface.ClientWidth  )  ) / projection.M11;
			v.Y = -( ((2.0f * vec.Y) / surface.ClientHeight )  ) / projection.M22;
			v.Z = 0;
			return v;

		}


	}
}

using System;
using EveAgain.Render;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace EveAgain.Cameras
{
	public class CameraPerspective : Camera
	{
		public CameraPerspective()
		{
			// some default values
			FOV = (float)(Math.PI / 2.0f);
			World = Matrix.Scaling(1f,1f,1f);
			nearPlane = 10f;
			farPlane = 100000.0f;
		}

		public override void RecreateProjection() 
		{
			projection = Matrix.PerspectiveFovLH(fov, aspectRatio, nearPlane, farPlane);
		}

		public override void DrawGrid(Microsoft.DirectX.Direct3D.Device device) {}
		public override void ProjectCoordinates( ref int x, ref int y, int width, int height, Vector3 pickRayOrigin) {}
		public override void UnProjectCoordinates( int x, int y, int width, int height, ref Vector3 pickRayOrigin, ref Vector3 pickRayDirection) {}
		
		public override Vector3 ScreenToWorld(Vector2 vec, IRenderSurface surface) 
		{
			
			//return Vector3.Unproject(new Vector3(vec.X,vec.Y,0.0f), surface.Viewport, projection, view, world);
			Vector3 v = new Vector3();
			v.X = ( ( (2.0f * vec.X) / surface.ClientWidth  )  ) / projection.M11;
			v.Y = -( ((2.0f * vec.Y) / surface.ClientHeight )  ) / projection.M22;
			v.Z = 0;
			v.Scale(5000);
			return v;
		}
	}
}

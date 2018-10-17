using EveAgain.Render;
using Microsoft.DirectX;

namespace EveAgain.Cameras
{
	/// <summary>
	/// Summary description for SceneCamera.
	/// </summary>
	public abstract class Camera
	{

		protected Matrix world;
		protected Matrix view;
		protected Matrix projection;

		protected Vector3 eyePosition;
		protected Vector3 targetPoint;
		protected Vector3 upVector;

		protected float aspectRatio;
		protected float nearPlane;
		protected float farPlane;
		protected float fov;
		protected float zoomOrtho;

		protected bool rotatable;
		protected bool pannable;
		protected bool zoomable;
		protected string name;

		public abstract void RecreateProjection();

		public abstract void DrawGrid(Microsoft.DirectX.Direct3D.Device device);
		public abstract void ProjectCoordinates( ref int x, ref int y, int width, int height, Vector3 pickRayOrigin);
		public abstract void UnProjectCoordinates( int x, int y, int width, int height, ref Vector3 pickRayOrigin, ref Vector3 pickRayDirection);

		public abstract Vector3 ScreenToWorld(Vector2 vec, IRenderSurface surface);

		public Camera()
		{
			world = Matrix.Identity;

			aspectRatio = 1.0f;

			targetPoint = new Vector3(0,0,0);
			upVector = new Vector3(0,1,0);
			eyePosition = new Vector3(0,0,-1);
			
			name = "Unnamed Camera";
			pannable = true;
			zoomable = true;
			rotatable = true;
		}

		// the two camera matrices
		public Matrix World 
		{
			get { return world; }
			set { world = value; }
		}
		public Matrix View { 
			get { return view; } 
			set { view = value; }
		}
		public Matrix Projection 
		{ 
			get { return projection; }
			set { projection = value; }
		}
		// change stuff
		public void SetViewParameters(Vector3 eyePosition, Vector3 targetPoint, Vector3 upVector)
		{
			this.eyePosition = eyePosition;
			this.targetPoint = targetPoint;
			this.upVector = upVector;
			RecreateView();
		}
		public void RecreateView() 
		{
			view = Matrix.LookAtLH(eyePosition, targetPoint, upVector);
		}
		public void SetProjectionParameters(float aspectRatio, float nearPlane, float farPlane) 
		{
			this.aspectRatio = aspectRatio;
			this.nearPlane = nearPlane;
			this.farPlane = farPlane;
			RecreateProjection();
		}
		// view parameters
		public Vector3 EyePosition { 
			/*get { return eyePosition; }*/
			set 
			{ 
				eyePosition = value; 
				RecreateView(); 
			}
		}
		public Vector3 TargetPoint { 
			/*get { return targetPoint; }*/
			set 
			{
				targetPoint = value;
				RecreateView();
			}
		}
		public Vector3 UpVector { 
			/*get { return upVector; } */
			set 
			{
				upVector = value;
				RecreateView();
			}
		}
		// projection parameters
		public float FOV 
		{
			get { return fov; }
			set 
			{
				fov = value;
				RecreateProjection();
			}
		}
		public float AspectRatio 
		{
			get { return aspectRatio; }
			set 
			{
				aspectRatio = value;
				RecreateProjection();
			}
		}
		public float NearPlane 
		{
			get { return nearPlane; }
			set 
			{
				nearPlane = value;
				RecreateProjection();
			}
		}
		public float FarPlane 
		{
			get { return farPlane; }
			set 
			{
				farPlane = value;
				RecreateProjection();
			}
		}	
		public float ZoomOrtho 
		{
			get { return zoomOrtho; }
			set 
			{
				zoomOrtho = value;
				RecreateProjection();
			}
		}
		// camera features
		public bool Rotatable 
		{ 
			get { return rotatable; }
			set { rotatable = value; }
		}
		public bool Pannable 
		{ 
			get { return pannable; }
			set { pannable = value; }
		}
		public bool Zoomable 
		{ 
			get { return zoomable; } 
			set { zoomable = value; }
		}
		public string Name 
		{ 
			get { return name; }
			set { name = value; }
		}
	}
}

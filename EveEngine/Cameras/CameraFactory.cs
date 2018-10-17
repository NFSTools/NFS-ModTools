using System;
using Microsoft.DirectX;

namespace EveAgain.Cameras
{
    public enum CameraType
    {
        CameraFront = 0,
        CameraBack = 1,
        CameraTop = 2,
        CameraBottom = 3,
        CameraLeft = 4,
        CameraRight = 5,
        CameraUser = 6,
        CameraPerpective = 7,
        CameraUV = 8
    }

    public class CameraFactory
    {

        public static Camera CreateCamera(CameraType type)
        {
            Camera camera;
            const float eyeMove = 1.0f;

            if (type == CameraType.CameraPerpective)
            {
                camera = new CameraPerspective
                {
                    Rotatable = true,
                    Pannable = true,
                    Zoomable = true
                };
            }
            else
            {
                camera = new CameraOrtho
                {
                    Rotatable = type == CameraType.CameraUser,
                    Pannable = true,
                    Zoomable = true
                };
            }

            switch (type)
            {
                case CameraType.CameraFront:
                    camera.Name = "Front";
                    camera.EyePosition = new Vector3(0, 0, -eyeMove);
                    camera.UpVector = new Vector3(0, 1, 0);
                    //camera.World = Matrix.Scaling(1,1,0);
                    break;
                case CameraType.CameraBack:
                    camera.Name = "Back";
                    camera.EyePosition = new Vector3(0, 0, eyeMove);
                    camera.UpVector = new Vector3(0, 1, 0);
                    //camera.World = Matrix.Scaling(1,1,0);
                    break;
                case CameraType.CameraTop:
                    camera.Name = "Top";
                    camera.EyePosition = new Vector3(0, eyeMove, 0);
                    camera.UpVector = new Vector3(0, 0, 1);
                    //camera.World = Matrix.Scaling(1,0,1);
                    break;
                case CameraType.CameraBottom:
                    camera.Name = "Bottom";
                    camera.EyePosition = new Vector3(0, -eyeMove, 0);
                    camera.UpVector = new Vector3(0, 0, 1);
                    //camera.World = Matrix.Scaling(1,0,1);
                    break;
                case CameraType.CameraLeft:
                    camera.Name = "Left";
                    camera.EyePosition = new Vector3(-eyeMove, 0, 0);
                    camera.UpVector = new Vector3(0, 1, 0);
                    //camera.World = Matrix.Scaling(0,1,1);
                    break;
                case CameraType.CameraRight:
                    camera.Name = "Right";
                    camera.EyePosition = new Vector3(eyeMove, 0, 0);
                    camera.UpVector = new Vector3(0, 1, 0);
                    //camera.World = Matrix.Scaling(0,1,1);
                    break;
                case CameraType.CameraUser:
                    camera.Name = "User";
                    camera.EyePosition = new Vector3(0, 0, -eyeMove);
                    camera.UpVector = new Vector3(0, 1, 0);
                    break;
                case CameraType.CameraPerpective:
                    camera.Name = "Perspective";
                    camera.World = Matrix.Scaling(1.0f, 1.0f, -1.0f);
                    camera.EyePosition = new Vector3(0.1f, 500.0f, 0.0f);
                    camera.TargetPoint = new Vector3(0.0f, 500.0f, 0.0f);
                    //camera.EyePosition = new Vector3(0,0,-eyeMove);
                    camera.UpVector = new Vector3(0, 1, 0);
                    break;
                case CameraType.CameraUV:
                    // map on (x,y)
                    camera.Name = "UV";
                    camera.EyePosition = new Vector3(0, 0, -eyeMove);
                    camera.UpVector = new Vector3(0, 1, 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return camera;
        }
    }
}

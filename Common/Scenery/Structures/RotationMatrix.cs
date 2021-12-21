using System.Numerics;
using System.Runtime.InteropServices;

namespace Common.Scenery.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RotationMatrix
{
    public Vector3 Rotation0;
    public Vector3 Rotation1;
    public Vector3 Rotation2;

    public RotationMatrix(Vector3 rotation0, Vector3 rotation1, Vector3 rotation2)
    {
        Rotation0 = rotation0;
        Rotation1 = rotation1;
        Rotation2 = rotation2;
    }

    public static implicit operator Matrix4x4(RotationMatrix rm)
    {
        return new Matrix4x4(
            rm.Rotation0.X, rm.Rotation0.Y, rm.Rotation0.Z, 0,
            rm.Rotation1.X, rm.Rotation1.Y, rm.Rotation1.Z, 0,
            rm.Rotation2.X, rm.Rotation2.Y, rm.Rotation2.Z, 0,
            0, 0, 0, 1);
    }
}
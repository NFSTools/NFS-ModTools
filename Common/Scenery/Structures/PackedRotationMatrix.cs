using System.Numerics;
using System.Runtime.InteropServices;

namespace Common.Scenery.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PackedRotationMatrix
{
    public short Value11;
    public short Value12;
    public short Value13;
    public short Value21;
    public short Value22;
    public short Value23;
    public short Value31;
    public short Value32;
    public short Value33;

    public static implicit operator Matrix4x4(PackedRotationMatrix prm)
    {
        var vec0 = new Vector3(prm.Value11, prm.Value12, prm.Value13) / 8192f;
        var vec1 = new Vector3(prm.Value21, prm.Value22, prm.Value23) / 8192f;
        var vec2 = new Vector3(prm.Value31, prm.Value32, prm.Value33) / 8192f;
        
        return new Matrix4x4
        {
            M11 = vec0.X,
            M12 = vec0.Y,
            M13 = vec0.Z,
            
            M21 = vec1.X,
            M22 = vec1.Y,
            M23 = vec1.Z,
            
            M31 = vec2.X,
            M32 = vec2.Y,
            M33 = vec2.Z,
            
            M44 = 1
        };
    }
}
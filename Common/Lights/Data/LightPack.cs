using System.Collections.Generic;
using System.Numerics;

namespace Common.Lights.Data;

public class LightPack : BasicResource
{
    public uint ScenerySectionNumber { get; set; }
    public List<Light> Lights { get; set; }
}

public class Light
{
    public uint NameHash { get; set; }
    public string Name { get; set; }
    public uint Color { get; set; }
    public Vector3 Position { get; set; }
    public float Size { get; set; }
    public float Intensity { get; set; }
    public float FarStart { get; set; }
    public float FarEnd { get; set; }
    public float Falloff { get; set; }
}
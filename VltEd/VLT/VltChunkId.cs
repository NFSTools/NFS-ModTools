namespace VltEd.VLT
{
    public enum VltChunkId : int
    {
        Dependency = 0x4465704E,
        StringsRaw = 0x53747245,
        Strings = 0x5374724E,
        Data = 0x4461744E,
        Expression = 0x4578704E,
        Pointers = 0x5074724E,
        Null = 0,
    }
}

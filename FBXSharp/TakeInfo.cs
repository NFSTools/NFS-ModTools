namespace FBXSharp
{
    public class TakeInfo
    {
        public string Name { get; }
        public string Filename { get; }
        public double LocalTimeFrom { get; }
        public double LocalTimeTo { get; }
        public double ReferenceTimeFrom { get; }
        public double ReferenceTimeTo { get; }

        public TakeInfo(string name, string filename, double localFrom, double localTo, double refFrom, double refTo)
        {
            Name = name ?? string.Empty;
            Filename = filename ?? string.Empty;
            LocalTimeFrom = localFrom;
            LocalTimeTo = localTo;
            ReferenceTimeFrom = refFrom;
            ReferenceTimeTo = refTo;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
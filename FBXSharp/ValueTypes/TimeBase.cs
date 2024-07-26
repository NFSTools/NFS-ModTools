namespace FBXSharp.ValueTypes
{
    public struct TimeBase
    {
        public const int SizeOf = 0x08;

        public double Time;

        public TimeBase(double time)
        {
            Time = time;
        }

        public TimeBase(long time)
        {
            Time = MathExtensions.FBXTimeToSeconds(time);
        }

        public long ToLong()
        {
            return MathExtensions.SecondsToFBXTime(Time);
        }
    }
}
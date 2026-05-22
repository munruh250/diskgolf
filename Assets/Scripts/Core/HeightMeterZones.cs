using DiskGolf.Flight;

namespace DiskGolf.Core
{
    public static class HeightMeterZones
    {
        public static ThrowHeight FromValue(float v)
        {
            if (v < 0.33f) return ThrowHeight.Low;
            if (v > 0.66f) return ThrowHeight.High;
            return ThrowHeight.Nice;
        }
    }
}

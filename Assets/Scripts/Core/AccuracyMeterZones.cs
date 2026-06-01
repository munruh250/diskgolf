using UnityEngine;

namespace DiskGolf.Core
{
    public enum AccuracyZone
    {
        RedLow,
        YellowLow,
        Green,
        YellowHigh,
        RedHigh,
    }

    /// <summary>Five-zone accuracy timing meter: red | yellow | green | yellow | red.</summary>
    public static class AccuracyMeterZones
    {
        public const float GreenMin = 0.4f;

        public const float GreenMax = 0.6f;

        public const float MeterCenter = 0.5f;

        public const float MeterWidth = 0.2f;

        const float YawErrorDeg = 11f;

        const float PowerErrorScale = 0.14f;

        public static AccuracyZone FromValue(float normalized01)
        {
            normalized01 = Mathf.Clamp01(normalized01);

            if (normalized01 < 0.2f)
                return AccuracyZone.RedLow;

            if (normalized01 < GreenMin)
                return AccuracyZone.YellowLow;

            if (normalized01 <= GreenMax)
                return AccuracyZone.Green;

            if (normalized01 < 0.8f)
                return AccuracyZone.YellowHigh;

            return AccuracyZone.RedHigh;
        }

        /// <summary>
        /// Green = on target. Yellow = one axis off (N/S distance or E/W aim).
        /// Red = both distance and lateral aim off.
        /// </summary>
        public static void ApplyToThrow(ref Vector3 aimDirection, ref float power, AccuracyZone zone)
        {
            aimDirection.y = 0f;

            if (aimDirection.sqrMagnitude < 1e-6f)
                aimDirection = Vector3.forward;

            aimDirection.Normalize();

            switch (zone)
            {
                case AccuracyZone.Green:
                    return;
                case AccuracyZone.YellowLow:
                    power *= 1f - PowerErrorScale;
                    return;
                case AccuracyZone.YellowHigh:
                    aimDirection = Quaternion.Euler(0f, YawErrorDeg, 0f) * aimDirection;
                    return;
                case AccuracyZone.RedLow:
                    power *= 1f - PowerErrorScale;
                    aimDirection = Quaternion.Euler(0f, -YawErrorDeg, 0f) * aimDirection;
                    return;
                case AccuracyZone.RedHigh:
                    power *= 1f + PowerErrorScale;
                    aimDirection = Quaternion.Euler(0f, YawErrorDeg, 0f) * aimDirection;
                    return;
            }
        }
    }
}

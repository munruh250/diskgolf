using UnityEngine;

namespace DiskGolf.Core
{
    /// <summary>Maps character attributes (60–100) to gameplay modifiers.</summary>
    public static class PlayerCharacterStats
    {
        public const int MinStat = 60;

        public const int MaxStat = 100;

        public const int BaselineStat = 80;

        const float MinPowerMultiplier = 0.8f;

        const float MaxPowerMultiplier = 1.2f;

        const float MinAccuracyMeterWidth = 0.12f;

        const float BaselineAccuracyMeterWidth = 0.2f;

        const float MaxAccuracyMeterWidth = 0.28f;

        const float MaxClutchErrorMultiplier = 1.5f;

        const float MinClutchErrorMultiplier = 0.5f;

        public static float Stat01(int stat) =>
            Mathf.InverseLerp(MinStat, MaxStat, Mathf.Clamp(stat, MinStat, MaxStat));

        /// <summary>Drive distance modifier: 80 = 1.0, 60 = 0.8, 100 = 1.2 (±20% cap).</summary>
        public static float PowerDistanceMultiplier(int power) =>
            EvaluateAnchored(power, MinPowerMultiplier, 1f, MaxPowerMultiplier);

        /// <summary>Accuracy meter sweet-spot width on the vertical timing bar.</summary>
        public static float AccuracyMeterWidth(int accuracy) =>
            EvaluateAnchored(accuracy, MinAccuracyMeterWidth, BaselineAccuracyMeterWidth, MaxAccuracyMeterWidth);

        /// <summary>Scales off-sweet-spot penalties; lower = more forgiving.</summary>
        public static float ClutchErrorMultiplier(int clutch) =>
            EvaluateAnchored(clutch, MaxClutchErrorMultiplier, 1f, MinClutchErrorMultiplier);

        static float EvaluateAnchored(int stat, float atMin, float atBaseline, float atMax)
        {
            stat = Mathf.Clamp(stat, MinStat, MaxStat);

            if (stat <= BaselineStat)
            {
                float t = (stat - MinStat) / (float)(BaselineStat - MinStat);
                return Mathf.Lerp(atMin, atBaseline, t);
            }

            float tHigh = (stat - BaselineStat) / (float)(MaxStat - BaselineStat);
            return Mathf.Lerp(atBaseline, atMax, tHigh);
        }
    }
}

using System.Collections.Generic;
using DiskGolf.Disc;
using UnityEngine;

namespace DiskGolf.Flight
{
    public static class FlightSimulator
    {
        const int WaypointCount = 32;
        const float FtToUnity = 0.3048f; // 1 ft in meters (Unity units = meters)

        /// <summary>Scales all disc max-distance ratings (tune in one place).</summary>
        public const float DistanceScale = 1f;

        /// <summary>Distance multiplier when power is below the disc's speed requirement.</summary>
        public const float UnderpowerDistanceMultiplier = 0.82f;

        public static FlightPath Compute(ThrowInput input)
        {
            float power = Mathf.Clamp(input.Power, 0f, 1.1f);
            float heightPower = NormalizePower(input.Power);
            float glideBonus = input.Height switch
            {
                ThrowHeight.Low => 0.88f,
                ThrowHeight.Nice => 1.0f,
                ThrowHeight.High => 1.08f,
                _ => 1f
            };

            float requiredPower = input.Disc.speed / 14f;
            float turnBoost = 0f;
            float distancePenalty = 1f;
            if (input.Power < requiredPower)
            {
                turnBoost = 1.5f;
                distancePenalty = UnderpowerDistanceMultiplier;
            }

            float distanceFt = input.Disc.maxDistanceFt * DistanceScale * power * glideBonus * distancePenalty;

            float turnMod = input.ReleaseAngle switch
            {
                ReleaseAngle.Hyzer => 0.5f,
                ReleaseAngle.Anhyzer => 1.5f,
                _ => 1f
            };
            float fadeMod = input.ReleaseAngle switch
            {
                ReleaseAngle.Hyzer => 1.3f,
                ReleaseAngle.Anhyzer => 0.7f,
                _ => 1f
            };
            float heightFadeMod = input.Height == ThrowHeight.High ? 0.85f : 1f;

            float turnAmount = input.Disc.turn * turnMod - turnBoost;
            float fadeAmount = input.Disc.fade * fadeMod * heightFadeMod;

            Vector3 forward = input.AimDirection.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 wind = input.Wind.DriftVector * (input.Disc.speed / 14f) * FtToUnity;

            var waypoints = new List<FlightWaypoint>(WaypointCount);
            float totalTime = 2.5f;
            float maxLateral = 0f;

            for (int i = 0; i < WaypointCount; i++)
            {
                float t = i / (float)(WaypointCount - 1);
                float dist = distanceFt * t * FtToUnity;
                float turnPhase = TurnPhase(t) * turnAmount * FtToUnity;
                float fadePhase = FadePhase(t) * fadeAmount * FtToUnity;
                float lateral = turnPhase + fadePhase;
                maxLateral = Mathf.Max(maxLateral, Mathf.Abs(lateral));

                float windEnvelope = WindEnvelope(t, input.Height);
                Vector3 windOffset = wind * windEnvelope;

                Vector3 pos = input.Origin
                    + forward * dist
                    + right * lateral
                    + windOffset;
                pos.y = ArcHeightFeet(t, input.Height, heightPower) * FtToUnity;

                waypoints.Add(new FlightWaypoint(pos, t * totalTime));
            }

            var shape = ClassifyShape(turnAmount, fadeAmount, maxLateral);
            return new FlightPath(waypoints, distanceFt, LieType.Fairway, shape);
        }

        /// <summary>0 at release, 1 at a full 1.1 power-meter reading.</summary>
        public static float NormalizePower(float rawPower) =>
            Mathf.Clamp01(Mathf.Clamp(rawPower, 0f, 1.1f) / 1.1f);

        /// <summary>Peak apex height in feet for the given height line and power.</summary>
        public static float PeakHeightFeet(ThrowHeight height, float normalizedPower)
        {
            float p = Mathf.Clamp01(normalizedPower);

            return height switch
            {
                ThrowHeight.Low => Mathf.Lerp(10f, 20f, p),
                ThrowHeight.Nice => Mathf.Lerp(15f, 40f, p),
                ThrowHeight.High => Mathf.Lerp(40f, 85f, p),
                _ => Mathf.Lerp(15f, 40f, p),
            };
        }

        static float TurnPhase(float t) => t <= 0.4f ? Mathf.Sin(t / 0.4f * Mathf.PI * 0.5f) : 0f;
        static float FadePhase(float t) => t >= 0.6f ? Mathf.Sin((t - 0.6f) / 0.4f * Mathf.PI * 0.5f) : 0f;

        /// <summary>
        /// Monotonic 0–1 exposure along the flight (contrast: Sin(πt)·t is zero at t=1, so the
        /// landing sample would never show downwind drift). Matches the plan’s High = 1.3× factor.
        /// </summary>
        static float WindEnvelope(float t, ThrowHeight height)
        {
            float baseExposure = (1f - Mathf.Cos(t * Mathf.PI)) * 0.5f;
            return height == ThrowHeight.High ? baseExposure * 1.3f : baseExposure;
        }

        /// <summary>Full-bar span for in-circle putt power meter (ft).</summary>
        public const float PuttMeterSpanFt = 30f;

        public static float GlideBonus(ThrowHeight height) =>
            height switch
            {
                ThrowHeight.Low => 0.88f,
                ThrowHeight.Nice => 1f,
                ThrowHeight.High => 1.08f,
                _ => 1f,
            };

        public static float MaxReachFeet(DiscProfile disc, ThrowHeight height) =>
            disc.maxDistanceFt * DistanceScale * GlideBonus(height);

        /// <summary>Meter reading (0–1.1) that should carry the disc the target distance.</summary>
        public static float MeterPowerForTargetDistance(DiscProfile disc, float targetDistanceFt, ThrowHeight height)
        {
            float maxReach = MaxReachFeet(disc, height);
            if (maxReach < 1f)
                return 0f;

            return Mathf.Clamp(targetDistanceFt / maxReach * 1.1f, 0f, 1.1f);
        }

        /// <summary>Slider-normalized center (0–1) for the height timing meter.</summary>
        public static float HeightMeterCenter(ThrowHeight height) =>
            height switch
            {
                ThrowHeight.Low => 0.16f,
                ThrowHeight.Nice => 0.5f,
                ThrowHeight.High => 0.84f,
                _ => 0.5f,
            };

        /// <summary>Convert a putt meter reading into travel distance using the putting scale.</summary>
        public static float PuttDistanceFromMeter(float meterValue) =>
            Mathf.Clamp(meterValue, 0f, 1.1f) / 1.1f * PuttMeterSpanFt;

        /// <summary>Simulator power for a putt of the given distance.</summary>
        public static float PuttPowerForDistance(float distanceFt, DiscProfile putter) =>
            distanceFt / Mathf.Max(putter.maxDistanceFt, 1e-4f);

        static float ArcHeightFeet(float t, ThrowHeight height, float normalizedPower) =>
            PeakHeightFeet(height, normalizedPower) * Mathf.Sin(t * Mathf.PI);

        static FlightShape ClassifyShape(float turn, float fade, float maxLateral)
        {
            if (maxLateral < 0.5f) return FlightShape.Straight;
            if (turn < -0.5f && fade > 0.5f) return FlightShape.SCurve;
            if (turn < -0.5f) return FlightShape.Turn;
            return FlightShape.Fade;
        }
    }
}

using System.Collections.Generic;
using DiskGolf.Core;
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

        /// <summary>Base hang time before distance adds flight seconds (disc glide, not ballistics).</summary>
        public const float FlightBaseSeconds = 1.15f;

        /// <summary>Extra seconds in the air per foot of carry — tune for floaty disc feel.</summary>
        public const float FlightSecondsPerFoot = 0.028f;

        public const float MinFlightSeconds = 2f;

        public const float MaxFlightSeconds = 12f;

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

            float distanceFt = input.Disc.maxDistanceFt * DistanceScale * CharacterPowerMultiplier()
                * power * glideBonus * distancePenalty;

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
            float totalTime = ComputeFlightDurationSeconds(distanceFt, input.Height);
            float maxLateral = 0f;

            for (int i = 0; i < WaypointCount; i++)
            {
                float u = i / (float)(WaypointCount - 1);
                float timeU = GlideTimeCurve(u);
                float dist = distanceFt * u * FtToUnity;
                float lateral = LateralOffsetFeet(u, turnAmount, fadeAmount) * FtToUnity;
                maxLateral = Mathf.Max(maxLateral, Mathf.Abs(lateral));

                float windEnvelope = WindEnvelope(u, input.Height);
                Vector3 windOffset = wind * windEnvelope;

                Vector3 pos = input.Origin
                    + forward * dist
                    + right * lateral
                    + windOffset;
                pos.y = ArcHeightFeet(u, input.Height, heightPower) * FtToUnity;

                waypoints.Add(new FlightWaypoint(pos, timeU * totalTime));
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

        static float LateralOffsetFeet(float u, float turnAmount, float fadeAmount)
        {
            float turnCurve = Mathf.Sin(Mathf.Clamp01(u / 0.55f) * Mathf.PI * 0.5f);
            float turnWeight = 1f - SmoothStep(0.32f, 0.68f, u);
            float fadeCurve = Mathf.Sin(Mathf.Clamp01((u - 0.35f) / 0.65f) * Mathf.PI * 0.5f);
            float fadeWeight = SmoothStep(0.38f, 0.72f, u);

            return turnAmount * turnCurve * turnWeight + fadeAmount * fadeCurve * fadeWeight;
        }

        static float SmoothStep(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(edge1 - edge0, 1e-5f));
            return t * t * (3f - 2f * t);
        }

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
            disc.maxDistanceFt * DistanceScale * CharacterPowerMultiplier() * GlideBonus(height);

        static float CharacterPowerMultiplier()
        {
            var character = GameSessionSettings.ActiveCharacter;
            return character != null
                ? PlayerCharacterStats.PowerDistanceMultiplier(character.power)
                : 1f;
        }

        /// <summary>Meter reading (0–1.1) that should carry the disc the target distance.</summary>
        public static float MeterPowerForTargetDistance(DiscProfile disc, float targetDistanceFt, ThrowHeight height)
        {
            float maxReach = MaxReachFeet(disc, height);
            if (maxReach < 1f)
                return 0f;

            return Mathf.Clamp(targetDistanceFt / maxReach * 1.1f, 0f, 1.1f);
        }

        /// <summary>Slider-normalized center (0–1) for the accuracy timing meter sweet spot.</summary>
        public static float HeightMeterCenter(ThrowHeight height) => AccuracyMeterZones.MeterCenter;

        public static float AccuracyMeterCenter => AccuracyMeterZones.MeterCenter;

        /// <summary>Convert a putt meter reading into travel distance using the putting scale.</summary>
        public static float PuttDistanceFromMeter(float meterValue) =>
            Mathf.Clamp(meterValue, 0f, 1.1f) / 1.1f * PuttMeterSpanFt;

        /// <summary>Simulator power for a putt of the given distance.</summary>
        public static float PuttPowerForDistance(float distanceFt, DiscProfile putter) =>
            distanceFt / Mathf.Max(putter.maxDistanceFt, 1e-4f);

        /// <summary>Estimated in-air time for a given carry distance.</summary>
        public static float ComputeFlightDurationSeconds(float distanceFt, ThrowHeight height)
        {
            float heightFactor = height switch
            {
                ThrowHeight.Low => 0.9f,
                ThrowHeight.Nice => 1f,
                ThrowHeight.High => 1.12f,
                _ => 1f,
            };

            float duration = (FlightBaseSeconds + distanceFt * FlightSecondsPerFoot) * heightFactor;
            return Mathf.Clamp(duration, MinFlightSeconds, MaxFlightSeconds);
        }

        /// <summary>Spend more clock time around apex so the disc feels like it is gliding.</summary>
        static float GlideTimeCurve(float u)
        {
            if (u <= 0.5f)
                return 0.44f * (u / 0.5f);

            return 0.44f + 0.56f * ((u - 0.5f) / 0.5f);
        }

        static float ArcHeightFeet(float u, ThrowHeight height, float normalizedPower) =>
            PeakHeightFeet(height, normalizedPower) * Mathf.Sin(u * Mathf.PI);

        static FlightShape ClassifyShape(float turn, float fade, float maxLateral)
        {
            if (maxLateral < 0.5f) return FlightShape.Straight;
            if (turn < -0.5f && fade > 0.5f) return FlightShape.SCurve;
            if (turn < -0.5f) return FlightShape.Turn;
            return FlightShape.Fade;
        }
    }
}

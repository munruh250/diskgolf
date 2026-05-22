using System.Collections.Generic;
using UnityEngine;

namespace DiskGolf.Flight
{
    public static class FlightSimulator
    {
        const int WaypointCount = 32;
        const float FtToUnity = 0.3048f; // 1 ft in meters (Unity units = meters)

        public static FlightPath Compute(ThrowInput input)
        {
            float power = Mathf.Clamp(input.Power, 0f, 1.1f);
            float glideBonus = input.Height switch
            {
                ThrowHeight.Low => 0.85f,
                ThrowHeight.Nice => 1.0f,
                ThrowHeight.High => 1.1f,
                _ => 1f
            };

            float requiredPower = input.Disc.speed / 14f;
            float turnBoost = 0f;
            float distancePenalty = 1f;
            if (power < requiredPower)
            {
                turnBoost = 1.5f;
                distancePenalty = 0.7f;
            }

            float distanceFt = input.Disc.maxDistanceFt * power * glideBonus * distancePenalty;

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
                pos.y = ArcHeight(t, input.Height) * FtToUnity;

                waypoints.Add(new FlightWaypoint(pos, t * totalTime));
            }

            var shape = ClassifyShape(turnAmount, fadeAmount, maxLateral);
            return new FlightPath(waypoints, distanceFt, LieType.Fairway, shape);
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

        static float ArcHeight(float t, ThrowHeight height)
        {
            float amp = height switch
            {
                ThrowHeight.Low => 2f,
                ThrowHeight.Nice => 5f,
                ThrowHeight.High => 10f,
                _ => 5f
            };
            return amp * Mathf.Sin(t * Mathf.PI);
        }

        static FlightShape ClassifyShape(float turn, float fade, float maxLateral)
        {
            if (maxLateral < 0.5f) return FlightShape.Straight;
            if (turn < -0.5f && fade > 0.5f) return FlightShape.SCurve;
            if (turn < -0.5f) return FlightShape.Turn;
            return FlightShape.Fade;
        }
    }
}

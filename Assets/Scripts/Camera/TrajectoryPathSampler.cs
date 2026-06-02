using System.Collections.Generic;
using DiskGolf.Flight;
using UnityEngine;

namespace DiskGolf.Camera
{
    /// <summary>Arc-length sampling along a flight preview path.</summary>
    public static class TrajectoryPathSampler
    {
        public static Vector3 SamplePosition(IReadOnlyList<FlightWaypoint> waypoints, float t)
        {
            if (waypoints == null || waypoints.Count == 0)
                return Vector3.zero;

            if (waypoints.Count == 1 || t <= 0f)
                return waypoints[0].Position;

            if (t >= 1f)
                return waypoints[waypoints.Count - 1].Position;

            float target = t * TotalLength(waypoints);
            float walked = 0f;

            for (int i = 1; i < waypoints.Count; i++)
            {
                var a = waypoints[i - 1].Position;
                var b = waypoints[i].Position;
                float seg = Vector3.Distance(a, b);

                if (walked + seg >= target)
                {
                    float segT = seg > 1e-6f ? (target - walked) / seg : 0f;
                    return Vector3.Lerp(a, b, segT);
                }

                walked += seg;
            }

            return waypoints[waypoints.Count - 1].Position;
        }

        public static Vector3 SampleTangent(IReadOnlyList<FlightWaypoint> waypoints, float t)
        {
            if (waypoints == null || waypoints.Count < 2)
                return Vector3.forward;

            const float delta = 0.02f;
            float t0 = Mathf.Clamp01(t - delta);
            float t1 = Mathf.Clamp01(t + delta);
            var a = SamplePosition(waypoints, t0);
            var b = SamplePosition(waypoints, t1);
            var tangent = b - a;

            if (tangent.sqrMagnitude < 1e-8f)
            {
                tangent = waypoints[waypoints.Count - 1].Position - waypoints[0].Position;
                tangent.y = 0f;
            }

            return tangent.sqrMagnitude > 1e-8f ? tangent.normalized : Vector3.forward;
        }

        static float TotalLength(IReadOnlyList<FlightWaypoint> waypoints)
        {
            float total = 0f;

            for (int i = 1; i < waypoints.Count; i++)
                total += Vector3.Distance(waypoints[i - 1].Position, waypoints[i].Position);

            return Mathf.Max(total, 1e-4f);
        }
    }
}

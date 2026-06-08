using System.Collections.Generic;
using DiskGolf.Flight;
using UnityEngine;

namespace DiskGolf.Camera
{
    public readonly struct TrajectoryZoomPose
    {
        public Vector3 Position { get; }

        public Vector3 LookAt { get; }

        public TrajectoryZoomPose(Vector3 position, Vector3 lookAt)
        {
            Position = position;
            LookAt = lookAt;
        }

        public static TrajectoryZoomPose Evaluate(
            IReadOnlyList<FlightWaypoint> waypoints,
            Transform thrower,
            Vector3 landing,
            float pathT,
            FlightCameraSettings settings)
        {
            pathT = Mathf.Clamp01(pathT);

            Vector3 forward = landing - thrower.position;
            forward.y = 0f;

            if (forward.sqrMagnitude < 1e-6f)
                forward = thrower.forward;

            forward.Normalize();

            Vector3 pathPoint = waypoints != null && waypoints.Count > 0
                ? TrajectoryPathSampler.SamplePosition(waypoints, pathT)
                : Vector3.Lerp(thrower.position, landing, pathT);

            Vector3 tangent = waypoints != null && waypoints.Count > 1
                ? TrajectoryPathSampler.SampleTangent(waypoints, pathT)
                : forward;

            tangent.y = 0f;

            if (tangent.sqrMagnitude < 1e-6f)
                tangent = forward;

            tangent.Normalize();

            float travelHeight = Mathf.Lerp(2.2f, 5.5f, pathT);
            float travelBack = Mathf.Lerp(4.5f, 7f, pathT);
            Vector3 travelPos = pathPoint - tangent * travelBack + Vector3.up * travelHeight;
            Vector3 travelLook = pathPoint + tangent * 2f;

            var finalPose = CameraRig.ComputeTargetZoomPose(thrower, landing, settings);
            float finalBlend = Smooth01(Mathf.InverseLerp(0.72f, 1f, pathT));
            Vector3 pos = Vector3.Lerp(travelPos, finalPose.Position, finalBlend);
            Vector3 look = Vector3.Lerp(travelLook, finalPose.LookAt, finalBlend);
            return new TrajectoryZoomPose(pos, look);
        }

        static float Smooth01(float t) => t * t * (3f - 2f * t);
    }
}

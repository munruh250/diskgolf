using System.Collections.Generic;
using DiskGolf.Flight;
using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Clips simulated flight paths to elevated terrain and release height.</summary>
    public static class FlightPathGrounding
    {
        const float FtToUnity = 0.3048f;

        const int SegmentSteps = 10;

        public static FlightPath Apply(FlightPath path, Vector3 releaseOrigin)
        {
            if (path?.Waypoints == null || path.Waypoints.Count < 2)
                return path;

            float fallbackGroundY = releaseOrigin.y - DiscLieGround.DiscRestLift;
            float clearance = DiscLieGround.DiscRestLift;

            var clipped = new List<FlightWaypoint>(path.Waypoints.Count);
            clipped.Add(new FlightWaypoint(releaseOrigin, path.Waypoints[0].Time));

            for (int i = 0; i < path.Waypoints.Count - 1; i++)
            {
                var from = clipped[clipped.Count - 1];
                var to = path.Waypoints[i + 1];

                if (TryFindGroundHit(from, to, fallbackGroundY, clearance, out var hit))
                {
                    clipped.Add(hit);
                    break;
                }

                clipped.Add(to);
            }

            float carryFt = HorizontalCarryFeet(clipped);
            return new FlightPath(clipped, carryFt, path.LandingLie, path.Shape);
        }

        static bool TryFindGroundHit(
            FlightWaypoint from,
            FlightWaypoint to,
            float fallbackGroundY,
            float clearance,
            out FlightWaypoint hit)
        {
            hit = default;
            var prevPos = from.Position;
            float prevTime = from.Time;

            for (int step = 1; step <= SegmentSteps; step++)
            {
                float t = step / (float)SegmentSteps;
                var pos = Vector3.Lerp(from.Position, to.Position, t);
                float time = Mathf.Lerp(from.Time, to.Time, t);
                float groundY = ResolveGroundY(pos, fallbackGroundY);

                if (pos.y > groundY + clearance)
                {
                    prevPos = pos;
                    prevTime = time;
                    continue;
                }

                var landing = RefineGroundHit(prevPos, pos, prevTime, time, fallbackGroundY, clearance);
                hit = landing;
                return true;
            }

            return false;
        }

        static FlightWaypoint RefineGroundHit(
            Vector3 from,
            Vector3 to,
            float timeFrom,
            float timeTo,
            float fallbackGroundY,
            float clearance)
        {
            for (int i = 0; i < 6; i++)
            {
                float midT = 0.5f;
                var mid = Vector3.Lerp(from, to, midT);
                float midTime = Mathf.Lerp(timeFrom, timeTo, midT);
                float groundY = ResolveGroundY(mid, fallbackGroundY);

                if (mid.y > groundY + clearance)
                {
                    from = mid;
                    timeFrom = midTime;
                }
                else
                {
                    to = mid;
                    timeTo = midTime;
                }
            }

            float ground = ResolveGroundY(to, fallbackGroundY);
            var landing = new Vector3(to.x, ground + clearance, to.z);
            return new FlightWaypoint(landing, timeTo);
        }

        static float HorizontalCarryFeet(IReadOnlyList<FlightWaypoint> waypoints)
        {
            if (waypoints == null || waypoints.Count == 0)
                return 0f;

            var start = waypoints[0].Position;
            var end = waypoints[waypoints.Count - 1].Position;
            var delta = end - start;
            delta.y = 0f;
            return delta.magnitude / FtToUnity;
        }

        static float ResolveGroundY(Vector3 pos, float fallbackGroundY)
        {
            if (DiscLieGround.IsInWaterFootprint(pos.x, pos.z))
                return DiscLieGround.SampleWaterSurfaceY(pos, fallbackGroundY);

            return DiscLieGround.SampleTerrainY(pos, fallbackGroundY);
        }
    }
}

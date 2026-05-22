using System.Collections.Generic;

namespace DiskGolf.Flight
{
    public sealed class FlightPath
    {
        public IReadOnlyList<FlightWaypoint> Waypoints => _waypoints;
        public float TotalDistanceFt { get; }
        public LieType LandingLie { get; }
        public FlightShape Shape { get; }

        readonly List<FlightWaypoint> _waypoints;

        public FlightPath(
            List<FlightWaypoint> waypoints,
            float totalDistanceFt,
            LieType landingLie,
            FlightShape shape)
        {
            _waypoints = waypoints;
            TotalDistanceFt = totalDistanceFt;
            LandingLie = landingLie;
            Shape = shape;
        }
    }
}

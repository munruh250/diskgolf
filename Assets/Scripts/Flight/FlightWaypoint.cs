using UnityEngine;

namespace DiskGolf.Flight
{
    public readonly struct FlightWaypoint
    {
        public Vector3 Position { get; }
        public float Time { get; }

        public FlightWaypoint(Vector3 position, float time)
        {
            Position = position;
            Time = time;
        }
    }
}

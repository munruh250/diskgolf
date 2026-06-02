using System.Collections.Generic;
using DiskGolf.Flight;
using UnityEngine;

namespace DiskGolf.Camera
{
    /// <summary>Shared state for the trajectory zoom travel animation.</summary>
    public sealed class TrajectoryZoomDriver : MonoBehaviour
    {
        public static TrajectoryZoomDriver Instance { get; private set; }

        [SerializeField] Transform thrower;

        public bool IsDriving { get; set; }

        public float PathT { get; set; }

        public IReadOnlyList<FlightWaypoint> Waypoints { get; set; }

        public Vector3 Landing { get; set; }

        public Transform Thrower => thrower;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void BindThrower(Transform t) => thrower = t;
    }
}

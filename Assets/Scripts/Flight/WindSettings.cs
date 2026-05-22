using UnityEngine;

namespace DiskGolf.Flight
{
    [System.Serializable]
    public struct WindSettings
    {
        public Vector2 direction; // normalized XZ
        public float speedMph;      // 0-15

        public Vector3 DriftVector => new Vector3(direction.x, 0f, direction.y) * speedMph;
    }
}

using DiskGolf.Disc;

namespace DiskGolf.Flight
{
    public readonly struct ThrowInput
    {
        public DiscProfile Disc { get; }
        public ReleaseAngle ReleaseAngle { get; }
        public float Power { get; }          // 0.0 - 1.1
        public ThrowHeight Height { get; }
        public WindSettings Wind { get; }
        public UnityEngine.Vector3 Origin { get; }
        public UnityEngine.Vector3 AimDirection { get; } // normalized XZ toward basket

        public ThrowInput(
            DiscProfile disc,
            ReleaseAngle releaseAngle,
            float power,
            ThrowHeight height,
            WindSettings wind,
            UnityEngine.Vector3 origin,
            UnityEngine.Vector3 aimDirection)
        {
            Disc = disc;
            ReleaseAngle = releaseAngle;
            Power = power;
            Height = height;
            Wind = wind;
            Origin = origin;
            AimDirection = aimDirection.normalized;
        }
    }
}

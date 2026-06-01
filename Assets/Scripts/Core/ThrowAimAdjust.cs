using DiskGolf.Disc;
using DiskGolf.Flight;
using UnityEngine;

namespace DiskGolf.Core
{
    /// <summary>Pre-throw aim: arrow keys shift trajectory target before the timing meters.</summary>
    public sealed class ThrowAimAdjust : MonoBehaviour
    {
        const float YawStepDeg = 2.5f;

        const float DistanceStepFt = 15f;

        const float MinTargetDistanceFt = 10f;

        [SerializeField] float maxYawDegrees = 35f;

        float _yawOffsetDeg;

        float _distanceOffsetFt;

        ThrowHeight _plannedHeight = ThrowHeight.Nice;

        public float YawOffsetDegrees => _yawOffsetDeg;

        public float TargetDistanceFt { get; private set; }

        public ThrowHeight PlannedHeight => _plannedHeight;

        public void ResetForLie(HoleSetup hole, Vector3 lie, DiscProfile disc)
        {
            _yawOffsetDeg = 0f;
            _distanceOffsetFt = 0f;
            _plannedHeight = ThrowHeight.Nice;
            RecalculateTarget(hole, lie, disc);
        }

        public void SetPlannedHeight(ThrowHeight height) => _plannedHeight = height;

        public void RecalculateTarget(HoleSetup hole, Vector3 lie, DiscProfile disc)
        {
            if (hole == null || disc == null)
            {
                TargetDistanceFt = MinTargetDistanceFt;
                return;
            }

            float alongBasket = DistanceAlongAim(hole, lie, AimDirection(hole, lie));
            float maxReach = disc.maxDistanceFt * FlightSimulator.DistanceScale;
            float baseline = alongBasket;

            TargetDistanceFt = Mathf.Clamp(baseline + _distanceOffsetFt, MinTargetDistanceFt, maxReach);
        }

        public Vector3 AimDirection(HoleSetup hole, Vector3 lie)
        {
            var baseAim = hole != null ? hole.AimDirectionFrom(lie) : Vector3.forward;
            return Quaternion.Euler(0f, _yawOffsetDeg, 0f) * baseAim;
        }

        public void ApplyHeldInput(HoleSetup hole, Vector3 lie, DiscProfile disc,
            bool left, bool right, bool up, bool down)
        {
            if (left)
                _yawOffsetDeg -= YawStepDeg;

            if (right)
                _yawOffsetDeg += YawStepDeg;

            _yawOffsetDeg = Mathf.Clamp(_yawOffsetDeg, -maxYawDegrees, maxYawDegrees);

            if (up)
                _distanceOffsetFt += DistanceStepFt;

            if (down)
                _distanceOffsetFt -= DistanceStepFt;

            RecalculateTarget(hole, lie, disc);
        }

        static float DistanceAlongAim(HoleSetup hole, Vector3 lie, Vector3 aim)
        {
            var toBasket = hole.BasketPosition - lie;
            toBasket.y = 0f;
            aim.y = 0f;

            if (toBasket.sqrMagnitude < 1e-6f || aim.sqrMagnitude < 1e-6f)
                return hole.DistanceToBasket(lie);

            return Mathf.Max(0f, Vector3.Dot(toBasket.normalized, aim.normalized) * toBasket.magnitude / 0.3048f);
        }
    }
}

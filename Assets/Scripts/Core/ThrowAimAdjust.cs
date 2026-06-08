using DiskGolf.Disc;
using DiskGolf.Flight;
using UnityEngine;

namespace DiskGolf.Core
{
    /// <summary>Pre-throw aim: arrow keys shift trajectory target before the timing meters.</summary>
    public sealed class ThrowAimAdjust : MonoBehaviour
    {
        const float MinTargetDistanceFt = 10f;

        const float YawHoldRateDegPerSec = 22f;

        const float DistanceHoldRateFtPerSec = 12f;

        [SerializeField] float maxYawDegrees = 90f;

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
            float maxReach = disc.maxDistanceFt * FlightSimulator.DistanceScale * PowerDistanceMultiplier();
            float baseline = alongBasket;

            TargetDistanceFt = Mathf.Clamp(baseline + _distanceOffsetFt, MinTargetDistanceFt, maxReach);
        }

        public Vector3 AimDirection(HoleSetup hole, Vector3 lie)
        {
            var baseAim = hole != null ? hole.AimDirectionFrom(lie) : Vector3.forward;
            return Quaternion.Euler(0f, _yawOffsetDeg, 0f) * baseAim;
        }

        public void ApplyHeldInput(
            HoleSetup hole,
            Vector3 lie,
            DiscProfile disc,
            bool left,
            bool right,
            bool up,
            bool down,
            float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            float yawDelta = 0f;
            if (left)
                yawDelta -= YawHoldRateDegPerSec * deltaTime;
            if (right)
                yawDelta += YawHoldRateDegPerSec * deltaTime;

            if (Mathf.Abs(yawDelta) > 0f)
            {
                _yawOffsetDeg = Mathf.Clamp(_yawOffsetDeg + yawDelta, -maxYawDegrees, maxYawDegrees);
            }

            float distanceDelta = 0f;
            if (up)
                distanceDelta += DistanceHoldRateFtPerSec * deltaTime;
            if (down)
                distanceDelta -= DistanceHoldRateFtPerSec * deltaTime;

            if (Mathf.Abs(distanceDelta) > 0f)
                _distanceOffsetFt += distanceDelta;

            if (Mathf.Abs(yawDelta) > 0f || Mathf.Abs(distanceDelta) > 0f)
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

        static float PowerDistanceMultiplier()
        {
            var character = GameSessionSettings.ActiveCharacter;
            return character != null
                ? PlayerCharacterStats.PowerDistanceMultiplier(character.power)
                : 1f;
        }
    }
}

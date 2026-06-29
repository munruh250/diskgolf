using DiskGolf.Disc;
using DiskGolf.Flight;
using UnityEngine;

namespace DiskGolf.Core
{
    /// <summary>Pre-throw aim: arrow keys shift trajectory target before the timing meters.</summary>
    public sealed class ThrowAimAdjust : MonoBehaviour
    {
        const float MinTargetDistanceFt = 10f;

        const float DistanceHoldRateFtPerSec = 12f;

        [SerializeField] float maxYawDegrees = 90f;

        float _yawOffsetDeg;

        float _downholeOffsetFt;

        ThrowHeight _plannedHeight = ThrowHeight.Nice;

        public float YawOffsetDegrees => _yawOffsetDeg;

        public float DownholeOffsetFt => _downholeOffsetFt;

        public float TargetDistanceFt { get; private set; }

        public ThrowHeight PlannedHeight => _plannedHeight;

        public void ResetForLie(HoleSetup hole, Vector3 lie, DiscProfile disc)
        {
            _yawOffsetDeg = 0f;
            _downholeOffsetFt = 0f;
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
            float maxReach = MaxReachFeet(disc);
            float cappedBaseline = Mathf.Min(alongBasket, maxReach);

            TargetDistanceFt = Mathf.Clamp(cappedBaseline, MinTargetDistanceFt, maxReach);
        }

        public Vector3 AimDirection(HoleSetup hole, Vector3 lie)
        {
            var baseAim = hole != null ? hole.AimDirectionFrom(lie) : Vector3.forward;
            return Quaternion.Euler(0f, _yawOffsetDeg, 0f) * baseAim;
        }

        public void BuildAdjustedThrowVectors(
            HoleSetup hole,
            Vector3 lie,
            DiscProfile disc,
            out Vector3 aim,
            out float distanceFt)
        {
            RecalculateTarget(hole, lie, disc);

            var baseAim = AimDirection(hole, lie);
            var downhole = hole != null ? hole.AimDirection : Vector3.forward;
            float maxReach = MaxReachFeet(disc);
            Vector3 landingPoint = lie
                + baseAim * (TargetDistanceFt * 0.3048f)
                + downhole * (_downholeOffsetFt * 0.3048f);

            var toTarget = landingPoint - lie;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 1e-6f)
            {
                aim = baseAim;
                distanceFt = TargetDistanceFt;
                return;
            }

            distanceFt = Mathf.Clamp(toTarget.magnitude / 0.3048f, MinTargetDistanceFt, maxReach);
            aim = toTarget.normalized;
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

            float yawRateDegPerSec = YawHoldRateForTargetDistance(TargetDistanceFt);
            float yawDelta = 0f;
            if (left)
                yawDelta -= yawRateDegPerSec * deltaTime;
            if (right)
                yawDelta += yawRateDegPerSec * deltaTime;

            if (Mathf.Abs(yawDelta) > 0f)
            {
                _yawOffsetDeg = Mathf.Clamp(_yawOffsetDeg + yawDelta, -maxYawDegrees, maxYawDegrees);
            }

            float downholeDelta = 0f;
            if (up)
                downholeDelta += DistanceHoldRateFtPerSec * deltaTime;
            if (down)
                downholeDelta -= DistanceHoldRateFtPerSec * deltaTime;

            if (Mathf.Abs(downholeDelta) > 0f)
                _downholeOffsetFt += downholeDelta;

            if (Mathf.Abs(yawDelta) > 0f || Mathf.Abs(downholeDelta) > 0f)
                RecalculateTarget(hole, lie, disc);
        }

        /// <summary>Match left/right arc motion to the up/down hold rate at the current target distance.</summary>
        static float YawHoldRateForTargetDistance(float targetDistanceFt)
        {
            float radiusFt = Mathf.Max(targetDistanceFt, MinTargetDistanceFt);
            return DistanceHoldRateFtPerSec / (radiusFt * Mathf.Deg2Rad);
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

        static float MaxReachFeet(DiscProfile disc) =>
            disc.maxDistanceFt * FlightSimulator.DistanceScale * PowerDistanceMultiplier();

        static float PowerDistanceMultiplier()
        {
            var character = GameSessionSettings.ActiveCharacter;
            return character != null
                ? PlayerCharacterStats.PowerDistanceMultiplier(character.power)
                : 1f;
        }
    }
}

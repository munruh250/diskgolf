using DiskGolf.Flight;
using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.Core
{
    public class HoleSetup : MonoBehaviour
    {
        [SerializeField] Transform teePad;
        [SerializeField] Transform basket;
        [SerializeField] Transform thrower;
        [SerializeField] float circleRadiusFt = 33f;
        [SerializeField] float holeLengthFt = 250f;

        public Vector3 TeePosition => teePad != null ? teePad.position : Vector3.zero;

        /// <summary>World position for disc in hand during aim (thrower) or on tee pad fallback.</summary>
        public Vector3 DiscHoldPosition
        {
            get
            {
                if (thrower != null)
                {
                    var visual = thrower.GetComponent<ThrowerVisual>();
                    if (visual != null && visual.HandAnchor != null)
                        return visual.HandAnchor.position;
                }

                if (thrower != null)
                    return thrower.position + thrower.forward * 0.35f + Vector3.up * 1.05f;

                return TeePosition + Vector3.up * (GreyboxScale.DiscThicknessM * 0.5f + 0.1f);
            }
        }

        /// <summary>Disc orientation while held — flat backhand, visible from side camera.</summary>
        public Quaternion DiscHoldRotation
        {
            get
            {
                if (thrower == null)
                    return Quaternion.Euler(90f, 0f, 0f);

                var visual = thrower.GetComponent<ThrowerVisual>();
                if (visual != null && visual.HandAnchor != null)
                    return visual.HandAnchor.rotation * Quaternion.Euler(90f, 0f, 0f);

                return thrower.rotation * Quaternion.Euler(90f, 0f, 0f);
            }
        }

        public bool IsNearTee(Vector3 worldPosition, float radiusMeters = 3f) =>
            Vector3.Distance(worldPosition, TeePosition) <= radiusMeters;

        public Vector3 BasketPosition => basket != null ? basket.position : Vector3.forward * 76.2f;

        public Transform Thrower => thrower;

        public void BindThrower(Transform t) => thrower = t;

        const float ThrowerBehindLieM = 0.55f;

        /// <summary>Place thrower at tee facing the basket (first throw / reset).</summary>
        public void PositionThrowerAtTee()
        {
            if (thrower == null || teePad == null || basket == null)
                return;

            var aim = AimDirection;
            var rot = Quaternion.LookRotation(aim, Vector3.up);
            thrower.SetPositionAndRotation(TeePosition + rot * Vector3.back * ThrowerBehindLieM, rot);
            ApplyThrowerSpriteLayout();
            RefreshCameraAimPoint();
        }

        /// <summary>Place thrower behind the disc lie for the next throw.</summary>
        public void PositionThrowerAtLie(Vector3 discLie)
        {
            if (thrower == null)
                return;

            var aim = AimDirectionFrom(discLie);
            var rot = Quaternion.LookRotation(aim, Vector3.up);
            thrower.SetPositionAndRotation(discLie + rot * Vector3.back * ThrowerBehindLieM, rot);
            ApplyThrowerSpriteLayout();
            RefreshCameraAimPoint();
        }

        public void RefreshCameraAimPoint()
        {
            if (thrower == null || basket == null)
                return;

            DiskGolf.Camera.NtmCameraRig.EnsureAimPoint(thrower, basket);
        }

        void ApplyThrowerSpriteLayout()
        {
            if (thrower == null)
                return;

            var visual = thrower.GetComponent<ThrowerVisual>();
            visual?.ApplySpriteLayout();
        }

        public float CircleRadiusFt => circleRadiusFt;

        /// <summary>Hole length tuning (inspector); visuals use tee/basket positions.</summary>
        public float HoleLengthFt => holeLengthFt;

        /// <summary>World-space aim axis from tee toward basket (legacy / hole framing).</summary>
        public Vector3 AimDirection
        {
            get
            {
                var delta = BasketPosition - TeePosition;
                return delta.sqrMagnitude < 1e-8f ? Vector3.forward : delta.normalized;
            }
        }

        /// <summary>Aim axis from any lie toward the basket (used for drives, previews, putts).</summary>
        public Vector3 AimDirectionFrom(Vector3 lieWorld)
        {
            var delta = BasketPosition - lieWorld;
            return delta.sqrMagnitude < 1e-8f ? Vector3.forward : delta.normalized;
        }

        public float DistanceToBasket(Vector3 from) =>
            Vector3.Distance(from, BasketPosition) / 0.3048f;

        public WindSettings RollWind()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            return new WindSettings
            {
                direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)),
                speedMph = Random.Range(0f, 15f)
            };
        }
    }
}

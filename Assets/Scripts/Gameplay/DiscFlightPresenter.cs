using System;
using System.Collections;
using DiskGolf.Flight;
using UnityEngine;
using UnityEngine.Serialization;

namespace DiskGolf.Gameplay
{
    public class DiscFlightPresenter : MonoBehaviour
    {
        [SerializeField] Transform discTransform;

        [SerializeField] float flightSpeed = 1f;

        [Tooltip("Nose-down pitch while gliding. Keep near 0 for a flat saucer look.")]
        [FormerlySerializedAs("flightTiltDegrees")]
        [SerializeField] float flightPitchDegrees = 0f;

        Action<FlightPath> _onComplete;

        FlightPath _activePath;

        Vector3 _restScale;

        float _restYaw;

        public Transform DiscTransform => discTransform;

        public bool IsFlying { get; private set; }

        public bool LastFlightHoled { get; private set; }

        public float FlightProgress { get; private set; }

        public Vector3 LandedPosition =>
            discTransform != null ? discTransform.position : Vector3.zero;

        void Awake()
        {
            if (discTransform == null)
                return;

            _restScale = discTransform.localScale;
            _restYaw = discTransform.rotation.eulerAngles.y;
        }

        public void SetPosition(Vector3 pos) => SetPositionAndRotation(pos, FlatRotation(_restYaw));

        public void SetPositionAndRotation(Vector3 pos, Quaternion rot)
        {
            if (discTransform == null)
                return;

            discTransform.gameObject.SetActive(true);
            discTransform.position = pos;
            ApplyDiscRotation(rot);
            EnsureRestScale();
            discTransform.localScale = _restScale;

            _restYaw = discTransform.rotation.eulerAngles.y;
        }

        void EnsureRestScale()
        {
            if (_restScale.sqrMagnitude > 1e-6f)
                return;

            if (discTransform != null && discTransform.localScale.sqrMagnitude > 1e-6f)
                _restScale = discTransform.localScale;
        }

        public void Play(FlightPath path, Action<FlightPath> onComplete)
        {
            _activePath = path;
            _onComplete = onComplete;
            LastFlightHoled = false;

            StopAllCoroutines();
            StartCoroutine(FlyRoutine());
        }

        IEnumerator FlyRoutine()
        {
            var wps = _activePath?.Waypoints;

            if (_activePath == null || wps == null || wps.Count == 0)
            {
                IsFlying = false;
                FlightProgress = 0f;
                _onComplete?.Invoke(_activePath);

                yield break;
            }

            IsFlying = true;
            FlightProgress = 0f;

            if (discTransform != null)
                discTransform.gameObject.SetActive(true);

            float elapsed = 0f;
            float duration = wps[wps.Count - 1].Time / Mathf.Max(flightSpeed, 1e-4f);
            float discRadius = GreyboxScale.DiscDiameterM * 0.45f;
            var previousPos = discTransform.position;

            while (elapsed < duration && discTransform != null)
            {
                elapsed += Time.deltaTime;
                float simTime = Mathf.Clamp(elapsed * flightSpeed, 0f, wps[wps.Count - 1].Time);
                FlightProgress = duration > 0f ? elapsed / duration : 1f;

                var nextPos = SamplePathAtTime(wps, simTime);

                if (TryHitGround(previousPos, nextPos, wps, out var groundHit))
                {
                    discTransform.position = groundHit;
                    ApplyDiscRotation(FlatRotation(YawFromPosition(previousPos, groundHit)));
                    IsFlying = false;
                    _onComplete?.Invoke(_activePath);
                    yield break;
                }

                if (TreeObstacle.TryHitSegment(previousPos, nextPos, discRadius, out var treeHit))
                {
                    float originGroundY = wps.Count > 0 ? wps[0].Position.y : treeHit.GroundFallbackY;
                    yield return TreeDeflectAndFallRoutine(treeHit, previousPos, originGroundY);
                    IsFlying = false;
                    Debug.Log("[Disk Golf] Disc hit a tree.");
                    _onComplete?.Invoke(_activePath);
                    yield break;
                }

                if (BasketCatchDetector.TryHitSegment(previousPos, nextPos, discRadius, out var basketHit))
                {
                    discTransform.position = basketHit;
                    ApplyDiscRotation(FlatRotation(YawFromPosition(previousPos, basketHit)));
                    IsFlying = false;
                    LastFlightHoled = true;
                    Debug.Log("[Disk Golf] Disc hit the basket.");
                    _onComplete?.Invoke(_activePath);
                    yield break;
                }

                discTransform.position = nextPos;
                previousPos = nextPos;
                OrientDiscInFlight(wps, simTime, FlightProgress);

                yield return null;
            }

            if (discTransform != null)
            {
                discTransform.position = wps[wps.Count - 1].Position;
                ApplyDiscRotation(FlatRotation(YawFromWaypoints(wps)));

                if (BasketCatchDetector.ContainsPoint(discTransform.position, discRadius))
                    LastFlightHoled = true;
            }

            IsFlying = false;
            FlightProgress = 1f;
            _onComplete?.Invoke(_activePath);
        }

        IEnumerator TreeDeflectAndFallRoutine(TreeHitInfo treeHit, Vector3 approachFrom, float originGroundY)
        {
            const float bounceDuration = 0.55f;

            var start = treeHit.HitPosition;
            var end = DiscLieGround.SnapLie(treeHit.DeflectedPosition, originGroundY);
            float elapsed = 0f;

            while (elapsed < bounceDuration && discTransform != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / bounceDuration);
                float horizontalT = Mathf.SmoothStep(0f, 1f, t);
                float verticalT = t * t;

                discTransform.position = new Vector3(
                    Mathf.Lerp(start.x, end.x, horizontalT),
                    Mathf.Lerp(start.y, end.y, verticalT),
                    Mathf.Lerp(start.z, end.z, horizontalT));
                ApplyDiscRotation(FlatRotation(YawFromPosition(approachFrom, discTransform.position)));
                yield return null;
            }

            if (discTransform != null)
            {
                discTransform.position = end;
                ApplyDiscRotation(FlatRotation(YawFromPosition(approachFrom, end)));
            }
        }

        static bool TryHitGround(
            Vector3 from,
            Vector3 to,
            System.Collections.Generic.IReadOnlyList<FlightWaypoint> wps,
            out Vector3 landing)
        {
            landing = to;
            float fallbackGroundY = wps.Count > 0
                ? wps[0].Position.y - DiscLieGround.DiscRestLift
                : to.y;
            float clearance = DiscLieGround.DiscRestLift;

            const int steps = 6;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                var pos = Vector3.Lerp(from, to, t);
                float groundY = ResolveLandingGroundY(pos, fallbackGroundY);

                if (pos.y > groundY + clearance)
                    continue;

                landing = new Vector3(pos.x, groundY + clearance, pos.z);
                return true;
            }

            return false;
        }

        static float ResolveLandingGroundY(Vector3 pos, float fallbackGroundY)
        {
            if (DiscLieGround.IsInWaterFootprint(pos.x, pos.z))
                return DiscLieGround.SampleWaterSurfaceY(pos, fallbackGroundY);

            return DiscLieGround.SampleTerrainY(pos, fallbackGroundY);
        }

        static Vector3 SamplePathAtTime(System.Collections.Generic.IReadOnlyList<FlightWaypoint> wps, float time)
        {
            for (int i = 0; i < wps.Count - 1; i++)
            {
                if (time > wps[i + 1].Time)
                    continue;

                float segDuration = wps[i + 1].Time - wps[i].Time;
                float localT = segDuration > 1e-5f ? (time - wps[i].Time) / segDuration : 0f;
                return Vector3.Lerp(wps[i].Position, wps[i + 1].Position, localT);
            }

            return wps[wps.Count - 1].Position;
        }

        void OrientDiscInFlight(System.Collections.Generic.IReadOnlyList<FlightWaypoint> wps, float simTime,
            float pathProgress)
        {
            const float sampleDt = 0.05f;
            var from = SamplePathAtTime(wps, Mathf.Max(0f, simTime - sampleDt));
            var to = SamplePathAtTime(wps, simTime + sampleDt);
            var vel = to - from;
            vel.y = 0f;

            if (vel.sqrMagnitude < 1e-8f)
                return;

            float yaw = Mathf.Atan2(vel.x, vel.z) * Mathf.Rad2Deg;
            float bank = Mathf.Sin(pathProgress * Mathf.PI) * 16f;
            ApplyDiscRotation(Quaternion.Euler(flightPitchDegrees, yaw, bank));
        }

        static float YawFromWaypoints(System.Collections.Generic.IReadOnlyList<FlightWaypoint> wps)
        {
            if (wps.Count < 2)
                return 0f;

            return YawFromPosition(wps[wps.Count - 2].Position, wps[wps.Count - 1].Position);
        }

        static float YawFromPosition(Vector3 from, Vector3 to)
        {
            var vel = to - from;
            vel.y = 0f;

            if (vel.sqrMagnitude < 1e-8f)
                return 0f;

            return Mathf.Atan2(vel.x, vel.z) * Mathf.Rad2Deg;
        }

        static Quaternion FlatRotation(float yaw) => Quaternion.Euler(0f, yaw, 0f);

        void ApplyDiscRotation(Quaternion rot)
        {
            discTransform.rotation = rot;
            _restYaw = rot.eulerAngles.y;
        }
    }
}

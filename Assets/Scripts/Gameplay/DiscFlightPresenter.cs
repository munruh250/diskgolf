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
            discTransform.localScale = _restScale.sqrMagnitude > 0f
                ? _restScale
                : new Vector3(GreyboxScale.DiscDiameterM, GreyboxScale.DiscThicknessM, GreyboxScale.DiscDiameterM);

            _restYaw = discTransform.rotation.eulerAngles.y;
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

                if (TreeObstacle.TryHitSegment(previousPos, nextPos, discRadius, out var hitPos))
                {
                    discTransform.position = hitPos;
                    ApplyDiscRotation(FlatRotation(YawFromPosition(previousPos, hitPos)));
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

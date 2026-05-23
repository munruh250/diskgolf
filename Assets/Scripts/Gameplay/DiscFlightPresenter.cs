using System;
using System.Collections;
using DiskGolf.Flight;
using UnityEngine;

namespace DiskGolf.Gameplay
{
    public class DiscFlightPresenter : MonoBehaviour
    {
        [SerializeField] Transform discTransform;

        [SerializeField] float flightSpeed = 1f;

        [SerializeField] float flightTiltDegrees = 22f;

        Action<FlightPath> _onComplete;

        FlightPath _activePath;

        Vector3 _restScale;

        Quaternion _restRotation;

        public bool IsFlying { get; private set; }

        public float FlightProgress { get; private set; }

        void Awake()
        {
            if (discTransform == null)
                return;

            _restScale = discTransform.localScale;
            _restRotation = discTransform.rotation;
        }

        public void SetPosition(Vector3 pos) => SetPositionAndRotation(pos, _restRotation);

        public void SetPositionAndRotation(Vector3 pos, Quaternion rot)
        {
            if (discTransform == null)
                return;

            discTransform.gameObject.SetActive(true);
            discTransform.position = pos;
            discTransform.rotation = rot;
            discTransform.localScale = _restScale.sqrMagnitude > 0f
                ? _restScale
                : new Vector3(GreyboxScale.DiscDiameterM, GreyboxScale.DiscThicknessM, GreyboxScale.DiscDiameterM);
        }

        public void Play(FlightPath path, Action<FlightPath> onComplete)
        {
            _activePath = path;
            _onComplete = onComplete;

            StopAllCoroutines();
            StartCoroutine(FlyRoutine());
        }

        IEnumerator FlyRoutine()
        {
            var wps = _activePath.Waypoints;

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

            float lastT = Mathf.Max(wps[wps.Count - 1].Time, 1e-4f);

            float duration = lastT / Mathf.Max(flightSpeed, 1e-4f);

            while (elapsed < duration && discTransform != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(duration > 0f ? elapsed / duration : 1f);
                FlightProgress = t;

                float pathT = t * (wps.Count - 1);
                var i = Mathf.Min(Mathf.FloorToInt(pathT), wps.Count - 2);
                float localT = pathT - i;

                var from = wps[i].Position;
                var to = wps[i + 1].Position;

                discTransform.position = Vector3.Lerp(from, to, localT);
                OrientDiscInFlight(from, to, t);

                yield return null;
            }

            if (discTransform != null)
            {
                discTransform.position = wps[wps.Count - 1].Position;
                discTransform.rotation = _restRotation;
            }

            IsFlying = false;
            FlightProgress = 1f;
            _onComplete?.Invoke(_activePath);
        }

        void OrientDiscInFlight(Vector3 from, Vector3 to, float pathProgress)
        {
            var vel = to - from;
            vel.y = 0f;

            if (vel.sqrMagnitude < 1e-8f)
                return;

            var forward = vel.normalized;
            var right = Vector3.Cross(Vector3.up, forward).normalized;

            float bank = Mathf.Sin(pathProgress * Mathf.PI) * 28f;
            var noseDown = Quaternion.AngleAxis(flightTiltDegrees, right);
            discTransform.rotation = noseDown * Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0f, 0f, bank);
        }
    }
}

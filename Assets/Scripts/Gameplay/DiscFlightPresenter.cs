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

        Action<FlightPath> _onComplete;

        FlightPath _activePath;

        public void SetPosition(Vector3 pos)
        {
            if (discTransform != null)
                discTransform.position = pos;
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
                _onComplete?.Invoke(_activePath);

                yield break;
            }

            float elapsed = 0f;

            float lastT = Mathf.Max(wps[wps.Count - 1].Time, 1e-4f);

            float duration = lastT / Mathf.Max(flightSpeed, 1e-4f);

            while (elapsed < duration && discTransform != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(duration > 0f ? elapsed / duration : 1f);

                float pathT = t * (wps.Count - 1);
                var i = Mathf.Min(Mathf.FloorToInt(pathT), wps.Count - 2);
                float localT = pathT - i;

                discTransform.position = Vector3.Lerp(wps[i].Position, wps[i + 1].Position, localT);

                yield return null;
            }

            if (discTransform != null)
                discTransform.position = wps[wps.Count - 1].Position;

            _onComplete?.Invoke(_activePath);
        }
    }
}

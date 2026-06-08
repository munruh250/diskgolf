using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Keeps world-space UI facing the active gameplay camera.</summary>
    public sealed class CameraFacingBillboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
                return;

            var toCam = transform.position - cam.transform.position;
            toCam.y = 0f;

            if (toCam.sqrMagnitude < 1e-6f)
                toCam = cam.transform.forward;

            transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        }
    }
}

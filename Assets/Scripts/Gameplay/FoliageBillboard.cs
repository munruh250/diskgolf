using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Keeps foliage sprites facing the gameplay camera.</summary>
    public sealed class FoliageBillboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
                return;

            var toCam = cam.transform.position - transform.position;
            toCam.y = 0f;

            if (toCam.sqrMagnitude < 1e-4f)
                return;

            transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }
    }
}

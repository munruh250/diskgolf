using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Keeps the thrower sprite facing the active gameplay camera.</summary>
    public sealed class ThrowerBillboard : MonoBehaviour
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

            var face = -toCam.normalized;
            transform.rotation = Quaternion.LookRotation(face, Vector3.up);

            var sprite = GetComponent<SpriteRenderer>();
            if (sprite == null || transform.parent == null)
                return;

            float side = Vector3.Dot(face, transform.parent.right);
            sprite.flipX = side > 0f;
        }
    }
}

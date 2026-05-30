using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Tree collider the disc can strike mid-flight.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class TreeObstacle : MonoBehaviour
    {
        public static bool TryHitSegment(Vector3 from, Vector3 to, float radius, out Vector3 hitPosition)
        {
            hitPosition = to;

            var delta = to - from;
            float distance = delta.magnitude;

            if (distance < 1e-5f)
                return false;

            var direction = delta / distance;

            if (!Physics.SphereCast(from, radius, direction, out RaycastHit hit, distance, ~0,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (hit.collider.GetComponentInParent<TreeObstacle>() == null)
                return false;

            hitPosition = hit.point - hit.normal * radius * 0.35f;
            return true;
        }
    }

    public enum CourseTreeVariant
    {
        Conical,
        Round,
    }
}

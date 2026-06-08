using System.Collections.Generic;
using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Tree collider the disc can strike mid-flight.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class TreeObstacle : MonoBehaviour
    {
        const float MinBounceFt = 2.5f;

        const float MaxBounceFt = 6f;

        void Awake() => CourseTree.FitColliderToSprite(gameObject);

        /// <summary>Returns the waypoint index where the path first enters a tree, or -1.</summary>
        public static int FindFirstHitWaypointIndex(IReadOnlyList<Vector3> points, float radius)
        {
            if (points == null || points.Count < 2)
                return -1;

            for (int i = 1; i < points.Count; i++)
            {
                if (TryHitSegment(points[i - 1], points[i], radius, out _))
                    return i;
            }

            return -1;
        }

        public static bool PathIntersectsTree(IReadOnlyList<Vector3> points, float radius) =>
            FindFirstHitWaypointIndex(points, radius) >= 0;

        public static bool TryHitSegment(Vector3 from, Vector3 to, float radius, out TreeHitInfo hitInfo)
        {
            hitInfo = default;

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

            var tree = hit.collider.GetComponentInParent<TreeObstacle>();
            if (tree == null)
                return false;

            var hitPosition = hit.point - hit.normal * radius * 0.35f;
            var bounceDirection = ComputeBounceDirection(tree.transform, hitPosition);
            float bounceDistanceM = Random.Range(MinBounceFt, MaxBounceFt) * 0.3048f;

            hitInfo = new TreeHitInfo
            {
                HitPosition = hitPosition,
                BounceDirection = bounceDirection,
                BounceDistanceM = bounceDistanceM,
                GroundFallbackY = Mathf.Min(from.y, to.y),
            };

            return true;
        }

        static Vector3 ComputeBounceDirection(Transform tree, Vector3 hitPosition)
        {
            var localHit = tree.InverseTransformPoint(hitPosition);
            float sideSign = localHit.x >= 0f ? 1f : -1f;

            var right = tree.right;
            right.y = 0f;

            if (right.sqrMagnitude < 1e-6f)
                right = Vector3.right;
            else
                right.Normalize();

            var toHit = hitPosition - tree.position;
            toHit.y = 0f;

            var lateral = right * sideSign;
            var outward = toHit.sqrMagnitude > 1e-6f ? toHit.normalized : lateral;
            var bounce = lateral * 0.75f + outward * 0.25f;
            bounce.y = 0f;

            return bounce.sqrMagnitude > 1e-6f ? bounce.normalized : lateral;
        }
    }

    public struct TreeHitInfo
    {
        public Vector3 HitPosition;

        public Vector3 BounceDirection;

        public float BounceDistanceM;

        public float GroundFallbackY;

        public Vector3 DeflectedPosition =>
            HitPosition + BounceDirection * BounceDistanceM;
    }

    public enum CourseTreeVariant
    {
        Conical,
        Round,
    }
}

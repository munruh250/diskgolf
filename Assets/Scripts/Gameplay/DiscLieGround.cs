using DiskGolf.Flight;
using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Raycasts fairway/rough geometry to place disc lies and throwers on the ground.</summary>
    public static class DiscLieGround
    {
        static readonly RaycastHit[] GroundHits = new RaycastHit[16];

        const float RayStartLift = 80f;

        const float RayLength = 160f;

        public static float DiscRestLift => GreyboxScale.DiscThicknessM * 0.5f;

        public static Vector3 SnapLie(Vector3 worldPos, float fallbackGroundY)
        {
            float groundY = SampleGroundY(worldPos, fallbackGroundY);
            return new Vector3(worldPos.x, groundY + DiscRestLift, worldPos.z);
        }

        public static LieType SampleLieType(Vector3 worldPos, float fallbackGroundY)
        {
            if (!TrySampleClosestGroundHit(worldPos, out RaycastHit hit))
                return LieType.Fairway;

            return ClassifyGroundCollider(hit.collider);
        }

        public static float SampleGroundY(Vector3 worldPos, float fallbackGroundY)
        {
            if (TrySampleClosestGroundHit(worldPos, out RaycastHit hit))
                return hit.point.y;

            return ResolveCourseGroundY(fallbackGroundY);
        }

        static bool TrySampleClosestGroundHit(Vector3 worldPos, out RaycastHit closestHit)
        {
            closestHit = default;
            float courseGroundY = ResolveCourseGroundY(worldPos.y);

            var origin = worldPos + Vector3.up * RayStartLift;
            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                GroundHits,
                RayLength,
                ~0,
                QueryTriggerInteraction.Ignore);

            float closest = float.PositiveInfinity;
            bool foundGround = false;

            for (int i = 0; i < count; i++)
            {
                ref RaycastHit hit = ref GroundHits[i];

                if (hit.collider.GetComponentInParent<TreeObstacle>() != null)
                    continue;

                if (hit.collider.CompareTag("Basket"))
                    continue;

                if (hit.distance >= closest)
                    continue;

                closest = hit.distance;
                closestHit = hit;
                foundGround = true;
            }

            if (!foundGround)
            {
                closestHit = default;
                return false;
            }

            return true;
        }

        static LieType ClassifyGroundCollider(Collider collider)
        {
            if (collider == null)
                return LieType.Fairway;

            if (collider.CompareTag("Green") || IsGreenName(collider.name))
                return LieType.Green;

            if (collider.CompareTag("Rough"))
                return LieType.Rough;

            if (collider.CompareTag("Fairway") || IsFairwayName(collider.name))
                return LieType.Fairway;

            if (collider.CompareTag("Tee"))
                return LieType.Tee;

            return LieType.Fairway;
        }

        static bool IsFairwayName(string objectName) =>
            !string.IsNullOrEmpty(objectName)
            && (objectName.StartsWith("Fairway") || objectName == CourseLayout.LegacyFairwayObjectName);

        static bool IsGreenName(string objectName) =>
            !string.IsNullOrEmpty(objectName)
            && objectName.StartsWith("Green", System.StringComparison.OrdinalIgnoreCase);

        static float ResolveCourseGroundY(float fallbackGroundY)
        {
            var fairway = FindFairwayTransform();
            if (fairway != null)
                return fairway.position.y;

            return fallbackGroundY;
        }

        static Transform FindFairwayTransform()
        {
            var underCourse = GameObject.Find(CourseLayout.RootName)?.transform;
            if (underCourse != null)
            {
                var fairway = underCourse.Find(CourseLayout.FairwayObjectName);
                if (fairway != null)
                    return fairway;

                fairway = underCourse.Find(CourseLayout.LegacyFairwayObjectName);
                if (fairway != null)
                    return fairway;
            }

            return GameObject.Find(CourseLayout.FairwayObjectName)?.transform
                ?? GameObject.Find(CourseLayout.LegacyFairwayObjectName)?.transform;
        }

        public static void EnsureGroundCollider(Transform surface)
        {
            if (surface == null || surface.GetComponent<Collider>() != null)
                return;

            var meshFilter = surface.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return;

            var collider = surface.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = meshFilter.sharedMesh;
        }
    }
}

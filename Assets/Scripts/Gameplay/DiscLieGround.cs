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

        public static float SampleGroundY(Vector3 worldPos, float fallbackGroundY)
        {
            float courseGroundY = ResolveCourseGroundY(fallbackGroundY);

            var origin = worldPos + Vector3.up * RayStartLift;
            int count = Physics.RaycastNonAlloc(
                origin,
                Vector3.down,
                GroundHits,
                RayLength,
                ~0,
                QueryTriggerInteraction.Ignore);

            float closest = float.PositiveInfinity;
            float groundY = courseGroundY;
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
                groundY = hit.point.y;
                foundGround = true;
            }

            return foundGround ? groundY : courseGroundY;
        }

        static float ResolveCourseGroundY(float fallbackGroundY)
        {
            var fairway = FindFairwayTransform();
            if (fairway != null)
                return fairway.position.y;

            return fallbackGroundY;
        }

        static Transform FindFairwayTransform()
        {
            var underCourse = GameObject.Find(CourseLayout.RootName)?.transform?.Find(CourseLayout.FairwayObjectName);
            if (underCourse != null)
                return underCourse;

            return GameObject.Find(CourseLayout.FairwayObjectName)?.transform;
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

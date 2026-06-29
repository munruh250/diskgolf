using DiskGolf.CourseEditor;
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

        /// <summary>Matches the visual water mesh offset in CourseBuilder.</summary>
        public const float WaterSurfaceLift = 0.12f;

        const float WaterContactTolerance = 0.08f;

        public static Vector3 SnapLie(Vector3 worldPos, float fallbackGroundY)
        {
            float groundY = SampleGroundY(worldPos, fallbackGroundY);
            return new Vector3(worldPos.x, groundY + DiscRestLift, worldPos.z);
        }

        public static LieType SampleLieType(Vector3 worldPos, float fallbackGroundY)
        {
            if (TrySampleHazardLie(worldPos, fallbackGroundY, out LieType hazardLie))
                return hazardLie;

            if (!TrySampleClosestGroundHit(worldPos, out RaycastHit hit))
                return LieType.Fairway;

            return ClassifyGroundCollider(hit.collider);
        }

        public static bool TrySampleHazardLie(Vector3 worldPos, float fallbackGroundY, out LieType lie)
        {
            if (TrySampleWaterContact(worldPos, fallbackGroundY, out lie))
                return true;

            if (TrySampleBuiltCourseOb(worldPos, out lie))
                return true;

            return TrySampleObTrigger(worldPos, out lie);
        }

        public static bool TrySampleWaterContact(Vector3 worldPos, float fallbackGroundY, out LieType lie)
        {
            lie = default;
            if (!IsTouchingWaterSurface(worldPos, fallbackGroundY))
                return false;

            lie = LieType.Water;
            return true;
        }

        public static float SampleWaterSurfaceY(Vector3 worldPos, float fallbackGroundY) =>
            SampleTerrainY(worldPos, fallbackGroundY) + WaterSurfaceLift;

        public static bool IsTouchingWaterSurface(Vector3 worldPos, float fallbackGroundY)
        {
            if (!IsInWaterFootprint(worldPos.x, worldPos.z))
                return false;

            float surfaceTop = SampleWaterSurfaceY(worldPos, fallbackGroundY) + DiscRestLift;
            return worldPos.y <= surfaceTop + WaterContactTolerance;
        }

        public static float SampleGroundY(Vector3 worldPos, float fallbackGroundY)
        {
            if (TrySampleClosestGroundHit(worldPos, out RaycastHit hit))
                return hit.point.y;

            return ResolveCourseGroundY(fallbackGroundY);
        }

        public static float SampleTerrainY(Vector3 worldPos, float fallbackGroundY)
        {
            var host = Object.FindFirstObjectByType<BuiltCourseHost>();
            if (host?.SourceData?.Elevation != null)
            {
                float gridY = HeightGridSampler.SampleWorldY(host.SourceData, worldPos.x, worldPos.z);
                float rayY = SampleGroundY(worldPos, fallbackGroundY);
                return Mathf.Max(gridY, rayY);
            }

            return SampleGroundY(worldPos, fallbackGroundY);
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

            if (collider.CompareTag("Water"))
                return LieType.Water;

            if (collider.CompareTag("OB"))
                return LieType.OB;

            return LieType.Fairway;
        }

        static bool TrySampleBuiltCourseOb(Vector3 worldPos, out LieType lie)
        {
            lie = default;
            var host = Object.FindFirstObjectByType<BuiltCourseHost>();
            if (host?.SourceData?.Hazards == null)
                return false;

            foreach (var hazard in host.SourceData.Hazards)
            {
                if (hazard.Type != HazardType.OB)
                    continue;

                if (!HazardGeometry.ContainsWorldPoint(host.SourceData, hazard, worldPos.x, worldPos.z))
                    continue;

                lie = LieType.OB;
                return true;
            }

            return false;
        }

        public static bool IsInWaterFootprint(float worldX, float worldZ)
        {
            var host = Object.FindFirstObjectByType<BuiltCourseHost>();
            if (host?.SourceData?.Hazards == null)
                return false;

            foreach (var hazard in host.SourceData.Hazards)
            {
                if (hazard.Type != HazardType.Water)
                    continue;

                if (HazardGeometry.ContainsWorldPoint(host.SourceData, hazard, worldX, worldZ))
                    return true;
            }

            return false;
        }

        static bool TrySampleObTrigger(Vector3 worldPos, out LieType lie)
        {
            lie = default;
            float sampleRadius = Mathf.Max(0.35f, GreyboxScale.DiscDiameterM * 0.45f);
            var hits = Physics.OverlapSphere(
                worldPos,
                sampleRadius,
                ~0,
                QueryTriggerInteraction.Collide);

            foreach (var collider in hits)
            {
                if (collider == null || !collider.isTrigger)
                    continue;

                if (collider.CompareTag("OB"))
                {
                    lie = LieType.OB;
                    return true;
                }
            }

            return false;
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

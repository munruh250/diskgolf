using DiskGolf.CourseEditor;
using DiskGolf.CourseEditor.Authoring;
using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.UI.CourseEditor
{
    public static class CourseEditorPointerPick
    {
        public enum PickKind
        {
            None,
            Tile,
            Foliage,
            Tee,
            Basket
        }

        public struct PickResult
        {
            public PickKind Kind;
            public Vector2Int Tile;
            public int FoliageIndex;
            public Vector3 WorldPoint;
        }

        const float FoliagePickRadius = 1.35f;
        const float MarkerPickRadius = 1.1f;

        public static bool TryPick(HoleData data, UnityEngine.Camera camera, out PickResult result)
        {
            result = default;
            if (data == null || camera == null)
                return false;

            var ray = camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            var hits = Physics.RaycastAll(ray, 10000f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (hit.collider == null)
                    continue;

                if (hit.collider.GetComponentInParent<TreeObstacle>() != null)
                {
                    int foliageIndex = FindFoliageIndexAt(data, hit.point, FoliagePickRadius);
                    if (foliageIndex >= 0)
                    {
                        result = new PickResult
                        {
                            Kind = PickKind.Foliage,
                            FoliageIndex = foliageIndex,
                            WorldPoint = hit.point
                        };
                        return true;
                    }

                    continue;
                }

                if (hit.collider.CompareTag("Tee"))
                {
                    result = new PickResult
                    {
                        Kind = PickKind.Tee,
                        WorldPoint = hit.point
                    };
                    if (CourseAuthoringGrid.TryWorldToTile(data, hit.point, out Vector2Int teeTile))
                        result.Tile = teeTile;
                    return true;
                }

                if (hit.collider.CompareTag("Basket"))
                {
                    result = new PickResult
                    {
                        Kind = PickKind.Basket,
                        WorldPoint = hit.point
                    };
                    if (CourseAuthoringGrid.TryWorldToTile(data, hit.point, out Vector2Int basketTile))
                        result.Tile = basketTile;
                    return true;
                }

                if (!IsBuiltCourseCollider(hit.collider))
                    continue;

                if (!CourseAuthoringGrid.TryWorldToTile(data, hit.point, out Vector2Int tile))
                    return false;

                result = new PickResult
                {
                    Kind = PickKind.Tile,
                    Tile = tile,
                    WorldPoint = hit.point
                };
                return true;
            }

            if (data.Hole.Tee != Vector2.zero
                && TryPickMarker(data, data.Hole.Tee, ray, MarkerPickRadius, out Vector3 teePoint))
            {
                result = new PickResult { Kind = PickKind.Tee, WorldPoint = teePoint };
                CourseAuthoringGrid.TryWorldToTile(data, teePoint, out result.Tile);
                return true;
            }

            if (data.Hole.Basket != Vector2.zero
                && TryPickMarker(data, data.Hole.Basket, ray, MarkerPickRadius, out Vector3 basketPoint))
            {
                result = new PickResult { Kind = PickKind.Basket, WorldPoint = basketPoint };
                CourseAuthoringGrid.TryWorldToTile(data, basketPoint, out result.Tile);
                return true;
            }

            return false;
        }

        public static int FindFoliageIndexAt(HoleData data, Vector3 world, float maxRadius)
        {
            if (data?.Placements == null || data.Placements.Count == 0)
                return -1;

            int bestIndex = -1;
            float bestDistanceSq = maxRadius * maxRadius;
            for (int i = 0; i < data.Placements.Count; i++)
            {
                var placement = data.Placements[i];
                if (placement == null)
                    continue;

                float dx = placement.X - world.x;
                float dz = placement.Z - world.z;
                float distanceSq = dx * dx + dz * dz;
                if (distanceSq > bestDistanceSq)
                    continue;

                bestDistanceSq = distanceSq;
                bestIndex = i;
            }

            return bestIndex;
        }

        static bool TryPickMarker(HoleData data, Vector2 marker, Ray ray, float radius, out Vector3 worldPoint)
        {
            worldPoint = default;
            float groundY = HeightGridSampler.SampleWorldY(data, marker.x, marker.y);
            var center = new Vector3(marker.x, groundY, marker.y);
            var toCenter = center - ray.origin;
            float along = Vector3.Dot(toCenter, ray.direction);
            if (along < 0f)
                return false;

            var closest = ray.origin + ray.direction * along;
            float distanceSq = (new Vector2(closest.x, closest.z) - marker).sqrMagnitude;
            if (distanceSq > radius * radius)
                return false;

            worldPoint = center;
            return true;
        }

        static bool IsBuiltCourseCollider(Collider collider) =>
            collider != null && collider.GetComponentInParent<BuiltCourseHost>() != null;
    }
}

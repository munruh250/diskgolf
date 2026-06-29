using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    /// <summary>
    /// Bounds provider for editor-built courses (mirrors CourseLayout minimap API).
    /// </summary>
    public sealed class BuiltCourseHost : MonoBehaviour
    {
        public const string RootName = "BuiltCourse";
        public const float MinimapBoundsPadding = 1.1f;

        [SerializeField] Transform teePad;
        [SerializeField] Transform basket;

        HoleData sourceData;
        Bounds worldBounds;
        bool boundsReady;

        public Transform TeePad => teePad;
        public Transform Basket => basket;
        public HoleData SourceData => sourceData;

        public Bounds WorldBounds
        {
            get
            {
                if (!boundsReady)
                    RefreshBounds();

                return worldBounds;
            }
        }

        public void Bind(Transform tee, Transform basketTransform, HoleData data)
        {
            teePad = tee;
            basket = basketTransform;
            sourceData = data;
            RefreshBounds();
        }

        public void RefreshBounds()
        {
            bool hasBounds = false;
            var bounds = new Bounds();

            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (teePad != null)
            {
                if (!hasBounds)
                {
                    bounds = new Bounds(teePad.position, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(teePad.position);
                }
            }

            if (basket != null)
            {
                if (!hasBounds)
                {
                    bounds = new Bounds(basket.position, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(basket.position);
                }
            }

            if (!hasBounds)
                bounds = new Bounds(transform.position, Vector3.one);

            worldBounds = bounds;
            boundsReady = true;
        }

        public void ComputeMinimapFraming(float viewAspect, out Vector3 center, out float orthographicSize)
        {
            var bounds = WorldBounds;
            center = bounds.center;

            float halfHeight = bounds.extents.z * MinimapBoundsPadding;
            float halfWidth = bounds.extents.x * MinimapBoundsPadding;
            orthographicSize = Mathf.Max(halfHeight, halfWidth / Mathf.Max(viewAspect, 1e-4f));
        }

        public Vector2 WorldToNormalizedMap(Vector3 world, float viewAspect)
        {
            ComputeMinimapFraming(viewAspect, out var center, out float orthoSize);

            float halfX = orthoSize * viewAspect;
            float halfZ = orthoSize;

            float u = halfX > 1e-4f ? (world.x - (center.x - halfX)) / (halfX * 2f) : 0.5f;
            float v = halfZ > 1e-4f ? (world.z - (center.z - halfZ)) / (halfZ * 2f) : 0.5f;
            return new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
        }

        public Vector2 WorldToMapAnchored(Vector3 world, RectTransform mapRect, float viewAspect)
        {
            var uv = WorldToNormalizedMap(world, viewAspect);
            var rect = mapRect.rect;
            return new Vector2(uv.x * rect.width - rect.width * 0.5f, uv.y * rect.height - rect.height * 0.5f);
        }

        public Transform TreesRoot => transform.Find("Foliage");

        public void Refresh() => RefreshBounds();

        public void ApplyMinimapLayer()
        {
            int layer = LayerMask.NameToLayer(CourseLayout.MinimapLayerName);
            if (layer < 0)
                return;

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is SpriteRenderer)
                    continue;

                renderer.gameObject.layer = layer;
            }
        }
    }
}

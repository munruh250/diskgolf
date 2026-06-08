using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Builds a billboard tree sprite with a blocking collider.</summary>
    public static class CourseTree
    {
        /// <summary>Collider is this fraction of sprite size — smaller = more forgiving misses.</summary>
        const float ColliderSizeScale = 0.75f;

        public static Transform Spawn(Transform parent, Vector3 worldPosition, CourseTreeVariant variant,
            float yawDegrees = 0f)
        {
            var spriteName = variant == CourseTreeVariant.Round
                ? FoliageSprites.TreeRound
                : FoliageSprites.TreeConical;

            var sprite = FoliageSprites.Load(spriteName);
            if (sprite == null)
                return null;

            var go = new GameObject(variant + "Tree");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPosition;
            go.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10;

            go.AddComponent<FoliageBillboard>();

            var collider = go.AddComponent<CapsuleCollider>();
            FitColliderToSprite(collider, sprite);
            go.AddComponent<TreeObstacle>();

            return go.transform;
        }

        public static void FitColliderToSprite(GameObject treeGo)
        {
            if (treeGo == null)
                return;

            var renderer = treeGo.GetComponent<SpriteRenderer>();
            var collider = treeGo.GetComponent<CapsuleCollider>();

            if (renderer == null || collider == null || renderer.sprite == null)
                return;

            FitColliderToSprite(collider, renderer.sprite);
        }

        public static void FitColliderToSprite(CapsuleCollider collider, Sprite sprite)
        {
            if (collider == null || sprite == null)
                return;

            var bounds = sprite.bounds;
            float width = Mathf.Max(bounds.size.x, bounds.size.z);
            float height = Mathf.Max(bounds.size.y, width);

            collider.direction = 1;
            collider.height = height * ColliderSizeScale;
            collider.radius = width * 0.5f * ColliderSizeScale;
            collider.center = bounds.center;

            if (collider.height < collider.radius * 2f)
                collider.radius = collider.height * 0.45f;
        }
    }
}

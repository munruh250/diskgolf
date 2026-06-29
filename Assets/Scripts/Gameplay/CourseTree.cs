using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Builds a billboard tree sprite with a blocking collider.</summary>
    public static class CourseTree
    {
        /// <summary>Collider is this fraction of sprite size — smaller = more forgiving misses.</summary>
        const float ColliderSizeScale = 0.75f;

        public static Transform Spawn(Transform parent, Vector3 groundPosition, CourseTreeVariant variant,
            float yawDegrees = 0f)
        {
            var sprite = FoliageSprites.LoadTree();
            if (sprite == null)
                return null;

            var go = new GameObject(variant + "Tree");
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10;

            go.AddComponent<FoliageBillboard>();

            var collider = go.AddComponent<CapsuleCollider>();
            FitColliderToSprite(collider, sprite);
            go.AddComponent<TreeObstacle>();

            AlignBaseToGround(go.transform, groundPosition);
            return go.transform;
        }

        public static void AlignBaseToGround(Transform tree, float groundY)
        {
            if (tree == null)
                return;

            AlignBaseToGround(tree, new Vector3(tree.position.x, groundY, tree.position.z));
        }

        /// <summary>Places the sprite base and collider bottom on the sampled ground height.</summary>
        public static void AlignBaseToGround(Transform tree, Vector3 groundPosition)
        {
            if (tree == null)
                return;

            float scaleY = tree.lossyScale.y;
            float anchorLocalY = GetGroundAnchorLocalY(tree);
            float y = groundPosition.y - anchorLocalY * scaleY;
            tree.position = new Vector3(groundPosition.x, y, groundPosition.z);
        }

        static float GetGroundAnchorLocalY(Transform tree)
        {
            if (tree.TryGetComponent<SpriteRenderer>(out var renderer) && renderer.sprite != null)
                return renderer.sprite.bounds.min.y;

            if (tree.TryGetComponent<CapsuleCollider>(out var collider))
                return collider.center.y - collider.height * 0.5f;

            return 0f;
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
            float bottomY = bounds.min.y;

            collider.direction = 1;
            collider.height = height * ColliderSizeScale;
            collider.radius = width * 0.5f * ColliderSizeScale;
            collider.center = new Vector3(bounds.center.x, bottomY + collider.height * 0.5f, bounds.center.z);

            if (collider.height < collider.radius * 2f)
                collider.radius = collider.height * 0.45f;
        }
    }
}

using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Builds a billboard tree sprite with a blocking collider.</summary>
    public static class CourseTree
    {
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
            ConfigureCollider(collider, variant);
            go.AddComponent<TreeObstacle>();

            return go.transform;
        }

        static void ConfigureCollider(CapsuleCollider collider, CourseTreeVariant variant)
        {
            switch (variant)
            {
                case CourseTreeVariant.Round:
                    collider.height = 5.5f;
                    collider.radius = 1.75f;
                    collider.center = new Vector3(0f, 2.6f, 0f);
                    break;
                default:
                    collider.height = 6.5f;
                    collider.radius = 1.25f;
                    collider.center = new Vector3(0f, 3.1f, 0f);
                    break;
            }

            collider.direction = 1;
        }
    }
}

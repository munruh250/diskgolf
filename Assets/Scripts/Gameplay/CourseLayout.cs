using System.Collections.Generic;
using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>
    /// Owns hole geometry (fairway, rough, tee, basket) and exposes world bounds for the minimap.
    /// </summary>
    public sealed class CourseLayout : MonoBehaviour
    {
        public const string RootName = "CourseElements";
        public const string FairwayObjectName = "Fairway1";
        public const string LegacyFairwayObjectName = "FairwayPlane";
        public const string GreenObjectName = "Green1";
        public const string MinimapLayerName = "MinimapCourse";

        [SerializeField] Transform fairwayPlane;

        [SerializeField] Transform greenSurface;

        [SerializeField] Transform teePad;

        [SerializeField] Transform basket;

        public const string TreesRootName = "Trees";

        [SerializeField] Transform[] roughBorders;

        [SerializeField] Transform treesRoot;

        Bounds _bounds;

        bool _boundsReady;

        public Transform FairwayPlane => fairwayPlane;

        public Transform GreenSurface => greenSurface;

        public Transform TeePad => teePad;

        public Transform Basket => basket;

        public IReadOnlyList<Transform> RoughBorders => roughBorders;

        public Transform TreesRoot => treesRoot;

        public Bounds WorldBounds
        {
            get
            {
                if (!_boundsReady)
                    Refresh();

                return _bounds;
            }
        }

        void Awake()
        {
            ResolveReferences();
            NormalizeCourseNames();
            EnsureGroundColliders();
            Refresh();
        }

        public void Refresh()
        {
            ResolveReferences();
            _bounds = ComputeBounds();
            _boundsReady = true;
        }

        public void ResolveReferences()
        {
            fairwayPlane ??= FindChildOrScene(FairwayObjectName);
            greenSurface ??= FindGreenSurface();
            teePad ??= FindByTag("Tee");
            basket ??= FindByTag("Basket");

            if (roughBorders == null || roughBorders.Length == 0)
            {
                var rough = GameObject.FindGameObjectsWithTag("Rough");
                roughBorders = new Transform[rough.Length];

                for (int i = 0; i < rough.Length; i++)
                    roughBorders[i] = rough[i].transform;
            }

            treesRoot ??= transform.Find(TreesRootName);
        }

        public Bounds ComputeBounds()
        {
            bool hasBounds = false;
            var bounds = new Bounds();

            foreach (var renderer in GetCourseRenderers())
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

            if (!hasBounds && teePad != null && basket != null)
            {
                bounds = new Bounds(teePad.position, Vector3.zero);
                bounds.Encapsulate(basket.position);
                bounds.Expand(new Vector3(12f, 0f, 12f));
            }

            return bounds;
        }

        public IEnumerable<Renderer> GetCourseRenderers()
        {
            foreach (var t in EnumerateCourseTransforms())
            {
                if (t != null && t.TryGetComponent<Renderer>(out var renderer))
                    yield return renderer;
            }
        }

        IEnumerable<Transform> EnumerateCourseTransforms()
        {
            if (fairwayPlane != null)
                yield return fairwayPlane;

            if (greenSurface != null)
                yield return greenSurface;

            if (roughBorders != null)
            {
                foreach (var rough in roughBorders)
                {
                    if (rough != null)
                        yield return rough;
                }
            }

            if (teePad != null)
                yield return teePad;

            if (basket != null)
                yield return basket;

            if (treesRoot != null)
            {
                foreach (Transform tree in treesRoot)
                {
                    if (tree != null)
                        yield return tree;
                }
            }
        }

        public Vector2 WorldToNormalizedMap(Vector3 world)
        {
            var b = WorldBounds;
            float u = b.size.x > 1e-4f ? (world.x - b.min.x) / b.size.x : 0.5f;
            float v = b.size.z > 1e-4f ? (world.z - b.min.z) / b.size.z : 0.5f;
            return new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
        }

        public Vector2 WorldToMapAnchored(Vector3 world, RectTransform mapRect)
        {
            var uv = WorldToNormalizedMap(world);
            var rect = mapRect.rect;
            return new Vector2(uv.x * rect.width - rect.width * 0.5f, uv.y * rect.height - rect.height * 0.5f);
        }

        public void ApplyMinimapLayer()
        {
            int layer = LayerMask.NameToLayer(MinimapLayerName);
            if (layer < 0)
                return;

            foreach (var t in EnumerateCourseTransforms())
            {
                if (t != null)
                    SetLayerRecursively(t.gameObject, layer);
            }

            if (treesRoot != null)
                SetLayerRecursively(treesRoot.gameObject, layer);
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;

            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        public void EnsureTopDownRough()
        {
            if (fairwayPlane == null)
                return;

            EnsureHorizontalRough("Rough1", "RoughBorder_L", Vector3.left * 95f);
            EnsureHorizontalRough("Rough2", "RoughBorder_R", Vector3.right * 95f);
            NormalizeCourseNames();
            Refresh();
        }

        void EnsureHorizontalRough(string name, string legacyName, Vector3 localOffset)
        {
            Transform existing = FindRoughTransform(name);
            if (existing == null && !string.IsNullOrEmpty(legacyName))
                existing = FindRoughTransform(legacyName);

            if (existing == null)
            {
                var roughGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
                roughGo.name = name;
                roughGo.tag = "Rough";
                roughGo.transform.SetParent(transform, false);
                existing = roughGo.transform;

                var renderer = roughGo.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var fairRenderer = fairwayPlane.GetComponent<Renderer>();
                    var baseColor = fairRenderer != null && fairRenderer.sharedMaterial != null
                        ? fairRenderer.sharedMaterial.color
                        : new Color(0.2f, 0.52f, 0.26f);

                    var mat = new Material(Shader.Find("Unlit/Color"));
                    mat.color = baseColor * new Color(0.55f, 0.4f, 0.25f);
                    renderer.sharedMaterial = mat;
                }

                Object.Destroy(existing.GetComponent<Collider>());
            }

            existing.SetParent(transform, false);
            existing.localPosition = fairwayPlane.localPosition + localOffset + Vector3.up * 0.02f;
            existing.localRotation = Quaternion.identity;
            existing.localScale = new Vector3(6f, 1f, 24f);
            existing.name = name;
        }

        static Transform FindRoughTransform(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            var underCourse = GameObject.Find(RootName)?.transform?.Find(name);
            if (underCourse != null)
                return underCourse;

            return GameObject.Find(name)?.transform;
        }

        public void EnsureFoliageAndTrees()
        {
            EnsureGroundColliders();
            EnsureTestTrees();
            Refresh();
        }

        void EnsureGroundColliders()
        {
            DiscLieGround.EnsureGroundCollider(fairwayPlane);
            DiscLieGround.EnsureGroundCollider(greenSurface);

            if (roughBorders == null)
                return;

            foreach (var rough in roughBorders)
                DiscLieGround.EnsureGroundCollider(rough);
        }

        void EnsureTestTrees()
        {
            if (teePad == null || basket == null)
                return;

            if (treesRoot == null)
            {
                var rootGo = new GameObject(TreesRootName);
                rootGo.transform.SetParent(transform, false);
                treesRoot = rootGo.transform;
            }

            if (treesRoot.childCount > 0)
                return;

            var forward = (basket.position - teePad.position).normalized;
            var right = Vector3.Cross(Vector3.up, forward);

            CourseTree.Spawn(treesRoot, teePad.position + forward * 52f + right * 13f, CourseTreeVariant.Round, 12f);
            CourseTree.Spawn(treesRoot, teePad.position + forward * 88f + right * -11f, CourseTreeVariant.Round, -6f);
            CourseTree.Spawn(treesRoot, teePad.position + forward * 124f + right * 15f, CourseTreeVariant.Round, -18f);
        }

        public static CourseLayout EnsureInScene()
        {
            var rootGo = GameObject.Find(RootName) ?? new GameObject(RootName);

            void Reparent(Transform t)
            {
                if (t == null || t.parent == rootGo.transform)
                    return;

                t.SetParent(rootGo.transform, true);
            }

            Reparent(GameObject.Find(FairwayObjectName)?.transform);
            Reparent(FindGreenSurface());

            foreach (var rough in GameObject.FindGameObjectsWithTag("Rough"))
                Reparent(rough.transform);

            Reparent(GameObject.FindGameObjectWithTag("Tee")?.transform);
            Reparent(GameObject.FindGameObjectWithTag("Basket")?.transform);

            var layout = rootGo.GetComponent<CourseLayout>() ?? rootGo.AddComponent<CourseLayout>();
            layout.ResolveReferences();
            layout.EnsureTopDownRough();
            layout.NormalizeCourseNames();
            layout.EnsureFoliageAndTrees();
            layout.ResolveReferences();
            layout.ApplyMinimapLayer();
            layout.Refresh();
            return layout;
        }

        public void NormalizeCourseNames()
        {
            ResolveReferences();

            if (fairwayPlane != null && fairwayPlane.name != FairwayObjectName)
                fairwayPlane.name = FairwayObjectName;

            if (greenSurface != null)
            {
                if (!greenSurface.CompareTag("Green"))
                    greenSurface.tag = "Green";

                if (greenSurface.name != GreenObjectName)
                    greenSurface.name = GreenObjectName;
            }

            int roughIndex = 1;
            var seen = new HashSet<Transform>();

            if (roughBorders != null)
            {
                foreach (var rough in roughBorders)
                {
                    if (rough == null || !seen.Add(rough) || rough.CompareTag("Green"))
                        continue;

                    rough.name = $"Rough{roughIndex++}";
                }
            }

            foreach (Transform child in transform)
            {
                if (child == null || !child.CompareTag("Rough") || child.CompareTag("Green") || !seen.Add(child))
                    continue;

                child.name = $"Rough{roughIndex++}";
            }

            roughBorders = CollectRoughSurfaces();
        }

        Transform[] CollectRoughSurfaces()
        {
            var roughs = new List<Transform>();

            foreach (Transform child in transform)
            {
                if (child != null && child.CompareTag("Rough"))
                    roughs.Add(child);
            }

            roughs.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return roughs.ToArray();
        }

        static Transform FindGreenSurface()
        {
            var named = FindChildOrScene(GreenObjectName);
            if (named != null)
                return named;

            var tagged = FindByTag("Green");
            return tagged;
        }

        static Transform FindChildOrScene(string objectName)
        {
            var underRoot = GameObject.Find(RootName)?.transform?.Find(objectName);
            if (underRoot != null)
                return underRoot;

            var direct = GameObject.Find(objectName)?.transform;
            if (direct != null)
                return direct;

            if (objectName == FairwayObjectName)
                return FindChildOrScene(LegacyFairwayObjectName);

            return null;
        }

        static Transform FindByTag(string tag)
        {
            var go = GameObject.FindGameObjectWithTag(tag);
            return go != null ? go.transform : null;
        }
    }
}

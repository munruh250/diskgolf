using System.Collections.Generic;
using DiskGolf.CourseEditor;
using DiskGolf.Flight;
using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    public static class CourseBuilder
    {
        const string GroundRootName = "Ground";
        const string HazardsRootName = "Hazards";
        const string TeeMarkerName = "TeePad";
        const string BasketName = "Basket";

        struct TileBuildInfo
        {
            public int X;
            public int Y;
            public Vector3 Center;
        }

        public static BuiltCourseHost Build(HoleData data, ThemePack theme, Transform parent = null)
        {
            if (data == null)
                return null;

            DestroyExistingRoot(parent);

            var root = new GameObject(BuiltCourseHost.RootName);
            if (parent != null)
                root.transform.SetParent(parent, false);

            var host = root.AddComponent<BuiltCourseHost>();

            var groundRoot = new GameObject(GroundRootName).transform;
            groundRoot.SetParent(root.transform, false);
            BuildSurfaceMeshes(data, theme, groundRoot);

            var hazardsRoot = new GameObject(HazardsRootName).transform;
            hazardsRoot.SetParent(root.transform, false);
            BuildHazards(data, hazardsRoot);

            var tee = CreateTeeMarker(data, root.transform, theme);
            var basket = ResolveOrCreateBasket(data, root.transform);

            host.Bind(tee, basket, data);
            host.RefreshBounds();
            host.ApplyMinimapLayer();
            return host;
        }

        static void DestroyExistingRoot(Transform parent)
        {
            GameObject existing;
            if (parent != null)
            {
                var child = parent.Find(BuiltCourseHost.RootName);
                existing = child != null ? child.gameObject : null;
            }
            else
            {
                existing = GameObject.Find(BuiltCourseHost.RootName);
            }

            DestroyObject(existing);
        }

        static void BuildSurfaceMeshes(HoleData data, ThemePack theme, Transform groundRoot)
        {
            var grouped = new Dictionary<SurfaceTileType, List<TileBuildInfo>>();

            foreach (var tile in data.SurfaceTiles)
            {
                if (!grouped.TryGetValue(tile.Type, out var tiles))
                {
                    tiles = new List<TileBuildInfo>();
                    grouped[tile.Type] = tiles;
                }

                float x = data.Origin.x + tile.X * data.TileSize + data.TileSize * 0.5f;
                float z = data.Origin.y + tile.Y * data.TileSize + data.TileSize * 0.5f;
                tiles.Add(new TileBuildInfo
                {
                    X = tile.X,
                    Y = tile.Y,
                    Center = new Vector3(x, 0f, z)
                });
            }

            foreach (var entry in grouped)
            {
                var surfaceType = entry.Key;
                var tiles = entry.Value;

                if (tiles.Count == 0)
                    continue;

                var go = new GameObject($"{surfaceType}Mesh");
                go.transform.SetParent(groundRoot, false);
                go.tag = SurfaceTileTags.ToUnityTag(surfaceType);

                var mesh = BuildTileMesh(data, tiles, data.TileSize);
                mesh.name = $"{surfaceType}SurfaceMesh";

                var meshFilter = go.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                var meshRenderer = go.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = theme != null ? theme.GetMaterial(surfaceType) : null;

                var meshCollider = go.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
            }
        }

        static Mesh BuildTileMesh(HoleData data, List<TileBuildInfo> tiles, float tileSize)
        {
            var mesh = new Mesh();

            var vertices = new List<Vector3>(tiles.Count * 4);
            var triangles = new List<int>(tiles.Count * 6);
            var uv = new List<Vector2>(tiles.Count * 4);

            float half = tileSize * 0.5f;

            for (int i = 0; i < tiles.Count; i++)
            {
                int baseIndex = vertices.Count;
                var tile = tiles[i];
                var center = tile.Center;
                var corners = HeightGridSampler.TileCornerHeights(data, tile.X, tile.Y);

                vertices.Add(new Vector3(center.x - half, corners[0], center.z - half));
                vertices.Add(new Vector3(center.x - half, corners[1], center.z + half));
                vertices.Add(new Vector3(center.x + half, corners[2], center.z + half));
                vertices.Add(new Vector3(center.x + half, corners[3], center.z - half));

                uv.Add(new Vector2(0f, 0f));
                uv.Add(new Vector2(0f, 1f));
                uv.Add(new Vector2(1f, 1f));
                uv.Add(new Vector2(1f, 0f));

                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);

                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        static void BuildHazards(HoleData data, Transform hazardsRoot)
        {
            if (data.Hazards == null || data.Hazards.Count == 0)
            {
                return;
            }

            foreach (var hazard in data.Hazards)
            {
                if (hazard?.Vertices == null || hazard.Vertices.Count < 3)
                {
                    continue;
                }

                CreateHazardTrigger(data, hazardsRoot, hazard);
            }
        }

        static void CreateHazardTrigger(HoleData data, Transform hazardsRoot, HazardPolygon hazard)
        {
            HazardGeometry.ComputeWorldBounds(data, hazard, out Bounds bounds);
            if (bounds.size.sqrMagnitude <= 0f)
            {
                return;
            }

            string prefix = hazard.Type == HazardType.OB ? "OB" : "Water";
            var go = new GameObject($"{prefix}_{hazard.Id}");
            go.transform.SetParent(hazardsRoot, false);
            go.transform.position = bounds.center;
            go.tag = HazardTags.ToUnityTag(hazard.Type);

            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = bounds.size;
        }

        static Transform CreateTeeMarker(HoleData data, Transform root, ThemePack theme)
        {
            var tee = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tee.name = TeeMarkerName;
            tee.tag = "Tee";
            tee.transform.SetParent(root, false);

            float teeY = HeightGridSampler.SampleWorldY(data, data.Hole.Tee.x, data.Hole.Tee.y);
            tee.transform.position = new Vector3(data.Hole.Tee.x, teeY + 0.05f, data.Hole.Tee.y);
            tee.transform.localScale = new Vector3(data.TileSize * 0.6f, 0.05f, data.TileSize * 0.6f);

            if (theme != null && tee.TryGetComponent<Renderer>(out var renderer))
            {
                var material = theme.GetMaterial(SurfaceTileType.Tee);
                if (material != null)
                    renderer.sharedMaterial = material;
            }

            return tee.transform;
        }

        static Transform ResolveOrCreateBasket(HoleData data, Transform root)
        {
            float basketY = HeightGridSampler.SampleWorldY(data, data.Hole.Basket.x, data.Hole.Basket.y);

            var existing = GameObject.FindGameObjectWithTag("Basket");
            if (existing != null)
            {
                existing.transform.position = new Vector3(data.Hole.Basket.x, basketY, data.Hole.Basket.y);
                return existing.transform;
            }

            var fromPrefab = TryCreateBasketFromPrefab(root);
            if (fromPrefab != null)
            {
                fromPrefab.position = new Vector3(data.Hole.Basket.x, basketY, data.Hole.Basket.y);
                return fromPrefab;
            }

            var basket = new GameObject(BasketName);
            basket.tag = "Basket";
            basket.transform.SetParent(root, false);
            basket.transform.position = new Vector3(data.Hole.Basket.x, basketY, data.Hole.Basket.y);

            BasketVisual.Ensure(basket.transform);
            BasketCatchDetector.Ensure(basket.transform);
            return basket.transform;
        }

        static Transform TryCreateBasketFromPrefab(Transform root)
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ProjectArtPaths.Prefabs.Basket);
            if (prefab == null)
                return null;

            var instance = UnityEditor.PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                return null;

            instance.name = BasketName;
            instance.tag = "Basket";
            instance.transform.SetParent(root, false);
            return instance.transform;
#else
            return null;
#endif
        }

        static void DestroyObject(Object target)
        {
            if (target == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(target);
                return;
            }
#endif
            Object.Destroy(target);
        }
    }
}

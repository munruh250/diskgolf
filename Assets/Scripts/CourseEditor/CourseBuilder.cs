using System.Collections.Generic;
using DiskGolf.Gameplay;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DiskGolf.CourseEditor
{
    public static class CourseBuilder
    {
        const string GroundRootName = "Ground";
        const string TeeMarkerName = "TeePad";
        const string BasketName = "Basket";

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

            var tee = CreateTeeMarker(data, root.transform, theme);
            var basket = ResolveOrCreateBasket(data, root.transform);

            host.Bind(tee, basket);
            host.RefreshBounds();
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
            var grouped = new Dictionary<SurfaceTileType, List<Vector3>>();

            foreach (var tile in data.SurfaceTiles)
            {
                if (!grouped.TryGetValue(tile.Type, out var centers))
                {
                    centers = new List<Vector3>();
                    grouped[tile.Type] = centers;
                }

                float x = data.Origin.x + tile.X * data.TileSize + data.TileSize * 0.5f;
                float z = data.Origin.y + tile.Y * data.TileSize + data.TileSize * 0.5f;
                centers.Add(new Vector3(x, 0f, z));
            }

            foreach (var entry in grouped)
            {
                var surfaceType = entry.Key;
                var centers = entry.Value;

                if (centers.Count == 0)
                    continue;

                var go = new GameObject($"{surfaceType}Mesh");
                go.transform.SetParent(groundRoot, false);
                go.tag = SurfaceTileTags.ToUnityTag(surfaceType);

                var mesh = BuildTileMesh(centers, data.TileSize);
                mesh.name = $"{surfaceType}SurfaceMesh";

                var meshFilter = go.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                var meshRenderer = go.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = theme != null ? theme.GetMaterial(surfaceType) : null;

                var meshCollider = go.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
            }
        }

        static Mesh BuildTileMesh(List<Vector3> tileCenters, float tileSize)
        {
            var mesh = new Mesh();

            var vertices = new List<Vector3>(tileCenters.Count * 4);
            var triangles = new List<int>(tileCenters.Count * 6);
            var uv = new List<Vector2>(tileCenters.Count * 4);

            float half = tileSize * 0.5f;

            for (int i = 0; i < tileCenters.Count; i++)
            {
                int baseIndex = vertices.Count;
                var center = tileCenters[i];

                vertices.Add(center + new Vector3(-half, 0f, -half));
                vertices.Add(center + new Vector3(-half, 0f, half));
                vertices.Add(center + new Vector3(half, 0f, half));
                vertices.Add(center + new Vector3(half, 0f, -half));

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

        static Transform CreateTeeMarker(HoleData data, Transform root, ThemePack theme)
        {
            var tee = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tee.name = TeeMarkerName;
            tee.tag = "Tee";
            tee.transform.SetParent(root, false);
            tee.transform.position = new Vector3(data.Hole.Tee.x, 0.05f, data.Hole.Tee.y);
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
            var existing = GameObject.FindGameObjectWithTag("Basket");
            if (existing != null)
            {
                var pos = existing.transform.position;
                existing.transform.position = new Vector3(data.Hole.Basket.x, pos.y, data.Hole.Basket.y);
                return existing.transform;
            }

            var fromPrefab = TryCreateBasketFromPrefab(root);
            if (fromPrefab != null)
            {
                fromPrefab.position = new Vector3(data.Hole.Basket.x, fromPrefab.position.y, data.Hole.Basket.y);
                return fromPrefab;
            }

            var basket = new GameObject(BasketName);
            basket.tag = "Basket";
            basket.transform.SetParent(root, false);
            basket.transform.position = new Vector3(data.Hole.Basket.x, 0f, data.Hole.Basket.y);

            BasketVisual.Ensure(basket.transform);
            BasketCatchDetector.Ensure(basket.transform);
            return basket.transform;
        }

        static Transform TryCreateBasketFromPrefab(Transform root)
        {
#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectArtPaths.Prefabs.Basket);
            if (prefab == null)
                return null;

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
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

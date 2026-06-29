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
        const string FoliageRootName = "Foliage";
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

            var foliageRoot = new GameObject(FoliageRootName).transform;
            foliageRoot.SetParent(root.transform, false);
            BuildFoliage(data, theme, foliageRoot);

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

        static readonly List<Vector2Int> HazardTileScratch = new();

        static void BuildSurfaceMeshes(HoleData data, ThemePack theme, Transform groundRoot)
        {
            var grouped = new Dictionary<SurfaceTileType, List<TileBuildInfo>>();

            foreach (var tile in data.SurfaceTiles)
            {
                if (HazardGeometry.IsTileInsideHazard(data, tile.X, tile.Y, HazardType.Water))
                {
                    continue;
                }

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
            BuildPaintedHazardTiles(data, hazardsRoot);

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

        static void BuildPaintedHazardTiles(HoleData data, Transform hazardsRoot)
        {
            if (data.HazardTiles == null || data.HazardTiles.Count == 0)
            {
                return;
            }

            HazardTileScratch.Clear();
            var obTiles = new List<Vector2Int>();

            foreach (var tile in data.HazardTiles)
            {
                if (tile.Type == HazardType.Water)
                    HazardTileScratch.Add(new Vector2Int(tile.X, tile.Y));
                else
                    obTiles.Add(new Vector2Int(tile.X, tile.Y));
            }

            if (HazardTileScratch.Count > 0)
                AddPaintedWaterSurface(data, hazardsRoot, HazardTileScratch);

            foreach (var tile in obTiles)
                CreatePaintedObTrigger(data, hazardsRoot, tile);
        }

        static void AddPaintedWaterSurface(HoleData data, Transform hazardsRoot, List<Vector2Int> tiles)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            AppendElevatedTileQuads(data, tiles, data.TileSize, WaterSurfaceLift, vertices, triangles);

            if (vertices.Count == 0)
            {
                return;
            }

            var mesh = new Mesh { name = "WaterSurface_Painted" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var surfaceGo = new GameObject("WaterSurface_Painted");
            surfaceGo.transform.SetParent(hazardsRoot, false);
            surfaceGo.tag = HazardTags.ToUnityTag(HazardType.Water);

            var meshFilter = surfaceGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = surfaceGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = GetWaterMaterial();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        static void CreatePaintedObTrigger(HoleData data, Transform hazardsRoot, Vector2Int tile)
        {
            float tileSize = data.TileSize;
            float centerX = data.Origin.x + tile.x * tileSize + tileSize * 0.5f;
            float centerZ = data.Origin.y + tile.y * tileSize + tileSize * 0.5f;
            float centerY = HeightGridSampler.SampleWorldY(data, centerX, centerZ);

            var go = new GameObject($"OB_tile_{tile.x}_{tile.y}");
            go.transform.SetParent(hazardsRoot, false);
            go.tag = HazardTags.ToUnityTag(HazardType.OB);
            go.transform.position = new Vector3(centerX, centerY + 1f, centerZ);

            var collider = go.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(tileSize, 2f, tileSize);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.name = "OB_Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0f, -0.98f, 0f);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.transform.localScale = new Vector3(tileSize * 0.92f, tileSize * 0.92f, 1f);

            var quadCollider = visual.GetComponent<Collider>();
            if (quadCollider != null)
                UnityEngine.Object.Destroy(quadCollider);

            var renderer = visual.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetObDebugMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        static Material _obDebugMaterial;

        static Material GetObDebugMaterial()
        {
            if (_obDebugMaterial != null)
                return _obDebugMaterial;

            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            _obDebugMaterial = new Material(shader);
            _obDebugMaterial.color = new Color(0.92f, 0.18f, 0.14f, 0.9f);
            return _obDebugMaterial;
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
            go.tag = HazardTags.ToUnityTag(hazard.Type);

            if (hazard.Type == HazardType.OB)
            {
                go.transform.position = bounds.center;
                var collider = go.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = bounds.size;
            }

            if (hazard.Type == HazardType.Water)
                AddWaterSurface(data, hazardsRoot, hazard);
        }

        const float WaterSurfaceLift = DiscLieGround.WaterSurfaceLift;

        static readonly Color WaterSurfaceColor = new(0.18f, 0.42f, 0.95f, 0.82f);

        static Material _waterMaterial;

        static void AddWaterSurface(HoleData data, Transform hazardsRoot, HazardPolygon hazard)
        {
            HazardGeometry.CollectTilesInside(data, hazard, HazardTileScratch);
            if (HazardTileScratch.Count == 0)
            {
                return;
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            AppendElevatedTileQuads(data, HazardTileScratch, data.TileSize, WaterSurfaceLift, vertices, triangles);

            if (vertices.Count == 0)
            {
                return;
            }

            var mesh = new Mesh { name = $"WaterSurface_{hazard.Id}" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var surfaceGo = new GameObject($"WaterSurface_{hazard.Id}");
            surfaceGo.transform.SetParent(hazardsRoot, false);
            surfaceGo.tag = HazardTags.ToUnityTag(HazardType.Water);

            var meshFilter = surfaceGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var meshRenderer = surfaceGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = GetWaterMaterial();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        static void AppendElevatedTileQuads(
            HoleData data,
            IReadOnlyList<Vector2Int> tiles,
            float tileSize,
            float yLift,
            List<Vector3> vertices,
            List<int> triangles)
        {
            float half = tileSize * 0.5f;

            for (int i = 0; i < tiles.Count; i++)
            {
                int tileX = tiles[i].x;
                int tileY = tiles[i].y;
                int baseIndex = vertices.Count;
                float centerX = data.Origin.x + tileX * tileSize + half;
                float centerZ = data.Origin.y + tileY * tileSize + half;
                var corners = HeightGridSampler.TileCornerHeights(data, tileX, tileY);

                vertices.Add(new Vector3(centerX - half, corners[0] + yLift, centerZ - half));
                vertices.Add(new Vector3(centerX - half, corners[1] + yLift, centerZ + half));
                vertices.Add(new Vector3(centerX + half, corners[2] + yLift, centerZ + half));
                vertices.Add(new Vector3(centerX + half, corners[3] + yLift, centerZ - half));

                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);

                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }
        }

        static Material GetWaterMaterial()
        {
            if (_waterMaterial != null)
                return _waterMaterial;

            var shader = Shader.Find("Standard");
            _waterMaterial = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
            _waterMaterial.color = WaterSurfaceColor;
            if (_waterMaterial.HasProperty("_Mode"))
            {
                _waterMaterial.SetFloat("_Mode", 3f);
                _waterMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _waterMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _waterMaterial.SetInt("_ZWrite", 0);
                _waterMaterial.DisableKeyword("_ALPHATEST_ON");
                _waterMaterial.EnableKeyword("_ALPHABLEND_ON");
                _waterMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                _waterMaterial.renderQueue = 3000;
            }

            return _waterMaterial;
        }

        static void BuildFoliage(HoleData data, ThemePack theme, Transform foliageRoot)
        {
            if (data.Placements == null || data.Placements.Count == 0)
            {
                return;
            }

            foreach (var placement in data.Placements)
            {
                if (placement == null || string.IsNullOrEmpty(placement.Archetype))
                {
                    continue;
                }

                float groundY = SampleFoliageGroundY(data, placement.X, placement.Z);
                var groundPosition = new Vector3(placement.X, groundY, placement.Z);
                var variant = ResolveTreeVariant(placement.Archetype);
                var tree = CourseTree.Spawn(foliageRoot, groundPosition, variant, placement.Yaw);
                if (tree == null)
                {
                    continue;
                }

                if (theme != null && tree.TryGetComponent<SpriteRenderer>(out var renderer))
                {
                    var sprite = theme.ResolveFoliage(placement.Archetype);
                    if (sprite != null)
                    {
                        renderer.sprite = sprite;
                        CourseTree.FitColliderToSprite(tree.gameObject);
                        CourseTree.AlignBaseToGround(tree, groundPosition);
                    }
                }

                if (Mathf.Abs(placement.Scale - 1f) > 0.01f)
                {
                    tree.localScale = Vector3.one * placement.Scale;
                    CourseTree.FitColliderToSprite(tree.gameObject);
                }

                CourseTree.AlignBaseToGround(tree, groundPosition);
            }
        }

        static float SampleFoliageGroundY(HoleData data, float worldX, float worldZ)
        {
            return HeightGridSampler.SampleTileSurfaceWorldY(data, worldX, worldZ);
        }

        static CourseTreeVariant ResolveTreeVariant(string archetypeId)
        {
            if (string.IsNullOrEmpty(archetypeId))
            {
                return CourseTreeVariant.Round;
            }

            if (archetypeId.Contains("pine", System.StringComparison.OrdinalIgnoreCase)
                || archetypeId.Contains("conical", System.StringComparison.OrdinalIgnoreCase))
            {
                return CourseTreeVariant.Conical;
            }

            return CourseTreeVariant.Round;
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
            var pos = new Vector3(data.Hole.Basket.x, basketY, data.Hole.Basket.y);

            var existing = root.Find(BasketName);
            Transform basketTf = existing != null
                ? existing
                : TryCreateBasketFromPrefab(root);

            if (basketTf == null)
            {
                var basket = new GameObject(BasketName);
                basket.tag = "Basket";
                basket.transform.SetParent(root, false);
                basketTf = basket.transform;
            }

            basketTf.SetParent(root, false);
            basketTf.position = pos;
            FinalizeBasket(basketTf);
            DisableStraySceneBaskets(basketTf);
            return basketTf;
        }

        static void FinalizeBasket(Transform basketTf)
        {
            BasketVisual.Ensure(basketTf);
            BasketCatchDetector.Ensure(basketTf);
        }

        static void DisableStraySceneBaskets(Transform keptBasket)
        {
            var courseRoot = keptBasket.GetComponentInParent<BuiltCourseHost>()?.transform;
            foreach (var go in GameObject.FindGameObjectsWithTag("Basket"))
            {
                if (go == null || go.transform == keptBasket)
                {
                    continue;
                }

                if (courseRoot != null && go.transform.IsChildOf(courseRoot))
                {
                    continue;
                }

                go.SetActive(false);
            }
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

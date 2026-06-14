#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cinemachine;
using DiskGolf.Camera;
using DiskGolf.Core;
using DiskGolf.CourseEditor;
using DiskGolf.Disc;
using DiskGolf.Gameplay;
using DiskGolf.Input;
using DiskGolf.UI;
using DiskGolf.UI.Callouts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiskGolf.EditorTools
{
    /// <summary>Full scene scaffold for bootstrapping a new prototype hole (editor-only, no menu entry).</summary>
    public static class PrototypeFlat3SceneBuilder
    {
        const string PrefabsDir = ProjectArtPaths.Prefabs.GameplayRoot;

        const string CoursePrefabsDir = ProjectArtPaths.Prefabs.CourseRoot;

        const string ScenePath = ProjectArtPaths.Scenes.PrototypeFlat3;

        const string CourseEditorScenePath = ProjectArtPaths.Scenes.CourseEditor;

        [MenuItem("Disk Golf/Course/Rebuild Course Editor Scene")]
        public static void BuildCourseEditorSceneFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Course Editor Scene",
                    "Replace the Course Editor scene with a fresh throw rig (no hand-built fairway). Continue?",
                    "Rebuild",
                    "Cancel"))
                return;

            BuildCourseEditorScene();
        }

        public static void BuildCourseEditorScene()
        {
            Directory.CreateDirectory(PrefabsDir);
            Directory.CreateDirectory(CoursePrefabsDir);
            Directory.CreateDirectory(ProjectArtPaths.Scenes.PrototypeRoot);
            Directory.CreateDirectory(ProjectArtPaths.Data.ThemesRoot);
            Directory.CreateDirectory(ProjectArtPaths.Data.CoursesRoot);

            EnsureTmpEssentials();
            EnsureTags(new[] { "Fairway", "Tee", "Basket", "Circle", "Rough", "Green" });

            var fairRgb = new Color(0.2f, 0.52f, 0.26f);
            var teeMat = LoadOrCreateMaterial("MAT_Tee", new Color(0.73f, 0.57f, 0.41f),
                ProjectArtPaths.Environment.Tee.Material);
            var metalMat = LoadOrCreateMaterial("MAT_Basket", new Color(0.46f, 0.49f, 0.53f),
                ProjectArtPaths.Environment.Basket.Material);
            var discOrange = LoadOrCreateMaterial("MAT_DiscOrange", new Color(0.92f, 0.42f, 0.06f),
                ProjectArtPaths.Gameplay.DiscMaterial);

            GameObject discPrefab = LoadOrCreateDiscPrefab(discOrange);
            GameObject teePrefab = LoadOrCreateTeePrefab(teeMat);
            GameObject basketPrefab = LoadOrCreateBasketPrefab(metalMat);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            DirLight(out _);

            var teePos = new Vector3(10f, 0f, 1f);
            var basketPos = new Vector3(10f, 0f, 229f);
            var teeTf = InstantiatePrefabIntoScene(teePrefab, teePos, Quaternion.identity);
            var basketTf = InstantiatePrefabIntoScene(basketPrefab, basketPos, Quaternion.identity);

            var aimRot = Quaternion.LookRotation((basketTf.position - teeTf.position).normalized, Vector3.up);
            var throwerVisual = ThrowerVisual.Build(
                teeTf.position + aimRot * Vector3.back * 0.55f,
                aimRot);
            var throwerTf = throwerVisual.transform;

            var discTf = InstantiatePrefabIntoScene(discPrefab, throwerVisual.HandAnchor.position, aimRot);
            discTf.SetPositionAndRotation(throwerVisual.HandAnchor.position, aimRot);

            SpawnCircleVisualizer(basketTf);

            MainCam(out CinemachineBrain _);
            EventSystemBootstrap();

            var gm = new GameObject("GameManager");
            var hole = gm.AddComponent<HoleSetup>();
            AssignSerialized(hole, "teePad", teeTf);
            AssignSerialized(hole, "basket", basketTf);
            AssignSerialized(hole, "thrower", throwerTf);

            var bag = gm.AddComponent<DiscBag>();
            PopulateDiscProfiles(bag);

            var presenter = gm.AddComponent<DiscFlightPresenter>();
            AssignSerialized(presenter, "discTransform", discTf);

            var inputs = gm.AddComponent<ThrowInputHandler>();
            var controller = gm.AddComponent<ThrowController>();
            AssignSerialized(controller, "hole", hole);
            AssignSerialized(controller, "bag", bag);
            AssignSerialized(controller, "input", inputs);
            AssignSerialized(controller, "presenter", presenter);

            var powerMb = gm.AddComponent<PowerMeterUI>();
            var heightMb = gm.AddComponent<HeightMeterUI>();
            AssignSerialized(controller, "powerMeter", powerMb);
            AssignSerialized(controller, "heightMeter", heightMb);

            var hudCanvas = HudCanvas(out _);
            CalloutPrefabAuthoring.InstantiateInScene();
            var calloutHost = hudCanvas.Find(GameplayCalloutHost.RootName)?.GetComponent<GameplayCalloutHost>();
            if (calloutHost != null)
                AssignSerialized(controller, "calloutHost", calloutHost);

            var hud = hudCanvas.gameObject.AddComponent<HUDController>();
            AssignSerialized(hud, "controller", controller);
            AssignSerialized(hud, "hole", hole);
            AssignSerialized(hud, "discTransform", discTf);

            SpawnMinimap(hudCanvas, hole, discTf, null, controller);

            var aimPoint = CameraRig.EnsureAimPoint(throwerVisual.transform, basketTf);
            var cameraRoot = new GameObject("Camera").transform;
            var side = Vcam(CameraRig.SideSetupName, cameraRoot, throwerTf, aimPoint, CameraRig.SideFollowOffset, 0f);
            var flightChase = Vcam(CameraRig.FlightChaseName, cameraRoot, discTf, basketTf, CameraRig.FlightChaseOffset, 0f);
            CameraRig.ConfigureSideThrowCam(side, throwerTf, aimPoint);
            CameraRig.ConfigureFlightChaseCam(flightChase, discTf, Vector3.forward);
            side.gameObject.SetActive(true);
            flightChase.gameObject.SetActive(false);

            var director = gm.AddComponent<CameraDirector>();
            AssignSerialized(director, "sideSetupCam", side);
            AssignSerialized(director, "flightChaseCam", flightChase);
            AssignSerialized(director, "throwController", controller);
            AssignSerialized(director, "flightPresenter", presenter);
            AssignSerialized(director, "hole", hole);

            var bootstrap = gm.AddComponent<CourseEditorRuntimeBootstrap>();
            var scratch = AssetDatabase.LoadAssetAtPath<HoleDataAsset>(
                ProjectArtPaths.Data.CoursesRoot + "/_EditorScratch.asset");
            if (scratch == null)
            {
                scratch = ScriptableObject.CreateInstance<HoleDataAsset>();
                AssetDatabase.CreateAsset(scratch, ProjectArtPaths.Data.CoursesRoot + "/_EditorScratch.asset");
            }

            var theme = AssetDatabase.LoadAssetAtPath<ThemePack>(
                ProjectArtPaths.Data.ThemesRoot + "/ThemePack_Temperate.asset");
            AssignSerialized(bootstrap, "playtestHole", scratch);
            AssignSerialized(bootstrap, "theme", theme);
            AssignSerialized(bootstrap, "holeSetup", hole);

            hudCanvas.localScale = Vector3.one;
            HudSceneAuthoring.BakeMissingSceneWidgets();
            HudLayout.ApplyCleanupOnly();
            hudCanvas.localScale = Vector3.one;

            SceneHierarchy.Organize();

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, CourseEditorScenePath);
            Debug.Log($"[Disk Golf] Saved {CourseEditorScenePath} — paint in Scene view, then Playtest.");
        }

        static Material LoadOrCreateMaterial(string name, Color color, string path)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            return mat != null ? mat : SaveMaterialAsset(name, color, path);
        }

        static GameObject LoadOrCreateDiscPrefab(Material mat) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(ProjectArtPaths.Prefabs.Disc) ?? SaveDiscPrefab(mat);

        static GameObject LoadOrCreateTeePrefab(Material mat) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(ProjectArtPaths.Prefabs.TeePad) ?? SaveTeePrefab(mat);

        static GameObject LoadOrCreateBasketPrefab(Material mat) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(ProjectArtPaths.Prefabs.Basket) ?? SaveBasketPrefab(mat);

        public static void Build()
        {
            Directory.CreateDirectory(PrefabsDir);
            Directory.CreateDirectory(CoursePrefabsDir);
            Directory.CreateDirectory(ProjectArtPaths.Scenes.PrototypeRoot);
            Directory.CreateDirectory(ProjectArtPaths.Environment.Fairway.Root);
            Directory.CreateDirectory(ProjectArtPaths.Environment.Basket.Root);
            Directory.CreateDirectory(ProjectArtPaths.Environment.Tee.Root);
            Directory.CreateDirectory(ProjectArtPaths.Gameplay.DiscMaterial.Replace("/MAT_DiscOrange.mat", ""));

            EnsureTmpEssentials();

            EnsureTags(new[] { "Fairway", "Tee", "Basket", "Circle", "Rough", "Green" });

            var fairRgb = new Color(0.2f, 0.52f, 0.26f);
            var fairMat = SaveMaterialAsset("MAT_FairwayGreybox", fairRgb,
                ProjectArtPaths.Environment.Fairway.Root + "/MAT_FairwayGreybox.mat");
            var teeMat = SaveMaterialAsset("MAT_Tee", new Color(0.73f, 0.57f, 0.41f),
                ProjectArtPaths.Environment.Tee.Material);
            var metalMat = SaveMaterialAsset("MAT_Basket", new Color(0.46f, 0.49f, 0.53f),
                ProjectArtPaths.Environment.Basket.Material);

            var discOrange = SaveMaterialAsset("MAT_DiscOrange", new Color(0.92f, 0.42f, 0.06f),
                ProjectArtPaths.Gameplay.DiscMaterial);

            GameObject discPrefab = SaveDiscPrefab(discOrange);
            GameObject teePrefab = SaveTeePrefab(teeMat);
            GameObject basketPrefab = SaveBasketPrefab(metalMat);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            DirLight(out _);

            var fairPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            fairPlane.name = "Fairway1";
            fairPlane.tag = "Fairway";
            DestroyColliderImmediate(fairPlane);
            fairPlane.transform.position = Vector3.forward * (76.2f * 0.5f);
            fairPlane.transform.localScale = new Vector3(17f, 1f, 24f);
            fairPlane.GetComponent<MeshRenderer>().sharedMaterial = fairMat;

            var teeTf = InstantiatePrefabIntoScene(teePrefab, Vector3.zero, Quaternion.identity);

            var basketTf = InstantiatePrefabIntoScene(basketPrefab, new Vector3(0f, 0f, 76.2f),
                Quaternion.identity);

            var aimRot = Quaternion.LookRotation((basketTf.position - teeTf.position).normalized, Vector3.up);
            var throwerVisual = ThrowerVisual.Build(
                teeTf.position + aimRot * Vector3.back * 0.55f,
                aimRot);
            var throwerTf = throwerVisual.transform;

            var discTf = InstantiatePrefabIntoScene(discPrefab, throwerVisual.HandAnchor.position, aimRot);
            discTf.SetPositionAndRotation(
                throwerVisual.HandAnchor.position,
                aimRot);

            SpawnCircleVisualizer(basketTf);

            RoughBands(fairRgb);

            var courseLayout = CourseLayout.EnsureInScene();

            var mainCam = MainCam(out CinemachineBrain _);

            EventSystemBootstrap();

            var gm = new GameObject("GameManager");

            var hole = gm.AddComponent<HoleSetup>();

            AssignSerialized(hole,
                "teePad", teeTf);
            AssignSerialized(hole,
                "basket", basketTf);
            AssignSerialized(hole,
                "thrower", throwerTf);

            var bag = gm.AddComponent<DiscBag>();

            PopulateDiscProfiles(bag);

            var presenter = gm.AddComponent<DiscFlightPresenter>();
            AssignSerialized(presenter, "discTransform", discTf);

            var inputs = gm.AddComponent<ThrowInputHandler>();
            var controller = gm.AddComponent<ThrowController>();
            AssignSerialized(controller,
                "hole", hole);
            AssignSerialized(controller,
                "bag", bag);

            AssignSerialized(controller, "input", inputs);
            AssignSerialized(controller,
                "presenter",
                presenter);

            var hudCanvas = HudCanvas(out _);
            CalloutPrefabAuthoring.InstantiateInScene();
            var calloutHost = hudCanvas.Find(GameplayCalloutHost.RootName)?.GetComponent<GameplayCalloutHost>();
            if (calloutHost != null)
                AssignSerialized(controller, "calloutHost", calloutHost);

            HudTmpLabel(hudCanvas, new Vector2(36f, 48f), "Disc", 28f, out TextMeshProUGUI discUi);
            discUi.gameObject.name = "Disc";
            HudTmpLabel(hudCanvas, new Vector2(36f, 88f), "FLAT", 26f, out TextMeshProUGUI stanceUi);
            stanceUi.gameObject.name = "StanceLabel";

            var powerMb = gm.AddComponent<PowerMeterUI>();
            var heightMb = gm.AddComponent<HeightMeterUI>();

            AssignSerialized(controller, "powerMeter", powerMb);
            AssignSerialized(controller, "heightMeter", heightMb);

            var hud = hudCanvas.gameObject.AddComponent<HUDController>();
            AssignSerialized(hud, "controller", controller);
            AssignSerialized(hud, "hole", hole);
            AssignSerialized(hud, "discTransform", discTf);
            AssignSerialized(hud, "discText", discUi);
            AssignSerialized(hud, "stanceText", stanceUi);

            SpawnMinimap(hudCanvas, hole, discTf, courseLayout, controller);

            var aimPoint = CameraRig.EnsureAimPoint(throwerVisual.transform, basketTf);
            var cameraRoot = new GameObject("Camera").transform;

            var side = Vcam(CameraRig.SideSetupName, cameraRoot, throwerTf, aimPoint, CameraRig.SideFollowOffset, 0f);
            var flightChase = Vcam(CameraRig.FlightChaseName, cameraRoot, discTf, basketTf, CameraRig.FlightChaseOffset, 0f);

            CameraRig.ConfigureSideThrowCam(side, throwerTf, aimPoint);
            CameraRig.ConfigureFlightChaseCam(flightChase, discTf, Vector3.forward);

            side.gameObject.SetActive(true);
            flightChase.gameObject.SetActive(false);

            HudLayout.Apply();

            var director = gm.AddComponent<CameraDirector>();
            AssignSerialized(director, "sideSetupCam", side);
            AssignSerialized(director, "flightChaseCam", flightChase);
            AssignSerialized(director, "throwController", controller);
            AssignSerialized(director, "flightPresenter", presenter);
            AssignSerialized(director, "hole", hole);

            SceneHierarchy.Organize();

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[PrototypeFlat3SceneBuilder] Saved {ScenePath}");
        }

        static void ZoneTextStyle(Text t)
        {
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = 18;
            t.color = Color.white;
        }

        static void SpawnMinimap(RectTransform hudRoot, HoleSetup hole, Transform discTf, CourseLayout course,
            ThrowController throwController)
        {
            // course may be null in CourseEditor scene — MinimapUI resolves BuiltCourseHost at runtime.
            var host = new GameObject("MinimapHost", typeof(RectTransform));
            var hostRt = host.GetComponent<RectTransform>();
            hostRt.SetParent(hudRoot, false);
            hostRt.anchorMin = new Vector2(1f, 1f);
            hostRt.anchorMax = new Vector2(1f, 1f);
            hostRt.pivot = new Vector2(1f, 1f);
            hostRt.anchoredPosition = new Vector2(-20f, -20f);
            hostRt.sizeDelta = new Vector2(248f, 392f);

            var panelGo = new GameObject("MapPanel", typeof(RectTransform));
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.SetParent(hostRt, false);
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            var markerLayerGo = new GameObject("MarkerLayer", typeof(RectTransform));
            var markerLayer = markerLayerGo.GetComponent<RectTransform>();
            markerLayer.SetParent(panelRt, false);
            markerLayer.anchorMin = Vector2.zero;
            markerLayer.anchorMax = Vector2.one;
            markerLayer.offsetMin = Vector2.zero;
            markerLayer.offsetMax = Vector2.zero;

            var mapRect = panelRt;
            const float minimapAspect = 248f / 392f;
            Vector2 TeeMapPos() => course != null
                ? course.WorldToMapAnchored(hole.TeePosition, mapRect, minimapAspect)
                : Vector2.zero;
            Vector2 BasketMapPos() => course != null
                ? course.WorldToMapAnchored(hole.BasketPosition, mapRect, minimapAspect)
                : Vector2.zero;
            Vector2 DiscMapPos() => course != null
                ? course.WorldToMapAnchored(discTf.position, mapRect, minimapAspect)
                : Vector2.zero;

            var teeDot = ImageRect(markerLayer, TeeMapPos(), new Vector2(8f, 8f), Color.white).rectTransform;
            teeDot.name = "TeeDot";

            var basketDot = ImageRect(markerLayer, BasketMapPos(), new Vector2(10f, 10f), new Color(1f, 0.55f, 0.25f))
                .rectTransform;
            basketDot.name = "BasketDot";

            var discDot = ImageRect(markerLayer, DiscMapPos(), new Vector2(8f, 8f), Color.cyan).rectTransform;
            discDot.name = "DiscDot";

            var mini = host.AddComponent<MinimapUI>();
            if (course != null)
                AssignSerialized(mini, "course", course);
            AssignSerialized(mini, "hole", hole);
            AssignSerialized(mini, "discTransform", discTf);
            AssignSerialized(mini, "controller", throwController);
            AssignSerialized(mini, "markerLayer", markerLayer);
            AssignSerialized(mini, "teeDot", teeDot);
            AssignSerialized(mini, "basketDot", basketDot);
            AssignSerialized(mini, "discDot", discDot);
        }

        static Image ImageRect(RectTransform parent, Vector2 anchored, Vector2 size, Color c)
        {
            var go = new GameObject("Img", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = c;
            return img;
        }

        static CinemachineVirtualCamera Vcam(string name, Transform parent, Transform follow, Transform lookAt,
            Vector3 offset, float eulerX)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(eulerX, 0f, 0f);

            var vcam = go.AddComponent<CinemachineVirtualCamera>();
            vcam.Follow = follow;
            vcam.LookAt = lookAt;
            vcam.Priority = 10;

            var body = vcam.AddCinemachineComponent<CinemachineTransposer>();
            body.m_FollowOffset = offset;
            vcam.AddCinemachineComponent<CinemachineComposer>();
            return vcam;
        }

        static void PopulateDiscProfiles(DiscBag bag)
        {
            var profiles = DiscDefaults.AssetPaths
                .Select(AssetDatabase.LoadAssetAtPath<DiscProfile>)
                .ToArray();

            if (profiles[0] == null)
            {
                Debug.LogError(
                    "[PrototypeFlat3SceneBuilder] Missing disc profiles under Assets/Data/Discs/. "
                    + "Expected Putter, Midrange, Fairway, and Distance assets.");
                return;
            }

            var so = new SerializedObject(bag);
            var prop = so.FindProperty("discs");
            prop.arraySize = profiles.Length;

            for (int i = 0; i < profiles.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = profiles[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignSerialized(UnityEngine.Object obj, string property, UnityEngine.Object value)
        {
            var so = new SerializedObject(obj);
            var p = so.FindProperty(property);

            if (p == null)
            {
                Debug.LogWarning($"[PrototypeFlat3SceneBuilder] Missing prop `{property}` on {obj}");
                return;
            }

            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureTags(string[] tags)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");

            if (assets.Length == 0)
                return;

            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("tags");
            var known = new HashSet<string>();
            for (int i = 0; i < prop.arraySize; i++)
                known.Add(prop.GetArrayElementAtIndex(i).stringValue);

            foreach (var t in tags)
            {
                if (known.Contains(t))
                    continue;

                prop.InsertArrayElementAtIndex(prop.arraySize);
                prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = t;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static Material SaveMaterialAsset(string name, Color c, string assetPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? ProjectArtPaths.ArtRoot);
            var shader = Shader.Find("Standard");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                mat = new Material(shader) { color = c, name = name };
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            else
                mat.color = c;

            return mat;
        }

        static Material Mat(string name, Color c)
        {
            var shader = Shader.Find("Standard");
            var mat = new Material(shader) { color = c, name = name };
            return mat;
        }

        static void DestroyColliderImmediate(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null)
                Object.DestroyImmediate(col);
        }

        static GameObject SaveDiscPrefab(Material mat)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Disc";
            DestroyColliderImmediate(disc);
            disc.transform.localScale = new Vector3(
                GreyboxScale.DiscDiameterM,
                GreyboxScale.DiscThicknessM,
                GreyboxScale.DiscDiameterM);
            disc.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return SavePrefabAsset(disc, ProjectArtPaths.Prefabs.Disc);
        }

        static GameObject SaveTeePrefab(Material mat)
        {
            var tee = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tee.name = "TeePad";
            tee.tag = "Tee";
            tee.transform.localScale = new Vector3(2f, 0.08f, 2f);
            tee.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return SavePrefabAsset(tee, ProjectArtPaths.Prefabs.TeePad);
        }

        static GameObject SaveBasketPrefab(Material mat)
        {
            var root = new GameObject("Basket");

            float catchY = GreyboxScale.BasketCatchHeightM;
            float ringDiameter = GreyboxScale.BasketCatchDiameterM;
            float poleH = catchY;
            float poleRadius = GreyboxScale.PoleDiameterM * 0.5f;

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(root.transform, false);
            pole.transform.localScale = new Vector3(poleRadius * 2f, poleH * 0.5f, poleRadius * 2f);
            pole.transform.localPosition = Vector3.up * (poleH * 0.5f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = mat;
            pole.GetComponent<MeshRenderer>().enabled = false;
            DestroyColliderImmediate(pole);

            var top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            top.name = "TopRing";
            top.transform.SetParent(root.transform, false);
            top.transform.localScale = new Vector3(ringDiameter, 0.04f, ringDiameter);
            top.transform.localPosition = Vector3.up * catchY;
            top.GetComponent<MeshRenderer>().sharedMaterial = mat;
            top.GetComponent<MeshRenderer>().enabled = false;
            DestroyColliderImmediate(top);

            root.AddComponent<BasketVisual>();
            root.AddComponent<BasketCatchDetector>();

            root.tag = "Basket";
            return SavePrefabAsset(root, ProjectArtPaths.Prefabs.Basket);
        }

        static GameObject SavePrefabAsset(GameObject instance, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static Transform InstantiatePrefabIntoScene(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetPositionAndRotation(pos, rot);
            return go.transform;
        }

        static void SpawnCircleVisualizer(Transform basket)
        {
            var circle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            circle.name = "CircleZone";
            circle.tag = "Circle";
            circle.transform.SetParent(basket, false);
            var radiusMeters = 33f * 0.3048f;
            circle.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            circle.transform.localScale = new Vector3(radiusMeters * 2f, 0.02f, radiusMeters * 2f);

            var col = circle.GetComponent<CapsuleCollider>();
            if (col != null)
                Object.DestroyImmediate(col);

            var trigger = circle.AddComponent<MeshCollider>();
            trigger.convex = true;
            trigger.isTrigger = true;

            // Trigger only — a giant sphere here made the basket look massive.
            circle.GetComponent<MeshRenderer>().enabled = false;
        }

        static string ResolveTextMeshProPackageRoot()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var cache = Path.Combine(projectRoot, "Library/PackageCache");

            if (!Directory.Exists(cache))
                return null;

            return Directory.GetDirectories(cache, "com.unity.textmeshpro@*").FirstOrDefault();
        }

        static void EnsureTmpEssentials()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var tmpSettings =
                Path.Combine(projectRoot,

                    ProjectArtPaths.ThirdParty.TmpSettings);

            if (File.Exists(tmpSettings))
                return;

            var root = ResolveTextMeshProPackageRoot();

            if (string.IsNullOrEmpty(root))
            {
                Debug.LogError("[PrototypeFlat3SceneBuilder] TextMesh Pro package cache not found.");

                return;
            }

            var unityPackage = Path.Combine(root, "Package Resources", "TMP Essential Resources.unitypackage");

            if (!File.Exists(unityPackage))
            {
                Debug.LogError($"[PrototypeFlat3SceneBuilder] Missing TMP essentials package at {unityPackage}");

                return;
            }

            AssetDatabase.ImportPackage(unityPackage, false);
            AssetDatabase.Refresh();
        }

        static void RoughBands(Color fairwayBase)
        {
            void Stripe(string name, Vector3 offset, Vector3 scale)
            {
                var slab = GameObject.CreatePrimitive(PrimitiveType.Plane);
                slab.tag = "Rough";
                slab.name = name;
                slab.transform.position = Vector3.forward * (76.2f * 0.5f) + offset;
                slab.transform.rotation = Quaternion.identity;
                slab.transform.localScale = scale;
                var mat = Mat("RoughMat", fairwayBase * new Color(0.55f, 0.4f, 0.25f));
                slab.GetComponent<MeshRenderer>().sharedMaterial = mat;
                DestroyColliderImmediate(slab);
            }

            Stripe("Rough1", Vector3.left * 95f + Vector3.up * 0.02f, new Vector3(6f, 1f, 24f));
            Stripe("Rough2", Vector3.right * 95f + Vector3.up * 0.02f, new Vector3(6f, 1f, 24f));
        }

        static void DirLight(out GameObject go)
        {
            go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.9f;
            go.transform.rotation = Quaternion.Euler(50f, -34f, 0f);
        }

        static GameObject MainCam(out CinemachineBrain brain)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.AddComponent<UnityEngine.Camera>().fieldOfView = 62f;
            go.AddComponent<AudioListener>();
            brain = go.AddComponent<CinemachineBrain>();
            go.transform.SetPositionAndRotation(new Vector3(-10f,
                    4f,
                    -9f),

                Quaternion.Euler(21f,

                    30f,

                    0f));
            return go;
        }

        static void EventSystemBootstrap()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        static RectTransform HudCanvas(out Canvas canvas)
        {
            var holder = new GameObject("GameplayHUD");

            canvas = holder.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            holder.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            holder.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);

            holder.AddComponent<GraphicRaycaster>();

            var rt = holder.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            return rt;
        }

        static void HudTmpLabel(RectTransform root, Vector2 anchored, string text, float size,
            out TextMeshProUGUI tmp)
        {
            var go = new GameObject("TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(root, false);
            rect.anchorMin = new Vector2(0.5f, 0.65f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = new Vector2(900f, 120f);
            tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Top;
            TryBindTmpFont(tmp);
        }

        static TextMeshProUGUI HudTmpLabelRow(RectTransform root, Vector2 anchored, string text, float size,
            TextAlignmentOptions align)
        {
            var go = new GameObject("TMPRow", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(root, false);
            rect.anchorMin = new Vector2(0.5f, 0.55f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = new Vector2(360f, 80f);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = align;
            TryBindTmpFont(tmp);
            return tmp;
        }

        static void TryBindTmpFont(TextMeshProUGUI tmp)
        {
            HudTypography.BindFont(tmp);
        }

        static GameObject HudSliderUi(RectTransform root, Vector2 anchored, out Slider slider)
        {
            var go = new GameObject("Slider", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(root, false);
            rt.anchorMin = new Vector2(0.5f, 0.45f);
            rt.anchorMax = rt.anchorMin;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(240f, 28f);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.SetParent(rt, false);
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.9f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.SetParent(rt, false);
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = new Vector2(6f, 4f);
            fillAreaRt.offsetMax = new Vector2(-6f, -4f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.SetParent(fillAreaRt, false);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(0f, 0.78f, 0.75f, 0.95f);

            slider = go.AddComponent<Slider>();
            slider.targetGraphic = fillImg;
            slider.fillRect = fillRt;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;
            return go;
        }

        static Text HudUnityText(RectTransform root, Vector2 anchored, string msg)
        {
            var go = new GameObject("UnityText", typeof(RectTransform), typeof(Text));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(root, false);
            rect.anchorMin = new Vector2(0.5f, 0.45f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = new Vector2(200f, 40f);
            var text = go.GetComponent<Text>();
            text.text = msg;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cinemachine;
using DiskGolf.Camera;
using DiskGolf.Core;
using DiskGolf.Disc;
using DiskGolf.Gameplay;
using DiskGolf.Input;
using DiskGolf.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiskGolf.EditorTools
{
    /// <summary>Full scene scaffold (optional). Prefer <see cref="GreyboxAutoSetup"/> auto-migration on load.</summary>
    public static class PrototypeFlat3SceneBuilder
    {
        const string PrefabsDir = "Assets/Prefabs";

        const string MaterialsDir = "Assets/Materials";

        const string ScenePath = "Assets/Scenes/PrototypeFlat3.unity";

        [MenuItem("Disk Golf/Rebuild Prototype Flat 3 Scene")] public static void MenuBuild() => Build();

        public static void Build()
        {
            Directory.CreateDirectory(PrefabsDir);
            Directory.CreateDirectory(MaterialsDir);
            Directory.CreateDirectory("Assets/Scenes");

            EnsureTmpEssentials();

            EnsureTags(new[] { "Fairway", "Tee", "Basket", "Circle", "Rough" });

            var fairRgb = new Color(0.2f, 0.52f, 0.26f);
            var fairMat = SaveMaterialAsset("FairwayMat", fairRgb, $"{MaterialsDir}/FairwayMat.mat");
            var teeMat = SaveMaterialAsset("TeeMat", new Color(0.73f, 0.57f, 0.41f), $"{MaterialsDir}/TeeMat.mat");
            var metalMat = SaveMaterialAsset("BasketMat", new Color(0.46f, 0.49f, 0.53f), $"{MaterialsDir}/BasketMat.mat");

            var discOrange = SaveMaterialAsset("DiscOrange", new Color(0.92f, 0.42f, 0.06f), $"{MaterialsDir}/DiscOrange.mat");

            GameObject discPrefab = SaveDiscPrefab(discOrange);
            GameObject teePrefab = SaveTeePrefab(teeMat);
            GameObject basketPrefab = SaveBasketPrefab(metalMat);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            DirLight(out _);

            var fairPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            fairPlane.name = "FairwayPlane";
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

            HudTmpLabel(hudCanvas, new Vector2(36f, 48f), "Disc", 28f, out TextMeshProUGUI discUi);
            discUi.gameObject.name = "Disc";
            HudTmpLabel(hudCanvas, new Vector2(36f, 88f), "FLAT", 26f, out TextMeshProUGUI stanceUi);
            stanceUi.gameObject.name = "StanceLabel";

            var powerMb = gm.AddComponent<PowerMeterUI>();
            var heightMb = gm.AddComponent<HeightMeterUI>();

            AssignSerialized(controller, "powerMeter", powerMb);
            AssignSerialized(controller, "heightMeter", heightMb);

            var banner =
                HudTmpLabelRow(hudCanvas,
                    new Vector2(0f, -36f),
                    "IN THE CIRCLE",
                    40f,
                    TextAlignmentOptions.Center);

            banner.gameObject.name = "InTheCircleBanner";
            banner.gameObject.SetActive(false);
            AssignSerialized(controller, "inTheCircleBanner", banner.gameObject);

            var throwLabel = HudTmpLabelRow(hudCanvas, Vector2.zero, "200 FEET", 64f,
                TextAlignmentOptions.Center);
            var throwRt = throwLabel.rectTransform;
            throwRt.anchorMin = throwRt.anchorMax = new Vector2(0.5f, 0.5f);
            throwRt.anchoredPosition = Vector2.zero;
            throwRt.sizeDelta = new Vector2(900f, 140f);
            throwLabel.gameObject.name = "ThrowResultBanner";
            ThrowResultBannerUI.ApplyStyle(throwLabel);
            var throwBanner = throwLabel.gameObject.AddComponent<ThrowResultBannerUI>();
            AssignSerialized(throwBanner, "label", throwLabel);
            throwLabel.gameObject.SetActive(false);
            AssignSerialized(controller, "throwResultBanner", throwBanner);

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

            var autoSetup = gm.GetComponent<GreyboxAutoSetup>() ?? gm.AddComponent<GreyboxAutoSetup>();
            autoSetup.Apply();
            SceneHierarchy.Organize();
            SceneContentCleanup.Apply();

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
            var teeDot = ImageRect(markerLayer, course.WorldToMapAnchored(hole.TeePosition, mapRect),
                new Vector2(8f, 8f), Color.white).rectTransform;
            teeDot.name = "TeeDot";

            var basketDot = ImageRect(markerLayer, course.WorldToMapAnchored(hole.BasketPosition, mapRect),
                new Vector2(10f, 10f), new Color(1f, 0.55f, 0.25f)).rectTransform;
            basketDot.name = "BasketDot";

            var discDot = ImageRect(markerLayer, course.WorldToMapAnchored(discTf.position, mapRect),
                new Vector2(8f, 8f), Color.cyan).rectTransform;
            discDot.name = "DiscDot";

            var mini = host.AddComponent<MinimapUI>();
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
            var profiles = new[]
            {
                "Assets/Data/Discs/P2.asset",
                "Assets/Data/Discs/Buzzz.asset",
                "Assets/Data/Discs/Teebird.asset",
                "Assets/Data/Discs/Destroyer.asset",
            }.Select(AssetDatabase.LoadAssetAtPath<DiscProfile>).ToArray();

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
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? MaterialsDir);
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
            return SavePrefabAsset(disc, $"{PrefabsDir}/Disc.prefab");
        }

        static GameObject SaveTeePrefab(Material mat)
        {
            var tee = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tee.name = "TeePad";
            tee.tag = "Tee";
            tee.transform.localScale = new Vector3(2f, 0.08f, 2f);
            tee.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return SavePrefabAsset(tee, $"{PrefabsDir}/TeePad.prefab");
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
            return SavePrefabAsset(root, $"{PrefabsDir}/Basket.prefab");
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

                    "Assets/TextMesh Pro/Resources/TMP Settings.asset");

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

            Stripe("RoughBorder_L", Vector3.left * 95f + Vector3.up * 0.02f, new Vector3(6f, 1f, 24f));
            Stripe("RoughBorder_R", Vector3.right * 95f + Vector3.up * 0.02f, new Vector3(6f, 1f, 24f));
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
            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            if (font != null)
                tmp.font = font;
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

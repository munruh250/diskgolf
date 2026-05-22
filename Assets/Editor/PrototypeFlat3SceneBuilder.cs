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
    /// <summary>One-click greybox scaffold for Prototype Flat 3 (+ CLI batch).</summary>
    public static class PrototypeFlat3SceneBuilder
    {
        const string PrefabsDir = "Assets/Prefabs";

        const string ScenePath = "Assets/Scenes/PrototypeFlat3.unity";

        [MenuItem("Disk Golf/Rebuild Prototype Flat 3 Scene")] public static void MenuBuild() => Build();

        public static void Build()
        {
            Directory.CreateDirectory(PrefabsDir);
            Directory.CreateDirectory("Assets/Scenes");

            EnsureTmpEssentials();

            EnsureTags(new[] { "Fairway", "Tee", "Basket", "Circle", "Rough" });

            var fairRgb = new Color(0.2f, 0.52f, 0.26f);
            var fairMat = Mat("FairwayMat", fairRgb);
            var teeMat = Mat("TeeMat", new Color(0.73f, 0.57f, 0.41f));
            var metalMat = Mat("BasketMat", new Color(0.46f, 0.49f, 0.53f));

            var discOrange = Mat("DiscOrange", new Color(0.92f, 0.42f, 0.06f));

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

            var discTf = InstantiatePrefabIntoScene(discPrefab, teeTf.position + Vector3.up * 0.12f, Quaternion.identity);
            discTf.rotation = Quaternion.Euler(90f, 0f, 0f);

            SpawnCircleVisualizer(basketTf);

            RoughBands(fairRgb);

            var mainCam = MainCam(out CinemachineBrain _);

            EventSystemBootstrap();

            var gm = new GameObject("GameManager");

            var hole = gm.AddComponent<HoleSetup>();

            AssignSerialized(hole,
                "teePad", teeTf);
            AssignSerialized(hole,
                "basket", basketTf);

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

            var hudRt = HudCanvas(out _);

            HudTmpLabel(hudRt, new Vector2(0f, 130f), "REST --- ft", 34f, out TextMeshProUGUI restUi);
            HudTmpLabel(hudRt, new Vector2(0f, 94f), "Disc", 28f, out TextMeshProUGUI discUi);

            HudTmpLabel(hudRt, new Vector2(0f, 60f), "FLAT", 26f,
                out TextMeshProUGUI stanceUi);

            HudTmpLabel(hudRt,
                new Vector2(0f,
                    28f),
                "WIND --- mph",
                24f,

                out TextMeshProUGUI windUi);

            HudTmpLabelRow(hudRt, new Vector2(-240f, -110f),

                "POWER", 18f,

                TextAlignmentOptions.Top);

            _ = HudSliderUi(hudRt, new Vector2(-230f,

                -146f),

                out Slider powerSlider);

            HudTmpLabelRow(hudRt,

                new Vector2(232f,

                    -108f),

                "HEIGHT",

                18f,

                TextAlignmentOptions.Top);

            _ = HudSliderUi(hudRt,
                    new Vector2(260f,

                        -146f),

                    out Slider heightSlider);

            var zoneText =
                HudUnityText(hudRt,
                    new Vector2(258f,

                        -188f),

                    "NICE");

            ZoneTextStyle(zoneText);

            var powerMb = gm.AddComponent<PowerMeterUI>();

            AssignSerialized(powerMb, "slider", powerSlider);

            var heightMb = gm.AddComponent<HeightMeterUI>();

            AssignSerialized(heightMb, "slider", heightSlider);

            AssignSerialized(heightMb, "zoneLabel", zoneText);

            AssignSerialized(controller, "powerMeter", powerMb);

            AssignSerialized(controller, "heightMeter", heightMb);

            var banner =
                HudTmpLabelRow(hudRt,
                    new Vector2(0f,

                        -36f),

                    "IN THE CIRCLE",

                    40f,

                    TextAlignmentOptions.Center);

            banner.gameObject.SetActive(false);

            AssignSerialized(controller, "inTheCircleBanner", banner.gameObject);

            var hud = hudRt.gameObject.AddComponent<HUDController>();

            AssignSerialized(hud, "controller", controller);

            AssignSerialized(hud, "hole", hole);

            AssignSerialized(hud, "discTransform", discTf);

            AssignSerialized(hud, "restText", restUi);

            AssignSerialized(hud, "discText", discUi);

            AssignSerialized(hud, "stanceText", stanceUi);

            AssignSerialized(hud, "windText", windUi);

            SpawnMinimap(hudRt, hole, discTf);

            SpawnTrajectory(gm.transform, controller);

            var side =
                Vcam("SideSetupCam", mainCam.transform, discTf, basketTf, new Vector3(-6f, 2.8f, -4f), 0f);

            var top =
                Vcam("TopDownTrackCam", mainCam.transform, discTf, basketTf, new Vector3(0f, 30f, 0.6f), 80f);

            var lie = Vcam("LieZoomCam", mainCam.transform, discTf, basketTf, new Vector3(1.8f, 2.1f, -2.2f), 0f);

            var putt =
                Vcam("OverheadPuttCam", mainCam.transform, basketTf, basketTf, new Vector3(0f, 15f, 0.9f), 72f);

            side.gameObject.SetActive(true);
            top.gameObject.SetActive(false);
            lie.gameObject.SetActive(false);
            putt.gameObject.SetActive(false);

            var directorGo = new GameObject("CameraDirector");
            directorGo.transform.SetParent(gm.transform, false);

            var director = directorGo.AddComponent<CameraDirector>();
            AssignSerialized(director, "sideSetupCam", side);
            AssignSerialized(director, "topDownTrackCam", top);
            AssignSerialized(director, "lieZoomCam", lie);
            AssignSerialized(director, "overheadPuttCam", putt);
            AssignSerialized(director, "throwController", controller);

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

        static void SpawnTrajectory(Transform parent, ThrowController controller)
        {
            var go = new GameObject("TrajectoryPreview");
            go.transform.SetParent(parent, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.numCornerVertices = 2;
            lr.numCapVertices = 2;
            lr.widthMultiplier = 0.12f;
            lr.material = Mat("TrajectoryLine", Color.green);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            var preview = go.AddComponent<TrajectoryPreview>();
            AssignSerialized(preview, "controller", controller);
        }

        static void SpawnMinimap(RectTransform hudRoot, HoleSetup hole, Transform discTf)
        {
            var host = new GameObject("MinimapHost", typeof(RectTransform));
            var hostRt = host.GetComponent<RectTransform>();
            hostRt.SetParent(hudRoot, false);
            hostRt.anchorMin = new Vector2(1f, 1f);
            hostRt.anchorMax = new Vector2(1f, 1f);
            hostRt.pivot = new Vector2(1f, 1f);
            hostRt.anchoredPosition = new Vector2(-24f, -90f);
            hostRt.sizeDelta = new Vector2(200f, 200f);

            var panel =
                ImageRect(hostRt,

                    Vector2.zero,

                    Vector2.one * 40f,

                    new Color(0f,

                        0f,

                        0f,

                        0.35f));

            var panelRt = panel.rectTransform;
            panelRt.anchorMin =
                Vector2.zero;
            panelRt.anchorMax =
                Vector2.one;
            panelRt.pivot =
                new Vector2(0.5f,

                    0.5f);

            panelRt.offsetMin =
                Vector2.zero;
            panelRt.offsetMax =
                Vector2.zero;

            var teeDot = ImageRect(panel.rectTransform, new Vector2(-58f, -58f), new Vector2(10f, 10f), Color.white)
                .rectTransform;

            var basketDot = ImageRect(panel.rectTransform, new Vector2(58f, 58f), new Vector2(12f, 12f),
                new Color(1f, 0.5f, 0.3f)).rectTransform;

            var discDot = ImageRect(panel.rectTransform, new Vector2(-32f, -24f), new Vector2(10f, 10f), Color.cyan)
                .rectTransform;

            var mini = host.AddComponent<MinimapUI>();
            AssignSerialized(mini, "hole", hole);
            AssignSerialized(mini, "discTransform", discTf);
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
            disc.transform.localScale = new Vector3(0.28f, 0.02f, 0.28f);
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

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(root.transform, false);
            pole.transform.localScale = new Vector3(0.18f, 2.2f, 0.18f);
            pole.transform.localPosition = Vector3.up * 2.2f;
            pole.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            top.name = "TopRing";
            top.transform.SetParent(root.transform, false);
            top.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);
            top.transform.localPosition = Vector3.up * 4.3f;
            top.GetComponent<MeshRenderer>().sharedMaterial = mat;

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
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "CircleZone";
            sphere.tag = "Circle";
            sphere.transform.SetParent(basket, false);
            sphere.transform.localPosition = Vector3.zero;
            var radiusMeters = 33f * 0.3048f;
            sphere.transform.localScale = Vector3.one * (radiusMeters / 0.5f); // unity sphere radius baseline 0.5
            sphere.GetComponent<SphereCollider>().isTrigger = true;
            sphere.GetComponent<MeshRenderer>().sharedMaterial =
                Mat("CircleFill", new Color(0f, 0.55f, 0.92f));
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
            void Stripe(Vector3 offset, Vector3 euler)
            {
                var slab = GameObject.CreatePrimitive(PrimitiveType.Plane);
                slab.tag = "Rough";
                slab.name = "RoughBorder";
                slab.transform.position = Vector3.forward * (76.2f * 0.5f) + offset;
                slab.transform.rotation = Quaternion.Euler(euler);
                slab.transform.localScale = Vector3.one * 6f;
                slab.GetComponent<MeshRenderer>().sharedMaterial =
                    Mat("RoughMat",
                        fairwayBase * new Color(0.55f, 0.4f,
                            0.25f));
            }

            Stripe(Vector3.left * 80f + Vector3.up * 0.02f, new Vector3(0f, 90f,
                0f));
            Stripe(Vector3.right * 80f + Vector3.up * 0.02f, new Vector3(0f, -90f,
                0f));
        }

        static void DirLight(out GameObject go)
        {
            go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            go.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
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

            var hudRootGo = new GameObject("HudRoot");

            hudRootGo.transform.SetParent(holder.transform,
                false);

            var rt = hudRootGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

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

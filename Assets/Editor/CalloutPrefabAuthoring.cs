#if UNITY_EDITOR
using System;
using System.IO;
using DiskGolf.Gameplay;
using DiskGolf.UI;
using DiskGolf.UI.Callouts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.EditorTools
{
    public static class CalloutPrefabAuthoring
    {
        const string MenuRoot = "Disk Golf/HUD/";

        [MenuItem(MenuRoot + "Create Gameplay Callouts Prefab")]
        public static void CreatePrefab()
        {
            Directory.CreateDirectory(ProjectArtPaths.Prefabs.UiRoot);

            var root = new GameObject(GameplayCalloutHost.RootName, typeof(RectTransform), typeof(GameplayCalloutHost));
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            CreateSpriteChild(root.transform, "LieLandingBanner");
            CreateSpriteChild(root.transform, "OnTheGreenBanner");
            CreateTextChild(root.transform, "ThrowResultBanner", TextCalloutLayout.FeetLabel);
            CreateSpriteChild(root.transform, "SweetSpotBanner", typeof(SweetSpotBannerUI));
            CreateHoleComplete(root.transform);
            ThrowSummaryPanel.CreateForScene(rt);
            HoleCompleteCutsceneUI.CreateForScene(rt);

            var host = root.GetComponent<GameplayCalloutHost>();
            host.BindReferences();

            PrefabUtility.SaveAsPrefabAsset(root, ProjectArtPaths.Prefabs.GameplayCallouts);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[Disk Golf] Saved " + ProjectArtPaths.Prefabs.GameplayCallouts);
        }

        [MenuItem(MenuRoot + "Instantiate Gameplay Callouts In Scene")]
        public static void InstantiateInScene()
        {
            var hud = GameObject.Find(HudCanvasUtility.HudCanvasName)?.GetComponent<RectTransform>();
            if (hud == null)
            {
                Debug.LogError("[Disk Golf] GameplayHUD not found.");
                return;
            }

            if (hud.Find(GameplayCalloutHost.RootName) != null)
            {
                Debug.LogWarning("[Disk Golf] GameplayCallouts already present.");
            }
            else
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectArtPaths.Prefabs.GameplayCallouts);
                if (prefab == null)
                {
                    CreatePrefab();
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectArtPaths.Prefabs.GameplayCallouts);
                }

                if (prefab == null)
                {
                    Debug.LogError("[Disk Golf] Could not load GameplayCallouts prefab.");
                    return;
                }

                _ = PrefabUtility.InstantiatePrefab(prefab, hud);
            }

            RemoveLegacyDuplicateCallouts(hud);
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        }

        /// <summary>Removes pre-prefab callout objects left as direct GameplayHUD children.</summary>
        public static void RemoveLegacyDuplicateCallouts(RectTransform hud)
        {
            if (hud == null)
                return;

            var hostRoot = hud.Find(GameplayCalloutHost.RootName);
            RemoveLegacyCalloutChild(hud, hostRoot, "HoleCompleteBanner");
            RemoveLegacyCalloutChild(hud, hostRoot, "HoleCompleteCutscene");
            RemoveLegacyCalloutChild(hud, hostRoot, "ThrowSummaryBanner");
            RemoveLegacyCalloutChild(hud, hostRoot, "LieLandingBanner");
            RemoveLegacyCalloutChild(hud, hostRoot, "OnTheGreenBanner");
            RemoveLegacyCalloutChild(hud, hostRoot, "ThrowResultBanner");
            RemoveLegacyCalloutChild(hud, hostRoot, "SweetSpotBanner");
        }

        static void RemoveLegacyCalloutChild(RectTransform hud, Transform hostRoot, string name)
        {
            var legacy = hud.Find(name);
            if (legacy == null)
                return;

            if (hostRoot != null && legacy.IsChildOf(hostRoot))
                return;

            UnityEngine.Object.DestroyImmediate(legacy.gameObject);
        }

        static void CreateSpriteChild(Transform parent, string name, System.Type facadeType = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(SpriteCalloutBanner));
            go.transform.SetParent(parent, false);
            if (facadeType != null)
                go.AddComponent(facadeType);
            go.SetActive(false);
        }

        static void CreateTextChild(Transform parent, string name, TextCalloutLayout layout)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(TextCalloutBanner));
            go.transform.SetParent(parent, false);
            var banner = go.GetComponent<TextCalloutBanner>();
            banner.Layout = layout;
            banner.ResolveLabel();

            go.SetActive(false);
        }

        static void CreateHoleComplete(Transform parent)
        {
            var root = new GameObject(
                "HoleCompleteBanner",
                typeof(RectTransform),
                typeof(MultiSlotSpriteBanner),
                typeof(HoleCompleteBannerUI));
            root.transform.SetParent(parent, false);

            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(640f, 160f);

            foreach (HoleCompleteScoreKind kind in Enum.GetValues(typeof(HoleCompleteScoreKind)))
            {
                var slot = new GameObject(kind.ToString(), typeof(RectTransform), typeof(Image));
                slot.transform.SetParent(root.transform, false);

                var slotRt = slot.GetComponent<RectTransform>();
                slotRt.anchorMin = Vector2.zero;
                slotRt.anchorMax = Vector2.one;
                slotRt.offsetMin = Vector2.zero;
                slotRt.offsetMax = Vector2.zero;
            }

            root.GetComponent<MultiSlotSpriteBanner>().BindSceneReferences();
            root.GetComponent<MultiSlotSpriteBanner>().Hide();
        }
    }
}
#endif

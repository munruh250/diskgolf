#if UNITY_EDITOR
using DiskGolf.Gameplay;
using DiskGolf.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiskGolf.EditorTools
{
    /// <summary>Creates and upgrades HUD widgets in the open scene so they can be edited in Scene view.</summary>
    public static class HudSceneAuthoring
    {
        const string MenuRoot = "Disk Golf/HUD/";

        const string PrototypeScenePath = "Assets/Scenes/Prototype/PrototypeFlat3.unity";

        [MenuItem(MenuRoot + "Bake Missing Scene Widgets")]
        public static void BakeMissingSceneWidgets()
        {
            var hud = GameObject.Find("GameplayHUD")?.GetComponent<RectTransform>();
            if (hud == null)
            {
                Debug.LogError("[Disk Golf] GameplayHUD not found in the open scene.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(hud.gameObject, "Bake HUD widgets");

            bool addedBar = UpgradeBottomBar(hud);
            bool upgradedBar = !addedBar && hud.Find("NtmBottomBar/ArcButton") != null;
            bool upgradedAccuracy = UpgradeAccuracyMeter(hud);

            if (hud.GetComponent<TimingMeterHud>() == null)
                Undo.AddComponent<TimingMeterHud>(hud.gameObject);

            if (hud.GetComponent<HudLayoutSettings>() == null)
            {
                var settings = Undo.AddComponent<HudLayoutSettings>(hud.gameObject);
                settings.preserveManualLayout = true;
            }

            EnsurePowerMeterPortrait(hud);
            bool addedHoleBanner = EnsureScoreBanners(hud);
            WireThrowControllerBannerRefs(hud);
            HudTypography.ApplyToGameplayHud(hud);

            EditorUtility.SetDirty(hud.gameObject);
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);

            Debug.Log(
                "[Disk Golf] HUD bake complete. "
                + (addedBar ? "Created NtmBottomBar. " : upgradedBar ? "Upgraded NtmBottomBar with ARC section. " : "NtmBottomBar unchanged. ")
                + (upgradedAccuracy ? "Upgraded HeightMeter to ACCURACY layout. " : "Accuracy meter already current. ")
                + (addedHoleBanner ? "Created HoleCompleteBanner under GameplayHUD. " : "HoleCompleteBanner already present. ")
                + "Save the scene to keep changes.");
        }

        [MenuItem(MenuRoot + "Bake Score Banners")]
        public static void BakeScoreBanners()
        {
            var hud = GameObject.Find("GameplayHUD")?.GetComponent<RectTransform>();
            if (hud == null)
            {
                Debug.LogError("[Disk Golf] GameplayHUD not found in the open scene.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(hud.gameObject, "Bake score banners");

            if (hud.GetComponent<HudLayoutSettings>() == null)
            {
                var settings = Undo.AddComponent<HudLayoutSettings>(hud.gameObject);
                settings.preserveManualLayout = true;
            }

            EnsureScoreBanners(hud);
            WireThrowControllerBannerRefs(hud);
            EditorUtility.SetDirty(hud.gameObject);
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            Debug.Log(
                "[Disk Golf] Score banners baked under GameplayHUD. "
                + "HoleCompleteBanner and ThrowSummaryBanner are visible in the editor for layout.");
        }

        static bool EnsureScoreBanners(RectTransform hud)
        {
            bool added = hud.Find("HoleCompleteBanner") == null;
            HoleCompleteBannerUI.CreateForScene(hud);
            HoleCompleteCutsceneUI.CreateForScene(hud);
            OnTheGreenBannerUI.Ensure();
            ThrowSummaryBannerUI.CreateForScene(hud);
            return added;
        }

        static void WireThrowControllerBannerRefs(RectTransform hud)
        {
            var controller = Object.FindObjectOfType<DiskGolf.Core.ThrowController>();
            if (controller == null)
                return;

            var holeBanner = hud.GetComponentInChildren<HoleCompleteBannerUI>(true);
            var holeCutscene = hud.GetComponentInChildren<HoleCompleteCutsceneUI>(true);
            var onGreenBanner = hud.GetComponentInChildren<OnTheGreenBannerUI>(true);
            var throwSummaryBanner = hud.GetComponentInChildren<ThrowSummaryBannerUI>(true);

            var so = new SerializedObject(controller);
            if (holeBanner != null)
                so.FindProperty("holeCompleteBanner").objectReferenceValue = holeBanner;
            if (holeCutscene != null)
                so.FindProperty("holeCompleteCutscene").objectReferenceValue = holeCutscene;
            if (onGreenBanner != null)
                so.FindProperty("onTheGreenBanner").objectReferenceValue = onGreenBanner;
            if (throwSummaryBanner != null)
                so.FindProperty("throwSummaryBanner").objectReferenceValue = throwSummaryBanner;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        public static void BakePrototypeSceneHudBatch()
        {
            EditorSceneManager.OpenScene(PrototypeScenePath);
            BakeMissingSceneWidgets();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Disk Golf] Saved baked HUD widgets to PrototypeFlat3.");
        }

        static bool UpgradeBottomBar(RectTransform hud)
        {
            var bar = hud.Find("NtmBottomBar")?.GetComponent<NtmBottomBar>();
            if (bar == null)
            {
                NtmBottomBar.CreateForScene(hud);
                return true;
            }

            bool hadArc = bar.transform.Find("ArcButton") != null;
            bar.BakeSceneUpgrades();
            return !hadArc && bar.transform.Find("ArcButton") != null;
        }

        static bool UpgradeAccuracyMeter(RectTransform hud)
        {
            var meters = hud.Find("TimingMeters");
            var visual = HeightMeterVisual.FindMeterRoot(meters)?.GetComponent<HeightMeterVisual>();
            if (visual == null)
            {
                Debug.LogWarning("[Disk Golf] TimingMeters accuracy meter not found — accuracy meter was not upgraded.");
                return false;
            }

            bool wasCurrent = HeightMeterVisual.IsAccuracyLayout(visual.transform);
            visual.BakeAccuracyLayoutEditor();
            EditorUtility.SetDirty(visual.gameObject);
            return !wasCurrent;
        }

        static void EnsurePowerMeterPortrait(RectTransform hud)
        {
            var arcHub = hud.Find("TimingMeters/PowerMeter/Pivot/ArcHub") as RectTransform;
            if (arcHub == null)
            {
                Debug.LogWarning("[Disk Golf] PowerMeter ArcHub not found — player portrait was not created.");
                return;
            }

            PowerMeterPortraitWidget.EnsureInArcHub(arcHub);
            EditorUtility.SetDirty(arcHub.gameObject);
        }
    }
}
#endif

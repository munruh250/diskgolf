#if UNITY_EDITOR
using Cinemachine;
using DiskGolf.Camera;
using DiskGolf.Core;
using DiskGolf.UI;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.EditorTools
{
    /// <summary>Removes unused scene objects left over from earlier greybox iterations.</summary>
    public static class SceneContentCleanup
    {
        static readonly string[] UnusedVirtualCameraNames =
        {
            CameraRig.LegacyTopDownName,
            CameraRig.LegacyLieZoomName,
            CameraRig.LegacyOverheadPuttName,
        };

        static readonly string[] LegacyHudRootChildNames =
        {
            "TMP",
            "Wind",
            "Power",
            "Height",
            "UnityText",
        };

        public static void Apply()
        {
            RemoveUnusedVirtualCameras();
            RemoveLegacyHudObjects();
            FlattenHudRoot();
            MergeCameraDirectorOntoGameManager();
            ClearLegacyMeterReferences();
        }

        static void RemoveUnusedVirtualCameras()
        {
            var vcams = Object.FindObjectsByType<CinemachineVirtualCamera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var vcam in vcams)
            {
                foreach (var name in UnusedVirtualCameraNames)
                {
                    if (vcam.name != name)
                        continue;

                    Object.DestroyImmediate(vcam.gameObject);
                    break;
                }
            }
        }

        static void RemoveLegacyHudObjects()
        {
            var hudRoot = GameObject.Find("GameplayHUD")?.transform.Find("HudRoot");
            if (hudRoot != null)
            {
                foreach (var childName in LegacyHudRootChildNames)
                    DestroyChildIfExists(hudRoot, childName);

                foreach (var slider in hudRoot.GetComponentsInChildren<Slider>(true))
                    Object.DestroyImmediate(slider.gameObject);
            }

            var gameplayHud = GameObject.Find("GameplayHUD")?.transform;
            if (gameplayHud == null)
                return;

            DestroyChildIfExists(gameplayHud, "DiscHeight");
            DestroyChildIfExists(gameplayHud, "Score");
        }

        static void FlattenHudRoot()
        {
            var gameplayHud = GameObject.Find("GameplayHUD");
            if (gameplayHud == null)
                return;

            var hudRoot = gameplayHud.transform.Find("HudRoot");
            if (hudRoot == null)
                return;

            var hudController = hudRoot.GetComponent<HUDController>();
            if (hudController != null && gameplayHud.GetComponent<HUDController>() == null)
            {
                ComponentUtility.CopyComponent(hudController);
                ComponentUtility.PasteComponentAsNew(gameplayHud);
            }

            RenameChild(hudRoot, "TMPRow", "InTheCircleBanner");
            RenameChild(hudRoot, "TypeThrow", "StanceLabel");

            var children = new Transform[hudRoot.childCount];
            for (int i = 0; i < hudRoot.childCount; i++)
                children[i] = hudRoot.GetChild(i);

            foreach (var child in children)
                child.SetParent(gameplayHud.transform, false);

            Object.DestroyImmediate(hudRoot.gameObject);
        }

        static void MergeCameraDirectorOntoGameManager()
        {
            var gameManager = GameObject.Find("GameManager");
            var directorGo = GameObject.Find("CameraDirector");
            if (gameManager == null || directorGo == null)
                return;

            var director = directorGo.GetComponent<CameraDirector>();
            if (director == null)
                return;

            if (gameManager.GetComponent<CameraDirector>() == null)
            {
                ComponentUtility.CopyComponent(director);
                ComponentUtility.PasteComponentAsNew(gameManager);
            }

            Object.DestroyImmediate(directorGo);
        }

        static void ClearLegacyMeterReferences()
        {
            var gameManager = GameObject.Find("GameManager");
            if (gameManager == null)
                return;

            foreach (var power in gameManager.GetComponents<PowerMeterUI>())
            {
                var so = new SerializedObject(power);
                var slider = so.FindProperty("slider");
                if (slider != null)
                {
                    slider.objectReferenceValue = null;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            foreach (var height in gameManager.GetComponents<HeightMeterUI>())
            {
                var so = new SerializedObject(height);
                var slider = so.FindProperty("slider");
                if (slider != null)
                    slider.objectReferenceValue = null;

                var zoneLabel = so.FindProperty("zoneLabel");
                if (zoneLabel != null)
                    zoneLabel.objectReferenceValue = null;

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var throwController = gameManager.GetComponent<ThrowController>();
            var banner = GameObject.Find("InTheCircleBanner");
            if (throwController != null && banner != null)
            {
                var so = new SerializedObject(throwController);
                var prop = so.FindProperty("inTheCircleBanner");
                if (prop != null)
                {
                    prop.objectReferenceValue = banner;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        static void RenameChild(Transform parent, string oldName, string newName)
        {
            var child = parent.Find(oldName);
            if (child != null)
                child.name = newName;
        }

        static void DestroyChildIfExists(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }
    }
}
#endif

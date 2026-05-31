using DiskGolf.Camera;
using UnityEngine;

namespace DiskGolf.Gameplay
{
    /// <summary>Top-level scene sections for editor organization.</summary>
    public static class SceneHierarchy
    {
        public const string DiscMechanics = "DiscMechanics";
        public const string GameplayHud = "GameplayHUD";
        public const string CourseElements = "CourseElements";
        public const string Camera = "Camera";
        public const string PlayerThrower = "PlayerThrower";

        public static void Organize()
        {
            var discSection = EnsureRoot(DiscMechanics);
            var courseSection = EnsureCourseSection();
            var cameraSection = EnsureRoot(Camera);
            var playerSection = EnsureRoot(PlayerThrower);

            ReparentIfFound("Disc", discSection);
            ReparentIfFound("Main Camera", cameraSection);
            ReparentIfFound(CameraRig.SideSetupName, cameraSection);
            ReparentIfFound(CameraRig.FlightChaseName, cameraSection);
            ReparentIfFound(CameraRig.TopDownName, cameraSection);
            ReparentIfFound(CameraRig.LieZoomName, cameraSection);
            ReparentIfFound(CameraRig.OverheadPuttName, cameraSection);
            ReparentIfFound("CameraDirector", cameraSection);
            ReparentIfFound("Thrower", playerSection);
            ReparentIfFound(CameraRig.AimPointName, playerSection);

            RenameLegacyHudObjects();
            SortRootObjects();
        }

        static Transform EnsureCourseSection()
        {
            var legacy = GameObject.Find("CourseRoot");
            if (legacy != null)
                legacy.name = CourseElements;

            return EnsureRoot(CourseElements);
        }

        static Transform EnsureRoot(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null)
                return existing.transform;

            return new GameObject(name).transform;
        }

        static void ReparentIfFound(string objectName, Transform parent)
        {
            if (parent == null)
                return;

            var go = GameObject.Find(objectName);
            if (go == null || go.transform.parent == parent)
                return;

            if (go.transform == parent)
                return;

            go.transform.SetParent(parent, true);
        }

        static void RenameLegacyHudObjects()
        {
            var hud = GameObject.Find(GameplayHud);
            if (hud == null)
                return;

            RenameChild(hud.transform, "NtmRestDrive", "RestDrive");
            RenameChild(hud.transform, "NtmWindWidget", "WindWidget");
            RenameChild(hud.transform, "NtmHoleInfo", "HoleInfo");
            RenameChild(hud.transform, "NtmPowerMeter", "PowerMeter");
            RenameChild(hud.transform, "NtmHeightMeter", "HeightMeter");

            var meters = hud.transform.Find("TimingMeters");
            if (meters != null)
            {
                RenameChild(meters, "NtmPowerMeter", "PowerMeter");
                RenameChild(meters, "NtmHeightMeter", "HeightMeter");
            }
        }

        static void RenameChild(Transform parent, string oldName, string newName)
        {
            var child = parent.Find(oldName);
            if (child != null)
                child.name = newName;
        }

        static void SortRootObjects()
        {
            string[] order =
            {
                DiscMechanics,
                PlayerThrower,
                CourseElements,
                GameplayHud,
                Camera,
                "GameManager",
                "Directional Light",
                "EventSystem",
            };

            for (int i = 0; i < order.Length; i++)
            {
                var go = GameObject.Find(order[i]);
                if (go != null)
                    go.transform.SetSiblingIndex(i);
            }
        }
    }
}

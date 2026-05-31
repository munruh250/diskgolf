using Cinemachine;
using DiskGolf.Camera;
using DiskGolf.Core;
using DiskGolf.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.Gameplay
{
    /// <summary>
    /// Idempotent greybox fixes applied in Edit Mode and Play Mode when the scene loads.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-200)]
    public sealed class GreyboxAutoSetup : MonoBehaviour
    {
        const string DiscName = "Disc";

        [SerializeField] HoleSetup holeSetup;

        [SerializeField] DiscFlightPresenter flightPresenter;

        [Header("Editor workflow")]
        [Tooltip("Run full greybox setup when the scene loads in Edit Mode. Off by default so Scene-view moves are kept.")]
        [SerializeField] bool applyInEditMode;

        [Tooltip("Reposition thrower and disc at the tee when greybox setup runs in Edit Mode.")]
        [SerializeField] bool repositionGameplayOnApply;

        void OnEnable()
        {
            SceneLightingBootstrap.Apply();
            EnsureEditorConfigComponents();

#if UNITY_EDITOR
            if (!Application.isPlaying)
                SceneEditorHooks.Instance?.RunContentCleanup();
#endif

            if (Application.isPlaying)
            {
                ApplyPlayMode();
                return;
            }

            if (applyInEditMode)
                ApplyEditMode();
        }

        [ContextMenu("Apply Greybox Setup Now")]
        public void ApplyGreyboxNow()
        {
            SceneLightingBootstrap.Apply();
            EnsureEditorConfigComponents();

            if (Application.isPlaying)
                ApplyPlayMode();
            else
                ApplyEditMode();
        }

        void Reset()
        {
            holeSetup = GetComponent<HoleSetup>();
            flightPresenter = GetComponent<DiscFlightPresenter>();
        }

        static void EnsureEditorConfigComponents()
        {
            HudLayoutSettings.EnsureOnHudRoot();

            var director = Object.FindObjectOfType<CameraDirector>();
            if (director != null && director.GetComponent<FlightCameraSettings>() == null)
                director.gameObject.AddComponent<FlightCameraSettings>();
        }

        void ApplyPlayMode()
        {
            if (holeSetup == null)
                holeSetup = GetComponent<HoleSetup>();

            if (flightPresenter == null)
                flightPresenter = GetComponent<DiscFlightPresenter>();

            var disc = ResolveDiscTransform();
            var basket = ResolveBasketTransform();

            ApplyPlayModeCamera(disc, basket);
            TimingMeterHud.Ensure();
            HudLayout.Apply();
            SceneHierarchy.Organize();
        }

        void ApplyEditMode()
        {
            SceneEditorHooks.Instance?.RunContentCleanup();
            ApplyCoreGreybox(repositionGameplayOnApply);
        }

        public void Apply()
        {
            SceneLightingBootstrap.Apply();
            EnsureEditorConfigComponents();

            if (Application.isPlaying)
                ApplyPlayMode();
            else
                ApplyEditMode();
        }

        void ApplyCoreGreybox(bool repositionGameplay)
        {
            if (holeSetup == null)
                holeSetup = GetComponent<HoleSetup>();

            if (flightPresenter == null)
                flightPresenter = GetComponent<DiscFlightPresenter>();

#if UNITY_EDITOR
            CameraRig.RemoveDuplicateVcams();
#endif

            var disc = ResolveDiscTransform();
            var basket = ResolveBasketTransform();
            var tee = ResolveTeeTransform();

            if (basket != null)
            {
                FixCircleZone(basket);
                BasketVisual.Ensure(basket);
            }

            if (disc != null)
                FixDiscVisual(disc);

            var thrower = EnsureThrower(tee, basket, repositionGameplay);
            if (holeSetup != null && thrower != null)
                holeSetup.BindThrower(thrower);

            var aimPoint = CameraRig.EnsureAimPoint(thrower, basket);
            FixSideCamera(thrower, aimPoint);
            EnsureFlightChaseCam(disc, basket);
            EnsureCourseHierarchy();
            HudLayout.Apply();
            WireCameraDirector();
            SceneHierarchy.Organize();

            if (repositionGameplay && disc != null && holeSetup != null)
            {
                holeSetup.PositionThrowerAtTee();
                disc.SetPositionAndRotation(holeSetup.DiscHoldPosition, holeSetup.DiscHoldRotation);
                flightPresenter?.SetPositionAndRotation(holeSetup.DiscHoldPosition, holeSetup.DiscHoldRotation);
            }
        }

        static void ApplyPlayModeCamera(Transform disc, Transform basket)
        {
            CameraRig.RemoveDuplicateVcams();
            EnsureCourseHierarchy();

            var thrower = GameObject.Find("Thrower")?.transform;
            if (thrower == null)
                return;

            var aimPoint = CameraRig.EnsureAimPoint(thrower, basket);
            FixSideCamera(thrower, aimPoint);
            EnsureFlightChaseCam(disc, basket);
            WireCameraDirector();
        }

        static void EnsureCourseHierarchy()
        {
            var course = CourseLayout.EnsureInScene();
            course?.EnsureFoliageAndTrees();
            UpgradeLegacyMinimap();

            EnsureDiscSelectUi();

            var minimap = Object.FindObjectOfType<MinimapUI>();
            minimap?.RefreshCapture();
        }

        static void UpgradeLegacyMinimap()
        {
            var host = GameObject.Find("MinimapHost");
            if (host == null)
                return;

            var mapPanel = host.transform.Find("MapPanel");
            if (mapPanel == null && host.transform.childCount > 0)
            {
                mapPanel = host.transform.GetChild(0);
                mapPanel.name = "MapPanel";
            }

            if (mapPanel == null)
                return;

            var markerLayer = mapPanel.Find("MarkerLayer");
            if (markerLayer == null)
            {
                var layerGo = new GameObject("MarkerLayer", typeof(RectTransform));
                var layerRt = layerGo.GetComponent<RectTransform>();
                layerRt.SetParent(mapPanel, false);
                layerRt.anchorMin = Vector2.zero;
                layerRt.anchorMax = Vector2.one;
                layerRt.offsetMin = Vector2.zero;
                layerRt.offsetMax = Vector2.zero;
                markerLayer = layerRt;
            }

            foreach (var image in mapPanel.GetComponentsInChildren<Image>(true))
            {
                if (image.GetComponent<RawImage>() != null)
                    continue;

                var rt = image.rectTransform;
                if (rt.parent == markerLayer)
                    continue;

                rt.SetParent(markerLayer, false);
                var c = image.color;

                if (rt.name is "TeeDot" or "BasketDot" or "DiscDot")
                    continue;

                if (c.g > 0.9f && c.r > 0.9f)
                    rt.name = "TeeDot";
                else if (c.g > 0.45f && c.r > 0.9f)
                    rt.name = "BasketDot";
                else if (c.g > 0.8f && c.b > 0.8f)
                    rt.name = "DiscDot";
            }

            var legacyImage = mapPanel.GetComponent<Image>();
            if (legacyImage != null)
            {
                if (Application.isPlaying)
                    Destroy(legacyImage);
                else
                    DestroyImmediate(legacyImage);
            }

#if UNITY_EDITOR
            var mini = host.GetComponent<MinimapUI>();
            if (mini != null)
            {
                var so = new UnityEditor.SerializedObject(mini);
                AssignRef(so, "markerLayer", markerLayer as RectTransform);
                AssignRef(so, "teeDot", markerLayer.Find("TeeDot") as RectTransform);
                AssignRef(so, "basketDot", markerLayer.Find("BasketDot") as RectTransform);
                AssignRef(so, "discDot", markerLayer.Find("DiscDot") as RectTransform);
                AssignRef(so, "course", Object.FindObjectOfType<CourseLayout>());
                so.ApplyModifiedPropertiesWithoutUndo();
            }
#endif
        }

        Transform ResolveDiscTransform()
        {
            var byName = GameObject.Find(DiscName);
            return byName != null ? byName.transform : null;
        }

        Transform ResolveBasketTransform()
        {
            var basketGo = GameObject.FindGameObjectWithTag("Basket");
            return basketGo != null ? basketGo.transform : null;
        }

        Transform ResolveTeeTransform()
        {
            var teeGo = GameObject.FindGameObjectWithTag("Tee");
            return teeGo != null ? teeGo.transform : null;
        }

        static void FixDiscVisual(Transform disc)
        {
            disc.localScale = new Vector3(
                GreyboxScale.DiscDiameterM,
                GreyboxScale.DiscThicknessM,
                GreyboxScale.DiscDiameterM);

            var renderer = disc.GetComponent<Renderer>();
            if (renderer == null)
                return;

            SetColor(renderer, GreyboxScale.DiscColor);
        }

        static void FixCircleZone(Transform basket)
        {
            var zone = basket.Find("CircleZone");
            if (zone == null)
                return;

            var renderer = zone.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = false;

            float radiusMeters = 33f * 0.3048f;
            zone.localPosition = new Vector3(0f, 0.03f, 0f);
            zone.localScale = new Vector3(radiusMeters * 2f, 0.02f, radiusMeters * 2f);

            var sphereCol = zone.GetComponent<SphereCollider>();
            if (sphereCol != null)
                sphereCol.isTrigger = true;
        }

        static Transform EnsureThrower(Transform tee, Transform basket, bool reposition = true)
        {
            if (tee == null || basket == null)
                return GameObject.Find("Thrower")?.transform;

            var aim = (basket.position - tee.position).normalized;
            var rot = Quaternion.LookRotation(aim, Vector3.up);
            var pos = tee.position + rot * Vector3.back * 0.55f;

            var existing = GameObject.Find("Thrower");
            if (existing != null)
            {
                var visual = existing.GetComponent<ThrowerVisual>() ?? existing.AddComponent<ThrowerVisual>();

                if (reposition)
                    existing.transform.SetPositionAndRotation(pos, rot);

                if (existing.GetComponentInChildren<SpriteRenderer>() == null)
                    visual.RebuildAsSprite();
                else
                    visual.ApplySpriteLayout();

                return existing.transform;
            }

            return ThrowerVisual.Build(pos, rot).transform;
        }

        static void FixSideCamera(Transform thrower, Transform aimPoint)
        {
            if (thrower == null)
                return;

            var side = CameraRig.FindSideSetupCam();
            if (side == null)
            {
                if (Application.isPlaying)
                    return;

                var parent = GameObject.Find(SceneHierarchy.Camera)?.transform
                    ?? GameObject.Find("Main Camera")?.transform;
                var go = new GameObject(CameraRig.SideSetupName);
                if (parent != null)
                    go.transform.SetParent(parent, false);

                side = go.AddComponent<CinemachineVirtualCamera>();
            }

            CameraRig.ConfigureSideThrowCam(side, thrower, aimPoint ?? thrower);
        }

        static void EnsureFlightChaseCam(Transform disc, Transform basket)
        {
            if (disc == null)
                return;

            var chase = CameraRig.FindFlightChaseCam();
            if (chase == null)
            {
                if (Application.isPlaying)
                    return;

                var parent = GameObject.Find(SceneHierarchy.Camera)?.transform
                    ?? GameObject.Find("Main Camera")?.transform;
                var chaseGo = new GameObject(CameraRig.FlightChaseName);
                if (parent != null)
                    chaseGo.transform.SetParent(parent, false);

                chase = chaseGo.AddComponent<CinemachineVirtualCamera>();
            }

            CameraRig.ConfigureFlightChaseCam(chase, disc, Vector3.forward);
        }

        static void WireCameraDirector()
        {
            var gameManager = GameObject.Find("GameManager");
            var director = gameManager != null
                ? gameManager.GetComponent<CameraDirector>()
                : null;
            director ??= Object.FindObjectOfType<CameraDirector>();
            if (director == null)
                return;

#if UNITY_EDITOR
            var so = new UnityEditor.SerializedObject(director);
            AssignRef(so, "sideSetupCam", CameraRig.FindSideSetupCam());
            AssignRef(so, "flightChaseCam", CameraRig.FindFlightChaseCam());
            AssignRef(so, "throwController", gameManager != null ? gameManager.GetComponent<ThrowController>() : null);
            AssignRef(so, "flightPresenter", gameManager != null ? gameManager.GetComponent<DiscFlightPresenter>() : null);
            AssignRef(so, "hole", gameManager != null ? gameManager.GetComponent<HoleSetup>() : null);
            so.ApplyModifiedPropertiesWithoutUndo();
#else
            _ = director;
#endif
        }

#if UNITY_EDITOR
        static void EnsureDiscSelectUi()
        {
            var hud = GameObject.Find("GameplayHUD");
            if (hud == null)
                return;

            if (hud.GetComponent<DiscSelectUI>() == null)
                hud.AddComponent<DiscSelectUI>();
        }

        static void AssignRef(UnityEditor.SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p != null)
                p.objectReferenceValue = value;
        }
#endif

        static void SetColor(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;

            if (renderer.sharedMaterial != null)
            {
                renderer.sharedMaterial.color = color;
                return;
            }

            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            renderer.sharedMaterial = mat;
        }
    }
}

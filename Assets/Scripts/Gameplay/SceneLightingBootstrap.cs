using UnityEngine;
using UnityEngine.Rendering;

namespace DiskGolf.Gameplay
{
    /// <summary>Outdoor sun + ambient so the disc and course cast readable shadows.</summary>
    public static class SceneLightingBootstrap
    {
        const string SunName = "Directional Light";

        public static void Apply()
        {
            ApplyRenderSettings();
            ApplySunLight(FindSun());
            ApplyMainCameraShadows();
            EnsureShadowDistance();
        }

        static Light FindSun()
        {
            var existing = GameObject.Find(SunName)?.GetComponent<Light>();
            if (existing != null)
                return existing;

            var go = new GameObject(SunName);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            return light;
        }

        static void ApplyRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.72f, 0.86f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.52f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.3f, 0.2f);
            RenderSettings.ambientIntensity = 1.05f;
            RenderSettings.reflectionIntensity = 0.65f;
        }

        static void ApplySunLight(Light sun)
        {
            if (sun == null)
                return;

            if (RenderSettings.sun == null)
                RenderSettings.sun = sun;

            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.9f;
            sun.shadowBias = 0.038f;
            sun.shadowNormalBias = 0.22f;

            // Low afternoon sun — throws disc shadow forward onto the fairway during flight.
            sun.transform.rotation = Quaternion.Euler(50f, -34f, 0f);
        }

        static void ApplyMainCameraShadows()
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
                return;

            cam.clearFlags = CameraClearFlags.Skybox;
        }

        static void EnsureShadowDistance()
        {
            if (QualitySettings.shadows == ShadowQuality.Disable)
                QualitySettings.shadows = ShadowQuality.All;

            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 120f);
        }
    }
}

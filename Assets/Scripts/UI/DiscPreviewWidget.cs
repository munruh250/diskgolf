using DiskGolf.Disc;
using DiskGolf.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Renders the active disc prefab into the power-meter hub circle (NTM ball window).</summary>
    public sealed class DiscPreviewWidget : MonoBehaviour
    {
        const int PreviewLayer = 31;

        const int TextureSize = 128;

        static readonly Color Backdrop = new(0.1f, 0.34f, 0.12f, 1f);

        [SerializeField] RawImage display;

        [SerializeField] DiscBag bag;

        UnityEngine.Camera _camera;

        RenderTexture _target;

        Transform _stage;

        GameObject _discInstance;

        DiscProfile _lastDisc;

        public static DiscPreviewWidget Ensure(RectTransform hubParent, DiscBag bagRef)
        {
            if (hubParent == null)
                return null;

            var existing = hubParent.GetComponentInChildren<DiscPreviewWidget>(true);
            if (existing != null)
            {
                existing.bag = bagRef;
                existing.EnsureBuilt(hubParent);
                return existing;
            }

            var go = new GameObject("DiscPreview", typeof(RectTransform), typeof(RawImage));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(hubParent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = hubParent.sizeDelta * 0.92f;

            var widget = go.AddComponent<DiscPreviewWidget>();
            widget.display = go.GetComponent<RawImage>();
            widget.display.raycastTarget = false;
            widget.bag = bagRef;
            widget.EnsureBuilt(hubParent);
            return widget;
        }

        void OnDestroy()
        {
            if (_target != null)
            {
                _target.Release();
                _target = null;
            }

            if (_camera != null)
                Destroy(_camera.gameObject);

            if (_stage != null)
                Destroy(_stage.gameObject);
        }

        void LateUpdate()
        {
            var active = bag != null ? bag.Active : null;
            if (active != _lastDisc)
            {
                _lastDisc = active;
                RebuildDiscMesh();
            }

            if (_camera != null && _discInstance != null)
                _camera.Render();
        }

        public void EnsureBuilt(RectTransform hubParent)
        {
            bag ??= FindObjectOfType<DiscBag>();

            if (_stage == null)
                BuildStage();

            if (display != null && _target != null)
                display.texture = _target;

            RebuildDiscMesh();
        }

        void BuildStage()
        {
            _target = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32);
            _target.antiAliasing = 2;

            var stageGo = new GameObject("DiscPreviewStage");
            stageGo.hideFlags = HideFlags.HideAndDontSave;
            _stage = stageGo.transform;
            _stage.position = new Vector3(1000f, 1000f, 1000f);

            var camGo = new GameObject("DiscPreviewCamera");
            camGo.hideFlags = HideFlags.HideAndDontSave;
            camGo.transform.SetParent(_stage, false);
            _camera = camGo.AddComponent<UnityEngine.Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Backdrop;
            _camera.cullingMask = 1 << PreviewLayer;
            _camera.orthographic = true;
            _camera.orthographicSize = 0.14f;
            _camera.nearClipPlane = 0.01f;
            _camera.farClipPlane = 4f;
            _camera.targetTexture = _target;
            _camera.transform.localPosition = new Vector3(0.05f, 0.35f, -0.55f);
            _camera.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
        }

        void RebuildDiscMesh()
        {
            if (_discInstance != null)
            {
                Destroy(_discInstance);
                _discInstance = null;
            }

            var source = ResolveDiscSource();
            if (source == null || _stage == null)
                return;

            _discInstance = Instantiate(source, _stage);
            _discInstance.name = "PreviewDisc";
            SetLayerRecursively(_discInstance, PreviewLayer);
            _discInstance.transform.localPosition = Vector3.zero;
            _discInstance.transform.localRotation = Quaternion.Euler(12f, 35f, 0f);
            _discInstance.transform.localScale = Vector3.one;
        }

        static GameObject ResolveDiscSource()
        {
            var presenter = FindObjectOfType<DiscFlightPresenter>();
            if (presenter?.DiscTransform != null)
                return presenter.DiscTransform.gameObject;

#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                ProjectArtPaths.Prefabs.Disc);
#else
            return null;
#endif
        }

        static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;

            foreach (Transform child in go.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}

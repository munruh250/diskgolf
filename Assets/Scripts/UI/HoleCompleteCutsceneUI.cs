using DiskGolf.Core;
using DiskGolf.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Full-screen hole summary cutscene with pose, background, and score banner.</summary>
    public sealed class HoleCompleteCutsceneUI : MonoBehaviour
    {
        const string HudCanvasName = "GameplayHUD";
        const string RootName = "HoleCompleteCutscene";

        [SerializeField] Image background;

        [SerializeField] Image characterPose;

        [SerializeField] Image scoreBanner;

        [SerializeField] float displaySeconds = 3f;

        float _activeDisplaySeconds = -1f;

        public float DisplaySeconds => _activeDisplaySeconds > 0f ? _activeDisplaySeconds : displaySeconds;

        public static HoleCompleteCutsceneUI Ensure()
        {
            var canvas = FindHudCanvas();
            if (canvas == null)
                return null;

            var existing = canvas.Find(RootName)?.GetComponent<HoleCompleteCutsceneUI>();
            if (existing != null)
            {
                existing.BindReferences();
                if (Application.isPlaying)
                    existing.Hide();

                return existing;
            }

            if (SceneHudAuthoring.IsActive)
            {
                Debug.LogWarning(
                    "[Disk Golf] HoleCompleteCutscene not found under GameplayHUD. "
                    + "Use Disk Golf → HUD → Bake Score Banners.");
                return null;
            }

            return Build(canvas);
        }

        public static HoleCompleteCutsceneUI CreateForScene(RectTransform hud)
        {
            if (hud == null)
                return null;

            var existing = hud.Find(RootName)?.GetComponent<HoleCompleteCutsceneUI>();
            if (existing != null)
            {
                existing.BindReferences();
                return existing;
            }

            var cutscene = Build(hud);
            cutscene.ApplyEditorPreview();
            return cutscene;
        }

        void Awake()
        {
            BindReferences();
            if (Application.isPlaying)
                Hide();
        }

        public void BindReferences()
        {
            var root = transform as RectTransform;
            if (root != null)
                ConfigureRootRect(root);

            background ??= transform.Find("Background")?.GetComponent<Image>();
            characterPose ??= transform.Find("CharacterPose")?.GetComponent<Image>();
            scoreBanner ??= transform.Find("ScoreBanner")?.GetComponent<Image>();
        }

        static HoleCompleteCutsceneUI Build(RectTransform canvas)
        {
            var rootGo = new GameObject(RootName, typeof(RectTransform));
            var root = rootGo.GetComponent<RectTransform>();
            root.SetParent(canvas, false);
            ConfigureRootRect(root);

            var background = CreateImageLayer(root, "Background", ConfigureBackgroundRect);
            var character = CreateImageLayer(root, "CharacterPose", ConfigureCharacterRect);
            var banner = CreateImageLayer(root, "ScoreBanner", ConfigureScoreBannerRect);

            background.preserveAspect = false;
            character.preserveAspect = true;
            banner.preserveAspect = true;

            var cutscene = rootGo.AddComponent<HoleCompleteCutsceneUI>();
            cutscene.background = background;
            cutscene.characterPose = character;
            cutscene.scoreBanner = banner;
            rootGo.SetActive(false);
            return cutscene;
        }

        static Image CreateImageLayer(
            RectTransform parent,
            string name,
            System.Action<RectTransform> configure)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            configure(rt);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.white;
            return image;
        }

        static void ConfigureRootRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void ConfigureBackgroundRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void ConfigureCharacterRect(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, -24f);
            rt.sizeDelta = new Vector2(920f, 980f);
        }

        static void ConfigureScoreBannerRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.54f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(760f, 220f);
        }

        static RectTransform FindHudCanvas()
        {
            var hud = GameObject.Find(HudCanvasName);
            return hud != null ? hud.GetComponent<RectTransform>() : null;
        }

        public void Show(int strokes, int par, float overrideDisplaySeconds = -1f)
        {
            _activeDisplaySeconds = overrideDisplaySeconds;
            BindReferences();

            if (background != null)
            {
                background.sprite = RuntimeArt.LoadHoleSummaryBackground();
                background.enabled = background.sprite != null;
            }

            if (characterPose != null)
            {
                bool happy = HoleScore.RelativeToPar(strokes, par) <= 0;
                characterPose.sprite = happy
                    ? RuntimeArt.LoadHappyCharacterPose()
                    : RuntimeArt.LoadSadCharacterPose();
                characterPose.enabled = characterPose.sprite != null;
            }

            if (scoreBanner != null)
            {
                var kind = ScoreBannerSprites.ResolveKind(strokes, par);
                scoreBanner.sprite = ScoreBannerSprites.LoadKind(kind);
                scoreBanner.enabled = scoreBanner.sprite != null;
            }

            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _activeDisplaySeconds = -1f;
            gameObject.SetActive(false);
        }

        void ApplyEditorPreview() => Show(3, 3);
    }
}

using DiskGolf.Core;
using DiskGolf.Gameplay;
using DiskGolf.UI.Callouts;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Full-screen hole summary cutscene with pose, background, and score banner.</summary>
    public sealed class HoleCompleteCutsceneUI : MonoBehaviour
    {
        const string RootName = "HoleCompleteCutscene";

        [SerializeField] Image background;

        [SerializeField] Image characterPose;

        [SerializeField] Image scoreBanner;

        [SerializeField] float displaySeconds = 3f;

        float _activeDisplaySeconds = -1f;

        public float DisplaySeconds => _activeDisplaySeconds > 0f ? _activeDisplaySeconds : displaySeconds;

        public static HoleCompleteCutsceneUI Ensure()
        {
            var canvas = HudCanvasUtility.FindHudCanvas();
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
                existing.ApplyLayout();
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

            var background = CreateImageLayer(root, "Background", HoleCompleteCutsceneLayout.ApplyBackgroundRect);
            var character = CreateImageLayer(root, "CharacterPose", HoleCompleteCutsceneLayout.ApplyCharacterRect);
            var banner = CreateImageLayer(root, "ScoreBanner", HoleCompleteCutsceneLayout.ApplyScoreBannerRect);

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

        public void Show(int strokes, int par, float overrideDisplaySeconds = -1f)
        {
            _activeDisplaySeconds = overrideDisplaySeconds;
            BindReferences();
            ApplyLayout();

            if (background != null)
            {
                background.sprite = RuntimeArt.LoadHoleSummaryBackground();
                background.preserveAspect = false;
                background.enabled = background.sprite != null;
            }

            if (characterPose != null)
            {
                bool happy = HoleScore.RelativeToPar(strokes, par) <= 0;
                characterPose.sprite = happy
                    ? RuntimeArt.LoadHappyCharacterPose()
                    : RuntimeArt.LoadSadCharacterPose();
                characterPose.preserveAspect = true;
                characterPose.enabled = characterPose.sprite != null;
            }

            if (scoreBanner != null)
            {
                var kind = ScoreBannerSprites.ResolveKind(strokes, par);
                scoreBanner.sprite = ScoreBannerSprites.LoadKind(kind);
                scoreBanner.preserveAspect = true;
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

        void ApplyLayout()
        {
            if (background != null)
                HoleCompleteCutsceneLayout.ApplyBackgroundRect(background.rectTransform);

            if (characterPose != null)
                HoleCompleteCutsceneLayout.ApplyCharacterRect(characterPose.rectTransform);

            if (scoreBanner != null)
                HoleCompleteCutsceneLayout.ApplyScoreBannerRect(scoreBanner.rectTransform);

            if (background != null)
                background.transform.SetSiblingIndex(0);

            if (characterPose != null)
                characterPose.transform.SetSiblingIndex(1);

            if (scoreBanner != null)
                scoreBanner.transform.SetSiblingIndex(2);
        }

        void ApplyEditorPreview() => Show(3, 4);
    }
}

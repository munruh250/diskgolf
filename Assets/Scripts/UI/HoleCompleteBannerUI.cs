using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Centered score-banner overlay when the disc finishes in the basket.</summary>
    public sealed class HoleCompleteBannerUI : MonoBehaviour
    {
        const string HudCanvasName = "GameplayHUD";
        const string BannerName = "HoleCompleteBanner";

        [SerializeField] Image holeInOne;

        [SerializeField] Image eagle;

        [SerializeField] Image birdie;

        [SerializeField] Image par;

        [SerializeField] Image bogey;

        [SerializeField] Image doubleBogey;

        [SerializeField] Image tripleBogey;

        [SerializeField] Image awful;

        [SerializeField] float displaySeconds = 3.5f;

        public float DisplaySeconds => displaySeconds;

        public static HoleCompleteBannerUI Ensure()
        {
            var canvas = FindHudCanvas();
            if (canvas == null)
                return null;

            var existing = canvas.Find(BannerName)?.GetComponent<HoleCompleteBannerUI>();
            if (existing != null)
            {
                existing.BindSceneReferences();
                if (Application.isPlaying)
                    existing.Hide();

                return existing;
            }

            if (SceneHudAuthoring.IsActive)
            {
                Debug.LogWarning(
                    "[Disk Golf] HoleCompleteBanner not found under GameplayHUD. "
                    + "Use Disk Golf → HUD → Bake Score Banners.");
                return null;
            }

            return BuildRuntime(canvas);
        }

        public static HoleCompleteBannerUI CreateForScene(RectTransform hud)
        {
            if (hud == null)
                return null;

            var existing = hud.Find(BannerName)?.GetComponent<HoleCompleteBannerUI>();
            if (existing != null)
            {
                existing.EnsureSceneLayout();
                return existing;
            }

            var banner = BuildSceneLayout(hud);
            banner.ApplyEditorPreview();
            return banner;
        }

        void Awake()
        {
            BindSceneReferences();
            if (Application.isPlaying)
                Hide();
        }

        void Reset() => BindSceneReferences();

        public void BindSceneReferences()
        {
            holeInOne ??= transform.Find("HoleInOne")?.GetComponent<Image>();
            eagle ??= transform.Find("Eagle")?.GetComponent<Image>();
            birdie ??= transform.Find("Birdie")?.GetComponent<Image>();
            par ??= transform.Find("Par")?.GetComponent<Image>();
            bogey ??= transform.Find("Bogey")?.GetComponent<Image>();
            doubleBogey ??= transform.Find("DoubleBogey")?.GetComponent<Image>();
            tripleBogey ??= transform.Find("TripleBogey")?.GetComponent<Image>();
            awful ??= transform.Find("Awful")?.GetComponent<Image>();
        }

        public void EnsureSceneLayout()
        {
            RemoveLegacyChildren();
            BindSceneReferences();

            var root = transform as RectTransform;
            if (root != null)
                ConfigureRootRect(root);

            EnsureSlot(ref holeInOne, root, "HoleInOne", HoleCompleteScoreKind.HoleInOne);
            EnsureSlot(ref eagle, root, "Eagle", HoleCompleteScoreKind.Eagle);
            EnsureSlot(ref birdie, root, "Birdie", HoleCompleteScoreKind.Birdie);
            EnsureSlot(ref par, root, "Par", HoleCompleteScoreKind.Par);
            EnsureSlot(ref bogey, root, "Bogey", HoleCompleteScoreKind.Bogey);
            EnsureSlot(ref doubleBogey, root, "DoubleBogey", HoleCompleteScoreKind.DoubleBogey);
            EnsureSlot(ref tripleBogey, root, "TripleBogey", HoleCompleteScoreKind.TripleBogey);
            EnsureSlot(ref awful, root, "Awful", HoleCompleteScoreKind.Awful);

            ApplyEditorPreview();
        }

        static HoleCompleteBannerUI BuildSceneLayout(RectTransform canvas)
        {
            var bannerGo = new GameObject(BannerName, typeof(RectTransform));
            var rt = bannerGo.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            ConfigureRootRect(rt);

            var banner = bannerGo.AddComponent<HoleCompleteBannerUI>();
            banner.EnsureSceneLayout();
            return banner;
        }

        static HoleCompleteBannerUI BuildRuntime(RectTransform canvas)
        {
            var banner = BuildSceneLayout(canvas);
            banner.gameObject.SetActive(false);
            return banner;
        }

        static void EnsureSlot(
            ref Image slot,
            RectTransform parent,
            string childName,
            HoleCompleteScoreKind kind)
        {
            if (slot != null)
            {
                ApplySlotSprite(slot, kind);
                slot.gameObject.SetActive(false);
                return;
            }

            slot = CreateScoreSlot(parent, childName, kind);
        }

        static Image CreateScoreSlot(RectTransform parent, string name, HoleCompleteScoreKind kind)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            ApplySlotSprite(image, kind);
            go.SetActive(false);
            return image;
        }

        static void ApplySlotSprite(Image image, HoleCompleteScoreKind kind)
        {
            if (image == null)
                return;

            if (image.sprite == null)
                image.sprite = ScoreBannerSprites.LoadKind(kind);

            image.enabled = image.sprite != null;
        }

        static void ConfigureRootRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(640f, 160f);
        }

        static RectTransform FindHudCanvas()
        {
            var hud = GameObject.Find(HudCanvasName);
            return hud != null ? hud.GetComponent<RectTransform>() : null;
        }

        void RemoveLegacyChildren()
        {
            DestroyChild("Title");
            DestroyChild("Score");
            DestroyChild("ScoreBanner");
        }

        void DestroyChild(string childName)
        {
            var child = transform.Find(childName);
            if (child == null)
                return;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        public void ApplyEditorPreview()
        {
            if (Application.isPlaying)
                return;

            BindSceneReferences();
            ShowKind(HoleCompleteScoreKind.Par, previewOnly: true);
            gameObject.SetActive(true);
        }

        public void Show(int strokes, int par)
        {
            BindSceneReferences();
            ShowKind(ScoreBannerSprites.ResolveKind(strokes, par), previewOnly: false);
        }

        void ShowKind(HoleCompleteScoreKind kind, bool previewOnly)
        {
            HideAllSlots();

            var slot = GetSlot(kind);
            if (slot == null)
                return;

            ApplySlotSprite(slot, kind);
            slot.gameObject.SetActive(true);

            if (!previewOnly)
                transform.SetAsLastSibling();

            gameObject.SetActive(true);
        }

        Image GetSlot(HoleCompleteScoreKind kind) => kind switch
        {
            HoleCompleteScoreKind.HoleInOne => holeInOne,
            HoleCompleteScoreKind.Eagle => eagle,
            HoleCompleteScoreKind.Birdie => birdie,
            HoleCompleteScoreKind.Par => par,
            HoleCompleteScoreKind.Bogey => bogey,
            HoleCompleteScoreKind.DoubleBogey => doubleBogey,
            HoleCompleteScoreKind.TripleBogey => tripleBogey,
            HoleCompleteScoreKind.Awful => awful,
            _ => par,
        };

        void HideAllSlots()
        {
            SetSlotActive(holeInOne, false);
            SetSlotActive(eagle, false);
            SetSlotActive(birdie, false);
            SetSlotActive(par, false);
            SetSlotActive(bogey, false);
            SetSlotActive(doubleBogey, false);
            SetSlotActive(tripleBogey, false);
            SetSlotActive(awful, false);
        }

        static void SetSlotActive(Image slot, bool active)
        {
            if (slot != null)
                slot.gameObject.SetActive(active);
        }

        public void Hide()
        {
            HideAllSlots();
            gameObject.SetActive(false);
        }
    }
}

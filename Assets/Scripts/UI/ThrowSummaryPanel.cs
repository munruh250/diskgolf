using System;
using System.Collections;
using DiskGolf.Disc;
using DiskGolf.UI.Callouts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>Pre-throw overlay: character portrait, player name, and upcoming throw number.</summary>
    [DisallowMultipleComponent]
    public sealed class ThrowSummaryPanel : MonoBehaviour
    {
        const string HudCanvasName = "GameplayHUD";
        const string BannerName = "ThrowSummaryBanner";

        static readonly Color PanelColor = new(0.52f, 0.32f, 0.68f, 1f);

        static Sprite _whiteSprite;

        [SerializeField] Image shadowImage;

        [SerializeField] Image panelImage;

        [SerializeField] Image portraitImage;

        [SerializeField] TextMeshProUGUI playerNameLabel;

        [SerializeField] TextMeshProUGUI throwLabel;

        [SerializeField] float displaySeconds = 2.5f;

        [SerializeField] float slideInSeconds = 0.4f;

        Vector2 _restAnchoredPosition;

        bool _restPositionCached;

        float _slideStartY;

        Coroutine _showRoutine;

        public float DisplaySeconds => displaySeconds;

        public float SlideInSeconds => slideInSeconds;

        public bool IsVisible => gameObject.activeSelf;

        public static ThrowSummaryPanel CreateForScene(RectTransform hud)
        {
            if (hud == null)
                return null;

            var existing = hud.Find(BannerName)?.GetComponent<ThrowSummaryPanel>();
            if (existing != null)
            {
                existing.EnsureSceneLayout();
                EnsureFacade(existing.gameObject);
                return existing;
            }

            var banner = BuildSceneLayout(hud);
            banner.ApplyEditorPreview();
            EnsureFacade(banner.gameObject);
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
            shadowImage ??= transform.Find("Shadow")?.GetComponent<Image>();
            panelImage ??= transform.Find("Panel")?.GetComponent<Image>();
            portraitImage ??= transform.Find("Portrait")?.GetComponent<Image>();

            var textBlock = transform.Find("TextBlock");
            playerNameLabel ??= textBlock?.Find("PlayerName")?.GetComponent<TextMeshProUGUI>();
            throwLabel ??= textBlock?.Find("ThrowLabel")?.GetComponent<TextMeshProUGUI>();
        }

        public void EnsureSceneLayout()
        {
            BindSceneReferences();

            var root = transform as RectTransform;
            if (root != null)
                ConfigureRootRect(root);

            shadowImage = EnsurePanelImage(ref shadowImage, root, "Shadow", Color.black);
            panelImage = EnsurePanelImage(ref panelImage, root, "Panel", PanelColor);
            portraitImage = EnsurePortraitImage(portraitImage, root);
            EnsureTextBlock(root);
            BindSceneReferences();
            CacheRestPosition();
        }

        static ThrowSummaryPanel BuildSceneLayout(RectTransform canvas)
        {
            var bannerGo = new GameObject(BannerName, typeof(RectTransform));
            var rt = bannerGo.GetComponent<RectTransform>();
            rt.SetParent(canvas, false);
            ConfigureRootRect(rt);

            var banner = bannerGo.AddComponent<ThrowSummaryPanel>();
            EnsureFacade(bannerGo);
            banner.EnsureSceneLayout();
            return banner;
        }

        static void EnsureFacade(GameObject bannerGo)
        {
            if (bannerGo.GetComponent<ThrowSummaryBannerUI>() == null)
                _ = bannerGo.AddComponent<ThrowSummaryBannerUI>();
        }

        static void ConfigureRootRect(RectTransform rt) =>
            ThrowSummaryLayout.ApplyRootRect(rt);

        void CacheRestPosition()
        {
            var rt = transform as RectTransform;
            if (rt == null)
                return;

            _restAnchoredPosition = rt.anchoredPosition;
            _restPositionCached = true;
        }

        static float ResolveOffScreenStartY(RectTransform rt)
        {
            var canvas = rt.root as RectTransform;
            float canvasHeight = canvas != null ? canvas.rect.height : 1080f;
            float anchorY = (rt.anchorMin.y + rt.anchorMax.y) * 0.5f;
            float centerY = canvasHeight * anchorY + rt.anchoredPosition.y;
            float clearTop = canvasHeight - centerY + rt.rect.height * 0.5f + 48f;
            return rt.anchoredPosition.y + clearTop;
        }

        static Image EnsurePanelImage(ref Image image, RectTransform parent, string name, Color color)
        {
            if (image == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                image = go.GetComponent<Image>();
            }

            ThrowSummaryLayout.ApplyPanelRect(image.rectTransform, name == "Shadow");
            ApplyPanelImage(image, color);
            return image;
        }

        static void ApplyPanelImage(Image image, Color color)
        {
            image.sprite = WhiteSprite();
            image.type = Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
        }

        static Image EnsurePortraitImage(Image image, RectTransform parent)
        {
            if (image == null)
            {
                var go = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                image = go.GetComponent<Image>();
            }

            ThrowSummaryLayout.ApplyPortraitRect(image.rectTransform);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        void EnsureTextBlock(RectTransform parent)
        {
            var textBlock = parent.Find("TextBlock") as RectTransform;
            if (textBlock == null)
            {
                var go = new GameObject("TextBlock", typeof(RectTransform));
                textBlock = go.GetComponent<RectTransform>();
                textBlock.SetParent(parent, false);
            }

            ThrowSummaryLayout.ApplyTextBlockRect(textBlock);

            playerNameLabel = EnsureLabel(
                playerNameLabel,
                textBlock,
                "PlayerName",
                tmp => ThrowSummaryLayout.ApplyNameStyle(tmp, "PLAYER 1"));

            throwLabel = EnsureLabel(
                throwLabel,
                textBlock,
                "ThrowLabel",
                tmp => ThrowSummaryLayout.ApplyThrowStyle(tmp, ThrowSummaryLabels.Format(1)));
        }

        static TextMeshProUGUI EnsureLabel(
            TextMeshProUGUI label,
            RectTransform parent,
            string name,
            Action<TextMeshProUGUI> applyStyle)
        {
            if (label == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                label = go.AddComponent<TextMeshProUGUI>();
            }

            if (name == "PlayerName")
                ThrowSummaryLayout.ApplyNameLabelRect(label.rectTransform);
            else
                ThrowSummaryLayout.ApplyThrowLabelRect(label.rectTransform);

            applyStyle(label);
            BindSummaryFont(label);
            return label;
        }

        static void BindSummaryFont(TextMeshProUGUI tmp)
        {
            HudTypography.BindFont(tmp);
            if (tmp.font != null)
                return;

            var hud = GameObject.Find(HudCanvasName);
            if (hud == null)
                return;

            foreach (var existing in hud.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (existing == tmp || existing.font == null)
                    continue;

                tmp.font = existing.font;
                return;
            }
        }

        static void TryApplyOutline(TextMeshProUGUI tmp, float width)
        {
            if (tmp == null || tmp.font == null || tmp.font.material == null)
                return;

            if (!tmp.gameObject.activeInHierarchy)
                return;

            try
            {
                _ = tmp.fontMaterial;
                tmp.outlineWidth = width;
                tmp.outlineColor = Color.black;
            }
            catch (MissingReferenceException)
            {
            }
            catch (NullReferenceException)
            {
            }
        }

        void ApplyRuntimeOutlines()
        {
            TryApplyOutline(playerNameLabel, 0.28f);
            TryApplyOutline(throwLabel, 0.24f);
        }

        static Sprite WhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
            return _whiteSprite;
        }

        public void ShowBriefly(int upcomingThrowNumber, PlayerCharacterProfile character, Action onDismissed = null)
        {
            EnsureSceneLayout();

            if (panelImage == null || playerNameLabel == null || throwLabel == null)
            {
                onDismissed?.Invoke();
                return;
            }

            ApplyContent(upcomingThrowNumber, character);
            transform.SetAsLastSibling();

            if (!_restPositionCached)
                CacheRestPosition();

            var rt = transform as RectTransform;
            _slideStartY = rt != null ? ResolveOffScreenStartY(rt) : _restAnchoredPosition.y + 600f;
            if (rt != null)
                rt.anchoredPosition = new Vector2(_restAnchoredPosition.x, _slideStartY);

            gameObject.SetActive(true);
            ApplyRuntimeOutlines();

            if (_showRoutine != null)
                StopCoroutine(_showRoutine);

            _showRoutine = StartCoroutine(ShowRoutine(onDismissed));
        }

        IEnumerator ShowRoutine(Action onDismissed)
        {
            var rt = transform as RectTransform;

            float elapsed = 0f;
            while (elapsed < slideInSeconds)
            {
                elapsed += Time.deltaTime;
                if (rt != null)
                {
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / slideInSeconds);
                    rt.anchoredPosition = new Vector2(
                        _restAnchoredPosition.x,
                        Mathf.Lerp(_slideStartY, _restAnchoredPosition.y, t));
                }

                yield return null;
            }

            if (rt != null)
                rt.anchoredPosition = _restAnchoredPosition;

            yield return new WaitForSeconds(displaySeconds);

            Hide();
            onDismissed?.Invoke();
            _showRoutine = null;
        }

        void ApplyContent(int upcomingThrowNumber, PlayerCharacterProfile character)
        {
            ThrowSummaryLayout.ApplyNameStyle(playerNameLabel, ResolvePlayerName(character));
            ThrowSummaryLayout.ApplyThrowStyle(throwLabel, ThrowSummaryLabels.Format(upcomingThrowNumber));
            BindSummaryFont(playerNameLabel);
            BindSummaryFont(throwLabel);
            ApplyPortrait(character);
        }

        void ApplyPortrait(PlayerCharacterProfile character)
        {
            if (portraitImage == null)
                return;

            if (character?.previewSprite != null)
            {
                portraitImage.sprite = character.previewSprite;
                portraitImage.color = Color.white;
                portraitImage.preserveAspect = true;
                portraitImage.enabled = true;
                return;
            }

            portraitImage.sprite = null;
            portraitImage.color = character != null ? character.portraitColor : PanelColor;
            portraitImage.enabled = true;
        }

        static string ResolvePlayerName(PlayerCharacterProfile character)
        {
            if (character == null || string.IsNullOrWhiteSpace(character.firstName))
                return "PLAYER 1";

            return character.firstName.ToUpperInvariant();
        }

        public void Hide()
        {
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }

            gameObject.SetActive(false);
        }

        void ApplyEditorPreview()
        {
            if (Application.isPlaying)
                return;

            EnsureSceneLayout();
            ApplyContent(1, null);
            gameObject.SetActive(true);
        }
    }
}

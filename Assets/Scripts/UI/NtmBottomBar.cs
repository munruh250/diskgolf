using DiskGolf.Core;
using DiskGolf.Disc;
using DiskGolf.Flight;
using DiskGolf.Input;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>NTM-style bottom bar: clickable stance and disc selectors with max distance.</summary>
    public sealed class NtmBottomBar : MonoBehaviour
    {
        const string RootName = "NtmBottomBar";

        static readonly Color BarColor = new(0.42f, 0.44f, 0.48f, 0.96f);

        static readonly Color InsetColor = new(0.1f, 0.11f, 0.13f, 0.98f);

        static readonly Color LabelYellow = new(1f, 0.92f, 0.18f, 1f);

        static readonly Color ValueWhite = new(0.95f, 0.97f, 1f, 1f);

        static float S => TimingMeterLayout.UiScale;

        [SerializeField] ThrowController throwController;

        [SerializeField] ThrowInputHandler input;

        [SerializeField] DiscBag bag;

        [SerializeField] TextMeshProUGUI stanceValue;

        [SerializeField] TextMeshProUGUI discValue;

        [SerializeField] Button stanceButton;

        [SerializeField] Button discButton;

        [SerializeField] TextMeshProUGUI arcValue;

        [SerializeField] Button arcButton;

        ReleaseAngle _lastStance;

        DiscProfile _lastDisc;

        ThrowHeight _lastArc;

        public static NtmBottomBar Ensure(RectTransform hudRoot)
        {
            if (hudRoot == null)
                return null;

            var existing = hudRoot.Find(RootName)?.GetComponent<NtmBottomBar>();
            if (existing != null)
            {
                existing.BindReferences();
                existing.HideLegacyHud();
                return existing;
            }

            if (SceneHudAuthoring.IsActive)
            {
                Debug.LogWarning("[Disk Golf] NtmBottomBar not found on GameplayHUD. Use Disk Golf → HUD → Bake Missing Scene Widgets.");
                return null;
            }

            var bar = Build(hudRoot);
            bar.HideLegacyHud();
            return bar;
        }

#if UNITY_EDITOR
        public static NtmBottomBar CreateForScene(RectTransform hudRoot)
        {
            if (hudRoot == null)
                return null;

            var existing = hudRoot.Find(RootName)?.GetComponent<NtmBottomBar>();
            if (existing != null)
            {
                existing.BakeSceneUpgrades();
                existing.HideLegacyHud();
                return existing;
            }

            var bar = Build(hudRoot);
            bar.HideLegacyHud();
            return bar;
        }

        /// <summary>Editor bake: add ARC section and wire buttons on an existing bar.</summary>
        public void BakeSceneUpgrades()
        {
            ResolveOptionalReferences();

            if (arcButton != null)
            {
                WireButtons();
                Refresh();
                return;
            }

            var root = transform as RectTransform;
            float innerH = root.sizeDelta.y > 1f ? root.sizeDelta.y - 8f * S : 52f * S - 8f * S;
            float x = ComputeArcSectionStartX(root);
            float sectionGap = 12f * S;

            x += sectionGap;
            x = AddSectionLabel(root, "ARC", x, innerH, 44f * S);
            AddClickableInset(root, "ArcButton", x, innerH, 96f * S, out arcButton, out arcValue);

            WireButtons();
            Refresh();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        float ComputeArcSectionStartX(RectTransform root)
        {
            var discBtn = root.Find("DiscButton") as RectTransform;
            if (discBtn != null)
                return discBtn.anchoredPosition.x + discBtn.sizeDelta.x;

            return 420f * S;
        }
#endif

        void Awake() => BindReferences();

        void OnEnable()
        {
            BindReferences();
            WireButtons();
            Refresh();
        }

        void Update()
        {
            var stance = input != null ? input.ReleaseAngle : ReleaseAngle.Flat;
            var disc = bag?.Active;
            var arc = input != null ? input.ArcHeight : ThrowHeight.Nice;

            if (stance != _lastStance || disc != _lastDisc || arc != _lastArc)
                Refresh();
        }

        void BindReferences()
        {
            throwController ??= FindObjectOfType<ThrowController>();
            input ??= throwController != null
                ? throwController.GetComponent<ThrowInputHandler>()
                : FindObjectOfType<ThrowInputHandler>();
            bag ??= throwController != null
                ? throwController.GetComponent<DiscBag>()
                : FindObjectOfType<DiscBag>();

            ResolveOptionalReferences();
            WireButtons();
        }

        void ResolveOptionalReferences()
        {
            var root = transform as RectTransform;
            if (root == null)
                return;

            stanceButton ??= root.Find("StanceButton")?.GetComponent<Button>();
            discButton ??= root.Find("DiscButton")?.GetComponent<Button>();
            stanceValue ??= root.Find("StanceButton/Value")?.GetComponent<TextMeshProUGUI>();
            discValue ??= root.Find("DiscButton/Value")?.GetComponent<TextMeshProUGUI>();
            arcButton ??= root.Find("ArcButton")?.GetComponent<Button>();
            arcValue ??= root.Find("ArcButton/Value")?.GetComponent<TextMeshProUGUI>();
        }

        void WireButtons()
        {
            ConfigureButton(stanceButton, CycleStance);
            ConfigureButton(discButton, CycleDisc);
            ConfigureButton(arcButton, CycleArc);
        }

        static void ConfigureButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        static void ClearUiFocus()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        void HideLegacyHud()
        {
            var hud = transform.parent;
            if (hud == null)
                return;

            HideLabel(hud, "StanceLabel");
            HideLabel(hud, "TypeThrow");
            HideLabel(hud, "FLAT");
            HideLabel(hud, "Disc");

            var row = hud.Find("DiscSelectRow");
            if (row != null)
                row.gameObject.SetActive(false);
        }

        static void HideLabel(Transform hud, string name)
        {
            var tf = hud.Find(name);
            if (tf != null)
                tf.gameObject.SetActive(false);
        }

        void Refresh()
        {
            _lastStance = input != null ? input.ReleaseAngle : ReleaseAngle.Flat;
            _lastDisc = bag?.Active;
            _lastArc = input != null ? input.ArcHeight : ThrowHeight.Nice;

            if (stanceValue != null)
                stanceValue.text = StanceLabel(_lastStance);

            if (discValue != null)
            {
                if (_lastDisc == null)
                    discValue.text = "—";
                else
                    discValue.text = $"{_lastDisc.displayName} · {MaxDistanceYards(_lastDisc)}Y";
            }

            if (arcValue != null)
                arcValue.text = ArcLabel(_lastArc);
        }

        static string ArcLabel(ThrowHeight height) =>
            height switch
            {
                ThrowHeight.Low => "LOW",
                ThrowHeight.High => "HIGH",
                _ => "NORMAL",
            };

        static string StanceLabel(ReleaseAngle angle) =>
            angle switch
            {
                ReleaseAngle.Hyzer => "HYZER",
                ReleaseAngle.Anhyzer => "ANHYZER",
                _ => "FLAT",
            };

        static int MaxDistanceYards(DiscProfile disc)
        {
            float ft = disc.maxDistanceFt * FlightSimulator.DistanceScale;
            return Mathf.Max(1, Mathf.RoundToInt(ft / 3f));
        }

        void CycleStance()
        {
            input?.CycleReleaseAngle();
            Refresh();
            ReleaseUiFocus();
        }

        void CycleDisc()
        {
            bag?.CycleCategoryNext();
            Refresh();
            ReleaseUiFocus();
        }

        void CycleArc()
        {
            input?.CycleArcHeight();

            var aim = throwController != null
                ? throwController.GetComponent<ThrowAimAdjust>()
                : FindObjectOfType<ThrowAimAdjust>();
            if (aim != null && input != null)
                aim.SetPlannedHeight(input.ArcHeight);

            Refresh();
            ReleaseUiFocus();
        }

        void ReleaseUiFocus()
        {
            ClearUiFocus();
            StartCoroutine(ClearUiFocusNextFrame());
        }

        static IEnumerator ClearUiFocusNextFrame()
        {
            yield return null;

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        static NtmBottomBar Build(RectTransform hudRoot)
        {
            float barHeight = 52f * S;
            float leftInset = HudTypography.LeftInset;
            float barWidth = TimingMeterLayout.BottomBarWidth;

            var rootGo = new GameObject(RootName, typeof(RectTransform), typeof(Image));
            var root = rootGo.GetComponent<RectTransform>();
            root.SetParent(hudRoot, false);
            root.anchorMin = root.anchorMax = new Vector2(0f, 0f);
            root.pivot = new Vector2(0f, 0f);
            root.anchoredPosition = new Vector2(leftInset, TimingMeterLayout.BottomBarInset);
            root.sizeDelta = new Vector2(barWidth, barHeight);

            var panel = rootGo.GetComponent<Image>();
            panel.color = BarColor;
            panel.sprite = ArcRingBuilder.WhiteSprite;
            panel.type = Image.Type.Sliced;

            var bar = rootGo.AddComponent<NtmBottomBar>();

            float sectionGap = 12f * S;
            float x = 10f * S;
            float innerH = barHeight - 8f * S;

            x = bar.AddSectionLabel(root, "STANCE", x, innerH, 72f * S);
            x = bar.AddClickableInset(root, "StanceButton", x, innerH, 88f * S, out bar.stanceButton, out bar.stanceValue);
            x += sectionGap;

            x = bar.AddSectionLabel(root, "DISC", x, innerH, 52f * S);
            x = bar.AddClickableInset(root, "DiscButton", x, innerH, 168f * S, out bar.discButton, out bar.discValue);
            x += sectionGap;

            x = bar.AddSectionLabel(root, "ARC", x, innerH, 44f * S);
            bar.AddClickableInset(root, "ArcButton", x, innerH, 96f * S, out bar.arcButton, out bar.arcValue);

            bar.BindReferences();
            bar.Refresh();
            return bar;
        }

        float AddSectionLabel(RectTransform parent, string text, float x, float height, float width)
        {
            var go = new GameObject(text + "Label", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(width, height);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = HudTypography.FontSize * 0.72f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = LabelYellow;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            BindFont(tmp);

            return x + width + 4f * S;
        }

        float AddClickableInset(
            RectTransform parent,
            string name,
            float x,
            float height,
            float width,
            out Button button,
            out TextMeshProUGUI value)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(width, height - 4f * S);

            var img = go.GetComponent<Image>();
            img.color = InsetColor;
            img.sprite = ArcRingBuilder.WhiteSprite;
            img.type = Image.Type.Sliced;

            button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.22f, 0.24f, 0.28f, 1f);
            colors.pressedColor = new Color(0.08f, 0.1f, 0.12f, 1f);
            button.colors = colors;

            var labelGo = new GameObject("Value", typeof(RectTransform));
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.SetParent(rt, false);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(8f * S, 2f * S);
            labelRt.offsetMax = new Vector2(-8f * S, -2f * S);

            value = labelGo.AddComponent<TextMeshProUGUI>();
            value.fontSize = HudTypography.FontSize * 0.78f;
            value.fontStyle = FontStyles.Bold;
            value.color = ValueWhite;
            value.alignment = TextAlignmentOptions.Center;
            value.raycastTarget = false;
            BindFont(value);

            return x + width + 6f * S;
        }

        static void BindFont(TextMeshProUGUI tmp) => HudTypography.BindFont(tmp);
    }
}

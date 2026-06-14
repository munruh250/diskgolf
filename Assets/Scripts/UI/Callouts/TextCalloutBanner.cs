using System.Collections;
using DiskGolf.UI;
using TMPro;
using UnityEngine;

namespace DiskGolf.UI.Callouts
{
    public sealed class TextCalloutBanner : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI label;

        [SerializeField] TextCalloutLayout layout = TextCalloutLayout.FeetLabel;

        [SerializeField] float displaySeconds = 2f;

        [SerializeField] bool autoHide;

        Coroutine _hideRoutine;

        public float DisplaySeconds => displaySeconds;

        void Awake() => ResolveLabel();

        public void ResolveLabel()
        {
            label ??= GetComponent<TextMeshProUGUI>();
            if (label != null)
                label.raycastTarget = false;
        }

        public void Show(string text)
        {
            ResolveLabel();
            if (label == null)
                return;

            ApplyLayout();
            label.text = text;
            label.ForceMeshUpdate();
            CalloutLifecycle.BringToFront(transform);
            gameObject.SetActive(true);
        }

        public void ShowBriefly(string text)
        {
            CalloutLifecycle.ShowBriefly(
                this,
                ref _hideRoutine,
                displaySeconds,
                () => Show(text),
                Hide);
        }

        public void ShowThrowDistance(float distanceFt)
        {
            ApplyFeetStyle();
            int feet = Mathf.Max(0, Mathf.RoundToInt(distanceFt));
            Show($"{feet} FEET");
        }

        public void Hide()
        {
            CalloutLifecycle.Cancel(ref _hideRoutine, this);
            gameObject.SetActive(false);
        }

        void ApplyLayout()
        {
            var rt = transform as RectTransform;
            if (rt == null)
                return;

            switch (layout)
            {
                case TextCalloutLayout.FeetLabel:
                    PostThrowCalloutLayout.ApplyFeetLabelRect(rt);
                    break;
                case TextCalloutLayout.CenterPopup:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.62f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(640f, 120f);
                    break;
            }
        }

        public void ApplyFeetStyle()
        {
            ResolveLabel();
            if (label == null)
                return;

            label.fontSize = 64f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Top;
            label.color = Color.black;
            label.outlineWidth = 0f;
            HudTypography.BindFont(label);
        }

        public void ApplySweetSpotStyle()
        {
            ResolveLabel();
            if (label == null)
                return;

            label.fontSize = 72f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.82f, 0.28f, 1f);
            label.outlineWidth = 0.35f;
            label.outlineColor = Color.black;
            HudTypography.BindFont(label);
        }
    }
}

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

        public TextCalloutLayout Layout
        {
            get => layout;
            set => layout = value;
        }

        void Awake()
        {
            ResolveLabel();
            ApplyLayout();
            if (Application.isPlaying)
                Hide();
        }

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
            label.ForceMeshUpdate(true);
            CalloutLifecycle.BringToFront(transform);
            gameObject.SetActive(true);
        }

        public void ShowBriefly(string text)
        {
            _hideRoutine = CalloutLifecycle.ShowBriefly(
                this,
                _hideRoutine,
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
            if (layout != TextCalloutLayout.FeetLabel)
                return;

            var rt = transform as RectTransform;
            if (rt != null)
                PostThrowCalloutLayout.ApplyFeetLabelRect(rt);
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
    }
}

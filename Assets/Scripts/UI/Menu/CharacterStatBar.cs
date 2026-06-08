using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI.Menu
{
    /// <summary>One attribute fill meter row on the character select stat panel.</summary>
    public sealed class CharacterStatBar : MonoBehaviour
    {
        const float FillInset = 2f;

        [SerializeField] TextMeshProUGUI valueLabel;

        [SerializeField] Image meterBackdrop;

        [SerializeField] Image meterTrack;

        [SerializeField] Image fill;

        [SerializeField] RectTransform fillRect;

        void Awake()
        {
            if (fillRect == null && fill != null)
                fillRect = fill.rectTransform;
        }

        public void SetValue(int stat, Color fillColor)
        {
            if (valueLabel != null)
                valueLabel.text = stat.ToString();

            if (fillRect == null && fill != null)
                fillRect = fill.rectTransform;

            if (fillRect == null)
                return;

            if (fill != null)
            {
                fill.type = Image.Type.Simple;
                fill.color = fillColor;
            }

            float amount = Mathf.Clamp01(stat / 100f);
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(amount, 1f);
            fillRect.offsetMin = new Vector2(FillInset, FillInset);
            fillRect.offsetMax = new Vector2(-FillInset, -FillInset);
        }
    }
}

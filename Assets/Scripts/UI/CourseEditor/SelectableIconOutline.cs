using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI.CourseEditor
{
    /// <summary>Square accent border for foliage/skybox icon pickers.</summary>
    public sealed class SelectableIconOutline : MonoBehaviour
    {
        const float SelectedInset = 3f;

        public Image border;
        public RectTransform content;

        public void SetSelected(bool selected, Color accentColor)
        {
            if (border != null)
                border.color = selected ? accentColor : Color.clear;

            if (content == null)
                return;

            var inset = selected ? SelectedInset : 0f;
            content.offsetMin = new Vector2(inset, inset);
            content.offsetMax = new Vector2(-inset, -inset);
        }
    }
}

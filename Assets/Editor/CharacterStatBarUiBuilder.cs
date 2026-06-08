#if UNITY_EDITOR
using System.Text.RegularExpressions;
using DiskGolf.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiskGolf.EditorTools
{
    public static class CharacterStatBarUiBuilder
    {
        public static void BuildFillMeter(RectTransform row, CharacterStatBar bar, TextMeshProUGUI valueLabel)
        {
            RemoveLegacyBlocks(row);

            var backdrop = EnsureBackdrop(row);
            var track = EnsureTrack(row);
            var fill = EnsureFill(track.rectTransform);

            AssignSerialized(bar, "valueLabel", valueLabel);
            AssignSerialized(bar, "meterBackdrop", backdrop);
            AssignSerialized(bar, "meterTrack", track);
            AssignSerialized(bar, "fill", fill);
            AssignSerialized(bar, "fillRect", fill.rectTransform);
            EditorUtility.SetDirty(bar);
        }

        public static void RemoveLegacyBlocks(Transform row)
        {
            for (int i = row.childCount - 1; i >= 0; i--)
            {
                var child = row.GetChild(i);
                if (BlockNamePattern.IsMatch(child.name))
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        static readonly Regex BlockNamePattern = new(@"^Block_\d+$", RegexOptions.Compiled);

        static Image EnsureBackdrop(RectTransform row)
        {
            var existing = row.Find("MeterBackdrop");
            if (existing != null)
                return existing.GetComponent<Image>();

            var go = new GameObject("MeterBackdrop", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(row, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(
                CharacterStatBarLayout.TrackLeft - CharacterStatBarLayout.BackdropPadding,
                0f);
            rt.sizeDelta = new Vector2(
                CharacterStatBarLayout.TrackWidth + CharacterStatBarLayout.BackdropPadding * 2f,
                CharacterStatBarLayout.TrackHeight + CharacterStatBarLayout.BackdropPadding * 2f);

            var image = go.GetComponent<Image>();
            image.color = CharacterStatBarLayout.BackdropColor;
            image.raycastTarget = false;
            return image;
        }

        static Image EnsureTrack(RectTransform row)
        {
            var existing = row.Find("MeterTrack");
            if (existing != null)
                return existing.GetComponent<Image>();

            var go = new GameObject("MeterTrack", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(row, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(CharacterStatBarLayout.TrackLeft, 0f);
            rt.sizeDelta = new Vector2(CharacterStatBarLayout.TrackWidth, CharacterStatBarLayout.TrackHeight);

            var image = go.GetComponent<Image>();
            image.color = CharacterStatBarLayout.TrackColor;
            image.raycastTarget = false;
            return image;
        }

        static Image EnsureFill(RectTransform track)
        {
            var existing = track.Find("Fill");
            if (existing != null)
                return ConfigureFill(existing.GetComponent<Image>());

            var go = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(track, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, -2f);
            return ConfigureFill(go.GetComponent<Image>());
        }

        static Image ConfigureFill(Image fill)
        {
            fill.type = Image.Type.Simple;
            fill.raycastTarget = false;

            var rt = fill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, -2f);
            return fill;
        }

        static void AssignSerialized(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
                return;

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    public static class CharacterStatBarFillUpgrader
    {
        [MenuItem("Disk Golf/Convert Character Select Stat Bars To Fill Meters")]
        public static void ConvertOpenScene()
        {
            var bars = Object.FindObjectsByType<CharacterStatBar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (bars.Length == 0)
            {
                Debug.LogWarning("[Disk Golf] No CharacterStatBar components found in the open scene.");
                return;
            }

            foreach (var bar in bars)
                ConvertBar(bar);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[Disk Golf] Converted {bars.Length} stat bar(s) to fill meters.");
        }

        public static void ConvertBar(CharacterStatBar bar)
        {
            var row = bar.transform as RectTransform;
            if (row == null)
                return;

            TextMeshProUGUI label = null;
            var so = new SerializedObject(bar);
            var labelProp = so.FindProperty("valueLabel");
            if (labelProp != null)
                label = labelProp.objectReferenceValue as TextMeshProUGUI;

            if (label == null)
            {
                var valueTransform = row.Find("Value");
                if (valueTransform != null)
                    label = valueTransform.GetComponent<TextMeshProUGUI>();
            }

            CharacterStatBarUiBuilder.BuildFillMeter(row, bar, label);
        }
    }
}
#endif

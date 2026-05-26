using DiskGolf.Core;
using DiskGolf.Disc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    /// <summary>On-screen buttons to switch discs in the bag.</summary>
    public sealed class DiscSelectUI : MonoBehaviour
    {
        [SerializeField] ThrowController throwController;

        [SerializeField] DiscBag bag;

        [SerializeField] RectTransform buttonRow;

        void Awake()
        {
            throwController ??= FindObjectOfType<ThrowController>();
            bag ??= throwController != null ? throwController.GetComponent<DiscBag>() : FindObjectOfType<DiscBag>();
            EnsureButtonRow();
            RebuildButtons();
        }

        DiscProfile _lastActive;

        void Update()
        {
            if (bag?.Active != _lastActive)
            {
                _lastActive = bag.Active;
                RefreshSelection();
            }
        }

        void OnEnable() => RefreshSelection();

        public void RebuildButtons()
        {
            EnsureButtonRow();
            if (buttonRow == null || bag == null)
                return;

            for (int i = buttonRow.childCount - 1; i >= 0; i--)
                Destroy(buttonRow.GetChild(i).gameObject);

            var discs = bag.All;
            if (discs == null)
                return;

            for (int i = 0; i < discs.Length; i++)
            {
                if (discs[i] == null)
                    continue;

                CreateButton(i, discs[i]);
            }

            RefreshSelection();
        }

        void CreateButton(int index, DiscProfile profile)
        {
            var go = new GameObject(profile.displayName + "Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(buttonRow, false);
            rt.sizeDelta = new Vector2(108f, 40f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.1f, 0.12f, 0.88f);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.SetParent(rt, false);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = profile.displayName;
            tmp.fontSize = 20f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            var font = FindAnyDiscLabelFont();
            if (font != null)
                tmp.font = font;

            int captured = index;
            go.GetComponent<Button>().onClick.AddListener(() => SelectDisc(captured));
        }

        void SelectDisc(int index)
        {
            if (bag == null)
                return;

            bag.SelectIndex(index);
            RefreshSelection();
        }

        void RefreshSelection()
        {
            if (bag == null || buttonRow == null)
                return;

            var active = bag.Active;
            for (int i = 0; i < buttonRow.childCount; i++)
            {
                var child = buttonRow.GetChild(i);
                var image = child.GetComponent<Image>();
                if (image == null)
                    continue;

                bool selected = active != null && child.name.StartsWith(active.displayName);
                image.color = selected
                    ? new Color(0.1f, 0.55f, 0.42f, 0.95f)
                    : new Color(0.08f, 0.1f, 0.12f, 0.88f);
            }
        }

        void EnsureButtonRow()
        {
            if (buttonRow != null)
                return;

            var hud = GameObject.Find("GameplayHUD")?.GetComponent<RectTransform>();
            if (hud == null)
                return;

            var go = new GameObject("DiscSelectRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonRow = go.GetComponent<RectTransform>();
            buttonRow.SetParent(hud, false);
            buttonRow.anchorMin = buttonRow.anchorMax = new Vector2(0.5f, 0f);
            buttonRow.pivot = new Vector2(0.5f, 0f);
            buttonRow.anchoredPosition = new Vector2(0f, 168f);
            buttonRow.sizeDelta = new Vector2(520f, 44f);

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        static TMP_FontAsset FindAnyDiscLabelFont()
        {
            var hud = GameObject.Find("GameplayHUD");
            if (hud == null)
                return null;

            var tmp = hud.GetComponentInChildren<TextMeshProUGUI>(true);
            return tmp != null ? tmp.font : null;
        }
    }
}

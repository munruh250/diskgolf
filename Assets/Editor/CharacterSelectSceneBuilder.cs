#if UNITY_EDITOR
using DiskGolf.Disc;
using DiskGolf.UI;
using DiskGolf.UI.Menu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiskGolf.EditorTools
{
    public static class CharacterSelectSceneBuilder
    {
        const string RosterPath = "Assets/Resources/PlayerCharacterRoster.asset";

        const int CharacterCount = 6;

        static readonly Color PanelColor = new(0.55f, 0.58f, 0.55f, 1f);

        static readonly Color PanelInnerColor = new(0.42f, 0.44f, 0.42f, 1f);

        static readonly Color NameColor = new(1f, 0.92f, 0.2f, 1f);

        static readonly Color TitleColor = new(1f, 0.95f, 0.35f, 1f);

        static readonly string[] StatLabels = { "POWER", "ACCURACY", "CLUTCH" };

        [MenuItem("Disk Golf/Build Character Select Scene")]
        public static void BuildSceneMenuItem()
        {
            MenuSceneBuilder.BuildCharacterSelectSceneOnly();
        }

        public static void BuildUi(RectTransform root, CharacterSelectScreen screen)
        {
            ClearChildren(root);

            var title = CreateAnchoredLabel(root, "Title", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                CharacterSelectLayoutDefaults.TitlePosition, CharacterSelectLayoutDefaults.TitleSize,
                CharacterSelectLayoutDefaults.TitleFontSize, TitleColor, TextAlignmentOptions.Center);
            title.text = CharacterSelectLayoutDefaults.TitleText;

            var portraitRow = CreateRect("PortraitRow", root);
            SetCentered(portraitRow, CharacterSelectLayoutDefaults.PortraitRowPosition,
                CharacterSelectLayoutDefaults.PortraitRowSize);

            var cards = new CharacterCardSlot[CharacterCount];
            for (int i = 0; i < CharacterCount; i++)
            {
                cards[i] = BuildCharacterCard(portraitRow, $"Card_{i}",
                    new Vector2(CharacterSelectLayoutDefaults.CardPositionsX[i], 0f));
            }

            var (statBars, playerNameLabel) = BuildStatPanel(root);

            var backButton = CreateNavButton(root, "Back To Main Menu",
                CharacterSelectLayoutDefaults.BackButtonPosition, CharacterSelectLayoutDefaults.BackButtonSize.x);
            var continueButton = CreateNavButton(root, "Continue To Course Select",
                CharacterSelectLayoutDefaults.ContinueButtonPosition,
                CharacterSelectLayoutDefaults.ContinueButtonSize.x);

            var roster = AssetDatabase.LoadAssetAtPath<PlayerCharacterRoster>(RosterPath);
            AssignSerialized(screen, "roster", roster);
            AssignArray(screen, "characterCards", cards);
            AssignArray(screen, "statBars", statBars);
            AssignSerialized(screen, "selectedPlayerLabel", playerNameLabel);
            AssignSerialized(screen, "backButton", backButton);
            AssignSerialized(screen, "continueButton", continueButton);

            EditorUtility.SetDirty(screen);
        }

        static CharacterCardSlot BuildCharacterCard(RectTransform parent, string name, Vector2 position)
        {
            var cardRoot = CreateRect(name, parent);
            SetCentered(cardRoot, position, CharacterSelectLayoutDefaults.CardSize);

            var slot = cardRoot.gameObject.AddComponent<CharacterCardSlot>();

            var cardButtonGo = new GameObject("SelectButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var cardButtonRt = cardButtonGo.GetComponent<RectTransform>();
            cardButtonRt.SetParent(cardRoot, false);
            Stretch(cardButtonRt);
            var cardImage = cardButtonGo.GetComponent<Image>();
            cardImage.color = new Color(0f, 0f, 0f, 0f);
            cardImage.raycastTarget = true;

            var nameBlock = CreateRect("NameBlock", cardRoot);
            var nameBlockRt = nameBlock;
            nameBlockRt.anchorMin = new Vector2(0f, 1f);
            nameBlockRt.anchorMax = new Vector2(1f, 1f);
            nameBlockRt.pivot = new Vector2(0.5f, 1f);
            nameBlockRt.anchoredPosition = Vector2.zero;
            nameBlockRt.sizeDelta = new Vector2(0f, 96f);

            var firstName = CreateAnchoredLabel(nameBlock, "FirstName", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -14f), new Vector2(-8f, 26f), 22f, NameColor, TextAlignmentOptions.Center);
            var nickname = CreateAnchoredLabel(nameBlock, "Nickname", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -42f), new Vector2(-8f, 26f), 20f, NameColor, TextAlignmentOptions.Center);
            var lastName = CreateAnchoredLabel(nameBlock, "LastName", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -70f), new Vector2(-8f, 26f), 22f, NameColor, TextAlignmentOptions.Center);

            var portraitFrame = CreateRect("PortraitFrame", cardRoot);
            SetCentered(portraitFrame, new Vector2(0f, -24f), new Vector2(168f, 168f));

            var borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.SetParent(portraitFrame, false);
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-5f, -5f);
            borderRt.offsetMax = new Vector2(5f, 5f);
            var borderImage = borderGo.GetComponent<Image>();
            borderImage.color = new Color(0f, 0f, 0f, 0f);
            borderImage.raycastTarget = false;
            borderGo.SetActive(false);

            var portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            var portraitRt = portraitGo.GetComponent<RectTransform>();
            portraitRt.SetParent(portraitFrame, false);
            Stretch(portraitRt);
            var portraitImage = portraitGo.GetComponent<Image>();
            portraitImage.color = new Color(0.4f, 0.5f, 0.7f);
            portraitImage.raycastTarget = false;

            var playerTag = CreateAnchoredLabel(portraitFrame, "PlayerTag", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(-8f, 10f), new Vector2(44f, 28f), 20f, NameColor, TextAlignmentOptions.MidlineLeft);
            playerTag.gameObject.SetActive(false);
            playerTag.text = "P1";

            var region = CreateAnchoredLabel(cardRoot, "Region", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 18f), new Vector2(0f, 28f), 20f, Color.white, TextAlignmentOptions.Center);

            cardButtonRt.SetAsLastSibling();

            AssignSerialized(slot, "firstName", firstName);
            AssignSerialized(slot, "nickname", nickname);
            AssignSerialized(slot, "lastName", lastName);
            AssignSerialized(slot, "region", region);
            AssignSerialized(slot, "playerTag", playerTag);
            AssignSerialized(slot, "portrait", portraitImage);
            AssignSerialized(slot, "border", borderImage);
            AssignSerialized(slot, "selectButton", cardButtonGo.GetComponent<Button>());

            return slot;
        }

        static (CharacterStatBar[] bars, TextMeshProUGUI playerNameLabel) BuildStatPanel(RectTransform parent)
        {
            var panelOuter = new GameObject("StatPanel", typeof(RectTransform), typeof(Image));
            var panelOuterRt = panelOuter.GetComponent<RectTransform>();
            panelOuterRt.SetParent(parent, false);
            SetCentered(panelOuterRt, CharacterSelectLayoutDefaults.StatPanelPosition,
                CharacterSelectLayoutDefaults.StatPanelSize);
            panelOuter.GetComponent<Image>().color = PanelColor;

            var panel = CreateRect("Inner", panelOuterRt);
            Stretch(panel);
            panel.offsetMin = new Vector2(8f, 8f);
            panel.offsetMax = new Vector2(-8f, -8f);
            panel.gameObject.AddComponent<Image>().color = PanelInnerColor;

            var playerNameLabel = CreateAnchoredLabel(panel, "P1Label", new Vector2(0f, 1f), new Vector2(0f, 1f),
                CharacterSelectLayoutDefaults.PlayerNameLabelPosition,
                CharacterSelectLayoutDefaults.PlayerNameLabelSize,
                CharacterSelectLayoutDefaults.PlayerNameLabelFontSize, Color.white, TextAlignmentOptions.MidlineLeft);
            playerNameLabel.text = string.Empty;

            var barsRoot = CreateRect("StatBars", panel);
            var barsRt = barsRoot;
            barsRt.anchorMin = new Vector2(0f, 0f);
            barsRt.anchorMax = new Vector2(1f, 1f);
            barsRt.offsetMin = new Vector2(16f, 16f);
            barsRt.offsetMax = new Vector2(-16f, -48f);

            var statBars = new CharacterStatBar[3];
            for (int i = 0; i < 3; i++)
            {
                var row = CreateRect(StatLabels[i], barsRoot);
                var rowRt = row;
                rowRt.anchorMin = new Vector2(0f, 1f);
                rowRt.anchorMax = new Vector2(1f, 1f);
                rowRt.pivot = new Vector2(0.5f, 1f);
                rowRt.anchoredPosition = new Vector2(0f, -i * CharacterSelectLayoutDefaults.StatRowSpacing);
                rowRt.sizeDelta = new Vector2(0f, 40f);
                statBars[i] = BuildStatBarRow(row, StatLabels[i]);
            }

            return (statBars, playerNameLabel);
        }

        static CharacterStatBar BuildStatBarRow(RectTransform row, string statName)
        {
            var bar = row.gameObject.AddComponent<CharacterStatBar>();

            CreateAnchoredLabel(row, "Label", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(56f, 0f), new Vector2(112f, 32f), 20f, Color.white, TextAlignmentOptions.MidlineLeft).text =
                statName;

            var value = CreateAnchoredLabel(row, "Value", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-20f, 0f), new Vector2(40f, 32f), 20f, Color.white, TextAlignmentOptions.MidlineRight);

            CharacterStatBarUiBuilder.BuildFillMeter(row, bar, value);

            return bar;
        }

        static Button CreateNavButton(RectTransform parent, string label, Vector2 pos, float width)
        {
            var go = new GameObject(label.Replace(" ", ""), typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            SetCentered(rt, pos, new Vector2(width, 52f));
            go.GetComponent<Image>().color = new Color(0.1f, 0.14f, 0.1f, 0.95f);

            var tmp = CreateAnchoredLabel(go.transform, "Label", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(width - 16f, 44f), 24f, Color.white, TextAlignmentOptions.Center);
            tmp.text = label;

            return go.GetComponent<Button>();
        }

        static TextMeshProUGUI CreateAnchoredLabel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            float fontSize,
            Color color,
            TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = name;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = align;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            HudTypography.BindFont(tmp);
            return tmp;
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static void SetCentered(RectTransform rt, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void ClearChildren(RectTransform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);
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

        static void AssignArray(Object target, string fieldName, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null || !prop.isArray)
                return;

            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif

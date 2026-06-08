using DiskGolf.Core;
using DiskGolf.Disc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI.Menu
{
    /// <summary>Binds character select scene UI to roster data and navigation.</summary>
    public sealed class CharacterSelectScreen : MonoBehaviour
    {
        static readonly Color[] StatBarColors =
        {
            new(0.92f, 0.22f, 0.18f, 1f),
            new(1f, 0.88f, 0.2f, 1f),
            new(0.2f, 0.78f, 0.32f, 1f),
        };

        [SerializeField] PlayerCharacterRoster roster;

        [SerializeField] CharacterCardSlot[] characterCards;

        [SerializeField] CharacterStatBar[] statBars;

        [SerializeField] Button backButton;

        [SerializeField] Button continueButton;

        [SerializeField] TextMeshProUGUI selectedPlayerLabel;

        int _selectedIndex;

        void Awake()
        {
            if (roster == null)
                roster = GameSessionSettings.Roster;

            _selectedIndex = GameSessionSettings.SelectedCharacterIndex;
            BindCardClicks();
            WireButtons();
            RefreshAll();
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow) || UnityEngine.Input.GetKeyDown(KeyCode.A))
                SelectRelative(-1);

            if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow) || UnityEngine.Input.GetKeyDown(KeyCode.D))
                SelectRelative(1);
        }

        void BindCardClicks()
        {
            if (characterCards == null)
                return;

            for (int i = 0; i < characterCards.Length; i++)
            {
                if (characterCards[i] == null)
                    continue;

                int captured = i;
                var button = characterCards[i].SelectButton;
                if (button != null)
                    button.onClick.AddListener(() => SelectIndex(captured));
            }
        }

        void WireButtons()
        {
            if (backButton != null)
                backButton.onClick.AddListener(() => SceneLoader.Load(SceneFlow.MainMenu));

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(() =>
                {
                    GameSessionSettings.SelectedCharacterIndex = _selectedIndex;
                    SceneLoader.Load(SceneFlow.CourseSelect);
                });
            }
        }

        void SelectRelative(int delta)
        {
            if (roster == null || roster.Count == 0)
                return;

            _selectedIndex = (_selectedIndex + delta + roster.Count) % roster.Count;
            GameSessionSettings.SelectedCharacterIndex = _selectedIndex;
            RefreshAll();
        }

        void SelectIndex(int index)
        {
            _selectedIndex = index;
            GameSessionSettings.SelectedCharacterIndex = index;
            RefreshAll();
        }

        void RefreshAll()
        {
            if (roster == null || characterCards == null)
                return;

            for (int i = 0; i < characterCards.Length; i++)
            {
                if (characterCards[i] == null)
                    continue;

                var profile = roster.Get(i);
                characterCards[i].Apply(profile, i == _selectedIndex);
            }

            var active = roster.Get(_selectedIndex);
            if (active == null)
                return;

            if (selectedPlayerLabel != null)
                selectedPlayerLabel.text = active.firstName.ToUpperInvariant();

            if (statBars == null)
                return;

            if (statBars.Length > 0 && statBars[0] != null)
                statBars[0].SetValue(active.power, StatBarColors[0]);
            if (statBars.Length > 1 && statBars[1] != null)
                statBars[1].SetValue(active.accuracy, StatBarColors[1]);
            if (statBars.Length > 2 && statBars[2] != null)
                statBars[2].SetValue(active.clutch, StatBarColors[2]);
        }
    }
}

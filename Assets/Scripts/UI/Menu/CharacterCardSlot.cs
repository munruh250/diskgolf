using DiskGolf.Disc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI.Menu
{
    /// <summary>One golfer card on the character select screen. Wire references in the scene.</summary>
    public sealed class CharacterCardSlot : MonoBehaviour
    {
        static readonly Color SelectBorderColor = Color.white;

        [SerializeField] TextMeshProUGUI firstName;

        [SerializeField] TextMeshProUGUI nickname;

        [SerializeField] TextMeshProUGUI lastName;

        [SerializeField] TextMeshProUGUI region;

        [SerializeField] TextMeshProUGUI playerTag;

        [SerializeField] Image portrait;

        [SerializeField] Image border;

        [SerializeField] Button selectButton;

        public Button SelectButton => selectButton;

        public void Apply(PlayerCharacterProfile profile, bool selected)
        {
            if (profile == null)
                return;

            firstName.text = profile.firstName.ToUpperInvariant();
            nickname.text = profile.NicknameLine.ToUpperInvariant();
            lastName.text = profile.lastName.ToUpperInvariant();
            region.text = profile.regionLabel;
            ApplyPortrait(profile);
            border.gameObject.SetActive(selected);
            if (selected)
                border.color = SelectBorderColor;
            playerTag.gameObject.SetActive(selected);
            playerTag.text = "P1";
        }

        void ApplyPortrait(PlayerCharacterProfile profile)
        {
            if (portrait == null)
                return;

            if (profile.previewSprite != null)
            {
                portrait.sprite = profile.previewSprite;
                portrait.color = Color.white;
                portrait.preserveAspect = true;
            }
            else
            {
                portrait.sprite = null;
                portrait.color = profile.portraitColor;
            }
        }
    }
}

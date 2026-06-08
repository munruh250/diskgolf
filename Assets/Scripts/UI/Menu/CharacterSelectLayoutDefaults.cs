using UnityEngine;

namespace DiskGolf.UI.Menu
{
    /// <summary>Default Character Select UI layout (baked from scene).</summary>
    public static class CharacterSelectLayoutDefaults
    {
        public const string TitleText = "Disk Golfer Selection";

        public static readonly Vector2 TitlePosition = new(0f, 430f);

        public static readonly Vector2 TitleSize = new(1200f, 100f);

        public const float TitleFontSize = 64f;

        public static readonly Vector2 PortraitRowPosition = new(0f, 90f);

        public static readonly Vector2 PortraitRowSize = new(1760f, 380f);

        public static readonly Vector2 CardSize = new(240f, 360f);

        public static readonly float[] CardPositionsX = { -675f, -405f, -135f, 135f, 405f, 675f };

        public static readonly Vector2 StatPanelPosition = new(0f, -275f);

        public static readonly Vector2 StatPanelSize = new(540f, 250f);

        public static readonly Vector2 PlayerNameLabelPosition = new(24f, -20f);

        public static readonly Vector2 PlayerNameLabelSize = new(160f, 36f);

        public const float PlayerNameLabelFontSize = 32f;

        public const float StatRowSpacing = 56f;

        public static readonly Vector2 BackButtonPosition = new(-300f, -474f);

        public static readonly Vector2 BackButtonSize = new(360f, 52f);

        public static readonly Vector2 ContinueButtonPosition = new(300f, -474f);

        public static readonly Vector2 ContinueButtonSize = new(519.23f, 52f);
    }
}

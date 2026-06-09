using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>
    /// HUD layout + typography settings. Add to the GameplayHUD object.
    /// When Preserve Manual Layout is on, Play Mode will not overwrite Scene-view positions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudLayoutSettings : MonoBehaviour
    {
        public static HudLayoutSettings Active { get; private set; }

        [Header("Editor workflow")]
        [Tooltip("Keep RectTransform positions you set in the Scene/Inspector when entering Play Mode. Timing meters, bottom bar, and other HUD chrome will not be rebuilt or repositioned.")]
        public bool preserveManualLayout = true;

        [Tooltip("Re-apply the default coded layout when Play starts (only if Preserve Manual Layout is off).")]
        public bool applyLayoutOnPlay;

        [Header("Typography")]
        public float fontSize = 28f;

        public float rowHeight = 36f;

        public float leftInset = 72f;

        public float topInset = 40f;

        public float labelWidth = 200f;

        public float valueOffset = 268f;

        public float valueWidth = 100f;

        public Color textColor = Color.white;

        [Header("Right stack (minimap column)")]
        public float minimapWidth = 248f;

        public float minimapHeight = 392f;

        public float rightInset = 20f;

        public float stackGap = 8f;

        [Header("Bottom-left disc labels")]
        public Vector2 flatLabelOffset = new(36f, 88f);

        public Vector2 discLabelOffset = new(36f, 48f);

        void OnEnable() => Active = this;

        void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        public float HoleInfoHeight => rowHeight * 3f;

        public static bool ShouldPreserveLayout() =>
            Active != null && Active.preserveManualLayout;

        public static bool ShouldApplyLayoutOnPlay() =>
            Active != null && !Active.preserveManualLayout && Active.applyLayoutOnPlay;

        [ContextMenu("Apply Default HUD Layout Now")]
        public void ApplyDefaultLayoutNow()
        {
            HudLayout.ApplyPositions();
            Debug.Log("[Disk Golf] Applied default HUD layout from HudLayoutSettings.");
        }

        public static HudLayoutSettings EnsureOnHudRoot()
        {
            var hud = GameObject.Find("GameplayHUD");
            if (hud == null)
                return null;

            return hud.GetComponent<HudLayoutSettings>()
                ?? hud.AddComponent<HudLayoutSettings>();
        }
    }
}

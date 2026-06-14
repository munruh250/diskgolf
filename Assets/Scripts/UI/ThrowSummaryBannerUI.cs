using System;
using DiskGolf.Disc;
using DiskGolf.UI.Callouts;
using UnityEngine;

namespace DiskGolf.UI
{
    [RequireComponent(typeof(ThrowSummaryPanel))]
    public sealed class ThrowSummaryBannerUI : MonoBehaviour
    {
        ThrowSummaryPanel _panel;

        ThrowSummaryPanel Panel => _panel ??= GetComponent<ThrowSummaryPanel>();

        public float DisplaySeconds => Panel.DisplaySeconds;

        public float SlideInSeconds => Panel.SlideInSeconds;

        public bool IsVisible => Panel.IsVisible;

        public static ThrowSummaryBannerUI Ensure()
        {
            var host = GameplayCalloutHost.Ensure();
            if (host?.ThrowSummary != null)
                return host.ThrowSummary;

            var canvas = HudCanvasUtility.FindHudCanvas();
            return canvas?.GetComponentInChildren<ThrowSummaryBannerUI>(true);
        }

        public static ThrowSummaryBannerUI CreateForScene(RectTransform hud) =>
            ThrowSummaryPanel.CreateForScene(hud)?.GetComponent<ThrowSummaryBannerUI>();

        public void ShowBriefly(int upcomingThrowNumber, PlayerCharacterProfile character, Action onDismissed = null) =>
            Panel.ShowBriefly(upcomingThrowNumber, character, onDismissed);

        public void Hide() => Panel.Hide();
    }
}

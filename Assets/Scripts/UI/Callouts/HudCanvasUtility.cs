using UnityEngine;

namespace DiskGolf.UI.Callouts
{
    public static class HudCanvasUtility
    {
        public const string HudCanvasName = "GameplayHUD";

        public static RectTransform FindHudCanvas()
        {
            var hud = GameObject.Find(HudCanvasName);
            return hud != null ? hud.GetComponent<RectTransform>() : null;
        }
    }
}

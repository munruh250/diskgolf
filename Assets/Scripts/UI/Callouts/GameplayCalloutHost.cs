using DiskGolf.UI;
using UnityEngine;

namespace DiskGolf.UI.Callouts
{
    public sealed class GameplayCalloutHost : MonoBehaviour
    {
        public const string RootName = "GameplayCallouts";

        [SerializeField] SpriteCalloutBanner lieLanding;

        [SerializeField] SpriteCalloutBanner onTheGreen;

        [SerializeField] TextCalloutBanner throwDistance;

        [SerializeField] SpriteCalloutBanner sweetSpot;

        [SerializeField] MultiSlotSpriteBanner holeComplete;

        [SerializeField] ThrowSummaryBannerUI throwSummary;

        [SerializeField] HoleCompleteCutsceneUI holeCutscene;

        public SpriteCalloutBanner LieLanding => lieLanding;

        public SpriteCalloutBanner OnTheGreen => onTheGreen;

        public TextCalloutBanner ThrowDistance => throwDistance;

        public SpriteCalloutBanner SweetSpot => sweetSpot;

        public MultiSlotSpriteBanner HoleComplete => holeComplete;

        public ThrowSummaryBannerUI ThrowSummary => throwSummary;

        public HoleCompleteCutsceneUI HoleCutscene => holeCutscene;

        public static GameplayCalloutHost Ensure(RectTransform hud = null)
        {
            hud ??= HudCanvasUtility.FindHudCanvas();
            if (hud == null)
                return null;

            var existing = hud.Find(RootName)?.GetComponent<GameplayCalloutHost>();
            if (existing != null)
            {
                existing.BindReferences();
                return existing;
            }

            return null;
        }

        public void BindReferences()
        {
            lieLanding ??= transform.Find("LieLandingBanner")?.GetComponent<SpriteCalloutBanner>();
            onTheGreen ??= transform.Find("OnTheGreenBanner")?.GetComponent<SpriteCalloutBanner>();
            throwDistance ??= transform.Find("ThrowResultBanner")?.GetComponent<TextCalloutBanner>();
            sweetSpot ??= transform.Find("SweetSpotBanner")?.GetComponent<SpriteCalloutBanner>();
            holeComplete ??= transform.Find("HoleCompleteBanner")?.GetComponent<MultiSlotSpriteBanner>();
            var throwSummaryBanner = transform.Find("ThrowSummaryBanner");
            throwSummary ??= throwSummaryBanner?.GetComponent<ThrowSummaryBannerUI>();
            holeCutscene ??= transform.Find("HoleCompleteCutscene")?.GetComponent<HoleCompleteCutsceneUI>();
        }
    }
}

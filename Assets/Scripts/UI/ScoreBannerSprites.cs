using DiskGolf.Core;
using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Score banner sprites from Art/UI.</summary>
    public static class ScoreBannerSprites
    {
        public static Sprite OnTheGreen => Load("ScoreBanner_OnTheGreen");

        public static Sprite Fairway => Load("ScoreBanner_Fairway");

        public static Sprite Rough => Load("ScoreBanner_Rough");

        public static HoleCompleteScoreKind ResolveKind(int strokes, int par)
        {
            if (strokes == 1)
                return HoleCompleteScoreKind.HoleInOne;

            return HoleScore.RelativeToPar(strokes, par) switch
            {
                <= -2 => HoleCompleteScoreKind.Eagle,
                -1 => HoleCompleteScoreKind.Birdie,
                0 => HoleCompleteScoreKind.Par,
                1 => HoleCompleteScoreKind.Bogey,
                2 => HoleCompleteScoreKind.DoubleBogey,
                3 => HoleCompleteScoreKind.TripleBogey,
                _ => HoleCompleteScoreKind.Awful,
            };
        }

        public static Sprite ResolveHoleResult(int strokes, int par) =>
            Load(FileNameForKind(ResolveKind(strokes, par)));

        public static string FileNameForKind(HoleCompleteScoreKind kind) => kind switch
        {
            HoleCompleteScoreKind.HoleInOne => "ScoreBanner_HoleInOne",
            HoleCompleteScoreKind.Eagle => "ScoreBanner_Eagle",
            HoleCompleteScoreKind.Birdie => "ScoreBanner_Birdie",
            HoleCompleteScoreKind.Par => "ScoreBanner_Par",
            HoleCompleteScoreKind.Bogey => "ScoreBanner_Bogey",
            HoleCompleteScoreKind.DoubleBogey => "ScoreBanner_DoubleBogey",
            HoleCompleteScoreKind.TripleBogey => "ScoreBanner_TripleBogey",
            HoleCompleteScoreKind.Awful => "ScoreBanner_Awful",
            _ => "ScoreBanner_Par",
        };

        public static Sprite LoadKind(HoleCompleteScoreKind kind) => Load(FileNameForKind(kind));

        static Sprite Load(string fileName)
        {
            var sprite = RuntimeArt.LoadScoreBannerSprite(fileName);
            if (sprite == null)
                Debug.LogWarning($"[Disk Golf] Score banner sprite not found: {fileName}");

            return sprite;
        }
    }
}

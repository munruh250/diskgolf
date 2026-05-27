namespace DiskGolf.Core
{
    public static class HoleScore
    {
        public static int RelativeToPar(int strokes, int par) => strokes - par;

        public static string ResultName(int strokes, int par)
        {
            if (strokes == 1)
                return "HOLE IN ONE";

            return RelativeToPar(strokes, par) switch
            {
                <= -3 => "ALBATROSS",
                -2 => "EAGLE",
                -1 => "BIRDIE",
                0 => "PAR",
                1 => "BOGEY",
                2 => "DOUBLE BOGEY",
                3 => "TRIPLE BOGEY",
                var over => $"+{over}",
            };
        }

        public static string VsParToken(int strokes, int par)
        {
            int diff = RelativeToPar(strokes, par);

            return diff switch
            {
                0 => "E",
                > 0 => $"+{diff}",
                _ => diff.ToString(),
            };
        }

        public static string InProgressLine(int strokes, int par) =>
            $"PAR {par}  ·  THROW {strokes}";

        public static string CompletedLine(int strokes, int par) =>
            $"{strokes} — {ResultName(strokes, par).ToUpperInvariant()}";
    }
}

using UnityEngine;

namespace DiskGolf.UI
{
    public static class ThrowSummaryLabels
    {
        public static string Format(int throwNumber)
        {
            int n = Mathf.Max(1, throwNumber);
            int mod100 = n % 100;
            string suffix = mod100 is >= 11 and <= 13
                ? "th"
                : (n % 10) switch
                {
                    1 => "st",
                    2 => "nd",
                    3 => "rd",
                    _ => "th",
                };

            return $"{n}{suffix} Throw";
        }
    }
}

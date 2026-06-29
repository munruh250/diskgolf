using System;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    /// <summary>Runtime lookup table for theme packs (lives under Resources/).</summary>
    [CreateAssetMenu(fileName = "ThemePackRegistry", menuName = "DiskGolf/Course/Theme Pack Registry")]
    public sealed class ThemePackRegistry : ScriptableObject
    {
        public ThemePack[] themes = Array.Empty<ThemePack>();

        public ThemePack Find(string themeId)
        {
            if (string.IsNullOrWhiteSpace(themeId) || themes == null)
                return null;

            var id = themeId.Trim();
            foreach (var theme in themes)
            {
                if (theme != null && string.Equals(theme.themeId, id, StringComparison.OrdinalIgnoreCase))
                    return theme;
            }

            return null;
        }
    }
}

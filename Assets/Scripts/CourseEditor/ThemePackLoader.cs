using System;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    public static class ThemePackLoader
    {
        const string RegistryResourcePath = "ThemePackRegistry";
        const string DefaultThemeId = "temperate";

        public static ThemePack Load(string themeId)
        {
            if (string.IsNullOrWhiteSpace(themeId))
                themeId = DefaultThemeId;

            themeId = themeId.Trim();

            var registry = Resources.Load<ThemePackRegistry>(RegistryResourcePath);
            var fromRegistry = registry?.Find(themeId);
            if (fromRegistry != null)
                return fromRegistry;

            var direct = Resources.Load<ThemePack>(GetDirectResourcePath(themeId));
            if (direct != null)
                return direct;

            if (!string.Equals(themeId, DefaultThemeId, StringComparison.OrdinalIgnoreCase))
                return Load(DefaultThemeId);

            Debug.LogWarning("[ThemePackLoader] Could not resolve temperate theme. Assign ThemePack_Temperate on Resources/ThemePackRegistry.");
            return null;
        }

        static string GetDirectResourcePath(string themeId)
        {
            if (string.Equals(themeId, DefaultThemeId, StringComparison.OrdinalIgnoreCase))
                return "Themes/ThemePack_Temperate";

            return $"Themes/ThemePack_{ToPascalCase(themeId)}";
        }

        static string ToPascalCase(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value.Length == 1)
                return char.ToUpperInvariant(value[0]).ToString();

            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }
    }
}

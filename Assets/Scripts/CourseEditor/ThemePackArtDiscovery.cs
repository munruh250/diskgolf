using System;
using System.Text;

namespace DiskGolf.CourseEditor
{
    /// <summary>Maps foliage/skybox asset file names to stable editor archetype ids.</summary>
    public static class ThemePackArtDiscovery
    {
        public static string ArchetypeIdFromSpriteName(string fileNameWithoutExtension)
        {
            if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
                return string.Empty;

            if (string.Equals(fileNameWithoutExtension, "Tree", StringComparison.OrdinalIgnoreCase))
                return "tree_round";
            if (string.Equals(fileNameWithoutExtension, "TreeRound", StringComparison.OrdinalIgnoreCase))
                return "tree_round_light";
            if (string.Equals(fileNameWithoutExtension, "TreeRoundDark", StringComparison.OrdinalIgnoreCase))
                return "tree_round_dark";

            return PascalCaseToSnakeCase(fileNameWithoutExtension);
        }

        public static string SkyboxIdFromMaterialName(string materialFileNameWithoutExtension)
        {
            if (string.IsNullOrWhiteSpace(materialFileNameWithoutExtension))
                return string.Empty;

            var name = materialFileNameWithoutExtension;
            if (name.StartsWith("MAT_", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(4);

            if (name.Contains("Overcast", StringComparison.OrdinalIgnoreCase))
                return "sky_overcast";
            if (name.Contains("Prototype", StringComparison.OrdinalIgnoreCase))
                return "sky_clear";

            return "sky_" + PascalCaseToSnakeCase(name);
        }

        public static string PascalCaseToSnakeCase(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var builder = new StringBuilder(value.Length + 4);
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                if (char.IsUpper(character) && i > 0)
                    builder.Append('_');

                builder.Append(char.ToLowerInvariant(character));
            }

            return builder.ToString();
        }
    }
}

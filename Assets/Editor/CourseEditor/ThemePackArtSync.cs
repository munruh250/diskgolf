#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using DiskGolf.CourseEditor;
using DiskGolf.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools.CourseEditor
{
    /// <summary>Syncs foliage sprites and skybox materials from art folders into the active theme pack.</summary>
    public static class ThemePackArtSync
    {
        [InitializeOnLoadMethod]
        static void ScheduleSync() => EditorApplication.delayCall += () => EnsureSynced(force: false);

        [MenuItem("Disk Golf/Course/Sync Theme Pack Art")]
        public static void SyncFromMenu() => EnsureSynced(force: true);

        public static void EnsureSynced(bool force = false)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var theme = AssetDatabase.LoadAssetAtPath<ThemePack>(ThemePackBootstrap.TemperateAssetPath);
            if (theme == null)
                theme = ThemePackBootstrap.EnsureTemperateThemePack();

            if (theme == null)
                return;

            if (!force && !NeedsSync(theme))
                return;

            if (SyncThemePack(theme))
            {
                EditorUtility.SetDirty(theme);
                AssetDatabase.SaveAssets();
                Debug.Log("[Disk Golf] Theme pack art synced from foliage/skybox folders.");
            }
        }

        public static bool SyncThemePack(ThemePack theme)
        {
            if (theme == null)
                return false;

            var foliage = DiscoverFoliageEntries();
            var skyboxes = DiscoverSkyboxEntries();

            bool changed = !EntryListsMatch(theme.foliage, foliage) || !SkyboxListsMatch(theme.skyboxes, skyboxes);
            theme.foliage.Clear();
            theme.foliage.AddRange(foliage);
            theme.skyboxes.Clear();
            theme.skyboxes.AddRange(skyboxes);
            return changed || foliage.Count > 0 || skyboxes.Count > 0;
        }

        static bool NeedsSync(ThemePack theme)
        {
            var foliage = DiscoverFoliageEntries();
            var skyboxes = DiscoverSkyboxEntries();
            return !EntryListsMatch(theme.foliage, foliage) || !SkyboxListsMatch(theme.skyboxes, skyboxes);
        }

        static List<FoliageArchetypeEntry> DiscoverFoliageEntries()
        {
            var entries = new List<FoliageArchetypeEntry>();
            var spritesRoot = ProjectArtPaths.Environment.Foliage.SpritesRoot;
            if (!AssetDatabase.IsValidFolder(spritesRoot))
                return entries;

            var seenIds = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { spritesRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                    continue;

                var fileName = Path.GetFileNameWithoutExtension(path);
                var archetypeId = ThemePackArtDiscovery.ArchetypeIdFromSpriteName(fileName);
                if (string.IsNullOrEmpty(archetypeId))
                    continue;

                if (!seenIds.Add(archetypeId))
                {
                    Debug.LogWarning(
                        $"[Disk Golf] Skipping duplicate foliage archetype '{archetypeId}' for sprite at {path}.");
                    continue;
                }

                entries.Add(new FoliageArchetypeEntry
                {
                    archetypeId = archetypeId,
                    sprite = sprite,
                    sortingOrder = 10
                });
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.archetypeId, b.archetypeId));
            return entries;
        }

        static List<SkyboxArchetypeEntry> DiscoverSkyboxEntries()
        {
            var entries = new List<SkyboxArchetypeEntry>();
            var skyboxRoot = ProjectArtPaths.Environment.Skybox.Root;
            if (!AssetDatabase.IsValidFolder(skyboxRoot))
                return entries;

            var seenIds = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { skyboxRoot }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".mat", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                    continue;

                var fileName = Path.GetFileNameWithoutExtension(path);
                if (!fileName.StartsWith("MAT_", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                var skyboxId = ThemePackArtDiscovery.SkyboxIdFromMaterialName(fileName);
                if (string.IsNullOrEmpty(skyboxId))
                    continue;

                if (!seenIds.Add(skyboxId))
                {
                    Debug.LogWarning(
                        $"[Disk Golf] Skipping duplicate skybox id '{skyboxId}' for material at {path}.");
                    continue;
                }

                entries.Add(new SkyboxArchetypeEntry
                {
                    skyboxId = skyboxId,
                    material = material,
                    preview = null
                });
            }

            entries.Sort((a, b) => string.CompareOrdinal(a.skyboxId, b.skyboxId));
            return entries;
        }

        static bool EntryListsMatch(List<FoliageArchetypeEntry> current, List<FoliageArchetypeEntry> discovered)
        {
            if (current == null || discovered == null)
                return false;

            if (current.Count != discovered.Count)
                return false;

            for (var i = 0; i < current.Count; i++)
            {
                var a = current[i];
                var b = discovered[i];
                if (a == null || b == null)
                    return false;

                if (!string.Equals(a.archetypeId, b.archetypeId, System.StringComparison.Ordinal)
                    || a.sprite != b.sprite)
                    return false;
            }

            return true;
        }

        static bool SkyboxListsMatch(List<SkyboxArchetypeEntry> current, List<SkyboxArchetypeEntry> discovered)
        {
            if (current == null || discovered == null)
                return false;

            if (current.Count != discovered.Count)
                return false;

            for (var i = 0; i < current.Count; i++)
            {
                var a = current[i];
                var b = discovered[i];
                if (a == null || b == null)
                    return false;

                if (!string.Equals(a.skyboxId, b.skyboxId, System.StringComparison.Ordinal)
                    || a.material != b.material)
                    return false;
            }

            return true;
        }
    }
}
#endif

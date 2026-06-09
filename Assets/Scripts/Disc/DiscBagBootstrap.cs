using UnityEngine;

namespace DiskGolf.Disc
{
    /// <summary>Ensures a DiscBag has default profiles when scene references are missing.</summary>
    public static class DiscBagBootstrap
    {
        public static void EnsurePopulated(DiscBag bag)
        {
            if (bag == null || !NeedsPopulation(bag))
                return;

            var loaded = new DiscProfile[DiscDefaults.AssetPaths.Length];
            for (int i = 0; i < DiscDefaults.AssetPaths.Length; i++)
                loaded[i] = LoadProfile(DiscDefaults.AssetPaths[i]);

            if (loaded[0] == null)
            {
                Debug.LogError(
                    "[Disk Golf] DiscBag has no disc profiles. Run Disk Golf → Course → Rebuild Course Editor Scene.");
                return;
            }

            bag.SetDiscs(loaded);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(bag);
#endif
        }

        public static bool NeedsPopulation(DiscBag bag)
        {
            var discs = bag.All;
            return discs == null || discs.Length == 0 || discs[0] == null;
        }

        static DiscProfile LoadProfile(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<DiscProfile>(path);
#else
            return null;
#endif
        }
    }
}

#if UNITY_EDITOR
using System.IO;
using DiskGolf.Disc;
using DiskGolf.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools
{
    public static class PlayerCharacterRosterBuilder
    {
        const string RosterPath = "Assets/Resources/PlayerCharacterRoster.asset";

        const string CharacterDir = "Assets/Data/Characters";

        [MenuItem("Disk Golf/Create Default Player Roster")]
        public static void CreateDefaultRoster()
        {
            Directory.CreateDirectory("Assets/Resources");
            Directory.CreateDirectory(CharacterDir);

            var profiles = new[]
            {
                CreateProfile("Jack", "The Young Hero", "Unruh", "USA", 78, 72, 76,
                    new Color(0.45f, 0.62f, 0.92f), "YoungHero"),
                CreateProfile("Rocky", "The Technician", "Phantom", "JPN", 68, 92, 74,
                    new Color(0.55f, 0.78f, 0.55f), "Technician"),
                CreateProfile("Rex", "Power Drive", "Hawk", "USA", 96, 58, 52,
                    new Color(0.92f, 0.42f, 0.28f), "PowerGolfer"),
                CreateProfile("Finn", "Putting Master", "Calloway", "USA", 62, 86, 88,
                    new Color(0.72f, 0.55f, 0.88f), "PuttingMaster"),
                CreateProfile("Sage", "The Trickster", "Vale", "SWE", 74, 78, 92,
                    new Color(0.35f, 0.72f, 0.82f), "Trickster"),
                CreateProfile("Gus", "The Veteran", "Mercer", "GER", 82, 80, 84,
                    new Color(0.62f, 0.58f, 0.48f), "Veteran"),
            };

            var roster = AssetDatabase.LoadAssetAtPath<PlayerCharacterRoster>(RosterPath);
            if (roster == null)
            {
                roster = ScriptableObject.CreateInstance<PlayerCharacterRoster>();
                AssetDatabase.CreateAsset(roster, RosterPath);
            }

            roster.characters = profiles;
            EditorUtility.SetDirty(roster);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Disk Golf] Player roster saved with {profiles.Length} characters at {RosterPath}");
        }

        static PlayerCharacterProfile CreateProfile(
            string firstName,
            string nickname,
            string lastName,
            string region,
            int power,
            int accuracy,
            int clutch,
            Color color,
            string assetKey)
        {
            var path = $"{CharacterDir}/Player_{assetKey}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<PlayerCharacterProfile>(path);
            if (existing != null)
            {
                ApplyProfile(existing, firstName, nickname, lastName, region, power, accuracy, clutch, color);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var profile = ScriptableObject.CreateInstance<PlayerCharacterProfile>();
            ApplyProfile(profile, firstName, nickname, lastName, region, power, accuracy, clutch, color);
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        static void ApplyProfile(
            PlayerCharacterProfile profile,
            string firstName,
            string nickname,
            string lastName,
            string region,
            int power,
            int accuracy,
            int clutch,
            Color color)
        {
            profile.firstName = firstName;
            profile.nickname = nickname;
            profile.lastName = lastName;
            profile.displayName = nickname;
            profile.regionLabel = region;
            profile.power = power;
            profile.accuracy = accuracy;
            profile.clutch = clutch;
            profile.portraitColor = color;
        }
    }
}
#endif

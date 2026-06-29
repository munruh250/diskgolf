using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    public sealed class HoleCatalogEntry
    {
        public string Id;
        public string DisplayName;
        public bool Published;
        public string LastEditedUtc;
    }

    public sealed class HoleDataCatalog
    {
        const string TrashFolderName = ".trash";

        readonly string root;

        public HoleDataCatalog(string rootDirectory)
        {
            root = rootDirectory;
            Directory.CreateDirectory(root);
        }

        public static HoleDataCatalog Player => new(Application.persistentDataPath + "/Courses");

        public IReadOnlyList<HoleCatalogEntry> ListEntries()
        {
            var entries = new List<HoleCatalogEntry>();

            if (!Directory.Exists(root))
                return entries;

            foreach (var jsonPath in Directory.GetFiles(root, "*.json"))
            {
                if (jsonPath.EndsWith(".meta.json", StringComparison.OrdinalIgnoreCase))
                    continue;

                string id = Path.GetFileNameWithoutExtension(jsonPath);
                string metaPath = MetaPath(id);
                HoleCatalogEntry entry;

                if (File.Exists(metaPath))
                {
                    var meta = JsonUtility.FromJson<HoleDataMeta>(File.ReadAllText(metaPath));
                    entry = new HoleCatalogEntry
                    {
                        Id = id,
                        DisplayName = meta.displayName,
                        Published = meta.published,
                        LastEditedUtc = meta.lastEditedUtc
                    };
                }
                else
                {
                    var data = HoleDataJson.LoadFromFile(jsonPath);
                    entry = new HoleCatalogEntry
                    {
                        Id = id,
                        DisplayName = data.Name,
                        Published = false,
                        LastEditedUtc = null
                    };
                }

                entries.Add(entry);
            }

            entries.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            return entries;
        }

        public void Save(HoleData data, bool published, string templateSource = null)
        {
            if (string.IsNullOrEmpty(data.Id))
                throw new ArgumentException("HoleData.Id must be set before saving.", nameof(data));

            string jsonPath = JsonPath(data.Id);
            HoleDataJson.SaveToFile(data, jsonPath);

            var meta = new HoleDataMeta
            {
                displayName = data.Name,
                published = published,
                lastEditedUtc = DateTime.UtcNow.ToString("o"),
                templateSource = templateSource
            };
            File.WriteAllText(MetaPath(data.Id), JsonUtility.ToJson(meta, true));
        }

        public HoleData Load(string id) => HoleDataJson.LoadFromFile(JsonPath(id));

        public bool IsDisplayNameTaken(string displayName, string excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return false;

            foreach (var entry in ListEntries())
            {
                if (!string.IsNullOrEmpty(excludeId)
                    && string.Equals(entry.Id, excludeId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.Equals(entry.DisplayName, displayName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public string MakeUniqueDisplayName(string proposedName, string excludeId = null)
        {
            string trimmed = string.IsNullOrWhiteSpace(proposedName) ? "Untitled Hole" : proposedName.Trim();
            if (!IsDisplayNameTaken(trimmed, excludeId))
                return trimmed;

            string stem = trimmed;
            int openParen = stem.LastIndexOf(" (", StringComparison.Ordinal);
            if (openParen > 0 && stem.EndsWith(")", StringComparison.Ordinal))
            {
                string suffix = stem.Substring(openParen + 2, stem.Length - openParen - 3);
                if (int.TryParse(suffix, out _))
                    stem = stem.Substring(0, openParen);
            }

            for (int i = 1; i < 1000; i++)
            {
                string candidate = $"{stem} ({i})";
                if (!IsDisplayNameTaken(candidate, excludeId))
                    return candidate;
            }

            return $"{stem} ({Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture).Substring(0, 8)})";
        }

        public void Delete(string id)
        {
            string trashRoot = Path.Combine(root, TrashFolderName);
            Directory.CreateDirectory(trashRoot);

            MoveToTrash(JsonPath(id), trashRoot);
            MoveToTrash(MetaPath(id), trashRoot);
        }

        static void MoveToTrash(string sourcePath, string trashRoot)
        {
            if (!File.Exists(sourcePath))
                return;

            string fileName = Path.GetFileName(sourcePath);
            string destPath = Path.Combine(trashRoot, fileName);

            if (File.Exists(destPath))
                File.Delete(destPath);

            File.Move(sourcePath, destPath);
        }

        string JsonPath(string id) => Path.Combine(root, id + ".json");

        string MetaPath(string id) => Path.Combine(root, id + ".meta.json");
    }
}

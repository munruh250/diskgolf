using System.IO;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    public static class HoleDataJson
    {
        public static string ToJson(HoleData data, bool prettyPrint = true)
        {
            var dto = HoleDataDto.FromDomain(data);
            return JsonUtility.ToJson(dto, prettyPrint);
        }

        public static HoleData FromJson(string json)
        {
            var dto = JsonUtility.FromJson<HoleDataDto>(json);
            return dto?.ToDomain() ?? new HoleData();
        }

        public static void SaveToFile(HoleData data, string absolutePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            File.WriteAllText(absolutePath, ToJson(data));
        }

        public static HoleData LoadFromFile(string absolutePath)
        {
            return FromJson(File.ReadAllText(absolutePath));
        }
    }
}

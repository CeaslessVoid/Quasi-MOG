using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RoomGen
{
    public static class RoomLibrary
    {
        public static string RoomsFolder => Path.Combine(Application.streamingAssetsPath, "Rooms");

        private static List<RoomTemplate> _cache;

        public static void Save(RoomData data)
        {
            Directory.CreateDirectory(RoomsFolder);
            string json = JsonUtility.ToJson(data, true);
            string path = Path.Combine(RoomsFolder, SanitizeFileName(data.templateId) + ".json");
            File.WriteAllText(path, json);
            InvalidateCache();
        }

        public static RoomData Load(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<RoomData>(json);
        }

        public static List<string> ListRoomFiles()
        {
            if (!Directory.Exists(RoomsFolder)) return new List<string>();
            var files = new List<string>(Directory.GetFiles(RoomsFolder, "*.json"));
            files.Sort();
            return files;
        }

        public static List<RoomTemplate> LoadAll()
        {
            if (_cache != null) return _cache;

            var result = new List<RoomTemplate>();
            foreach (var file in ListRoomFiles())
                result.Add(RoomTemplate.FromRoomData(Load(file)));

            _cache = result;
            return _cache;
        }

        public static void InvalidateCache() => _cache = null;

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "UnnamedRoom";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}

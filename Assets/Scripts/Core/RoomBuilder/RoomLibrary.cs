using System;
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
            return Normalize(JsonUtility.FromJson<RoomData>(json));
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
            {
                try
                {
                    result.Add(RoomTemplate.FromRoomData(Load(file)));
                }
                catch (Exception e)
                {
                    Debug.LogError($"RoomLibrary: failed to load '{file}': {e.Message}");
                }
            }

            _cache = result;
            return _cache;
        }

        public static RoomTemplate GetByTemplateId(string templateId)
        {
            if (string.IsNullOrEmpty(templateId)) return null;
            foreach (var template in LoadAll())
                if (template.data.templateId == templateId) return template;
            return null;
        }

        public static void InvalidateCache() => _cache = null;

        private static RoomData Normalize(RoomData d)
        {
            d.width = Mathf.Max(3, d.width);
            d.height = Mathf.Max(3, d.height);
            int n = d.CellCount;

            d.floorLayer = Fit(d.floorLayer, n);
            d.normalLayer = Fit(d.normalLayer, n);
            d.connectorLayer = Fit(d.connectorLayer, n);
            d.wallDefLayer = Fit(d.wallDefLayer, n);
            d.doorDefLayer = Fit(d.doorDefLayer, n);
            d.floorDefLayer = Fit(d.floorDefLayer, n);

            d.typeTags ??= new List<string>();
            d.zoneTags ??= new List<string>();
            d.props ??= new List<PropPlacement>();
            return d;
        }

        private static T[] Fit<T>(T[] source, int count) => source != null && source.Length == count ? source : new T[count];

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "UnnamedRoom";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}

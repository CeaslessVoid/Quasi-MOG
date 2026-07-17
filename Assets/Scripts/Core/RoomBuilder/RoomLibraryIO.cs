using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RoomGen
{
    /// <summary>
    /// Persists RoomBuilderState to/from JSON files. Uses Application.persistentDataPath
    /// so it works identically in the editor and in actual builds (an in-game tool has to
    /// save somewhere that exists at runtime, not just inside the editor's Assets folder).
    /// </summary>
    public static class RoomLibraryIO
    {
        // StreamingAssets (not Application.persistentDataPath) so the room library actually
        // ships inside the build - Unity guarantees files under Assets/StreamingAssets are
        // copied verbatim, unlike arbitrary loose files elsewhere under Assets. In the editor
        // this resolves to <project>/Assets/StreamingAssets/Rooms.
        // Note: StreamingAssets is effectively read-only on some platforms (Android/WebGL/iOS).
        // Fine for a PC build, worth knowing if this ever targets those platforms.
        public static string RoomsFolder => Path.Combine(Application.streamingAssetsPath, "Rooms");

        public static void Save(RoomBuilderState state)
        {
            Directory.CreateDirectory(RoomsFolder);
            string json = JsonUtility.ToJson(state, true);
            string path = Path.Combine(RoomsFolder, SanitizeFileName(state.templateId) + ".json");
            File.WriteAllText(path, json);
        }

        public static RoomBuilderState Load(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<RoomBuilderState>(json);
        }

        public static List<string> ListRoomFiles()
        {
            if (!Directory.Exists(RoomsFolder)) return new List<string>();
            var files = new List<string>(Directory.GetFiles(RoomsFolder, "*.json"));
            files.Sort();
            return files;
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "UnnamedRoom";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}

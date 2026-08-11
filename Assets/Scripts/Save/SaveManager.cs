using System;
using System.IO;
using UnityEngine;

namespace Save
{
    public static class SaveManager
    {
        public const int SlotCount = 3;

        private static string FolderPath => Path.Combine(Application.persistentDataPath, "Saves");
        private static string SlotPath(int index) => Path.Combine(FolderPath, $"slot_{index}.json");

        public static SaveSlotInfo[] GetSlots()
        {
            var result = new SaveSlotInfo[SlotCount];
            for (int i = 0; i < SlotCount; i++)
                result[i] = ReadSlot(i);
            return result;
        }

        private static SaveSlotInfo ReadSlot(int index)
        {
            string path = SlotPath(index);
            if (!File.Exists(path)) return new SaveSlotInfo { slotIndex = index, hasSave = false };

            try
            {
                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveSlotInfo>(json);
                data.slotIndex = index;
                data.hasSave = true;
                return data;
            }
            catch
            {
                return new SaveSlotInfo { slotIndex = index, hasSave = false };
            }
        }

        public static void CreateNewGame(int index, string saveName)
        {
            Directory.CreateDirectory(FolderPath);
            var info = new SaveSlotInfo
            {
                slotIndex = index,
                hasSave = true,
                saveName = string.IsNullOrWhiteSpace(saveName) ? $"Save {index + 1}" : saveName,
                lastPlayedUtc = DateTime.UtcNow.ToString("O")
            };
            File.WriteAllText(SlotPath(index), JsonUtility.ToJson(info));
        }

        public static void TouchSlot(int index)
        {
            var info = ReadSlot(index);
            if (!info.hasSave) return;
            info.lastPlayedUtc = DateTime.UtcNow.ToString("O");
            File.WriteAllText(SlotPath(index), JsonUtility.ToJson(info));
        }

        public static void DeleteSlot(int index)
        {
            string path = SlotPath(index);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}

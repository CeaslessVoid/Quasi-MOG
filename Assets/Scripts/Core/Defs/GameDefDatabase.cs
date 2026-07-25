using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameDefs
{
    [CreateAssetMenu(fileName = "NewGameDefDatabase", menuName = "Defs/Game Def Database")]
    public class GameDefDatabase : ScriptableObject
    {
        [SerializeField] private List<WallDef> wallDefs = new List<WallDef>();
        [SerializeField] private List<FloorDef> floorDefs = new List<FloorDef>();
        [SerializeField] private List<DoorDef> doorDefs = new List<DoorDef>();

        public void EnsureInitialized()
        {
            DefDatabase<WallDef>.Initialize(wallDefs);
            DefDatabase<FloorDef>.Initialize(floorDefs);
            DefDatabase<DoorDef>.Initialize(doorDefs);
        }

#if UNITY_EDITOR
        [ContextMenu("Scan Assets/Defs")]
        public void EditorScanDefs()
        {
            wallDefs = FindAll<WallDef>();
            floorDefs = FindAll<FloorDef>();
            doorDefs = FindAll<DoorDef>();

            EditorUtility.SetDirty(this);
            Debug.Log($"GameDefDatabase: found {wallDefs.Count} wall defs, {floorDefs.Count} floor defs, {doorDefs.Count} door defs.");
        }

        private static List<T> FindAll<T>() where T : Def
        {
            var result = new List<T>();
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/Defs" });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) result.Add(asset);
            }
            return result;
        }
#endif
    }
}

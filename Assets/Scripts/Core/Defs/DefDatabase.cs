using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameDefs
{
    [CreateAssetMenu(fileName = "NewDefDatabase", menuName = "Defs/Def Database")]
    public class DefDatabase : ScriptableObject
    {
        [SerializeField] private List<Def> defs = new List<Def>();

        private static DefDatabase _instance;
        private static bool _warnedMissingInstance;
        private readonly Dictionary<Type, Dictionary<string, Def>> _lookup = new Dictionary<Type, Dictionary<string, Def>>();
        private readonly Dictionary<Type, object> _allCacheTyped = new Dictionary<Type, object>();

        public void EnsureInitialized()
        {
            _instance = this;
            _warnedMissingInstance = false;
            _lookup.Clear();
            _allCacheTyped.Clear();

            foreach (var def in defs)
            {
                if (def == null || string.IsNullOrEmpty(def.DefName)) continue;
                var type = def.GetType();
                if (!_lookup.TryGetValue(type, out var dict))
                {
                    dict = new Dictionary<string, Def>();
                    _lookup[type] = dict;
                }
                dict[def.DefName] = def;
            }
        }

        public static T Get<T>(string defName) where T : Def
        {
            if (_instance == null)
            {
                if (!_warnedMissingInstance)
                {
                    Debug.LogWarning("DefDatabase: no instance initialized yet. Ensure GameManager (or another EnsureInitialized() call) runs before defs are queried.");
                    _warnedMissingInstance = true;
                }
                return null;
            }

            if (string.IsNullOrEmpty(defName)) return null;
            if (_instance._lookup.TryGetValue(typeof(T), out var dict) && dict.TryGetValue(defName, out var def))
                return (T)def;
            return null;
        }

        public static bool TryGet<T>(string defName, out T def) where T : Def
        {
            def = Get<T>(defName);
            return def != null;
        }

        public static IReadOnlyList<T> All<T>() where T : Def
        {
            return _instance == null ? Array.Empty<T>() : _instance.GetAllCached<T>();
        }

        private List<T> GetAllCached<T>() where T : Def
        {
            var type = typeof(T);
            if (_allCacheTyped.TryGetValue(type, out var cached)) return (List<T>)cached;
            var list = defs.OfType<T>().ToList();
            _allCacheTyped[type] = list;
            return list;
        }

#if UNITY_EDITOR
        [ContextMenu("Scan Assets/Defs")]
        private void ScanDefs()
        {
            defs = FindAllDefs();
            EditorUtility.SetDirty(this);
            Debug.Log($"DefDatabase: found {defs.Count} defs.");
        }

        private static List<Def> FindAllDefs()
        {
            var result = new List<Def>();
            var guids = AssetDatabase.FindAssets("t:Def", new[] { "Assets/Defs" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Def>(path);
                if (asset != null) result.Add(asset);
            }
            return result;
        }
#endif
    }
}
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
        private const string ResourcePath = "DefDatabase";

        [SerializeField] private List<Def> defs = new List<Def>();

        private static DefDatabase _instance;
        private readonly Dictionary<Type, Dictionary<string, Def>> _lookup = new Dictionary<Type, Dictionary<string, Def>>();
        private readonly Dictionary<Type, object> _allCacheTyped = new Dictionary<Type, object>();

        private static DefDatabase Instance
        {
            get
            {
                if (_instance == null) LoadAndInitialize();
                return _instance;
            }
        }

        public static void WarmUp()
        {
            if (_instance == null) LoadAndInitialize();
        }

        private static void LoadAndInitialize()
        {
            var loaded = Resources.Load<DefDatabase>(ResourcePath);
            if (loaded == null)
            {
                Debug.LogError($"DefDatabase: no asset found at Resources/{ResourcePath}. Defs will not resolve.");
                return;
            }
            loaded.EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            _instance = this;
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
            var db = Instance;
            if (db == null || string.IsNullOrEmpty(defName)) return null;
            return db.GetInternal<T>(defName);
        }

        private T GetInternal<T>(string defName) where T : Def
        {
            if (_lookup.TryGetValue(typeof(T), out var exact) && exact.TryGetValue(defName, out var exactDef))
                return (T)exactDef;

            foreach (var kvp in _lookup)
            {
                if (!typeof(T).IsAssignableFrom(kvp.Key)) continue;
                if (kvp.Value.TryGetValue(defName, out var def)) return (T)def;
            }
            return null;
        }

        public static bool TryGet<T>(string defName, out T def) where T : Def
        {
            def = Get<T>(defName);
            return def != null;
        }

        public static IReadOnlyList<T> All<T>() where T : Def
        {
            var db = Instance;
            return db == null ? Array.Empty<T>() : db.GetAllCached<T>();
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
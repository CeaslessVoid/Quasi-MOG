using System.Collections.Generic;

namespace GameDefs
{
    public static class DefDatabase<T> where T : Def
    {
        private static readonly Dictionary<string, T> _lookup = new Dictionary<string, T>();
        private static readonly List<T> _all = new List<T>();

        public static IReadOnlyList<T> All => _all;

        public static void Initialize(IEnumerable<T> defs)
        {
            _lookup.Clear();
            _all.Clear();

            foreach (var def in defs)
            {
                if (def == null || string.IsNullOrEmpty(def.DefName)) continue;
                _lookup[def.DefName] = def;
                _all.Add(def);
            }
        }

        public static T Get(string defName)
        {
            if (string.IsNullOrEmpty(defName)) return null;
            return _lookup.TryGetValue(defName, out var def) ? def : null;
        }

        public static bool TryGet(string defName, out T def)
        {
            if (string.IsNullOrEmpty(defName))
            {
                def = null;
                return false;
            }
            return _lookup.TryGetValue(defName, out def);
        }
    }
}

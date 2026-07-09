using System.Collections.Generic;
using UnityEngine;

namespace BattleAngel.Rooms
{
    /// Author this asset once (e.g. Assets/Data/PropCatalog.asset) and list every prop
    /// sprite you have — doors, crates, cover objects, terminals, decals — with a stable
    /// string id and the sprite index it maps to in the instanced renderer's texture array.
    [CreateAssetMenu(fileName = "PropCatalog", menuName = "BattleAngel/Prop Catalog")]
    public class PropCatalogAsset : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string propId;
            public int spriteIndex;
        }

        public Entry[] entries;
    }

    /// Static wrapper so gameplay/generation code can resolve a propId without threading
    /// the catalog asset reference through every call site. Call Load() once at game boot.
    public static class PropCatalog
    {
        private static Dictionary<string, int> lookup;

        public static void Load(PropCatalogAsset asset)
        {
            lookup = new Dictionary<string, int>();
            foreach (var e in asset.entries)
            {
                lookup[e.propId] = e.spriteIndex;
            }
        }

        public static int GetSpriteIndex(string propId)
        {
            if (lookup != null && lookup.TryGetValue(propId, out int idx))
            {
                return idx;
            }
            Debug.LogWarning($"PropCatalog: unknown propId '{propId}', falling back to sprite 0.");
            return 0;
        }
    }
}

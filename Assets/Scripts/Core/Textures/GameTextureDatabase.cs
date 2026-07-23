using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameTexture
{
    [CreateAssetMenu(fileName = "NewGameTextureDatabase", menuName = "Textures/Game Texture Database")]
    public class GameTextureDatabase : ScriptableObject
    {
        [SerializeField] private List<TextureRef> assets = new List<TextureRef>();
        private Dictionary<string, TextureRef> _lookup;

        private void OnEnable() => BuildLookup();

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, TextureRef>(assets.Count);
            foreach (var a in assets)
            {
                if (a == null || string.IsNullOrEmpty(a.Id)) continue;
                _lookup[a.Id] = a;
            }
        }

        public T Get<T>(string id) where T : TextureRef
        {
            if (_lookup == null) BuildLookup();
            if (string.IsNullOrEmpty(id)) return null;
            return _lookup.TryGetValue(id, out var asset) ? asset as T : null;
        }

#if UNITY_EDITOR
        [ContextMenu("Scan Assets/Textures")]
        public void EditorScanTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:TextureRef", new[] { "Assets/Textures" });

            var found = new List<TextureRef>(guids.Length);

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var texRef = AssetDatabase.LoadAssetAtPath<TextureRef>(path);
                if (texRef != null)
                    found.Add(texRef);
            }

            assets = found;
            BuildLookup();

            EditorUtility.SetDirty(this);
            Debug.Log($"GameTextureDatabase: Found {found.Count} TextureRef assets.");
        }
#endif
    }
}

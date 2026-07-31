using UnityEngine;

namespace GameDefs
{
    public abstract class Def : ScriptableObject
    {
        [SerializeField] private string defName;
        [SerializeField] private Color tintColor = Color.white;
        [SerializeField] private Color secondaryTintColor = Color.white;
        [SerializeField] private Texture2D maskTexture;

        public string DefName => defName;
        public Color TintColor => tintColor;
        public Color SecondaryTintColor => secondaryTintColor;
        public Texture2D MaskTexture => maskTexture;
        public bool HasMask => maskTexture != null;
    }
}
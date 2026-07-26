using UnityEngine;

namespace GameDefs
{
    [CreateAssetMenu(fileName = "NewWallDef", menuName = "Defs/Wall Def")]
    public class WallDef : BlockerDef
    {
        [SerializeField] private WallTextureElement textureElement;
        [SerializeField] private Color tintColor = Color.white;

        public Color TintColor => tintColor;
        public bool HasTexture => textureElement != null && textureElement.HasSprites;

        public Sprite GetSprite(int bitmask) => textureElement != null ? textureElement.GetSprite(bitmask) : null;
    }
}
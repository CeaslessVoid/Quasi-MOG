using UnityEngine;

namespace GameDefs
{
    [CreateAssetMenu(fileName = "NewWallDef", menuName = "Defs/Wall Def")]
    public class WallDef : BlockerDef
    {
        [SerializeField] private WallTextureElement textureElement;

        public bool HasTexture => textureElement != null && textureElement.HasSprites;

        public Sprite GetSprite(int bitmask) => textureElement != null ? textureElement.GetSprite(bitmask) : null;
    }
}
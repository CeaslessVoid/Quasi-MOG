using UnityEngine;

namespace GameDefs
{
    [CreateAssetMenu(fileName = "NewWallDef", menuName = "Defs/Wall Def")]
    public class WallDef : Def
    {
        [SerializeField] private WallTextureElement textureElement;
        [SerializeField] private Color tintColor = Color.white;

        [Header("Physics")]
        [SerializeField] private PhysicsMaterial2D physicsMaterial;
        [SerializeField] private bool blocksProjectiles = true;

        public Color TintColor => tintColor;
        public PhysicsMaterial2D PhysicsMaterial => physicsMaterial;
        public bool BlocksProjectiles => blocksProjectiles;
        public bool HasTexture => textureElement != null && textureElement.HasSprites;

        public Sprite GetSprite(int bitmask) => textureElement != null ? textureElement.GetSprite(bitmask) : null;
    }
}

using UnityEngine;

namespace GameDefs
{
    [CreateAssetMenu(fileName = "NewWallTextureElement", menuName = "Defs/Wall Texture Element")]
    public class WallTextureElement : ScriptableObject
    {
        [SerializeField] private Sprite[] sprites = new Sprite[16];

        public bool HasSprites => sprites != null && sprites.Length > 0;

        public Sprite GetSprite(int bitmask) => RoomGen.WallAtlas.Lookup(sprites, bitmask);
    }
}

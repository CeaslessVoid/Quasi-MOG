using UnityEngine;
using UnityEngine.Tilemaps;
using RoomGen;

namespace GameTexture
{
    [CreateAssetMenu(fileName = "NewWallTexture", menuName = "Textures/Wall Texture")]
    public class WallTexture : TextureRef
    {
        [SerializeField] private Sprite[] sprites = new Sprite[16];

        private Tile[] _tiles;

        public Sprite GetSprite(int bitmask) => WallAtlas.Lookup(sprites, bitmask);

        public Tile GetTile(int bitmask)
        {
            EnsureTiles();
            return WallAtlas.Lookup(_tiles, bitmask);
        }

        private void EnsureTiles()
        {
            if (_tiles != null) return;
            _tiles = new Tile[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                var tile = CreateInstance<Tile>();
                tile.sprite = sprites[i];
                tile.colliderType = Tile.ColliderType.None;
                _tiles[i] = tile;
            }
        }
    }
}
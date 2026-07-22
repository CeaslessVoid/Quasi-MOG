using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoomGen
{
    public class WallAtlas
    {
        public const int North = 1, East = 2, South = 4, West = 8;

        private static readonly int[] BitmaskToIndex =
        {
            /*  0 none        */ 12,
            /*  1 N            */ 13,
            /*  2 E            */ 14,
            /*  3 N+E          */ 15,
            /*  4 S            */ 8,
            /*  5 N+S          */ 9,
            /*  6 E+S          */ 10,
            /*  7 N+E+S        */ 11,
            /*  8 W            */ 4,
            /*  9 N+W          */ 5,
            /* 10 E+W          */ 6,
            /* 11 N+E+W        */ 7,
            /* 12 S+W          */ 0,
            /* 13 N+S+W        */ 1,
            /* 14 E+S+W        */ 2,
            /* 15 N+E+S+W      */ 3,
        };

        private readonly Sprite[] _sprites;
        private readonly TileBase[] _tiles;

        public WallAtlas(TileBase[] tiles)
        {
            _tiles = tiles ?? new TileBase[16];
            _sprites = new Sprite[_tiles.Length];
            for (int i = 0; i < _tiles.Length; i++)
                _sprites[i] = (_tiles[i] as Tile)?.sprite;
        }

        /// <summary>
        /// Build from 16 pre-cropped Sprite sub-assets instead of Tile assets. A matching
        /// Tile is created at runtime wrapping each one purely so Tilemap has something to
        /// place - the crop/pivot/PPU is entirely whatever's already baked into the Sprite,
        /// no pixel math happens here.
        /// </summary>
        public WallAtlas(Sprite[] sprites)
        {
            _sprites = sprites ?? new Sprite[16];
            _tiles = new TileBase[_sprites.Length];
            for (int i = 0; i < _sprites.Length; i++)
            {
                if (_sprites[i] == null) continue;
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = _sprites[i];
                tile.colliderType = Tile.ColliderType.None;
                _tiles[i] = tile;
            }
        }

        public static int ComputeBitmask(bool north, bool east, bool south, bool west)
        {
            int mask = 0;
            if (north) mask |= North;
            if (east) mask |= East;
            if (south) mask |= South;
            if (west) mask |= West;
            return mask;
        }

        public Sprite GetSprite(int bitmask)
        {
            if (_sprites == null || _sprites.Length == 0) return null;
            return _sprites[BitmaskToIndex[Mathf.Clamp(bitmask, 0, 15)]];
        }

        public TileBase GetTile(int bitmask)
        {
            if (_tiles == null || _tiles.Length == 0) return null;
            return _tiles[BitmaskToIndex[Mathf.Clamp(bitmask, 0, 15)]];
        }
    }
}

using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoomGen
{
    public class WallAtlas
    {
        public const int North = 1, East = 2, South = 4, West = 8;

        private static readonly int[] BitmaskToGridIndex =
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
        private readonly Tile[] _tiles;

        public WallAtlas(Texture2D atlasTexture, int columns, int rows, float pixelsPerUnit)
        {
            _sprites = new Sprite[columns * rows];
            _tiles = new Tile[columns * rows];

            if (atlasTexture == null)
            {
                Debug.LogError("WallAtlas: no atlas texture assigned - walls will be invisible.");
                return;
            }

            int tileW = atlasTexture.width / columns;
            int tileH = atlasTexture.height / rows;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    int gridIndex = row * columns + col;
                    int texRow = rows - 1 - row;
                    var rect = new Rect(col * tileW, texRow * tileH, tileW, tileH);
                    _sprites[gridIndex] = Sprite.Create(atlasTexture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
                }
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
            if (_sprites.Length == 0) return null;
            return _sprites[BitmaskToGridIndex[Mathf.Clamp(bitmask, 0, 15)]];
        }

        public TileBase GetTile(int bitmask)
        {
            if (_tiles.Length == 0) return null;
            int gridIndex = BitmaskToGridIndex[Mathf.Clamp(bitmask, 0, 15)];

            if (_tiles[gridIndex] == null)
            {
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = _sprites[gridIndex];
                tile.colliderType = Tile.ColliderType.None;
                _tiles[gridIndex] = tile;
            }
            return _tiles[gridIndex];
        }
    }
}

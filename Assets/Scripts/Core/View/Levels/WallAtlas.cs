using UnityEngine;

namespace RoomGen
{
    /// <summary>
    /// Slices a 4x4 "linked wall" atlas texture into 16 sprites and maps a 4-bit
    /// North/East/South/West neighbor-connection bitmask to the correct one. Plain C#
    /// class (not a MonoBehaviour) - each visual layer (builder, level view) owns its own
    /// instance, built from a Texture2D reference in its own inspector.
    /// </summary>
    public class WallAtlas
    {
        public const int North = 1, East = 2, South = 4, West = 8;

        /// <summary>
        /// Maps a bitmask (0-15) to a grid index in the atlas, where grid index counts
        /// tiles left-to-right then top-to-bottom starting at 0 (top-left), same as you'd
        /// read the reference image.
        ///
        /// This was read by hand off the arrow-annotated reference image, matching each
        /// tile's visible arrows to which of North/East/South/West it connects. This is
        /// the one part of the whole texture system I can't fully guarantee without seeing
        /// it rendered - a couple of entries could plausibly be swapped. If a wall corner
        /// looks wrong once it's running: figure out which sides SHOULD connect there, add
        /// up North=1 + East=2 + South=4 + West=8 to get the bitmask, then just change that
        /// array slot below to the correct grid index (counting tiles left-to-right,
        /// top-to-bottom from 0, off the reference image). That's the entire fix.
        /// </summary>
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

        public WallAtlas(Texture2D atlasTexture, int columns, int rows, float pixelsPerUnit)
        {
            _sprites = new Sprite[columns * rows];

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
                    // Texture space is bottom-up; visual row 0 (top of the reference
                    // image) is the LAST row in texture pixel coordinates.
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
    }
}

using UnityEngine;
using UnityEngine.Tilemaps;
using BattleAngel.Grid;

namespace BattleAngel.Rendering
{
    /// <summary>
    /// The only place that touches Unity's Tilemap/TileBase directly. GridManager stores
    /// floor/wall tiles as plain ints; this component translates those ints into actual
    /// tiles on two Tilemaps (floor, wall) via a palette array indexed by id. Unity's
    /// Tilemap renderer already batches and culls efficiently, so we lean on it rather
    /// than reinventing tile rendering.
    /// </summary>
    public class TilemapVisualSync : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private Tilemap floorTilemap;
        [SerializeField] private Tilemap wallTilemap;

        // Index 0 is reserved for "empty" in both arrays — leave element 0 null.
        [SerializeField] private TileBase[] floorPalette;
        [SerializeField] private TileBase[] wallPalette;

        public void PaintRegion(RectInt region)
        {
            for (int y = region.yMin; y < region.yMax; y++)
            {
                for (int x = region.xMin; x < region.xMax; x++)
                {
                    if (!gridManager.InBounds(x, y)) continue;

                    var cell = gridManager.CellAt(x, y);
                    var pos = new Vector3Int(x, y, 0);

                    floorTilemap.SetTile(pos, ResolveTile(floorPalette, cell.floorTileId));
                    wallTilemap.SetTile(pos, ResolveTile(wallPalette, cell.wallTileId));
                }
            }
        }

        public void PaintAll()
        {
            PaintRegion(new RectInt(0, 0, gridManager.Width, gridManager.Height));
        }

        public void ClearAll()
        {
            floorTilemap.ClearAllTiles();
            wallTilemap.ClearAllTiles();
        }

        private static TileBase ResolveTile(TileBase[] palette, int id)
        {
            if (id <= 0 || palette == null || id >= palette.Length) return null;
            return palette[id];
        }
    }
}

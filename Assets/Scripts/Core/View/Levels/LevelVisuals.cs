using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoomGen
{
    /// <summary>
    /// Renders a generated LevelGrid with real textures - visible in the actual game
    /// camera at runtime/in builds, not just Scene-view gizmos. Uses Unity's Tilemap
    /// rather than a GameObject+SpriteRenderer per cell: a generated level can easily be
    /// thousands of cells across dozens of rooms, and Tilemap batches all of that into a
    /// small number of chunked meshes instead of spawning one object per tile.
    ///
    /// Consumes already-correct assets rather than slicing any raw texture itself - assign
    /// EITHER Wall Tiles (16 Tile assets, e.g. straight from an existing Tile Palette) OR
    /// Wall Sprites (16 pre-cropped Sprite sub-assets) below, whichever you have. Same for
    /// the floor - a single pre-made Sprite, not a raw texture.
    ///
    /// Doors currently render as an opening (no wall tile, floor shows through) rather than
    /// dedicated door art - "ignore doors" per this texture pass. A door does NOT count as
    /// wall-like for a neighboring wall's autotile bitmask - it's an opening, so the wall
    /// next to it shows its exposed edge rather than acting like the wall continues through.
    /// </summary>
    public class LevelVisuals : MonoBehaviour
    {
        [Header("Wall Tiles - assign ONE of the two below (16 entries, left-to-right/top-to-bottom off the reference image)")]
        [SerializeField] private TileBase[] wallTiles = new TileBase[16];
        [SerializeField] private Sprite[] wallSprites = new Sprite[16];

        [Header("Floor")]
        [Tooltip("Pre-made floor Sprite. Leave unassigned to keep the flat debug color.")]
        [SerializeField] private Sprite floorSprite;

        [SerializeField] private float cellSize = 1f;

        private WallAtlas _wallAtlas;
        private Tile _floorTile;
        private Tilemap _floorTilemap;
        private Tilemap _wallTilemap;
        private bool _initialized;

        /// <summary>Lets a bootstrap assign assets when creating this component in code. Unity calls Awake() synchronously during AddComponent, before a caller has a chance to call this - so initialization is idempotent and safe to re-run here.</summary>
        public void Configure(TileBase[] tiles, Sprite[] sprites, Sprite floor)
        {
            wallTiles = tiles;
            wallSprites = sprites;
            floorSprite = floor;
            _initialized = false;
            EnsureInitialized();
        }

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            _wallAtlas = HasAny(wallTiles) ? new WallAtlas(wallTiles) : new WallAtlas(wallSprites);

            if (floorSprite != null)
            {
                _floorTile = ScriptableObject.CreateInstance<Tile>();
                _floorTile.sprite = floorSprite;
                _floorTile.colliderType = Tile.ColliderType.None;
            }

            if (_floorTilemap == null)
            {
                var gridGO = new GameObject("LevelGrid");
                gridGO.transform.SetParent(transform, false);
                var grid = gridGO.AddComponent<Grid>();
                grid.cellSize = new Vector3(cellSize, cellSize, 1f);

                var floorGO = new GameObject("FloorTilemap");
                floorGO.transform.SetParent(gridGO.transform, false);
                _floorTilemap = floorGO.AddComponent<Tilemap>();
                var floorRenderer = floorGO.AddComponent<TilemapRenderer>();
                floorRenderer.sortingOrder = 0;

                var wallGO = new GameObject("WallTilemap");
                wallGO.transform.SetParent(gridGO.transform, false);
                _wallTilemap = wallGO.AddComponent<Tilemap>();
                var wallRenderer = wallGO.AddComponent<TilemapRenderer>();
                wallRenderer.sortingOrder = 5;
            }
        }

        private static bool HasAny<T>(T[] arr) where T : class
        {
            if (arr == null) return false;
            foreach (var x in arr) if (x != null) return true;
            return false;
        }

        public void Rebuild(LevelGrid grid)
        {
            EnsureInitialized();
            _floorTilemap.ClearAllTiles();
            _wallTilemap.ClearAllTiles();
            if (grid == null) return;

            foreach (var room in grid.PlacedRooms)
            {
                for (int y = 0; y < room.template.height; y++)
                {
                    for (int x = 0; x < room.template.width; x++)
                    {
                        var world = RoomTemplateUtility.LocalToWorld(x, y, room.template.width, room.template.height, room.rotationDeg, room.origin);
                        RefreshCell(grid, world);
                    }
                }
            }
        }

        private void RefreshCell(LevelGrid grid, Vector2Int cell)
        {
            var data = grid.GetCell(cell);
            var pos = new Vector3Int(cell.x, cell.y, 0);

            if (data.floor != FloorType.Void && _floorTile != null)
                _floorTilemap.SetTile(pos, _floorTile);
            // Water and an unassigned floor sprite still have no tile art yet - left blank
            // rather than faked with a flat-color tile, since Tilemap doesn't do per-cell
            // tint without a second tile variant.

            if (data.normal == NormalType.Wall)
            {
                bool n = IsWallLike(grid, cell + Vector2Int.up);
                bool e = IsWallLike(grid, cell + Vector2Int.right);
                bool s = IsWallLike(grid, cell + Vector2Int.down);
                bool w = IsWallLike(grid, cell + Vector2Int.left);
                int bitmask = WallAtlas.ComputeBitmask(n, e, s, w);
                _wallTilemap.SetTile(pos, _wallAtlas.GetTile(bitmask));
            }
        }

        /// <summary>Only a real Wall counts toward a neighbor's autotile bitmask - a Door is an opening, not a continuation of the wall.</summary>
        private static bool IsWallLike(LevelGrid grid, Vector2Int cell)
        {
            return grid.GetCell(cell).normal == NormalType.Wall;
        }
    }
}
using UnityEngine;

namespace BattleAngel.Grid
{
    /// <summary>
    /// Flags describing a single tile's gameplay properties. Kept as bit flags (not a class)
    /// so GridCell stays small and cache-friendly at map scale (hundreds of thousands of cells).
    /// </summary>
    [System.Flags]
    public enum TileFlags
    {
        None = 0,
        Walkable = 1 << 0,
        FullCover = 1 << 1,
        HalfCover = 1 << 2,
        Hazard = 1 << 3,
        Door = 1 << 4,
        Destructible = 1 << 5,
    }

    /// <summary>
    /// Logical (non-visual) data for one grid cell. This is the source of truth for
    /// pathfinding, line-of-sight, and combat queries. Rendering is a separate concern —
    /// see TilemapVisualSync and InstancedPropRenderer, which read from this data rather
    /// than the other way around. Keeping logic and rendering decoupled is what lets us
    /// scale to large maps: we never spawn a GameObject just to know a tile is walkable.
    /// </summary>
    public struct GridCell
    {
        public TileFlags flags;
        public int floorTileId;   // index into floor tile palette, 0 = unpainted/void
        public int wallTileId;    // index into wall tile palette, 0 = none
        public int occupantId;    // 0 = empty; otherwise an id into a unit/prop registry

        public bool IsWalkable => (flags & TileFlags.Walkable) != 0 && occupantId == 0;
        public bool BlocksLineOfSight => (flags & TileFlags.FullCover) != 0;
        public bool ProvidesHalfCover => (flags & TileFlags.HalfCover) != 0;
    }

    /// <summary>
    /// Central authority for grid state on the currently loaded map. Single flat array,
    /// no per-cell GameObjects. All other systems (assembler, combat, pathfinding, save/load)
    /// go through this rather than touching Tilemap/Transform data directly.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [SerializeField] private int width = 128;
        [SerializeField] private int height = 128;
        [SerializeField] private float cellSize = 1f;

        private GridCell[] cells;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Allocate(width, height);
        }

        /// Call before generating a new map. Wipes all cell data.
        public void Allocate(int newWidth, int newHeight)
        {
            width = newWidth;
            height = newHeight;
            cells = new GridCell[width * height];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;
        public bool InBounds(Vector2Int c) => InBounds(c.x, c.y);

        private int Index(int x, int y) => y * width + x;

        /// Returned by ref so callers can mutate a cell in place without a copy/write-back —
        /// important when painting thousands of tiles during level generation.
        public ref GridCell CellAt(int x, int y) => ref cells[Index(x, y)];
        public ref GridCell CellAt(Vector2Int c) => ref cells[Index(c.x, c.y)];

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return new Vector3((cell.x + 0.5f) * cellSize, (cell.y + 0.5f) * cellSize, 0f);
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            return new Vector2Int(
                Mathf.FloorToInt(world.x / cellSize),
                Mathf.FloorToInt(world.y / cellSize));
        }

        public bool IsWalkable(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            return cells[Index(x, y)].IsWalkable;
        }

        public bool IsWalkable(Vector2Int c) => IsWalkable(c.x, c.y);
    }
}

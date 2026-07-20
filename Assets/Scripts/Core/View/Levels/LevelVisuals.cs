using System.Collections.Generic;
using UnityEngine;

namespace RoomGen
{
    /// <summary>
    /// Renders a generated LevelGrid with real textures (wall autotiling via WallAtlas,
    /// single floor texture) as pooled SpriteRenderers - visible in the actual game camera
    /// at runtime/in builds, not just Scene-view gizmos. Built once per Rebuild() call
    /// after generation finishes, so unlike the room builder there's no need for
    /// incremental neighbor-refresh logic - every cell's neighbors are already final by
    /// the time we compute wall bitmasks.
    ///
    /// Doors currently render as an opening (no wall sprite, floor shows through) rather
    /// than dedicated door art - "ignore doors" per the current texture pass. Door cells
    /// still count as wall-like for neighboring walls' autotile bitmask, so a wall next to
    /// a door doesn't show a broken/open corner toward it.
    ///
    /// Pooled SpriteRenderers is a pragmatic choice for right now, consistent with the
    /// room builder - a Tilemap-based renderer would be more efficient for large levels,
    /// but that's explicitly a later "Cleanup (Performance)" pass on the roadmap, not this one.
    /// </summary>
    public class LevelVisuals : MonoBehaviour
    {
        [Header("Textures")]
        [SerializeField] private Texture2D wallAtlasTexture;
        [SerializeField] private float wallPixelsPerUnit = 100f;
        [SerializeField] private Texture2D floorTexture;
        [SerializeField] private float floorPixelsPerUnit = 100f;

        [SerializeField] private float cellSize = 1f;

        private WallAtlas _wallAtlas;
        private Sprite _floorSprite;
        private Sprite _solidSprite;
        private Transform _root;
        private bool _initialized;

        /// <summary>
        /// Lets a bootstrap assign textures when creating this component in code, without
        /// needing the Inspector. Must work even though Unity calls Awake() synchronously
        /// during AddComponent - before a caller has a chance to call this - so
        /// initialization is idempotent and re-runs here if the textures changed.
        /// </summary>
        public void Configure(Texture2D wallAtlas, Texture2D floor)
        {
            wallAtlasTexture = wallAtlas;
            floorTexture = floor;
            _initialized = false;
            EnsureInitialized();
        }

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            _solidSprite = CreateSolidSprite();
            _wallAtlas = new WallAtlas(wallAtlasTexture, 4, 4, wallPixelsPerUnit);
            if (floorTexture != null)
                _floorSprite = Sprite.Create(floorTexture, new Rect(0, 0, floorTexture.width, floorTexture.height), new Vector2(0.5f, 0.5f), floorPixelsPerUnit);

            if (_root == null)
            {
                _root = new GameObject("LevelVisualsRoot").transform;
                _root.SetParent(transform, false);
            }
        }

        private static Sprite CreateSolidSprite()
        {
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        public void Rebuild(LevelGrid grid)
        {
            Clear();
            if (grid == null) return;

            var allCells = new HashSet<Vector2Int>();
            foreach (var room in grid.PlacedRooms)
            {
                for (int y = 0; y < room.template.height; y++)
                {
                    for (int x = 0; x < room.template.width; x++)
                    {
                        var world = RoomTemplateUtility.LocalToWorld(x, y, room.template.width, room.template.height, room.rotationDeg, room.origin);
                        allCells.Add(world);
                    }
                }
            }

            foreach (var cell in allCells)
                RefreshCell(grid, cell);
        }

        private void Clear()
        {
            var toDestroy = new List<GameObject>();
            foreach (Transform child in _root) toDestroy.Add(child.gameObject);
            foreach (var go in toDestroy) Destroy(go);
        }

        private void RefreshCell(LevelGrid grid, Vector2Int cell)
        {
            var data = grid.GetCell(cell);
            var pos = new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);

            if (data.floor != FloorType.Void)
            {
                var floorGO = NewSpriteChild($"Floor_{cell.x}_{cell.y}", pos, 0);
                if (data.floor == FloorType.Water)
                {
                    floorGO.sprite = _solidSprite;
                    floorGO.color = new Color(0.2f, 0.4f, 0.9f); // no water texture yet
                }
                else if (_floorSprite != null)
                {
                    floorGO.sprite = _floorSprite;
                    floorGO.color = Color.white;
                }
                else
                {
                    floorGO.sprite = _solidSprite;
                    floorGO.color = new Color(0.55f, 0.55f, 0.55f); // no floor texture assigned
                }
            }

            if (data.normal == NormalType.Wall)
            {
                bool n = IsWallLike(grid, cell + Vector2Int.up);
                bool e = IsWallLike(grid, cell + Vector2Int.right);
                bool s = IsWallLike(grid, cell + Vector2Int.down);
                bool w = IsWallLike(grid, cell + Vector2Int.left);
                int bitmask = WallAtlas.ComputeBitmask(n, e, s, w);

                var wallGO = NewSpriteChild($"Wall_{cell.x}_{cell.y}", pos, 5);
                wallGO.sprite = _wallAtlas.GetSprite(bitmask);
                wallGO.color = Color.white;
            }
            // NormalType.Door and Empty: no wall sprite - floor shows through as an opening.
        }

        private static bool IsWallLike(LevelGrid grid, Vector2Int cell)
        {
            var n = grid.GetCell(cell).normal;
            return n == NormalType.Wall || n == NormalType.Door;
        }

        private SpriteRenderer NewSpriteChild(string name, Vector3 localPos, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * cellSize;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            return sr;
        }
    }
}

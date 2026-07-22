using System.Collections.Generic;
using UnityEngine;

namespace RoomGen
{
    /// <summary>
    /// Renders a RoomBuilderState as colored quads (no art yet - this is the "debug draw"
    /// layer). Pooled SpriteRenderers rather than IMGUI/Gizmos so it actually shows up at
    /// runtime/in builds, not just the editor Scene view.
    /// </summary>
    public class RoomBuilderVisuals : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1f;

        [Header("Wall Tiles - assign ONE of the two below (16 entries, left-to-right/top-to-bottom off the reference image)")]
        [SerializeField] private UnityEngine.Tilemaps.TileBase[] wallTiles = new UnityEngine.Tilemaps.TileBase[16];
        [SerializeField] private Sprite[] wallSprites = new Sprite[16];

        [Header("Floor")]
        [Tooltip("Pre-made floor Sprite. Leave unassigned to keep the flat debug color.")]
        [SerializeField] private Sprite floorSprite;

        private WallAtlas _wallAtlas;
        private Sprite _solidSprite;
        private Transform _root;
        private Transform _gridRoot;
        private bool _initialized;

        private readonly Dictionary<Vector2Int, SpriteRenderer> _floorRenderers = new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly Dictionary<Vector2Int, SpriteRenderer> _normalRenderers = new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly Dictionary<Vector2Int, SpriteRenderer> _connectorRenderers = new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly Dictionary<Vector2Int, PropVisual> _propVisuals = new Dictionary<Vector2Int, PropVisual>();

        private class PropVisual
        {
            public Transform root;
            public SpriteRenderer body;
        }

        public float CellSize => cellSize;

        /// <summary>
        /// Lets a bootstrap assign assets when creating this component in code. Unity
        /// calls Awake() synchronously during AddComponent, before a caller has a chance to
        /// call this - so initialization is idempotent and safe to re-run here.
        /// </summary>
        public void ConfigureTextures(UnityEngine.Tilemaps.TileBase[] tiles, Sprite[] sprites, Sprite floor)
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

            _solidSprite = CreateSolidSprite();
            _wallAtlas = HasAny(wallTiles) ? new WallAtlas(wallTiles) : new WallAtlas(wallSprites);

            if (_root == null)
            {
                _root = new GameObject("BuilderVisualsRoot").transform;
                _root.SetParent(transform, false);
            }
        }

        private static bool HasAny<T>(T[] arr) where T : class
        {
            if (arr == null) return false;
            foreach (var x in arr) if (x != null) return true;
            return false;
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

        public void Rebuild(RoomBuilderState state)
        {
            Clear();
            _gridRoot = new GameObject("GridLines").transform;
            _gridRoot.SetParent(_root, false);
            BuildGridLines(state.width, state.height);

            for (int y = 0; y < state.height; y++)
            {
                for (int x = 0; x < state.width; x++)
                {
                    CreateCellRenderers(new Vector2Int(x, y));
                    RefreshCell(state, x, y);
                }
            }
            foreach (var p in state.props)
                RefreshProp(p);
        }

        private void Clear()
        {
            var toDestroy = new List<GameObject>();
            foreach (Transform child in _root) toDestroy.Add(child.gameObject);
            foreach (var go in toDestroy) Destroy(go);

            _floorRenderers.Clear();
            _normalRenderers.Clear();
            _connectorRenderers.Clear();
            _propVisuals.Clear();
        }

        private void CreateCellRenderers(Vector2Int cell)
        {
            _floorRenderers[cell] = NewSpriteChild($"Floor_{cell.x}_{cell.y}", cell, 0);
            _normalRenderers[cell] = NewSpriteChild($"Normal_{cell.x}_{cell.y}", cell, 5);
            _connectorRenderers[cell] = NewSpriteChild($"Conn_{cell.x}_{cell.y}", cell, 8);
        }

        private SpriteRenderer NewSpriteChild(string name, Vector2Int cell, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            go.transform.localPosition = new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);
            go.transform.localScale = Vector3.one * cellSize;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _solidSprite;
            sr.sortingOrder = sortingOrder;
            sr.enabled = false;
            return sr;
        }

        /// <summary>
        /// Draws a white line grid over every cell boundary (width+1 vertical, height+1
        /// horizontal), independent of what's painted, so the placeable area is always
        /// visible without needing floor tiles laid down first. The outer boundary is
        /// drawn brighter/thicker so the room's actual edge is unambiguous.
        /// </summary>
        private void BuildGridLines(int width, int height)
        {
            float innerThickness = Mathf.Max(0.015f, cellSize * 0.02f);
            float outerThickness = Mathf.Max(0.03f, cellSize * 0.05f);
            var innerColor = new Color(1f, 1f, 1f, 0.35f);
            var outerColor = new Color(1f, 1f, 1f, 0.95f);

            float yCenter = (height - 1) * 0.5f * cellSize;
            for (int i = 0; i <= width; i++)
            {
                float x = (i - 0.5f) * cellSize;
                bool boundary = i == 0 || i == width;
                CreateGridLine($"GridV_{i}", new Vector3(x, yCenter, -0.05f),
                    new Vector3(boundary ? outerThickness : innerThickness, height * cellSize, 1f),
                    boundary ? outerColor : innerColor);
            }

            float xCenter = (width - 1) * 0.5f * cellSize;
            for (int j = 0; j <= height; j++)
            {
                float y = (j - 0.5f) * cellSize;
                bool boundary = j == 0 || j == height;
                CreateGridLine($"GridH_{j}", new Vector3(xCenter, y, -0.05f),
                    new Vector3(width * cellSize, boundary ? outerThickness : innerThickness, 1f),
                    boundary ? outerColor : innerColor);
            }
        }

        private void CreateGridLine(string name, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_gridRoot, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _solidSprite;
            sr.color = color;
            sr.sortingOrder = 15; // above floor/normal/connector so bounds always read clearly
        }

        public void RefreshCell(RoomBuilderState state, int x, int y)
        {
            if (!state.InBounds(x, y)) return; // painting a wall can trigger neighbor refreshes just outside the room
            var cell = new Vector2Int(x, y);

            var floor = state.GetFloorAt(x, y);
            var floorR = _floorRenderers[cell];
            if (floor == FloorType.Void)
            {
                floorR.enabled = false;
            }
            else if (floor == FloorType.Water)
            {
                floorR.enabled = true;
                floorR.sprite = _solidSprite;
                floorR.color = new Color(0.2f, 0.4f, 0.9f); // no water texture yet - flat debug color
            }
            else if (floorSprite != null)
            {
                floorR.enabled = true;
                floorR.sprite = floorSprite;
                floorR.color = Color.white;
            }
            else
            {
                floorR.enabled = true;
                floorR.sprite = _solidSprite;
                floorR.color = new Color(0.55f, 0.55f, 0.55f); // no floor sprite assigned - flat debug color
            }

            var normal = state.GetNormalAt(x, y);
            var normalR = _normalRenderers[cell];
            if (normal == NormalType.Wall)
            {
                bool n = IsWallLike(state, x, y + 1);
                bool e = IsWallLike(state, x + 1, y);
                bool s = IsWallLike(state, x, y - 1);
                bool w = IsWallLike(state, x - 1, y);
                int bitmask = WallAtlas.ComputeBitmask(n, e, s, w);

                normalR.enabled = true;
                normalR.sprite = _wallAtlas.GetSprite(bitmask);
                normalR.color = Color.white;
            }
            else if (normal == NormalType.Door)
            {
                // No door art yet - a thin debug marker so it's visible while editing, but
                // doesn't render as a solid wall (a door is an opening).
                normalR.enabled = true;
                normalR.sprite = _solidSprite;
                normalR.color = new Color(0.65f, 0.4f, 0.1f, 0.5f);
            }
            else
            {
                normalR.enabled = false;
            }

            var conn = state.GetConnectorAt(x, y);
            var connR = _connectorRenderers[cell];
            if (conn == ConnectorType.None)
            {
                connR.enabled = false;
            }
            else
            {
                connR.enabled = true;
                connR.sprite = _solidSprite;
                connR.color = conn switch
                {
                    ConnectorType.Restricted => new Color(1f, 0.3f, 0.1f, 0.55f),
                    ConnectorType.AlwaysDouble => new Color(0.2f, 0.7f, 1f, 0.55f),
                    _ => new Color(1f, 0.95f, 0.2f, 0.45f)
                };
            }
        }

        /// <summary>
        /// Whether a neighbor cell counts as "wall-like" for autotiling purposes - only a
        /// real Wall does. A Door is an opening, so a wall next to one should show its
        /// exposed edge, not act like the wall continues through the doorway. A neighbor
        /// outside the room's grid also doesn't count, for the same reason.
        /// </summary>
        private static bool IsWallLike(RoomBuilderState state, int x, int y)
        {
            if (!state.InBounds(x, y)) return false;
            return state.GetNormalAt(x, y) == NormalType.Wall;
        }

        public void RefreshProp(PropPlacement p)
        {
            var cell = new Vector2Int(p.cellX, p.cellY);
            if (!_propVisuals.TryGetValue(cell, out var pv))
            {
                pv = CreatePropVisual(cell);
                _propVisuals[cell] = pv;
            }
            pv.body.color = ColorForPropId(p.propId);
            //pv.root.localRotation = Quaternion.Euler(0f, 0f, -p.baseRotationDeg);
        }

        private PropVisual CreatePropVisual(Vector2Int cell)
        {
            var root = new GameObject($"Prop_{cell.x}_{cell.y}").transform;
            root.SetParent(_root, false);
            root.localPosition = new Vector3(cell.x * cellSize, cell.y * cellSize, -0.2f);

            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(root, false);
            bodyGO.transform.localScale = Vector3.one * cellSize * 0.5f;
            var body = bodyGO.AddComponent<SpriteRenderer>();
            body.sprite = _solidSprite;
            body.sortingOrder = 10;

            // Thin marker showing which way the prop currently faces (0deg = "up").
            var facingGO = new GameObject("Facing");
            facingGO.transform.SetParent(root, false);
            facingGO.transform.localPosition = new Vector3(0f, cellSize * 0.3f, 0f);
            facingGO.transform.localScale = new Vector3(cellSize * 0.12f, cellSize * 0.35f, 1f);
            var facing = facingGO.AddComponent<SpriteRenderer>();
            facing.sprite = _solidSprite;
            facing.color = Color.white;
            facing.sortingOrder = 11;

            return new PropVisual { root = root, body = body };
        }

        public void RemoveProp(Vector2Int cell)
        {
            if (_propVisuals.TryGetValue(cell, out var pv))
            {
                Destroy(pv.root.gameObject);
                _propVisuals.Remove(cell);
            }
        }

        private static Color ColorForPropId(string id)
        {
            int hash = string.IsNullOrEmpty(id) ? 0 : id.GetHashCode();
            var rnd = new System.Random(hash);
            return new Color(
                (float)rnd.NextDouble() * 0.6f + 0.4f,
                (float)rnd.NextDouble() * 0.6f + 0.4f,
                (float)rnd.NextDouble() * 0.6f + 0.4f);
        }

        public bool TryWorldToCell(Vector3 world, out int x, out int y)
        {
            var local = world - transform.position;
            x = Mathf.RoundToInt(local.x / cellSize);
            y = Mathf.RoundToInt(local.y / cellSize);
            return true;
        }
    }
}
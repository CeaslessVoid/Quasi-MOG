using System.Collections.Generic;
using UnityEngine;
using GameDefs;

namespace RoomGen
{
    public class RoomBuilderVisuals : RoomVisualsBase
    {
        private Transform _root;
        private Transform _gridRoot;

        private readonly Dictionary<Vector2Int, SpriteRenderer> _floorRenderers = new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly Dictionary<Vector2Int, SpriteRenderer> _normalRenderers = new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly Dictionary<Vector2Int, SpriteRenderer> _connectorRenderers = new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly Dictionary<Vector2Int, PropVisual> _propVisuals = new Dictionary<Vector2Int, PropVisual>();

        private class PropVisual
        {
            public Transform root;
            public SpriteRenderer body;
        }

        protected override void OnInitialize()
        {
            _root = new GameObject("BuilderVisualsRoot").transform;
            _root.SetParent(transform, false);
        }

        public void Rebuild(RoomBuilderState state)
        {
            EnsureInitialized();
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
            sr.sprite = DefVisualUtility.SolidSprite;
            sr.sortingOrder = sortingOrder;
            sr.enabled = false;
            return sr;
        }

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
            sr.sprite = DefVisualUtility.SolidSprite;
            sr.color = color;
            sr.sortingOrder = 15;
        }

        public void RefreshCell(RoomBuilderState state, int x, int y)
        {
            if (!state.InBounds(x, y)) return;
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
                floorR.sprite = DefVisualUtility.SolidSprite;
                floorR.color = new Color(0.2f, 0.4f, 0.9f);
            }
            else
            {
                var floorDef = DefDatabase<FloorDef>.Get(state.GetFloorDefAt(x, y));
                floorR.enabled = true;
                if (floorDef != null && floorDef.HasTexture)
                {
                    floorR.sprite = floorDef.Sprite;
                    floorR.color = floorDef.TintColor;
                }
                else
                {
                    floorR.sprite = DefVisualUtility.MissingSprite;
                    floorR.color = Color.white;
                }
            }

            var normal = state.GetNormalAt(x, y);
            var normalR = _normalRenderers[cell];
            if (normal == NormalType.Wall)
            {
                bool n = IsWallLike(state, x, y + 1);
                bool e = IsWallLike(state, x + 1, y);
                bool s = IsWallLike(state, x, y - 1);
                bool w = IsWallLike(state, x - 1, y);
                int bitmask = ComputeBitmask(n, e, s, w);

                var wallDef = DefDatabase<WallDef>.Get(state.GetWallDefAt(x, y));
                normalR.enabled = true;
                if (wallDef != null && wallDef.HasTexture)
                {
                    normalR.sprite = wallDef.GetSprite(bitmask);
                    normalR.color = wallDef.TintColor;
                }
                else
                {
                    normalR.sprite = DefVisualUtility.MissingSprite;
                    normalR.color = Color.white;
                }
            }
            else if (normal == NormalType.Door)
            {
                var doorDef = DefDatabase<DoorDef>.Get(state.GetDoorDefAt(x, y));
                normalR.enabled = true;
                if (doorDef != null && doorDef.HasTexture)
                {
                    normalR.sprite = doorDef.ClosedSprite;
                    normalR.color = doorDef.TintColor;
                }
                else
                {
                    normalR.sprite = DefVisualUtility.MissingSprite;
                    normalR.color = new Color(0.65f, 0.4f, 0.1f, 1f);
                }
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
                connR.sprite = DefVisualUtility.SolidSprite;
                connR.color = conn switch
                {
                    ConnectorType.Restricted => new Color(1f, 0.3f, 0.1f, 0.55f),
                    ConnectorType.AlwaysDouble => new Color(0.2f, 0.7f, 1f, 0.55f),
                    _ => new Color(1f, 0.95f, 0.2f, 0.45f)
                };
            }
        }

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
            body.sprite = DefVisualUtility.SolidSprite;
            body.sortingOrder = 10;

            var facingGO = new GameObject("Facing");
            facingGO.transform.SetParent(root, false);
            facingGO.transform.localPosition = new Vector3(0f, cellSize * 0.3f, 0f);
            facingGO.transform.localScale = new Vector3(cellSize * 0.12f, cellSize * 0.35f, 1f);
            var facing = facingGO.AddComponent<SpriteRenderer>();
            facing.sprite = DefVisualUtility.SolidSprite;
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

using System.Collections.Generic;
using UnityEngine;
using GameDefs;

namespace RoomGen
{
    public class RoomBuilderVisuals : RoomVisualsBase
    {
        private static readonly Color ValidPreviewColor = new Color(0.25f, 1f, 0.25f, 0.35f);
        private static readonly Color InvalidPreviewColor = new Color(1f, 0.2f, 0.2f, 0.4f);

        private Transform _root;
        private Transform _gridRoot;

        private readonly Dictionary<Vector2Int, SpriteRenderer> _floorRenderers = new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly Dictionary<Vector2Int, SpriteRenderer> _normalRenderers = new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly Dictionary<Vector2Int, SpriteRenderer> _doorLeafBRenderers = new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly Dictionary<Vector2Int, SpriteRenderer> _connectorRenderers = new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly Dictionary<Vector2Int, ConnectorType> _lastConnectorType = new Dictionary<Vector2Int, ConnectorType>();
        private readonly Dictionary<Vector2Int, PropVisual> _propVisuals = new Dictionary<Vector2Int, PropVisual>();

        private readonly List<SpriteRenderer> _previewOverlayPool = new List<SpriteRenderer>();
        private SpriteRenderer _previewGhost;

        private bool _connectorOverlayVisible;

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

        public void SetConnectorOverlayVisible(bool visible)
        {
            _connectorOverlayVisible = visible;
            foreach (var kvp in _connectorRenderers)
            {
                _lastConnectorType.TryGetValue(kvp.Key, out var conn);
                kvp.Value.enabled = visible && conn != ConnectorType.None;
            }
        }

        public void Rebuild(RoomData state)
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
            _doorLeafBRenderers.Clear();
            _connectorRenderers.Clear();
            _lastConnectorType.Clear();
            _propVisuals.Clear();
            _previewOverlayPool.Clear();
            _previewGhost = null;
        }

        private void CreateCellRenderers(Vector2Int cell)
        {
            _floorRenderers[cell] = NewSpriteChild($"Floor_{cell.x}_{cell.y}", cell, 0);
            _normalRenderers[cell] = NewSpriteChild($"Normal_{cell.x}_{cell.y}", cell, 5);
            _doorLeafBRenderers[cell] = NewSpriteChild($"DoorLeafB_{cell.x}_{cell.y}", cell, 5);
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

        public void RefreshCell(RoomData state, int x, int y)
        {
            if (!state.InBounds(x, y)) return;
            var cell = new Vector2Int(x, y);

            var floor = state.GetFloor(x, y);
            var floorR = _floorRenderers[cell];
            if (floor == FloorType.Void)
            {
                floorR.enabled = false;
            }
            else if (floor == FloorType.Liquid)
            {
                var liquidDef = DefDatabase.Get<LiquidDef>(state.GetFloorDef(x, y));
                floorR.enabled = true;
                if (liquidDef != null && liquidDef.HasTexture)
                {
                    floorR.sprite = liquidDef.Sprite;
                    floorR.color = liquidDef.TintColor;
                }
                else
                {
                    floorR.sprite = DefVisualUtility.MissingSprite;
                    floorR.color = Color.white;
                }
            }
            else
            {
                var floorDef = DefDatabase.Get<FloorDef>(state.GetFloorDef(x, y));
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

            var normal = state.GetNormal(x, y);
            var normalR = _normalRenderers[cell];
            var doorLeafB = _doorLeafBRenderers[cell];

            if (normal == NormalType.Wall)
            {
                bool n = IsWallLike(state, x, y + 1);
                bool e = IsWallLike(state, x + 1, y);
                bool s = IsWallLike(state, x, y - 1);
                bool w = IsWallLike(state, x - 1, y);
                int bitmask = ComputeBitmask(n, e, s, w);

                var wallDef = DefDatabase.Get<WallDef>(state.GetWallDef(x, y));
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
                doorLeafB.enabled = false;
            }
            else if (normal == NormalType.Door)
            {
                var doorDef = DefDatabase.Get<DoorDef>(state.GetDoorDef(x, y));
                bool isNorthOrientation = IsNorthOrientedDoor(state, x, y);
                Sprite baseSprite = doorDef != null ? (isNorthOrientation ? doorDef.NorthSprite : doorDef.EastSprite) : null;

                normalR.enabled = true;
                if (baseSprite != null)
                {
                    normalR.sprite = baseSprite;
                    normalR.color = doorDef.TintColor;
                }
                else
                {
                    normalR.sprite = DefVisualUtility.MissingSprite;
                    normalR.color = new Color(0.65f, 0.4f, 0.1f, 1f);
                }

                doorLeafB.enabled = true;
                doorLeafB.sprite = normalR.sprite;
                doorLeafB.color = normalR.color;
                doorLeafB.transform.localScale = isNorthOrientation
                    ? new Vector3(-cellSize, cellSize, 1f)
                    : new Vector3(cellSize, -cellSize, 1f);
            }
            else
            {
                normalR.enabled = false;
                doorLeafB.enabled = false;
            }

            var conn = state.GetConnector(x, y);
            _lastConnectorType[cell] = conn;
            var connR = _connectorRenderers[cell];
            if (conn == ConnectorType.None)
            {
                connR.enabled = false;
            }
            else
            {
                connR.enabled = _connectorOverlayVisible;
                connR.sprite = DefVisualUtility.SolidSprite;
                connR.color = conn switch
                {
                    ConnectorType.Restricted => new Color(1f, 0.3f, 0.1f, 0.55f),
                    ConnectorType.AlwaysDouble => new Color(0.2f, 0.7f, 1f, 0.55f),
                    _ => new Color(1f, 0.95f, 0.2f, 0.45f)
                };
            }
        }

        private static bool IsWallLike(RoomData state, int x, int y)
        {
            if (!state.InBounds(x, y)) return false;
            return state.GetNormal(x, y) == NormalType.Wall;
        }

        public static bool IsNorthOrientedDoor(RoomData state, int x, int y)
        {
            bool n = IsWallLike(state, x, y + 1);
            bool e = IsWallLike(state, x + 1, y);
            bool s = IsWallLike(state, x, y - 1);
            bool w = IsWallLike(state, x - 1, y);
            return WallAtlas.IsNorthOriented(n, e, s, w);
        }

        public void RefreshProp(PropPlacement p)
        {
            var origin = new Vector2Int(p.cellX, p.cellY);
            var def = DefDatabase.Get<PropDef>(p.propId);

            if (!_propVisuals.TryGetValue(origin, out var pv))
            {
                pv = CreatePropVisual(origin);
                _propVisuals[origin] = pv;
            }

            int width = def != null ? def.Width : 1;
            int height = def != null ? def.Height : 1;
            var footprint = PropPlacementUtility.GetFootprintCells(origin, width, height, p.facing);
            var category = def != null ? def.Category : PropCategory.Normal;
            PropPlacementUtility.GetRenderBounds(footprint, category, p.facing, out var min, out var max);

            float cx = (min.x + max.x) * 0.5f * cellSize;
            float cy = (min.y + max.y) * 0.5f * cellSize;
            int w = max.x - min.x + 1;
            int h = max.y - min.y + 1;
            bool flip = p.facing == PropFacing.West;
            float depth = category == PropCategory.Decorative ? -0.25f : -0.2f;

            pv.root.localPosition = new Vector3(cx, cy, depth);

            Sprite sprite = def != null && def.HasTexture ? def.GetSprite(p.facing) : DefVisualUtility.MissingSprite;
            pv.body.sprite = sprite;

            if (def != null && def.HasTexture)
                DefTintRenderer.Apply(pv.body, def.TintColor, def.SecondaryTintColor, def.GetMask(p.facing));
            else
                DefTintRenderer.ApplyFlatTint(pv.body, DefVisualUtility.MissingColor);

            var fitSize = PropPlacementUtility.GetUniformFitSize(sprite, w * cellSize, h * cellSize);
            pv.body.transform.localScale = new Vector3((flip ? -1f : 1f) * fitSize.x, fitSize.y, 1f);
        }

        private PropVisual CreatePropVisual(Vector2Int origin)
        {
            var root = new GameObject($"Prop_{origin.x}_{origin.y}").transform;
            root.SetParent(_root, false);

            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(root, false);
            var body = bodyGO.AddComponent<SpriteRenderer>();
            body.sortingOrder = 10;

            return new PropVisual { root = root, body = body };
        }

        public void RemoveProp(Vector2Int origin)
        {
            if (_propVisuals.TryGetValue(origin, out var pv))
            {
                Destroy(pv.root.gameObject);
                _propVisuals.Remove(origin);
            }
        }

        public bool TryWorldToCell(Vector3 world, out int x, out int y)
        {
            var local = world - transform.position;
            x = Mathf.RoundToInt(local.x / cellSize);
            y = Mathf.RoundToInt(local.y / cellSize);
            return true;
        }

        private SpriteRenderer GetOverlay(int index)
        {
            while (_previewOverlayPool.Count <= index)
            {
                var go = new GameObject($"PreviewOverlay_{_previewOverlayPool.Count}");
                go.transform.SetParent(_root, false);
                go.transform.localScale = Vector3.one * cellSize;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = DefVisualUtility.SolidSprite;
                sr.sortingOrder = 20;
                sr.enabled = false;
                _previewOverlayPool.Add(sr);
            }
            return _previewOverlayPool[index];
        }

        private void EnsureGhost()
        {
            if (_previewGhost != null) return;
            var go = new GameObject("PreviewGhost");
            go.transform.SetParent(_root, false);
            _previewGhost = go.AddComponent<SpriteRenderer>();
            _previewGhost.sortingOrder = 21;
            _previewGhost.enabled = false;
        }

        public void ClearPreview()
        {
            foreach (var sr in _previewOverlayPool) sr.enabled = false;
            if (_previewGhost != null) _previewGhost.enabled = false;
        }

        public void ShowPreview(IReadOnlyList<(Vector2Int cell, bool valid)> footprint, Sprite ghostSprite, Color primaryTint, Color secondaryTint, Texture2D mask, Vector2Int boundsMin, Vector2Int boundsMax, bool flipGhostX)
        {
            EnsureGhost();

            int i = 0;
            if (footprint != null)
            {
                for (; i < footprint.Count; i++)
                {
                    var sr = GetOverlay(i);
                    var c = footprint[i].cell;
                    sr.transform.localPosition = new Vector3(c.x * cellSize, c.y * cellSize, -0.3f);
                    sr.color = footprint[i].valid ? ValidPreviewColor : InvalidPreviewColor;
                    sr.enabled = true;
                }
            }
            for (; i < _previewOverlayPool.Count; i++) _previewOverlayPool[i].enabled = false;

            if (ghostSprite != null)
            {
                _previewGhost.enabled = true;
                _previewGhost.sprite = ghostSprite;

                DefTintRenderer.Apply(_previewGhost, primaryTint, secondaryTint, mask);
                _previewGhost.color = new Color(1f, 1f, 1f, 0.55f);

                int w = boundsMax.x - boundsMin.x + 1;
                int h = boundsMax.y - boundsMin.y + 1;
                float cx = (boundsMin.x + boundsMax.x) * 0.5f * cellSize;
                float cy = (boundsMin.y + boundsMax.y) * 0.5f * cellSize;
                _previewGhost.transform.localPosition = new Vector3(cx, cy, -0.25f);

                var fitSize = PropPlacementUtility.GetUniformFitSize(ghostSprite, w * cellSize, h * cellSize);
                _previewGhost.transform.localScale = new Vector3((flipGhostX ? -1f : 1f) * fitSize.x, fitSize.y, 1f);
            }
            else
            {
                _previewGhost.enabled = false;
            }
        }
    }
}

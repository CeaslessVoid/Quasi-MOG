using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using GameDefs;
using Util;

namespace RoomGen
{
    public enum BuilderTool { None, Floor, Normal, Connector, Prop }

    [RequireComponent(typeof(RoomBuilderVisuals))]
    public class RoomBuilderController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private RoomBuilderVisuals visuals;

        private RoomData _state;
        private BuilderTool _tool = BuilderTool.None;

        private FloorType _floorBrushType = FloorType.Floor;
        private NormalType _normalBrushType = NormalType.Wall;
        private ConnectorType _connectorBrush = ConnectorType.Normal;

        private string _wallDefBrush;
        private string _doorDefBrush;
        private string _floorDefBrush;

        private string _currentPropId;
        private PropFacing _currentPropFacing = PropFacing.North;

        private Vector2Int? _lastPaintCellLeft;
        private Vector2Int? _lastPaintCellRight;

        public int CurrentDesiredConnections => _state != null ? _state.desiredConnections : 0;
        public float CurrentChanceToConnectWhenBelowTarget => _state != null ? _state.chanceToConnectWhenBelowTarget : 0f;
        public float CurrentSelectionWeight => _state != null ? _state.selectionWeight : 0f;


        private readonly List<(Vector2Int cell, bool valid)> _singleCellScratch = new List<(Vector2Int, bool)>(1);
        private readonly List<(Vector2Int cell, bool valid)> _propPreviewScratch = new List<(Vector2Int, bool)>();

        private List<string> _roomFiles = new List<string>();
        private string _statusMessage = "";

        public RoomData CurrentRoom => _state;
        public IReadOnlyList<string> RoomFiles => _roomFiles;
        public string StatusMessage => _statusMessage;

        public BuilderTool ActiveTool => _tool;
        public string CurrentFloorDefBrush => _floorDefBrush;
        public string CurrentWallDefBrush => _wallDefBrush;
        public string CurrentDoorDefBrush => _doorDefBrush;
        public string CurrentPropDefBrush => _currentPropId;
        public ConnectorType CurrentConnectorBrush => _connectorBrush;

        public void Configure(Camera cam, RoomBuilderVisuals vis)
        {
            targetCamera = cam;
            visuals = vis;
        }

        private void Awake()
        {
            if (visuals == null) visuals = GetComponent<RoomBuilderVisuals>();
            if (targetCamera == null) targetCamera = Camera.main;
            RefreshFileList();

            if (DefDatabase.All<WallDef>().Count > 0) _wallDefBrush = DefDatabase.All<WallDef>()[0].DefName;
            if (DefDatabase.All<DoorDef>().Count > 0) _doorDefBrush = DefDatabase.All<DoorDef>()[0].DefName;
            if (DefDatabase.All<FloorDef>().Count > 0) _floorDefBrush = DefDatabase.All<FloorDef>()[0].DefName;
            if (DefDatabase.All<PropDef>().Count > 0) _currentPropId = DefDatabase.All<PropDef>()[0].DefName;
        }
        public void SetActiveTool(BuilderTool tool)
        {
            _tool = tool;
            visuals.SetConnectorOverlayVisible(_tool == BuilderTool.Connector);
            visuals.ClearPreview();
        }
        public void SetDesiredConnections(int value) { if (_state != null) _state.desiredConnections = Mathf.Max(0, value); }
        public void SetChanceToConnectWhenBelowTarget(float value) { if (_state != null) _state.chanceToConnectWhenBelowTarget = Mathf.Clamp01(value); }
        public void SetSelectionWeight(float value) { if (_state != null) _state.selectionWeight = Mathf.Max(0f, value); }

        public void ClearActiveTool() => SetActiveTool(BuilderTool.None);

        public void SetFloorDefBrush(string defName)
        {
            _floorBrushType = FloorType.Floor;
            _floorDefBrush = defName;
        }

        public void SetLiquidDefBrush(string defName)
        {
            _floorBrushType = FloorType.Liquid;
            _floorDefBrush = defName;
        }

        public void SetWallDefBrush(string defName)
        {
            _normalBrushType = NormalType.Wall;
            _wallDefBrush = defName;
        }

        public void SetDoorDefBrush(string defName)
        {
            _normalBrushType = NormalType.Door;
            _doorDefBrush = defName;
        }

        public void SetPropBrush(string defName) => _currentPropId = defName;

        public void SetConnectorBrush(ConnectorType type) => _connectorBrush = type;

        public void CreateNewRoom(int width, int height, string templateId)
        {
            _state = new RoomData();
            _state.Allocate(width, height);
            _state.templateId = string.IsNullOrWhiteSpace(templateId) ? "NewRoom" : templateId;
            visuals.Rebuild(_state);
            _statusMessage = $"Created {_state.width}x{_state.height} room.";
        }

        public void SaveRoom(string templateIdOverride = null)
        {
            if (_state == null) { _statusMessage = "Nothing to save."; return; }
            if (!string.IsNullOrWhiteSpace(templateIdOverride)) _state.templateId = templateIdOverride;
            RoomLibrary.Save(_state);
            RefreshFileList();
            _statusMessage = $"Saved '{_state.templateId}' to {RoomLibrary.RoomsFolder}";
        }

        public void LoadRoom(string path)
        {
            _state = RoomLibrary.Load(path);
            visuals.Rebuild(_state);
            _statusMessage = $"Loaded '{_state.templateId}'.";
        }

        public void RefreshFileList() => _roomFiles = RoomLibrary.ListRoomFiles();
        public void AddTypeTag(string tag)
        {
            if (_state == null || string.IsNullOrWhiteSpace(tag)) return;
            if (!_state.typeTags.Contains(tag)) _state.typeTags.Add(tag);
        }

        public void RemoveTypeTag(string tag) => _state?.typeTags.Remove(tag);

        public void AddZoneTag(string tag)
        {
            if (_state == null || string.IsNullOrWhiteSpace(tag)) return;
            if (!_state.zoneTags.Contains(tag)) _state.zoneTags.Add(tag);
        }

        public void RemoveZoneTag(string tag) => _state?.zoneTags.Remove(tag);
        public void SetPreferredSingleDoorDef(string defName) { if (_state != null) _state.preferredSingleDoorDef = defName; }
        public void ClearPreferredSingleDoorDef() { if (_state != null) _state.preferredSingleDoorDef = null; }
        public void SetPreferredDoubleDoorDef(string defName) { if (_state != null) _state.preferredDoubleDoorDef = defName; }
        public void ClearPreferredDoubleDoorDef() { if (_state != null) _state.preferredDoubleDoorDef = null; }

        private void Update()
        {
            if (_state == null || targetCamera == null) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                visuals.ClearPreview();
                return;
            }
            if (_tool == BuilderTool.None)
            {
                visuals.ClearPreview();
                return;
            }

            var mouseWorld = targetCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            visuals.TryWorldToCell(mouseWorld, out int cx, out int cy);

            if (!_state.InBounds(cx, cy))
            {
                _lastPaintCellLeft = null;
                _lastPaintCellRight = null;
                visuals.ClearPreview();
                return;
            }

            UpdatePreview(cx, cy);

            if (_tool == BuilderTool.Prop)
            {
                if (Input.GetMouseButtonDown(0)) PlaceProp(cx, cy);
                if (Input.GetMouseButtonDown(1)) RemovePropAt(cx, cy);
                if (!InputFocusUtility.IsTypingInField && Input.GetKeyDown(KeyCode.R))
                    _currentPropFacing = (PropFacing)(((int)_currentPropFacing + 1) % 4);
                return;
            }

            var cell = new Vector2Int(cx, cy);

            if (Input.GetMouseButton(0))
            {
                if (_lastPaintCellLeft.HasValue) PaintLine(_lastPaintCellLeft.Value, cell, false);
                else PaintCell(cx, cy, false);
                _lastPaintCellLeft = cell;
            }
            else _lastPaintCellLeft = null;

            if (Input.GetMouseButton(1))
            {
                if (_lastPaintCellRight.HasValue) PaintLine(_lastPaintCellRight.Value, cell, true);
                else PaintCell(cx, cy, true);
                _lastPaintCellRight = cell;
            }
            else _lastPaintCellRight = null;
        }

        private void UpdatePreview(int cx, int cy)
        {
            switch (_tool)
            {
                case BuilderTool.Floor: PreviewFloor(cx, cy); break;
                case BuilderTool.Normal: PreviewNormal(cx, cy); break;
                case BuilderTool.Connector: PreviewConnector(cx, cy); break;
                case BuilderTool.Prop: PreviewProp(cx, cy); break;
            }
        }

        private void PreviewFloor(int x, int y)
        {
            var cell = new Vector2Int(x, y);
            _singleCellScratch.Clear();
            _singleCellScratch.Add((cell, true));

            if (Input.GetMouseButton(1))
            {
                visuals.ShowPreview(_singleCellScratch, null, Color.white, Color.white, null, cell, cell, false);
                return;
            }

            SurfaceDef def = _floorBrushType == FloorType.Liquid
                ? (SurfaceDef)DefDatabase.Get<LiquidDef>(_floorDefBrush)
                : DefDatabase.Get<FloorDef>(_floorDefBrush);

            Sprite sprite = def != null && def.HasTexture ? def.Sprite : DefVisualUtility.MissingSprite;
            Color tint = def != null ? def.TintColor : Color.white;
            Color secondary = def != null ? def.SecondaryTintColor : Color.white;
            Texture2D mask = def != null ? def.MaskTexture : null;
            visuals.ShowPreview(_singleCellScratch, sprite, tint, secondary, mask, cell, cell, false);
        }

        private void PreviewNormal(int x, int y)
        {
            var cell = new Vector2Int(x, y);
            _singleCellScratch.Clear();
            _singleCellScratch.Add((cell, true));

            if (Input.GetMouseButton(1) || _normalBrushType == NormalType.Empty)
            {
                visuals.ShowPreview(_singleCellScratch, null, Color.white, Color.white, null, cell, cell, false);
                return;
            }

            bool n = IsWallLikeAt(x, y + 1);
            bool e = IsWallLikeAt(x + 1, y);
            bool s = IsWallLikeAt(x, y - 1);
            bool w = IsWallLikeAt(x - 1, y);

            if (_normalBrushType == NormalType.Wall)
            {
                int bitmask = WallAtlas.ComputeBitmask(n, e, s, w);
                var def = DefDatabase.Get<WallDef>(_wallDefBrush);
                Sprite sprite = def != null && def.HasTexture ? def.GetSprite(bitmask) : DefVisualUtility.MissingSprite;
                Color tint = def != null ? def.TintColor : Color.white;
                Color secondary = def != null ? def.SecondaryTintColor : Color.white;
                Texture2D mask = def != null ? def.MaskTexture : null;
                visuals.ShowPreview(_singleCellScratch, sprite, tint, secondary, mask, cell, cell, false);
            }
            else
            {
                bool isNorthOrientation = WallAtlas.IsNorthOriented(n, e, s, w);

                var def = DefDatabase.Get<DoorDef>(_doorDefBrush);
                Sprite sprite = def != null ? (isNorthOrientation ? def.NorthSprite : def.EastSprite) : null;
                if (sprite == null) sprite = DefVisualUtility.MissingSprite;
                Color tint = def != null ? def.TintColor : new Color(0.65f, 0.4f, 0.1f, 1f);
                Color secondary = def != null ? def.SecondaryTintColor : Color.white;
                Texture2D mask = def != null ? def.MaskTexture : null;
                visuals.ShowPreview(_singleCellScratch, sprite, tint, secondary, mask, cell, cell, false);
            }
        }

        private void PreviewConnector(int x, int y)
        {
            var cell = new Vector2Int(x, y);
            bool valid = _state.GetNormal(x, y) == NormalType.Wall;
            _singleCellScratch.Clear();
            _singleCellScratch.Add((cell, valid));
            visuals.ShowPreview(_singleCellScratch, null, Color.white, Color.white, null, cell, cell, false);
        }

        private void PreviewProp(int x, int y)
        {
            if (Input.GetMouseButton(1)) { visuals.ClearPreview(); return; }
            if (string.IsNullOrEmpty(_currentPropId)) { visuals.ClearPreview(); return; }

            var def = DefDatabase.Get<PropDef>(_currentPropId);
            if (def == null) { visuals.ClearPreview(); return; }

            var candidate = new PropPlacement { propId = _currentPropId, cellX = x, cellY = y, facing = _currentPropFacing };
            _propPreviewScratch.Clear();
            _propPreviewScratch.AddRange(PropPlacementValidator.Evaluate(_state, candidate, def));

            var footprintCells = _propPreviewScratch.ConvertAll(e => e.cell);
            if (!PropPlacementUtility.GetRenderBounds(footprintCells, def.Category, _currentPropFacing, out var min, out var max))
            {
                visuals.ClearPreview();
                return;
            }

            Sprite sprite = def.HasTexture ? def.GetSprite(_currentPropFacing) : DefVisualUtility.MissingSprite;
            bool flip = _currentPropFacing == PropFacing.West;
            Color tint = def.HasTexture ? def.TintColor : DefVisualUtility.MissingColor;
            Color secondary = def.HasTexture ? def.SecondaryTintColor : Color.white;
            Texture2D mask = def.HasTexture ? def.GetMask(_currentPropFacing) : null;

            visuals.ShowPreview(_propPreviewScratch, sprite, tint, secondary, mask, min, max, flip);
        }

        private bool IsWallLikeAt(int x, int y) => _state.InBounds(x, y) && _state.GetNormal(x, y) == NormalType.Wall;

        private void PaintLine(Vector2Int from, Vector2Int to, bool erase)
        {
            int steps = Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y));
            if (steps == 0) { PaintCell(to.x, to.y, erase); return; }
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(from.y, to.y, t));
                PaintCell(x, y, erase);
            }
        }

        private void PaintCell(int x, int y, bool erase)
        {
            if (!_state.InBounds(x, y)) return;

            switch (_tool)
            {
                case BuilderTool.Floor:
                    if (erase)
                    {
                        _state.SetFloor(x, y, FloorType.Void);
                    }
                    else
                    {
                        _state.SetFloor(x, y, _floorBrushType);
                        _state.SetFloorDef(x, y, _floorDefBrush);
                    }
                    visuals.RefreshCell(_state, x, y);
                    break;

                case BuilderTool.Normal:
                    if (erase)
                    {
                        _state.SetNormal(x, y, NormalType.Empty);
                    }
                    else
                    {
                        _state.SetNormal(x, y, _normalBrushType);
                        if (_normalBrushType == NormalType.Wall) _state.SetWallDef(x, y, _wallDefBrush);
                        else if (_normalBrushType == NormalType.Door) _state.SetDoorDef(x, y, _doorDefBrush);
                    }
                    visuals.RefreshCell(_state, x, y);
                    visuals.RefreshCell(_state, x + 1, y);
                    visuals.RefreshCell(_state, x - 1, y);
                    visuals.RefreshCell(_state, x, y + 1);
                    visuals.RefreshCell(_state, x, y - 1);
                    break;

                case BuilderTool.Connector:
                    if (_state.GetNormal(x, y) != NormalType.Wall)
                    {
                        _statusMessage = "Connectors can only be placed on Wall cells.";
                        return;
                    }
                    _state.SetConnector(x, y, erase ? ConnectorType.None : _connectorBrush);
                    visuals.RefreshCell(_state, x, y);
                    break;
            }
        }

        private void PlaceProp(int x, int y)
        {
            var cell = new Vector2Int(x, y);
            if (PropPlacementUtility.FindPlacementAtCell(_state, cell).HasValue)
            {
                _statusMessage = "Cell already occupied (right-click to remove).";
                return;
            }

            if (string.IsNullOrEmpty(_currentPropId)) { _statusMessage = "Select a prop def first."; return; }
            var def = DefDatabase.Get<PropDef>(_currentPropId);
            if (def == null) { _statusMessage = "Unknown prop def."; return; }

            var candidate = new PropPlacement { propId = _currentPropId, cellX = x, cellY = y, facing = _currentPropFacing };
            if (!PropPlacementValidator.IsValid(_state, candidate, def, out string reason))
            {
                _statusMessage = reason;
                return;
            }

            _state.SetProp(candidate);
            visuals.RefreshProp(candidate);
        }

        private void RemovePropAt(int x, int y)
        {
            var hit = PropPlacementUtility.FindPlacementAtCell(_state, new Vector2Int(x, y));
            if (!hit.HasValue) return;

            var origin = new Vector2Int(hit.Value.cellX, hit.Value.cellY);
            if (_state.RemoveProp(origin.x, origin.y)) visuals.RemoveProp(origin);
        }
    }
}

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GameDefs;

namespace RoomGen
{
    public enum BuilderTool { Floor, Normal, Connector, Prop }

    [RequireComponent(typeof(RoomBuilderVisuals))]
    public class RoomBuilderController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private RoomBuilderVisuals visuals;
        [SerializeField] private float panelWidth = 300f;

        private RoomData _state;
        private BuilderTool _tool = BuilderTool.Floor;

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

        private readonly List<(Vector2Int cell, bool valid)> _singleCellScratch = new List<(Vector2Int, bool)>(1);
        private readonly List<(Vector2Int cell, bool valid)> _propPreviewScratch = new List<(Vector2Int, bool)>();

        private string _newWidthField = "5";
        private string _newHeightField = "5";
        private string _templateIdField = "NewRoom";
        private string _typeTagField = "";
        private string _zoneTagField = "";
        private Vector2 _panelScroll;
        private Vector2 _fileListScroll;
        private List<string> _roomFiles = new List<string>();
        private string _statusMessage = "";

        public BuilderTool ActiveTool => _tool;
        public string CurrentFloorDefBrush => _floorDefBrush;
        public string CurrentWallDefBrush => _wallDefBrush;
        public string CurrentDoorDefBrush => _doorDefBrush;
        public string CurrentPropDefBrush => _currentPropId;
        public ConnectorType CurrentConnectorBrush => _connectorBrush;
        public bool IsWaterBrushActive => _floorBrushType == FloorType.Liquid;

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

        private bool IsTypingInField => GUIUtility.keyboardControl != 0;

        private void Update()
        {
            if (_state == null || targetCamera == null) return;
            if (Input.mousePosition.x < panelWidth) { visuals.ClearPreview(); return; }
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
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
                if (!IsTypingInField && Input.GetKeyDown(KeyCode.R))
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
                bool northSouthOpen = !n && !s;
                bool eastWestOpen = !e && !w;
                bool isNorthOrientation = !(eastWestOpen && !northSouthOpen);

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

        private void CreateNewRoom()
        {
            int w = ParseIntOrDefault(_newWidthField, 5);
            int h = ParseIntOrDefault(_newHeightField, 5);
            _state = new RoomData();
            _state.Allocate(w, h);
            _state.templateId = _templateIdField;
            visuals.Rebuild(_state);
            _statusMessage = $"Created {_state.width}x{_state.height} room.";
        }

        private void SaveRoom()
        {
            if (_state == null) { _statusMessage = "Nothing to save."; return; }
            if (!string.IsNullOrWhiteSpace(_templateIdField)) _state.templateId = _templateIdField;
            RoomLibraryIO.Save(_state);
            RefreshFileList();
            _statusMessage = $"Saved '{_state.templateId}' to {RoomLibraryIO.RoomsFolder}";
        }

        private void LoadRoom(string path)
        {
            _state = RoomLibraryIO.Load(path);
            _templateIdField = _state.templateId;
            _newWidthField = _state.width.ToString();
            _newHeightField = _state.height.ToString();
            visuals.Rebuild(_state);
            _statusMessage = $"Loaded '{_state.templateId}'.";
        }

        private void RefreshFileList() => _roomFiles = RoomLibraryIO.ListRoomFiles();

        private static int ParseIntOrDefault(string s, int fallback) => int.TryParse(s, out int v) ? Mathf.Max(3, v) : fallback;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0, 0, panelWidth, Screen.height), GUI.skin.box);
            _panelScroll = GUILayout.BeginScrollView(_panelScroll);

            GUILayout.Label("Room Builder");
            GUILayout.Space(6);

            DrawNewRoomSection();
            GUILayout.Space(8);
            DrawSaveLoadSection();
            GUILayout.Space(8);
            DrawTagSection();
            GUILayout.Space(8);
            DrawRoomDefaultsSection();
            GUILayout.Space(8);
            DrawWeightsSection();
            GUILayout.Space(8);

            GUILayout.Label(_statusMessage);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawNewRoomSection()
        {
            GUILayout.Label("New Room (min 3x3)");
            GUILayout.BeginHorizontal();
            GUILayout.Label("W", GUILayout.Width(14));
            _newWidthField = GUILayout.TextField(_newWidthField, GUILayout.Width(40));
            GUILayout.Label("H", GUILayout.Width(14));
            _newHeightField = GUILayout.TextField(_newHeightField, GUILayout.Width(40));
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Create New Room")) CreateNewRoom();
        }

        private void DrawSaveLoadSection()
        {
            GUILayout.Label("Save / Load");
            GUILayout.Label("Template Id");
            _templateIdField = GUILayout.TextField(_templateIdField);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save")) SaveRoom();
            if (GUILayout.Button("Refresh List")) RefreshFileList();
            GUILayout.EndHorizontal();

            _fileListScroll = GUILayout.BeginScrollView(_fileListScroll, GUILayout.Height(100));
            foreach (var file in _roomFiles)
            {
                if (GUILayout.Button(Path.GetFileNameWithoutExtension(file)))
                    LoadRoom(file);
            }
            GUILayout.EndScrollView();
        }

        private void DrawTagSection()
        {
            if (_state == null) { GUILayout.Label("(create or load a room first)"); return; }

            GUILayout.Label("Type Tags");
            GUILayout.BeginHorizontal();
            _typeTagField = GUILayout.TextField(_typeTagField);
            if (GUILayout.Button("Add", GUILayout.Width(40)) && !string.IsNullOrWhiteSpace(_typeTagField))
            {
                _state.typeTags.Add(_typeTagField.Trim());
                _typeTagField = "";
            }
            GUILayout.EndHorizontal();
            DrawTagList(_state.typeTags);

            GUILayout.Label("Zone Tags (reserved)");
            GUILayout.BeginHorizontal();
            _zoneTagField = GUILayout.TextField(_zoneTagField);
            if (GUILayout.Button("Add", GUILayout.Width(40)) && !string.IsNullOrWhiteSpace(_zoneTagField))
            {
                _state.zoneTags.Add(_zoneTagField.Trim());
                _zoneTagField = "";
            }
            GUILayout.EndHorizontal();
            DrawTagList(_state.zoneTags);
        }

        private void DrawTagList(List<string> tags)
        {
            for (int i = tags.Count - 1; i >= 0; i--)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(tags[i]);
                if (GUILayout.Button("x", GUILayout.Width(22))) tags.RemoveAt(i);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawRoomDefaultsSection()
        {
            if (_state == null) return;

            GUILayout.Label("Preferred Single Door Def (used for generated connections)");
            DrawDefBrushButtons(DefDatabase.All<DoorDef>(), _state.preferredSingleDoorDef, v => _state.preferredSingleDoorDef = v);
            if (GUILayout.Button("Clear (use generator default)")) _state.preferredSingleDoorDef = null;

            GUILayout.Space(4);

            GUILayout.Label("Preferred Double Door Def (used for generated connections)");
            DrawDefBrushButtons(DefDatabase.All<DoorDef>(), _state.preferredDoubleDoorDef, v => _state.preferredDoubleDoorDef = v);
            if (GUILayout.Button("Clear (use generator default)")) _state.preferredDoubleDoorDef = null;
        }

        private void DrawWeightsSection()
        {
            if (_state == null) return;
            GUILayout.Label("Generation Weights");

            GUILayout.Label($"Desired Connections: {_state.desiredConnections}");
            _state.desiredConnections = Mathf.RoundToInt(GUILayout.HorizontalSlider(_state.desiredConnections, 1, 8));

            GUILayout.Label($"Chance To Connect (below target): {_state.chanceToConnectWhenBelowTarget:0.00}");
            _state.chanceToConnectWhenBelowTarget = GUILayout.HorizontalSlider(_state.chanceToConnectWhenBelowTarget, 0f, 1f);

            GUILayout.Label($"Extra Connection Chance: {_state.extraConnectionChance:0.00}");
            _state.extraConnectionChance = GUILayout.HorizontalSlider(_state.extraConnectionChance, 0f, 1f);

            GUILayout.Label($"Selection Weight: {_state.selectionWeight:0.00}");
            _state.selectionWeight = GUILayout.HorizontalSlider(_state.selectionWeight, 0.1f, 5f);
        }

        private void DrawDefBrushButtons<T>(IReadOnlyList<T> defs, string current, System.Action<string> onPick) where T : Def
        {
            if (defs.Count == 0)
            {
                GUILayout.Label("no defs found");
                return;
            }
            foreach (var def in defs)
            {
                bool selected = def.DefName == current;

                GUI.backgroundColor = selected ? Color.cyan : Color.white;
                if (GUILayout.Button(def.DefName)) onPick(def.DefName);
            }
            GUI.backgroundColor = Color.white;
        }

        public void SetActiveTool(BuilderTool tool)
        {
            _tool = tool;
            visuals.SetConnectorOverlayVisible(_tool == BuilderTool.Connector);
            visuals.ClearPreview();
        }

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
    }
}
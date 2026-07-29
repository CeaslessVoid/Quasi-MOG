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
        private Vector2Int? _selectedPropCell;

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
                if (Input.GetMouseButtonDown(0)) PlaceOrSelectProp(cx, cy);
                if (Input.GetMouseButtonDown(1)) RemovePropAt(cx, cy);
                if (!IsTypingInField && Input.GetKeyDown(KeyCode.R)) RotateSelectedProp();
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
                visuals.ShowPreview(_singleCellScratch, null, Color.white, cell, cell, false);
                return;
            }

            Sprite sprite;
            Color color;
            if (_floorBrushType == FloorType.Floor)
            {
                var def = DefDatabase.Get<FloorDef>(_floorDefBrush);
                if (def != null && def.HasTexture) { sprite = def.Sprite; color = def.TintColor; }
                else { sprite = DefVisualUtility.MissingSprite; color = Color.white; }
            }
            else if (_floorBrushType == FloorType.Water)
            {
                sprite = DefVisualUtility.SolidSprite;
                color = new Color(0.2f, 0.4f, 0.9f);
            }
            else
            {
                sprite = DefVisualUtility.SolidSprite;
                color = new Color(1f, 1f, 1f, 0.15f);
            }

            visuals.ShowPreview(_singleCellScratch, sprite, color, cell, cell, false);
        }

        private void PreviewNormal(int x, int y)
        {
            var cell = new Vector2Int(x, y);
            _singleCellScratch.Clear();
            _singleCellScratch.Add((cell, true));

            if (Input.GetMouseButton(1) || _normalBrushType == NormalType.Empty)
            {
                visuals.ShowPreview(_singleCellScratch, null, Color.white, cell, cell, false);
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
                Color color = def != null && def.HasTexture ? def.TintColor : Color.white;
                visuals.ShowPreview(_singleCellScratch, sprite, color, cell, cell, false);
            }
            else
            {
                bool northSouthOpen = !n && !s;
                bool eastWestOpen = !e && !w;
                bool isNorthOrientation = !(eastWestOpen && !northSouthOpen);

                var def = DefDatabase.Get<DoorDef>(_doorDefBrush);
                Sprite sprite = def != null ? (isNorthOrientation ? def.NorthSprite : def.EastSprite) : null;
                Color color = def != null ? def.TintColor : new Color(0.65f, 0.4f, 0.1f, 1f);
                if (sprite == null) sprite = DefVisualUtility.MissingSprite;
                visuals.ShowPreview(_singleCellScratch, sprite, color, cell, cell, false);
            }
        }

        private void PreviewConnector(int x, int y)
        {
            var cell = new Vector2Int(x, y);
            bool valid = _state.GetNormal(x, y) == NormalType.Wall;
            _singleCellScratch.Clear();
            _singleCellScratch.Add((cell, valid));
            visuals.ShowPreview(_singleCellScratch, null, Color.white, cell, cell, false);
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

            if (!PropPlacementUtility.GetFootprintBounds(_propPreviewScratch.ConvertAll(e => e.cell), out var min, out var max))
            {
                visuals.ClearPreview();
                return;
            }

            if (def.Category == PropCategory.Wall)
            {
                var offset = PropPlacementUtility.GetWallMountOffset(_currentPropFacing);
                min += offset;
                max += offset;
            }

            Sprite sprite = def.HasTexture ? def.GetSprite(_currentPropFacing) : DefVisualUtility.MissingSprite;
            Color color = def.HasTexture ? def.TintColor : DefVisualUtility.MissingColor;
            bool flip = _currentPropFacing == PropFacing.West;

            visuals.ShowPreview(_propPreviewScratch, sprite, color, min, max, flip);
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
                        if (_floorBrushType == FloorType.Floor) _state.SetFloorDef(x, y, _floorDefBrush);
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

        private void PlaceOrSelectProp(int x, int y)
        {
            var cell = new Vector2Int(x, y);
            var existing = PropPlacementUtility.FindPlacementAtCell(_state, cell);
            if (existing.HasValue)
            {
                _selectedPropCell = new Vector2Int(existing.Value.cellX, existing.Value.cellY);
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
            _selectedPropCell = cell;
        }

        private void RemovePropAt(int x, int y)
        {
            var hit = PropPlacementUtility.FindPlacementAtCell(_state, new Vector2Int(x, y));
            if (!hit.HasValue) return;

            var origin = new Vector2Int(hit.Value.cellX, hit.Value.cellY);
            if (_state.RemoveProp(origin.x, origin.y)) visuals.RemoveProp(origin);
            if (_selectedPropCell == origin) _selectedPropCell = null;
        }

        private void RotateSelectedProp()
        {
            if (!_selectedPropCell.HasValue) return;
            var cell = _selectedPropCell.Value;
            var existing = _state.GetPropAt(cell.x, cell.y);
            if (!existing.HasValue) return;

            var p = existing.Value;
            p.facing = (PropFacing)(((int)p.facing + 1) % 4);
            _state.SetProp(p);
            visuals.RefreshProp(p);
        }

        private void CreateNewRoom()
        {
            int w = ParseIntOrDefault(_newWidthField, 5);
            int h = ParseIntOrDefault(_newHeightField, 5);
            _state = new RoomData();
            _state.Allocate(w, h);
            _state.templateId = _templateIdField;
            visuals.Rebuild(_state);
            _selectedPropCell = null;
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
            _selectedPropCell = null;
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
            DrawToolSection();
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

        private void DrawToolSection()
        {
            if (_state == null) return;
            GUILayout.Label("Tool (left-drag paint / right-drag erase)");

            GUILayout.BeginHorizontal();
            DrawToolButton(BuilderTool.Floor, "Floor");
            DrawToolButton(BuilderTool.Normal, "Normal");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            DrawToolButton(BuilderTool.Connector, "Connector");
            DrawToolButton(BuilderTool.Prop, "Prop");
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            switch (_tool)
            {
                case BuilderTool.Floor:
                    GUILayout.Label("Floor Type");
                    DrawEnumBrushButtons(new[] { FloorType.Void, FloorType.Floor, FloorType.Water }, _floorBrushType, v => _floorBrushType = v);
                    if (_floorBrushType == FloorType.Floor)
                    {
                        GUILayout.Label("Floor Def");
                        DrawDefBrushButtons(DefDatabase.All<FloorDef>(), _floorDefBrush, v => _floorDefBrush = v);
                    }
                    break;
                case BuilderTool.Normal:
                    GUILayout.Label("Structure Type");
                    DrawEnumBrushButtons(new[] { NormalType.Empty, NormalType.Wall, NormalType.Door }, _normalBrushType, v => _normalBrushType = v);
                    if (_normalBrushType == NormalType.Wall)
                    {
                        GUILayout.Label("Wall Def");
                        DrawDefBrushButtons(DefDatabase.All<WallDef>(), _wallDefBrush, v => _wallDefBrush = v);
                    }
                    else if (_normalBrushType == NormalType.Door)
                    {
                        GUILayout.Label("Door Def");
                        DrawDefBrushButtons(DefDatabase.All<DoorDef>(), _doorDefBrush, v => _doorDefBrush = v);
                    }
                    break;
                case BuilderTool.Connector:
                    GUILayout.Label("Connector Brush (any wall cell)");
                    DrawEnumBrushButtons(new[] { ConnectorType.None, ConnectorType.Normal, ConnectorType.Restricted, ConnectorType.AlwaysDouble }, _connectorBrush, v => _connectorBrush = v);
                    break;
                case BuilderTool.Prop:
                    GUILayout.Label("Prop Def");
                    DrawDefBrushButtons(DefDatabase.All<PropDef>(), _currentPropId, v => _currentPropId = v);
                    GUILayout.Label($"Facing: {_currentPropFacing}");
                    if (GUILayout.Button("Rotate Pending +90")) _currentPropFacing = (PropFacing)(((int)_currentPropFacing + 1) % 4);
                    GUILayout.Label("Click: place/select   Right-click: delete   R: rotate selected");
                    break;
            }
        }

        private void DrawToolButton(BuilderTool tool, string label)
        {
            GUI.backgroundColor = _tool == tool ? Color.cyan : Color.white;
            if (GUILayout.Button(label))
            {
                _tool = tool;
                visuals.SetConnectorOverlayVisible(_tool == BuilderTool.Connector);
                visuals.ClearPreview();
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawEnumBrushButtons<T>(T[] values, T current, System.Action<T> onPick) where T : System.Enum
        {
            foreach (var v in values)
            {
                GUI.backgroundColor = EqualityComparer<T>.Default.Equals(v, current) ? Color.cyan : Color.white;
                if (GUILayout.Button(v.ToString())) onPick(v);
            }
            GUI.backgroundColor = Color.white;
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
    }
}
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RoomGen
{
    public enum BuilderTool { Floor, Normal, Connector, Prop }

    /// <summary>
    /// The in-game room builder. Left-drag paints the current tool's brush, right-drag
    /// erases (sets back to the layer's default value). No sprites - cells render as flat
    /// colored quads via RoomBuilderVisuals. Panel on the left is plain IMGUI (OnGUI) so
    /// there's no scene/prefab setup required - just drop this + RoomBuilderVisuals on a
    /// GameObject (or use RoomBuilderBootstrap to do that for you) and press Play.
    /// </summary>
    [RequireComponent(typeof(RoomBuilderVisuals))]
    public class RoomBuilderController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private RoomBuilderVisuals visuals;
        [SerializeField] private float panelWidth = 300f;

        [Header("Camera Control")]
        [SerializeField] private float panSpeed = 10f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoom = 2f;
        [SerializeField] private float maxZoom = 40f;

        private RoomBuilderState _state;
        private BuilderTool _tool = BuilderTool.Floor;

        private FloorType _floorBrush = FloorType.Floor;
        private NormalType _normalBrush = NormalType.Wall;
        private ConnectorType _connectorBrush = ConnectorType.Normal;

        private string _currentPropId = "prop_crate";
        private int _currentPropRotation = 0;
        private Vector2Int? _selectedPropCell;

        private Vector2Int? _lastPaintCellLeft;
        private Vector2Int? _lastPaintCellRight;

        // IMGUI field buffers
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
        }

        private bool IsTypingInField => GUIUtility.keyboardControl != 0;

        private void Update()
        {
            HandleCameraControls();

            if (_state == null || targetCamera == null) return;
            if (Input.mousePosition.x < panelWidth) return; // pointer is over the tool panel

            var mouseWorld = targetCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0f;
            visuals.TryWorldToCell(mouseWorld, out int cx, out int cy);

            if (!_state.InBounds(cx, cy))
            {
                _lastPaintCellLeft = null;
                _lastPaintCellRight = null;
                return;
            }

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

        private void HandleCameraControls()
        {
            if (targetCamera == null || IsTypingInField) return;

            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move.y += 1f;
            if (Input.GetKey(KeyCode.S)) move.y -= 1f;
            if (Input.GetKey(KeyCode.A)) move.x -= 1f;
            if (Input.GetKey(KeyCode.D)) move.x += 1f;

            if (move != Vector3.zero)
            {
                // Scale pan speed with zoom so it feels the same whether zoomed in or out.
                float speedScale = targetCamera.orthographicSize / 10f;
                targetCamera.transform.position += move.normalized * (panSpeed * speedScale * Time.deltaTime);
            }

            if (Input.mousePosition.x >= panelWidth)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.0001f)
                    targetCamera.orthographicSize = Mathf.Clamp(targetCamera.orthographicSize - scroll * zoomSpeed, minZoom, maxZoom);
            }
        }

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
                    _state.SetFloorAt(x, y, erase ? FloorType.Void : _floorBrush);
                    break;
                case BuilderTool.Normal:
                    _state.SetNormalAt(x, y, erase ? NormalType.Empty : _normalBrush);
                    break;
                case BuilderTool.Connector:
                    if (!_state.IsBoundary(x, y))
                    {
                        _statusMessage = "Connectors can only be placed on boundary walls.";
                        return;
                    }
                    _state.SetConnectorAt(x, y, erase ? ConnectorType.None : _connectorBrush);
                    break;
            }
            visuals.RefreshCell(_state, x, y);
        }

        private void PlaceOrSelectProp(int x, int y)
        {
            var existing = _state.GetPropAt(x, y);
            if (existing.HasValue)
            {
                _selectedPropCell = new Vector2Int(x, y);
                return;
            }

            var prop = new PropPlacement
            {
                propId = string.IsNullOrEmpty(_currentPropId) ? "prop" : _currentPropId,
                cellX = x,
                cellY = y,
                baseRotationDeg = _currentPropRotation
            };
            _state.SetProp(prop);
            visuals.RefreshProp(prop);
            _selectedPropCell = new Vector2Int(x, y);
        }

        private void RemovePropAt(int x, int y)
        {
            if (_state.RemoveProp(x, y)) visuals.RemoveProp(new Vector2Int(x, y));
            if (_selectedPropCell == new Vector2Int(x, y)) _selectedPropCell = null;
        }

        private void RotateSelectedProp()
        {
            if (!_selectedPropCell.HasValue) return;
            var cell = _selectedPropCell.Value;
            var existing = _state.GetPropAt(cell.x, cell.y);
            if (!existing.HasValue) return;

            var p = existing.Value;
            p.baseRotationDeg = (p.baseRotationDeg + 90) % 360;
            _state.SetProp(p);
            visuals.RefreshProp(p);
        }

        // ---------- Room lifecycle ----------

        private void CreateNewRoom()
        {
            int w = ParseIntOrDefault(_newWidthField, 5);
            int h = ParseIntOrDefault(_newHeightField, 5);
            _state = new RoomBuilderState();
            _state.Initialize(w, h);
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

        // ---------- OnGUI ----------

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

            GUILayout.Label($"Reconnection Chance: {_state.reconnectionChance:0.00}");
            _state.reconnectionChance = GUILayout.HorizontalSlider(_state.reconnectionChance, 0f, 1f);

            GUILayout.Label($"Reconnection Double Chance: {_state.reconnectionDoubleChance:0.00}");
            _state.reconnectionDoubleChance = GUILayout.HorizontalSlider(_state.reconnectionDoubleChance, 0f, 1f);
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
                    GUILayout.Label("Floor Brush");
                    DrawEnumBrushButtons(new[] { FloorType.Void, FloorType.Floor, FloorType.Water }, _floorBrush, v => _floorBrush = v);
                    break;
                case BuilderTool.Normal:
                    GUILayout.Label("Normal Brush");
                    DrawEnumBrushButtons(new[] { NormalType.Empty, NormalType.Wall, NormalType.Door }, _normalBrush, v => _normalBrush = v);
                    break;
                case BuilderTool.Connector:
                    GUILayout.Label("Connector Brush (boundary only)");
                    DrawEnumBrushButtons(new[] { ConnectorType.None, ConnectorType.Normal, ConnectorType.Restricted, ConnectorType.AlwaysDouble }, _connectorBrush, v => _connectorBrush = v);
                    break;
                case BuilderTool.Prop:
                    GUILayout.Label("Prop Id");
                    _currentPropId = GUILayout.TextField(_currentPropId);
                    GUILayout.Label($"Placement Rotation: {_currentPropRotation}°");
                    if (GUILayout.Button("Rotate Pending +90")) _currentPropRotation = (_currentPropRotation + 90) % 360;
                    GUILayout.Label("Click: place/select   Right-click: delete   R: rotate selected");
                    break;
            }
        }

        private void DrawToolButton(BuilderTool tool, string label)
        {
            GUI.backgroundColor = _tool == tool ? Color.cyan : Color.white;
            if (GUILayout.Button(label)) _tool = tool;
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
    }
}

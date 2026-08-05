using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameDefs;

namespace RoomGen.UI
{
    public class RoomBuilderUIController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private RoomBuilderController controller;
        [SerializeField] private RoomBuilderTopBarController topBar;

        [Header("Bottom Tabs")]
        [SerializeField] private Button floorTabButton;
        [SerializeField] private Button normalTabButton;
        [SerializeField] private Button propTabButton;
        [SerializeField] private Button connectorTabButton;
        [SerializeField] private Color activeTabColor = new Color(0.25f, 0.65f, 1f);
        [SerializeField] private Color inactiveTabColor = Color.white;

        [Header("Side Panel")]
        [SerializeField] private GameObject sidePanelRoot;
        [SerializeField] private CanvasGroup sidePanelVisibilityGroup;
        [SerializeField] private CategoryTabBar categoryTabBar;
        [SerializeField] private DefListPanel defListPanel;
        [SerializeField] private TMP_InputField searchInput;

        [Header("Prop Filters")]
        [SerializeField] private GameObject propFilterRoot;
        [SerializeField] private Toggle includeStorageToggle;
        [SerializeField] private Toggle includeWallPropsToggle;

        [Header("Auto-Hide On Drag")]
        [SerializeField] private bool debugDrawThresholds = true;
        [SerializeField] private float hideThresholdY = 900f;
        [SerializeField] private float showThresholdY = 500f;
        [SerializeField] private float showThresholdX = 150f;
        [SerializeField] private RectTransform debugHideLine;
        [SerializeField] private RectTransform debugShowLineY;
        [SerializeField] private RectTransform debugShowLineX;

        private static readonly string[] NormalCategoryLabels = { "Walls", "Doors" };
        private static readonly string[] PropCategoryLabels =
            { "All", "Work / Production", "Living Quarters", "Decorations", "Plants", "Entertainment" };
        private static readonly PropUseCategory[] PropCategoryValues =
        {
            PropUseCategory.None,
            PropUseCategory.WorkProduction,
            PropUseCategory.LivingQuarters,
            PropUseCategory.Decoration,
            PropUseCategory.Plants,
            PropUseCategory.Entertainment
        };

        private BuilderTool _activeTool = BuilderTool.None;
        private int _normalCategoryIndex;
        private int _propCategoryIndex;
        private string _searchText = "";
        private bool _hiddenByDrag;

        public bool IsSidePanelOpen => _activeTool != BuilderTool.None;

        private void Awake()
        {
            floorTabButton.onClick.AddListener(() => SelectTool(BuilderTool.Floor));
            normalTabButton.onClick.AddListener(() => SelectTool(BuilderTool.Normal));
            propTabButton.onClick.AddListener(() => SelectTool(BuilderTool.Prop));
            connectorTabButton.onClick.AddListener(() => SelectTool(BuilderTool.Connector));

            if (searchInput != null) searchInput.onValueChanged.AddListener(HandleSearchChanged);
            if (includeStorageToggle != null)
            {
                includeStorageToggle.isOn = true;
                includeStorageToggle.onValueChanged.AddListener(_ => RefreshList());
            }
            if (includeWallPropsToggle != null)
            {
                includeWallPropsToggle.isOn = true;
                includeWallPropsToggle.onValueChanged.AddListener(_ => RefreshList());
            }

            sidePanelRoot.SetActive(false);

            if (debugHideLine != null) debugHideLine.gameObject.SetActive(debugDrawThresholds);
            if (debugShowLineY != null) debugShowLineY.gameObject.SetActive(debugDrawThresholds);
            if (debugShowLineX != null) debugShowLineX.gameObject.SetActive(debugDrawThresholds);
            PositionDebugLines();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseSidePanel();
                topBar.CloseAll();
            }

            UpdateDragVisibility();
        }

        private void SelectTool(BuilderTool tool)
        {
            _activeTool = tool;
            controller.SetActiveTool(tool);
            topBar.CloseAll();
            RefreshTabHighlight();
            RefreshPanelLayout();
            RefreshList();
        }

        public void CloseSidePanel()
        {
            _activeTool = BuilderTool.None;
            controller.ClearActiveTool();
            sidePanelRoot.SetActive(false);
            RefreshTabHighlight();
        }

        private void RefreshTabHighlight()
        {
            SetTabColor(floorTabButton, _activeTool == BuilderTool.Floor);
            SetTabColor(normalTabButton, _activeTool == BuilderTool.Normal);
            SetTabColor(propTabButton, _activeTool == BuilderTool.Prop);
            SetTabColor(connectorTabButton, _activeTool == BuilderTool.Connector);
        }

        private void SetTabColor(Button button, bool active)
        {
            if (button.targetGraphic is Image image)
                image.color = active ? activeTabColor : inactiveTabColor;
        }

        private void RefreshPanelLayout()
        {
            if (_activeTool == BuilderTool.None)
            {
                sidePanelRoot.SetActive(false);
                return;
            }

            sidePanelRoot.SetActive(true);

            if (searchInput != null) searchInput.gameObject.SetActive(_activeTool != BuilderTool.Connector);
            if (propFilterRoot != null) propFilterRoot.SetActive(_activeTool == BuilderTool.Prop);

            switch (_activeTool)
            {
                case BuilderTool.Floor:
                case BuilderTool.Connector:
                    categoryTabBar.Hide();
                    break;

                case BuilderTool.Normal:
                    categoryTabBar.Show();
                    categoryTabBar.Setup(NormalCategoryLabels, i => { _normalCategoryIndex = i; RefreshList(); }, _normalCategoryIndex);
                    break;

                case BuilderTool.Prop:
                    categoryTabBar.Show();
                    categoryTabBar.Setup(PropCategoryLabels, i => { _propCategoryIndex = i; RefreshList(); }, _propCategoryIndex);
                    break;
            }
        }

        private void HandleSearchChanged(string value)
        {
            _searchText = value ?? "";
            RefreshList();
        }

        private void RefreshList()
        {
            switch (_activeTool)
            {
                case BuilderTool.Floor:
                    defListPanel.Populate(BuildFloorItems(), name => name == controller.CurrentFloorDefBrush);
                    break;

                case BuilderTool.Normal:
                    if (_normalCategoryIndex == 1)
                        defListPanel.Populate(BuildDoorItems(), name => name == controller.CurrentDoorDefBrush);
                    else
                        defListPanel.Populate(BuildWallItems(), name => name == controller.CurrentWallDefBrush);
                    break;

                case BuilderTool.Prop:
                    defListPanel.Populate(BuildPropItems(), name => name == controller.CurrentPropDefBrush);
                    break;

                case BuilderTool.Connector:
                    defListPanel.Populate(BuildConnectorItems(), name => name == controller.CurrentConnectorBrush.ToString());
                    break;
            }
        }

        private bool Matches(Def def)
        {
            string needle = _searchText?.Trim();
            if (string.IsNullOrEmpty(needle)) return true;
            bool nameMatch = def.DefName != null && def.DefName.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
            bool displayMatch = def.DisplayName != null && def.DisplayName.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
            return nameMatch || displayMatch;
        }

        private List<PlaceableItem> BuildFloorItems()
        {
            var result = new List<PlaceableItem>();
            result.AddRange(DefDatabase.All<FloorDef>().Where(Matches)
                .Select(def => new PlaceableItem(def.DefName, def.DisplayName, def.Icon, () => controller.SetFloorDefBrush(def.DefName))));
            result.AddRange(DefDatabase.All<LiquidDef>().Where(Matches)
                .Select(def => new PlaceableItem(def.DefName, def.DisplayName, def.Icon, () => controller.SetLiquidDefBrush(def.DefName))));
            return result;
        }

        private List<PlaceableItem> BuildWallItems() =>
            DefDatabase.All<WallDef>().Where(Matches)
                .Select(def => new PlaceableItem(def.DefName, def.DisplayName, def.Icon, () => controller.SetWallDefBrush(def.DefName)))
                .ToList();

        private List<PlaceableItem> BuildDoorItems() =>
            DefDatabase.All<DoorDef>().Where(Matches)
                .Select(def => new PlaceableItem(def.DefName, def.DisplayName, def.Icon, () => controller.SetDoorDefBrush(def.DefName)))
                .ToList();

        private List<PlaceableItem> BuildPropItems()
        {
            bool includeStorage = includeStorageToggle == null || includeStorageToggle.isOn;
            bool includeWallProps = includeWallPropsToggle == null || includeWallPropsToggle.isOn;
            PropUseCategory categoryFilter = _propCategoryIndex >= 0 && _propCategoryIndex < PropCategoryValues.Length
                ? PropCategoryValues[_propCategoryIndex]
                : PropUseCategory.None;

            return DefDatabase.All<PropDef>()
                .Where(Matches)
                .Where(def => categoryFilter == PropUseCategory.None || (def.UseCategories & categoryFilter) != 0)
                .Where(def => includeStorage || !def.CanHaveStorage)
                .Where(def => includeWallProps || def.Category != PropCategory.Wall)
                .Select(def => new PlaceableItem(def.DefName, def.DisplayName, def.Icon, () => controller.SetPropBrush(def.DefName)))
                .ToList();
        }

        private List<PlaceableItem> BuildConnectorItems()
        {
            var values = new[] { ConnectorType.Normal, ConnectorType.Restricted, ConnectorType.AlwaysDouble };
            var result = new List<PlaceableItem>(values.Length);
            foreach (var v in values)
            {
                var captured = v;
                result.Add(new PlaceableItem(v.ToString(), v.ToString(), null, () => controller.SetConnectorBrush(captured)));
            }
            return result;
        }

        private void UpdateDragVisibility()
        {
            if (debugDrawThresholds) PositionDebugLines();
            if (sidePanelVisibilityGroup == null || !IsSidePanelOpen) return;

            Vector2 mouse = Input.mousePosition;

            if (!_hiddenByDrag && mouse.y >= hideThresholdY)
                _hiddenByDrag = true;
            else if (_hiddenByDrag && (mouse.y <= showThresholdY || mouse.x <= showThresholdX))
                _hiddenByDrag = false;

            bool visible = !_hiddenByDrag;
            sidePanelVisibilityGroup.alpha = visible ? 1f : 0f;
            sidePanelVisibilityGroup.interactable = visible;
            sidePanelVisibilityGroup.blocksRaycasts = visible;
        }

        private void PositionDebugLines()
        {
            PositionHorizontalLine(debugHideLine, hideThresholdY, Color.red);
            PositionHorizontalLine(debugShowLineY, showThresholdY, new Color(0.2f, 1f, 0.3f));
            PositionVerticalLine(debugShowLineX, showThresholdX, new Color(0.3f, 0.6f, 1f));
        }

        private void PositionHorizontalLine(RectTransform line, float screenY, Color color)
        {
            if (line == null) return;
            var canvasRect = (RectTransform)line.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, new Vector2(0f, screenY), null, out var local);
            line.anchoredPosition = new Vector2(line.anchoredPosition.x, local.y);
            if (line.TryGetComponent<Image>(out var img)) img.color = color;
        }

        private void PositionVerticalLine(RectTransform line, float screenX, Color color)
        {
            if (line == null) return;
            var canvasRect = (RectTransform)line.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, new Vector2(screenX, 0f), null, out var local);
            line.anchoredPosition = new Vector2(local.x, line.anchoredPosition.y);
            if (line.TryGetComponent<Image>(out var img)) img.color = color;
        }
    }
}
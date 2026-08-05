using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RoomGen.UI
{
    public class RoomIOPanel : MonoBehaviour
    {
        [SerializeField] private RoomBuilderController controller;

        [Header("Create")]
        [SerializeField] private TMP_InputField widthInput;
        [SerializeField] private TMP_InputField heightInput;
        [SerializeField] private TMP_InputField templateIdInput;
        [SerializeField] private Button createButton;

        [Header("Save / Load")]
        [SerializeField] private Button saveButton;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private SimpleButtonListView roomListView;

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;

        private string _searchText = "";

        private void Awake()
        {
            createButton.onClick.AddListener(HandleCreate);
            saveButton.onClick.AddListener(HandleSave);
            if (searchInput != null) searchInput.onValueChanged.AddListener(v => { _searchText = v ?? ""; RefreshRoomList(); });
        }

        private void OnEnable() => RefreshRoomList();

        private void Update()
        {
            if (statusText != null) statusText.text = controller.StatusMessage;
        }

        private void HandleCreate()
        {
            int w = ParseOrDefault(widthInput != null ? widthInput.text : "5", 5);
            int h = ParseOrDefault(heightInput != null ? heightInput.text : "5", 5);
            string id = templateIdInput != null && !string.IsNullOrWhiteSpace(templateIdInput.text) ? templateIdInput.text : "NewRoom";

            controller.CreateNewRoom(w, h, id);
            RefreshRoomList();
        }

        private void HandleSave()
        {
            controller.SaveRoom(templateIdInput != null ? templateIdInput.text : null);
            RefreshRoomList();
        }

        private void RefreshRoomList()
        {
            controller.RefreshFileList();
            var files = controller.RoomFiles
                .Where(f => string.IsNullOrEmpty(_searchText) ||
                            Path.GetFileNameWithoutExtension(f).IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            roomListView.Populate(files.Select(f =>
                new SimpleButtonItem(Path.GetFileNameWithoutExtension(f), () => HandleLoad(f))).ToList());
        }

        private void HandleLoad(string path)
        {
            controller.LoadRoom(path);
            var room = controller.CurrentRoom;
            if (room == null) return;
            if (templateIdInput != null) templateIdInput.text = room.templateId;
            if (widthInput != null) widthInput.text = room.width.ToString();
            if (heightInput != null) heightInput.text = room.height.ToString();
        }

        private static int ParseOrDefault(string s, int fallback) => int.TryParse(s, out int v) ? Mathf.Max(3, v) : fallback;
    }
}
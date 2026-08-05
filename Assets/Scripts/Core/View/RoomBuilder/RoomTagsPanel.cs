using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RoomGen.UI
{
    public class RoomTagsPanel : MonoBehaviour
    {
        [SerializeField] private RoomBuilderController controller;

        [Header("Type Tags")]
        [SerializeField] private TMP_InputField typeTagInput;
        [SerializeField] private Button typeTagAddButton;
        [SerializeField] private SimpleButtonListView typeTagListView;

        [Header("Zone Tags")]
        [SerializeField] private TMP_InputField zoneTagInput;
        [SerializeField] private Button zoneTagAddButton;
        [SerializeField] private SimpleButtonListView zoneTagListView;

        private void Awake()
        {
            typeTagAddButton.onClick.AddListener(HandleAddTypeTag);
            zoneTagAddButton.onClick.AddListener(HandleAddZoneTag);
        }

        private void OnEnable() => RefreshLists();

        private void HandleAddTypeTag()
        {
            if (typeTagInput == null || string.IsNullOrWhiteSpace(typeTagInput.text)) return;
            controller.AddTypeTag(typeTagInput.text.Trim());
            typeTagInput.text = "";
            RefreshLists();
        }

        private void HandleAddZoneTag()
        {
            if (zoneTagInput == null || string.IsNullOrWhiteSpace(zoneTagInput.text)) return;
            controller.AddZoneTag(zoneTagInput.text.Trim());
            zoneTagInput.text = "";
            RefreshLists();
        }

        private void RefreshLists()
        {
            var room = controller.CurrentRoom;
            if (room == null)
            {
                typeTagListView.Populate(System.Array.Empty<SimpleButtonItem>());
                zoneTagListView.Populate(System.Array.Empty<SimpleButtonItem>());
                return;
            }

            var typeItems = new List<SimpleButtonItem>(room.typeTags.Count);
            foreach (var tag in room.typeTags)
            {
                var captured = tag;
                typeItems.Add(new SimpleButtonItem(captured, () => { controller.RemoveTypeTag(captured); RefreshLists(); }));
            }
            typeTagListView.Populate(typeItems);

            var zoneItems = new List<SimpleButtonItem>(room.zoneTags.Count);
            foreach (var tag in room.zoneTags)
            {
                var captured = tag;
                zoneItems.Add(new SimpleButtonItem(captured, () => { controller.RemoveZoneTag(captured); RefreshLists(); }));
            }
            zoneTagListView.Populate(zoneItems);
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RoomGen.UI
{
    public class RoomTagsPanel : TopBarWindowPanel
    {
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
            Controller.AddTypeTag(typeTagInput.text.Trim());
            typeTagInput.text = "";
            RefreshLists();
        }

        private void HandleAddZoneTag()
        {
            if (zoneTagInput == null || string.IsNullOrWhiteSpace(zoneTagInput.text)) return;
            Controller.AddZoneTag(zoneTagInput.text.Trim());
            zoneTagInput.text = "";
            RefreshLists();
        }

        private void RefreshLists()
        {
            if (Controller?.CurrentRoom == null) return;

            var room = Controller.CurrentRoom;
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
                typeItems.Add(new SimpleButtonItem(captured, () => { Controller.RemoveTypeTag(captured); RefreshLists(); }));
            }
            typeTagListView.Populate(typeItems);

            var zoneItems = new List<SimpleButtonItem>(room.zoneTags.Count);
            foreach (var tag in room.zoneTags)
            {
                var captured = tag;
                zoneItems.Add(new SimpleButtonItem(captured, () => { Controller.RemoveZoneTag(captured); RefreshLists(); }));
            }
            zoneTagListView.Populate(zoneItems);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using GameDefs;

namespace RoomGen.UI
{
    public class RoomDoorDefaultsPanel : TopBarWindowPanel
    {
        [SerializeField] private SimpleButtonListView singleDoorListView;
        [SerializeField] private SimpleButtonListView doubleDoorListView;

        private void OnEnable() => RefreshLists();

        private void RefreshLists()
        {
            if (!Controller) return;

            var room = Controller.CurrentRoom;
            string selectedSingle = room?.preferredSingleDoorDef;
            string selectedDouble = room?.preferredDoubleDoorDef;

            var singles = new List<SimpleButtonItem>
            {
                new SimpleButtonItem("Default", () => { Controller.ClearPreferredSingleDoorDef(); RefreshLists(); }, string.IsNullOrEmpty(selectedSingle))
            };
            var doubles = new List<SimpleButtonItem>
            {
                new SimpleButtonItem("Default", () => { Controller.ClearPreferredDoubleDoorDef(); RefreshLists(); }, string.IsNullOrEmpty(selectedDouble))
            };

            foreach (var door in DefDatabase.All<DoorDef>())
            {
                var def = door;
                if (def.IsDoubleDoor)
                    doubles.Add(new SimpleButtonItem(def.DisplayName, () => { Controller.SetPreferredDoubleDoorDef(def.DefName); RefreshLists(); }, def.DefName == selectedDouble));
                else
                    singles.Add(new SimpleButtonItem(def.DisplayName, () => { Controller.SetPreferredSingleDoorDef(def.DefName); RefreshLists(); }, def.DefName == selectedSingle));
            }

            singleDoorListView.Populate(singles);
            doubleDoorListView.Populate(doubles);
        }
    }
}

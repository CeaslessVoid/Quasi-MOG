using System.Linq;
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
            var doors = DefDatabase.All<DoorDef>();

            singleDoorListView.Populate(doors.Select(d =>
                new SimpleButtonItem(d.DisplayName, () => { Controller.SetPreferredSingleDoorDef(d.DefName); RefreshLists(); })).ToList());

            doubleDoorListView.Populate(doors.Select(d =>
                new SimpleButtonItem(d.DisplayName, () => { Controller.SetPreferredDoubleDoorDef(d.DefName); RefreshLists(); })).ToList());
        }
    }
}

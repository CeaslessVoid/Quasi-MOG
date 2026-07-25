using UnityEngine;

namespace RoomGen
{
    public static class RoomTemplateConverter
    {
        public static RoomTemplate ToRoomTemplate(RoomData data)
        {
            var t = ScriptableObject.CreateInstance<RoomTemplate>();
            t.name = data.templateId;
            t.data = data.Clone();
            t.ceilingLayer = new CeilingCell[data.CellCount];
            return t;
        }

        public static RoomData FromRoomTemplate(RoomTemplate t) => t.data.Clone();
    }
}
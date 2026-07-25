using System.Collections.Generic;
using UnityEngine;

namespace RoomGen
{
    public static class RoomTemplateConverter
    {
        public static RoomTemplate ToRoomTemplate(RoomBuilderState state)
        {
            int count = state.width * state.height;
            var t = ScriptableObject.CreateInstance<RoomTemplate>();
            t.name = state.templateId;
            t.width = state.width;
            t.height = state.height;
            t.typeTags = new List<string>(state.typeTags);
            t.zoneTags = new List<string>(state.zoneTags);
            t.floorLayer = (FloorType[])state.floorLayer.Clone();
            t.normalLayer = (NormalType[])state.normalLayer.Clone();
            t.connectorLayer = (ConnectorType[])state.connectorLayer.Clone();
            t.ceilingLayer = new CeilingCell[count];
            t.wallDefLayer = CloneOrNew(state.wallDefLayer, count);
            t.doorDefLayer = CloneOrNew(state.doorDefLayer, count);
            t.floorDefLayer = CloneOrNew(state.floorDefLayer, count);
            t.preferredDoorDef = state.preferredDoorDef;
            t.props = new List<PropPlacement>(state.props);
            t.desiredConnections = state.desiredConnections;
            t.extraConnectionChance = state.extraConnectionChance;
            t.chanceToConnectWhenBelowTarget = state.chanceToConnectWhenBelowTarget;
            t.selectionWeight = state.selectionWeight;
            t.reconnectionChance = state.reconnectionChance;
            t.reconnectionDoubleChance = state.reconnectionDoubleChance;
            return t;
        }

        public static RoomBuilderState FromRoomTemplate(RoomTemplate t)
        {
            int count = t.width * t.height;
            var s = new RoomBuilderState
            {
                templateId = t.name,
                width = t.width,
                height = t.height,
                typeTags = new List<string>(t.typeTags),
                zoneTags = new List<string>(t.zoneTags),
                floorLayer = (FloorType[])t.floorLayer.Clone(),
                normalLayer = (NormalType[])t.normalLayer.Clone(),
                connectorLayer = (ConnectorType[])t.connectorLayer.Clone(),
                wallDefLayer = CloneOrNew(t.wallDefLayer, count),
                doorDefLayer = CloneOrNew(t.doorDefLayer, count),
                floorDefLayer = CloneOrNew(t.floorDefLayer, count),
                preferredDoorDef = t.preferredDoorDef,
                props = new List<PropPlacement>(t.props),
                desiredConnections = t.desiredConnections,
                extraConnectionChance = t.extraConnectionChance,
                chanceToConnectWhenBelowTarget = t.chanceToConnectWhenBelowTarget,
                selectionWeight = t.selectionWeight,
                reconnectionChance = t.reconnectionChance,
                reconnectionDoubleChance = t.reconnectionDoubleChance
            };
            return s;
        }

        private static string[] CloneOrNew(string[] source, int count)
        {
            if (source != null && source.Length == count) return (string[])source.Clone();
            return new string[count];
        }
    }
}

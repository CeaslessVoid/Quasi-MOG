using System.Collections.Generic;
using UnityEngine;

namespace RoomGen
{
    public static class RoomTemplateConverter
    {
        public static RoomTemplate ToRoomTemplate(RoomBuilderState state)
        {
            var t = ScriptableObject.CreateInstance<RoomTemplate>();
            t.name = state.templateId;
            t.width = state.width;
            t.height = state.height;
            t.typeTags = new List<string>(state.typeTags);
            t.zoneTags = new List<string>(state.zoneTags);
            t.floorLayer = (FloorType[])state.floorLayer.Clone();
            t.normalLayer = (NormalType[])state.normalLayer.Clone();
            t.connectorLayer = (ConnectorType[])state.connectorLayer.Clone();
            t.ceilingLayer = new CeilingCell[state.width * state.height];
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
    }
}
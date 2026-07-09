using System;
using UnityEngine;
using BattleAngel.Grid;

namespace BattleAngel.Rooms
{
    public enum ConnectorDirection { North, East, South, West }

    [Serializable]
    public struct RoomConnector
    {
        public Vector2Int localPosition;
        public ConnectorDirection direction;
        public bool isCriticalPath;
    }

    [Serializable]
    public struct RoomTileEntry
    {
        public Vector2Int localPosition;
        public int floorTileId;

        public TileFlags flags;
    }

    [Serializable]
    public struct RoomPropEntry
    {
        public Vector2Int localPosition;
        public string propId;
        public int rotationSteps;
    }

    [CreateAssetMenu(fileName = "RoomDefinition", menuName = "BattleAngel/Room Definition")]
    public class RoomDefinition : ScriptableObject
    {
        public string roomId;
        public Vector2Int size;
        public string[] tags;

        public RoomTileEntry[] tiles;
        public RoomPropEntry[] props;
        public RoomConnector[] connectors;

        [Range(0f, 10f)] public float selectionWeight = 1f;
    }

}
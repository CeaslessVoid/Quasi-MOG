using System;
using System.Collections.Generic;
using GameDefs;
using UnityEngine;

namespace RoomGen
{
    public enum FloorType { Void = 0, Floor = 1, Liquid = 2 }
    public enum NormalType { Empty = 0, Wall = 1, Door = 2 }
    public enum ConnectorType { None = 0, Normal = 1, Restricted = 2, AlwaysDouble = 3 }
    public enum Edge { North = 0, East = 1, South = 2, West = 3 }
    public enum ConnectorState { Open = 0, Sealed = 1, Connected = 2 }
    public enum DoorSize { None = 0, Single1x1 = 1, Double2x1 = 2 }

    [Serializable]
    public struct PropPlacement
    {
        public string propId;
        public int cellX;
        public int cellY;
        public PropFacing facing;
    }

    [Serializable]
    public struct CeilingCell
    {
        public string propId;
    }

    public class ConnectionResult
    {
        public DoorSize size;
        public List<Vector2Int> doorCells = new List<Vector2Int>();
    }

    public struct LevelCell
    {
        public FloorType floor;
        public NormalType normal;
        public string wallDef;
        public string doorDef;
        public string floorDef;
        public int ownerRoomId;

        public static LevelCell Empty => new LevelCell
        {
            floor = FloorType.Void,
            normal = NormalType.Empty,
            wallDef = null,
            doorDef = null,
            floorDef = null,
            ownerRoomId = -1
        };
    }

    public class WorldConnectorRun
    {
        public int ownerRoomId;
        public bool isHorizontal;
        public ConnectorType type;
        public List<Vector2Int> cells = new List<Vector2Int>();
        public ConnectorState state = ConnectorState.Open;
        public int connectedToRoomId = -1;

        private HashSet<Vector2Int> _cellSet;
        public HashSet<Vector2Int> CellSet => _cellSet ??= new HashSet<Vector2Int>(cells);
    }

    public class PlacedProp
    {
        public string propId;
        public List<Vector2Int> worldCells = new List<Vector2Int>();
        public PropFacing worldFacing;
        public int ownerRoomId;
    }

    public class PlacedRoom
    {
        public int id;
        public RoomTemplate template;
        public Vector2Int origin;
        public int rotationDeg;
        public RectInt worldBounds;
        public List<WorldConnectorRun> connectorRuns = new List<WorldConnectorRun>();
        public List<PlacedProp> props = new List<PlacedProp>();

        public int corridorChainDepth = 0;

        public int ResolvedConnectionCount
        {
            get
            {
                int count = 0;
                foreach (var r in connectorRuns)
                    if (r.state == ConnectorState.Connected) count++;
                return count;
            }
        }
    }
}
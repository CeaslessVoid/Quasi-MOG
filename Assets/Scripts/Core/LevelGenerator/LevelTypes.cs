using System.Collections.Generic;
using UnityEngine;
using GameDefs;

namespace RoomGen
{
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
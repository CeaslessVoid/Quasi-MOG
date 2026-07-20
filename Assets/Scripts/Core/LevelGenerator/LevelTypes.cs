using System.Collections.Generic;
using UnityEngine;

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
        public int ownerRoomId;

        public static LevelCell Empty => new LevelCell { floor = FloorType.Void, normal = NormalType.Empty, ownerRoomId = -1 };
    }

    public class WorldConnectorRun
    {
        public int ownerRoomId;
        public bool isHorizontal;
        public ConnectorType type;
        public List<Vector2Int> cells = new List<Vector2Int>();
        public ConnectorState state = ConnectorState.Open;
        public int connectedToRoomId = -1;
    }

    public class PlacedRoom
    {
        public int id;
        public RoomTemplate template;
        public Vector2Int origin;
        public int rotationDeg;
        public RectInt worldBounds;
        public List<WorldConnectorRun> connectorRuns = new List<WorldConnectorRun>();

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
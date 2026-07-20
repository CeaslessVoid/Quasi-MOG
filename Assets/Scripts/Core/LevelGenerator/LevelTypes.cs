using System.Collections.Generic;
using UnityEngine;

namespace RoomGen
{
    /// <summary>
    /// What a connection-resolution call actually did - needed so a follow-up reconnection
    /// attempt knows which cells are already doors (and therefore off-limits to sit next to).
    /// </summary>
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

    /// <summary>
    /// A connector run belonging to a placed room, expressed in world-space cells.
    /// </summary>
    public class WorldConnectorRun
    {
        public int ownerRoomId;
        public bool isHorizontal;
        public ConnectorType type;
        public List<Vector2Int> cells = new List<Vector2Int>(); // world space, ordered
        public ConnectorState state = ConnectorState.Open;
        public int connectedToRoomId = -1; // set once state == Connected
    }

    /// <summary>
    /// One room instance stamped into the level grid.
    /// </summary>
    public class PlacedRoom
    {
        public int id;
        public RoomTemplate template;
        public Vector2Int origin;
        public int rotationDeg;
        public RectInt worldBounds;
        public List<WorldConnectorRun> connectorRuns = new List<WorldConnectorRun>();

        /// <summary>
        /// 0 for a non-corridor room. For a corridor, how many corridors deep this one is
        /// in an unbroken corridor-to-corridor chain (1 = attached straight off a normal
        /// room, 2 = attached to another corridor that was itself attached off a normal
        /// room, etc). Used to cap runaway corridor chains - see RoomGenerator.
        /// </summary>
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
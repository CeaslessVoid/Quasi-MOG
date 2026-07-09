using System.Collections.Generic;
using UnityEngine;
using BattleAngel.Grid;
using BattleAngel.Rendering;

namespace BattleAngel.Rooms
{
    /// <summary>
    /// Builds a mission map by placing prefab RoomDefinitions and snapping them together
    /// through matching connector sockets (a North-facing connector mates with a
    /// South-facing one, etc). Grows outward from a start room, breadth-first, until the
    /// target room count is hit, then forces the objective room onto the last open socket.
    ///
    /// This is deliberately not noise-based generation — Quasimorph-style room-prefab
    /// assembly gives far more control over pacing, chokepoints, and guaranteeing the
    /// objective is reachable, which matters a lot for a game built around cover combat.
    ///
    /// Writes results directly into GridManager (logical data) and InstancedPropRenderer
    /// (visual props). Call TilemapVisualSync.PaintAll() afterward to render the floor/walls.
    /// </summary>
    public class LevelAssembler : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private InstancedPropRenderer propRenderer;

        [SerializeField] private RoomDefinition[] roomPool;
        [SerializeField] private RoomDefinition startRoom;
        [SerializeField] private RoomDefinition objectiveRoom;

        [SerializeField] private int targetRoomCount = 12;
        [SerializeField] private int maxPlacementAttempts = 400;

        private readonly List<PlacedRoom> placedRooms = new();
        private readonly List<PlacedConnector> openConnectors = new();
        private System.Random rng;

        private struct PlacedRoom
        {
            public RoomDefinition def;
            public Vector2Int origin; // bottom-left cell in world grid space
            public int rotationSteps; // 0-3, 90 degree increments
        }

        private struct PlacedConnector
        {
            public Vector2Int worldPosition;
            public ConnectorDirection worldDirection;
        }

        public void Generate(int seed)
        {
            rng = new System.Random(seed);
            placedRooms.Clear();
            openConnectors.Clear();

            var startOrigin = new Vector2Int(gridManager.Width / 2, gridManager.Height / 2);
            PlaceRoom(startRoom, startOrigin, 0);

            int attempts = 0;
            while (placedRooms.Count < targetRoomCount && openConnectors.Count > 0
                   && attempts < maxPlacementAttempts)
            {
                attempts++;

                int connIdx = rng.Next(openConnectors.Count);
                var socket = openConnectors[connIdx];
                openConnectors.RemoveAt(connIdx);

                bool wantObjective = placedRooms.Count == targetRoomCount - 1;
                RoomDefinition candidate = wantObjective
                    ? objectiveRoom
                    : roomPool[rng.Next(roomPool.Length)];

                TryAttachRoom(candidate, socket);
            }

            PaintAllRooms();
        }

        private bool TryAttachRoom(RoomDefinition def, PlacedConnector socket)
        {
            var requiredDir = Opposite(socket.worldDirection);

            // try every rotation of the candidate room and every connector on it,
            // looking for one that faces the socket correctly and doesn't overlap
            // anything already placed.
            for (int rot = 0; rot < 4; rot++)
            {
                foreach (var conn in def.connectors)
                {
                    if (RotateDirection(conn.direction, rot) != requiredDir) continue;

                    var rotatedLocal = RotatePoint(conn.localPosition, def.size, rot);
                    var origin = socket.worldPosition - rotatedLocal + DirectionOffset(socket.worldDirection);

                    if (CanPlace(def, origin, rot))
                    {
                        PlaceRoom(def, origin, rot);
                        return true;
                    }
                }
            }
            return false;
        }

        private bool CanPlace(RoomDefinition def, Vector2Int origin, int rot)
        {
            foreach (var tile in def.tiles)
            {
                var p = origin + RotatePoint(tile.localPosition, def.size, rot);
                if (!gridManager.InBounds(p.x, p.y)) return false;
                if (gridManager.CellAt(p.x, p.y).floorTileId != 0) return false; // occupied
            }
            return true;
        }

        private void PlaceRoom(RoomDefinition def, Vector2Int origin, int rot)
        {
            placedRooms.Add(new PlacedRoom { def = def, origin = origin, rotationSteps = rot });

            foreach (var conn in def.connectors)
            {
                openConnectors.Add(new PlacedConnector
                {
                    worldPosition = origin + RotatePoint(conn.localPosition, def.size, rot),
                    worldDirection = RotateDirection(conn.direction, rot)
                });
            }
        }

        private void PaintAllRooms()
        {
            foreach (var room in placedRooms)
            {
                foreach (var tile in room.def.tiles)
                {
                    var p = room.origin + RotatePoint(tile.localPosition, room.def.size, room.rotationSteps);
                    ref var cell = ref gridManager.CellAt(p.x, p.y);
                    cell.floorTileId = tile.floorTileId;
                    cell.wallTileId = tile.wallTileId;
                    cell.flags = tile.flags;
                }

                foreach (var prop in room.def.props)
                {
                    var p = room.origin + RotatePoint(prop.localPosition, room.def.size, room.rotationSteps);
                    var worldPos = gridManager.CellToWorld(p);
                    int spriteIndex = PropCatalog.GetSpriteIndex(prop.propId);
                    propRenderer.AddInstance(worldPos, prop.rotationSteps * 90f, spriteIndex);
                }
            }
        }

        // --- rotation helpers -------------------------------------------------

        private static Vector2Int RotatePoint(Vector2Int p, Vector2Int size, int rotSteps)
        {
            Vector2Int result = p;
            Vector2Int s = size;
            for (int i = 0; i < rotSteps; i++)
            {
                result = new Vector2Int(s.y - 1 - result.y, result.x);
                s = new Vector2Int(s.y, s.x);
            }
            return result;
        }

        private static ConnectorDirection RotateDirection(ConnectorDirection dir, int rotSteps)
        {
            // cycle N -> E -> S -> W per 90-degree step
            int d = (int)dir;
            for (int i = 0; i < rotSteps; i++) d = (d + 1) % 4;
            return (ConnectorDirection)d;
        }

        private static ConnectorDirection Opposite(ConnectorDirection dir)
        {
            // N<->S and E<->W are two rotation steps apart in the N,E,S,W ordering.
            return RotateDirection(dir, 2);
        }

        private static Vector2Int DirectionOffset(ConnectorDirection dir) => dir switch
        {
            ConnectorDirection.North => new Vector2Int(0, 1),
            ConnectorDirection.South => new Vector2Int(0, -1),
            ConnectorDirection.East => new Vector2Int(1, 0),
            ConnectorDirection.West => new Vector2Int(-1, 0),
            _ => Vector2Int.zero
        };
    }
}

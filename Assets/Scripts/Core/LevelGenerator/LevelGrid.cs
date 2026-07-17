using System.Collections.Generic;
using UnityEngine;

namespace RoomGen
{
    /// <summary>
    /// Sparse master grid for a generated level. Pure C# / no MonoBehaviour dependency -
    /// safe to build and inspect in edit-mode tests.
    /// </summary>
    public class LevelGrid
    {
        private readonly Dictionary<Vector2Int, LevelCell> _cells = new Dictionary<Vector2Int, LevelCell>();
        public List<PlacedRoom> PlacedRooms { get; } = new List<PlacedRoom>();
        private int _nextRoomId = 0;

        public LevelCell GetCell(Vector2Int pos) => _cells.TryGetValue(pos, out var c) ? c : LevelCell.Empty;

        public bool HasCell(Vector2Int pos) => _cells.ContainsKey(pos);

        /// <summary>
        /// Checks whether a room template can be stamped at the given origin/rotation.
        /// A cell may only be shared with something already in the grid if BOTH sides are
        /// Wall - that's what a shared seam between two rooms actually is. A room's
        /// connecting edge is almost always longer than the specific connector run being
        /// used to attach it (corner wall cells beyond the run), and those still need to
        /// land on the other room's wall without being treated as a collision - hence
        /// checking "is this cell a wall on both sides" rather than "is this cell part of
        /// the exact overlap we pre-computed for the door."
        /// </summary>
        public bool CanPlace(RoomTemplate t, Vector2Int origin, int rotationDeg)
        {
            for (int y = 0; y < t.height; y++)
            {
                for (int x = 0; x < t.width; x++)
                {
                    var world = RoomTemplateUtility.LocalToWorld(x, y, t.width, t.height, rotationDeg, origin);
                    if (_cells.TryGetValue(world, out var existing))
                    {
                        bool bothWalls = existing.normal == NormalType.Wall && t.GetNormal(x, y) == NormalType.Wall;
                        if (!bothWalls) return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Writes a template's layers into the grid at origin/rotation and registers it as
        /// a PlacedRoom with world-space connector runs. Assumes CanPlace was already checked.
        /// </summary>
        public PlacedRoom Stamp(RoomTemplate t, Vector2Int origin, int rotationDeg)
        {
            var room = new PlacedRoom
            {
                id = _nextRoomId++,
                template = t,
                origin = origin,
                rotationDeg = rotationDeg
            };

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;

            for (int y = 0; y < t.height; y++)
            {
                for (int x = 0; x < t.width; x++)
                {
                    var world = RoomTemplateUtility.LocalToWorld(x, y, t.width, t.height, rotationDeg, origin);
                    var cell = new LevelCell
                    {
                        floor = t.GetFloor(x, y),
                        normal = t.GetNormal(x, y),
                        ownerRoomId = room.id
                    };

                    if (_cells.TryGetValue(world, out var existingCell))
                    {
                        // Merging onto shared boundary cells from the room we're attaching to -
                        // don't stomp what's already there, just fill gaps.
                        if (cell.floor == FloorType.Void) cell.floor = existingCell.floor;
                        if (cell.normal == NormalType.Empty) cell.normal = existingCell.normal;
                        cell.ownerRoomId = existingCell.ownerRoomId; // first owner keeps the credit
                    }

                    _cells[world] = cell;

                    minX = Mathf.Min(minX, world.x);
                    minY = Mathf.Min(minY, world.y);
                    maxX = Mathf.Max(maxX, world.x);
                    maxY = Mathf.Max(maxY, world.y);
                }
            }

            room.worldBounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);

            foreach (var localRun in RoomTemplateUtility.FindConnectorRuns(t))
            {
                var worldRun = new WorldConnectorRun
                {
                    ownerRoomId = room.id,
                    isHorizontal = localRun.isHorizontal,
                    type = localRun.type
                };
                foreach (var c in localRun.cells)
                    worldRun.cells.Add(RoomTemplateUtility.LocalToWorld(c.x, c.y, t.width, t.height, rotationDeg, origin));
                room.connectorRuns.Add(worldRun);
            }

            PlacedRooms.Add(room);
            return room;
        }

        public void SetNormal(Vector2Int pos, NormalType type)
        {
            var cell = GetCell(pos);
            cell.normal = type;
            _cells[pos] = cell;
        }

        /// <summary>
        /// Resolves an overlap between two connector runs into a door. The two outermost
        /// cells of the overlap are never eligible (so a door never touches where the run
        /// ends against plain wall).
        ///
        /// Sizing rules:
        /// - forceDoubleOverride (used for corridor-to-corridor connections) always wins:
        ///   ignores both sides' declared types entirely and always tries for a 2x1.
        /// - Else, if either side is AlwaysDouble: always try for a 2x1, even if the other
        ///   side is Restricted (that's the whole point of AlwaysDouble - it overrides the
        ///   partner's restriction).
        /// - Else if either side is Restricted: always 1x1, no exceptions.
        /// - Else (both Normal): normally always try for a 2x1 when there's room.
        ///   overrideDoubleChance lets a caller replace that "always" with a coin flip
        ///   instead - used for the reconnection pass, where extra doors are meant to be
        ///   more varied than the deterministic primary connection.
        /// Falls back to 1x1 whenever there isn't physically room for a 2x1, regardless of
        /// which rule above applied.
        /// </summary>
        public DoorSize ResolveConnection(List<Vector2Int> overlapCellsInOrder, ConnectorType typeA, ConnectorType typeB, System.Random rng, float? overrideDoubleChance = null, bool forceDoubleOverride = false)
        {
            foreach (var c in overlapCellsInOrder) SetNormal(c, NormalType.Wall);

            bool forceDouble = forceDoubleOverride || typeA == ConnectorType.AlwaysDouble || typeB == ConnectorType.AlwaysDouble;
            bool forceSingle = !forceDouble && (typeA == ConnectorType.Restricted || typeB == ConnectorType.Restricted);

            if (overlapCellsInOrder.Count <= 2)
            {
                var mid = overlapCellsInOrder[overlapCellsInOrder.Count / 2];
                SetNormal(mid, NormalType.Door);
                return DoorSize.Single1x1;
            }

            var eligible = overlapCellsInOrder.GetRange(1, overlapCellsInOrder.Count - 2);
            bool canDouble = eligible.Count >= 2;

            bool wantsDouble;
            if (forceSingle) wantsDouble = false;
            else if (forceDouble) wantsDouble = true;
            else if (overrideDoubleChance.HasValue) wantsDouble = rng.NextDouble() < overrideDoubleChance.Value;
            else wantsDouble = true; // primary Normal/Normal connection: always prefer double when possible

            if (wantsDouble && canDouble)
            {
                int startIndex = rng.Next(0, eligible.Count - 1);
                SetNormal(eligible[startIndex], NormalType.Door);
                SetNormal(eligible[startIndex + 1], NormalType.Door);
                return DoorSize.Double2x1;
            }

            int index = rng.Next(0, eligible.Count);
            SetNormal(eligible[index], NormalType.Door);
            return DoorSize.Single1x1;
        }

        /// <summary>True if any of the 4 orthogonal neighbors of cell is a Door.</summary>
        public bool IsAdjacentToDoor(Vector2Int cell)
        {
            return GetCell(cell + Vector2Int.up).normal == NormalType.Door
                || GetCell(cell + Vector2Int.down).normal == NormalType.Door
                || GetCell(cell + Vector2Int.left).normal == NormalType.Door
                || GetCell(cell + Vector2Int.right).normal == NormalType.Door;
        }
    }
}

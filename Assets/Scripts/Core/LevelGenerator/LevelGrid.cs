using System.Collections.Generic;
using UnityEngine;
using GameDefs;

namespace RoomGen
{
    public class LevelGrid
    {
        private readonly Dictionary<Vector2Int, LevelCell> _cells = new Dictionary<Vector2Int, LevelCell>();
        public List<PlacedRoom> PlacedRooms { get; } = new List<PlacedRoom>();
        private int _nextRoomId = 0;

        public IReadOnlyDictionary<Vector2Int, LevelCell> Cells => _cells;

        public LevelCell GetCell(Vector2Int pos) => _cells.TryGetValue(pos, out var c) ? c : LevelCell.Empty;

        public bool HasCell(Vector2Int pos) => _cells.ContainsKey(pos);

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
                        wallDef = t.GetWallDef(x, y),
                        doorDef = t.GetDoorDef(x, y),
                        floorDef = t.GetFloorDef(x, y),
                        ownerRoomId = room.id
                    };

                    if (_cells.TryGetValue(world, out var existingCell))
                    {
                        if (cell.floor == FloorType.Void)
                        {
                            cell.floor = existingCell.floor;
                            cell.floorDef = existingCell.floorDef;
                        }
                        if (cell.normal == NormalType.Empty)
                        {
                            cell.normal = existingCell.normal;
                            cell.wallDef = existingCell.wallDef;
                            cell.doorDef = existingCell.doorDef;
                        }
                        cell.ownerRoomId = existingCell.ownerRoomId;
                    }

                    _cells[world] = cell;

                    minX = Mathf.Min(minX, world.x);
                    minY = Mathf.Min(minY, world.y);
                    maxX = Mathf.Max(maxX, world.x);
                    maxY = Mathf.Max(maxY, world.y);
                }
            }

            room.worldBounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);

            foreach (var localRun in t.GetConnectorRuns())
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

            foreach (var p in t.props)
            {
                var def = DefDatabase.Get<PropDef>(p.propId);
                int pw = def != null ? def.Width : 1;
                int ph = def != null ? def.Height : 1;

                var localCells = PropPlacementUtility.GetFootprintCells(new Vector2Int(p.cellX, p.cellY), pw, ph, p.facing);
                var worldCells = new List<Vector2Int>(localCells.Count);
                foreach (var lc in localCells)
                    worldCells.Add(RoomTemplateUtility.LocalToWorld(lc.x, lc.y, t.width, t.height, rotationDeg, origin));

                room.props.Add(new PlacedProp
                {
                    propId = p.propId,
                    worldCells = worldCells,
                    worldFacing = PropPlacementUtility.RotateFacing(p.facing, rotationDeg),
                    ownerRoomId = room.id
                });
            }

            PlacedRooms.Add(room);
            return room;
        }

        public void SetNormal(Vector2Int pos, NormalType type, string defName = null)
        {
            var cell = GetCell(pos);
            cell.normal = type;
            if (defName != null)
            {
                if (type == NormalType.Wall) cell.wallDef = defName;
                else if (type == NormalType.Door) cell.doorDef = defName;
            }
            _cells[pos] = cell;
        }

        public DoorSize ResolveConnection(List<Vector2Int> overlapCellsInOrder, ConnectorType typeA, ConnectorType typeB, System.Random rng, string singleDoorDef, string doubleDoorDef, float? overrideDoubleChance = null, bool forceDoubleOverride = false)
        {
            foreach (var c in overlapCellsInOrder) SetNormal(c, NormalType.Wall);

            bool forceDouble = forceDoubleOverride || typeA == ConnectorType.AlwaysDouble || typeB == ConnectorType.AlwaysDouble;
            bool forceSingle = !forceDouble && (typeA == ConnectorType.Restricted || typeB == ConnectorType.Restricted);

            if (overlapCellsInOrder.Count == 1)
            {
                SetNormal(overlapCellsInOrder[0], NormalType.Door, singleDoorDef);
                return DoorSize.Single1x1;
            }

            if (overlapCellsInOrder.Count == 2)
            {
                bool wantsDoubleHere = forceDouble || (!forceSingle && (!overrideDoubleChance.HasValue || rng.NextDouble() < overrideDoubleChance.Value));
                if (wantsDoubleHere)
                {
                    SetNormal(overlapCellsInOrder[0], NormalType.Door, doubleDoorDef);
                    SetNormal(overlapCellsInOrder[1], NormalType.Door, doubleDoorDef);
                    return DoorSize.Double2x1;
                }
                SetNormal(overlapCellsInOrder[rng.Next(0, 2)], NormalType.Door, singleDoorDef);
                return DoorSize.Single1x1;
            }

            var eligible = overlapCellsInOrder.GetRange(1, overlapCellsInOrder.Count - 2);
            bool canDouble = eligible.Count >= 2;

            bool wantsDouble;
            if (forceSingle) wantsDouble = false;
            else if (forceDouble) wantsDouble = true;
            else if (overrideDoubleChance.HasValue) wantsDouble = rng.NextDouble() < overrideDoubleChance.Value;
            else wantsDouble = true;

            if (wantsDouble && canDouble)
            {
                int startIndex = rng.Next(0, eligible.Count - 1);
                SetNormal(eligible[startIndex], NormalType.Door, doubleDoorDef);
                SetNormal(eligible[startIndex + 1], NormalType.Door, doubleDoorDef);
                return DoorSize.Double2x1;
            }

            int index = rng.Next(0, eligible.Count);
            SetNormal(eligible[index], NormalType.Door, singleDoorDef);
            return DoorSize.Single1x1;
        }

        public bool IsAdjacentToDoor(Vector2Int cell)
        {
            return GetCell(cell + Vector2Int.up).normal == NormalType.Door
                || GetCell(cell + Vector2Int.down).normal == NormalType.Door
                || GetCell(cell + Vector2Int.left).normal == NormalType.Door
                || GetCell(cell + Vector2Int.right).normal == NormalType.Door;
        }

        public bool IsWallBlocking(Vector2Int cell) => GetCell(cell).normal == NormalType.Wall;

        public bool IsNorthOrientedDoor(Vector2Int cell)
        {
            bool northSouthOpen = !IsWallBlocking(cell + Vector2Int.up) && !IsWallBlocking(cell + Vector2Int.down);
            bool eastWestOpen = !IsWallBlocking(cell + Vector2Int.right) && !IsWallBlocking(cell + Vector2Int.left);

            if (northSouthOpen && !eastWestOpen) return true;
            if (eastWestOpen && !northSouthOpen) return false;
            return true;
        }

        public bool IsMatchingDoor(Vector2Int cell, string doorDefName)
        {
            var c = GetCell(cell);
            return c.normal == NormalType.Door && c.doorDef == doorDefName;
        }

        public bool TryFindDoorPartner(Vector2Int cell, string doorDefName, out Vector2Int partner)
        {
            bool isNorthOrientation = IsNorthOrientedDoor(cell);

            if (isNorthOrientation)
            {
                var east = cell + Vector2Int.right;
                var west = cell + Vector2Int.left;
                if (IsMatchingDoor(east, doorDefName)) { partner = east; return true; }
                if (IsMatchingDoor(west, doorDefName)) { partner = west; return true; }
            }
            else
            {
                var north = cell + Vector2Int.up;
                var south = cell + Vector2Int.down;
                if (IsMatchingDoor(north, doorDefName)) { partner = north; return true; }
                if (IsMatchingDoor(south, doorDefName)) { partner = south; return true; }
            }

            partner = default;
            return false;
        }
    }
}
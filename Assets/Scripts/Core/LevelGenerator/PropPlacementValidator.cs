using System.Collections.Generic;
using UnityEngine;
using GameDefs;

namespace RoomGen
{
    public static class PropPlacementValidator
    {
        public static List<(Vector2Int cell, bool valid)> Evaluate(RoomData state, PropPlacement placement, PropDef def)
        {
            var result = new List<(Vector2Int cell, bool valid)>();
            if (def == null) return result;

            foreach (var cell in PropPlacementUtility.GetFootprintCells(placement, def))
                result.Add((cell, IsCellValid(state, placement, def, cell)));

            return result;
        }

        public static bool IsValid(RoomData state, PropPlacement placement, PropDef def, out string reason)
        {
            reason = null;
            if (def == null) { reason = "Unknown prop def."; return false; }

            foreach (var (cell, valid) in Evaluate(state, placement, def))
            {
                if (!valid) { reason = $"Cannot place '{def.DefName}' at ({cell.x}, {cell.y})."; return false; }
            }
            return true;
        }

        private static bool IsCellValid(RoomData state, PropPlacement placement, PropDef def, Vector2Int cell)
        {
            if (!state.InBounds(cell.x, cell.y)) return false;

            switch (def.Category)
            {
                case PropCategory.Wall:
                    if (state.GetNormal(cell.x, cell.y) != NormalType.Wall) return false;
                    break;

                case PropCategory.Decorative:
                    bool onProp = PropPlacementUtility.FindPlacementAtCell(state, cell).HasValue;
                    bool onOpenWall = state.GetNormal(cell.x, cell.y) == NormalType.Wall && !IsVisionBlocking(state, cell);
                    if (!onProp && !onOpenWall) return false;
                    break;

                default:
                    if (state.GetFloor(cell.x, cell.y) == FloorType.Void) return false;
                    if (state.GetNormal(cell.x, cell.y) != NormalType.Empty) return false;
                    break;
            }

            var existing = PropPlacementUtility.FindPlacementAtCell(state, cell);
            if (existing.HasValue && (existing.Value.cellX != placement.cellX || existing.Value.cellY != placement.cellY))
            {
                var existingDef = DefDatabase.Get<PropDef>(existing.Value.propId);
                bool stackAllowed = def.Category == PropCategory.Decorative || (existingDef != null && existingDef.Category == PropCategory.Decorative);
                if (!stackAllowed) return false;
            }

            return true;
        }

        private static bool IsVisionBlocking(RoomData state, Vector2Int cell)
        {
            var wallDef = DefDatabase.Get<WallDef>(state.GetWallDef(cell.x, cell.y));
            return wallDef == null || wallDef.BlocksVision;
        }
    }
}
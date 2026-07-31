using System.Collections.Generic;
using UnityEngine;
using GameDefs;

namespace RoomGen
{
    public static class PropPlacementUtility
    {
        public static Vector2Int RotateOffset(Vector2Int offset, PropFacing facing)
        {
            int steps = (int)facing & 3;
            for (int i = 0; i < steps; i++)
                offset = new Vector2Int(offset.y, -offset.x);
            return offset;
        }

        public static PropFacing RotateFacing(PropFacing facing, int roomRotationDeg)
        {
            int roomCwSteps = (4 - (roomRotationDeg / 90) % 4) % 4;
            return (PropFacing)(((int)facing + roomCwSteps) % 4);
        }

        public static List<Vector2Int> GetFootprintCells(Vector2Int origin, int width, int height, PropFacing facing)
        {
            var cells = new List<Vector2Int>(width * height);
            for (int j = 0; j < height; j++)
                for (int i = 0; i < width; i++)
                    cells.Add(origin + RotateOffset(new Vector2Int(i, -j), facing));
            return cells;
        }

        public static List<Vector2Int> GetFootprintCells(PropPlacement placement, PropDef def) =>
            GetFootprintCells(new Vector2Int(placement.cellX, placement.cellY), def.Width, def.Height, placement.facing);

        public static Vector2Int GetWallMountOffset(PropFacing facing) => RotateOffset(new Vector2Int(0, -1), facing);

        public static bool GetFootprintBounds(List<Vector2Int> cells, out Vector2Int min, out Vector2Int max)
        {
            min = default;
            max = default;
            if (cells == null || cells.Count == 0) return false;

            min = max = cells[0];
            for (int i = 1; i < cells.Count; i++)
            {
                min = Vector2Int.Min(min, cells[i]);
                max = Vector2Int.Max(max, cells[i]);
            }
            return true;
        }

        public static bool GetRenderBounds(List<Vector2Int> footprintCells, PropCategory category, PropFacing facing, out Vector2Int min, out Vector2Int max)
        {
            if (!GetFootprintBounds(footprintCells, out min, out max)) return false;

            if (category == PropCategory.Wall)
            {
                var offset = GetWallMountOffset(facing);
                min += offset;
                max += offset;
            }
            return true;
        }

        public static PropPlacement? FindPlacementAtCell(RoomData state, Vector2Int cell)
        {
            foreach (var p in state.props)
            {
                var def = DefDatabase.Get<PropDef>(p.propId);
                int width = def != null ? def.Width : 1;
                int height = def != null ? def.Height : 1;

                foreach (var c in GetFootprintCells(new Vector2Int(p.cellX, p.cellY), width, height, p.facing))
                {
                    if (c == cell) return p;
                }
            }
            return null;
        }
    }
}
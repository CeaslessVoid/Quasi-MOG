using BattleAngel.Grid;

namespace BattleAngel.Rooms
{
    public static class WallAutotiler
    {
        private const int North = 1;
        private const int East = 2;
        private const int South = 4;
        private const int West = 8;

        public static void Autotile(GridManager grid)
        {
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    ref var cell = ref grid.CellAt(x, y);

                    if ((cell.flags & TileFlags.Wall) == 0)
                    {
                        cell.wallTileId = 0;
                        continue;
                    }

                    int mask = 0;
                    if (IsWall(grid, x, y + 1)) mask |= North;
                    if (IsWall(grid, x + 1, y)) mask |= East;
                    if (IsWall(grid, x, y - 1)) mask |= South;
                    if (IsWall(grid, x - 1, y)) mask |= West;

                    cell.wallTileId = mask + 1;
                }
            }
        }

        private static bool IsWall(GridManager grid, int x, int y)
        {
            if (!grid.InBounds(x, y)) return false;
            return (grid.CellAt(x, y).flags & TileFlags.Wall) != 0;
        }
    }
}
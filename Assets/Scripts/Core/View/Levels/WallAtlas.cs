using UnityEngine;

namespace RoomGen
{
    public static class WallAtlas
    {
        public const int North = 1, East = 2, South = 4, West = 8;

        private static readonly int[] BitmaskToIndex =
        {
            12, 13, 14, 15, 8, 9, 10, 11, 4, 5, 6, 7, 0, 1, 2, 3
        };

        public static int ComputeBitmask(bool north, bool east, bool south, bool west)
        {
            int mask = 0;
            if (north) mask |= North;
            if (east) mask |= East;
            if (south) mask |= South;
            if (west) mask |= West;
            return mask;
        }

        public static T Lookup<T>(T[] array, int bitmask) where T : class
        {
            if (array == null || array.Length == 0) return null;
            return array[BitmaskToIndex[Mathf.Clamp(bitmask, 0, 15)]];
        }
    }
}
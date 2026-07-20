using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoomGen
{
    public class LocalConnectorRun
    {
        public bool isHorizontal;
        public ConnectorType type;
        public List<Vector2Int> cells = new List<Vector2Int>();
    }

    public static class RoomTemplateUtility
    {
        public static void RotateCell(int x, int y, int width, int height, int rotationDeg, out int rx, out int ry)
        {
            int norm = ((rotationDeg % 360) + 360) % 360;
            switch (norm)
            {
                case 0:
                    rx = x; ry = y;
                    break;
                case 90:
                    rx = height - 1 - y;
                    ry = x;
                    break;
                case 180:
                    rx = width - 1 - x;
                    ry = height - 1 - y;
                    break;
                case 270:
                    rx = y;
                    ry = width - 1 - x;
                    break;
                default:
                    throw new System.ArgumentException("Rotation must be 0/90/180/270, got " + rotationDeg);
            }
        }

        public static void GetRotatedSize(int width, int height, int rotationDeg, out int rw, out int rh)
        {
            int norm = ((rotationDeg % 360) + 360) % 360;
            if (norm % 180 == 0) { rw = width; rh = height; }
            else { rw = height; rh = width; }
        }

        public static bool IsConnectorEligible(int x, int y, int width, int height, NormalType[] normalLayer)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return false;
            if (normalLayer[y * width + x] != NormalType.Wall) return false;

            return IsExposedNeighbor(x + 1, y, width, height, normalLayer)
                || IsExposedNeighbor(x - 1, y, width, height, normalLayer)
                || IsExposedNeighbor(x, y + 1, width, height, normalLayer)
                || IsExposedNeighbor(x, y - 1, width, height, normalLayer);
        }

        private static bool IsExposedNeighbor(int x, int y, int width, int height, NormalType[] normalLayer)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return true;
            return normalLayer[y * width + x] != NormalType.Wall;
        }

        public static Vector2Int LocalToWorld(int x, int y, int width, int height, int rotationDeg, Vector2Int origin)
        {
            RotateCell(x, y, width, height, rotationDeg, out int rx, out int ry);
            return origin + new Vector2Int(rx, ry);
        }

        public static Edge RotateEdge(Edge edge, int rotationDeg)
        {
            int steps = ((rotationDeg / 90) % 4 + 4) % 4;
            int e = ((int)edge - steps) % 4;
            if (e < 0) e += 4;
            return (Edge)e;
        }

        public static List<LocalConnectorRun> FindConnectorRuns(RoomTemplate t)
        {
            var horizontalRuns = new List<LocalConnectorRun>();
            for (int y = 0; y < t.height; y++)
                ScanLine(t, horizontalRuns, isHorizontal: true, fixedIndex: y, length: t.width);

            var verticalRuns = new List<LocalConnectorRun>();
            for (int x = 0; x < t.width; x++)
                ScanLine(t, verticalRuns, isHorizontal: false, fixedIndex: x, length: t.height);

            var result = new List<LocalConnectorRun>();
            var claimed = new HashSet<Vector2Int>();

            foreach (var run in horizontalRuns.Concat(verticalRuns))
            {
                if (run.cells.Count < 2) continue;
                result.Add(run);
                foreach (var c in run.cells) claimed.Add(c);
            }

            foreach (var run in horizontalRuns.Concat(verticalRuns))
            {
                if (run.cells.Count != 1) continue;
                var cell = run.cells[0];
                if (!claimed.Add(cell)) continue;
                result.Add(run);
            }

            return result;
        }

        private static void ScanLine(RoomTemplate t, List<LocalConnectorRun> runs, bool isHorizontal, int fixedIndex, int length)
        {
            LocalConnectorRun current = null;
            for (int i = 0; i < length; i++)
            {
                int x = isHorizontal ? i : fixedIndex;
                int y = isHorizontal ? fixedIndex : i;

                bool eligible = t.IsConnectorEligible(x, y);
                var connType = eligible ? t.GetConnector(x, y) : ConnectorType.None;

                if (connType != ConnectorType.None)
                {
                    if (current == null || !SameRunType(current.type, connType))
                    {
                        current = new LocalConnectorRun { type = connType, isHorizontal = isHorizontal };
                        runs.Add(current);
                    }
                    current.cells.Add(new Vector2Int(x, y));
                }
                else
                {
                    current = null;
                }
            }
        }

        static bool SameRunType(ConnectorType a, ConnectorType b)
        {
            if (a == b) return true;

            if ((a == ConnectorType.Normal && b == ConnectorType.AlwaysDouble) ||
                (a == ConnectorType.AlwaysDouble && b == ConnectorType.Normal))
                return true;

            return false;
        }

    }
}

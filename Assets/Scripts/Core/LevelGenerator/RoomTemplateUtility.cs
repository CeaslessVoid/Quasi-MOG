using System.Collections.Generic;
using UnityEngine;

namespace RoomGen
{
    /// <summary>
    /// A contiguous run of same-type connector cells along one edge of a room, in the
    /// room's own local (unrotated) space, ordered along the edge.
    /// </summary>
    public class LocalConnectorRun
    {
        public bool isHorizontal;
        public ConnectorType type;
        public List<Vector2Int> cells = new List<Vector2Int>();
    }

    /// <summary>
    /// Rotation math (0/90/180/270, integer grid) and connector-run scanning. Rooms only
    /// ever rotate in 90-degree steps, so all of this stays exact-integer.
    /// </summary>
    public static class RoomTemplateUtility
    {
        /// <summary>
        /// Rotates a local cell (x,y) within a width x height template by rotationDeg
        /// (0/90/180/270), returning the cell's position in the rotated bounding box.
        /// This is a fixed convention - see RotateEdge for the matching edge-label mapping.
        /// </summary>
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

        /// <summary>
        /// A cell is connector-eligible if it's a Wall AND at least one of its 4 orthogonal
        /// neighbors is NOT a Wall (including being off the edge of the grid entirely).
        /// This generalizes the old "boundary only" rule: a plain rectangular room's outer
        /// ring always qualifies (every boundary cell has an off-grid neighbor), but so does
        /// an interior wall facing a notch cut out of the room (e.g. an L-shaped corridor's
        /// inner corner) - connectors are no longer restricted to the room's bounding-box
        /// edge, just to walls that actually face open/exterior space.
        /// </summary>
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
            if (x < 0 || y < 0 || x >= width || y >= height) return true; // off-grid counts as exposed
            return normalLayer[y * width + x] != NormalType.Wall;
        }

        public static Vector2Int LocalToWorld(int x, int y, int width, int height, int rotationDeg, Vector2Int origin)
        {
            RotateCell(x, y, width, height, rotationDeg, out int rx, out int ry);
            return origin + new Vector2Int(rx, ry);
        }

        /// <summary>
        /// Maps a local edge label to the edge it becomes after rotating the room by
        /// rotationDeg. Derived directly from RotateCell - see README for the derivation.
        /// </summary>
        public static Edge RotateEdge(Edge edge, int rotationDeg)
        {
            int steps = ((rotationDeg / 90) % 4 + 4) % 4;
            int e = ((int)edge - steps) % 4;
            if (e < 0) e += 4;
            return (Edge)e;
        }

        /// <summary>
        /// Scans every row and every column of the template for contiguous runs of
        /// same-type, connector-eligible cells. No longer restricted to the 4 named
        /// boundary edges - a connector run can sit on any exposed wall, including an
        /// interior-facing wall around a notch cut out of the room (an L-bend). A run is
        /// always a single straight line (all in one row, or all in one column) since door
        /// placement fundamentally needs a straight line of cells.
        ///
        /// A lone connector cell that has eligible connector-flagged neighbors in both a
        /// row and a column direction can be picked up by both scans, producing two
        /// overlapping length-1 runs - same known caveat as the old "don't flag corners"
        /// advice, just generalized. Avoid isolated single-cell connector flags if you want
        /// unambiguous runs; a straight line of 2+ cells is always unambiguous.
        /// </summary>
        public static List<LocalConnectorRun> FindConnectorRuns(RoomTemplate t)
        {
            var runs = new List<LocalConnectorRun>();

            for (int y = 0; y < t.height; y++)
                ScanLine(t, runs, isHorizontal: true, fixedIndex: y, length: t.width);

            for (int x = 0; x < t.width; x++)
                ScanLine(t, runs, isHorizontal: false, fixedIndex: x, length: t.height);

            return runs;
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
                    if (current == null || current.type != connType)
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
    }
}

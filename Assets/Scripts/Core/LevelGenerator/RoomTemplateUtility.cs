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
        public Edge edge;
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
        /// Scans all four boundary edges of a template and returns every contiguous run of
        /// same-type connector cells, in local (unrotated) space. Corner cells that are
        /// flagged as connectors may appear in two runs (one per adjoining edge) - avoid
        /// flagging exact corners as connectors when authoring rooms.
        /// </summary>
        public static List<LocalConnectorRun> FindConnectorRuns(RoomTemplate t)
        {
            var runs = new List<LocalConnectorRun>();
            ScanEdge(t, Edge.South, i => new Vector2Int(i, 0), t.width, runs);
            ScanEdge(t, Edge.North, i => new Vector2Int(i, t.height - 1), t.width, runs);
            ScanEdge(t, Edge.West, i => new Vector2Int(0, i), t.height, runs);
            ScanEdge(t, Edge.East, i => new Vector2Int(t.width - 1, i), t.height, runs);
            return runs;
        }

        private static void ScanEdge(RoomTemplate t, Edge edge, System.Func<int, Vector2Int> indexer, int length, List<LocalConnectorRun> runs)
        {
            LocalConnectorRun current = null;
            for (int i = 0; i < length; i++)
            {
                var cell = indexer(i);
                var connType = t.GetConnector(cell.x, cell.y);
                if (connType != ConnectorType.None)
                {
                    if (current == null || current.type != connType)
                    {
                        current = new LocalConnectorRun { edge = edge, type = connType };
                        runs.Add(current);
                    }
                    current.cells.Add(cell);
                }
                else
                {
                    current = null;
                }
            }
        }
    }
}

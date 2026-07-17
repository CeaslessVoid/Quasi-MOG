using UnityEngine;

namespace RoomGen
{
    /// <summary>
    /// Builds simple rectangular room templates in code, so the generator can be tested
    /// without hand-editing arrays in the inspector. Delete/ignore once the in-game room
    /// builder exists and real content is authored through it.
    /// </summary>
    public static class RoomTemplateFactory
    {
        public static RoomTemplate CreateRectRoom(string name, int width, int height, params string[] typeTags)
        {
            var t = ScriptableObject.CreateInstance<RoomTemplate>();
            t.name = name;
            t.width = Mathf.Max(3, width);
            t.height = Mathf.Max(3, height);
            t.typeTags.AddRange(typeTags);

            int count = t.width * t.height;
            t.floorLayer = new FloorType[count];
            t.normalLayer = new NormalType[count];
            t.connectorLayer = new ConnectorType[count];
            t.ceilingLayer = new CeilingCellStub[count];

            for (int y = 0; y < t.height; y++)
            {
                for (int x = 0; x < t.width; x++)
                {
                    bool boundary = t.IsBoundary(x, y);
                    t.SetFloor(x, y, FloorType.Floor);
                    t.SetNormal(x, y, boundary ? NormalType.Wall : NormalType.Empty);
                }
            }

            return t;
        }

        /// <summary>
        /// Flags a straight run of boundary wall cells along one edge as connectors.
        /// start/length are measured along the edge in the same order RoomTemplateUtility
        /// uses (increasing X for North/South, increasing Y for East/West).
        /// </summary>
        public static void AddConnector(RoomTemplate t, Edge edge, int start, int length, ConnectorType type = ConnectorType.Normal)
        {
            for (int i = 0; i < length; i++)
            {
                Vector2Int cell = edge switch
                {
                    Edge.South => new Vector2Int(start + i, 0),
                    Edge.North => new Vector2Int(start + i, t.height - 1),
                    Edge.West => new Vector2Int(0, start + i),
                    Edge.East => new Vector2Int(t.width - 1, start + i),
                    _ => throw new System.ArgumentOutOfRangeException(nameof(edge))
                };
                t.SetConnector(cell.x, cell.y, type);
            }
        }
    }
}

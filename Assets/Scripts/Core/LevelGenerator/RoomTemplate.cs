using System.Collections.Generic;
using UnityEngine;

namespace RoomGen
{
    [CreateAssetMenu(fileName = "NewRoomTemplate", menuName = "RoomGen/Room Template")]
    public class RoomTemplate : ScriptableObject
    {
        [Header("Size")]
        [Min(3)] public int width = 3;
        [Min(3)] public int height = 3;

        [Header("Tags")]
        public List<string> typeTags = new List<string>();
        public List<string> zoneTags = new List<string>();

        public FloorType[] floorLayer;
        public NormalType[] normalLayer;
        public ConnectorType[] connectorLayer;
        public CeilingCell[] ceilingLayer;

        [Header("Defs")]
        public string[] wallDefLayer;
        public string[] doorDefLayer;
        public string[] floorDefLayer;
        public string preferredDoorDef;

        [Header("Props")]
        public List<PropPlacement> props = new List<PropPlacement>();

        [Header("Generation weighting")]
        public int desiredConnections = 2;

        [Range(0f, 1f)]
        public float extraConnectionChance = 0.15f;

        [Range(0f, 1f)]
        public float chanceToConnectWhenBelowTarget = 0.9f;

        public float selectionWeight = 1f;

        [Header("Reconnection")]
        [Range(0f, 1f)]
        public float reconnectionChance = 0.2f;

        [Range(0f, 1f)]
        public float reconnectionDoubleChance = 0.5f;

        public bool HasTag(string tag) => typeTags != null && typeTags.Contains(tag);

        public int CellCount => width * height;

        public FloorType GetFloor(int x, int y) => floorLayer[y * width + x];
        public NormalType GetNormal(int x, int y) => normalLayer[y * width + x];
        public ConnectorType GetConnector(int x, int y) => connectorLayer[y * width + x];
        public string GetWallDef(int x, int y) => wallDefLayer != null && wallDefLayer.Length == CellCount ? wallDefLayer[y * width + x] : null;
        public string GetDoorDef(int x, int y) => doorDefLayer != null && doorDefLayer.Length == CellCount ? doorDefLayer[y * width + x] : null;
        public string GetFloorDef(int x, int y) => floorDefLayer != null && floorDefLayer.Length == CellCount ? floorDefLayer[y * width + x] : null;

        public void SetFloor(int x, int y, FloorType v) => floorLayer[y * width + x] = v;
        public void SetNormal(int x, int y, NormalType v) => normalLayer[y * width + x] = v;
        public void SetConnector(int x, int y, ConnectorType v) => connectorLayer[y * width + x] = v;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

        public bool IsBoundary(int x, int y) => x == 0 || y == 0 || x == width - 1 || y == height - 1;

        public bool IsConnectorEligible(int x, int y) => RoomTemplateUtility.IsConnectorEligible(x, y, width, height, normalLayer);

#if UNITY_EDITOR
        [ContextMenu("Allocate / Resize Layers")]
        private void AllocateLayers()
        {
            int count = width * height;
            floorLayer = Resize(floorLayer, count);
            normalLayer = Resize(normalLayer, count);
            connectorLayer = Resize(connectorLayer, count);
            ceilingLayer = Resize(ceilingLayer, count);
            wallDefLayer = Resize(wallDefLayer, count);
            doorDefLayer = Resize(doorDefLayer, count);
            floorDefLayer = Resize(floorDefLayer, count);
        }

        private static T[] Resize<T>(T[] source, int size)
        {
            var result = new T[size];
            if (source != null)
            {
                for (int i = 0; i < Mathf.Min(source.Length, size); i++)
                    result[i] = source[i];
            }
            return result;
        }
#endif
    }
}

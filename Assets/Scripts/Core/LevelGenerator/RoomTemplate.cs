using System.Collections.Generic;
using UnityEngine;

namespace RoomGen
{
    /// <summary>
    /// A room's authored data: fixed size grid, three layers (Floor, Normal, reserved
    /// Ceiling-stub), boundary connector designators, and discrete props. This is pure
    /// data - it has no idea whether/where it's been placed in a level.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRoomTemplate", menuName = "RoomGen/Room Template")]
    public class RoomTemplate : ScriptableObject
    {
        [Header("Size (fixed once authored, min 3x3)")]
        [Min(3)] public int width = 3;
        [Min(3)] public int height = 3;

        [Header("Tags")]
        [Tooltip("Functional type: spawn, corridor, factory, living_quarters, outside, etc.")]
        public List<string> typeTags = new List<string>();

        [Tooltip("Reserved - base-location tagging, lightly used for now.")]
        public List<string> zoneTags = new List<string>();

        [Header("Layers (flattened, index = y * width + x)")]
        public FloorType[] floorLayer;
        public NormalType[] normalLayer;

        [Tooltip("Only meaningful on boundary cells. Interior cells should stay None.")]
        public ConnectorType[] connectorLayer;

        [Header("Reserved - stub only, not read/written by the generator yet")]
        public CeilingCellStub[] ceilingLayer;

        [Header("Props (carry rotation; do not block placement checks in v1)")]
        public List<PropPlacement> props = new List<PropPlacement>();

        [Header("Generation weighting - tweak freely")]
        [Tooltip("How many resolved connections this room 'wants' before it stops actively seeking more.")]
        public int desiredConnections = 2;

        [Range(0f, 1f)]
        [Tooltip("Chance to still attempt a connection once desiredConnections has already been reached.")]
        public float extraConnectionChance = 0.15f;

        [Range(0f, 1f)]
        [Tooltip("Chance to attempt a connection while still below desiredConnections.")]
        public float chanceToConnectWhenBelowTarget = 0.9f;

        [Tooltip("Relative weight when the generator picks a candidate room to grow into an open connector.")]
        public float selectionWeight = 1f;

        [Header("Reconnection (extra doors between already-connected rooms)")]
        [Range(0f, 1f)]
        [Tooltip("Chance this room tries to open a second door to a room it's already connected to, if space allows.")]
        public float reconnectionChance = 0.2f;

        [Range(0f, 1f)]
        [Tooltip("For reconnections only: chance the extra door is a 2x1 instead of 1x1, when a 2x1 would physically fit. Restricted connectors always stay 1x1 regardless of this.")]
        public float reconnectionDoubleChance = 0.5f;

        public bool HasTag(string tag) => typeTags != null && typeTags.Contains(tag);

        public int CellCount => width * height;

        public FloorType GetFloor(int x, int y) => floorLayer[y * width + x];
        public NormalType GetNormal(int x, int y) => normalLayer[y * width + x];
        public ConnectorType GetConnector(int x, int y) => connectorLayer[y * width + x];

        public void SetFloor(int x, int y, FloorType v) => floorLayer[y * width + x] = v;
        public void SetNormal(int x, int y, NormalType v) => normalLayer[y * width + x] = v;
        public void SetConnector(int x, int y, ConnectorType v) => connectorLayer[y * width + x] = v;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

        public bool IsBoundary(int x, int y) => x == 0 || y == 0 || x == width - 1 || y == height - 1;

#if UNITY_EDITOR
        [ContextMenu("Allocate / Resize Layers")]
        private void AllocateLayers()
        {
            int count = width * height;
            floorLayer = Resize(floorLayer, count);
            normalLayer = Resize(normalLayer, count);
            connectorLayer = Resize(connectorLayer, count);
            ceilingLayer = Resize(ceilingLayer, count);
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

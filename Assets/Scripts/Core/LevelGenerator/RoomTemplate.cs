using System.Collections.Generic;
using UnityEngine;

namespace RoomGen
{
    [CreateAssetMenu(fileName = "NewRoomTemplate", menuName = "RoomGen/Room Template")]
    public class RoomTemplate : ScriptableObject
    {
        public RoomData data = new RoomData();
        public CeilingCell[] ceilingLayer;

        private List<LocalConnectorRun> _connectorRunsCache;

        public int width => data.width;
        public int height => data.height;
        public int CellCount => data.CellCount;

        public bool HasTag(string tag) => data.HasTag(tag);
        public bool InBounds(int x, int y) => data.InBounds(x, y);
        public bool IsBoundary(int x, int y) => data.IsBoundary(x, y);
        public bool IsConnectorEligible(int x, int y) => data.IsConnectorEligible(x, y);

        public FloorType GetFloor(int x, int y) => data.GetFloor(x, y);
        public NormalType GetNormal(int x, int y) => data.GetNormal(x, y);
        public ConnectorType GetConnector(int x, int y) => data.GetConnector(x, y);
        public string GetWallDef(int x, int y) => data.GetWallDef(x, y);
        public string GetDoorDef(int x, int y) => data.GetDoorDef(x, y);
        public string GetFloorDef(int x, int y) => data.GetFloorDef(x, y);

        public int desiredConnections => data.desiredConnections;
        public float extraConnectionChance => data.extraConnectionChance;
        public float chanceToConnectWhenBelowTarget => data.chanceToConnectWhenBelowTarget;
        public float selectionWeight => data.selectionWeight;
        public string preferredSingleDoorDef => data.preferredSingleDoorDef;
        public string preferredDoubleDoorDef => data.preferredDoubleDoorDef;
        public List<PropPlacement> props => data.props;

        public List<LocalConnectorRun> GetConnectorRuns()
        {
            _connectorRunsCache ??= RoomTemplateUtility.FindConnectorRuns(data);
            return _connectorRunsCache;
        }

#if UNITY_EDITOR
        [ContextMenu("Allocate / Resize Layers")]
        private void AllocateLayers()
        {
            data.Allocate(data.width, data.height);
            ceilingLayer = new CeilingCell[data.CellCount];
        }
#endif
    }
}
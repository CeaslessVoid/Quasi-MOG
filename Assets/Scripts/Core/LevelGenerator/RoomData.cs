using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoomGen
{
    [Serializable]
    public class RoomData
    {
        public string templateId = "NewRoom";
        public int width = 3;
        public int height = 3;

        public List<string> typeTags = new List<string>();
        public List<string> zoneTags = new List<string>();

        public FloorType[] floorLayer;
        public NormalType[] normalLayer;
        public ConnectorType[] connectorLayer;

        public string[] wallDefLayer;
        public string[] doorDefLayer;
        public string[] floorDefLayer;
        public string preferredSingleDoorDef;
        public string preferredDoubleDoorDef;

        public List<PropPlacement> props = new List<PropPlacement>();

        public int desiredConnections = 2;
        public float chanceToConnectWhenBelowTarget = 0.9f;
        public float selectionWeight = 1f;

        public int CellCount => width * height;

        public void Allocate(int w, int h)
        {
            width = Mathf.Max(3, w);
            height = Mathf.Max(3, h);
            int count = CellCount;
            floorLayer = new FloorType[count];
            normalLayer = new NormalType[count];
            connectorLayer = new ConnectorType[count];
            wallDefLayer = new string[count];
            doorDefLayer = new string[count];
            floorDefLayer = new string[count];
            props = new List<PropPlacement>();
            preferredSingleDoorDef = null;
            preferredDoubleDoorDef = null;
        }

        public RoomData Clone()
        {
            int count = CellCount;
            return new RoomData
            {
                templateId = templateId,
                width = width,
                height = height,
                typeTags = new List<string>(typeTags),
                zoneTags = new List<string>(zoneTags),
                floorLayer = (FloorType[])floorLayer.Clone(),
                normalLayer = (NormalType[])normalLayer.Clone(),
                connectorLayer = (ConnectorType[])connectorLayer.Clone(),
                wallDefLayer = CloneOrNew(wallDefLayer, count),
                doorDefLayer = CloneOrNew(doorDefLayer, count),
                floorDefLayer = CloneOrNew(floorDefLayer, count),
                preferredSingleDoorDef = preferredSingleDoorDef,
                preferredDoubleDoorDef = preferredDoubleDoorDef,
                props = new List<PropPlacement>(props),
                desiredConnections = desiredConnections,
                chanceToConnectWhenBelowTarget = chanceToConnectWhenBelowTarget,
                selectionWeight = selectionWeight
            };
        }

        private int Index(int x, int y) => y * width + x;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;
        public bool IsBoundary(int x, int y) => x == 0 || y == 0 || x == width - 1 || y == height - 1;
        public bool HasTag(string tag) => typeTags != null && typeTags.Contains(tag);

        public FloorType GetFloor(int x, int y) => floorLayer[Index(x, y)];
        public NormalType GetNormal(int x, int y) => normalLayer[Index(x, y)];
        public ConnectorType GetConnector(int x, int y) => connectorLayer[Index(x, y)];
        public string GetWallDef(int x, int y) => wallDefLayer != null && wallDefLayer.Length == CellCount ? wallDefLayer[Index(x, y)] : null;
        public string GetDoorDef(int x, int y) => doorDefLayer != null && doorDefLayer.Length == CellCount ? doorDefLayer[Index(x, y)] : null;
        public string GetFloorDef(int x, int y) => floorDefLayer != null && floorDefLayer.Length == CellCount ? floorDefLayer[Index(x, y)] : null;

        public void SetFloor(int x, int y, FloorType v)
        {
            if (!InBounds(x, y)) return;
            floorLayer[Index(x, y)] = v;
            if (v == FloorType.Void) floorDefLayer[Index(x, y)] = null;
        }

        public void SetNormal(int x, int y, NormalType v)
        {
            if (!InBounds(x, y)) return;
            normalLayer[Index(x, y)] = v;
            if (v != NormalType.Wall) wallDefLayer[Index(x, y)] = null;
            if (v != NormalType.Door) doorDefLayer[Index(x, y)] = null;
        }

        public void SetWallDef(int x, int y, string defName) { if (InBounds(x, y)) wallDefLayer[Index(x, y)] = defName; }
        public void SetDoorDef(int x, int y, string defName) { if (InBounds(x, y)) doorDefLayer[Index(x, y)] = defName; }
        public void SetFloorDef(int x, int y, string defName) { if (InBounds(x, y)) floorDefLayer[Index(x, y)] = defName; }

        public void SetConnector(int x, int y, ConnectorType v) { if (InBounds(x, y) && GetNormal(x, y) == NormalType.Wall) connectorLayer[Index(x, y)] = v; }

        public bool IsConnectorEligible(int x, int y) => RoomTemplateUtility.IsConnectorEligible(x, y, width, height, normalLayer);

        public void SetProp(PropPlacement p)
        {
            RemoveProp(p.cellX, p.cellY);
            props.Add(p);
        }

        public bool RemoveProp(int x, int y)
        {
            for (int i = 0; i < props.Count; i++)
            {
                if (props[i].cellX == x && props[i].cellY == y)
                {
                    props.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public PropPlacement? GetPropAt(int x, int y)
        {
            foreach (var p in props)
                if (p.cellX == x && p.cellY == y) return p;
            return null;
        }

        private static string[] CloneOrNew(string[] source, int count)
        {
            if (source != null && source.Length == count) return (string[])source.Clone();
            return new string[count];
        }
    }
}
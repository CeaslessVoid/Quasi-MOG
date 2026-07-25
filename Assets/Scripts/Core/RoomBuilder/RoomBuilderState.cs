using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoomGen
{
    [Serializable]
    public class RoomBuilderState
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
        public string preferredDoorDef;

        public List<PropPlacement> props = new List<PropPlacement>();

        public int desiredConnections = 2;
        public float extraConnectionChance = 0.15f;
        public float chanceToConnectWhenBelowTarget = 0.9f;
        public float selectionWeight = 1f;
        public float reconnectionChance = 0.2f;
        public float reconnectionDoubleChance = 0.5f;

        public void Initialize(int w, int h)
        {
            width = Mathf.Max(3, w);
            height = Mathf.Max(3, h);
            int count = width * height;
            floorLayer = new FloorType[count];
            normalLayer = new NormalType[count];
            connectorLayer = new ConnectorType[count];
            wallDefLayer = new string[count];
            doorDefLayer = new string[count];
            floorDefLayer = new string[count];
            props = new List<PropPlacement>();
            preferredDoorDef = null;
        }

        private int Index(int x, int y) => y * width + x;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;
        public bool IsBoundary(int x, int y) => x == 0 || y == 0 || x == width - 1 || y == height - 1;

        public FloorType GetFloorAt(int x, int y) => floorLayer[Index(x, y)];
        public NormalType GetNormalAt(int x, int y) => normalLayer[Index(x, y)];
        public ConnectorType GetConnectorAt(int x, int y) => connectorLayer[Index(x, y)];
        public string GetWallDefAt(int x, int y) => wallDefLayer[Index(x, y)];
        public string GetDoorDefAt(int x, int y) => doorDefLayer[Index(x, y)];
        public string GetFloorDefAt(int x, int y) => floorDefLayer[Index(x, y)];

        public void SetFloorAt(int x, int y, FloorType v)
        {
            if (!InBounds(x, y)) return;
            floorLayer[Index(x, y)] = v;
            if (v == FloorType.Void) floorDefLayer[Index(x, y)] = null;
        }

        public void SetNormalAt(int x, int y, NormalType v)
        {
            if (!InBounds(x, y)) return;
            normalLayer[Index(x, y)] = v;
            if (v != NormalType.Wall) wallDefLayer[Index(x, y)] = null;
            if (v != NormalType.Door) doorDefLayer[Index(x, y)] = null;
        }

        public void SetWallDefAt(int x, int y, string defName) { if (InBounds(x, y)) wallDefLayer[Index(x, y)] = defName; }
        public void SetDoorDefAt(int x, int y, string defName) { if (InBounds(x, y)) doorDefLayer[Index(x, y)] = defName; }
        public void SetFloorDefAt(int x, int y, string defName) { if (InBounds(x, y)) floorDefLayer[Index(x, y)] = defName; }

        public void SetConnectorAt(int x, int y, ConnectorType v) { if (InBounds(x, y) && GetNormalAt(x, y) == NormalType.Wall) connectorLayer[Index(x, y)] = v; }

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
    }
}

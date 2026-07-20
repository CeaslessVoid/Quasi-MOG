using System;

namespace RoomGen
{
    public enum FloorType
    {
        Void = 0,
        Floor = 1,
        Water = 2,
    }

    public enum NormalType
    {
        Empty = 0,
        Wall = 1,
        Door = 2,
    }

    public enum ConnectorType
    {
        None = 0,
        Normal = 1,
        Restricted = 2,
        AlwaysDouble = 3,
    }

    public enum Edge
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3,
    }

    public enum ConnectorState
    {
        Open = 0,
        Sealed = 1,
        Connected = 2,
    }

    public enum DoorSize
    {
        None = 0,
        Single1x1 = 1,
        Double2x1 = 2,
    }


    [Serializable]
    public struct PropPlacement
    {
        public string propId;
        public int cellX;
        public int cellY;
        public PropRotation rotation;

        public enum PropRotation
        {
            North = 0,
            East = 1,
            South = 2,
            West = 3,
        }
    }

    [Serializable]
    public struct CeilingCell
    {
        public string propId;
    }
}

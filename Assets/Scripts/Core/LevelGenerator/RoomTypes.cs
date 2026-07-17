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
        Restricted = 2, // always resolves to a 1x1 door, never 2x1
        AlwaysDouble = 3, // tries to resolve to a 2x1 door even if the partner side is Restricted
    }

    public enum Edge
    {
        North = 0, // +Y (max Y row)
        East = 1,  // +X (max X column)
        South = 2, // -Y (Y = 0 row)
        West = 3,  // -X (X = 0 column)
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

    /// <summary>
    /// A discrete placed object (prop/furniture/machine). Carries rotation, unlike
    /// walls/doors which have no stored orientation and are inferred visually at build time.
    /// baseRotationDeg is applied on top of the room's own rotation when the room is placed.
    /// </summary>
    [Serializable]
    public struct PropPlacement
    {
        public string propId;
        public int cellX;
        public int cellY;
        public int baseRotationDeg; // 0/90/180/270
    }

    /// <summary>
    /// Reserved for future ceiling props (lights, vents, sprinklers, etc). Stub only - not
    /// read or written by the generator yet.
    /// </summary>
    [Serializable]
    public struct CeilingCellStub
    {
        public string propId;
    }
}

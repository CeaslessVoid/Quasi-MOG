using System;
using UnityEngine;
using BattleAngel.Grid;

namespace BattleAngel.Rooms
{
    /// Which side of the room's bounding box a connector sits on. Used to match rooms
    /// face-to-face during assembly (a North connector must mate with a South connector).
    /// Order matters: it must go clockwise (N, E, S, W) so a 90-degree rotation step can
    /// be applied as a simple (value + 1) % 4 — see LevelAssembler.RotateDirection.
    public enum ConnectorDirection { North, East, South, West }

    [Serializable]
    public struct RoomConnector
    {
        public Vector2Int localPosition; // cell position relative to room origin (bottom-left)
        public ConnectorDirection direction;
        public bool isCriticalPath; // reserved for objective-chain placement logic
    }

    [Serializable]
    public struct RoomTileEntry
    {
        public Vector2Int localPosition;
        public int floorTileId;
        public int wallTileId;
        public TileFlags flags;
    }

    [Serializable]
    public struct RoomPropEntry
    {
        public Vector2Int localPosition;
        public string propId;      // key into PropCatalogAsset
        public int rotationSteps;  // 0-3, 90 degree increments
    }

    /// <summary>
    /// A hand-authored prefab room, stored as pure data rather than a scene/GameObject prefab.
    /// The LevelAssembler reads this and writes directly into GridManager + the instanced
    /// prop renderer — no Instantiate() calls per tile or per prop. Author these with an
    /// in-editor painter tool (future work) or by hand for the first pass.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomDefinition", menuName = "BattleAngel/Room Definition")]
    public class RoomDefinition : ScriptableObject
    {
        public string roomId;
        public Vector2Int size;
        public string[] tags; // e.g. "corridor", "objective", "corp_office", "sewer", "start"

        public RoomTileEntry[] tiles;
        public RoomPropEntry[] props;
        public RoomConnector[] connectors;

        [Range(0f, 10f)] public float selectionWeight = 1f;
    }
}

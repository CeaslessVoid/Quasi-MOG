using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RoomGen;

namespace Networking
{
    public static class LevelNetworkSerializer
    {
        public static byte[] Serialize(LevelGrid grid)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(grid.Width);
            writer.Write(grid.Height);
            writer.Write(grid.Origin.x);
            writer.Write(grid.Origin.y);

            var stringTable = new List<string>();
            var stringLookup = new Dictionary<string, int>();

            int InternString(string s)
            {
                if (s == null) return -1;
                if (stringLookup.TryGetValue(s, out int idx)) return idx;
                idx = stringTable.Count;
                stringTable.Add(s);
                stringLookup[s] = idx;
                return idx;
            }

            var cells = grid.RawCells;
            var wallIdx = new int[cells.Length];
            var doorIdx = new int[cells.Length];
            var floorIdx = new int[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                wallIdx[i] = InternString(cells[i].wallDef);
                doorIdx[i] = InternString(cells[i].doorDef);
                floorIdx[i] = InternString(cells[i].floorDef);
            }

            writer.Write(stringTable.Count);
            foreach (var s in stringTable) writer.Write(s);

            writer.Write(cells.Length);
            for (int i = 0; i < cells.Length; i++)
            {
                var c = cells[i];
                writer.Write((byte)c.floor);
                writer.Write((byte)c.normal);
                writer.Write(wallIdx[i]);
                writer.Write(doorIdx[i]);
                writer.Write(floorIdx[i]);
                writer.Write(c.ownerRoomId);
            }

            writer.Write(grid.PlacedRooms.Count);
            foreach (var room in grid.PlacedRooms)
            {
                writer.Write(room.id);
                writer.Write(room.template != null ? room.template.data.templateId : "");
                writer.Write(room.origin.x);
                writer.Write(room.origin.y);
                writer.Write(room.rotationDeg);

                writer.Write(room.props.Count);
                foreach (var p in room.props)
                {
                    writer.Write(p.propId ?? "");
                    writer.Write((int)p.worldFacing);
                    writer.Write(p.worldCells.Count);
                    foreach (var c in p.worldCells)
                    {
                        writer.Write(c.x);
                        writer.Write(c.y);
                    }
                }
            }

            return stream.ToArray();
        }

        public static LevelGrid Deserialize(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            var origin = new Vector2Int(reader.ReadInt32(), reader.ReadInt32());

            int stringCount = reader.ReadInt32();
            var stringTable = new string[stringCount];
            for (int i = 0; i < stringCount; i++) stringTable[i] = reader.ReadString();

            string Resolve(int idx) => idx < 0 ? null : stringTable[idx];

            int cellCount = reader.ReadInt32();
            var cells = new LevelCell[cellCount];
            for (int i = 0; i < cellCount; i++)
            {
                cells[i] = new LevelCell
                {
                    floor = (FloorType)reader.ReadByte(),
                    normal = (NormalType)reader.ReadByte(),
                    wallDef = Resolve(reader.ReadInt32()),
                    doorDef = Resolve(reader.ReadInt32()),
                    floorDef = Resolve(reader.ReadInt32()),
                    ownerRoomId = reader.ReadInt32()
                };
            }

            int roomCount = reader.ReadInt32();
            var placedRooms = new List<PlacedRoom>(roomCount);
            for (int i = 0; i < roomCount; i++)
            {
                int id = reader.ReadInt32();
                string templateId = reader.ReadString();
                var roomOrigin = new Vector2Int(reader.ReadInt32(), reader.ReadInt32());
                int rotationDeg = reader.ReadInt32();

                var template = RoomLibrary.GetByTemplateId(templateId);
                if (template == null)
                    Debug.LogWarning($"LevelNetworkSerializer: could not resolve room template '{templateId}' locally. Make sure StreamingAssets/Rooms is identical on every client build.");

                var room = new PlacedRoom
                {
                    id = id,
                    template = template,
                    origin = roomOrigin,
                    rotationDeg = rotationDeg
                };

                int propCount = reader.ReadInt32();
                for (int p = 0; p < propCount; p++)
                {
                    string propId = reader.ReadString();
                    var facing = (GameDefs.PropFacing)reader.ReadInt32();
                    int worldCellCount = reader.ReadInt32();
                    var worldCells = new List<Vector2Int>(worldCellCount);
                    for (int c = 0; c < worldCellCount; c++)
                        worldCells.Add(new Vector2Int(reader.ReadInt32(), reader.ReadInt32()));

                    room.props.Add(new PlacedProp
                    {
                        propId = propId,
                        worldCells = worldCells,
                        worldFacing = facing,
                        ownerRoomId = id
                    });
                }

                placedRooms.Add(room);
            }

            return new LevelGrid(cells, width, height, origin, placedRooms);
        }
    }
}

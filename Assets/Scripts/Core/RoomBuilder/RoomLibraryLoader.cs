using System.Collections.Generic;

namespace RoomGen
{
    public static class RoomLibraryLoader
    {
        private static List<RoomTemplate> _cache;

        public static List<RoomTemplate> LoadAll()
        {
            if (_cache != null) return _cache;

            var result = new List<RoomTemplate>();
            foreach (var file in RoomLibraryIO.ListRoomFiles())
                result.Add(RoomTemplateConverter.ToRoomTemplate(RoomLibraryIO.Load(file)));

            _cache = result;
            return _cache;
        }

        public static void InvalidateCache() => _cache = null;
    }
}
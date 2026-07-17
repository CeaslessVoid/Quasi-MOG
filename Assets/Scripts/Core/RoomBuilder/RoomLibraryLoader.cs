using System.Collections.Generic;

namespace RoomGen
{
    /// <summary>
    /// Loads every room saved by the builder and converts them into RoomTemplates ready
    /// to hand to RoomGenerator.SetTemplates(...). This is what replaces
    /// RoomGenTestBootstrap's code-defined rooms once you've actually built some.
    /// </summary>
    public static class RoomLibraryLoader
    {
        public static List<RoomTemplate> LoadAll()
        {
            var result = new List<RoomTemplate>();
            foreach (var file in RoomLibraryIO.ListRoomFiles())
            {
                var state = RoomLibraryIO.Load(file);
                result.Add(RoomTemplateConverter.ToRoomTemplate(state));
            }
            return result;
        }
    }
}

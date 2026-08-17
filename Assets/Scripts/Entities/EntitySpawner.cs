using System.Collections.Generic;
using System.Linq;
using GameDefs;
using RoomGen;
using UnityEngine;

namespace Entities
{
    public static class EntitySpawner
    {
        public static List<PlayableEntity> SpawnPlayers(LevelGrid grid, string entityDefName, int playerCount, float cellSize, Transform parent = null)
        {
            var result = new List<PlayableEntity>();
            if (grid == null || playerCount <= 0) return result;

            var def = DefDatabase.Get<EntityDef>(entityDefName);
            if (def == null)
            {
                Debug.LogError($"EntitySpawner: no EntityDef named '{entityDefName}' found.");
                return result;
            }

            var candidates = FindSpawnableCells(grid);
            var used = new HashSet<Vector2Int>();

            for (int i = 0; i < playerCount; i++)
            {
                var cell = PickUnusedCell(candidates, used, grid.Origin);
                result.Add(SpawnOne(def, cell, cellSize, parent));
            }

            return result;
        }

        public static Vector2Int PickUnusedCell(List<Vector2Int> candidates, HashSet<Vector2Int> used, Vector2Int fallback)
        {
            foreach (var cell in candidates)
                if (used.Add(cell)) return cell;

            used.Add(fallback);
            return fallback;
        }

        public static List<Vector2Int> FindSpawnableCells(LevelGrid grid)
        {
            var spawnRoom = grid.PlacedRooms.FirstOrDefault(r => r.template != null && r.template.HasTag("spawn"));
            var room = spawnRoom ?? grid.PlacedRooms.FirstOrDefault();
            if (room == null) return new List<Vector2Int>();

            var cells = new List<Vector2Int>();
            for (int y = 0; y < room.template.height; y++)
            {
                for (int x = 0; x < room.template.width; x++)
                {
                    if (room.template.GetFloor(x, y) == FloorType.Void) continue;
                    var world = RoomTemplateUtility.LocalToWorld(x, y, room.template.width, room.template.height, room.rotationDeg, room.origin);
                    if (grid.GetCell(world).normal == NormalType.Empty) cells.Add(world);
                }
            }

            return cells;
        }

        private static PlayableEntity SpawnOne(EntityDef def, Vector2Int cell, float cellSize, Transform parent)
        {
            var go = new GameObject($"Entity_{def.DefName}");
            if (parent != null) go.transform.SetParent(parent, false);

            var entity = go.AddComponent<PlayableEntity>();
            entity.Configure(def, cell, cellSize);
            entity.RefreshVisuals(isLocalPlayer: true);
            return entity;
        }
    }
}
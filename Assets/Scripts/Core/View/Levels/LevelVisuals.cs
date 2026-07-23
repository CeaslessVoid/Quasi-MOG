using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoomGen
{
    public class LevelVisuals : RoomVisualsBase
    {
        private Tilemap _floorTilemap;
        private Tilemap _wallTilemap;
        private Tile _floorTile;

        protected override void OnInitialize()
        {
            var gridGO = new GameObject("LevelGrid");
            gridGO.transform.SetParent(transform, false);
            var grid = gridGO.AddComponent<Grid>();
            grid.cellSize = new Vector3(cellSize, cellSize, 1f);

            var floorGO = new GameObject("FloorTilemap");
            floorGO.transform.SetParent(gridGO.transform, false);
            _floorTilemap = floorGO.AddComponent<Tilemap>();
            floorGO.AddComponent<TilemapRenderer>().sortingOrder = 0;

            var wallGO = new GameObject("WallTilemap");
            wallGO.transform.SetParent(gridGO.transform, false);
            _wallTilemap = wallGO.AddComponent<Tilemap>();
            wallGO.AddComponent<TilemapRenderer>().sortingOrder = 5;

            if (floorAsset != null && floorAsset.Sprite != null)
            {
                _floorTile = ScriptableObject.CreateInstance<Tile>();
                _floorTile.sprite = floorAsset.Sprite;
                _floorTile.colliderType = Tile.ColliderType.None;
            }
        }

        public void Rebuild(LevelGrid grid)
        {
            EnsureInitialized();
            _floorTilemap.ClearAllTiles();
            _wallTilemap.ClearAllTiles();
            if (grid == null) return;

            foreach (var room in grid.PlacedRooms)
            {
                for (int y = 0; y < room.template.height; y++)
                {
                    for (int x = 0; x < room.template.width; x++)
                    {
                        var world = RoomTemplateUtility.LocalToWorld(x, y, room.template.width, room.template.height, room.rotationDeg, room.origin);
                        RefreshCell(grid, world);
                    }
                }
            }
        }

        private void RefreshCell(LevelGrid grid, Vector2Int cell)
        {
            var data = grid.GetCell(cell);
            var pos = new Vector3Int(cell.x, cell.y, 0);

            if (data.floor != FloorType.Void && _floorTile != null)
                _floorTilemap.SetTile(pos, _floorTile);

            if (data.normal == NormalType.Wall)
            {
                bool n = IsWallLike(grid, cell + Vector2Int.up);
                bool e = IsWallLike(grid, cell + Vector2Int.right);
                bool s = IsWallLike(grid, cell + Vector2Int.down);
                bool w = IsWallLike(grid, cell + Vector2Int.left);
                int bitmask = ComputeBitmask(n, e, s, w);
                _wallTilemap.SetTile(pos, wallAsset != null ? wallAsset.GetTile(bitmask) : null);
            }
        }

        private static bool IsWallLike(LevelGrid grid, Vector2Int cell) => grid.GetCell(cell).normal == NormalType.Wall;
    }
}
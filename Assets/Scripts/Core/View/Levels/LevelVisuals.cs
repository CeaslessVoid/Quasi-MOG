using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GameDefs;

namespace RoomGen
{
    public class LevelVisuals : RoomVisualsBase
    {
        [SerializeField] private PhysicsMaterial2D doorPhysicsMaterial;

        private Transform _root;
        private Tilemap _floorTilemap;
        private Tilemap _doorTilemap;

        private class WallLayer
        {
            public Tilemap tilemap;
            public CompositeCollider2D composite;
        }

        private WallLayer _defaultWallLayer;
        private readonly Dictionary<PhysicsMaterial2D, WallLayer> _materialWallLayers = new Dictionary<PhysicsMaterial2D, WallLayer>();
        private int _nextWallSortingOrder = 5;

        private readonly Dictionary<FloorDef, Tile> _floorTileCache = new Dictionary<FloorDef, Tile>();
        private readonly Dictionary<(WallDef, int), Tile> _wallTileCache = new Dictionary<(WallDef, int), Tile>();
        private readonly Dictionary<DoorDef, Tile> _doorTileCache = new Dictionary<DoorDef, Tile>();

        private Tile _waterTile;
        private Tile _missingFloorTile;
        private Tile _missingWallTile;
        private Tile _missingDoorTile;

        protected override void OnInitialize()
        {
            var gridGO = new GameObject("LevelGrid");
            gridGO.transform.SetParent(transform, false);
            var grid = gridGO.AddComponent<Grid>();
            grid.cellSize = new Vector3(cellSize, cellSize, 1f);
            _root = gridGO.transform;

            var floorGO = new GameObject("FloorTilemap");
            floorGO.transform.SetParent(_root, false);
            _floorTilemap = floorGO.AddComponent<Tilemap>();
            floorGO.AddComponent<TilemapRenderer>().sortingOrder = 0;

            var doorGO = new GameObject("DoorTilemap");
            doorGO.transform.SetParent(_root, false);
            _doorTilemap = doorGO.AddComponent<Tilemap>();
            doorGO.AddComponent<TilemapRenderer>().sortingOrder = 1000;

            var doorRb = doorGO.AddComponent<Rigidbody2D>();
            doorRb.bodyType = RigidbodyType2D.Static;

            var doorTmCollider = doorGO.AddComponent<TilemapCollider2D>();
            doorTmCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            var doorComposite = doorGO.AddComponent<CompositeCollider2D>();
            doorComposite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            if (doorPhysicsMaterial != null) doorComposite.sharedMaterial = doorPhysicsMaterial;

            _waterTile = BuildSolidTile(new Color(0.2f, 0.4f, 0.9f));
            _missingFloorTile = BuildSolidTile(Color.white, DefVisualUtility.MissingSprite);
            _missingWallTile = BuildSolidTile(Color.white, DefVisualUtility.MissingSprite);
            _missingDoorTile = BuildSolidTile(new Color(0.65f, 0.4f, 0.1f), DefVisualUtility.MissingSprite);
        }

        public void Rebuild(LevelGrid grid)
        {
            EnsureInitialized();
            ClearAll();
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

        private void ClearAll()
        {
            _floorTilemap.ClearAllTiles();
            _doorTilemap.ClearAllTiles();
            if (_defaultWallLayer != null) _defaultWallLayer.tilemap.ClearAllTiles();
            foreach (var layer in _materialWallLayers.Values)
                layer.tilemap.ClearAllTiles();
        }

        private void RefreshCell(LevelGrid grid, Vector2Int cell)
        {
            var data = grid.GetCell(cell);
            var pos = new Vector3Int(cell.x, cell.y, 0);

            if (data.floor == FloorType.Water)
            {
                _floorTilemap.SetTile(pos, _waterTile);
            }
            else if (data.floor == FloorType.Floor)
            {
                var floorDef = DefDatabase.Get<FloorDef>(data.floorDef);
                _floorTilemap.SetTile(pos, GetFloorTile(floorDef));
            }

            if (data.normal == NormalType.Wall)
            {
                bool n = IsWallLike(grid, cell + Vector2Int.up);
                bool e = IsWallLike(grid, cell + Vector2Int.right);
                bool s = IsWallLike(grid, cell + Vector2Int.down);
                bool w = IsWallLike(grid, cell + Vector2Int.left);
                int bitmask = ComputeBitmask(n, e, s, w);

                var wallDef = DefDatabase.Get<WallDef>(data.wallDef);
                var layer = GetOrCreateWallLayer(wallDef != null ? wallDef.PhysicsMaterial : null);
                layer.tilemap.SetTile(pos, GetWallTile(wallDef, bitmask));
            }
            else if (data.normal == NormalType.Door)
            {
                var doorDef = DefDatabase.Get<DoorDef>(data.doorDef);
                _doorTilemap.SetTile(pos, GetDoorTile(doorDef));
            }
        }

        private static bool IsWallLike(LevelGrid grid, Vector2Int cell) => grid.GetCell(cell).normal == NormalType.Wall;

        private WallLayer GetOrCreateWallLayer(PhysicsMaterial2D material)
        {
            if (material == null)
            {
                if (_defaultWallLayer == null) _defaultWallLayer = CreateWallLayer("WallTilemap_Default", null);
                return _defaultWallLayer;
            }

            if (_materialWallLayers.TryGetValue(material, out var layer)) return layer;
            layer = CreateWallLayer($"WallTilemap_{material.name}", material);
            _materialWallLayers[material] = layer;
            return layer;
        }

        private WallLayer CreateWallLayer(string name, PhysicsMaterial2D material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);

            var tilemap = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = _nextWallSortingOrder++;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            var tmCollider = go.AddComponent<TilemapCollider2D>();
            tmCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            var composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            if (material != null) composite.sharedMaterial = material;

            return new WallLayer { tilemap = tilemap, composite = composite };
        }

        private Tile GetFloorTile(FloorDef def)
        {
            if (def == null || !def.HasTexture) return _missingFloorTile;
            if (_floorTileCache.TryGetValue(def, out var tile)) return tile;

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = def.Sprite;
            tile.color = def.TintColor;
            tile.colliderType = Tile.ColliderType.None;
            _floorTileCache[def] = tile;
            return tile;
        }

        private Tile GetWallTile(WallDef def, int bitmask)
        {
            if (def == null || !def.HasTexture) return _missingWallTile;
            var key = (def, bitmask);
            if (_wallTileCache.TryGetValue(key, out var tile)) return tile;

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = def.GetSprite(bitmask);
            tile.color = def.TintColor;
            tile.colliderType = def.BlocksProjectiles ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            _wallTileCache[key] = tile;
            return tile;
        }

        private Tile GetDoorTile(DoorDef def)
        {
            if (def == null || !def.HasTexture) return _missingDoorTile;
            if (_doorTileCache.TryGetValue(def, out var tile)) return tile;

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = def.ClosedSprite;
            tile.color = def.TintColor;
            tile.colliderType = def.BlocksProjectilesWhenClosed ? Tile.ColliderType.Grid : Tile.ColliderType.None;
            _doorTileCache[def] = tile;
            return tile;
        }

        private static Tile BuildSolidTile(Color color, Sprite sprite = null)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite != null ? sprite : DefVisualUtility.SolidSprite;
            tile.color = color;
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }
    }
}

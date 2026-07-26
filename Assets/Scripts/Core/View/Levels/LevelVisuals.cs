using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using GameDefs;

namespace RoomGen
{
    public class LevelVisuals : RoomVisualsBase
    {
        private Transform _root;
        private Tilemap _floorTilemap;

        private class WallLayer
        {
            public Tilemap tilemap;
            public CompositeCollider2D composite;
        }

        private WallLayer _defaultWallLayer;
        private readonly Dictionary<PhysicsMaterial2D, WallLayer> _materialWallLayers = new Dictionary<PhysicsMaterial2D, WallLayer>();
        private int _nextWallSortingOrder = 5;

        private Transform _doorRoot;
        private readonly Dictionary<Vector2Int, DoorInstance> _doorsByCell = new Dictionary<Vector2Int, DoorInstance>();
        private readonly HashSet<Vector2Int> _processedDoorCells = new HashSet<Vector2Int>();
        private const int DoorSortingOrder = 2;

        private readonly Dictionary<FloorDef, Tile> _floorTileCache = new Dictionary<FloorDef, Tile>();
        private readonly Dictionary<(WallDef, int), Tile> _wallTileCache = new Dictionary<(WallDef, int), Tile>();

        private Tile _waterTile;
        private Tile _missingFloorTile;
        private Tile _missingWallTile;
        private Sprite _missingDoorSprite;

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

            _doorRoot = new GameObject("Doors").transform;
            _doorRoot.SetParent(_root, false);

            _waterTile = BuildSolidTile(new Color(0.2f, 0.4f, 0.9f));
            _missingFloorTile = BuildSolidTile(Color.white, DefVisualUtility.MissingSprite);
            _missingWallTile = BuildSolidTile(Color.white, DefVisualUtility.MissingSprite);
            _missingDoorSprite = DefVisualUtility.MissingSprite;
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
            if (_defaultWallLayer != null) _defaultWallLayer.tilemap.ClearAllTiles();
            foreach (var layer in _materialWallLayers.Values)
                layer.tilemap.ClearAllTiles();

            for (int i = _doorRoot.childCount - 1; i >= 0; i--)
                Destroy(_doorRoot.GetChild(i).gameObject);

            _doorsByCell.Clear();
            _processedDoorCells.Clear();
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
                RefreshDoorCell(grid, cell);
            }
        }

        private static bool IsWallLike(LevelGrid grid, Vector2Int cell) => grid.GetCell(cell).normal == NormalType.Wall;

        private void RefreshDoorCell(LevelGrid grid, Vector2Int cell)
        {
            if (_processedDoorCells.Contains(cell)) return;

            var data = grid.GetCell(cell);
            var doorDef = DefDatabase.Get<DoorDef>(data.doorDef);
            bool isNorthOrientation = grid.IsNorthOrientedDoor(cell);
            bool isDouble = doorDef != null && doorDef.IsDoubleDoor;

            if (isDouble && grid.TryFindDoorPartner(cell, data.doorDef, out var partner))
            {
                _processedDoorCells.Add(cell);
                _processedDoorCells.Add(partner);

                bool selfIsLeafA = IsLeafA(cell, partner, isNorthOrientation);
                var leafACell = selfIsLeafA ? cell : partner;
                var leafBCell = selfIsLeafA ? partner : cell;
                BuildDoubleDoor(leafACell, leafBCell, doorDef, isNorthOrientation);
            }
            else
            {
                _processedDoorCells.Add(cell);
                BuildSingleDoor(cell, doorDef, isNorthOrientation);
            }
        }

        private static bool IsLeafA(Vector2Int cell, Vector2Int partner, bool isNorthOrientation) =>
            isNorthOrientation ? partner.x > cell.x : partner.y < cell.y;

        private void BuildSingleDoor(Vector2Int cell, DoorDef def, bool isNorthOrientation)
        {
            Vector3 rootPos = CellCenter(cell);
            float slideDistance = cellSize * 0.5f;
            BuildDoorInstance(new[] { cell }, rootPos, 0f, slideDistance, def, isNorthOrientation, new Vector2(cellSize, cellSize));
        }

        private void BuildDoubleDoor(Vector2Int leafACell, Vector2Int leafBCell, DoorDef def, bool isNorthOrientation)
        {
            Vector3 rootPos = Vector3.Lerp(CellCenter(leafACell), CellCenter(leafBCell), 0.5f);
            float leafPositionOffset = cellSize * 0.5f;
            float slideDistance = cellSize;
            Vector2 colliderSize = isNorthOrientation
                ? new Vector2(cellSize * 2f, cellSize)
                : new Vector2(cellSize, cellSize * 2f);

            BuildDoorInstance(new[] { leafACell, leafBCell }, rootPos, leafPositionOffset, slideDistance, def, isNorthOrientation, colliderSize);
        }

        private void BuildDoorInstance(Vector2Int[] cells, Vector3 rootLocalPos, float leafPositionOffset, float slideDistance, DoorDef def, bool isNorthOrientation, Vector2 colliderSize)
        {
            var go = new GameObject("Door");
            go.transform.SetParent(_doorRoot, false);
            go.transform.localPosition = rootLocalPos;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = colliderSize;
            if (def != null && def.PhysicsMaterial != null) collider.sharedMaterial = def.PhysicsMaterial;

            Sprite baseSprite = def != null ? (isNorthOrientation ? def.NorthSprite : def.EastSprite) : null;
            if (baseSprite == null) baseSprite = _missingDoorSprite;
            Color tint = def != null ? def.TintColor : Color.white;

            Vector3 axis = isNorthOrientation ? Vector3.right : Vector3.up;
            float leafASign = isNorthOrientation ? -1f : 1f;
            Vector3 mirrorScale = isNorthOrientation ? new Vector3(-1f, 1f, 1f) : new Vector3(1f, -1f, 1f);

            Vector3 leafAOpenDirection = axis * leafASign;
            Vector3 leafBOpenDirection = -leafAOpenDirection;

            Vector3 leafAPos = leafAOpenDirection * leafPositionOffset;
            Vector3 leafBPos = -leafAPos;

            var leafA = CreateLeaf(go.transform, "LeafA", baseSprite, tint, leafAPos, Vector3.one);
            var leafB = CreateLeaf(go.transform, "LeafB", baseSprite, tint, leafBPos, mirrorScale);

            var doorInstance = go.AddComponent<DoorInstance>();
            doorInstance.Configure(def, leafA, leafB, leafAOpenDirection, leafBOpenDirection, slideDistance);

            foreach (var cell in cells)
                _doorsByCell[cell] = doorInstance;
        }

        private SpriteRenderer CreateLeaf(Transform parent, string name, Sprite sprite, Color color, Vector3 localPos, Vector3 localScale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = DoorSortingOrder;
            return sr;
        }

        private Vector3 CellCenter(Vector2Int cell) => new Vector3((cell.x + 0.5f) * cellSize, (cell.y + 0.5f) * cellSize, 0f);

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
            tile.colliderType = Tile.ColliderType.Grid;
            _wallTileCache[key] = tile;
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
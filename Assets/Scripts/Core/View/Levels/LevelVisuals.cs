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

        private Transform _propRoot;
        private const int PropSortingOrder = 3;
        private const int PropDecorativeSortingOrder = 4;

        private readonly Dictionary<FloorDef, Tile> _floorTileCache = new Dictionary<FloorDef, Tile>();
        private readonly Dictionary<(WallDef, int), Tile> _wallTileCache = new Dictionary<(WallDef, int), Tile>();
        private readonly Dictionary<LiquidDef, Tile> _liquidTileCache = new Dictionary<LiquidDef, Tile>();


        private Tile _missingLiquidTile;
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

            _propRoot = new GameObject("Props").transform;
            _propRoot.SetParent(_root, false);

            _missingLiquidTile = BuildSolidTile(Color.white, DefVisualUtility.MissingSprite);
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

                foreach (var prop in room.props)
                    SpawnProp(prop);
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

            for (int i = _propRoot.childCount - 1; i >= 0; i--)
                Destroy(_propRoot.GetChild(i).gameObject);

            _doorsByCell.Clear();
            _processedDoorCells.Clear();
        }

        private void RefreshCell(LevelGrid grid, Vector2Int cell)
        {
            var data = grid.GetCell(cell);
            var pos = new Vector3Int(cell.x, cell.y, 0);

            if (data.floor == FloorType.Liquid)
            {
                var liquidDef = DefDatabase.Get<LiquidDef>(data.floorDef);
                _floorTilemap.SetTile(pos, GetLiquidTile(liquidDef));
            }
            else if (data.floor == FloorType.Floor)
            {
                var floorDef = DefDatabase.Get<FloorDef>(data.floorDef);
                _floorTilemap.SetTile(pos, GetFloorTile(floorDef));
            }

            if (data.normal == NormalType.Wall)
            {
                bool n = grid.IsWallBlocking(cell + Vector2Int.up);
                bool e = grid.IsWallBlocking(cell + Vector2Int.right);
                bool s = grid.IsWallBlocking(cell + Vector2Int.down);
                bool w = grid.IsWallBlocking(cell + Vector2Int.left);
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

        private Tile GetLiquidTile(LiquidDef def)
        {
            if (def == null || !def.HasTexture) return _missingLiquidTile;
            if (_liquidTileCache.TryGetValue(def, out var tile)) return tile;

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = def.Sprite;
            tile.color = def.TintColor;
            tile.colliderType = Tile.ColliderType.None;
            _liquidTileCache[def] = tile;
            return tile;
        }

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

            Vector3 axis = isNorthOrientation ? Vector3.right : Vector3.up;
            float leafASign = isNorthOrientation ? -1f : 1f;
            Vector3 mirrorScale = isNorthOrientation ? new Vector3(-1f, 1f, 1f) : new Vector3(1f, -1f, 1f);

            Vector3 leafAOpenDirection = axis * leafASign;
            Vector3 leafBOpenDirection = -leafAOpenDirection;

            Vector3 leafAPos = leafAOpenDirection * leafPositionOffset;
            Vector3 leafBPos = -leafAPos;

            var leafA = CreateLeaf(go.transform, "LeafA", baseSprite, def, leafAPos, Vector3.one);
            var leafB = CreateLeaf(go.transform, "LeafB", baseSprite, def, leafBPos, mirrorScale);

            var doorInstance = go.AddComponent<DoorInstance>();
            doorInstance.Configure(def, leafA, leafB, leafAOpenDirection, leafBOpenDirection, slideDistance);

            foreach (var cell in cells)
                _doorsByCell[cell] = doorInstance;
        }

        private SpriteRenderer CreateLeaf(Transform parent, string name, Sprite sprite, Def def, Vector3 localPos, Vector3 localScale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = DoorSortingOrder;

            if (def != null) DefTintRenderer.Apply(sr, def);
            else DefTintRenderer.ApplyFlatTint(sr, Color.white);

            return sr;
        }

        private Vector3 CellCenter(Vector2Int cell) => new Vector3((cell.x + 0.5f) * cellSize, (cell.y + 0.5f) * cellSize, 0f);

        private void SpawnProp(PlacedProp p)
        {
            var def = DefDatabase.Get<PropDef>(p.propId);
            var category = def != null ? def.Category : PropCategory.Normal;
            if (!PropPlacementUtility.GetRenderBounds(p.worldCells, category, p.worldFacing, out var min, out var max)) return;

            int w = max.x - min.x + 1;
            int h = max.y - min.y + 1;
            bool flip = p.worldFacing == PropFacing.West;

            var go = new GameObject(string.IsNullOrEmpty(p.propId) ? "Prop" : p.propId);
            go.transform.SetParent(_propRoot, false);
            go.transform.localPosition = Vector3.Lerp(CellCenter(min), CellCenter(max), 0.5f);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(w * cellSize, h * cellSize);
            if (def != null && def.PhysicsMaterial != null) collider.sharedMaterial = def.PhysicsMaterial;

            var spriteGO = new GameObject("Sprite");
            spriteGO.transform.SetParent(go.transform, false);

            var sr = spriteGO.AddComponent<SpriteRenderer>();
            sr.sortingOrder = category == PropCategory.Decorative ? PropDecorativeSortingOrder : PropSortingOrder;

            Sprite sprite = def != null && def.HasTexture ? def.GetSprite(p.worldFacing) : DefVisualUtility.MissingSprite;
            sr.sprite = sprite;

            if (def != null && def.HasTexture)
                DefTintRenderer.Apply(sr, def.TintColor, def.SecondaryTintColor, def.GetMask(p.worldFacing));
            else
                DefTintRenderer.ApplyFlatTint(sr, DefVisualUtility.MissingColor);

            var fitSize = PropPlacementUtility.GetUniformFitSize(sprite, w * cellSize, h * cellSize);
            spriteGO.transform.localScale = new Vector3((flip ? -1f : 1f) * fitSize.x, fitSize.y, 1f);

            go.AddComponent<PropInstance>().Configure(def);
        }
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

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoomGen
{
    /// <summary>
    /// Grows a level outward from a single "spawn"-tagged room by repeatedly picking an
    /// open connector, deciding whether to grow through it, and fitting a candidate room's
    /// own connector run into the overlap. See README.md in this folder for the full
    /// algorithm write-up and known simplifications.
    /// </summary>
    public class RoomGenerator : MonoBehaviour
    {
        [Header("Room Pool")]
        [Tooltip("If true, every saved room under Assets/StreamingAssets/Rooms is loaded automatically each Generate() call, in addition to anything assigned below.")]
        [SerializeField] private bool autoLoadFromRoomLibrary = true;
        [SerializeField] private List<RoomTemplate> roomTemplates = new List<RoomTemplate>();

        private List<RoomTemplate> _activePool;

        [Header("Generation Targets (tweak freely)")]
        [SerializeField] private int desiredRoomCount = 40;
        [SerializeField] private int minCorridors = 3;
        [SerializeField] private float corridorWeightMultiplierBeforeMinMet = 4f;
        [Tooltip("Multiplies a corridor candidate's weight when the room it would attach to is ALSO a corridor - lower means corridors are less likely to chain directly into each other. When they do connect, that connection always forces a double door regardless of either side's connector type (see RoomGenerator.Generate).")]
        [SerializeField] private float corridorToCorridorWeightMultiplier = 0.25f;
        [SerializeField] private int maxPlacementAttemptsPerConnector = 12;

        [Header("Random")]
        [SerializeField] private bool useFixedSeed = false;
        [SerializeField] private int seed = 12345;

        [Header("Debug Draw")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private float cellSize = 1f;

        private LevelGrid _grid;
        private System.Random _rng;
        private readonly List<WorldConnectorRun> _openConnectors = new List<WorldConnectorRun>();

        public LevelGrid Grid => _grid;

        public void SetTemplates(List<RoomTemplate> templates) => roomTemplates = templates;

        [ContextMenu("Generate")]
        public void Generate()
        {
            _rng = useFixedSeed ? new System.Random(seed) : new System.Random();
            _grid = new LevelGrid();
            _openConnectors.Clear();

            _activePool = BuildTemplatePool();

            var spawnTemplate = _activePool.FirstOrDefault(t => t.HasTag("spawn"));
            if (spawnTemplate == null)
            {
                Debug.LogError("RoomGenerator: no room template tagged 'spawn' found.");
                return;
            }

            int corridorsPlaced = spawnTemplate.HasTag("corridor") ? 1 : 0;
            int roomsPlaced = 1;

            var spawnRoom = _grid.Stamp(spawnTemplate, Vector2Int.zero, 0);
            EnqueueOpenConnectors(spawnRoom);

            int safety = 0;
            while (_openConnectors.Count > 0 && roomsPlaced < desiredRoomCount && safety < desiredRoomCount * 50)
            {
                safety++;
                int idx = _rng.Next(_openConnectors.Count);
                var targetRun = _openConnectors[idx];
                _openConnectors.RemoveAt(idx);

                if (targetRun.state != ConnectorState.Open) continue;

                var ownerRoom = _grid.PlacedRooms.First(r => r.id == targetRun.ownerRoomId);
                bool wantsMore = ownerRoom.ResolvedConnectionCount < ownerRoom.template.desiredConnections;
                float chance = wantsMore ? ownerRoom.template.chanceToConnectWhenBelowTarget : ownerRoom.template.extraConnectionChance;

                if (_rng.NextDouble() > chance)
                {
                    SealRun(targetRun);
                    continue;
                }

                bool preferCorridor = corridorsPlaced < minCorridors;
                bool ownerIsCorridor = ownerRoom.template.HasTag("corridor");
                var placement = TryFindPlacement(targetRun, preferCorridor, ownerIsCorridor);

                if (placement == null)
                {
                    SealRun(targetRun);
                    continue;
                }

                var newRoom = _grid.Stamp(placement.Value.template, placement.Value.origin, placement.Value.rotationDeg);
                roomsPlaced++;
                if (placement.Value.template.HasTag("corridor")) corridorsPlaced++;

                bool corridorToCorridor = ownerIsCorridor && placement.Value.template.HasTag("corridor");
                _grid.ResolveConnection(placement.Value.overlapCells, targetRun.type, placement.Value.candidateRunType, _rng,
                    forceDoubleOverride: corridorToCorridor);

                targetRun.state = ConnectorState.Connected;
                targetRun.connectedToRoomId = newRoom.id;

                var overlapSet = new HashSet<Vector2Int>(placement.Value.overlapCells);
                foreach (var run in newRoom.connectorRuns)
                {
                    bool isUsedRun = run.cells.Count == overlapSet.Count && run.cells.All(c => overlapSet.Contains(c));
                    if (isUsedRun)
                    {
                        run.state = ConnectorState.Connected;
                        run.connectedToRoomId = ownerRoom.id;
                    }
                    else _openConnectors.Add(run);
                }
            }

            foreach (var run in _openConnectors)
                SealRun(run);
            _openConnectors.Clear();

            PerformReconnectionPass();
        }

        private List<RoomTemplate> BuildTemplatePool()
        {
            if (!autoLoadFromRoomLibrary) return roomTemplates;

            var pool = RoomLibraryLoader.LoadAll();
            if (roomTemplates != null && roomTemplates.Count > 0)
                pool.AddRange(roomTemplates); // manually-assigned templates (e.g. a test bootstrap) still work alongside the library
            return pool;
        }

        /// <summary>
        /// After the main growth loop, every room gets one independent chance-roll (its own
        /// reconnectionChance) per room it's already connected to, to try opening a second
        /// door to that same neighbor using leftover connector space. Each room's roll uses
        /// its own chance, so this is evaluated once per (room, connectedNeighbor) pair in
        /// each direction - a room with a high reconnectionChance will reach out to its
        /// neighbors more often than one with a low chance, independent of what its
        /// neighbors are configured with.
        /// </summary>
        private void PerformReconnectionPass()
        {
            var pairs = new List<(PlacedRoom room, int neighborId)>();
            foreach (var room in _grid.PlacedRooms)
            {
                var neighborIds = room.connectorRuns
                    .Where(r => r.state == ConnectorState.Connected)
                    .Select(r => r.connectedToRoomId)
                    .Distinct();
                foreach (var neighborId in neighborIds)
                    pairs.Add((room, neighborId));
            }

            var roomsById = _grid.PlacedRooms.ToDictionary(r => r.id);

            foreach (var (room, neighborId) in pairs)
            {
                if (_rng.NextDouble() > room.template.reconnectionChance) continue;
                if (!roomsById.TryGetValue(neighborId, out var neighbor)) continue;

                TryAddReconnection(room, neighbor);
            }
        }

        /// <summary>
        /// Looks for leftover space between two already-connected rooms: any cell that both
        /// rooms independently flagged as a connector (regardless of which specific runs
        /// were used for their original door), that's still plain Wall, and that wouldn't
        /// end up touching an existing door anywhere. Adds at most one extra door per call.
        /// </summary>
        private bool TryAddReconnection(PlacedRoom a, PlacedRoom b)
        {
            foreach (var runA in a.connectorRuns)
            {
                foreach (var runB in b.connectorRuns)
                {
                    var bSet = new HashSet<Vector2Int>(runB.cells);
                    var overlapOrdered = runA.cells.Where(c => bSet.Contains(c)).ToList();
                    if (overlapOrdered.Count == 0) continue;

                    foreach (var segment in SplitIntoContiguousSegments(overlapOrdered))
                    {
                        var eligible = segment.Where(c =>
                            _grid.GetCell(c).normal == NormalType.Wall &&
                            !_grid.IsAdjacentToDoor(c)).ToList();

                        foreach (var subSegment in SplitIntoContiguousSegments(eligible))
                        {
                            if (subSegment.Count == 0) continue;

                            _grid.ResolveConnection(subSegment, runA.type, runB.type, _rng,
                                overrideDoubleChance: a.template.reconnectionDoubleChance,
                                forceDoubleOverride: a.template.HasTag("corridor") && b.template.HasTag("corridor"));
                            return true; // one extra door per successful roll
                        }
                    }
                }
            }
            return false;
        }

        private static List<List<Vector2Int>> SplitIntoContiguousSegments(List<Vector2Int> orderedCells)
        {
            var segments = new List<List<Vector2Int>>();
            List<Vector2Int> current = null;
            Vector2Int? prev = null;

            foreach (var c in orderedCells)
            {
                bool adjacent = prev.HasValue && Mathf.Abs(c.x - prev.Value.x) + Mathf.Abs(c.y - prev.Value.y) == 1;
                if (current == null || !adjacent)
                {
                    current = new List<Vector2Int>();
                    segments.Add(current);
                }
                current.Add(c);
                prev = c;
            }
            return segments;
        }

        private void EnqueueOpenConnectors(PlacedRoom room)
        {
            foreach (var run in room.connectorRuns)
                _openConnectors.Add(run);
        }

        private void SealRun(WorldConnectorRun run)
        {
            if (run.state != ConnectorState.Open) return;
            run.state = ConnectorState.Sealed;
            foreach (var c in run.cells)
                _grid.SetNormal(c, NormalType.Wall);
        }

        private struct Placement
        {
            public RoomTemplate template;
            public Vector2Int origin;
            public int rotationDeg;
            public List<Vector2Int> overlapCells;
            public ConnectorType candidateRunType;
        }

        private Placement? TryFindPlacement(WorldConnectorRun targetRun, bool preferCorridor, bool ownerIsCorridor)
        {
            var candidates = _activePool.Where(t => !t.HasTag("spawn")).ToList();
            if (candidates.Count == 0) return null;

            int attempts = 0;
            while (attempts < maxPlacementAttemptsPerConnector)
            {
                attempts++;
                var candidateTemplate = PickWeighted(candidates, preferCorridor, ownerIsCorridor);
                int rotationDeg = _rng.Next(0, 4) * 90;

                var localRuns = RoomTemplateUtility.FindConnectorRuns(candidateTemplate)
                    .Where(r => r.cells.Count <= targetRun.cells.Count)
                    .OrderBy(_ => _rng.Next())
                    .ToList();

                foreach (var candidateRun in localRuns)
                {
                    int maxOffset = targetRun.cells.Count - candidateRun.cells.Count;
                    int offset = maxOffset > 0 ? _rng.Next(0, maxOffset + 1) : 0;
                    var overlapCells = targetRun.cells.GetRange(offset, candidateRun.cells.Count);

                    Vector2Int origin;
                    bool solved = TrySolveTransform(candidateTemplate, candidateRun.cells, overlapCells, rotationDeg, out origin)
                        || TrySolveTransform(candidateTemplate, candidateRun.cells, ReverseCopy(overlapCells), rotationDeg, out origin);

                    if (!solved) continue;

                    if (!_grid.CanPlace(candidateTemplate, origin, rotationDeg)) continue;

                    return new Placement
                    {
                        template = candidateTemplate,
                        origin = origin,
                        rotationDeg = rotationDeg,
                        overlapCells = overlapCells,
                        candidateRunType = candidateRun.type
                    };
                }
            }

            return null;
        }

        private bool TrySolveTransform(RoomTemplate t, List<Vector2Int> localCells, List<Vector2Int> worldTargetCells, int rotationDeg, out Vector2Int origin)
        {
            var first = localCells[0];
            RoomTemplateUtility.RotateCell(first.x, first.y, t.width, t.height, rotationDeg, out int rx, out int ry);
            origin = worldTargetCells[0] - new Vector2Int(rx, ry);

            for (int i = 0; i < localCells.Count; i++)
            {
                var world = RoomTemplateUtility.LocalToWorld(localCells[i].x, localCells[i].y, t.width, t.height, rotationDeg, origin);
                if (world != worldTargetCells[i]) return false;
            }
            return true;
        }

        private static List<Vector2Int> ReverseCopy(List<Vector2Int> input)
        {
            var copy = new List<Vector2Int>(input);
            copy.Reverse();
            return copy;
        }

        private RoomTemplate PickWeighted(List<RoomTemplate> candidates, bool preferCorridor, bool ownerIsCorridor)
        {
            float total = 0f;
            var weights = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                float w = Mathf.Max(0.0001f, candidates[i].selectionWeight);
                if (preferCorridor && candidates[i].HasTag("corridor")) w *= corridorWeightMultiplierBeforeMinMet;
                if (ownerIsCorridor && candidates[i].HasTag("corridor")) w *= corridorToCorridorWeightMultiplier;
                weights[i] = w;
                total += w;
            }

            double roll = _rng.NextDouble() * total;
            double cumulative = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative) return candidates[i];
            }
            return candidates[candidates.Count - 1];
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos || _grid == null) return;

            foreach (var room in _grid.PlacedRooms)
            {
                for (int y = 0; y < room.template.height; y++)
                {
                    for (int x = 0; x < room.template.width; x++)
                    {
                        var world = RoomTemplateUtility.LocalToWorld(x, y, room.template.width, room.template.height, room.rotationDeg, room.origin);
                        DrawCell(world, _grid.GetCell(world));
                    }
                }

                foreach (var run in room.connectorRuns)
                {
                    if (run.state != ConnectorState.Open) continue;
                    Gizmos.color = new Color(1f, 1f, 0f, 0.7f);
                    foreach (var c in run.cells)
                        Gizmos.DrawWireCube(CellToWorldPos(c), new Vector3(cellSize, cellSize, 0.5f) * 0.9f);
                }
            }
        }

        private void DrawCell(Vector2Int cell, LevelCell data)
        {
            Vector3 pos = CellToWorldPos(cell);

            if (data.floor != FloorType.Void)
            {
                Gizmos.color = data.floor == FloorType.Water
                    ? new Color(0.2f, 0.4f, 0.9f)
                    : new Color(0.55f, 0.55f, 0.55f);
                Gizmos.DrawCube(pos, new Vector3(cellSize * 0.98f, cellSize * 0.98f, 0.05f));
            }

            switch (data.normal)
            {
                case NormalType.Wall:
                    Gizmos.color = new Color(0.15f, 0.15f, 0.15f);
                    Gizmos.DrawCube(pos, new Vector3(cellSize * 0.9f, cellSize * 0.9f, 0.3f));
                    break;
                case NormalType.Door:
                    Gizmos.color = new Color(0.65f, 0.4f, 0.1f);
                    Gizmos.DrawCube(pos, new Vector3(cellSize * 0.9f, cellSize * 0.9f, 0.3f));
                    break;
            }
        }

        private Vector3 CellToWorldPos(Vector2Int cell) => transform.position + new Vector3(cell.x * cellSize, cell.y * cellSize, 0f);
    }
}

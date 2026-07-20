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
        [Tooltip("Boosts a corridor candidate's weight for ANY connector while the level still has fewer than minCorridors corridors placed - guarantees the minimum gets reached.")]
        [SerializeField] private float corridorWeightMultiplierBeforeMinMet = 2.5f;
        [Tooltip("Multiplies a corridor candidate's weight for ANY connector once minCorridors has already been reached - keep this under 1 so corridors get rarer (not just neutral) once the minimum is satisfied.")]
        [SerializeField] private float corridorWeightMultiplierAfterMinMet = 0.35f;
        [Tooltip("On top of the above, multiplies a corridor candidate's weight specifically when the room it would attach to is ALSO a corridor - lower means corridors are less likely to chain directly into each other. This dampener used to get canceled out during the early 'need more corridors' phase (see PickWeighted) - that's fixed now, so this value applies consistently throughout generation, not just after minCorridors is met. When two corridors DO connect (primary or reconnection), that connection always forces a double door regardless of either side's connector type - see RoomGenerator.Generate.")]
        [SerializeField] private float corridorToCorridorWeightMultiplier = 0.05f;
        [Tooltip("Hard cap: once a corridor chain reaches this many corridors in a row, further corridor-to-corridor growth off the END of that chain is blocked outright (weight forced to zero) rather than just made unlikely. 1 means a corridor may attach to one other corridor, but that second corridor can never chain into a third.")]
        [SerializeField] private int maxConsecutiveCorridors = 1;
        [SerializeField] private int maxPlacementAttemptsPerConnector = 12;
        [Tooltip("Once roomsPlaced reaches desiredRoomCount, each further connect-roll's chance gets multiplied by this (compounding) instead of generation stopping outright - growth tapers off naturally as rooms finish wanting more connections, rather than getting cut off mid-shape with a wall of abrupt dead ends. desiredRoomCount is a target to wind down around, not a hard cap - the level WILL go over it.")]
        [Range(0f, 1f)]
        [SerializeField] private float overflowGrowthDamping = 0.6f;

        [Header("Random")]
        [SerializeField] private bool useFixedSeed = false;
        [SerializeField] private int seed = 12345;

        [Header("Connectivity Safety Net")]
        [Tooltip("If true, any pair of placed rooms whose connector-flagged cells end up coinciding (touching walls) but have zero doors between them get one added automatically after generation. Without this, rooms can end up merged wall-to-wall with no way through - which happens whenever two branches of generation grow toward each other and happen to touch, not just via the deliberate parent/child connections the main loop makes.")]
        [SerializeField] private bool guaranteeDoorsOnConnectorOverlap = true;

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
            spawnRoom.corridorChainDepth = spawnTemplate.HasTag("corridor") ? 1 : 0;
            EnqueueOpenConnectors(spawnRoom);

            int safety = 0;
            while (_openConnectors.Count > 0 && safety < desiredRoomCount * 100)
            {
                safety++;
                int idx = _rng.Next(_openConnectors.Count);
                var targetRun = _openConnectors[idx];
                _openConnectors.RemoveAt(idx);

                if (targetRun.state != ConnectorState.Open) continue;

                var ownerRoom = _grid.PlacedRooms.First(r => r.id == targetRun.ownerRoomId);
                bool wantsMore = ownerRoom.ResolvedConnectionCount < ownerRoom.template.desiredConnections;
                float chance = wantsMore ? ownerRoom.template.chanceToConnectWhenBelowTarget : ownerRoom.template.extraConnectionChance;

                if (roomsPlaced >= desiredRoomCount)
                {
                    int overflow = roomsPlaced - desiredRoomCount + 1;
                    chance *= Mathf.Pow(overflowGrowthDamping, overflow);
                }

                if (_rng.NextDouble() > chance)
                {
                    SealRun(targetRun);
                    continue;
                }

                bool preferCorridor = corridorsPlaced < minCorridors;
                var placement = TryFindPlacement(targetRun, preferCorridor, ownerRoom.corridorChainDepth);

                if (placement == null)
                {
                    SealRun(targetRun);
                    continue;
                }

                var newRoom = _grid.Stamp(placement.Value.template, placement.Value.origin, placement.Value.rotationDeg);
                bool newIsCorridor = placement.Value.template.HasTag("corridor");
                newRoom.corridorChainDepth = newIsCorridor
                    ? (ownerRoom.template.HasTag("corridor") ? ownerRoom.corridorChainDepth + 1 : 1)
                    : 0;
                roomsPlaced++;
                if (newIsCorridor) corridorsPlaced++;

                bool involvesCorridor = ownerRoom.template.HasTag("corridor") || newIsCorridor;
                _grid.ResolveConnection(placement.Value.overlapCells, targetRun.type, placement.Value.candidateRunType, _rng,
                    forceDoubleOverride: involvesCorridor);

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

            ReviveDeadCorridorEnds();
            if (guaranteeDoorsOnConnectorOverlap) ResolveOrphanedConnectorOverlaps();
            PerformReconnectionPass();

            Debug.Log($"RoomGenerator: generated {roomsPlaced} room(s) (target was {desiredRoomCount}).");
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
                if (!roomsById.TryGetValue(neighborId, out var neighbor)) continue;
                TryResolveConnection(room, neighbor, chance: room.template.reconnectionChance);
            }
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

        /// <summary>
        /// Two rooms can end up sharing wall cells purely as a side effect of geometry -
        /// two branches of generation growing toward each other and happening to touch -
        /// without ever going through the deliberate parent/child match in the main loop.
        /// If that touch happens to land on cells BOTH rooms flagged as connectors, that
        /// reads as an intentional connection that never got resolved into a door. This
        /// pass finds every such pair (zero doors between them despite overlapping
        /// connector cells) and guarantees exactly one door - unlike the reconnection pass,
        /// this isn't chance-based, since leaving two rooms silently wall-merged with no way
        /// through is a correctness problem, not a variety knob.
        /// </summary>
        private void ResolveOrphanedConnectorOverlaps()
        {
            var rooms = _grid.PlacedRooms;
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    var a = rooms[i];
                    var b = rooms[j];

                    bool alreadyConnected = a.connectorRuns.Any(r => r.state == ConnectorState.Connected && r.connectedToRoomId == b.id);
                    if (alreadyConnected) continue;

                    TryResolveConnection(a, b, chance: null); // null = guaranteed, not rolled
                }
            }
        }

        /// <summary>
        /// Shared implementation for both the orphan-resolution pass (chance == null,
        /// always connects if eligible space exists) and reconnections between already
        /// - connected pairs (chance != null, rolled per call site). Finds the first
        /// eligible segment of overlapping connector cells between two rooms - still plain
        /// Wall, not touching an existing door anywhere - and resolves it into a door.
        /// </summary>
        private bool TryResolveConnection(PlacedRoom a, PlacedRoom b, float? chance)
        {
            if (chance.HasValue && _rng.NextDouble() > chance.Value) return false;

            bool bothCorridors = a.template.HasTag("corridor") && b.template.HasTag("corridor");
            bool involvesCorridor = a.template.HasTag("corridor") || b.template.HasTag("corridor");

            foreach (var runA in a.connectorRuns)
            {
                // Corridor-to-corridor may ONLY happen through a run BOTH sides flagged
                // AlwaysDouble - their long Normal/Restricted sides are for connecting to
                // rooms, not chaining hallway-to-hallway.
                if (bothCorridors && runA.type != ConnectorType.AlwaysDouble) continue;

                foreach (var runB in b.connectorRuns)
                {
                    if (bothCorridors && runB.type != ConnectorType.AlwaysDouble) continue;

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
                                overrideDoubleChance: chance.HasValue ? a.template.reconnectionDoubleChance : (float?)null,
                                forceDoubleOverride: involvesCorridor);

                            runA.state = ConnectorState.Connected;
                            runA.connectedToRoomId = b.id;
                            runB.state = ConnectorState.Connected;
                            runB.connectedToRoomId = a.id;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// A corridor's AlwaysDouble connectors are its "this is meant to lead somewhere
        /// important" ends. If one of them failed to connect during main growth (sealed,
        /// not connected) but there's actually still room to fit something there, that's a
        /// dead end that shouldn't exist - the main loop can fail to find a placement for
        /// reasons that have nothing to do with "there's no space" (a bad chance roll, or
        /// exhausting its attempt budget on a crowded pool). This pass gives every such
        /// sealed AlwaysDouble end one more, corridor-excluded try: if a normal room can fit
        /// there, place it as a terminal room (its OTHER connectors get sealed immediately,
        /// not grown further - this is a dead-end patch, not a new growth branch). Not
        /// every one of these will find space, and that's fine - some corridor ends are
        /// just surrounded by already-placed rooms with nothing left to attach.
        /// </summary>
        private void ReviveDeadCorridorEnds()
        {
            var deadEnds = _grid.PlacedRooms
                .Where(r => r.template.HasTag("corridor"))
                .SelectMany(r => r.connectorRuns.Select(run => (room: r, run: run)))
                .Where(x => x.run.type == ConnectorType.AlwaysDouble && x.run.state == ConnectorState.Sealed)
                .ToList();

            foreach (var (room, run) in deadEnds)
            {
                var placement = TryFindPlacement(run, preferCorridor: false, room.corridorChainDepth, excludeCorridorCandidates: true);
                if (placement == null) continue; // genuinely no space here - stays a dead end

                var newRoom = _grid.Stamp(placement.Value.template, placement.Value.origin, placement.Value.rotationDeg);
                newRoom.corridorChainDepth = 0; // never a corridor - excludeCorridorCandidates guarantees this

                _grid.ResolveConnection(placement.Value.overlapCells, run.type, placement.Value.candidateRunType, _rng,
                    forceDoubleOverride: true); // AlwaysDouble end - always try for the double it was built for

                run.state = ConnectorState.Connected;
                run.connectedToRoomId = newRoom.id;

                var overlapSet = new HashSet<Vector2Int>(placement.Value.overlapCells);
                foreach (var newRun in newRoom.connectorRuns)
                {
                    bool isUsedRun = newRun.cells.Count == overlapSet.Count && newRun.cells.All(c => overlapSet.Contains(c));
                    if (isUsedRun)
                    {
                        newRun.state = ConnectorState.Connected;
                        newRun.connectedToRoomId = room.id;
                    }
                    else
                    {
                        SealRun(newRun); // terminal room - the safety-net/reconnection passes can still pick up further coincidental connections for it
                    }
                }
            }
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

        private Placement? TryFindPlacement(WorldConnectorRun targetRun, bool preferCorridor, int ownerCorridorChainDepth, bool excludeCorridorCandidates = false)
        {
            bool ownerIsCorridor = ownerCorridorChainDepth > 0;

            // Corridor-to-corridor may ONLY happen through a run BOTH sides flagged
            // AlwaysDouble. If the owner is a corridor and this particular target run isn't
            // AlwaysDouble, don't even consider corridor candidates here - only normal
            // rooms remain eligible for this connector.
            bool corridorCandidatesAllowed = !excludeCorridorCandidates && (!ownerIsCorridor || targetRun.type == ConnectorType.AlwaysDouble);

            var candidates = _activePool.Where(t => !t.HasTag("spawn")).ToList();
            if (!corridorCandidatesAllowed)
                candidates = candidates.Where(t => !t.HasTag("corridor")).ToList();
            if (candidates.Count == 0) return null;

            int attempts = 0;
            while (attempts < maxPlacementAttemptsPerConnector)
            {
                attempts++;
                var candidateTemplate = PickWeighted(candidates, preferCorridor, ownerCorridorChainDepth);
                int rotationDeg = _rng.Next(0, 4) * 90;

                bool bothCorridors = ownerIsCorridor && candidateTemplate.HasTag("corridor");

                var localRuns = RoomTemplateUtility.FindConnectorRuns(candidateTemplate)
                    .Where(r => r.cells.Count <= targetRun.cells.Count)
                    .Where(r => !bothCorridors || r.type == ConnectorType.AlwaysDouble)
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

        private RoomTemplate PickWeighted(List<RoomTemplate> candidates, bool preferCorridor, int ownerCorridorChainDepth)
        {
            float total = 0f;
            var weights = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                bool candidateIsCorridor = candidates[i].HasTag("corridor");
                float w = Mathf.Max(0.0001f, candidates[i].selectionWeight);

                if (candidateIsCorridor)
                {
                    bool ownerIsCorridor = ownerCorridorChainDepth > 0;

                    // The "we still need more corridors" boost is meant to recruit corridors
                    // off of NORMAL rooms - it should never also boost a corridor picking
                    // another corridor, or it cancels out the dampener below and corridors
                    // keep chaining right through the "early phase" of generation.
                    w *= (preferCorridor && !ownerIsCorridor) ? corridorWeightMultiplierBeforeMinMet : corridorWeightMultiplierAfterMinMet;

                    if (ownerIsCorridor)
                    {
                        // Owner is itself a corridor - this would extend the chain by one.
                        w = ownerCorridorChainDepth >= maxConsecutiveCorridors ? 0f : w * corridorToCorridorWeightMultiplier;
                    }
                }

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

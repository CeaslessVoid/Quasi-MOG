# RoomGen

Unity 2D top-down project. Procedurally assembles a level from hand-built room templates. Singleplayer and LAN multiplayer (Netcode for GameObjects).

## Rules

1. README stays concise: enough detail, straight to the point.
2. No inline code comments. Code explains itself through naming; README explains the rest. README is updated whenever code changes.
3. Performance is a must. Prefer fewer files and a smaller codebase.
4. All wiring and setup instructions live in `Setup.md`, covering only the current patch/cycle.
5. All new or changed code ships in a zip containing only those files, laid out in the existing project structure so it can be dragged in and replaced.
6. Deleting files is fine. Keep naming conventions clear.

## Scenes

| Scene | Purpose |
|---|---|
| MainMenu | Root/Tutorial/Play/Multiplayer panels, save slots, LAN lobby, links to Room Builder and Generator Showcase. |
| Game | Builds and renders the generated level, spawns test player entities. |
| RoomBuilder | Tool for authoring room templates. Included in builds; reachable from MainMenu and the showcase. |
| GeneratorShowcase | Interactive generator: options panel on the left, live level on the right. |

**MainMenu objects:** `NetworkManager` (NetworkManager, UnityTransport, PersistentNetworkManager), `MainMenuController`, `LanRoomDiscovery`, `SaveSlotSelectController`, `Canvas` (all panels, plus the Room Builder and Generator Showcase buttons, each with `SceneLoadButton`).
**Game objects:** `Grid`, `GameManager`, `LevelViewerBootstrap`.
**RoomBuilder objects:** `GameManager`, `RoomBuilder` (RoomBuilderController + RoomBuilderVisuals), `RoomBuilderUIController`, an object with `SceneLoadButton` (back to menu).
**GeneratorShowcase objects:** one empty object with `GeneratorShowcase`. Camera, level view and UI are created at runtime.

Singleplayer never starts Netcode, never opens a socket, and needs no connection. LAN sockets open only while the Multiplayer panel is shown.

## Flow

1. `MainMenuController.Awake` ensures `GameManager` exists (`DontDestroyOnLoad`, warms `DefDatabase`).
2. Singleplayer: `PlaySlotsPanelController` -> `SaveSlotSelectController` -> `GameManager.ConfigureSingleplayer*` -> load `Game`.
3. `LevelViewerBootstrap.Start` branches: multiplayer with a NetworkManager -> `StartMultiplayer`, else `StartSingleplayer` (camera, `RoomGenerator.Generate`, `LevelVisuals.Rebuild`, spawn one test player, center camera).

## Code Map

Namespaces: `RoomGen` (generation, builder, level view), `GameDefs` (data defs), `Entities`, `Networking`, `Save`, `UI.MainMenu`, `RoomGen.UI`, `Util`. `GameManager` is global.

### Defs (`Assets/Scripts/Defs`)

ScriptableObject data, looked up by `defName` string through `DefDatabase`.

- `Def`: base. defName, displayName, icon, primary/secondary tint, optional mask texture.
- `DefDatabase`: loaded from `Resources/DefDatabase`. `Get<T>`, `TryGet<T>`, `All<T>` (cached per type). Editor context menu "Scan Assets/Defs" fills it from `Assets/Defs`.
- `SurfaceDef` (sprite) -> `FloorDef`, `LiquidDef`.
- `BlockerDef` (bullet block chance, blocks vision, max HP, physics material) -> `WallDef`, `DoorDef`, `PropDef`.
- `WallTextureElement`: 16 sprites for a wall autotile. `WallDef` references one.
- `DoorDef`: north/east sprites, double-door flag, single-door fallback def.
- `PropDef`: category (Normal, Decorative, Wall), use categories (flags, for builder filtering), width/height, interaction type, north/south/east sprites and masks (west = east flipped).
- `EntityDef`: max health, limbs, world sprite. `LimbDef`: optional child limb.
- `DefVisualUtility`: generated magenta checker "missing" sprite and solid sprite.
- `DefTintRenderer`: applies primary/secondary tint and mask through `Resources/Shaders/MaskedTintSprite` (URP, no keywords, so a single shader variant) using one shared material and a property block.

### Core / LevelGenerator

- `RoomGenTypes`: enums (`FloorType`, `NormalType`, `ConnectorType`, `ConnectorState`, `DoorSize`), `PropPlacement`, `LevelCell`, `WorldConnectorRun`, `PlacedProp`, `PlacedRoom`.
- `RoomData`: serializable room: size (min 3x3), tags, per-cell layers (floor, normal, connector, wall/door/floor def names), props, preferred door defs, generation weights.
- `RoomTemplate`: ScriptableObject wrapper around `RoomData` with a cached connector-run list. Built at runtime from JSON via `FromRoomData`.
- `RoomTemplateUtility`: rotation math, connector eligibility, connector run detection.
- `PropPlacementUtility` / `PropPlacementValidator`: footprint and facing math, render bounds, placement rules.
- `LevelGrid.cs`: `ILevelCellSource` + query extensions (wall/vision blocking, door orientation, door partner, line of sight), `LevelGridBuilder` (mutable, used during generation), `LevelGrid` (dense baked array), `LevelGridBaker`.
- `RoomGenerator`: the generator (see Level Generation). Also holds `GenerationOptions` and `TagRequirement`.

### Core / RoomBuilder

- `RoomLibrary`: rooms are JSON files. Editor reads and writes `StreamingAssets/Rooms`. Builds read shipped `StreamingAssets/Rooms` plus user rooms in `persistentDataPath/Rooms` (same file name overrides shipped) and save to the latter. Loads are normalized (missing layers allocated) and unreadable files are logged and skipped. `CollectTags` lists distinct type or zone tags.
- `RoomBuilderController`: paint/erase logic, previews, prop placement, room I/O, tag/door/weight setters.

### View

- `Levels/RoomVisualsBase`: lazy init, cell size. `Levels/WallAtlas`: 4-bit neighbor bitmask (N=1,E=2,S=4,W=8) to sprite index, door orientation.
- `Levels/LevelVisuals`: renders a `LevelGrid`. Floor tilemap; one wall tilemap per physics material with merged composite collider; doors and props as GameObjects. Sorting: floor 0, door 2, prop 3, decorative prop 4, walls 5+.
- `Levels/DoorInstance`: slides two leaves, toggles collider. `Open`/`Close` exist but nothing calls them yet.
- `Levels/PropInstance`: holds `PropDef`, HP, static set of interactable props.
- `Levels/LevelViewerBootstrap`: see Flow.
- `RoomBuilder/RoomBuilderVisuals`: per-cell renderers, grid lines, connector overlay, placement preview ghost.
- `RoomBuilder/RoomBuilderUIController`: bottom tabs (Floor, Normal, Prop, Connector), side panel with search and category tabs, auto-hide by pointer position.
- `RoomBuilder/RoomBuilderTopBarController`: top bar toggling one window panel at a time. Panels derive `TopBarWindowPanel`: `RoomIOPanel` (create/save/load), `RoomTagsPanel`, `RoomDoorDefaultsPanel`, `RoomWeightsPanel`.
- `RoomBuilder/DefListPanel`, `DefListItemView`, `CategoryTabBar`, `SimpleButtonListView`: pooled UI lists and tabs. `SimpleButtonListView` builds a plain button at runtime when no Item Prefab is assigned, and tints items flagged `Selected`. `RoomDoorDefaultsPanel` lists only single doors on the left and only double doors on the right, highlights the room's current choice, and has a "Default" entry that clears it.
- `Showcase/GeneratorShowcase`: runtime IMGUI options panel. Drives `RoomGenerator.Generate(GenerationOptions)`, rebuilds the view, fits the camera, shows counts, seed, timings and unmet requirements. Changes that alter the panel layout are queued and applied in `Update`.
- `SceneLoadButton`: loads a scene. On a Button it wires itself; without one it draws a corner button.
- `SimpleTopDownCameraController`: WASD pan, scroll zoom (2-60), blocked while typing in an input field. `Spawn(size, reservedPanelWidth)` finds or creates the orthographic camera and attaches the controller; zoom is ignored over the reserved left-side pixels.
- `MainMenu/*`: panel switching, save slot UI, multiplayer browse and lobby, tutorial placeholder.

### Entities

- `LivingEntity`: def, health, limbs, inventory, grid cell, world position `(cell + 0.5) * cellSize`.
- `PlayableEntity`: adds character name, skill/implant id lists.
- `EntityVisuals`: creates body sprite and TMP name label at runtime. Local player name is blue.
- `EntitySpawner`: spawn cells are non-wall floor cells of the room tagged `spawn` (first room if none).
- `NetworkEntityLink`: replicates `EntitySpawnState` (def, name, owner, cell) from server and applies it on every client; centers camera on the local player.

### Network

- `NetworkGameLauncher`: StartHost (0.0.0.0:7777), StartClient, Shutdown.
- `PersistentNetworkManager`: keeps one NetworkManager across scenes.
- `LanRoomDiscovery`: UDP broadcast on port 47657 every 1s, rooms time out after 3s. Payload `ROOMGEN|<json RoomInfo>`.
- `RoomSession`: networked lobby player list (max 4), name submission by RPC, server loads `Game`.
- `NetworkedLevelSync`: server generates the level, serializes it, sends it to clients in 500-byte chunk RPCs (also to late joiners), and spawns one player entity per client. Clients deserialize and raise `OnLevelReady`.
- `LevelNetworkSerializer`: binary grid + placed rooms (templateId, origin, rotation) + props. Clients resolve templates locally by id, so the room library (shipped and user rooms) must match on every build.

### Save / Utils

- `SaveManager`: 3 slots, metadata only (name, last played) at `persistentDataPath/Saves/slot_N.json`. No gameplay state is saved yet.
- `FpsLimiter`: vSync off, 120 FPS cap. `InputFocusUtility`: true while a TMP input field or an external (IMGUI) field is focused.

## Level Generation

`RoomGenerator.Generate`:

1. Pool = all library rooms (optional) + assigned templates, filtered by the context. Start from the first template tagged `spawn`, stamped at origin.
2. Every placed room contributes its connector runs to an open list. Loop: pop a random open run, roll the room's `chanceToConnectWhenBelowTarget` (damped by `overflowGrowthDamping^n` once `desiredRoomCount` is reached), otherwise seal it.
3. `TryFindPlacement`: weighted template pick, random rotation, a candidate run no longer than the target run, solve the origin, accept only if overlap is wall-on-wall (`CanPlace`) and the map limits still hold. Up to `maxPlacementAttemptsPerConnector` tries.
4. `Stamp` writes cells (void/empty cells keep what was beneath), `ResolveConnection` turns the overlap into a door.
5. Post-pass: seal leftovers, `PlaceRequiredRooms`, `ReviveDeadCorridorEnds`, `ResolveOrphanedConnectorOverlaps` (adds doors between rooms whose connectors coincide), `ReplaceOrphanedDoubleDoors` (partnerless double doors fall back to single), then bake to `LevelGrid`.

**Connector runs:** straight lines of wall cells flagged as connectors that touch a non-wall neighbor. `Normal` and `AlwaysDouble` merge in one run. `Restricted` forces single doors, `AlwaysDouble` forces double.
**Door size:** 1 cell = single. 2 cells = double or single. 3+ cells = door chosen from interior cells only.
**Corridors (tag `corridor`):** `minCorridors` boosted weight until met, reduced after; corridor-to-corridor is heavily penalized, capped by `maxConsecutiveCorridors`, and always joined by a double door.
**Tags used by code:** `spawn`, `corridor`. Any other type tag can be required through `TagRequirement`.

## Using the Generator

```csharp
var generator = gameObject.AddComponent<RoomGenerator>();
var options = new GenerationOptions { roomCount = 30, maxHallwayLength = 2, useFixedSeed = true, seed = 42, maxMapWidth = 120 };
options.context["planet"] = "Mars";
options.requirements.Add(new TagRequirement { tag = "storage", count = 2 });
generator.Generate(options);
LevelGrid grid = generator.Grid;
```

`Generate()` with no options uses the component's inspector values. Same options and seed on the same room library reproduce the same level.

| Option | Effect |
|---|---|
| `roomCount` | Target room count. Growth is damped past it, so the final count can exceed it. Guaranteed rooms are added on top. |
| `minCorridors` | Hallway rooms favored until this many exist. |
| `maxHallwayLength` | Max hallway rooms chained in a row. |
| `maxMapWidth`, `maxMapHeight` | Hard cap on the level bounding box in cells. 0 = unlimited. |
| `useFixedSeed`, `seed` | Reproducible output. Otherwise a tick-based seed is used. |
| `context[axis] = value` | Context filter. Zone tags use `axis:value` (`planet:Mars`, `faction:Corp`, `outpost:Research`). A room is dropped only if it declares that axis and none of its values match. Rooms without that axis always fit. |
| `requirements` | Guaranteed room tags and counts (spawn excluded). Missing rooms are attached to sealed connectors after the main pass. |

After generating: `LastSeed`, `LastRoomCount`, `LastCorridorCount`, and `Unmet` (unsatisfied requirements, or a missing spawn room for the chosen context). A null `Grid` means no spawn room matched.

## Generator Showcase

Left panel: rooms, max hallway length, min hallways, max width/height, seed (Random, or type one), outpost context per axis (`<` `>` cycles Any and every value found in room zone tags), guaranteed rooms (tag and count), Generate and New Seed + Generate. Below: room/hallway counts, seed, map size, generate and build times in ms, and unmet requirements in red. WASD pans, scroll zooms over the map.

## Props

- Footprint: width extends along local +x, height along local -y, rotated by facing. Room rotation rotates facing. West uses the east sprite flipped.
- `Normal`: needs non-void floor and empty normal cell. `Wall`: must sit on a wall cell and renders one cell behind its facing. `Decorative`: may stack on another prop or sit on a wall that does not block vision. Only decorative stacking is allowed.
- Every prop gets a static box collider and a `PropInstance`.

## Room Builder

LMB paint, RMB erase, `R` rotates prop facing, WASD pan, scroll zoom, `Esc` closes panels. Rooms save as `<templateId>.json` (see `RoomLibrary` for the folder).

## Prefabs (`Assets/Prefabs`)

| Prefab | Contents | Used by |
|---|---|---|
| CategoryButton | Button, TMP text | `CategoryTabBar.tabButtonPrefab` (category filters) and every `SimpleButtonListView.itemPrefab` (room list, tag lists, door default lists) |
| ItemView | `DefListItemView`; Button with highlight, icon image, name text | `DefListPanel.itemPrefab` (click to select a def to place) |
| NetworkedLevelSync | NetworkObject, `NetworkedLevelSync` | Instantiated and spawned by the host in `LevelViewerBootstrap`; generates and syncs the level, spawns players |
| PlayerEntity | NetworkObject, `EntityVisuals`, `PlayableEntity`, `NetworkEntityLink` | `NetworkedLevelSync.playableEntityPrefab`; one per client (multiplayer only) |
| RoomSession | NetworkObject, `RoomSession` | `MultiplayerPanelController.roomSessionPrefab`; spawned by the host on room creation |
| PlayerLobbyView | `LobbyPlayerRowView`, text | One row per player in the lobby |
| RoomListing | `RoomListItemView`; Button with text, name text, count text | One row per discovered LAN room |

## Currently Unused

`ITurnActor`/`TakeTurn`, `Inventory`, limbs (`DetachLimb`), `skillIds`/`implantIds`, `CeilingCell`/`ceilingLayer`, `ConnectionResult`, `Edge`/`RotateEdge`, `zoneTags` in game logic outside the generator context filter, `desiredConnections`, `DoorInstance.Open/Close`, `PropInstance.ApplyDamage`, `HasLineOfSight`/`TraceLine`, `BlockerDef.ChanceToBlockBullet`, `GameManager.IsServerAuthority`, `PendingSaveName`/`IsNewGame` in the Game scene.

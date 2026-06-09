# Course Editor — Design Spec

**Date:** 2026-05-30  
**Status:** Approved  
**Branch:** `course-editor`  
**Engine:** Unity (URP, 3D greybox + billboard foliage)  
**Authoring surface:** Unity Editor only (Option A)

---

## 1. Vision

Build a **tile-style course editor inside Unity** so designers can mass-produce disc golf holes without hand-placing scene primitives. Each hole is stored as portable **JSON** (topology, elevation, hazards, foliage placements, tee/basket metadata). Visual appearance comes from **theme packs** that map stable archetype IDs to sprites and materials — enabling regional reskins (e.g. evergreen → Florida palms) without moving a single tree.

Holes target **150–550 yards** (137–503 m). The editor optimizes for **low memory and texture usage** via shared atlases, sparse data, and archetype indirection.

This spec covers the full course-editor system decomposed into phased delivery (P0–P5). P0 is the minimum shippable slice.

---

## 2. Design Decisions (Locked)

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Authoring surface | Unity Editor window + Scene overlay | Integrated playtest, reuses `GameplayArtCatalog`, matches existing workflow |
| Source of truth | JSON per hole | Easy share, diff, version; scenes are baked previews only |
| Grid resolution | **2 m tiles** default | 550 yd hole ≈ 250 tiles on long axis; tunable to 1 m for greens in P4 |
| World units | Meters (display yards in UI) | Aligns with `GreyboxScale` and physics |
| Terrain representation | Sparse tile layers + height grid | Small JSON; no per-hole textures |
| Foliage | Sparse placements referencing archetype IDs | Regional swap without editing courses |
| Visual assets | Theme packs (ScriptableObject) | One material/atlas per theme, not per hole |
| Runtime integration | `CourseBuilder` generates tagged colliders | Reuses `DiscLieGround`, `CourseLayout` bounds API, minimap |
| Lie types | Tee, Fairway, Rough, Green, OB, Water | `LieType` already defines OB; water is new penalty surface |
| Legacy scenes | `PrototypeFlat3` unchanged until migrated | No breaking change to current playtest hole |

---

## 3. Goals & Non-Goals

### Goals

- Paint fairway, rough, and green on a snapped grid in Scene view.
- Place trees, bushes, and other foliage props with rotation and scale.
- Support elevation editing with smooth playable ground mesh.
- Define OB and water hazard polygons with penalty-stroke behavior.
- Export/import hole JSON; share as text + optional theme pack reference.
- Swap entire environment look via theme pack (regional biomes).
- Drop new sprite PNGs into convention folders; editor/catalog picks them up.
- One-click playtest from editor without manual scene wiring.
- Validate holes (tee/basket on valid surfaces, reachable basket, bounds).

### Non-Goals (this spec)

- Standalone web or external tile editor.
- Multiplayer course sync or cloud hosting.
- Procedural hole generation (may come later).
- Full 18-hole routing UI (P5 adds course manifest only).
- Photoreal terrain or splat-map painting.
- In-editor disc flight tuning per hole (uses existing throw systems).

---

## 4. Scale & Performance Budget

| Metric | Target |
|--------|--------|
| Hole length | 150–550 yd (137–503 m) |
| Tile size | 2 m (configurable per project) |
| Typical grid | ~40 × 120 cells for a 250 yd hole |
| JSON file size | < 200 KB typical (sparse encoding) |
| Height grid | ≤ 250 × 80 floats ≈ 80 KB uncompressed |
| Foliage instances | ≤ 300 per hole (soft limit with editor warning) |
| Load + bake time | < 500 ms desktop for full hole |
| Runtime textures | 1 ground atlas + 1 foliage atlas per active theme |
| Draw calls (foliage) | Prefer GPU instancing or shared material batching |

---

## 5. Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Unity Course Editor (P0+)                 │
│  CourseEditorWindow │ Scene overlay │ Paint/Elevate/Hazard  │
└──────────────────────────┬──────────────────────────────────┘
                           │ read/write
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                     HoleData (JSON)                          │
│  schemaVersion │ tiles │ heights │ hazards │ placements     │
│  │ tee/basket/par │ themeId │ bounds                        │
└──────────────────────────┬──────────────────────────────────┘
                           │ load
                           ▼
┌─────────────────────────────────────────────────────────────┐
│              ThemePack (ScriptableObject)                    │
│  archetypeId → Sprite/Material │ ground atlas │ biome tag   │
└──────────────────────────┬──────────────────────────────────┘
                           │ resolve at bake
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                   CourseBuilder (runtime + editor)           │
│  mesh + colliders │ foliage instances │ hazard triggers     │
│  implements CourseLayout-compatible bounds/minimap API      │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  Existing gameplay: HoleSetup, DiscLieGround, MinimapUI,    │
│  ThrowController, lie banners, tree obstacles               │
└─────────────────────────────────────────────────────────────┘
```

### Module boundaries

| Module | Responsibility | Depends on |
|--------|----------------|------------|
| `HoleData` / schema | Serialize/deserialize, validate, migrate versions | None |
| `ThemePack` | Archetype → art resolution | `ProjectArtPaths`, `GameplayArtCatalog` |
| `CourseEditorWindow` | Authoring UX, tools, undo | `HoleData`, Unity Editor APIs |
| `CourseBuilder` | JSON + theme → scene geometry | `HoleData`, `ThemePack`, `DiscLieGround` tags |
| `HazardRules` | OB/water penalty logic | `ThrowController`, `LieType` |
| `CourseValidator` | Pre-playtest checks | `HoleData` |

Each module is testable independently: round-trip JSON tests, builder snapshot tests, validator unit tests.

---

## 6. Data Model

### 6.1 File layout (project)

```
Assets/
├── Data/
│   ├── Courses/
│   │   └── <CourseName>/
│   │       ├── course.manifest.json   # P5: hole list, metadata
│   │       └── hole_01.json
│   └── Themes/
│       ├── ThemePack_Temperate.asset
│       └── ThemePack_Florida.asset
├── Art/Environment/
│   ├── Course/Tiles/          # shared ground atlas source art
│   ├── Foliage/Sprites/       # drop-in sprites (TEX_Tree_*.png, etc.)
│   └── Themes/<Biome>/        # optional per-biome source art
```

Shared holes can be distributed as JSON files outside `Assets/`; import copies into `Data/Courses/`.

### 6.2 Hole JSON schema (v1)

```json
{
  "schemaVersion": 1,
  "id": "hole_03_dogleg_left",
  "name": "Hole 3 — Dogleg Left",
  "units": "meters",
  "tileSize": 2.0,
  "origin": [0.0, 0.0],
  "themeId": "temperate",
  "bounds": { "min": [0, 0], "max": [80, 240] },
  "elevation": {
    "width": 41,
    "height": 121,
    "heights": [0.0, 0.0, 0.15]
  },
  "surfaceTiles": [
    { "x": 10, "y": 40, "type": "fairway" },
    { "x": 11, "y": 40, "type": "fairway" }
  ],
  "hazards": [
    {
      "id": "water_1",
      "type": "water",
      "vertices": [[12, 55], [18, 55], [18, 62], [12, 62]]
    },
    {
      "id": "ob_left",
      "type": "ob",
      "vertices": [[0, 0], [0, 120], [-2, 120], [-2, 0]]
    }
  ],
  "placements": [
    { "archetype": "tree_round", "x": 24.5, "z": 88.0, "yaw": 15.0, "scale": 1.0 }
  ],
  "hole": {
    "tee": [4.0, 6.0],
    "basket": [72.0, 218.0],
    "par": 3,
    "circleRadiusFt": 33
  }
}
```

**Encoding notes:**

- `surfaceTiles`: sparse list in v1; migrate to RLE column runs if files grow (P4).
- `elevation.heights`: row-major, `(width × height)` floats; world Y = sampled height at tile corners (bilinear).
- `placements`: world XZ in meters (not tile indices) for sub-tile precision; Y from height sample.
- `hazards.vertices`: tile-space polygon (integer or half-tile); builder extrudes to mesh/trigger on bake.
- `themeId`: resolves to `ThemePack` asset; missing theme falls back to project default with warning.

### 6.3 Surface types

| Type | Gameplay tag | `LieType` | Notes |
|------|--------------|-----------|-------|
| `tee` | `Tee` | Tee | Painted or explicit tee marker |
| `fairway` | `Fairway` | Fairway | Default lie |
| `rough` | `Rough` | Rough | Lie penalty (existing flight modifiers) |
| `green` | `Green` | Green | Putting lie |
| `water` | `Water` | (hazard) | Penalty stroke; drop per rules |
| `ob` | `OB` | OB | Penalty stroke; re-tee or marked drop |

### 6.4 Archetype catalog (stable IDs)

Archetype IDs are **theme-independent**. Theme packs map them to art.

| Archetype ID | Category | Collider |
|--------------|----------|----------|
| `tree_round` | Tree | Capsule (existing `CourseTree` pattern) |
| `tree_tall` | Tree | Capsule |
| `bush_low` | Bush | Optional small capsule or none |
| `rock_small` | Prop | None (visual only) |
| `grass_clump` | Ground detail | None |

New archetypes are added to a project-wide registry (`FoliageArchetypeCatalog` ScriptableObject). Artists assign sprites per theme in `ThemePack`.

### 6.5 Course manifest (P5)

```json
{
  "schemaVersion": 1,
  "id": "lakeside_9",
  "name": "Lakeside 9",
  "themeId": "florida",
  "holes": [
    { "file": "hole_01.json", "number": 1 },
    { "file": "hole_02.json", "number": 2 }
  ],
  "author": "marku",
  "version": "1.0.0"
}
```

---

## 7. Theme Packs & Asset Pipeline

### 7.1 ThemePack ScriptableObject

```
ThemePack
├── id: "florida"
├── displayName: "Florida"
├── groundMaterial: MAT_CourseTiles_Florida
├── groundAtlas: TEX_CourseTiles_Florida_Atlas
├── foliageEntries[]
│   ├── archetypeId: "tree_round"
│   ├── sprite: TEX_Palm_01
│   ├── colliderProfile: TreeStandard
│   └── sortingOrder: 10
└── fallbackSprite: greybox square (dev only)
```

### 7.2 Artist drop-folder workflow

1. Add `TEX_<Feature>_<Variant>.png` under `Assets/Art/Environment/Foliage/Sprites/`.
2. Existing `FoliageSpriteImporter` applies import settings.
3. Open `ThemePack` asset; assign sprite to archetype slot (or run catalog builder to suggest new entries).
4. Add path to `ProjectArtPaths.cs` if introducing a new root folder.
5. Register runtime-facing entries in `GameplayArtCatalogBuilder` only for bootstrap/fallback sprites.

**Regional swap:** Duplicate `ThemePack_Temperate` → `ThemePack_Florida`, remap archetype sprites. All `hole_*.json` files referencing `themeId: "florida"` re-skin on next bake. Course placement data unchanged.

### 7.3 Memory rules

- **Never** embed texture paths per placement or per tile.
- **One** ground material per active theme (texture atlas UV per tile type).
- Foliage shares materials per archetype within a theme; enable instancing where possible.
- Builder destroys previous baked root (`BuiltCourse`) before rebake; no duplicate geometry.

---

## 8. Unity Editor UX

### 8.1 Window layout

**Menu:** `Disk Golf → Course Editor`

| Panel | Contents |
|-------|----------|
| Toolbar | New / Open / Save / Export JSON / Import JSON / Bake / Playtest |
| Tool palette | Paint, Erase, Elevate, Hazard, Place, Hole markers, Select |
| Layer/target | Surface type brush, hazard type, foliage archetype |
| Theme | Dropdown of loaded `ThemePack` assets (live preview swap) |
| Properties | Tile size (read-only v1), par, circle radius, hole name |
| Validation | Errors/warnings list with click-to-focus |
| Stats | Tile count, foliage count, yardage tee→basket, JSON size estimate |

### 8.2 Scene overlay

- Grid drawn in Scene view aligned to `origin` and `tileSize`.
- Tee→basket axis guide line with yard markers every 50 yd.
- Semi-transparent tile tint for active surface brush.
- Elevation: contour lines + brush falloff circle.
- Hazards: filled polygon preview (blue = water, red stripe = OB).
- Gizmos for tee (T), basket (B), placements (archetype icon).

### 8.3 Tool behaviors

| Tool | Input | Behavior |
|------|-------|----------|
| Paint | Click/drag | Sets surface tile type; shift = temp erase |
| Elevate | Click/drag | Adjusts height grid; alt = lower; smooth brush option |
| Hazard | Click vertices | Close polygon; snap to tile corners |
| Place | Click | Spawns placement at cursor XZ, Y from height; R = rotate |
| Hole | Click | Set tee or basket position (snap optional) |
| Playtest | Button | Bake → enter Play Mode with `HoleSetup` wired |

### 8.4 Undo

All paint, elevate, hazard, and placement operations register Unity `Undo` records.

### 8.5 Templates (P4)

Starter holes: straight par 3, dogleg L/R, island green — load as new `HoleData` presets.

---

## 9. Runtime Build Pipeline

### 9.1 CourseBuilder output hierarchy

```
BuiltCourse (root, generated)
├── Ground
│   ├── FairwayMesh   (tag: Fairway, MeshCollider)
│   ├── RoughMesh     (tag: Rough)
│   └── GreenMesh     (tag: Green)
├── Hazards
│   ├── Water_*       (tag: Water, trigger collider)
│   └── OB_*          (tag: OB, trigger collider)
├── Foliage
│   └── Trees/        (billboards + TreeObstacle where applicable)
├── TeePad            (tag: Tee)
└── Basket            (tag: Basket, existing prefab)
```

### 9.2 Ground mesh generation

1. Merge contiguous tiles of same surface type into regions (greedy or marching squares).
2. Sample elevation at region vertices from height grid.
3. Emit one mesh per surface type (or submesh) with shared theme material.
4. UVs map to atlas cells per tile type (fairway, rough, green).

### 9.3 Integration with existing systems

| Existing API | Editor-built course |
|--------------|---------------------|
| `CourseLayout.WorldBounds` | Computed from baked renderers + tee/basket |
| `CourseLayout.ComputeMinimapFraming` | Unchanged |
| `DiscLieGround.SampleLieType` | Extended for `Water` and `OB` tags |
| `HoleSetup` | Tee/basket transforms assigned at bake |
| `MinimapUI` tree markers | Enumerate `Foliage` placements with tree archetypes |
| `CourseTree` / `TreeObstacle` | Reused for tree archetype instances |

`CourseLayout` on `CourseElements` remains for legacy scenes. Editor-built holes use `BuiltCourse` with a shared `ICourseBounds` interface (or `CourseLayout` adapter component).

### 9.4 Hazard rules (P1)

| Hazard | On disc enter | Penalty | Lie after |
|--------|---------------|---------|-----------|
| Water | Trigger while disc grounded or flight end | +1 stroke | Nearest drop zone (builder-defined) or prior lie |
| OB | Trigger while grounded or flight end | +1 stroke | Re-tee or OB line drop (configurable per hole) |

Exact drop logic mirrors PDGA-style defaults; tunable per hole in JSON `hazardRules` block (P2).

---

## 10. Validation Rules

| Code | Severity | Rule |
|------|----------|------|
| `E001` | Error | Basket not placed |
| `E002` | Error | Tee not placed |
| `E003` | Error | No fairway tiles |
| `E004` | Error | Basket not on green or fairway |
| `W001` | Warning | Tee not on tee/fairway |
| `W002` | Warning | Foliage count > 300 |
| `W003` | Warning | Hazard polygon self-intersects |
| `W004` | Warning | Theme archetype missing sprite (uses fallback) |
| `W005` | Warning | Hole length outside 150–550 yd |

Playtest blocked on errors; warnings allowed with confirmation.

---

## 11. Phased Delivery

### P0 — Core loop (MVP)

- `HoleData` schema v1 + JSON import/export
- `CourseEditorWindow` with Paint tool (fairway/rough/green)
- Tee + basket placement; par field
- `CourseBuilder` flat mesh (no elevation)
- `ThemePack` with one theme, 3 archetypes
- Playtest button wires `HoleSetup`
- Unit tests: JSON round-trip, builder produces tagged colliders

**Exit criteria:** New par-3 from blank JSON to playable throw in < 30 min.

### P1 — Elevation & hazards

- Height grid + elevate tool + smooth
- Water and OB polygon tool
- `HazardRules` + `DiscLieGround` water/OB classification
- Minimap hazard tint

### P2 — Foliage at scale

- Place tool with archetype brush, rotate, scale
- `FoliageArchetypeCatalog`
- Collider profiles per archetype
- GPU instancing or batching pass

### P3 — Regional themes

- Multiple `ThemePack` assets
- Theme dropdown with live rebake preview
- Document artist workflow in `Assets/README.md`

### P4 — Production UX

- Validation panel, templates, yardage ruler
- Optional 1 m green sub-grid
- RLE tile compression for large holes
- JSON schema migration tooling

### P5 — Course packs

- `course.manifest.json`
- Load sequence of holes in session
- Export zip (JSON + theme id manifest) for sharing

---

## 12. Testing Strategy

### Edit Mode (automated)

- `HoleData` serialize/deserialize round-trip equality
- Schema version migration tests
- `CourseValidator` error/warning cases
- `CourseBuilder` produces expected tags and child counts from fixture JSON

### Play Mode (manual QA checklist)

- [ ] Painted lie types match banner text (fairway, rough, green)
- [ ] Water/OB adds penalty stroke and correct lie
- [ ] Disc snaps to elevated ground at tile seams
- [ ] Trees block flight; colliders match sprite footprint
- [ ] Minimap dots align with tee/basket/disc/trees
- [ ] Theme swap changes visuals only, not collision topology
- [ ] Export → import → bake produces identical topology hash

### Performance

- Profile 550 yd hole with 200 trees: frame time and memory vs baseline scene

---

## 13. Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| JSON/scene drift | JSON authoritative; scene objects are always generated |
| Large JSON files | Sparse tiles + RLE in P4; height grid sized to bounds only |
| Elevation seams | Shared vertex welding at tile borders; bilinear sampling |
| Theme missing archetype | Fallback sprite + validator warning; block export in strict mode |
| Breaking `PrototypeFlat3` | Legacy path untouched; editor uses separate `BuiltCourse` root |
| Scope creep | Strict phase gates; P0 ships before elevation |

---

## 14. Open Items (post-P0)

- Exact water drop algorithm (nearest land vs lateral hazard line)
- Whether green requires 1 m sub-grid in P0 or P4 only
- OB re-tee vs drop zone default for casual vs pro rules
- Multi-hole scene streaming vs per-hole scene load

---

## 15. Approval

| Role | Sign-off |
|------|----------|
| Technical Director | Pending |
| Design | Pending |
| Production | Pending |
| QA | Pending |
| Art | Pending |

**Next step after approval:** Implementation plan via `writing-plans` skill, starting with P0 on branch `course-editor`.

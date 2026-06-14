# Course Editor P1 — Elevation & Hazards Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the course editor and runtime so designers can sculpt height on the 2 m grid, paint water/OB hazard polygons, bake deformed ground + hazard triggers, and playtest penalty strokes with minimap hazard tinting.

**Architecture:** `HoleData` gains optional `ElevationGrid` and `HazardPolygon` collections (backward-compatible with flat P0 JSON). `HeightGridSampler` bilinearly samples Y for mesh vertices and disc placement. `CourseBuilder` emits welded height-aware ground meshes plus `Water`/`OB` trigger colliders under `BuiltCourse/Hazards`. `HazardRules` applies +1 stroke and drop/re-tee logic via `ThrowController`. Editor tools live in `CourseEditorWindow` + `CourseEditorOverlay` with Undo.

**Tech Stack:** Unity Editor APIs, C#, Unity Test Framework (EditMode), existing `HoleDataJson` DTO pattern, `DiscLieGround`, `BuiltCourseHost`, `MinimapUI`

**Spec:** `docs/superpowers/specs/2026-05-30-course-editor-design.md` (§6.2, §8.3, §9.4, P1 exit criteria)

**Branch:** Create `course-editor-p1` from `main` after P0 merge (`4c5be26`).

---

## P1 Exit Criteria

- [ ] Paint elevation on height grid; ground mesh follows height at tile corners (no visible seams on fairway).
- [ ] Water and OB polygons bake as tagged trigger volumes.
- [ ] Landing in water or OB adds +1 stroke and relocates disc per default rules (see §Design locks).
- [ ] Minimap shows hazard regions (blue water, red OB tint).
- [ ] JSON round-trip preserves elevation + hazards; P0 holes without those fields still load flat.
- [ ] Validator warns on self-intersecting hazard polygons (`W003`).

---

## Design Locks (resolve spec open items for P1)

| Decision | P1 choice | Notes |
|----------|-----------|-------|
| Water drop | Nearest playable tile (fairway/green/tee) by XZ distance | No lateral-line math in P1 |
| OB drop | Re-tee at hole tee | Matches casual play; `hazardRules` JSON block deferred to P2 |
| Height grid origin | Aligns to `HoleData.Origin` + grid `(0,0)` corner | Same as surface tile indexing |
| Grid sizing | Auto-expand to painted tile bounds + 1 tile padding | Empty grid = flat Y=0 everywhere |
| Schema | Still `schemaVersion: 1`; new JSON fields optional | Avoid migration tooling until P4 |

---

## File Map

| File | Responsibility |
|------|----------------|
| `Assets/Scripts/CourseEditor/ElevationGrid.cs` | Width/height, row-major heights, resize, sample API |
| `Assets/Scripts/CourseEditor/HazardPolygon.cs` | Id, type (water/ob), tile-space vertices |
| `Assets/Scripts/CourseEditor/HazardType.cs` | Enum: Water, OB |
| `Assets/Scripts/CourseEditor/HoleData.cs` | Add optional `Elevation`, `Hazards` lists |
| `Assets/Scripts/CourseEditor/HoleDataDto.cs` | DTO fields for `elevation`, `hazards` |
| `Assets/Scripts/CourseEditor/HeightGridSampler.cs` | World XZ → Y (bilinear), grid coord helpers |
| `Assets/Scripts/CourseEditor/CourseBuilder.cs` | Height-aware mesh; hazard trigger bake |
| `Assets/Scripts/CourseEditor/HazardRules.cs` | Resolve drop position + penalty kind |
| `Assets/Scripts/CourseEditor/CourseValidator.cs` | W003 self-intersect; E005 hazard outside bounds |
| `Assets/Scripts/Flight/LieType.cs` | Add `Water` (OB already exists) |
| `Assets/Scripts/Gameplay/DiscLieGround.cs` | Classify Water/OB tags + triggers |
| `Assets/Scripts/Core/ThrowController.cs` | Hazard landing path (+1 stroke, drop) |
| `Assets/Scripts/UI/MinimapUI.cs` | Hazard overlay tint from baked hazard bounds |
| `Assets/Scripts/UI/ScoreBannerSprites.cs` | Wire `PenaltyStroke` banner on hazard landing (optional) |
| `Assets/Editor/CourseEditor/CourseEditorState.cs` | Active tool: Elevate, Hazard; brush radius |
| `Assets/Editor/CourseEditor/CourseEditorOverlay.cs` | Contours, hazard polygon preview, elevate brush |
| `Assets/Editor/CourseEditor/CourseEditorWindow.cs` | Tool buttons, hazard type selector, elevate strength |
| `Assets/Tests/EditMode/ElevationGridTests.cs` | Sample + resize |
| `Assets/Tests/EditMode/HoleDataJsonElevationTests.cs` | JSON round-trip with elevation/hazards |
| `Assets/Tests/EditMode/HazardRulesTests.cs` | Drop point selection |
| `Assets/Tests/EditMode/CourseValidatorHazardTests.cs` | W003 cases |

---

### Task 1: Domain types — elevation grid & hazard polygons

**Files:**
- Create: `Assets/Scripts/CourseEditor/ElevationGrid.cs`
- Create: `Assets/Scripts/CourseEditor/HazardType.cs`
- Create: `Assets/Scripts/CourseEditor/HazardPolygon.cs`
- Modify: `Assets/Scripts/CourseEditor/HoleData.cs`
- Test: `Assets/Tests/EditMode/ElevationGridTests.cs`

- [ ] **Step 1: Write failing tests** for grid resize, flat default, `SampleBilinear` at corners/center.
- [ ] **Step 2: Run tests** — expect compile fail then fail on behavior.
- [ ] **Step 3: Implement** `ElevationGrid` (width, height, float[] heights row-major), `EnsureSize(int w, int h)`, `Get/Set`, `SampleBilinear(normalized x, normalized z)`.
- [ ] **Step 4: Add** `HazardPolygon` (string Id, HazardType Type, List<Vector2Int> Vertices in tile coords).
- [ ] **Step 5: Extend** `HoleData` with `ElevationGrid Elevation` (null = flat) and `List<HazardPolygon> Hazards`.
- [ ] **Step 6: Run tests** — pass.
- [ ] **Step 7: Commit** `feat(course-editor): add elevation grid and hazard polygon domain types`

---

### Task 2: JSON serialization for elevation & hazards

**Files:**
- Modify: `Assets/Scripts/CourseEditor/HoleDataDto.cs`
- Modify: `Assets/Scripts/CourseEditor/HoleDataJson.cs` (if needed)
- Test: `Assets/Tests/EditMode/HoleDataJsonElevationTests.cs`

- [ ] **Step 1: Write failing test** — round-trip hole with 3×3 elevation and one water polygon; P0-only JSON still loads with null elevation/hazards.
- [ ] **Step 2: Run test** — fail.
- [ ] **Step 3: Add DTOs** matching spec §6.2: `elevation { width, height, heights[] }`, `hazards[] { id, type, vertices[][] }`.
- [ ] **Step 4: Implement** `FromDomain` / `ToDomain` conversion.
- [ ] **Step 5: Run tests** — pass.
- [ ] **Step 6: Commit** `feat(course-editor): serialize elevation and hazards in hole JSON`

---

### Task 3: HeightGridSampler utilities

**Files:**
- Create: `Assets/Scripts/CourseEditor/HeightGridSampler.cs`
- Test: extend `ElevationGridTests.cs`

- [ ] **Step 1: Write failing tests** — world XZ from `HoleData.Origin` + tile index maps to expected Y.
- [ ] **Step 2: Implement** static helpers: `GridSizeForHole(HoleData)`, `SampleWorldY(HoleData, float x, float z)`, `TileCornerHeights(HoleData, int tx, int ty)` for mesh welding.
- [ ] **Step 3: Run tests** — pass.
- [ ] **Step 4: Commit** `feat(course-editor): add height grid world sampling helpers`

---

### Task 4: CourseBuilder — elevated ground meshes

**Files:**
- Modify: `Assets/Scripts/CourseEditor/CourseBuilder.cs`
- Modify: `Assets/Scripts/CourseEditor/BuiltCourseHost.cs` (if bounds need Y extent)

- [ ] **Step 1: Change** `BuildTileMesh` to place quad corners at sampled Y from `HeightGridSampler` (4 vertices per tile, shared edge welding optional in P1 — start per-tile quads; weld if seams visible).
- [ ] **Step 2: Ensure** tee/basket Y sampled from grid at their XZ.
- [ ] **Step 3: Manual test** — elevate a hill in scratch data (temporary test menu or unit test fixture), bake, confirm mesh rises in Scene view.
- [ ] **Step 4: Commit** `feat(course-editor): bake height-aware ground meshes`

---

### Task 5: CourseBuilder — hazard trigger volumes

**Files:**
- Modify: `Assets/Scripts/CourseEditor/CourseBuilder.cs`
- Modify: `Assets/Scripts/CourseEditor/SurfaceTileTags.cs` or new `HazardTags.cs`

- [ ] **Step 1: Add** `BuildHazards(HoleData, Transform hazardsRoot)` — extrude polygon to thin box trigger (Y min/max from grid sample ± margin).
- [ ] **Step 2: Tag** water colliders `Water`, OB colliders `OB` (add tags to TagManager if missing).
- [ ] **Step 3: Parent** under `BuiltCourse/Hazards/Water_*`, `OB_*`.
- [ ] **Step 4: Manual test** — bake hole with rectangle water hazard; confirm trigger visible in Scene (Gizmos) and disc raycast can hit tag on landing test.
- [ ] **Step 5: Commit** `feat(course-editor): bake water and OB hazard triggers`

---

### Task 6: LieType + DiscLieGround classification

**Files:**
- Modify: `Assets/Scripts/Flight/LieType.cs`
- Modify: `Assets/Scripts/Gameplay/DiscLieGround.cs`
- Test: `Assets/Tests/EditMode/DiscLieGroundTests.cs` (new, use mock collider tags)

- [ ] **Step 1: Add** `LieType.Water`.
- [ ] **Step 2: Extend** `ClassifyGroundCollider` — `Water` tag → Water, `OB` tag → OB.
- [ ] **Step 3: Add** optional trigger query on landing: if disc inside water/OB trigger at rest, prefer hazard over ground mesh below.
- [ ] **Step 4: Run tests** — pass.
- [ ] **Step 5: Commit** `feat(gameplay): classify water and OB lies on built courses`

---

### Task 7: HazardRules + ThrowController penalty flow

**Files:**
- Create: `Assets/Scripts/CourseEditor/HazardRules.cs`
- Modify: `Assets/Scripts/Core/ThrowController.cs`
- Test: `Assets/Tests/EditMode/HazardRulesTests.cs`

- [ ] **Step 1: Write failing tests** — water landing → +1 stroke, drop at nearest playable tile; OB → +1 stroke, re-tee position.
- [ ] **Step 2: Implement** `HazardRules.ResolveDrop(HoleData, LieType hazard, Vector3 discPos)` returning world position.
- [ ] **Step 3: In** `ThrowController` post-throw landing: if lie is Water or OB, increment stroke, show penalty banner (`ScoreBannerSprites.PenaltyStroke`), relocate disc, skip normal lie banner or show after drop.
- [ ] **Step 4: Playtest** on CourseEditor scene with baked hazards.
- [ ] **Step 5: Commit** `feat(gameplay): apply hazard penalty strokes and drop positions`

---

### Task 8: Editor — Elevate tool

**Files:**
- Modify: `Assets/Editor/CourseEditor/CourseEditorState.cs`
- Modify: `Assets/Editor/CourseEditor/CourseEditorOverlay.cs`
- Modify: `Assets/Editor/CourseEditor/CourseEditorWindow.cs`

- [ ] **Step 1: Add** tool enum value `Elevate`; brush radius (1–5 tiles), strength (+/- 0.25 m per stroke), optional smooth mode (average neighbors).
- [ ] **Step 2: Scene drag** adjusts height grid cells under cursor; register `Undo.RecordObject` on scratch asset.
- [ ] **Step 3: Draw** contour lines in overlay (every 1 m delta) when elevation non-flat.
- [ ] **Step 4: Auto-resize** elevation grid when painting tiles outside current bounds.
- [ ] **Step 5: Commit** `feat(course-editor): add elevate tool with undo`

---

### Task 9: Editor — Hazard polygon tool

**Files:**
- Modify: `Assets/Editor/CourseEditor/CourseEditorState.cs`
- Modify: `Assets/Editor/CourseEditor/CourseEditorOverlay.cs`
- Modify: `Assets/Editor/CourseEditor/CourseEditorWindow.cs`

- [ ] **Step 1: Add** tool `Hazard`; layer selector Water / OB.
- [ ] **Step 2: Click** adds vertices snapped to tile corners; Enter/double-click closes polygon; Esc cancels.
- [ ] **Step 3: Draw** filled preview (blue water, red OB hatch).
- [ ] **Step 4: Delete** selected hazard from list in window UI.
- [ ] **Step 5: Commit** `feat(course-editor): add hazard polygon authoring tool`

---

### Task 10: Validation — hazard rules W003/E005

**Files:**
- Modify: `Assets/Scripts/CourseEditor/CourseValidator.cs`
- Test: `Assets/Tests/EditMode/CourseValidatorHazardTests.cs`

- [ ] **Step 1: Implement** self-intersection test for hazard polygons → `W003`.
- [ ] **Step 2: Implement** vertices outside painted bounds → `W003` or new `W006`.
- [ ] **Step 3: Run tests** — pass.
- [ ] **Step 4: Commit** `feat(course-editor): validate hazard polygon geometry`

---

### Task 11: Minimap hazard tint

**Files:**
- Modify: `Assets/Scripts/UI/MinimapUI.cs`
- Modify: `Assets/Scripts/CourseEditor/BuiltCourseHost.cs` (expose hazard AABBs or render targets)

- [ ] **Step 1: After bake**, collect hazard trigger XZ bounds in world space.
- [ ] **Step 2: In** minimap render pass, draw semi-transparent quads (blue/red) before disc/tee dots.
- [ ] **Step 3: Verify** alignment with existing minimap framing from `BuiltCourseHost`.
- [ ] **Step 4: Commit** `feat(ui): tint water and OB on minimap for built courses`

---

### Task 12: Sample hole + docs + QA checklist

**Files:**
- Modify: `Assets/Data/Courses/Example/hole_01.json` (add small water strip + mild elevation)
- Modify: `Assets/README.md`

- [ ] **Step 1: Update** example hole JSON with elevation + one water hazard for regression.
- [ ] **Step 2: Add** README section: Elevate tool, Hazard tool, penalty behavior defaults.
- [ ] **Step 3: Run** EditMode test suite (`HoleDataJson`, `CourseValidator`, new P1 tests).
- [ ] **Step 4: Manual QA** from spec §12 Play Mode checklist items for water/OB and elevation snap.
- [ ] **Step 5: Commit** `docs: course editor P1 elevation and hazards workflow`

---

## Spec Coverage (P1 self-review)

| P1 requirement | Task |
|----------------|------|
| Height grid + elevate tool + smooth | Tasks 1, 3, 8 |
| Water and OB polygon tool | Tasks 1, 2, 9 |
| `HazardRules` + penalty stroke | Task 7 |
| `DiscLieGround` water/OB classification | Task 6 |
| Minimap hazard tint | Task 11 |
| Deformed ground mesh | Task 4 |
| Hazard trigger bake | Task 5 |
| JSON round-trip | Task 2 |
| Validation W003 | Task 10 |

**Deferred to P2+:** foliage placement, `hazardRules` per-hole JSON tuning, OB line drop, theme swap UI, RLE compression.

---

## Suggested implementation order

1. Tasks 1–3 (data + JSON + sampler) — no visual change yet, tests green.
2. Task 4 (elevated mesh) — unlocks seeing height in bake.
3. Task 8 (elevate tool) — designer-facing height authoring.
4. Tasks 5–6–7 (hazards bake + gameplay) — playable penalties.
5. Tasks 9–11 (hazard tool + minimap + validation polish).
6. Task 12 (docs + example hole).

---

## Manual QA checklist (P1)

- [ ] Load P0-only JSON — still flat, no errors.
- [ ] Elevate fairway center — disc rests on slope; no z-fighting between adjacent tiles.
- [ ] Land in painted water — +1 stroke, penalty banner, disc at nearest fairway/green.
- [ ] Land in OB — +1 stroke, disc at tee.
- [ ] Minimap shows water/OB regions aligned with world.
- [ ] Export JSON → re-import → bake produces same max height and hazard count.

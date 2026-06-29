# Player Course Editor E0–E2 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a PC player-facing course editor reachable from Main Menu: Hub library with save/load, in-game edit HUD with paint/erase/tee/basket/hazard tools, and inline playtest with plain-language validation — no Unity EditorWindow required for players.

**Architecture:** Extract authoring operations from `CourseEditorOverlay` into runtime `CourseAuthoring*` modules shared with the dev overlay. Add `CourseEditorHub` + rebuilt `CourseEditor` scenes to the player build. Persist holes under `Application.persistentDataPath/Courses/`. Session state (`CourseEditorSession`) drives edit vs playtest modes reusing `CourseBuilder` + `CourseValidator`.

**Tech Stack:** Unity URP, uGUI + TMP, C#, Unity Test Framework (EditMode), existing `HoleDataJson` / `CourseBuilder` / `CourseValidator`, Windows standalone file dialogs (PC only)

**Spec:** `docs/superpowers/specs/2026-05-30-player-course-editor-design.md` (Approved; W005 disabled, no profanity filter, PC only)

**Branch:** Create `player-course-editor` from current `main` (or `course-editor-p1` if that branch holds latest editor work).

---

## E0–E2 Exit Criteria

- [ ] Main Menu has **Course Editor** button → loads `CourseEditorHub` scene (in build settings).
- [ ] Hub: New Hole wizard (name + template), list saved holes, Edit opens editor scene.
- [ ] **Straight Par 3** template produces ~250 yd tee→basket spacing.
- [ ] Editor: paint, erase, tee, basket, hazard tools work in Game view with live rebake.
- [ ] Save writes JSON + meta to `persistentDataPath`; reload round-trips.
- [ ] Playtest toggles inline without scene reload; validation blocks on errors with player copy.
- [ ] `W005` hole-length warning removed from `CourseValidator`.
- [ ] PC import/export: Windows file open/save for `.json` (or paste-json fallback documented in README if package blocked).

---

## Design Locks (from approved spec)

| Decision | Choice |
|----------|--------|
| Hole length | No `W005` warnings in v1 |
| Templates default | Straight Par 3 ≈ 250 yd |
| Profanity filter | None |
| Platform | PC standalone only |
| JSON schema | v1 unchanged |
| Dev Unity editor | Keep `Course Editor (Dev)` menu; not player path |

---

## File Map

| File | Responsibility |
|------|----------------|
| `Assets/Scripts/CourseEditor/Authoring/CourseAuthoringTool.cs` | Enum: Paint, Erase, HoleTee, HoleBasket, Hazard (+ Elevate later) |
| `Assets/Scripts/CourseEditor/Authoring/CourseAuthoringState.cs` | Runtime-safe tool/brush/hazard draft state |
| `Assets/Scripts/CourseEditor/Authoring/CourseAuthoringGrid.cs` | `TryWorldToTile`, `TileCenterWorld`, `TileCornerWorld` |
| `Assets/Scripts/CourseEditor/Authoring/CourseAuthoringOperations.cs` | Paint, erase, markers, hazard commit |
| `Assets/Scripts/CourseEditor/Authoring/CourseAuthoringCommandStack.cs` | Runtime undo/redo (max 50) |
| `Assets/Scripts/CourseEditor/Authoring/CourseAuthoringCommands.cs` | `IAuthoringCommand` implementations |
| `Assets/Scripts/CourseEditor/HoleDataCatalog.cs` | Scan/load/save/delete holes in persistent path |
| `Assets/Scripts/CourseEditor/HoleDataMeta.cs` | Sidecar meta: displayName, published, lastEdited |
| `Assets/Scripts/CourseEditor/HoleDataTemplates.cs` | Blank, Straight Par 3 (~250 yd), Ridgeline clone |
| `Assets/Scripts/CourseEditor/CourseEditorSession.cs` | Mode Editing/Playtesting, dirty, active hole, theme |
| `Assets/Scripts/CourseEditor/ValidationMessageCopy.cs` | Code → player-facing string + focus hint |
| `Assets/Scripts/CourseEditor/ThemePackLoader.cs` | Resolve theme by id from Resources |
| `Assets/Scripts/CourseEditor/CourseEditorRuntimeBootstrap.cs` | Extend: accept `HoleData` directly, playtest flag |
| `Assets/Scripts/UI/CourseEditor/CourseEditorHubController.cs` | Hub UI logic |
| `Assets/Scripts/UI/CourseEditor/NewHoleWizardController.cs` | Wizard steps |
| `Assets/Scripts/UI/CourseEditor/CourseEditorHudController.cs` | Top bar, dock, context strip |
| `Assets/Scripts/UI/CourseEditor/CourseEditorInputController.cs` | Raycast + call operations |
| `Assets/Scripts/UI/CourseEditor/CourseEditorCameraController.cs` | Orbit/pan/zoom + presets |
| `Assets/Scripts/UI/CourseEditor/ValidationDrawerController.cs` | Plain-language validation list |
| `Assets/Scripts/UI/CourseEditor/CourseEditorPlaytestOverlay.cs` | “Press Esc to exit playtest” pill |
| `Assets/Scripts/Core/SceneFlow.cs` | Add Hub + Editor scene names |
| `Assets/Scripts/Gameplay/ProjectArtPaths.cs` | Add `CourseEditorHub` scene path |
| `Assets/Scripts/UI/Menu/MainMenuController.cs` | Course Editor button |
| `Assets/Editor/MenuSceneBuilder.cs` | Hub scene + main menu button + build settings |
| `Assets/Editor/CourseEditor/PlayerCourseEditorSceneBuilder.cs` | Rebuild runtime editor scene (HUD + camera + bootstrap) |
| `Assets/Editor/CourseEditor/CourseEditorOverlay.cs` | Refactor to call `CourseAuthoringOperations` |
| `Assets/Tests/EditMode/HoleDataTemplatesTests.cs` | Template yardage + tile counts |
| `Assets/Tests/EditMode/HoleDataCatalogTests.cs` | Save/load round-trip (temp directory) |
| `Assets/Tests/EditMode/CourseAuthoringOperationsTests.cs` | Paint, hazard, tee |
| `Assets/Tests/EditMode/ValidationMessageCopyTests.cs` | Player strings for E001–E004 |

---

### Task 1: Remove W005 and add template factory tests

**Files:**
- Modify: `Assets/Scripts/CourseEditor/CourseValidator.cs`
- Create: `Assets/Scripts/CourseEditor/HoleDataTemplates.cs`
- Create: `Assets/Tests/EditMode/HoleDataTemplatesTests.cs`

- [ ] **Step 1: Write failing test for Straight Par 3 ~250 yd**

```csharp
// Assets/Tests/EditMode/HoleDataTemplatesTests.cs
using DiskGolf.CourseEditor;
using NUnit.Framework;

namespace DiskGolf.Tests.EditMode
{
    public sealed class HoleDataTemplatesTests
    {
        [Test]
        public void StraightPar3_IsAbout250Yards()
        {
            var data = HoleDataTemplates.CreateStraightPar3("Test");
            float yards = data.HoleLengthYards();
            Assert.Greater(yards, 230f);
            Assert.Less(yards, 270f);
            Assert.AreEqual(3, data.Hole.Par);
        }
    }
}
```

- [ ] **Step 2: Run test — expect FAIL**

Run: Unity Test Runner EditMode filter `HoleDataTemplatesTests` (or `dotnet` if CI wired)

Expected: `HoleDataTemplates` type not found

- [ ] **Step 3: Implement `HoleDataTemplates`**

```csharp
// Assets/Scripts/CourseEditor/HoleDataTemplates.cs
using System;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    public static class HoleDataTemplates
    {
        public const string StraightPar3Id = "template_straight_par3";

        public static HoleData CreateBlankPar3(string displayName)
        {
            var data = NewBase(displayName, "blank_par3");
            PaintRoughBorder(data, width: 12, length: 65);
            return data;
        }

        public static HoleData CreateStraightPar3(string displayName)
        {
            var data = NewBase(displayName, StraightPar3Id);
            int centerX = 6;
            // ~250 yd at 2 m/tile ≈ 114 tiles along Y; use 58 tiles tee row to basket row
            for (int y = 0; y < 60; y++)
                data.SetTile(centerX, y, SurfaceTileType.Fairway);
            for (int y = 55; y < 60; y++)
                data.SetTile(centerX, y, SurfaceTileType.Green);
            data.SetTile(centerX, 0, SurfaceTileType.Tee);
            data.Hole.Tee = TileCenter(data, centerX, 0);
            data.Hole.Basket = TileCenter(data, centerX, 58);
            data.Hole.Par = 3;
            PaintRoughBorder(data, width: 12, length: 65);
            return data;
        }

        public static HoleData CreateFromTemplateId(string templateId, string displayName)
        {
            return templateId switch
            {
                StraightPar3Id => CreateStraightPar3(displayName),
                "blank_par3" => CreateBlankPar3(displayName),
                "ridgeline" => HoleDataJson.LoadFromFile(
                    System.IO.Path.Combine(UnityEngine.Application.dataPath, "Data/Courses/Example/hole_01.json")),
                _ => CreateBlankPar3(displayName)
            };
        }

        static HoleData NewBase(string displayName, string idSuffix)
        {
            return new HoleData
            {
                Id = $"{idSuffix}_{Guid.NewGuid():N}".Substring(0, 24),
                Name = displayName,
                ThemeId = "temperate",
                TileSize = HoleData.DefaultTileSize
            };
        }

        static Vector2 TileCenter(HoleData data, int x, int y) =>
            new(data.Origin.x + (x + 0.5f) * data.TileSize, data.Origin.y + (y + 0.5f) * data.TileSize);

        static void PaintRoughBorder(HoleData data, int width, int length)
        {
            for (int x = 0; x < width; x++)
            for (int y = 0; y < length; y++)
            {
                if (x == 0 || y == 0 || x == width - 1 || y == length - 1)
                    if (!data.TryGetTile(x, y, out _))
                        data.SetTile(x, y, SurfaceTileType.Rough);
            }
        }
    }
}
```

Adjust tile indices until test passes 230–270 yd band.

- [ ] **Step 4: Remove W005 from validator**

In `CourseValidator.Validate`, delete the block:

```csharp
float yards = data.HoleLengthYards();
if (yards > 0f && (yards < MinYards || yards > MaxYards))
    ...
```

Remove unused `MinYards` / `MaxYards` constants.

- [ ] **Step 5: Run tests — expect PASS**

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/CourseEditor/HoleDataTemplates.cs Assets/Scripts/CourseEditor/CourseValidator.cs Assets/Tests/EditMode/HoleDataTemplatesTests.cs
git commit -m "feat: add hole templates and remove W005 length warning"
```

---

### Task 2: Hole catalog persistence (save/load/list)

**Files:**
- Create: `Assets/Scripts/CourseEditor/HoleDataMeta.cs`
- Create: `Assets/Scripts/CourseEditor/HoleDataCatalog.cs`
- Create: `Assets/Tests/EditMode/HoleDataCatalogTests.cs`

- [ ] **Step 1: Write failing round-trip test**

```csharp
[Test]
public void SaveThenLoad_PreservesNameAndTiles()
{
    var dir = Path.Combine(Path.GetTempPath(), "diskg_catalog_test");
    Directory.CreateDirectory(dir);
    try
    {
        var catalog = new HoleDataCatalog(dir);
        var data = HoleDataTemplates.CreateStraightPar3("My Hole");
        catalog.Save(data, published: false);
        var entries = catalog.ListEntries();
        Assert.AreEqual(1, entries.Count);
        var loaded = catalog.Load(entries[0].Id);
        Assert.AreEqual("My Hole", loaded.Name);
        Assert.AreEqual(data.Hole.Tee, loaded.Hole.Tee);
    }
    finally { Directory.Delete(dir, true); }
}
```

- [ ] **Step 2: Run test — expect FAIL**

- [ ] **Step 3: Implement catalog**

```csharp
// HoleDataCatalog.cs — key API
public sealed class HoleDataCatalog
{
    readonly string root;
    public HoleDataCatalog(string rootDirectory) { root = rootDirectory; Directory.CreateDirectory(root); }

    public IReadOnlyList<HoleCatalogEntry> ListEntries() { /* scan *.json, join *.meta.json */ }
    public void Save(HoleData data, bool published) { /* HoleDataJson.SaveToFile + meta */ }
    public HoleData Load(string id) { /* load json by id */ }
    public void Delete(string id) { /* move to .trash/ */ }
}
```

Meta sidecar JSON: `{ "displayName", "published", "lastEditedUtc", "templateSource" }`.

Default player path wrapper:

```csharp
public static HoleDataCatalog Player => new(Application.persistentDataPath + "/Courses");
```

- [ ] **Step 4: Run test — expect PASS**

- [ ] **Step 5: Commit**

```bash
git commit -m "feat: add HoleDataCatalog persistence layer"
```

---

### Task 3: Extract authoring core from editor overlay

**Files:**
- Create: `Assets/Scripts/CourseEditor/Authoring/CourseAuthoringTool.cs`
- Create: `Assets/Scripts/CourseEditor/Authoring/CourseAuthoringState.cs`
- Create: `Assets/Scripts/CourseEditor/Authoring/CourseAuthoringGrid.cs`
- Create: `Assets/Scripts/CourseEditor/Authoring/CourseAuthoringOperations.cs`
- Modify: `Assets/Editor/CourseEditor/CourseEditorState.cs` — delegate grid helpers or duplicate thin wrapper
- Modify: `Assets/Editor/CourseEditor/CourseEditorOverlay.cs` — call operations
- Create: `Assets/Tests/EditMode/CourseAuthoringOperationsTests.cs`

- [ ] **Step 1: Write failing paint test**

```csharp
[Test]
public void PaintTile_SetsSurfaceType()
{
    var data = new HoleData();
    var state = new CourseAuthoringState();
    state.BrushType = SurfaceTileType.Fairway;
    Assert.IsTrue(CourseAuthoringOperations.TryPaintTile(data, state, 3, 4));
    Assert.IsTrue(data.TryGetTile(3, 4, out var t) && t == SurfaceTileType.Fairway);
}
```

- [ ] **Step 2: Run test — expect FAIL**

- [ ] **Step 3: Move logic from overlay**

Port `PaintTile`, `EraseTile`, `PlaceMarker`, `AppendHazardVertex`, `CommitHazardPolygon`, `ElevateAt` bodies into `CourseAuthoringOperations` (static methods taking `HoleData` + `CourseAuthoringState`). Keep `#if UNITY_EDITOR` undo in overlay; runtime uses command stack in Task 4.

`CourseAuthoringGrid.TryWorldToTile` copies math from `CourseEditorState.TryWorldToTile`.

- [ ] **Step 4: Refactor overlay to call operations** — behavior unchanged in Unity dev editor.

- [ ] **Step 5: Run EditMode tests + manual dev editor smoke (paint one tile)**

- [ ] **Step 6: Commit**

```bash
git commit -m "refactor: extract CourseAuthoringOperations for runtime reuse"
```

---

### Task 4: Runtime undo command stack

**Files:**
- Create: `Assets/Scripts/CourseEditor/Authoring/IAuthoringCommand.cs`
- Create: `Assets/Scripts/CourseEditor/Authoring/CourseAuthoringCommands.cs`
- Create: `Assets/Scripts/CourseEditor/Authoring/CourseAuthoringCommandStack.cs`

- [ ] **Step 1: Write failing undo test**

```csharp
[Test]
public void Undo_RestoresPreviousTile()
{
    var data = new HoleData();
    var stack = new CourseAuthoringCommandStack();
    stack.Execute(new PaintTileCommand(data, 1, 1, SurfaceTileType.Fairway));
    Assert.IsTrue(data.TryGetTile(1, 1, out var t) && t == SurfaceTileType.Fairway);
    stack.Undo();
    Assert.IsFalse(data.TryGetTile(1, 1, out _));
}
```

- [ ] **Step 2: Implement stack + `PaintTileCommand`, `EraseTileCommand`, `PlaceTeeCommand`, `PlaceBasketCommand`, `AddHazardCommand`**

Coalesce: `CourseAuthoringInputController` starts stroke on pointer down, commits one `PaintStrokeCommand` on pointer up.

- [ ] **Step 3: Run tests — expect PASS**

- [ ] **Step 4: Commit**

---

### Task 5: Session + validation copy + theme loader

**Files:**
- Create: `Assets/Scripts/CourseEditor/CourseEditorSession.cs`
- Create: `Assets/Scripts/CourseEditor/ValidationMessageCopy.cs`
- Create: `Assets/Scripts/CourseEditor/ThemePackLoader.cs`
- Create: `Assets/Tests/EditMode/ValidationMessageCopyTests.cs`

- [ ] **Step 1: Write failing copy test**

```csharp
[Test]
public void E001_PlayerCopy_IsPlainLanguage()
{
    var msg = ValidationMessageCopy.ForCode("E001");
    Assert.That(msg, Does.Contain("basket").IgnoreCase);
    Assert.That(msg, Does.Not.Contain("E001"));
}
```

- [ ] **Step 2: Implement copy table** per spec §11 (E001–E004, W001, W003, W004).

- [ ] **Step 3: Implement session singleton**

```csharp
public sealed class CourseEditorSession
{
    public static CourseEditorSession Instance { get; } = new();
    public HoleData Hole { get; private set; }
    public ThemePack Theme { get; private set; }
    public CourseEditorSessionMode Mode { get; set; } = CourseEditorSessionMode.Editing;
    public bool IsDirty { get; set; }
    public CourseAuthoringState Authoring { get; } = new();
    public CourseAuthoringCommandStack Commands { get; } = new();

    public void Load(HoleData hole, ThemePack theme) { Hole = hole; Theme = theme; IsDirty = false; }
}
```

- [ ] **Step 4: `ThemePackLoader.Load("temperate")`** — load from `Resources/Themes/ThemePack_Temperate` or Addressables path used by existing bootstrap.

- [ ] **Step 5: Run tests — commit**

---

### Task 6: Scene flow + Main Menu entry

**Files:**
- Modify: `Assets/Scripts/Gameplay/ProjectArtPaths.cs`
- Modify: `Assets/Scripts/Core/SceneFlow.cs`
- Modify: `Assets/Scripts/UI/Menu/MainMenuController.cs`
- Modify: `Assets/Editor/MenuSceneBuilder.cs`

- [ ] **Step 1: Add scene paths**

```csharp
// ProjectArtPaths.Scenes
public const string CourseEditorHub = MenuRoot + "/CourseEditorHub.unity";
// CourseEditor already exists as PrototypeRoot/CourseEditor.unity
```

```csharp
// SceneFlow
public const string CourseEditorHub = "CourseEditorHub";
public const string CourseEditor = "CourseEditor";
```

- [ ] **Step 2: Add Course Editor button in `BuildMainMenuScene`**

Fourth button at `y = -240f`; wire `MainMenuController.courseEditorButton`.

- [ ] **Step 3: Update `MainMenuController`**

```csharp
[SerializeField] Button courseEditorButton;
// Awake: courseEditorButton.onClick → SceneLoader.Load(SceneFlow.CourseEditorHub);
```

- [ ] **Step 4: Add `BuildCourseEditorHubScene()` stub** — title + Back + New Hole buttons + `CourseEditorHubController`.

- [ ] **Step 5: Update `UpdateBuildSettings`** — insert Hub before PrototypeFlat3; include CourseEditor scene.

- [ ] **Step 6: Run menu build** — `Disk Golf → Build Menu Scenes`

- [ ] **Step 7: Commit**

---

### Task 7: Course Editor Hub UI

**Files:**
- Create: `Assets/Scripts/UI/CourseEditor/CourseEditorHubController.cs`
- Create: `Assets/Scripts/UI/CourseEditor/NewHoleWizardController.cs`
- Modify: `Assets/Editor/MenuSceneBuilder.cs`

- [ ] **Step 1: Hub loads catalog on enable** — populate scroll list from `HoleDataCatalog.Player.ListEntries()`.

- [ ] **Step 2: New Hole opens wizard overlay** — Step 1 name field, Step 2 template cards (Blank, Straight Par 3 default selected, Ridgeline), Step 3 theme (Temperate only), Step 4 Start.

- [ ] **Step 3: Start Building** — save new hole via catalog, set `CourseEditorSession.Load`, store selected hole id in `PlayerPrefs` key `course_editor_active_id`, load `SceneFlow.CourseEditor`.

- [ ] **Step 4: Edit button** on card — same session load path.

- [ ] **Step 5: Back → MainMenu**

- [ ] **Step 6: Manual test** — Main Menu → Course Editor → New → name "Test" → Straight Par 3 → lands in editor scene (may be empty HUD until Task 9).

- [ ] **Step 7: Commit**

---

### Task 8: Extend runtime bootstrap for session data

**Files:**
- Modify: `Assets/Scripts/CourseEditor/CourseEditorRuntimeBootstrap.cs`

- [ ] **Step 1: Add overload path**

```csharp
public void Configure(HoleData data, ThemePack themePack, HoleSetup setup)
{
    playtestHole = null;
    runtimeHole = data;
    theme = themePack;
    holeSetup = setup;
}

HoleData runtimeHole;

void Awake()
{
    // ...
    HoleData data = runtimeHole ?? playtestHole?.Data;
    if (data == null || theme == null) return;
    var host = CourseBuilder.Build(data, theme);
    // ... existing bind
}
```

- [ ] **Step 2: On scene load in edit mode**, `CourseEditorHudController` calls bootstrap rebuild after each edit debounce — extract `CourseBuilder.Build` to shared method `CourseEditorSession.Rebake()`.

- [ ] **Step 3: Commit**

---

### Task 9: Rebuild player CourseEditor scene

**Files:**
- Create: `Assets/Editor/CourseEditor/PlayerCourseEditorSceneBuilder.cs`
- Modify: `Assets/Editor/PrototypeFlat3SceneBuilder.cs` or delegate to new builder

- [ ] **Step 1: Menu item** `Disk Golf → Course → Rebuild Player Course Editor Scene`

- [ ] **Step 2: Scene contents**

Reuse throw rig + HUD from existing `BuildCourseEditorScene()`:
- Main camera + `CourseEditorCameraController`
- Directional light, skybox
- EventSystem + Editor HUD canvas (empty shells wired in Task 10)
- `CourseEditorRuntimeBootstrap`
- `CourseEditorInputController`
- `HoleSetup`, throw controller, disc bag, baked HUD via `HudSceneAuthoring`

- [ ] **Step 3: Ensure scene in build settings**

- [ ] **Step 4: Commit**

---

### Task 10: Editor HUD + input (E1 tools)

**Files:**
- Create: `Assets/Scripts/UI/CourseEditor/CourseEditorHudController.cs`
- Create: `Assets/Scripts/UI/CourseEditor/CourseEditorInputController.cs`
- Create: `Assets/Scripts/UI/CourseEditor/CourseEditorCameraController.cs`

- [ ] **Step 1: HudController Awake** — read `CourseEditorSession.Instance`, load hole from `PlayerPrefs` active id if session empty, initial rebake.

- [ ] **Step 2: Top bar** — Hub back (dirty prompt), hole name, par dropdown, yardage label, Save, Undo, Redo, Playtest.

- [ ] **Step 3: Tool dock** — Paint, Erase, Tee, Basket, Hazard toggles → `CourseAuthoringState.ActiveTool`.

- [ ] **Step 4: Context strip** — brush types for Paint; Water/OB + Finish/Cancel/Undo vertex for Hazard.

- [ ] **Step 5: InputController** — on LMB, raycast from camera to ground collider, `TryWorldToTile`, dispatch to operations via command stack. Drag painting coalesced. Enter/Esc for hazard when active.

- [ ] **Step 6: Debounced rebake** — 100 ms coroutine after edit; call `CourseBuilder.Build` destroy old `BuiltCourse`.

- [ ] **Step 7: CameraController** — MMB orbit, scroll zoom, shift+MMB pan; presets Overview/Tee/Basket/Top-Down buttons.

- [ ] **Step 8: Manual QA checklist**

| Step | Action | Expected |
|------|--------|----------|
| 1 | Paint fairway strip | Green mesh appears |
| 2 | Place tee + basket | Gizmos visible, yardage updates |
| 3 | Draw water hazard | Blue draped preview, bakes on finish |
| 4 | Save + Hub + Edit | Hole reloads identically |

- [ ] **Step 9: Commit**

---

### Task 11: Validation drawer + playtest mode (E2)

**Files:**
- Create: `Assets/Scripts/UI/CourseEditor/ValidationDrawerController.cs`
- Create: `Assets/Scripts/UI/CourseEditor/CourseEditorPlaytestOverlay.cs`
- Modify: `Assets/Scripts/UI/CourseEditor/CourseEditorHudController.cs`
- Modify: `Assets/Scripts/CourseEditor/CourseEditorSession.cs`

- [ ] **Step 1: Live validation** — on hole change, `CourseValidator.Validate`; update ⚠ count; drawer lists Must fix / Suggestions with player copy.

- [ ] **Step 2: Show me** — E001 selects Basket tool + Overview camera; map each code per spec §11.

- [ ] **Step 3: Playtest button flow**

```csharp
void OnPlaytestClicked()
{
    var result = CourseValidator.Validate(session.Hole);
    if (!result.CanPlaytest) { OpenDrawer(focusFirstError: true); return; }
    if (HasWarnings(result) && !confirmed) { ShowWarningsModal(); return; }
    session.Mode = CourseEditorSessionMode.Playtesting;
    hudRoot.SetActive(false);
    gameplayHud.SetActive(true);
    playtestOverlay.Show();
    holeSetup.PositionThrowerAtTee();
}
```

- [ ] **Step 4: Esc exits playtest** — restore editor HUD, hide throw UI, keep hole state.

- [ ] **Step 5: Playtest overlay pill** — TMP text top-center.

- [ ] **Step 6: Manual QA** — missing basket blocks playtest with readable message; valid Straight Par 3 playtests with meters + throw.

- [ ] **Step 7: Commit**

---

### Task 12: PC import/export on Hub

**Files:**
- Create: `Assets/Scripts/CourseEditor/Platform/WindowsFileDialog.cs` (wrapper)
- Modify: `Assets/Scripts/UI/CourseEditor/CourseEditorHubController.cs`

- [ ] **Step 1: Windows open file** — use `[DllImport("comdlg32.dll")]` GetOpenFileName or add minimal StandaloneFileBrowser plugin; filter `*.json`.

- [ ] **Step 2: Import** — copy picked file into catalog via `HoleDataJson.LoadFromFile` + `catalog.Save`.

- [ ] **Step 3: Export** — from editor top bar overflow or Hub card menu: SaveFileDialog to user path.

- [ ] **Step 4: Non-Windows builds** — guard with `#if UNITY_STANDALONE_WIN`; show "PC only" toast elsewhere (future).

- [ ] **Step 5: Commit**

---

### Task 13: Autosave + docs

**Files:**
- Modify: `Assets/Scripts/UI/CourseEditor/CourseEditorHudController.cs`
- Modify: `Assets/README.md`

- [ ] **Step 1: Autosave coroutine** — every 30 s if `session.IsDirty`, `HoleDataCatalog.Player.Save`.

- [ ] **Step 2: Save on playtest entry**

- [ ] **Step 3: Update README** — player path: Main Menu → Course Editor; dev path: Course Editor (Dev).

- [ ] **Step 4: Final verification**

Run all EditMode tests in `Assets/Tests/EditMode/`.

- [ ] **Step 5: Commit**

```bash
git commit -m "docs: document player course editor flow"
```

---

## Spec Coverage Self-Review

| Spec section | Task |
|--------------|------|
| §3 Navigation / scenes | Task 6, 7, 9 |
| §4 Hub | Task 7, 12 |
| §5 Wizard + 250 yd template | Task 1, 7 |
| §7 Editor HUD layout | Task 10 |
| §8 Tools (paint–hazard) | Task 3, 4, 10 |
| §9 Camera | Task 10 |
| §10 Playtest | Task 8, 11 |
| §11 Validation copy | Task 5, 11 |
| §12 Save/autosave | Task 2, 13 |
| §14 Authoring architecture | Task 3, 4, 5 |
| §19 W005 off, PC, no filter | Task 1, 12 |
| §13 Course Select publish | **Deferred to E5** (not in E0–E2) |
| §6 Coach tutorial | **Deferred to E4** |
| §8 Elevate/Trees/Theme | **Deferred to E3–E4** |

---

## Out of Scope (Next Plans)

- **E3:** Elevate tool + contour preview in runtime HUD
- **E4:** Foliage place, theme picker, first-run coach
- **E5:** Course Select My Courses + `CourseRuntimeLoader`
- **E6:** Share clipboard + gamepad

Follow-up plan file: `docs/superpowers/plans/2026-05-30-player-course-editor-e3-e5.md` (create after E0–E2 merges).

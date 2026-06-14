# Callout Banner Consolidation — Design Spec

**Date:** 2026-05-31  
**Status:** Draft (pending approval)  
**Branch:** `course-editor` (or follow-up `refactor/callout-banners`)  
**Engine:** Unity (URP, TextMesh Pro, uGUI)  
**Scope:** Runtime + editor HUD callout banners only

---

## 1. Vision

Consolidate seven near-duplicate banner MonoBehaviours into a **small callout stack** under `GameplayHUD`: shared lifecycle utilities, two reusable presentation primitives (sprite / text), one multi-slot score banner, and two specialized panels (throw summary, hole-complete cutscene). Scene builders and bake menus instantiate a **single prefab** instead of procedurally generating UI hierarchies in C#.

**Player-facing behavior must not change:** same sprites, text, timings, z-order, slide-in animation, and throw-flow sequencing.

---

## 2. Problem Statement

### Current inventory (~1,560 LOC in banner cluster)

| File | Lines | Role |
|------|------:|------|
| `ThrowSummaryBannerUI` | 483 | Pre-throw panel: portrait, name, throw #, slide-in |
| `HoleCompleteBannerUI` | 286 | Score sprite (8 slots: HIO → Awful) |
| `HoleCompleteCutsceneUI` | 206 | Full-screen hole summary overlay |
| `OnTheGreenBannerUI` | 150 | Circle landing sprite |
| `LieLandingBannerUI` | 142 | Fairway / rough landing sprite |
| `ThrowResultBannerUI` | 118 | `"NNN FEET"` text under banner |
| `SweetSpotBannerUI` | 115 | `"SWEET!"` timing overlay |
| `ScoreBannerSprites` | 60 | Asset loader (**keep as-is**) |

### Duplication

Every banner file repeats:

- `FindHudCanvas()` / `GameplayHUD` lookup
- `Ensure()` → find child → `EnsureBuilt()` → `Build()` fallback
- `CreateForScene()` vs runtime `Build()` (duplicate hierarchy construction)
- Coroutine auto-hide (`ShowBriefly`)
- Layout configuration (partially shared via `PostThrowCalloutLayout`)

### Consumer coupling

`ThrowController` serializes **six banner references** plus legacy `inTheCircleBanner` GameObject lookup, with ~15 lazy `??= X.Ensure()` call sites.

---

## 3. Design Decisions (Locked)

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Migration style | Strangler fig + thin facades | Zero behavior change during refactor; delete facades last |
| UI source of truth | `GameplayCallouts.prefab` | Eliminates code-gen drift between PrototypeFlat3 and CourseEditor |
| Shared primitives | `SpriteCalloutBanner`, `TextCalloutBanner` | Covers 4 of 7 current types |
| Multi-score banner | `MultiSlotSpriteBanner` | Absorbs `HoleCompleteBannerUI` slot logic |
| Specialized panels | Keep separate | `ThrowSummaryPanel`, `HoleCompleteCutsceneUI` have unique layout/animation |
| Asset loading | Keep `ScoreBannerSprites` | Already centralized; no change |
| Layout | Keep `PostThrowCalloutLayout` | Proven positions for sprite + feet label |
| Host component | `GameplayCalloutHost` on HUD | Single entry point for Ensure + serialized refs |
| ThrowController end state | One `[SerializeField] GameplayCalloutHost` | Replaces 6 banner fields |
| Legacy names | Preserve GameObject names in prefab | Scene find + existing bakes keep working |

---

## 4. Goals & Non-Goals

### Goals

- Reduce banner-cluster LOC by **~35–45%** (~1,560 → ~900–1,100)
- Single prefab instantiated by scene builders and bake menu
- One `FindHudCanvas` implementation
- One auto-hide coroutine helper
- `ThrowController` banner wiring simplified
- Manual QA checklist passes on `PrototypeFlat3` and `CourseEditor` scenes

### Non-Goals

- Redesigning banner art, copy, or timings
- Merging `ThrowSummaryPanel` into generic sprite/text primitives
- Merging `HoleCompleteCutsceneUI` into score banner
- Refactoring non-banner HUD (`NtmBottomBar`, meters, minimap)
- Play Mode automated UI tests (Edit Mode unit tests only)
- Addressing general UI folder size (~6.8k LOC) beyond this cluster

---

## 5. Architecture

```
GameplayHUD
└── GameplayCallouts                    [GameplayCalloutHost]
    ├── LieLandingBanner                [SpriteCalloutBanner]
    ├── OnTheGreenBanner                [SpriteCalloutBanner]
    ├── ThrowResultBanner               [TextCalloutBanner]
    ├── SweetSpotBanner                 [TextCalloutBanner]
    ├── HoleCompleteBanner              [MultiSlotSpriteBanner]
    ├── ThrowSummaryBanner              [ThrowSummaryPanel]
    └── HoleCompleteCutscene            [HoleCompleteCutsceneUI]
```

### Module responsibilities

| Module | Responsibility |
|--------|----------------|
| `HudCanvasUtility` | Find `GameplayHUD` RectTransform |
| `CalloutLifecycle` | Timed show/hide coroutines, bring-to-front |
| `SpriteCalloutBanner` | Single `Image`, sprite show/brief/hide |
| `TextCalloutBanner` | Single `TextMeshProUGUI`, layout presets |
| `MultiSlotSpriteBanner` | N child `Image` slots, show one kind |
| `GameplayCalloutHost` | Ensures prefab instance; exposes callout refs |
| `ThrowSummaryPanel` | Renamed/slimmed `ThrowSummaryBannerUI` (portrait + slide) |
| `HoleCompleteCutsceneUI` | Unchanged API; uses `HudCanvasUtility` only |
| Legacy facades | `LieLandingBannerUI`, etc. delegate to primitives until Task 11 |

### Data flow (unchanged semantics)

```
ThrowController
  → callouts.LieLanding.ShowBriefly(lie sprite)
  → callouts.OnTheGreen.ShowBriefly(on-green sprite)
  → callouts.ThrowDistance.ShowFeet(distanceFt)
  → callouts.SweetSpot.ShowBriefly("SWEET!")
  → callouts.HoleComplete.Show(strokes, par)
  → callouts.ThrowSummary.ShowBriefly(throw#, character, onDismissed)
  → HoleCompleteCutsceneUI.Show(strokes, par)  // existing cutscene path
```

---

## 6. Behavior Preservation Matrix

Manual QA must confirm each row after migration.

| ID | Trigger | Banner | Content | Duration | Notes |
|----|---------|--------|---------|----------|-------|
| B01 | Land on fairway | LieLanding | `ScoreBanner_Fairway` sprite | 2.25s auto-hide | Via `PostThrowCalloutLayout` |
| B02 | Land on rough | LieLanding | `ScoreBanner_Rough` sprite | 2.25s | |
| B03 | Land in circle (not green) | OnTheGreen | `ScoreBanner_OnTheGreen` | 2.25s | Legacy `InTheCircleBanner` hidden |
| B04 | Throw completes | ThrowResult | `"NNN FEET"` black TMP | Until next phase hides | Below sprite banner |
| B05 | Both meters sweet | SweetSpot | `"SWEET!"` purple TMP | 2.0s | |
| B06 | Pre-throw presentation | ThrowSummary | Player name + `"Nth Throw"` | 2.5s hold + 0.4s slide | Slide from above |
| B07 | Hole in basket | HoleComplete | Score kind sprite | 3.5s (cutscene timing separate) | One of 8 slots |
| B08 | Hole complete flow | Cutscene | Background + pose + score | 3.0s default | Full-screen overlay |
| B09 | Z-order | ThrowResult | Feet label | — | On-green or lie banner drawn above feet when both active |
| B10 | Scene bake | All | Prefab children | — | **Disk Golf → HUD → Bake Score Banners** shows preview |

---

## 7. Prefab & Paths

| Asset | Path |
|-------|------|
| Callouts prefab | `Assets/Prefabs/UI/GameplayCallouts.prefab` |
| Prefab constant | `ProjectArtPaths.Prefabs.GameplayCallouts` |

Prefab child names **must** match existing scene object names for backward compatibility:

- `LieLandingBanner`, `OnTheGreenBanner`, `ThrowResultBanner`, `SweetSpotBanner`
- `HoleCompleteBanner`, `ThrowSummaryBanner`, `HoleCompleteCutscene`

---

## 8. Migration Phases

### Phase A — Infrastructure (Tasks 1–4)

Add utilities + primitives. No consumer changes.

### Phase B — Strangler facades (Tasks 5–7)

Existing banner classes delegate to primitives. `ThrowController` unchanged.

### Phase C — Prefab + scenes (Tasks 8–9)

Author prefab, update bake menu and scene builders.

### Phase D — Consumer cleanup (Tasks 10–11)

`ThrowController` → `GameplayCalloutHost`. Delete facades and dead `Build()` code.

### Phase E — Tests + docs (Task 12)

Edit Mode tests for score-kind selection; README bake note.

---

## 9. File Layout (target)

```
Assets/
├── Prefabs/UI/
│   └── GameplayCallouts.prefab
├── Scripts/UI/
│   ├── Callouts/
│   │   ├── HudCanvasUtility.cs
│   │   ├── CalloutLifecycle.cs
│   │   ├── SpriteCalloutBanner.cs
│   │   ├── TextCalloutBanner.cs
│   │   ├── TextCalloutLayout.cs
│   │   ├── MultiSlotSpriteBanner.cs
│   │   └── GameplayCalloutHost.cs
│   ├── ThrowSummaryPanel.cs          (renamed from ThrowSummaryBannerUI)
│   ├── HoleCompleteCutsceneUI.cs      (minimal edits)
│   ├── PostThrowCalloutLayout.cs     (unchanged)
│   ├── ScoreBannerSprites.cs          (unchanged)
│   └── [deleted after Phase D]
│       LieLandingBannerUI.cs
│       OnTheGreenBannerUI.cs
│       ThrowResultBannerUI.cs
│       SweetSpotBannerUI.cs
│       HoleCompleteBannerUI.cs
│       ThrowSummaryBannerUI.cs
├── Editor/
│   ├── CalloutPrefabAuthoring.cs     (menu: build/save prefab)
│   └── HudSceneAuthoring.cs            (modified)
└── Tests/EditMode/
    └── MultiSlotSpriteBannerTests.cs
```

---

## 10. Testing Strategy

### Edit Mode (automated)

- `MultiSlotSpriteBannerTests`: `ShowKind` activates correct slot, hides others
- Existing `HoleScoreTests` / `ScoreBannerSprites.ResolveKind` remain valid

### Play Mode (manual — Behavior Matrix §6)

Run B01–B10 on:

- `Assets/Scenes/Prototype/PrototypeFlat3.unity`
- `Assets/Scenes/Prototype/CourseEditor.unity`

---

## 11. Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Scene references break after prefab swap | Keep GameObject names; facades during Phase B–C |
| ThrowSummary slide animation regresses | Dedicated panel; no generic primitive |
| Editor preview broken | `ApplyEditorPreview()` preserved on panel + multi-slot |
| CourseEditor vs PrototypeFlat3 drift | Both use same prefab via `GameplayCalloutHost.Ensure` |
| `SceneHudAuthoring.IsActive` warnings | Host finds baked prefab instance first |

---

## 12. Success Metrics

| Metric | Before | Target |
|--------|--------|--------|
| Banner-cluster LOC | ~1,560 | ~900–1,100 |
| `FindHudCanvas` copies | 8 | 1 |
| Code-generated banner hierarchies | 7 | 0 |
| ThrowController banner `[SerializeField]` count | 6 + legacy GO | 1 host |
| Behavior matrix B01–B10 | — | All pass |

---

## 13. Approval

| Role | Sign-off |
|------|----------|
| Technical Director | Pending |
| Design | Pending |
| QA | Pending |

**Next step after approval:** Implementation plan at `docs/superpowers/plans/2026-05-31-callout-banner-consolidation.md`

# Disk Golf Throw Prototype — MVP Design Spec

**Date:** 2026-05-22  
**Status:** Approved (brainstorming)  
**Engine:** Unity (3D greybox)  
**Inspiration:** Neo Turf Masters (1996, Nazca/SNK) — arcade golf mechanics re-themed as disc golf

---

## 1. Vision

Build an arcade disc golf game that captures the feel of **Neo Turf Masters**: fast sessions, simple two-meter timing input, pre-selected shot shaping, wind as a core skill, and dramatic camera cuts — but with disc golf identity (Hyzer/Flat/Anhyzer, disc flight numbers, basket targets).

This spec covers **MVP Phase 1 only**: a throw-feel prototype on a single open par-3 test hole. No menus, scoring, characters, or cutscenes.

---

## 2. Design Decisions (Locked)

| Decision | Choice | Rationale |
|----------|--------|-----------|
| MVP scope | Throw-feel prototype | Validate core loop before building meta-game |
| Test hole | Open par-3 (~250 ft) | Minimal hazards; focus on mechanics |
| Camera | Full NTM suite (4 cameras) | Side setup → top-down flight → lie zoom → overhead putt |
| Flight model | Hybrid arcade + disc stats | NTM meter input on surface; disc golf flight numbers underneath |
| STANCE labels | Hyzer / Flat / Anhyzer | Authentic disc golf terminology |
| Architecture | 3D greybox + kinematic flight paths | Natural arcs, Cinemachine cameras, tunable arcade feel |
| Art (MVP) | Greybox primitives | Placeholder visuals; pixel-art swap later |

---

## 3. Reference: Neo Turf Masters Core Systems

Systems to preserve (adapted for disc golf):

| NTM System | Disk Golf Equivalent |
|------------|---------------------|
| 14-club bag | 4-disc bag (Putter, Mid, Fairway, Distance) |
| Hook / Slice pre-select | Hyzer / Flat / Anhyzer pre-select |
| Power meter + Height meter | Same two-meter timing loop |
| Wind 0–15 | Same; interacts with throw height |
| Auto-aim at pin | Auto-aim at basket |
| Trajectory preview arrow | Green curved arrow from flight preview |
| Side view → top-down flight → lie zoom → putt | Same camera flow |
| Character stats (later) | Thrower archetypes affecting meter speed, power window |
| Terrain lies (later) | Tee / Fairway / Rough / Circle / OB |
| Arcade lives / scoring (later) | Stroke play, match play, birdie/bogey cutscenes |

---

## 4. Disc Golf Domain Model

### 4.1 Disc Categories

| Category | Speed Range | Role | MVP Max Distance |
|----------|-------------|------|------------------|
| Putter | 1–3 | Short control, putting | 80 ft |
| Midrange | 4–5 | Versatile tee/approach | 250 ft |
| Fairway Driver | 6–7 | Controlled distance | 320 ft |
| Distance Driver | 8–14 | Maximum distance | 420 ft |

### 4.2 Flight Numbers (stored from day one)

Industry-standard four-number system (RHBH reference):

| Stat | Range | Effect |
|------|-------|--------|
| **Speed** | 1–14 | Power required; underpower → less distance, more turn |
| **Glide** | 1–7 | Hang time; higher glide → more distance potential |
| **Turn** | +1 to −5 | Early flight drift right; negative = understable |
| **Fade** | 0–5 | Late flight hook left; high fade = reliable hyzer finish |

**Stability shorthand:** overstable (high fade, low turn), stable/neutral, understable (high negative turn).

Factors deferred post-MVP: weight, plastic type, beat-in wear, forehand vs backhand stance.

### 4.3 MVP Disc Roster

| Name | Category | Flight Numbers | Max Dist | Notes |
|------|----------|----------------|----------|-------|
| P2 | Putter | 2/3/0/1 | 80 ft | Straight, forgiving |
| Buzzz | Midrange | 5/4/−1/1 | 250 ft | Default tee disc for test hole |
| Teebird | Fairway | 7/5/0/2 | 320 ft | Controlled, reliable fade |
| Destroyer | Distance | 12/5/−1/3 | 420 ft | Punishes underpower |

---

## 5. Core Throw Loop

### 5.1 State Machine

```
AIMING → POWER_METER → HEIGHT_METER → THROWING → IN_FLIGHT → LANDED → [PUTTING?] → RESOLVE
```

| State | Description |
|-------|-------------|
| **AIMING** | Side camera. Select disc, set Hyzer/Flat/Anhyzer. Trajectory preview visible. Wind displayed. |
| **POWER_METER** | Semi-circular oscillating gauge. Player confirms at 50%–110% (110% = max shot zone, NTM-style). |
| **HEIGHT_METER** | Vertical LOW / NICE / HIGH bar oscillates. Timing sets release angle / nose attitude. |
| **THROWING** | Brief throw animation (~0.5s), then camera cut. |
| **IN_FLIGHT** | Top-down camera tracks disc. Live `DRIVE ###ft` and `REST ###ft` counters. |
| **LANDED** | Lie zoom camera (0.5–1s close-up on disc). Detect lie type. |
| **PUTTING** | If within circle (~33 ft): overhead camera, simplified single meter, putter auto-selected. |
| **RESOLVE** | Ready for next throw or hole complete. Prototype: press R to reset to tee. No scoring UI. |

### 5.2 Input (Keyboard MVP)

| Action | Key |
|--------|-----|
| Confirm meter / advance | Space |
| Cycle disc | Q / E (or 1–4 direct select) |
| Stance: Hyzer / Flat / Anhyzer | ← / ↓ / → |
| Reset hole | R |

Gamepad mapping deferred.

### 5.3 HUD Layout

```
┌─────────────────────────────────────────────┐
│ REST 250ft                          [minimap]│
│                                     hole info│
│                                              │
│         [3D course view / active camera]     │
│              ↗ green trajectory arrow       │
│                                              │
│ STANCE     DISC          WIND    [power meter]│
│ HYZ/FLT/AN  Buzzz 5/4/-1/1  ↗ 8m   LOW/NICE/HIGH│
└─────────────────────────────────────────────┘
```

HUD elements mirror NTM: distance remaining, minimap with dashed aim line, wind box, stance panel, disc info with flight numbers, semi-circular power meter with LOW/NICE/HIGH zones.

---

## 6. Flight Simulation

### 6.1 Architecture

```
ThrowInput                    FlightSimulator              FlightPath
├── disc: DiscProfile    →    Compute(input)         →    ├── waypoints[]
├── releaseAngle              (kinematic, tunable)         ├── totalDistanceFt
├── power: 0.0–1.1                                           ├── landingLie
├── height: Low|Nice|High                                    └── flightShape
└── wind: direction + speed
```

Disc follows computed waypoints — **not** raw Rigidbody physics. Deterministic, arcade-predictable. Physics-based simulation can replace `FlightSimulator` later without changing input/UI.

### 6.2 DiscProfile (ScriptableObject)

```csharp
// Conceptual schema
DiscProfile {
    string displayName;
    DiscCategory category;      // Putter | Mid | Fairway | Distance
    int speed, glide, turn, fade;
    float maxDistanceFt;
    float meterSpeedMod;        // 1.0 default; character hook later
    Sprite icon;                // placeholder
}
```

Location: `Assets/Data/Discs/`

### 6.3 Distance Calculation

```
baseDist       = disc.maxDistanceFt × power
glideBonus     = f(disc.glide, height)    // Nice=1.0, Low=0.85, High=1.1
actualDist     = baseDist × glideBonus × liePenalty

// Underpower penalty (speed stat)
requiredPower  = disc.speed / 14
if power < requiredPower:
    turnBoost         += 1.5
    distancePenalty   = 0.7
    actualDist       *= distancePenalty
```

### 6.4 Lateral Curve (Turn + Fade)

```
turnAmount = disc.turn × releaseAngleMod(hyzer, flat, anhyzer) + turnBoost
fadeAmount = disc.fade × (1 - heightMod)

// turnPhase: peaks early in flight (0–40%)
// fadePhase: peaks late in flight (60–100%)
curve(t) = turnPhase(t, turnAmount) + fadePhase(t, fadeAmount)
```

**Release angle modifiers (RHBH):**

| Stance | Turn effect | Fade effect |
|--------|-------------|-------------|
| Hyzer | Dampens turn (×0.5) | Amplifies fade (×1.3) |
| Flat | Neutral | Neutral |
| Anhyzer | Amplifies turn (×1.5) | Dampens fade (×0.7) |

### 6.5 Wind

```
windDrift = windVector × (disc.speed / 14) × heightExposure(t)
// heightExposure: 0 at ground, 1 at apex; High throws spend more time exposed
```

Wind speed: 0–15 mph, random direction each throw (NTM-style).

### 6.6 Ground Interaction

- Disc lands at path terminus; position from waypoints
- **Skip/roll:** stubbed at 0 for MVP; hook exists for `low height + high power + low fade`
- **Lie detection:** terrain tag raycast → Tee / Fairway / Rough / Circle

### 6.7 Trajectory Preview

During AIMING, green curved arrow renders preview of `FlightSimulator.Compute()` assuming Nice height and 100% power. Updates live on disc or stance change. Actual throw uses real meter results.

### 6.8 Putting (Simplified)

Triggered when `REST ≤ 33 ft` and lie = Circle:

- Overhead putt camera
- Putter auto-selected
- Single oscillating power meter (no height bar)
- Hyzer/Flat/Anhyzer available but minimal effect at short range
- Overpower → disc overshoots basket

---

## 7. Camera System

### 7.1 Camera States

| State | Camera | Behavior |
|-------|--------|----------|
| AIMING | `SideSetupCam` | Side/behind thrower, NTM angle. Player, arrow, full HUD visible. |
| IN_FLIGHT | `TopDownTrackCam` | Follows disc from above. Minimap and distance counters active. |
| LANDED | `LieZoomCam` | Brief close-up on disc in lie (0.5–1s), then transition. |
| PUTTING | `OverheadPuttCam` | Directly above basket + disc. "IN THE CIRCLE" overlay (placeholder text). |

### 7.2 Implementation

- Unity **Cinemachine** virtual cameras, one per state
- `CameraDirector` listens to throw state machine
- Blends: ~0.3s hard cut or 0.5s blend between states
- All cameras target same 3D world space (consistent disc position across views)

---

## 8. Test Hole: "Prototype Flat 3"

```
Par 3 — 250 ft — Open

[Tee Pad] ──── 250 ft fairway ──── [Basket]
     │                                  │
  flat grass                      33 ft putting circle
  rough border (visual only)      flat green, no elevation
  no trees, water, or OB
```

**Goals:**

- Validate disc distance caps across all 4 disc types
- Validate hyzer/flat/anhyzer curve differences
- Validate wind drift
- Validate two-meter timing feel
- Validate all 4 camera transitions in one throw cycle
- Validate putt flow from circle range

**Wind:** variable 0–15 mph, random direction per throw.

**Visuals:** greybox — green fairway plane, tan tee pad, yellow basket placeholder (cylinder + chain hint), blue skybox.

---

## 9. Unity Project Structure

```
Assets/
├── Scripts/
│   ├── Core/           GameState, ThrowStateMachine
│   ├── Disc/           DiscProfile, DiscBag, DiscCategory
│   ├── Flight/         FlightSimulator, FlightPath, ThrowInput, TurnFadePhases
│   ├── Input/          ThrowInputHandler
│   ├── Camera/         CameraDirector
│   └── UI/             HUDController, PowerMeter, HeightMeter, TrajectoryArrow, Minimap
├── Data/
│   └── Discs/          4 DiscProfile ScriptableObject assets
├── Scenes/
│   └── PrototypeFlat3  single test scene
└── Prefabs/
    ├── Disc            flying disc visual
    ├── Basket          target
    └── TeePad          tee marker
```

### Key Dependencies

- Unity 2022 LTS or Unity 6
- Cinemachine package
- Input System (optional; legacy Input Manager acceptable for MVP)

---

## 10. MVP Success Criteria

The prototype is **done** when a player can:

1. Select any of 4 discs and see the trajectory preview arrow update
2. Set Hyzer / Flat / Anhyzer and see the preview curve change
3. Complete both timing meters and watch the disc fly a believable path
4. Experience all 4 camera transitions in one throw cycle
5. Feel a meaningful distance gap between Destroyer (~350+ ft) and Putter (80 ft cap)
6. See wind affect flight direction and compensate for it
7. Putt from circle range using the simplified putt meter
8. Reset and retry instantly with R

---

## 11. Out of Scope (MVP)

- Title screen, character select, course select
- Scoring, par tracking, birdie/bogey/ace cutscenes
- Arcade lives / continue system
- Match play, multiplayer
- Hazard logic (OB, water, mandos)
- Rough lie penalties (tags exist; penalties not enforced)
- Spin, roller, flex shot tuning as named mechanics
- Audio, music, pixel art, character animations
- Gamepad input
- Weight, plastic, beat-in disc progression

---

## 12. Long-Term Roadmap (Post-MVP)

Phases after throw prototype validates:

| Phase | Features |
|-------|----------|
| **2 — Scoring & hole flow** | Par, stroke count, hole preview screen, scorecard, result cutscenes |
| **3 — Characters** | 6 thrower archetypes with stat spreads (Drive, Accuracy, Skill, Recovery, Putting) |
| **4 — Course content** | Hazard logic, 4 biome courses, dogleg holes, elevation |
| **5 — Meta-game** | Arcade lives, stroke/match play, character select, course select |
| **6 — Polish** | Pixel art presentation, audio, animations, gamepad, skip/roll/flex tuning |

Data architecture (`DiscProfile`, `ThrowInput`, `FlightSimulator` interface) is designed so later phases add content and modifiers without rewriting the throw loop.

---

## 13. Future Visual Target

Reference screenshots establish the production art direction:

- 16-bit Neo Geo pixel art aesthetic
- Character reaction cutscenes (Birdie/Bogey)
- Isometric hole preview diorama
- Dense arcade HUD with minimap, wind, stance, disc info, power meter
- "ON THE GREEN" → "IN THE CIRCLE" state overlays

MVP greybox defers all of this; camera positions and HUD layout are designed to accommodate pixel-art UI swap.

---

## 14. Open Questions (Resolved)

| Question | Resolution |
|----------|------------|
| MVP scope | Throw-feel prototype, 1 hole |
| Camera | Full 4-camera NTM suite |
| Flight model | Hybrid: NTM meters + disc flight numbers |
| Terminology | Hyzer / Flat / Anhyzer |
| Test hole | Open par-3, 250 ft |
| Architecture | 3D greybox + kinematic paths (Approach 2) |

No open questions remain for MVP Phase 1.

# Disk Golf Throw Prototype — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Unity throw-feel prototype on one open par-3 hole with NTM-style two-meter input, hyzer/flat/anhyzer shaping, 4 disc types, wind, full 4-camera flow, and simplified putting.

**Architecture:** 3D greybox course with kinematic flight paths computed by `FlightSimulator` from `DiscProfile` + `ThrowInput`. A `ThrowStateMachine` drives gameplay phases; `CameraDirector` swaps Cinemachine virtual cameras per phase. Pure flight logic is EditMode-testable; MonoBehaviours handle scene presentation.

**Tech Stack:** Unity 2022 LTS or Unity 6, C#, Cinemachine, Unity Test Framework (EditMode), Legacy Input Manager (MVP)

**Spec:** `docs/superpowers/specs/2026-05-22-disk-golf-throw-prototype-design.md`

---

## File Map

| File | Responsibility |
|------|----------------|
| `Assets/Scripts/Disc/DiscCategory.cs` | Enum: Putter, Mid, Fairway, Distance |
| `Assets/Scripts/Disc/DiscProfile.cs` | ScriptableObject with flight numbers + max distance |
| `Assets/Scripts/Disc/DiscBag.cs` | Holds 4 MVP disc references |
| `Assets/Scripts/Flight/ReleaseAngle.cs` | Enum: Hyzer, Flat, Anhyzer |
| `Assets/Scripts/Flight/ThrowHeight.cs` | Enum: Low, Nice, High |
| `Assets/Scripts/Flight/LieType.cs` | Enum: Tee, Fairway, Rough, Circle, OB |
| `Assets/Scripts/Flight/ThrowInput.cs` | Immutable input struct for one throw |
| `Assets/Scripts/Flight/FlightWaypoint.cs` | Position + time sample on path |
| `Assets/Scripts/Flight/FlightPath.cs` | Waypoints, distance, lie, shape |
| `Assets/Scripts/Flight/FlightShape.cs` | Enum: Straight, Turn, Fade, SCurve |
| `Assets/Scripts/Flight/FlightSimulator.cs` | Static kinematic path computation |
| `Assets/Scripts/Flight/WindSettings.cs` | Direction + speed struct |
| `Assets/Scripts/Core/ThrowPhase.cs` | Enum of throw loop phases |
| `Assets/Scripts/Core/ThrowStateMachine.cs` | Phase transitions + events |
| `Assets/Scripts/Core/ThrowController.cs` | Orchestrates input, sim, disc movement |
| `Assets/Scripts/Core/HoleSetup.cs` | Tee, basket, circle radius, wind roll |
| `Assets/Scripts/Input/ThrowInputHandler.cs` | Keyboard → stance, disc, meter confirm |
| `Assets/Scripts/Camera/CameraDirector.cs` | Cinemachine camera switching |
| `Assets/Scripts/Gameplay/DiscFlightPresenter.cs` | Moves disc along FlightPath in world space |
| `Assets/Scripts/Gameplay/LieDetector.cs` | Terrain tag → LieType |
| `Assets/Scripts/UI/HUDController.cs` | Binds HUD to ThrowController state |
| `Assets/Scripts/UI/PowerMeterUI.cs` | Oscillating semi-circle meter |
| `Assets/Scripts/UI/HeightMeterUI.cs` | LOW/NICE/HIGH oscillating bar |
| `Assets/Scripts/UI/TrajectoryPreview.cs` | Green arrow from preview FlightPath |
| `Assets/Scripts/UI/MinimapUI.cs` | Top-down hole line + disc dot |
| `Assets/Tests/EditMode/FlightSimulatorTests.cs` | EditMode unit tests |
| `Assets/Data/Discs/*.asset` | 4 DiscProfile assets |
| `Assets/Scenes/PrototypeFlat3.unity` | Test scene |

---

### Task 1: Unity Project Bootstrap

**Files:**
- Create: Unity project at repo root (alongside `docs/`)
- Create: `.gitignore` (Unity template)
- Modify: `Packages/manifest.json` (add Cinemachine + Test Framework)

- [ ] **Step 1: Create Unity 3D project**

In Unity Hub: New Project → 3D (URP or Built-in both fine; Built-in is simpler for greybox MVP) → name `DiskGolf` or use repo root directly.

Alternatively from CLI (if Unity installed):
```bash
/Applications/Unity/Hub/Editor/<VERSION>/Unity.app/Contents/MacOS/Unity \
  -createProject "/Users/markunruh/Desktop/Disk Golf" \
  -quit -batchmode
```

- [ ] **Step 2: Initialize git**

```bash
cd "/Users/markunruh/Desktop/Disk Golf"
git init
```

Add standard Unity `.gitignore` (from [github.com/github/gitignore](https://github.com/github/gitignore/blob/main/Unity.gitignore)).

- [ ] **Step 3: Add packages**

Edit `Packages/manifest.json` — add to `dependencies`:
```json
"com.unity.cinemachine": "2.10.0",
"com.unity.test-framework": "1.1.33"
```

Open project in Unity; wait for package import.

- [ ] **Step 4: Create folder structure**

```
Assets/Scripts/{Core,Disc,Flight,Input,Camera,Gameplay,UI}
Assets/Data/Discs
Assets/Scenes
Assets/Prefabs
Assets/Tests/EditMode
```

- [ ] **Step 5: Commit**

```bash
git add .gitignore Packages/manifest.json Assets/
git commit -m "chore: bootstrap Unity project with Cinemachine and Test Framework"
```

---

### Task 2: Disc Domain Types

**Files:**
- Create: `Assets/Scripts/Disc/DiscCategory.cs`
- Create: `Assets/Scripts/Disc/DiscProfile.cs`
- Create: `Assets/Scripts/Disc/DiscBag.cs`

- [ ] **Step 1: Create DiscCategory.cs**

```csharp
namespace DiskGolf.Disc
{
    public enum DiscCategory
    {
        Putter,
        Mid,
        Fairway,
        Distance
    }
}
```

- [ ] **Step 2: Create DiscProfile.cs**

```csharp
using UnityEngine;

namespace DiskGolf.Disc
{
    [CreateAssetMenu(fileName = "DiscProfile", menuName = "Disk Golf/Disc Profile")]
    public class DiscProfile : ScriptableObject
    {
        public string displayName = "Buzzz";
        public DiscCategory category = DiscCategory.Mid;
        [Range(1, 14)] public int speed = 5;
        [Range(1, 7)] public int glide = 4;
        [Range(-5, 1)] public int turn = -1;
        [Range(0, 5)] public int fade = 1;
        public float maxDistanceFt = 250f;
        public float meterSpeedMod = 1f;
    }
}
```

- [ ] **Step 3: Create DiscBag.cs**

```csharp
using UnityEngine;

namespace DiskGolf.Disc
{
    public class DiscBag : MonoBehaviour
    {
        [SerializeField] DiscProfile[] discs = new DiscProfile[4];
        int _index;

        public DiscProfile Active => discs[_index];
        public DiscProfile[] All => discs;

        public void SelectIndex(int index)
        {
            _index = Mathf.Clamp(index, 0, discs.Length - 1);
        }

        public void CycleNext() => SelectIndex((_index + 1) % discs.Length);
        public void CyclePrev() => SelectIndex((_index - 1 + discs.Length) % discs.Length);
    }
}
```

- [ ] **Step 4: Create 4 DiscProfile assets in Unity Editor**

| Asset name | displayName | category | speed/glide/turn/fade | maxDistanceFt |
|------------|-------------|----------|----------------------|---------------|
| P2 | P2 | Putter | 2/3/0/1 | 80 |
| Buzzz | Buzzz | Mid | 5/4/-1/1 | 250 |
| Teebird | Teebird | Fairway | 7/5/0/2 | 320 |
| Destroyer | Destroyer | Distance | 12/5/-1/3 | 420 |

Save to `Assets/Data/Discs/`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Disc/ Assets/Data/Discs/
git commit -m "feat: add DiscProfile ScriptableObject and DiscBag"
```

---

### Task 3: Flight Types

**Files:**
- Create: `Assets/Scripts/Flight/ReleaseAngle.cs`
- Create: `Assets/Scripts/Flight/ThrowHeight.cs`
- Create: `Assets/Scripts/Flight/LieType.cs`
- Create: `Assets/Scripts/Flight/FlightShape.cs`
- Create: `Assets/Scripts/Flight/WindSettings.cs`
- Create: `Assets/Scripts/Flight/ThrowInput.cs`
- Create: `Assets/Scripts/Flight/FlightWaypoint.cs`
- Create: `Assets/Scripts/Flight/FlightPath.cs`

- [ ] **Step 1: Create enums and WindSettings**

```csharp
// ReleaseAngle.cs
namespace DiskGolf.Flight
{
    public enum ReleaseAngle { Hyzer = -1, Flat = 0, Anhyzer = 1 }
}
```

```csharp
// ThrowHeight.cs
namespace DiskGolf.Flight
{
    public enum ThrowHeight { Low, Nice, High }
}
```

```csharp
// LieType.cs
namespace DiskGolf.Flight
{
    public enum LieType { Tee, Fairway, Rough, Circle, OB }
}
```

```csharp
// FlightShape.cs
namespace DiskGolf.Flight
{
    public enum FlightShape { Straight, Turn, Fade, SCurve }
}
```

```csharp
// WindSettings.cs
using UnityEngine;

namespace DiskGolf.Flight
{
    [System.Serializable]
    public struct WindSettings
    {
        public Vector2 direction; // normalized XZ
        public float speedMph;      // 0-15

        public Vector3 DriftVector => new Vector3(direction.x, 0f, direction.y) * speedMph;
    }
}
```

- [ ] **Step 2: Create ThrowInput, FlightWaypoint, FlightPath**

```csharp
// ThrowInput.cs
using DiskGolf.Disc;

namespace DiskGolf.Flight
{
    public readonly struct ThrowInput
    {
        public DiscProfile Disc { get; }
        public ReleaseAngle ReleaseAngle { get; }
        public float Power { get; }          // 0.0 - 1.1
        public ThrowHeight Height { get; }
        public WindSettings Wind { get; }
        public UnityEngine.Vector3 Origin { get; }
        public UnityEngine.Vector3 AimDirection { get; } // normalized XZ toward basket

        public ThrowInput(
            DiscProfile disc,
            ReleaseAngle releaseAngle,
            float power,
            ThrowHeight height,
            WindSettings wind,
            UnityEngine.Vector3 origin,
            UnityEngine.Vector3 aimDirection)
        {
            Disc = disc;
            ReleaseAngle = releaseAngle;
            Power = power;
            Height = height;
            Wind = wind;
            Origin = origin;
            AimDirection = aimDirection.normalized;
        }
    }
}
```

```csharp
// FlightWaypoint.cs
using UnityEngine;

namespace DiskGolf.Flight
{
    public readonly struct FlightWaypoint
    {
        public Vector3 Position { get; }
        public float Time { get; }

        public FlightWaypoint(Vector3 position, float time)
        {
            Position = position;
            Time = time;
        }
    }
}
```

```csharp
// FlightPath.cs
using System.Collections.Generic;

namespace DiskGolf.Flight
{
    public sealed class FlightPath
    {
        public IReadOnlyList<FlightWaypoint> Waypoints => _waypoints;
        public float TotalDistanceFt { get; }
        public LieType LandingLie { get; }
        public FlightShape Shape { get; }

        readonly List<FlightWaypoint> _waypoints;

        public FlightPath(
            List<FlightWaypoint> waypoints,
            float totalDistanceFt,
            LieType landingLie,
            FlightShape shape)
        {
            _waypoints = waypoints;
            TotalDistanceFt = totalDistanceFt;
            LandingLie = landingLie;
            Shape = shape;
        }
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Flight/
git commit -m "feat: add flight domain types (ThrowInput, FlightPath)"
```

---

### Task 4: FlightSimulator — Failing Tests

**Files:**
- Create: `Assets/Tests/EditMode/FlightSimulatorTests.cs`
- Create: `Assets/Tests/EditMode/DiskGolf.Tests.EditMode.asmdef`
- Create: `Assets/Scripts/DiskGolf.asmdef` (main scripts assembly)

- [ ] **Step 1: Create assembly definitions**

`Assets/Scripts/DiskGolf.asmdef`:
```json
{
    "name": "DiskGolf",
    "rootNamespace": "DiskGolf",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false
}
```

`Assets/Tests/EditMode/DiskGolf.Tests.EditMode.asmdef`:
```json
{
    "name": "DiskGolf.Tests.EditMode",
    "rootNamespace": "DiskGolf.Tests",
    "references": ["DiskGolf", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
    "includePlatforms": ["Editor"],
    "optionalUnityReferences": ["TestAssemblies"]
}
```

- [ ] **Step 2: Write failing tests**

```csharp
using DiskGolf.Disc;
using DiskGolf.Flight;
using NUnit.Framework;
using UnityEngine;

namespace DiskGolf.Tests
{
    public class FlightSimulatorTests
    {
        DiscProfile MakeDisc(int speed, int glide, int turn, int fade, float maxFt)
        {
            var d = ScriptableObject.CreateInstance<DiscProfile>();
            d.speed = speed;
            d.glide = glide;
            d.turn = turn;
            d.fade = fade;
            d.maxDistanceFt = maxFt;
            return d;
        }

        ThrowInput MakeInput(DiscProfile disc, ReleaseAngle angle, float power,
            ThrowHeight height, float windSpeed = 0f)
        {
            return new ThrowInput(
                disc, angle, power, height,
                new WindSettings { direction = Vector2.right, speedMph = windSpeed },
                Vector3.zero,
                Vector3.forward);
        }

        [Test]
        public void Compute_FullPowerMid_DistanceNearMax()
        {
            var buzzz = MakeDisc(5, 4, -1, 1, 250f);
            var path = FlightSimulator.Compute(MakeInput(buzzz, ReleaseAngle.Flat, 1f, ThrowHeight.Nice));
            Assert.That(path.TotalDistanceFt, Is.InRange(200f, 280f));
        }

        [Test]
        public void Compute_Anhyzer_MoreTurnThanHyzer()
        {
            var disc = MakeDisc(5, 4, -2, 1, 250f);
            var hyzer = FlightSimulator.Compute(MakeInput(disc, ReleaseAngle.Hyzer, 1f, ThrowHeight.Nice));
            var anhyzer = FlightSimulator.Compute(MakeInput(disc, ReleaseAngle.Anhyzer, 1f, ThrowHeight.Nice));
            float HyzerLateral(FlightPath p) => p.Waypoints[p.Waypoints.Count / 3].Position.x;
            Assert.That(Mathf.Abs(HyzerLateral(anhyzer)), Is.GreaterThan(Mathf.Abs(HyzerLateral(hyzer))));
        }

        [Test]
        public void Compute_UnderpoweredDriver_ShorterThanFullPower()
        {
            var destroyer = MakeDisc(12, 5, -1, 3, 420f);
            var full = FlightSimulator.Compute(MakeInput(destroyer, ReleaseAngle.Flat, 1f, ThrowHeight.Nice));
            var weak = FlightSimulator.Compute(MakeInput(destroyer, ReleaseAngle.Flat, 0.5f, ThrowHeight.Nice));
            Assert.That(weak.TotalDistanceFt, Is.LessThan(full.TotalDistanceFt * 0.75f));
        }

        [Test]
        public void Compute_WindDriftsDownwind()
        {
            var disc = MakeDisc(5, 4, 0, 1, 250f);
            var noWind = FlightSimulator.Compute(MakeInput(disc, ReleaseAngle.Flat, 1f, ThrowHeight.Nice, 0f));
            var wind = FlightSimulator.Compute(MakeInput(disc, ReleaseAngle.Flat, 1f, ThrowHeight.High, 10f));
            var noWindEnd = noWind.Waypoints[noWind.Waypoints.Count - 1].Position;
            var windEnd = wind.Waypoints[wind.Waypoints.Count - 1].Position;
            Assert.That(windEnd.x, Is.GreaterThan(noWindEnd.x + 1f));
        }
    }
}
```

- [ ] **Step 3: Create stub FlightSimulator.cs (empty Compute throws)**

```csharp
// Assets/Scripts/Flight/FlightSimulator.cs
namespace DiskGolf.Flight
{
    public static class FlightSimulator
    {
        public static FlightPath Compute(ThrowInput input)
        {
            throw new System.NotImplementedException();
        }
    }
}
```

- [ ] **Step 4: Run tests — expect FAIL**

Unity Editor → Window → General → Test Runner → EditMode → Run All  
Expected: 4 failures (`NotImplementedException`)

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Flight/FlightSimulator.cs Assets/Tests/ Assets/Scripts/DiskGolf.asmdef
git commit -m "test: add FlightSimulator EditMode tests (failing)"
```

---

### Task 5: FlightSimulator — Implementation

**Files:**
- Modify: `Assets/Scripts/Flight/FlightSimulator.cs`

- [ ] **Step 1: Implement FlightSimulator.Compute**

```csharp
using System.Collections.Generic;
using DiskGolf.Disc;
using UnityEngine;

namespace DiskGolf.Flight
{
    public static class FlightSimulator
    {
        const int WaypointCount = 32;
        const float FtToUnity = 0.3048f; // 1 ft in meters (Unity units = meters)

        public static FlightPath Compute(ThrowInput input)
        {
            float power = Mathf.Clamp(input.Power, 0f, 1.1f);
            float glideBonus = input.Height switch
            {
                ThrowHeight.Low => 0.85f,
                ThrowHeight.Nice => 1.0f,
                ThrowHeight.High => 1.1f,
                _ => 1f
            };

            float requiredPower = input.Disc.speed / 14f;
            float turnBoost = 0f;
            float distancePenalty = 1f;
            if (power < requiredPower)
            {
                turnBoost = 1.5f;
                distancePenalty = 0.7f;
            }

            float distanceFt = input.Disc.maxDistanceFt * power * glideBonus * distancePenalty;

            float turnMod = input.ReleaseAngle switch
            {
                ReleaseAngle.Hyzer => 0.5f,
                ReleaseAngle.Anhyzer => 1.5f,
                _ => 1f
            };
            float fadeMod = input.ReleaseAngle switch
            {
                ReleaseAngle.Hyzer => 1.3f,
                ReleaseAngle.Anhyzer => 0.7f,
                _ => 1f
            };
            float heightFadeMod = input.Height == ThrowHeight.High ? 0.85f : 1f;

            float turnAmount = input.Disc.turn * turnMod - turnBoost;
            float fadeAmount = input.Disc.fade * fadeMod * heightFadeMod;

            Vector3 forward = input.AimDirection.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 wind = input.Wind.DriftVector * (input.Disc.speed / 14f) * FtToUnity;

            var waypoints = new List<FlightWaypoint>(WaypointCount);
            float totalTime = 2.5f;
            float maxLateral = 0f;

            for (int i = 0; i < WaypointCount; i++)
            {
                float t = i / (float)(WaypointCount - 1);
                float dist = distanceFt * t * FtToUnity;
                float turnPhase = TurnPhase(t) * turnAmount * FtToUnity;
                float fadePhase = FadePhase(t) * fadeAmount * FtToUnity;
                float lateral = turnPhase + fadePhase;
                maxLateral = Mathf.Max(maxLateral, Mathf.Abs(lateral));

                float heightExposure = HeightExposure(t, input.Height);
                Vector3 windOffset = wind * heightExposure * t;

                Vector3 pos = input.Origin
                    + forward * dist
                    + right * lateral
                    + windOffset;
                pos.y = ArcHeight(t, input.Height) * FtToUnity;

                waypoints.Add(new FlightWaypoint(pos, t * totalTime));
            }

            var shape = ClassifyShape(turnAmount, fadeAmount, maxLateral);
            return new FlightPath(waypoints, distanceFt, LieType.Fairway, shape);
        }

        static float TurnPhase(float t) => t <= 0.4f ? Mathf.Sin(t / 0.4f * Mathf.PI * 0.5f) : 0f;
        static float FadePhase(float t) => t >= 0.6f ? Mathf.Sin((t - 0.6f) / 0.4f * Mathf.PI * 0.5f) : 0f;

        static float HeightExposure(float t, ThrowHeight height)
        {
            float baseExposure = Mathf.Sin(t * Mathf.PI);
            return height == ThrowHeight.High ? baseExposure * 1.3f : baseExposure;
        }

        static float ArcHeight(float t, ThrowHeight height)
        {
            float amp = height switch
            {
                ThrowHeight.Low => 2f,
                ThrowHeight.Nice => 5f,
                ThrowHeight.High => 10f,
                _ => 5f
            };
            return amp * Mathf.Sin(t * Mathf.PI);
        }

        static FlightShape ClassifyShape(float turn, float fade, float maxLateral)
        {
            if (maxLateral < 0.5f) return FlightShape.Straight;
            if (turn < -0.5f && fade > 0.5f) return FlightShape.SCurve;
            if (turn < -0.5f) return FlightShape.Turn;
            return FlightShape.Fade;
        }
    }
}
```

- [ ] **Step 2: Run tests — expect PASS**

Test Runner → EditMode → Run All  
Expected: 4 passed

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Flight/FlightSimulator.cs
git commit -m "feat: implement kinematic FlightSimulator"
```

---

### Task 6: Throw State Machine

**Files:**
- Create: `Assets/Scripts/Core/ThrowPhase.cs`
- Create: `Assets/Scripts/Core/ThrowStateMachine.cs`

- [ ] **Step 1: Create ThrowPhase enum**

```csharp
namespace DiskGolf.Core
{
    public enum ThrowPhase
    {
        Aiming,
        PowerMeter,
        HeightMeter,
        Throwing,
        InFlight,
        Landed,
        Putting,
        Resolve
    }
}
```

- [ ] **Step 2: Create ThrowStateMachine**

```csharp
using System;

namespace DiskGolf.Core
{
    public class ThrowStateMachine
    {
        public ThrowPhase Phase { get; private set; } = ThrowPhase.Aiming;
        public event Action<ThrowPhase> PhaseChanged;

        public void TransitionTo(ThrowPhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        public void Advance()
        {
            TransitionTo(Phase switch
            {
                ThrowPhase.Aiming => ThrowPhase.PowerMeter,
                ThrowPhase.PowerMeter => ThrowPhase.HeightMeter,
                ThrowPhase.HeightMeter => ThrowPhase.Throwing,
                ThrowPhase.Throwing => ThrowPhase.InFlight,
                ThrowPhase.InFlight => ThrowPhase.Landed,
                ThrowPhase.Landed => ThrowPhase.Resolve,
                ThrowPhase.Putting => ThrowPhase.Resolve,
                ThrowPhase.Resolve => ThrowPhase.Aiming,
                _ => ThrowPhase.Aiming
            });
        }

        public void EnterPutting() => TransitionTo(ThrowPhase.Putting);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/
git commit -m "feat: add ThrowStateMachine"
```

---

### Task 7: Timing Meters

**Files:**
- Create: `Assets/Scripts/Core/TimingMeter.cs`
- Create: `Assets/Scripts/UI/PowerMeterUI.cs`
- Create: `Assets/Scripts/UI/HeightMeterUI.cs`

- [ ] **Step 1: Create TimingMeter (pure logic)**

```csharp
using UnityEngine;

namespace DiskGolf.Core
{
    public class TimingMeter
    {
        readonly float _speed;
        float _value;
        int _direction = 1;

        public TimingMeter(float speed = 1f) => _speed = speed;

        public float Value => _value;

        public void Tick(float deltaTime)
        {
            _value += _direction * _speed * deltaTime;
            if (_value >= 1.1f) { _value = 1.1f; _direction = -1; }
            if (_value <= 0f) { _value = 0f; _direction = 1; }
        }

        public void Reset(float start = 0f) { _value = start; _direction = 1; }

        public float Confirm() => _value;
    }
}
```

- [ ] **Step 2: Create HeightMeterUI zone resolver**

```csharp
using DiskGolf.Flight;

namespace DiskGolf.Core
{
    public static class HeightMeterZones
    {
        public static ThrowHeight FromValue(float v)
        {
            if (v < 0.33f) return ThrowHeight.Low;
            if (v > 0.66f) return ThrowHeight.High;
            return ThrowHeight.Nice;
        }
    }
}
```

- [ ] **Step 3: Create PowerMeterUI MonoBehaviour**

```csharp
using DiskGolf.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI
{
    public class PowerMeterUI : MonoBehaviour
    {
        [SerializeField] Slider slider;
        TimingMeter _meter = new TimingMeter(0.8f);
        bool _active;

        public void Begin() { _meter.Reset(); _active = true; }
        public void Stop() => _active = false;
        public float Confirm() { _active = false; return _meter.Confirm(); }

        void Update()
        {
            if (!_active) return;
            _meter.Tick(Time.deltaTime);
            slider.value = _meter.Value / 1.1f;
        }
    }
}
```

- [ ] **Step 4: Create HeightMeterUI (same pattern, maps to LOW/NICE/HIGH labels)**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Core/TimingMeter.cs Assets/Scripts/UI/
git commit -m "feat: add oscillating timing meters"
```

---

### Task 8: Throw Controller + Input

**Files:**
- Create: `Assets/Scripts/Input/ThrowInputHandler.cs`
- Create: `Assets/Scripts/Core/ThrowController.cs`
- Create: `Assets/Scripts/Core/HoleSetup.cs`

- [ ] **Step 1: Create HoleSetup**

```csharp
using DiskGolf.Flight;
using UnityEngine;

namespace DiskGolf.Core
{
    public class HoleSetup : MonoBehaviour
    {
        [SerializeField] Transform teePad;
        [SerializeField] Transform basket;
        [SerializeField] float circleRadiusFt = 33f;
        [SerializeField] float holeLengthFt = 250f;

        public Vector3 TeePosition => teePad.position;
        public Vector3 BasketPosition => basket.position;
        public float CircleRadiusFt => circleRadiusFt;

        public Vector3 AimDirection =>
            (BasketPosition - TeePosition).normalized;

        public float DistanceToBasket(Vector3 from) =>
            Vector3.Distance(from, BasketPosition) / 0.3048f;

        public WindSettings RollWind()
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return new WindSettings
            {
                direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)),
                speedMph = Random.Range(0f, 15f)
            };
        }
    }
}
```

- [ ] **Step 2: Create ThrowInputHandler**

```csharp
using DiskGolf.Flight;
using UnityEngine;

namespace DiskGolf.Input
{
    public class ThrowInputHandler : MonoBehaviour
    {
        public ReleaseAngle ReleaseAngle { get; private set; } = ReleaseAngle.Flat;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) ReleaseAngle = ReleaseAngle.Hyzer;
            if (Input.GetKeyDown(KeyCode.DownArrow)) ReleaseAngle = ReleaseAngle.Flat;
            if (Input.GetKeyDown(KeyCode.RightArrow)) ReleaseAngle = ReleaseAngle.Anhyzer;
        }

        public bool ConfirmPressed => Input.GetKeyDown(KeyCode.Space);
        public bool ResetPressed => Input.GetKeyDown(KeyCode.R);
        public int DiscHotkey => Input.GetKeyDown(KeyCode.Alpha1) ? 0 :
            Input.GetKeyDown(KeyCode.Alpha2) ? 1 :
            Input.GetKeyDown(KeyCode.Alpha3) ? 2 :
            Input.GetKeyDown(KeyCode.Alpha4) ? 3 : -1;
        public bool CycleNext => Input.GetKeyDown(KeyCode.E);
        public bool CyclePrev => Input.GetKeyDown(KeyCode.Q);
    }
}
```

- [ ] **Step 3: Create ThrowController (orchestrator)**

```csharp
using DiskGolf.Disc;
using DiskGolf.Flight;
using DiskGolf.Gameplay;
using DiskGolf.Input;
using UnityEngine;

namespace DiskGolf.Core
{
    public class ThrowController : MonoBehaviour
    {
        [SerializeField] HoleSetup hole;
        [SerializeField] DiscBag bag;
        [SerializeField] ThrowInputHandler input;
        [SerializeField] DiscFlightPresenter presenter;
        [SerializeField] PowerMeterUI powerMeter;
        [SerializeField] HeightMeterUI heightMeter;

        readonly ThrowStateMachine _state = new();
        WindSettings _wind;
        float _confirmedPower;
        Vector3 _discPosition;

        public ThrowPhase Phase => _state.Phase;
        public event System.Action<ThrowPhase> PhaseChanged;
        public WindSettings Wind => _wind;
        public ReleaseAngle ReleaseAngle => input.ReleaseAngle;
        public DiscProfile ActiveDisc => bag.Active;

        void Start()
        {
            _state.PhaseChanged += p => PhaseChanged?.Invoke(p);
            _state.PhaseChanged += OnPhaseChanged;
            ResetHole();
        }

        void Update()
        {
            if (input.ResetPressed) { ResetHole(); return; }

            switch (_state.Phase)
            {
                case ThrowPhase.Aiming:
                    HandleAiming();
                    break;
                case ThrowPhase.PowerMeter:
                    if (input.ConfirmPressed) { _confirmedPower = powerMeter.Confirm(); _state.Advance(); }
                    break;
                case ThrowPhase.HeightMeter:
                    if (input.ConfirmPressed)
                    {
                        var height = HeightMeterZones.FromValue(heightMeter.Confirm());
                        ExecuteThrow(_confirmedPower, height);
                    }
                    break;
            }
        }

        void HandleAiming()
        {
            if (input.CycleNext) bag.CycleNext();
            if (input.CyclePrev) bag.CyclePrev();
            var hk = input.DiscHotkey;
            if (hk >= 0) bag.SelectIndex(hk);
            if (input.ConfirmPressed) { powerMeter.Begin(); _state.Advance(); }
        }

        void ExecuteThrow(float power, ThrowHeight height)
        {
            var throwInput = new ThrowInput(
                bag.Active, input.ReleaseAngle, power, height, _wind,
                _discPosition, hole.AimDirection);
            var path = FlightSimulator.Compute(throwInput);
            _state.Advance(); // Throwing
            presenter.Play(path, OnFlightComplete);
        }

        void OnFlightComplete(FlightPath path)
        {
            _discPosition = path.Waypoints[path.Waypoints.Count - 1].Position;
            _state.Advance(); // Landed
            float rest = hole.DistanceToBasket(_discPosition);
            if (rest <= hole.CircleRadiusFt)
                _state.EnterPutting();
            else
                _state.Advance(); // Resolve → back to Aiming
        }

        void OnPhaseChanged(ThrowPhase phase)
        {
            if (phase == ThrowPhase.HeightMeter) heightMeter.Begin();
            if (phase == ThrowPhase.Putting) { /* simplified putt setup */ }
        }

        public void ResetHole()
        {
            _wind = hole.RollWind();
            _discPosition = hole.TeePosition;
            presenter.SetPosition(_discPosition);
            _state.TransitionTo(ThrowPhase.Aiming);
        }

        public FlightPath GetPreviewPath()
        {
            return FlightSimulator.Compute(new ThrowInput(
                bag.Active, input.ReleaseAngle, 1f, ThrowHeight.Nice, _wind,
                _discPosition, hole.AimDirection));
        }
    }
}
```

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core/ThrowController.cs Assets/Scripts/Core/HoleSetup.cs Assets/Scripts/Input/
git commit -m "feat: add ThrowController and keyboard input"
```

---

### Task 9: Disc Flight Presenter

**Files:**
- Create: `Assets/Scripts/Gameplay/DiscFlightPresenter.cs`

- [ ] **Step 1: Implement path follower**

```csharp
using System;
using System.Collections;
using DiskGolf.Flight;
using UnityEngine;

namespace DiskGolf.Gameplay
{
    public class DiscFlightPresenter : MonoBehaviour
    {
        [SerializeField] Transform discTransform;
        [SerializeField] float flightSpeed = 1f;

        Action<FlightPath> _onComplete;
        FlightPath _activePath;

        public void SetPosition(Vector3 pos) => discTransform.position = pos;

        public void Play(FlightPath path, Action<FlightPath> onComplete)
        {
            _activePath = path;
            _onComplete = onComplete;
            StartCoroutine(FlyRoutine());
        }

        IEnumerator FlyRoutine()
        {
            var wps = _activePath.Waypoints;
            float elapsed = 0f;
            float duration = wps[wps.Count - 1].Time / flightSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                int i = Mathf.Min(Mathf.FloorToInt(t * (wps.Count - 1)), wps.Count - 2);
                float localT = t * (wps.Count - 1) - i;
                discTransform.position = Vector3.Lerp(wps[i].Position, wps[i + 1].Position, localT);
                yield return null;
            }

            discTransform.position = wps[wps.Count - 1].Position;
            _onComplete?.Invoke(_activePath);
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Assets/Scripts/Gameplay/
git commit -m "feat: add DiscFlightPresenter path animation"
```

---

### Task 10: Prototype Flat 3 Scene (Greybox)

**Files:**
- Create: `Assets/Scenes/PrototypeFlat3.unity`
- Create: `Assets/Prefabs/Basket.prefab`, `TeePad.prefab`, `Disc.prefab`

- [ ] **Step 1: Build scene geometry**

In `PrototypeFlat3` scene:

| Object | Transform | Components |
|--------|-----------|------------|
| Fairway | scale (80, 0.1, 20) | Plane or Cube, green material, tag `Fairway` |
| TeePad | position (0,0,0) | Cube 2×0.1×2, tan material, tag `Tee` |
| Basket | position (0,0,76.2) | Cylinder pole + torus chains, tag `Basket` — 250ft ≈ 76.2m |
| CircleZone | child of Basket | Trigger sphere radius 10m (33ft), tag `Circle` |
| RoughBorder | surrounding planes | tag `Rough` (visual only MVP) |
| Disc | prefab | small cylinder, orange |

- [ ] **Step 2: Wire scene objects**

- Empty `GameManager` with: `HoleSetup`, `ThrowController`, `DiscBag`, `ThrowInputHandler`, `DiscFlightPresenter`
- Assign references in Inspector
- Assign 4 DiscProfile assets to DiscBag

- [ ] **Step 3: Manual play test**

Press Play → Space to start meters → confirm power/height → disc flies toward basket  
Expected: disc moves along path, REST distance decreases

- [ ] **Step 4: Commit**

```bash
git add Assets/Scenes/ Assets/Prefabs/
git commit -m "feat: add Prototype Flat 3 greybox scene"
```

---

### Task 11: Cinemachine Camera Director

**Files:**
- Create: `Assets/Scripts/Camera/CameraDirector.cs`

- [ ] **Step 1: Add 4 Cinemachine Virtual Cameras to scene**

| VCam | Type | Follow/Look At |
|------|------|----------------|
| SideSetupCam | Follow | thrower position + basket direction, offset (-5, 2, -3) |
| TopDownTrackCam | Follow | disc, offset (0, 30, 0), looking down |
| LieZoomCam | Static/Fixed | disc close-up offset |
| OverheadPuttCam | Fixed | basket, offset (0, 15, 0) |

- [ ] **Step 2: Implement CameraDirector**

```csharp
using Cinemachine;
using DiskGolf.Core;
using UnityEngine;

namespace DiskGolf.Camera
{
    public class CameraDirector : MonoBehaviour
    {
        [SerializeField] CinemachineVirtualCamera sideCam;
        [SerializeField] CinemachineVirtualCamera topDownCam;
        [SerializeField] CinemachineVirtualCamera lieZoomCam;
        [SerializeField] CinemachineVirtualCamera puttCam;
        [SerializeField] ThrowController throwController;
        [SerializeField] Transform disc;
        [SerializeField] Transform basket;

        void OnEnable() => throwController.PhaseChanged += OnPhase;
        void OnDisable() => throwController.PhaseChanged -= OnPhase;

        void OnPhase(ThrowPhase phase)
        {
            sideCam.gameObject.SetActive(phase is ThrowPhase.Aiming or ThrowPhase.PowerMeter or ThrowPhase.HeightMeter);
            topDownCam.gameObject.SetActive(phase == ThrowPhase.InFlight);
            lieZoomCam.gameObject.SetActive(phase == ThrowPhase.Landed);
            puttCam.gameObject.SetActive(phase == ThrowPhase.Putting);
        }
    }
}
```

- [ ] **Step 3: Play test camera cuts**

Expected: side view during aiming/meters → top-down during flight → brief lie zoom → overhead if in circle

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Camera/ Assets/Scenes/
git commit -m "feat: add Cinemachine CameraDirector with 4-camera flow"
```

---

### Task 12: HUD + Trajectory Preview

**Files:**
- Create: `Assets/Scripts/UI/HUDController.cs`
- Create: `Assets/Scripts/UI/TrajectoryPreview.cs`
- Create: `Assets/Scripts/UI/MinimapUI.cs`

- [ ] **Step 1: HUDController — bind REST distance, wind, disc name, stance**

```csharp
using DiskGolf.Core;
using DiskGolf.Flight;
using TMPro;
using UnityEngine;

namespace DiskGolf.UI
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] ThrowController controller;
        [SerializeField] HoleSetup hole;
        [SerializeField] Transform disc;
        [SerializeField] TextMeshProUGUI restText;
        [SerializeField] TextMeshProUGUI discText;
        [SerializeField] TextMeshProUGUI stanceText;
        [SerializeField] TextMeshProUGUI windText;

        void Update()
        {
            restText.text = $"REST {(int)hole.DistanceToBasket(disc.position)}ft";
            discText.text = $"{controller.ActiveDisc.displayName} " +
                $"{controller.ActiveDisc.speed}/{controller.ActiveDisc.glide}/" +
                $"{controller.ActiveDisc.turn}/{controller.ActiveDisc.fade}";
            stanceText.text = controller.ReleaseAngle.ToString().ToUpper();
            windText.text = $"WIND {(int)controller.Wind.speedMph}m";
        }
    }
}
```

- [ ] **Step 2: TrajectoryPreview — LineRenderer from GetPreviewPath()**

```csharp
using DiskGolf.Core;
using UnityEngine;

namespace DiskGolf.UI
{
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryPreview : MonoBehaviour
    {
        [SerializeField] ThrowController controller;
        LineRenderer _line;

        void Awake() => _line = GetComponent<LineRenderer>();

        void Update()
        {
            if (controller.Phase != ThrowPhase.Aiming) { _line.enabled = false; return; }
            _line.enabled = true;
            var path = controller.GetPreviewPath();
            _line.positionCount = path.Waypoints.Count;
            for (int i = 0; i < path.Waypoints.Count; i++)
                _line.SetPosition(i, path.Waypoints[i].Position + Vector3.up * 0.1f);
            _line.startColor = _line.endColor = Color.green;
            _line.startWidth = _line.endWidth = 0.3f;
        }
    }
}
```

- [ ] **Step 3: MinimapUI — simple top-down RawImage or second camera rendering tee-to-basket line**

Minimal MVP: UI Image with a line from tee dot to basket dot; update disc dot position normalized to hole length.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI/
git commit -m "feat: add HUD, trajectory preview, and minimap"
```

---

### Task 13: Putting Flow

**Files:**
- Modify: `Assets/Scripts/Core/ThrowController.cs`

- [ ] **Step 1: Add putting branch in ThrowController**

When `ThrowPhase.Putting`:
- Auto-select Putter disc (index 0)
- Show "IN THE CIRCLE" overlay (TextMeshPro)
- Single power meter only (skip height meter)
- On confirm: compute short ThrowInput (cap power effect at 80ft)
- If distance to basket < 1ft → disc at basket, show debug log "Made putt"
- If overshoot → disc past basket, return to Aiming from new lie

- [ ] **Step 2: Play test full hole**

Tee off with Buzzz → reach circle → putt cam activates → single meter putt → reset with R

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Core/ThrowController.cs
git commit -m "feat: add simplified putting flow"
```

---

### Task 14: MVP Verification Checklist

**Files:** none (manual verification)

- [ ] **Step 1: Run EditMode tests**

Test Runner → All pass (4+ tests)

- [ ] **Step 2: Manual success criteria check**

| # | Criterion | Pass? |
|---|-----------|-------|
| 1 | Disc select updates trajectory preview | |
| 2 | Hyzer/Flat/Anhyzer changes preview curve | |
| 3 | Both meters produce believable flight | |
| 4 | All 4 camera transitions in one throw | |
| 5 | Destroyer vs Putter distance gap obvious | |
| 6 | Wind drifts disc downwind | |
| 7 | Circle putt with single meter | |
| 8 | R resets to tee with new wind | |

- [ ] **Step 3: Final commit**

```bash
git add .
git commit -m "feat: complete throw-feel prototype MVP"
```

---

## Spec Coverage Check

| Spec Section | Task |
|--------------|------|
| DiscProfile + 4 discs | Task 2 |
| ThrowInput / FlightPath | Task 3 |
| FlightSimulator formulas | Tasks 4–5 |
| Throw state machine | Task 6 |
| Two timing meters | Task 7 |
| Keyboard input | Task 8 |
| Full throw loop | Tasks 8–9 |
| Prototype Flat 3 hole | Task 10 |
| 4-camera flow | Task 11 |
| HUD + trajectory arrow | Task 12 |
| Putting | Task 13 |
| Success criteria verification | Task 14 |
| Out of scope items | Not implemented (correct) |

No spec gaps identified.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-22-disk-golf-throw-prototype.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — dispatch a fresh subagent per task, review between tasks, fast iteration

2. **Inline Execution** — execute tasks in this session using executing-plans, batch execution with checkpoints

**Which approach?**

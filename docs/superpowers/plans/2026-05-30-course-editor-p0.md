# Course Editor P0 — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship P0 of the Unity-native course editor: paint fairway/rough/green on a 2 m grid, place tee/basket, export/import hole JSON, bake flat ground meshes with tagged colliders, and playtest via one button.

**Architecture:** `HoleData` is the in-memory source of truth; `HoleDataJson` handles file I/O. `ThemePack` ScriptableObject resolves surface materials. `CourseBuilder` spawns a `BuiltCourse` hierarchy with tags compatible with `DiscLieGround`. `CourseEditorWindow` provides paint + hole-marker tools and delegates scene drawing to `CourseEditorOverlay`. Legacy `PrototypeFlat3` scenes are untouched.

**Tech Stack:** Unity 6 / 2022 LTS, C#, Unity Test Framework (EditMode), `JsonUtility` with DTO wrappers, existing `HoleSetup` / `CourseLayout` / `GameplayArtCatalog`

**Spec:** `docs/superpowers/specs/2026-05-30-course-editor-design.md` (P0 section)

---

## File Map

| File | Responsibility |
|------|----------------|
| `Assets/Scripts/CourseEditor/SurfaceTileType.cs` | Enum: Tee, Fairway, Rough, Green |
| `Assets/Scripts/CourseEditor/HoleData.cs` | Domain model: tiles, hole meta, bounds |
| `Assets/Scripts/CourseEditor/HoleDataDto.cs` | JsonUtility-serializable DTO + conversion |
| `Assets/Scripts/CourseEditor/HoleDataJson.cs` | Read/write JSON files |
| `Assets/Scripts/CourseEditor/SurfaceTileTags.cs` | Maps surface type → Unity tag string |
| `Assets/Scripts/CourseEditor/CourseValidator.cs` | P0 validation rules E001–E004, W001, W005 |
| `Assets/Scripts/CourseEditor/ThemePack.cs` | ScriptableObject: theme id + surface materials |
| `Assets/Scripts/CourseEditor/FoliageArchetypeEntry.cs` | Struct: archetype id + sprite ref (P0: registry only) |
| `Assets/Scripts/CourseEditor/BuiltCourseHost.cs` | MonoBehaviour: exposes bounds like `CourseLayout` |
| `Assets/Scripts/CourseEditor/CourseBuilder.cs` | Builds flat ground meshes + tee/basket from HoleData |
| `Assets/Editor/CourseEditor/CourseEditorState.cs` | Active tool, brush, dirty flag, loaded HoleData |
| `Assets/Editor/CourseEditor/CourseEditorOverlay.cs` | Scene GUI: grid, tile tint, tee/basket gizmos |
| `Assets/Editor/CourseEditor/CourseEditorWindow.cs` | Main editor window UI |
| `Assets/Editor/CourseEditor/CourseEditorPlaytest.cs` | Bake + wire HoleSetup + enter Play Mode |
| `Assets/Data/Themes/ThemePack_Temperate.asset` | Default theme |
| `Assets/Data/Courses/Example/hole_01.json` | Sample exported hole |
| `Assets/Tests/EditMode/HoleDataJsonTests.cs` | JSON round-trip tests |
| `Assets/Tests/EditMode/SurfaceTileTagsTests.cs` | Tag mapping tests |
| `Assets/Tests/EditMode/CourseValidatorTests.cs` | Validation tests |
| `Assets/Scripts/Gameplay/ProjectArtPaths.cs` | Add `Themes` + `Courses` data paths |

---

### Task 1: Surface types and tag mapping

**Files:**
- Create: `Assets/Scripts/CourseEditor/SurfaceTileType.cs`
- Create: `Assets/Scripts/CourseEditor/SurfaceTileTags.cs`
- Test: `Assets/Tests/EditMode/SurfaceTileTagsTests.cs`

- [ ] **Step 1: Write the failing test**

Create `Assets/Tests/EditMode/SurfaceTileTagsTests.cs`:

```csharp
using DiskGolf.CourseEditor;
using NUnit.Framework;

namespace DiskGolf.Tests.EditMode
{
    public sealed class SurfaceTileTagsTests
    {
        [TestCase(SurfaceTileType.Fairway, "Fairway")]
        [TestCase(SurfaceTileType.Rough, "Rough")]
        [TestCase(SurfaceTileType.Green, "Green")]
        [TestCase(SurfaceTileType.Tee, "Tee")]
        public void ToUnityTag_ReturnsExpectedTag(SurfaceTileType type, string expected)
        {
            Assert.AreEqual(expected, SurfaceTileTags.ToUnityTag(type));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run in Unity: **Window → General → Test Runner → EditMode → Run SurfaceTileTagsTests**

Expected: FAIL — types/namespaces not found.

- [ ] **Step 3: Write minimal implementation**

Create `Assets/Scripts/CourseEditor/SurfaceTileType.cs`:

```csharp
namespace DiskGolf.CourseEditor
{
    public enum SurfaceTileType
    {
        Tee,
        Fairway,
        Rough,
        Green
    }
}
```

Create `Assets/Scripts/CourseEditor/SurfaceTileTags.cs`:

```csharp
namespace DiskGolf.CourseEditor
{
    public static class SurfaceTileTags
    {
        public static string ToUnityTag(SurfaceTileType type) => type switch
        {
            SurfaceTileType.Tee => "Tee",
            SurfaceTileType.Fairway => "Fairway",
            SurfaceTileType.Rough => "Rough",
            SurfaceTileType.Green => "Green",
            _ => "Fairway"
        };
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Expected: PASS (1 test, 4 cases).

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/CourseEditor/ Assets/Tests/EditMode/SurfaceTileTagsTests.cs
git commit -m "feat(course-editor): add surface tile types and tag mapping"
```

---

### Task 2: HoleData domain model

**Files:**
- Create: `Assets/Scripts/CourseEditor/HoleData.cs`

- [ ] **Step 1: Create HoleData**

Create `Assets/Scripts/CourseEditor/HoleData.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    [Serializable]
    public sealed class HoleTile
    {
        public int X;
        public int Y;
        public SurfaceTileType Type;

        public HoleTile() { }

        public HoleTile(int x, int y, SurfaceTileType type)
        {
            X = x;
            Y = y;
            Type = type;
        }
    }

    [Serializable]
    public sealed class HoleMeta
    {
        public Vector2 Tee = Vector2.zero;
        public Vector2 Basket = Vector2.zero;
        public int Par = 3;
        public float CircleRadiusFt = 33f;
    }

    [Serializable]
    public sealed class HoleData
    {
        public const int SchemaVersion = 1;
        public const float DefaultTileSize = 2f;

        public int SchemaVersionField = SchemaVersion;
        public string Id = "hole_01";
        public string Name = "New Hole";
        public float TileSize = DefaultTileSize;
        public Vector2 Origin = Vector2.zero;
        public string ThemeId = "temperate";
        public List<HoleTile> SurfaceTiles = new();
        public HoleMeta Hole = new();

        public void SetTile(int x, int y, SurfaceTileType type)
        {
            for (int i = 0; i < SurfaceTiles.Count; i++)
            {
                if (SurfaceTiles[i].X == x && SurfaceTiles[i].Y == y)
                {
                    if (type == SurfaceTileType.Tee && SurfaceTiles[i].Type != SurfaceTileType.Tee)
                    {
                        SurfaceTiles[i] = new HoleTile(x, y, type);
                        return;
                    }

                    SurfaceTiles[i] = new HoleTile(x, y, type);
                    return;
                }
            }

            SurfaceTiles.Add(new HoleTile(x, y, type));
        }

        public bool TryGetTile(int x, int y, out SurfaceTileType type)
        {
            foreach (var tile in SurfaceTiles)
            {
                if (tile.X == x && tile.Y == y)
                {
                    type = tile.Type;
                    return true;
                }
            }

            type = default;
            return false;
        }

        public void ClearTile(int x, int y)
        {
            SurfaceTiles.RemoveAll(t => t.X == x && t.Y == y);
        }

        public Vector2Int ComputeBoundsMax()
        {
            int maxX = 0;
            int maxY = 0;

            foreach (var tile in SurfaceTiles)
            {
                maxX = Mathf.Max(maxX, tile.X);
                maxY = Mathf.Max(maxY, tile.Y);
            }

            return new Vector2Int(maxX + 1, maxY + 1);
        }

        public float HoleLengthYards()
        {
            var delta = Hole.Basket - Hole.Tee;
            float meters = delta.magnitude;
            return meters / 0.9144f;
        }
    }
}
```

- [ ] **Step 2: Verify compiles**

Open Unity; wait for script compile. No errors in Console.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/CourseEditor/HoleData.cs
git commit -m "feat(course-editor): add HoleData domain model"
```

---

### Task 3: JSON serialization round-trip

**Files:**
- Create: `Assets/Scripts/CourseEditor/HoleDataDto.cs`
- Create: `Assets/Scripts/CourseEditor/HoleDataJson.cs`
- Test: `Assets/Tests/EditMode/HoleDataJsonTests.cs`

- [ ] **Step 1: Write the failing test**

Create `Assets/Tests/EditMode/HoleDataJsonTests.cs`:

```csharp
using DiskGolf.CourseEditor;
using NUnit.Framework;
using UnityEngine;

namespace DiskGolf.Tests.EditMode
{
    public sealed class HoleDataJsonTests
    {
        [Test]
        public void RoundTrip_PreservesTilesAndHoleMeta()
        {
            var original = new HoleData
            {
                Id = "test_hole",
                Name = "Test",
                TileSize = 2f,
                Origin = new Vector2(0f, 0f),
                ThemeId = "temperate"
            };
            original.SetTile(5, 10, SurfaceTileType.Fairway);
            original.SetTile(6, 10, SurfaceTileType.Green);
            original.Hole.Tee = new Vector2(10f, 20f);
            original.Hole.Basket = new Vector2(70f, 200f);
            original.Hole.Par = 3;

            string json = HoleDataJson.ToJson(original);
            var restored = HoleDataJson.FromJson(json);

            Assert.AreEqual(original.Id, restored.Id);
            Assert.AreEqual(2, restored.SurfaceTiles.Count);
            Assert.IsTrue(restored.TryGetTile(5, 10, out var fairway));
            Assert.AreEqual(SurfaceTileType.Fairway, fairway);
            Assert.AreEqual(original.Hole.Basket, restored.Hole.Basket);
            Assert.AreEqual(3, restored.Hole.Par);
        }
    }
}
```

- [ ] **Step 2: Run test — expect FAIL**

- [ ] **Step 3: Implement DTO + JSON helpers**

Create `Assets/Scripts/CourseEditor/HoleDataDto.cs`:

```csharp
using System;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    [Serializable]
    public sealed class HoleTileDto
    {
        public int x;
        public int y;
        public string type;
    }

    [Serializable]
    public sealed class HoleMetaDto
    {
        public float[] tee = { 0f, 0f };
        public float[] basket = { 0f, 0f };
        public int par = 3;
        public float circleRadiusFt = 33f;
    }

    [Serializable]
    public sealed class HoleDataDto
    {
        public int schemaVersion = HoleData.SchemaVersion;
        public string id = "hole_01";
        public string name = "New Hole";
        public string units = "meters";
        public float tileSize = HoleData.DefaultTileSize;
        public float[] origin = { 0f, 0f };
        public string themeId = "temperate";
        public HoleTileDto[] surfaceTiles = Array.Empty<HoleTileDto>();
        public HoleMetaDto hole = new();

        public static HoleDataDto FromDomain(HoleData data)
        {
            var tiles = new HoleTileDto[data.SurfaceTiles.Count];
            for (int i = 0; i < data.SurfaceTiles.Count; i++)
            {
                var t = data.SurfaceTiles[i];
                tiles[i] = new HoleTileDto
                {
                    x = t.X,
                    y = t.Y,
                    type = t.Type.ToString().ToLowerInvariant()
                };
            }

            return new HoleDataDto
            {
                schemaVersion = data.SchemaVersionField,
                id = data.Id,
                name = data.Name,
                tileSize = data.TileSize,
                origin = new[] { data.Origin.x, data.Origin.y },
                themeId = data.ThemeId,
                surfaceTiles = tiles,
                hole = new HoleMetaDto
                {
                    tee = new[] { data.Hole.Tee.x, data.Hole.Tee.y },
                    basket = new[] { data.Hole.Basket.x, data.Hole.Basket.y },
                    par = data.Hole.Par,
                    circleRadiusFt = data.Hole.CircleRadiusFt
                }
            };
        }

        public HoleData ToDomain()
        {
            var data = new HoleData
            {
                SchemaVersionField = schemaVersion,
                Id = id,
                Name = name,
                TileSize = tileSize,
                Origin = new Vector2(origin[0], origin[1]),
                ThemeId = themeId
            };

            if (surfaceTiles != null)
            {
                foreach (var tile in surfaceTiles)
                {
                    if (!Enum.TryParse<SurfaceTileType>(tile.type, true, out var type))
                        continue;

                    data.SetTile(tile.x, tile.y, type);
                }
            }

            if (hole != null)
            {
                data.Hole.Tee = new Vector2(hole.tee[0], hole.tee[1]);
                data.Hole.Basket = new Vector2(hole.basket[0], hole.basket[1]);
                data.Hole.Par = hole.par;
                data.Hole.CircleRadiusFt = hole.circleRadiusFt;
            }

            return data;
        }
    }
}
```

Create `Assets/Scripts/CourseEditor/HoleDataJson.cs`:

```csharp
using System.IO;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    public static class HoleDataJson
    {
        public static string ToJson(HoleData data, bool prettyPrint = true)
        {
            var dto = HoleDataDto.FromDomain(data);
            return JsonUtility.ToJson(dto, prettyPrint);
        }

        public static HoleData FromJson(string json)
        {
            var dto = JsonUtility.FromJson<HoleDataDto>(json);
            return dto?.ToDomain() ?? new HoleData();
        }

        public static void SaveToFile(HoleData data, string absolutePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            File.WriteAllText(absolutePath, ToJson(data));
        }

        public static HoleData LoadFromFile(string absolutePath)
        {
            return FromJson(File.ReadAllText(absolutePath));
        }
    }
}
```

- [ ] **Step 4: Run test — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/CourseEditor/HoleDataDto.cs Assets/Scripts/CourseEditor/HoleDataJson.cs Assets/Tests/EditMode/HoleDataJsonTests.cs
git commit -m "feat(course-editor): add hole JSON serialization"
```

---

### Task 4: Course validator (P0 rules)

**Files:**
- Create: `Assets/Scripts/CourseEditor/CourseValidator.cs`
- Create: `Assets/Scripts/CourseEditor/ValidationMessage.cs`
- Test: `Assets/Tests/EditMode/CourseValidatorTests.cs`

- [ ] **Step 1: Write failing tests**

Create `Assets/Tests/EditMode/CourseValidatorTests.cs`:

```csharp
using DiskGolf.CourseEditor;
using NUnit.Framework;
using UnityEngine;

namespace DiskGolf.Tests.EditMode
{
    public sealed class CourseValidatorTests
    {
        [Test]
        public void Validate_MissingBasket_IsError()
        {
            var data = new HoleData();
            data.SetTile(0, 0, SurfaceTileType.Fairway);
            data.Hole.Tee = new Vector2(0f, 0f);
            data.Hole.Basket = Vector2.zero;

            var result = CourseValidator.Validate(data);

            Assert.IsFalse(result.CanPlaytest);
            Assert.IsTrue(result.HasCode("E002") || result.HasCode("E001"));
        }

        [Test]
        public void ValidPar3_HasNoErrors()
        {
            var data = BuildSimplePar3();
            var result = CourseValidator.Validate(data);
            Assert.IsTrue(result.CanPlaytest);
        }

        static HoleData BuildSimplePar3()
        {
            var data = new HoleData();
            for (int y = 0; y < 30; y++)
                data.SetTile(5, y, SurfaceTileType.Fairway);
            data.SetTile(5, 29, SurfaceTileType.Green);
            data.Hole.Tee = new Vector2(10f, 0f);
            data.Hole.Basket = new Vector2(10f, 58f);
            data.Hole.Par = 3;
            return data;
        }
    }
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement validator**

Create `Assets/Scripts/CourseEditor/ValidationMessage.cs`:

```csharp
namespace DiskGolf.CourseEditor
{
    public enum ValidationSeverity { Error, Warning }

    public readonly struct ValidationMessage
    {
        public string Code { get; }
        public ValidationSeverity Severity { get; }
        public string Text { get; }

        public ValidationMessage(string code, ValidationSeverity severity, string text)
        {
            Code = code;
            Severity = severity;
            Text = text;
        }
    }
}
```

Create `Assets/Scripts/CourseEditor/CourseValidator.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    public sealed class ValidationResult
    {
        public List<ValidationMessage> Messages { get; } = new();

        public bool CanPlaytest
        {
            get
            {
                foreach (var m in Messages)
                {
                    if (m.Severity == ValidationSeverity.Error)
                        return false;
                }

                return true;
            }
        }

        public bool HasCode(string code)
        {
            foreach (var m in Messages)
            {
                if (m.Code == code)
                    return true;
            }

            return false;
        }
    }

    public static class CourseValidator
    {
        const float MinYards = 150f;
        const float MaxYards = 550f;

        public static ValidationResult Validate(HoleData data)
        {
            var result = new ValidationResult();

            if (data == null)
            {
                result.Messages.Add(new ValidationMessage("E000", ValidationSeverity.Error, "No hole data."));
                return result;
            }

            if (data.SurfaceTiles.Count == 0)
                result.Messages.Add(new ValidationMessage("E003", ValidationSeverity.Error, "No surface tiles painted."));

            if (data.Hole.Tee == Vector2.zero)
                result.Messages.Add(new ValidationMessage("E002", ValidationSeverity.Error, "Tee not placed."));

            if (data.Hole.Basket == Vector2.zero)
                result.Messages.Add(new ValidationMessage("E001", ValidationSeverity.Error, "Basket not placed."));

            if (data.Hole.Basket != Vector2.zero && !IsOnGreenOrFairway(data, data.Hole.Basket))
                result.Messages.Add(new ValidationMessage("E004", ValidationSeverity.Error, "Basket must be on green or fairway."));

            if (data.Hole.Tee != Vector2.zero && !IsOnTeeOrFairway(data, data.Hole.Tee))
                result.Messages.Add(new ValidationMessage("W001", ValidationSeverity.Warning, "Tee is not on tee or fairway tile."));

            float yards = data.HoleLengthYards();
            if (yards > 0f && (yards < MinYards || yards > MaxYards))
                result.Messages.Add(new ValidationMessage("W005", ValidationSeverity.Warning,
                    $"Hole length {yards:F0} yd is outside {MinYards:F0}–{MaxYards:F0} yd."));

            return result;
        }

        static bool IsOnGreenOrFairway(HoleData data, Vector2 worldPos)
        {
            if (TrySampleType(data, worldPos, out var type))
                return type == SurfaceTileType.Green || type == SurfaceTileType.Fairway;

            return false;
        }

        static bool IsOnTeeOrFairway(HoleData data, Vector2 worldPos)
        {
            if (TrySampleType(data, worldPos, out var type))
                return type == SurfaceTileType.Tee || type == SurfaceTileType.Fairway;

            return false;
        }

        static bool TrySampleType(HoleData data, Vector2 worldPos, out SurfaceTileType type)
        {
            int x = Mathf.FloorToInt((worldPos.x - data.Origin.x) / data.TileSize);
            int y = Mathf.FloorToInt((worldPos.y - data.Origin.y) / data.TileSize);
            return data.TryGetTile(x, y, out type);
        }
    }
}
```

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/CourseEditor/ValidationMessage.cs Assets/Scripts/CourseEditor/CourseValidator.cs Assets/Tests/EditMode/CourseValidatorTests.cs
git commit -m "feat(course-editor): add P0 course validation"
```

---

### Task 5: ThemePack ScriptableObject

**Files:**
- Create: `Assets/Scripts/CourseEditor/ThemePack.cs`
- Create: `Assets/Scripts/CourseEditor/FoliageArchetypeEntry.cs`
- Create: `Assets/Data/Themes/ThemePack_Temperate.asset`
- Modify: `Assets/Scripts/Gameplay/ProjectArtPaths.cs`

- [ ] **Step 1: Add paths**

In `ProjectArtPaths.cs`, add:

```csharp
public static class Data
{
    public const string Root = "Assets/Data";
    public const string ThemesRoot = Root + "/Themes";
    public const string CoursesRoot = Root + "/Courses";
}
```

- [ ] **Step 2: Create ThemePack**

Create `Assets/Scripts/CourseEditor/FoliageArchetypeEntry.cs`:

```csharp
using System;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    [Serializable]
    public sealed class FoliageArchetypeEntry
    {
        public string archetypeId = "tree_round";
        public Sprite sprite;
        public int sortingOrder = 10;
    }
}
```

Create `Assets/Scripts/CourseEditor/ThemePack.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    [CreateAssetMenu(fileName = "ThemePack", menuName = "DiskGolf/Course/Theme Pack")]
    public sealed class ThemePack : ScriptableObject
    {
        public string themeId = "temperate";
        public string displayName = "Temperate";
        public Material fairwayMaterial;
        public Material roughMaterial;
        public Material greenMaterial;
        public Material teeMaterial;
        public List<FoliageArchetypeEntry> foliage = new();

        public Material GetMaterial(SurfaceTileType type) => type switch
        {
            SurfaceTileType.Tee => teeMaterial != null ? teeMaterial : fairwayMaterial,
            SurfaceTileType.Fairway => fairwayMaterial,
            SurfaceTileType.Rough => roughMaterial,
            SurfaceTileType.Green => greenMaterial,
            _ => fairwayMaterial
        };

        public Sprite ResolveFoliage(string archetypeId)
        {
            foreach (var entry in foliage)
            {
                if (entry.archetypeId == archetypeId)
                    return entry.sprite;
            }

            return null;
        }
    }
}
```

- [ ] **Step 3: Create asset in Unity**

1. **Assets → Create → DiskGolf → Course → Theme Pack**
2. Save as `Assets/Data/Themes/ThemePack_Temperate.asset`
3. Assign existing materials from `ProjectArtPaths.Environment.Fairway/Rough/Green/Tee`
4. Add foliage entry: `tree_round` → `GameplayArtCatalog.treeRound` sprite

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/CourseEditor/ThemePack.cs Assets/Scripts/CourseEditor/FoliageArchetypeEntry.cs Assets/Scripts/Gameplay/ProjectArtPaths.cs Assets/Data/Themes/
git commit -m "feat(course-editor): add ThemePack ScriptableObject"
```

---

### Task 6: CourseBuilder — flat ground meshes

**Files:**
- Create: `Assets/Scripts/CourseEditor/CourseBuilder.cs`
- Create: `Assets/Scripts/CourseEditor/BuiltCourseHost.cs`

- [ ] **Step 1: Create BuiltCourseHost**

Create `Assets/Scripts/CourseEditor/BuiltCourseHost.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    /// <summary>Bounds provider for editor-built courses (mirrors CourseLayout minimap API).</summary>
    public sealed class BuiltCourseHost : MonoBehaviour
    {
        public const string RootName = "BuiltCourse";

        [SerializeField] Transform teePad;
        [SerializeField] Transform basket;
        Bounds _bounds;
        bool _ready;

        public Transform TeePad => teePad;
        public Transform Basket => basket;

        public Bounds WorldBounds
        {
            get
            {
                if (!_ready)
                    RefreshBounds();

                return _bounds;
            }
        }

        public void Bind(Transform tee, Transform basket)
        {
            teePad = tee;
            this.basket = basket;
            RefreshBounds();
        }

        public void RefreshBounds()
        {
            bool has = false;
            var b = new Bounds();

            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (!has)
                {
                    b = r.bounds;
                    has = true;
                }
                else
                {
                    b.Encapsulate(r.bounds);
                }
            }

            if (teePad != null)
            {
                if (!has) { b = new Bounds(teePad.position, Vector3.zero); has = true; }
                else b.Encapsulate(teePad.position);
            }

            if (basket != null)
            {
                if (!has) { b = new Bounds(basket.position, Vector3.zero); has = true; }
                else b.Encapsulate(basket.position);
            }

            _bounds = b;
            _ready = has;
        }

        public Vector2 WorldToNormalizedMap(Vector3 world, float viewAspect)
        {
            var b = WorldBounds;
            float halfX = b.extents.x * 1.1f;
            float halfZ = b.extents.z * 1.1f;
            float ortho = Mathf.Max(halfZ, halfX / Mathf.Max(viewAspect, 1e-4f));
            float hx = ortho * viewAspect;
            float hz = ortho;

            float u = hx > 1e-4f ? (world.x - (b.center.x - hx)) / (hx * 2f) : 0.5f;
            float v = hz > 1e-4f ? (world.z - (b.center.z - hz)) / (hz * 2f) : 0.5f;
            return new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
        }
    }
}
```

- [ ] **Step 2: Create CourseBuilder**

Create `Assets/Scripts/CourseEditor/CourseBuilder.cs`:

```csharp
using System.Collections.Generic;
using DiskGolf.Gameplay;
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    public static class CourseBuilder
    {
        const string GroundChild = "Ground";

        public static BuiltCourseHost Build(HoleData data, ThemePack theme, Transform parent = null)
        {
            DestroyExisting();

            var rootGo = new GameObject(BuiltCourseHost.RootName);
            if (parent != null)
                rootGo.transform.SetParent(parent, false);

            var host = rootGo.AddComponent<BuiltCourseHost>();
            var groundRoot = new GameObject(GroundChild).transform;
            groundRoot.SetParent(rootGo.transform, false);

            BuildSurfaceMeshes(data, theme, groundRoot);
            var tee = CreateMarker("TeePad", data.Hole.Tee, "Tee", theme?.GetMaterial(SurfaceTileType.Tee));
            var basket = CreateBasketMarker(data.Hole.Basket);
            tee.SetParent(rootGo.transform, true);
            basket.SetParent(rootGo.transform, true);
            host.Bind(tee, basket);
            host.RefreshBounds();

            return host;
        }

        static void DestroyExisting()
        {
            var existing = GameObject.Find(BuiltCourseHost.RootName);
            if (existing != null)
                Object.DestroyImmediate(existing);
        }

        static void BuildSurfaceMeshes(HoleData data, ThemePack theme, Transform groundRoot)
        {
            var grouped = new Dictionary<SurfaceTileType, List<Vector3>>();
            foreach (var tile in data.SurfaceTiles)
            {
                if (!grouped.TryGetValue(tile.Type, out var list))
                {
                    list = new List<Vector3>();
                    grouped[tile.Type] = list;
                }

                float x = data.Origin.x + tile.X * data.TileSize;
                float z = data.Origin.y + tile.Y * data.TileSize;
                list.Add(new Vector3(x, 0f, z));
            }

            foreach (var kv in grouped)
            {
                var go = new GameObject(kv.Key + "Mesh");
                go.transform.SetParent(groundRoot, false);
                go.tag = SurfaceTileTags.ToUnityTag(kv.Key);
                var mesh = BuildTileMesh(kv.Value, data.TileSize);
                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = theme != null ? theme.GetMaterial(kv.Key) : null;
                var collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }
        }

        static Mesh BuildTileMesh(List<Vector3> origins, float tileSize)
        {
            var mesh = new Mesh { name = "TileSurface" };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float half = tileSize * 0.5f;

            foreach (var origin in origins)
            {
                int baseIndex = vertices.Count;
                vertices.Add(origin + new Vector3(-half, 0f, -half));
                vertices.Add(origin + new Vector3(-half, 0f, half));
                vertices.Add(origin + new Vector3(half, 0f, half));
                vertices.Add(origin + new Vector3(half, 0f, -half));
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Transform CreateMarker(string name, Vector2 xz, string tag, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.tag = tag;
            go.transform.position = new Vector3(xz.x, 0.05f, xz.y);
            go.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);
            if (mat != null && go.TryGetComponent<Renderer>(out var r))
                r.sharedMaterial = mat;
            return go.transform;
        }

        static Transform CreateBasketMarker(Vector2 xz)
        {
            var basketGo = GameObject.FindGameObjectWithTag("Basket");
            if (basketGo != null)
            {
                basketGo.transform.position = new Vector3(xz.x, basketGo.transform.position.y, xz.y);
                return basketGo.transform;
            }

            var go = new GameObject("Basket");
            go.tag = "Basket";
            go.transform.position = new Vector3(xz.x, 0f, xz.y);
            BasketVisual.EnsureOn(go);
            return go.transform;
        }
    }
}
```

- [ ] **Step 3: Manual smoke test**

1. Create empty test scene.
2. Temporary menu item or `Debug` script:
   ```csharp
   var data = HoleDataJson.LoadFromFile(".../hole_01.json");
   var theme = AssetDatabase.LoadAssetAtPath<ThemePack>("Assets/Data/Themes/ThemePack_Temperate.asset");
   CourseBuilder.Build(data, theme);
   ```
3. Confirm `BuiltCourse/Ground` children have Fairway/Rough/Green tags and MeshColliders.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/CourseEditor/CourseBuilder.cs Assets/Scripts/CourseEditor/BuiltCourseHost.cs
git commit -m "feat(course-editor): add flat CourseBuilder and BuiltCourseHost"
```

---

### Task 7: Editor state + scene overlay (grid + paint)

**Files:**
- Create: `Assets/Editor/CourseEditor/CourseEditorState.cs`
- Create: `Assets/Editor/CourseEditor/CourseEditorOverlay.cs`

- [ ] **Step 1: Editor state singleton**

Create `Assets/Editor/CourseEditor/CourseEditorState.cs`:

```csharp
#if UNITY_EDITOR
using UnityEngine;

namespace DiskGolf.EditorTools.CourseEditor
{
    public enum CourseEditorTool { Paint, Erase, HoleTee, HoleBasket }

    public static class CourseEditorState
    {
        public static HoleData Data { get; set; } = new();
        public static ThemePack Theme { get; set; }
        public static CourseEditorTool ActiveTool = CourseEditorTool.Paint;
        public static SurfaceTileType BrushType = SurfaceTileType.Fairway;
        public static bool IsDirty { get; set; }

        public static Vector3? WorldToTile(Vector3 world)
        {
            if (Data == null)
                return null;

            int x = Mathf.FloorToInt((world.x - Data.Origin.x) / Data.TileSize);
            int y = Mathf.FloorToInt((world.z - Data.Origin.y) / Data.TileSize);
            if (x < 0 || y < 0)
                return null;

            return new Vector3(x, y, 0f);
        }
    }
}
#endif
```

- [ ] **Step 2: Scene overlay**

Create `Assets/Editor/CourseEditor/CourseEditorOverlay.cs`:

```csharp
#if UNITY_EDITOR
using DiskGolf.CourseEditor;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools.CourseEditor
{
    [InitializeOnLoad]
    public static class CourseEditorOverlay
    {
        static CourseEditorOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void OnSceneGUI(SceneView view)
        {
            if (CourseEditorState.Data == null)
                return;

            DrawGrid();
            HandleInput();
            DrawMarkers();
        }

        static void DrawGrid()
        {
            var data = CourseEditorState.Data;
            var bounds = data.ComputeBoundsMax();
            Handles.color = new Color(1f, 1f, 1f, 0.15f);
            float tile = data.TileSize;
            var origin = data.Origin;

            for (int x = 0; x <= bounds.x; x++)
            {
                float wx = origin.x + x * tile;
                Handles.DrawLine(new Vector3(wx, 0f, origin.y), new Vector3(wx, 0f, origin.y + bounds.y * tile));
            }

            for (int y = 0; y <= bounds.y; y++)
            {
                float wz = origin.y + y * tile;
                Handles.DrawLine(new Vector3(origin.x, 0f, wz), new Vector3(origin.x + bounds.x * tile, 0f, wz));
            }

            foreach (var tile in data.SurfaceTiles)
            {
                Handles.color = ColorFor(tile.Type);
                var center = new Vector3(
                    origin.x + tile.X * tile + tile * 0.5f,
                    0.02f,
                    origin.y + tile.Y * tile + tile * 0.5f);
                Handles.DrawSolidRectangleWithOutline(
                    new[]
                    {
                        center + new Vector3(-tile * 0.5f, 0f, -tile * 0.5f),
                        center + new Vector3(-tile * 0.5f, 0f, tile * 0.5f),
                        center + new Vector3(tile * 0.5f, 0f, tile * 0.5f),
                        center + new Vector3(tile * 0.5f, 0f, -tile * 0.5f)
                    },
                    ColorFor(tile.Type) * new Color(1f, 1f, 1f, 0.35f),
                    Color.clear);
            }
        }

        static Color ColorFor(SurfaceTileType type) => type switch
        {
            SurfaceTileType.Fairway => new Color(0.2f, 0.7f, 0.3f),
            SurfaceTileType.Rough => new Color(0.55f, 0.4f, 0.2f),
            SurfaceTileType.Green => new Color(0.1f, 0.85f, 0.35f),
            SurfaceTileType.Tee => new Color(0.3f, 0.5f, 0.9f),
            _ => Color.gray
        };

        static void HandleInput()
        {
            var e = Event.current;
            if (e.type != EventType.MouseDown && e.type != EventType.MouseDrag)
                return;

            if (e.button != 0 || e.alt)
                return;

            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float dist))
                return;

            var world = ray.GetPoint(dist);
            var tile = CourseEditorState.WorldToTile(world);
            if (tile == null)
                return;

            int x = (int)tile.Value.x;
            int y = (int)tile.Value.y;

            switch (CourseEditorState.ActiveTool)
            {
                case CourseEditorTool.Paint:
                    Undo.RecordObject(CourseEditorWindow.GetSerializedHoleTarget(), "Paint tile");
                    CourseEditorState.Data.SetTile(x, y, CourseEditorState.BrushType);
                    CourseEditorState.IsDirty = true;
                    e.Use();
                    break;
                case CourseEditorTool.Erase:
                    CourseEditorState.Data.ClearTile(x, y);
                    CourseEditorState.IsDirty = true;
                    e.Use();
                    break;
                case CourseEditorTool.HoleTee:
                    CourseEditorState.Data.Hole.Tee = new Vector2(
                        CourseEditorState.Data.Origin.x + x * CourseEditorState.Data.TileSize + CourseEditorState.Data.TileSize * 0.5f,
                        CourseEditorState.Data.Origin.y + y * CourseEditorState.Data.TileSize + CourseEditorState.Data.TileSize * 0.5f);
                    CourseEditorState.IsDirty = true;
                    e.Use();
                    break;
                case CourseEditorTool.HoleBasket:
                    CourseEditorState.Data.Hole.Basket = new Vector2(
                        CourseEditorState.Data.Origin.x + x * CourseEditorState.Data.TileSize + CourseEditorState.Data.TileSize * 0.5f,
                        CourseEditorState.Data.Origin.y + y * CourseEditorState.Data.TileSize + CourseEditorState.Data.TileSize * 0.5f);
                    CourseEditorState.IsDirty = true;
                    e.Use();
                    break;
            }

            SceneView.RepaintAll();
        }

        static void DrawMarkers()
        {
            var hole = CourseEditorState.Data.Hole;
            if (hole.Tee != Vector2.zero)
                Handles.Label(new Vector3(hole.Tee.x, 0.5f, hole.Tee.y), "TEE");
            if (hole.Basket != Vector2.zero)
                Handles.Label(new Vector3(hole.Basket.x, 0.5f, hole.Basket.y), "BASKET");
        }
    }
}
#endif
```

Note: `CourseEditorWindow.GetSerializedHoleTarget()` is added in Task 8 as a ScriptableObject scratch pad for Undo — or use a lightweight `HoleDataAsset : ScriptableObject` wrapper.

- [ ] **Step 3: Commit**

```bash
git add Assets/Editor/CourseEditor/
git commit -m "feat(course-editor): add editor state and scene overlay"
```

---

### Task 8: CourseEditorWindow UI

**Files:**
- Create: `Assets/Scripts/CourseEditor/HoleDataAsset.cs` (Undo target)
- Create: `Assets/Editor/CourseEditor/CourseEditorWindow.cs`

- [ ] **Step 1: HoleDataAsset for Undo**

Create `Assets/Scripts/CourseEditor/HoleDataAsset.cs`:

```csharp
using UnityEngine;

namespace DiskGolf.CourseEditor
{
    public sealed class HoleDataAsset : ScriptableObject
    {
        public HoleData Data = new();
    }
}
```

- [ ] **Step 2: Editor window**

Create `Assets/Editor/CourseEditor/CourseEditorWindow.cs` with menu `Disk Golf/Course Editor`, toolbar buttons (New, Open, Save, Export, Import, Bake), tool enum popup, brush type popup, theme object field, validation list, par field, yardage label.

Key handlers:

```csharp
[MenuItem("Disk Golf/Course Editor")]
public static void Open() => GetWindow<CourseEditorWindow>("Course Editor");

void OnBake()
{
    CourseBuilder.Build(CourseEditorState.Data, CourseEditorState.Theme);
    CourseEditorState.IsDirty = false;
}

void OnExport()
{
    var path = EditorUtility.SaveFilePanel("Export hole JSON", ProjectArtPaths.Data.CoursesRoot, "hole_01.json", "json");
    if (string.IsNullOrEmpty(path)) return;
    HoleDataJson.SaveToFile(CourseEditorState.Data, path);
}

void OnImport()
{
    var path = EditorUtility.OpenFilePanel("Import hole JSON", ProjectArtPaths.Data.CoursesRoot, "json");
    if (string.IsNullOrEmpty(path)) return;
    _asset.Data = HoleDataJson.LoadFromFile(path);
    CourseEditorState.Data = _asset.Data;
}

public static Object GetSerializedHoleTarget() => _scratchAsset;
```

Window creates/loads a hidden `HoleDataAsset` instance at `Assets/Data/Courses/_EditorScratch.asset` for Undo support.

- [ ] **Step 3: Verify in Unity**

1. Open **Disk Golf → Course Editor**
2. Paint fairway tiles in Scene view
3. Place tee + basket
4. Export JSON to `Assets/Data/Courses/Example/hole_01.json`

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/CourseEditor/HoleDataAsset.cs Assets/Editor/CourseEditor/CourseEditorWindow.cs Assets/Data/Courses/
git commit -m "feat(course-editor): add Course Editor window with paint and JSON I/O"
```

---

### Task 9: Playtest wiring

**Files:**
- Create: `Assets/Editor/CourseEditor/CourseEditorPlaytest.cs`
- Modify: `Assets/Scripts/Core/HoleSetup.cs` (optional: add `ApplyFromBuiltCourse` helper)

- [ ] **Step 1: Playtest helper**

Create `Assets/Editor/CourseEditor/CourseEditorPlaytest.cs`:

```csharp
#if UNITY_EDITOR
using DiskGolf.Core;
using DiskGolf.CourseEditor;
using UnityEditor;
using UnityEngine;

namespace DiskGolf.EditorTools.CourseEditor
{
    public static class CourseEditorPlaytest
    {
        public static void Run(HoleData data, ThemePack theme)
        {
            var validation = CourseValidator.Validate(data);
            if (!validation.CanPlaytest)
            {
                EditorUtility.DisplayDialog("Course Editor", "Fix validation errors before playtest.", "OK");
                return;
            }

            var host = CourseBuilder.Build(data, theme);
            var holeSetup = Object.FindObjectOfType<HoleSetup>();
            if (holeSetup == null)
            {
                var go = new GameObject("HoleSetup");
                holeSetup = go.AddComponent<HoleSetup>();
            }

            ApplyHoleSetup(holeSetup, host, data);
            EditorApplication.isPlaying = true;
        }

        static void ApplyHoleSetup(HoleSetup setup, BuiltCourseHost host, HoleData data)
        {
            var so = new SerializedObject(setup);
            so.FindProperty("teePad").objectReferenceValue = host.TeePad;
            so.FindProperty("basket").objectReferenceValue = host.Basket;
            so.FindProperty("par").intValue = data.Hole.Par;
            so.FindProperty("circleRadiusFt").floatValue = data.Hole.CircleRadiusFt;
            so.FindProperty("holeLengthFt").floatValue = data.HoleLengthYards() * 3f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
```

- [ ] **Step 2: Add Playtest button to window**

Wire `CourseEditorPlaytest.Run(CourseEditorState.Data, CourseEditorState.Theme)` to toolbar.

- [ ] **Step 3: Manual playtest**

1. Open `PrototypeFlat3` or empty scene with `ThrowController` rig
2. Paint a par-3, bake, playtest
3. Confirm throw loop starts with tee/basket wired

- [ ] **Step 4: Commit**

```bash
git add Assets/Editor/CourseEditor/CourseEditorPlaytest.cs Assets/Editor/CourseEditor/CourseEditorWindow.cs
git commit -m "feat(course-editor): wire playtest to HoleSetup"
```

---

### Task 10: Sample hole + README note

**Files:**
- Create: `Assets/Data/Courses/Example/hole_01.json`
- Modify: `Assets/README.md`

- [ ] **Step 1: Export example hole from editor** (~250 yd par 3)

Save JSON matching spec shape; commit file.

- [ ] **Step 2: Add README section**

Under Team workflow, add:

```markdown
## Course editor (P0)

- Open **Disk Golf → Course Editor**
- Paint tiles, place tee/basket, export `Assets/Data/Courses/<name>/hole_XX.json`
- Themes live in `Assets/Data/Themes/ThemePack_*.asset`
```

- [ ] **Step 3: Commit**

```bash
git add Assets/Data/Courses/Example/hole_01.json Assets/README.md
git commit -m "docs: add course editor workflow and example hole"
```

---

## Spec Coverage (P0 self-review)

| P0 requirement | Task |
|----------------|------|
| HoleData schema v1 + JSON I/O | Tasks 2–3 |
| Paint fairway/rough/green | Tasks 7–8 |
| Tee + basket + par | Tasks 7–8 |
| CourseBuilder flat mesh | Task 6 |
| ThemePack one theme | Task 5 |
| Playtest button | Task 9 |
| JSON round-trip tests | Task 3 |
| Tagged colliders | Task 6 (manual + validator) |
| Validation panel | Tasks 4, 8 |

**Deferred to P1+:** elevation, hazards, foliage placement, multi-theme swap UI, templates, course manifest.

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-30-course-editor-p0.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — fresh subagent per task, review between tasks, fast iteration
2. **Inline Execution** — implement tasks in this session with checkpoints

Which approach do you want?

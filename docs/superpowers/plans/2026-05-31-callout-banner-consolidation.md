# Callout Banner Consolidation — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Consolidate seven duplicate HUD banner MonoBehaviours into shared callout primitives + one prefab, preserving all throw-flow callout behavior.

**Architecture:** `GameplayCalloutHost` lives under `GameplayHUD` and exposes `SpriteCalloutBanner`, `TextCalloutBanner`, `MultiSlotSpriteBanner`, `ThrowSummaryPanel`, and `HoleCompleteCutsceneUI`. Legacy banner classes become thin facades during migration, then are deleted. Scene builders instantiate `Assets/Prefabs/UI/GameplayCallouts.prefab` instead of code-generating UI.

**Tech Stack:** Unity 6 / 2022 LTS, C#, uGUI, TextMesh Pro, Unity Test Framework (EditMode)

**Spec:** `docs/superpowers/specs/2026-05-31-callout-banner-consolidation-design.md`

---

## File Map

| File | Responsibility |
|------|----------------|
| `Assets/Scripts/UI/Callouts/HudCanvasUtility.cs` | Single `GameplayHUD` lookup |
| `Assets/Scripts/UI/Callouts/CalloutLifecycle.cs` | Timed hide coroutines, bring-to-front |
| `Assets/Scripts/UI/Callouts/TextCalloutLayout.cs` | Layout preset enum |
| `Assets/Scripts/UI/Callouts/SpriteCalloutBanner.cs` | Image sprite callout |
| `Assets/Scripts/UI/Callouts/TextCalloutBanner.cs` | TMP text callout |
| `Assets/Scripts/UI/Callouts/MultiSlotSpriteBanner.cs` | Hole-complete score slots |
| `Assets/Scripts/UI/Callouts/GameplayCalloutHost.cs` | Host + Ensure + prefab spawn |
| `Assets/Scripts/UI/ThrowSummaryPanel.cs` | Slim pre-throw panel (from ThrowSummaryBannerUI) |
| `Assets/Prefabs/UI/GameplayCallouts.prefab` | Baked callout hierarchy |
| `Assets/Editor/CalloutPrefabAuthoring.cs` | Menu to create prefab + bake into scenes |
| `Assets/Tests/EditMode/MultiSlotSpriteBannerTests.cs` | Slot show/hide tests |
| `Assets/Scripts/Gameplay/ProjectArtPaths.cs` | Add `Prefabs.GameplayCallouts` path |

**Modified:** `HudSceneAuthoring.cs`, `PrototypeFlat3SceneBuilder.cs`, `ThrowController.cs`, `HoleCompleteCutsceneUI.cs`, legacy banner facades (temporary), `Assets/README.md`

**Deleted (Task 11):** `LieLandingBannerUI.cs`, `OnTheGreenBannerUI.cs`, `ThrowResultBannerUI.cs`, `SweetSpotBannerUI.cs`, `HoleCompleteBannerUI.cs`, `ThrowSummaryBannerUI.cs`

---

### Task 1: Shared HUD utilities

**Files:**
- Create: `Assets/Scripts/UI/Callouts/HudCanvasUtility.cs`
- Create: `Assets/Scripts/UI/Callouts/CalloutLifecycle.cs`

- [ ] **Step 1: Create HudCanvasUtility**

Create `Assets/Scripts/UI/Callouts/HudCanvasUtility.cs`:

```csharp
using UnityEngine;

namespace DiskGolf.UI.Callouts
{
    public static class HudCanvasUtility
    {
        public const string HudCanvasName = "GameplayHUD";

        public static RectTransform FindHudCanvas()
        {
            var hud = GameObject.Find(HudCanvasName);
            return hud != null ? hud.GetComponent<RectTransform>() : null;
        }
    }
}
```

- [ ] **Step 2: Create CalloutLifecycle**

Create `Assets/Scripts/UI/Callouts/CalloutLifecycle.cs`:

```csharp
using System;
using System.Collections;
using UnityEngine;

namespace DiskGolf.UI.Callouts
{
    public static class CalloutLifecycle
    {
        public static void BringToFront(Transform target)
        {
            if (target != null)
                target.SetAsLastSibling();
        }

        public static Coroutine ShowBriefly(
            MonoBehaviour runner,
            ref Coroutine routine,
            float seconds,
            Action show,
            Action hide)
        {
            if (routine != null)
                runner.StopCoroutine(routine);

            show?.Invoke();
            routine = runner.StartCoroutine(HideAfter(runner, seconds, hide, () => routine = null));
            return routine;
        }

        static IEnumerator HideAfter(MonoBehaviour runner, float seconds, Action hide, Action clearRoutine)
        {
            yield return new WaitForSeconds(seconds);
            hide?.Invoke();
            clearRoutine?.Invoke();
        }

        public static void Cancel(ref Coroutine routine, MonoBehaviour runner)
        {
            if (routine == null)
                return;

            runner.StopCoroutine(routine);
            routine = null;
        }
    }
}
```

- [ ] **Step 3: Verify compile**

Open Unity; Console shows no errors for new scripts.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI/Callouts/HudCanvasUtility.cs Assets/Scripts/UI/Callouts/CalloutLifecycle.cs
git commit -m "refactor(ui): add shared HUD canvas and callout lifecycle helpers"
```

---

### Task 2: SpriteCalloutBanner primitive

**Files:**
- Create: `Assets/Scripts/UI/Callouts/SpriteCalloutBanner.cs`

- [ ] **Step 1: Create SpriteCalloutBanner**

Create `Assets/Scripts/UI/Callouts/SpriteCalloutBanner.cs`:

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI.Callouts
{
    public sealed class SpriteCalloutBanner : MonoBehaviour
    {
        [SerializeField] Image bannerImage;

        [SerializeField] float displaySeconds = 2.25f;

        [SerializeField] bool useScoreBannerLayout = true;

        Coroutine _hideRoutine;

        public float DisplaySeconds => displaySeconds;

        void Awake() => ResolveImage();

        public void ResolveImage()
        {
            bannerImage ??= GetComponent<Image>();
            if (bannerImage != null)
                bannerImage.raycastTarget = false;
        }

        public void Show(Sprite sprite)
        {
            ResolveImage();
            if (bannerImage == null || sprite == null)
                return;

            ApplyLayout();
            bannerImage.sprite = sprite;
            bannerImage.preserveAspect = true;
            bannerImage.color = Color.white;
            bannerImage.enabled = true;
            CalloutLifecycle.BringToFront(transform);
            gameObject.SetActive(true);
        }

        public void ShowBriefly(Sprite sprite)
        {
            CalloutLifecycle.ShowBriefly(
                this,
                ref _hideRoutine,
                displaySeconds,
                () => Show(sprite),
                Hide);
        }

        public void Hide()
        {
            CalloutLifecycle.Cancel(ref _hideRoutine, this);
            gameObject.SetActive(false);
        }

        void ApplyLayout()
        {
            var rt = transform as RectTransform;
            if (rt == null)
                return;

            if (useScoreBannerLayout)
                PostThrowCalloutLayout.ApplyScoreBannerRect(rt);
        }
    }
}
```

- [ ] **Step 2: Verify compile**

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/UI/Callouts/SpriteCalloutBanner.cs
git commit -m "refactor(ui): add SpriteCalloutBanner primitive"
```

---

### Task 3: TextCalloutBanner primitive

**Files:**
- Create: `Assets/Scripts/UI/Callouts/TextCalloutLayout.cs`
- Create: `Assets/Scripts/UI/Callouts/TextCalloutBanner.cs`

- [ ] **Step 1: Create layout enum**

Create `Assets/Scripts/UI/Callouts/TextCalloutLayout.cs`:

```csharp
namespace DiskGolf.UI.Callouts
{
    public enum TextCalloutLayout
    {
        FeetLabel,
        CenterPopup,
    }
}
```

- [ ] **Step 2: Create TextCalloutBanner**

Create `Assets/Scripts/UI/Callouts/TextCalloutBanner.cs`:

```csharp
using System.Collections;
using TMPro;
using UnityEngine;

namespace DiskGolf.UI.Callouts
{
    public sealed class TextCalloutBanner : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI label;

        [SerializeField] TextCalloutLayout layout = TextCalloutLayout.FeetLabel;

        [SerializeField] float displaySeconds = 2f;

        [SerializeField] bool autoHide;

        Coroutine _hideRoutine;

        public float DisplaySeconds => displaySeconds;

        void Awake() => ResolveLabel();

        public void ResolveLabel()
        {
            label ??= GetComponent<TextMeshProUGUI>();
            if (label != null)
                label.raycastTarget = false;
        }

        public void Show(string text)
        {
            ResolveLabel();
            if (label == null)
                return;

            ApplyLayout();
            label.text = text;
            label.ForceMeshUpdate();
            CalloutLifecycle.BringToFront(transform);
            gameObject.SetActive(true);
        }

        public void ShowBriefly(string text)
        {
            CalloutLifecycle.ShowBriefly(
                this,
                ref _hideRoutine,
                displaySeconds,
                () => Show(text),
                Hide);
        }

        public void ShowThrowDistance(float distanceFt)
        {
            ApplyFeetStyle();
            int feet = Mathf.Max(0, Mathf.RoundToInt(distanceFt));
            Show($"{feet} FEET");
        }

        public void Hide()
        {
            CalloutLifecycle.Cancel(ref _hideRoutine, this);
            gameObject.SetActive(false);
        }

        void ApplyLayout()
        {
            var rt = transform as RectTransform;
            if (rt == null)
                return;

            switch (layout)
            {
                case TextCalloutLayout.FeetLabel:
                    PostThrowCalloutLayout.ApplyFeetLabelRect(rt);
                    break;
                case TextCalloutLayout.CenterPopup:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.62f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = new Vector2(640f, 120f);
                    break;
            }
        }

        public void ApplyFeetStyle()
        {
            ResolveLabel();
            if (label == null)
                return;

            label.fontSize = 64f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Top;
            label.color = Color.black;
            label.outlineWidth = 0f;
            HudTypography.BindFont(label);
        }

        public void ApplySweetSpotStyle()
        {
            ResolveLabel();
            if (label == null)
                return;

            label.fontSize = 72f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.82f, 0.28f, 1f);
            label.outlineWidth = 0.35f;
            label.outlineColor = Color.black;
            HudTypography.BindFont(label);
        }
    }
}
```

- [ ] **Step 3: Verify compile**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI/Callouts/TextCalloutLayout.cs Assets/Scripts/UI/Callouts/TextCalloutBanner.cs
git commit -m "refactor(ui): add TextCalloutBanner primitive"
```

---

### Task 4: MultiSlotSpriteBanner primitive

**Files:**
- Create: `Assets/Scripts/UI/Callouts/MultiSlotSpriteBanner.cs`
- Test: `Assets/Tests/EditMode/MultiSlotSpriteBannerTests.cs`

- [ ] **Step 1: Write failing test**

Create `Assets/Tests/EditMode/MultiSlotSpriteBannerTests.cs`:

```csharp
using DiskGolf.UI;
using DiskGolf.UI.Callouts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.Tests.EditMode
{
    public sealed class MultiSlotSpriteBannerTests
    {
        [Test]
        public void ShowKind_ActivatesOnlyMatchingSlot()
        {
            var root = new GameObject("Banner", typeof(RectTransform), typeof(MultiSlotSpriteBanner));
            var banner = root.GetComponent<MultiSlotSpriteBanner>();

            var parGo = new GameObject("Par", typeof(RectTransform), typeof(Image));
            parGo.transform.SetParent(root.transform, false);
            var birdieGo = new GameObject("Birdie", typeof(RectTransform), typeof(Image));
            birdieGo.transform.SetParent(root.transform, false);

            banner.ConfigureSlotsForTests(new[]
            {
                (HoleCompleteScoreKind.Par, parGo.GetComponent<Image>()),
                (HoleCompleteScoreKind.Birdie, birdieGo.GetComponent<Image>()),
            });

            banner.ShowKind(HoleCompleteScoreKind.Birdie);

            Assert.IsFalse(parGo.activeSelf);
            Assert.IsTrue(birdieGo.activeSelf);

            Object.DestroyImmediate(root);
        }
    }
}
```

- [ ] **Step 2: Run test — expect FAIL**

Unity Test Runner → EditMode → `MultiSlotSpriteBannerTests`

Expected: FAIL — type not found.

- [ ] **Step 3: Implement MultiSlotSpriteBanner**

Create `Assets/Scripts/UI/Callouts/MultiSlotSpriteBanner.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DiskGolf.UI.Callouts
{
    public sealed class MultiSlotSpriteBanner : MonoBehaviour
    {
        readonly Dictionary<HoleCompleteScoreKind, Image> _slots = new();

        [SerializeField] float displaySeconds = 3.5f;

        public float DisplaySeconds => displaySeconds;

        public void BindSceneReferences()
        {
            _slots.Clear();
            RegisterSlot(HoleCompleteScoreKind.HoleInOne, "HoleInOne");
            RegisterSlot(HoleCompleteScoreKind.Eagle, "Eagle");
            RegisterSlot(HoleCompleteScoreKind.Birdie, "Birdie");
            RegisterSlot(HoleCompleteScoreKind.Par, "Par");
            RegisterSlot(HoleCompleteScoreKind.Bogey, "Bogey");
            RegisterSlot(HoleCompleteScoreKind.DoubleBogey, "DoubleBogey");
            RegisterSlot(HoleCompleteScoreKind.TripleBogey, "TripleBogey");
            RegisterSlot(HoleCompleteScoreKind.Awful, "Awful");
            HideAll();
        }

        void RegisterSlot(HoleCompleteScoreKind kind, string childName)
        {
            var image = transform.Find(childName)?.GetComponent<Image>();
            if (image == null)
                return;

            ApplySlotSprite(image, kind);
            image.raycastTarget = false;
            _slots[kind] = image;
            image.gameObject.SetActive(false);
        }

        public void Show(int strokes, int par) =>
            ShowKind(ScoreBannerSprites.ResolveKind(strokes, par));

        public void ShowKind(HoleCompleteScoreKind kind)
        {
            HideAll();
            if (!_slots.TryGetValue(kind, out var slot) || slot == null)
                return;

            ApplySlotSprite(slot, kind);
            slot.gameObject.SetActive(true);
            CalloutLifecycle.BringToFront(transform);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            HideAll();
            gameObject.SetActive(false);
        }

        void HideAll()
        {
            foreach (var slot in _slots.Values)
            {
                if (slot != null)
                    slot.gameObject.SetActive(false);
            }
        }

        static void ApplySlotSprite(Image image, HoleCompleteScoreKind kind)
        {
            if (image.sprite == null)
                image.sprite = ScoreBannerSprites.LoadKind(kind);

            image.preserveAspect = true;
            image.color = Color.white;
            image.enabled = image.sprite != null;
        }

#if UNITY_INCLUDE_TESTS
        public void ConfigureSlotsForTests((HoleCompleteScoreKind kind, Image image)[] slots)
        {
            _slots.Clear();
            foreach (var entry in slots)
                _slots[entry.kind] = entry.image;
        }
#endif
    }
}
```

- [ ] **Step 4: Run test — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/UI/Callouts/MultiSlotSpriteBanner.cs Assets/Tests/EditMode/MultiSlotSpriteBannerTests.cs
git commit -m "refactor(ui): add MultiSlotSpriteBanner with EditMode test"
```

---

### Task 5: Strangler facades for simple banners

**Files:**
- Modify: `Assets/Scripts/UI/LieLandingBannerUI.cs`
- Modify: `Assets/Scripts/UI/OnTheGreenBannerUI.cs`
- Modify: `Assets/Scripts/UI/ThrowResultBannerUI.cs`
- Modify: `Assets/Scripts/UI/SweetSpotBannerUI.cs`

- [ ] **Step 1: Replace LieLandingBannerUI body**

Replace `Assets/Scripts/UI/LieLandingBannerUI.cs` with:

```csharp
using DiskGolf.Flight;
using DiskGolf.UI.Callouts;
using UnityEngine;

namespace DiskGolf.UI
{
    /// <summary>Facade — delegates to SpriteCalloutBanner on same GameObject.</summary>
    public sealed class LieLandingBannerUI : MonoBehaviour
    {
        SpriteCalloutBanner _banner;

        public float DisplaySeconds => Banner.DisplaySeconds;

        SpriteCalloutBanner Banner => _banner ??= GetComponent<SpriteCalloutBanner>();

        public static LieLandingBannerUI Ensure()
        {
            var canvas = HudCanvasUtility.FindHudCanvas();
            if (canvas == null)
                return null;

            var existing = canvas.Find("LieLandingBanner")?.GetComponent<LieLandingBannerUI>();
            if (existing != null)
                return existing;

            if (SceneHudAuthoring.IsActive)
            {
                Debug.LogWarning("[Disk Golf] LieLandingBanner missing. Bake GameplayCallouts prefab.");
                return null;
            }

            return GameplayCalloutHost.Ensure(canvas)?.GetComponentInChildren<LieLandingBannerUI>(true);
        }

        public void Show(LieType lie)
        {
            var sprite = lie switch
            {
                LieType.Fairway => ScoreBannerSprites.Fairway,
                LieType.Rough => ScoreBannerSprites.Rough,
                _ => null,
            };

            Banner.Show(sprite);
        }

        public void ShowBriefly(LieType lie) => Banner.ShowBriefly(lie switch
        {
            LieType.Fairway => ScoreBannerSprites.Fairway,
            LieType.Rough => ScoreBannerSprites.Rough,
            _ => null,
        });

        public void Hide() => Banner.Hide();
    }
}
```

- [ ] **Step 2: Replace OnTheGreenBannerUI** (same pattern)

Key differences: banner name `OnTheGreenBanner`, sprite `ScoreBannerSprites.OnTheGreen`, hide legacy `InTheCircleBanner` in `Awake`:

```csharp
void Awake()
{
    var canvas = transform.parent;
    canvas?.Find("InTheCircleBanner")?.gameObject.SetActive(false);
}
```

- [ ] **Step 3: Replace ThrowResultBannerUI**

Delegate to `TextCalloutBanner` with `ApplyFeetStyle()` before show; keep static `ApplyStyle(TextMeshProUGUI)` calling `HudTypography.BindFont` for scene-builder compatibility.

- [ ] **Step 4: Replace SweetSpotBannerUI**

Delegate to `TextCalloutBanner`; call `ApplySweetSpotStyle()` in `Show()`.

- [ ] **Step 5: Manual smoke test**

Play `PrototypeFlat3`: land on fairway, rough, circle; verify banners unchanged.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/UI/LieLandingBannerUI.cs Assets/Scripts/UI/OnTheGreenBannerUI.cs Assets/Scripts/UI/ThrowResultBannerUI.cs Assets/Scripts/UI/SweetSpotBannerUI.cs
git commit -m "refactor(ui): slim simple banner facades to callout primitives"
```

---

### Task 6: GameplayCalloutHost + ProjectArtPaths

**Files:**
- Create: `Assets/Scripts/UI/Callouts/GameplayCalloutHost.cs`
- Modify: `Assets/Scripts/Gameplay/ProjectArtPaths.cs`

- [ ] **Step 1: Add prefab path**

In `ProjectArtPaths.Prefabs`, add:

```csharp
public const string UiRoot = "Assets/Prefabs/UI";

public const string GameplayCallouts = UiRoot + "/GameplayCallouts.prefab";
```

- [ ] **Step 2: Create GameplayCalloutHost**

Create `Assets/Scripts/UI/Callouts/GameplayCalloutHost.cs`:

```csharp
using UnityEngine;

namespace DiskGolf.UI.Callouts
{
    public sealed class GameplayCalloutHost : MonoBehaviour
    {
        public const string RootName = "GameplayCallouts";

        [SerializeField] SpriteCalloutBanner lieLanding;

        [SerializeField] SpriteCalloutBanner onTheGreen;

        [SerializeField] TextCalloutBanner throwDistance;

        [SerializeField] TextCalloutBanner sweetSpot;

        [SerializeField] MultiSlotSpriteBanner holeComplete;

        [SerializeField] ThrowSummaryPanel throwSummary;

        [SerializeField] HoleCompleteCutsceneUI holeCutscene;

        public SpriteCalloutBanner LieLanding => lieLanding;

        public SpriteCalloutBanner OnTheGreen => onTheGreen;

        public TextCalloutBanner ThrowDistance => throwDistance;

        public TextCalloutBanner SweetSpot => sweetSpot;

        public MultiSlotSpriteBanner HoleComplete => holeComplete;

        public ThrowSummaryPanel ThrowSummary => throwSummary;

        public HoleCompleteCutsceneUI HoleCutscene => holeCutscene;

        public static GameplayCalloutHost Ensure(RectTransform hud = null)
        {
            hud ??= HudCanvasUtility.FindHudCanvas();
            if (hud == null)
                return null;

            var existing = hud.Find(RootName)?.GetComponent<GameplayCalloutHost>();
            if (existing != null)
            {
                existing.BindReferences();
                return existing;
            }

            return null;
        }

        public void BindReferences()
        {
            lieLanding ??= transform.Find("LieLandingBanner")?.GetComponent<SpriteCalloutBanner>();
            onTheGreen ??= transform.Find("OnTheGreenBanner")?.GetComponent<SpriteCalloutBanner>();
            throwDistance ??= transform.Find("ThrowResultBanner")?.GetComponent<TextCalloutBanner>();
            sweetSpot ??= transform.Find("SweetSpotBanner")?.GetComponent<TextCalloutBanner>();
            holeComplete ??= transform.Find("HoleCompleteBanner")?.GetComponent<MultiSlotSpriteBanner>();
            throwSummary ??= transform.Find("ThrowSummaryBanner")?.GetComponent<ThrowSummaryPanel>();
            holeCutscene ??= transform.Find("HoleCompleteCutscene")?.GetComponent<HoleCompleteCutsceneUI>();
        }
    }
}
```

Note: `ThrowSummaryPanel` is created in Task 7; use `ThrowSummaryBannerUI` temporarily if Task 6 runs before Task 7, then rename.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/UI/Callouts/GameplayCalloutHost.cs Assets/Scripts/Gameplay/ProjectArtPaths.cs
git commit -m "refactor(ui): add GameplayCalloutHost and prefab path"
```

---

### Task 7: ThrowSummaryPanel extraction

**Files:**
- Create: `Assets/Scripts/UI/ThrowSummaryPanel.cs`
- Modify: `Assets/Scripts/UI/ThrowSummaryBannerUI.cs` (facade only, delegates to panel)

- [ ] **Step 1: Copy ThrowSummaryBannerUI → ThrowSummaryPanel**

Copy `Assets/Scripts/UI/ThrowSummaryBannerUI.cs` to `Assets/Scripts/UI/ThrowSummaryPanel.cs`.

Changes in the copy:
- Rename class `ThrowSummaryPanel`
- Remove static `Ensure()` / `CreateForScene()` / `BuildSceneLayout()` / `BuildRuntime()` — panel expects prefab hierarchy
- Keep: `ShowBriefly`, `Hide`, slide coroutine, `BindSceneReferences`, `EnsureSceneLayout`, `ApplyEditorPreview`
- Replace `FindHudCanvas()` with `HudCanvasUtility.FindHudCanvas()`

- [ ] **Step 2: Slim ThrowSummaryBannerUI to facade**

```csharp
public sealed class ThrowSummaryBannerUI : MonoBehaviour
{
    ThrowSummaryPanel _panel;
    ThrowSummaryPanel Panel => _panel ??= GetComponent<ThrowSummaryPanel>();

    public float DisplaySeconds => Panel.DisplaySeconds;
    public bool IsVisible => Panel.IsVisible;

    public static ThrowSummaryBannerUI Ensure() =>
        GameplayCalloutHost.Ensure()?.ThrowSummary?.GetComponent<ThrowSummaryBannerUI>()
        ?? GameplayCalloutHost.Ensure()?.GetComponentInChildren<ThrowSummaryBannerUI>(true);

    public void ShowBriefly(int n, PlayerCharacterProfile c, Action onDismissed = null) =>
        Panel.ShowBriefly(n, c, onDismissed);

    public void Hide() => Panel.Hide();
}
```

Add `[RequireComponent(typeof(ThrowSummaryPanel))]` on facade.

- [ ] **Step 3: Manual test — pre-throw slide banner**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/UI/ThrowSummaryPanel.cs Assets/Scripts/UI/ThrowSummaryBannerUI.cs
git commit -m "refactor(ui): extract ThrowSummaryPanel from banner facade"
```

---

### Task 8: HoleCompleteBannerUI facade + cutscene utility

**Files:**
- Modify: `Assets/Scripts/UI/HoleCompleteBannerUI.cs`
- Modify: `Assets/Scripts/UI/HoleCompleteCutsceneUI.cs`

- [ ] **Step 1: Replace HoleCompleteBannerUI with facade**

```csharp
public sealed class HoleCompleteBannerUI : MonoBehaviour
{
    MultiSlotSpriteBanner _banner;
    MultiSlotSpriteBanner Banner => _banner ??= GetComponent<MultiSlotSpriteBanner>();

    public float DisplaySeconds => Banner.DisplaySeconds;

    public static HoleCompleteBannerUI Ensure() =>
        GameplayCalloutHost.Ensure()?.GetComponentInChildren<HoleCompleteBannerUI>(true);

    public void Show(int strokes, int par) => Banner.Show(strokes, par);
    public void Hide() => Banner.Hide();
    public void BindSceneReferences() => Banner.BindSceneReferences();
    public void EnsureSceneLayout() => Banner.BindSceneReferences();
    public void ApplyEditorPreview() => Banner.ShowKind(HoleCompleteScoreKind.Par);
}
```

- [ ] **Step 2: Update HoleCompleteCutsceneUI**

Replace local `FindHudCanvas()` with `HudCanvasUtility.FindHudCanvas()` from `DiskGolf.UI.Callouts`.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/UI/HoleCompleteBannerUI.cs Assets/Scripts/UI/HoleCompleteCutsceneUI.cs
git commit -m "refactor(ui): facade hole-complete banner; shared HUD utility in cutscene"
```

---

### Task 9: Prefab authoring + editor menu

**Files:**
- Create: `Assets/Editor/CalloutPrefabAuthoring.cs`
- Create: `Assets/Prefabs/UI/GameplayCallouts.prefab` (via menu)

- [ ] **Step 1: Create editor authoring script**

Create `Assets/Editor/CalloutPrefabAuthoring.cs`:

```csharp
#if UNITY_EDITOR
using DiskGolf.Gameplay;
using DiskGolf.UI;
using DiskGolf.UI.Callouts;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiskGolf.EditorTools
{
    public static class CalloutPrefabAuthoring
    {
        const string MenuRoot = "Disk Golf/HUD/";

        [MenuItem(MenuRoot + "Create Gameplay Callouts Prefab")]
        public static void CreatePrefab()
        {
            System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");

            var root = new GameObject("GameplayCallouts", typeof(RectTransform), typeof(GameplayCalloutHost));
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            CreateSpriteChild(root.transform, "LieLandingBanner");
            CreateSpriteChild(root.transform, "OnTheGreenBanner");
            CreateTextChild(root.transform, "ThrowResultBanner", TextCalloutLayout.FeetLabel);
            CreateTextChild(root.transform, "SweetSpotBanner", TextCalloutLayout.CenterPopup);
            CreateHoleComplete(root.transform);
            CreateThrowSummary(root.transform);
            HoleCompleteCutsceneUI.CreateForScene(rt);

            var host = root.GetComponent<GameplayCalloutHost>();
            host.BindReferences();

            PrefabUtility.SaveAsPrefabAsset(root, ProjectArtPaths.Prefabs.GameplayCallouts);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[Disk Golf] Saved " + ProjectArtPaths.Prefabs.GameplayCallouts);
        }

        static void CreateSpriteChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(SpriteCalloutBanner));
            go.transform.SetParent(parent, false);
        }

        static void CreateTextChild(Transform parent, string name, TextCalloutLayout layout)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(TextCalloutBanner));
            go.transform.SetParent(parent, false);
            go.GetComponent<TextCalloutBanner>().ResolveLabel();
        }

        static void CreateHoleComplete(Transform parent)
        {
            var root = new GameObject("HoleCompleteBanner", typeof(RectTransform), typeof(MultiSlotSpriteBanner), typeof(HoleCompleteBannerUI));
            root.transform.SetParent(parent, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(640f, 160f);

            foreach (HoleCompleteScoreKind kind in System.Enum.GetValues(typeof(HoleCompleteScoreKind)))
            {
                var slot = new GameObject(kind.ToString(), typeof(RectTransform), typeof(Image));
                slot.transform.SetParent(root.transform, false);
                var srt = slot.GetComponent<RectTransform>();
                srt.anchorMin = Vector2.zero;
                srt.anchorMax = Vector2.one;
                srt.offsetMin = srt.offsetMax = Vector2.zero;
            }

            root.GetComponent<MultiSlotSpriteBanner>().BindSceneReferences();
        }

        static void CreateThrowSummary(Transform parent)
        {
            ThrowSummaryBannerUI.CreateForScene(parent as RectTransform);
        }

        [MenuItem(MenuRoot + "Instantiate Gameplay Callouts In Scene")]
        public static void InstantiateInScene()
        {
            var hud = GameObject.Find("GameplayHUD")?.GetComponent<RectTransform>();
            if (hud == null)
            {
                Debug.LogError("[Disk Golf] GameplayHUD not found.");
                return;
            }

            if (hud.Find(GameplayCalloutHost.RootName) != null)
            {
                Debug.LogWarning("[Disk Golf] GameplayCallouts already present.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectArtPaths.Prefabs.GameplayCallouts);
            if (prefab == null)
            {
                CreatePrefab();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectArtPaths.Prefabs.GameplayCallouts);
            }

            PrefabUtility.InstantiatePrefab(prefab, hud);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        }
    }
}
#endif
```

- [ ] **Step 2: Run menu**

Unity → **Disk Golf → HUD → Create Gameplay Callouts Prefab**

Expected: `Assets/Prefabs/UI/GameplayCallouts.prefab` created.

- [ ] **Step 3: Commit**

```bash
git add Assets/Editor/CalloutPrefabAuthoring.cs Assets/Prefabs/UI/
git commit -m "refactor(ui): add GameplayCallouts prefab authoring menu"
```

---

### Task 10: Update HudSceneAuthoring + scene builders

**Files:**
- Modify: `Assets/Editor/HudSceneAuthoring.cs`
- Modify: `Assets/Editor/PrototypeFlat3SceneBuilder.cs`

- [ ] **Step 1: Update EnsureScoreBanners in HudSceneAuthoring**

Replace procedural `HoleCompleteBannerUI.CreateForScene` / `OnTheGreenBannerUI.Ensure` calls with:

```csharp
CalloutPrefabAuthoring.InstantiateInScene();
```

Keep `WireThrowControllerBannerRefs` but add host wiring:

```csharp
var host = hud.GetComponentInChildren<GameplayCalloutHost>(true);
if (host != null)
    so.FindProperty("calloutHost").objectReferenceValue = host;
```

(ThrowController field added in Task 11.)

- [ ] **Step 2: Update PrototypeFlat3SceneBuilder**

Remove procedural `ThrowResultBannerUI` / `InTheCircleBanner` TMP creation for callouts.

After `HudCanvas` creation, call `CalloutPrefabAuthoring.InstantiateInScene()` or load prefab via `PrefabUtility.InstantiatePrefab`.

- [ ] **Step 3: Re-bake scenes**

Run on open scenes:
- **Disk Golf → HUD → Bake Score Banners**
- **Disk Golf → Course → Rebuild Course Editor Scene** (if needed)

Save `PrototypeFlat3.unity` and `CourseEditor.unity`.

- [ ] **Step 4: Run behavior matrix B01–B10**

- [ ] **Step 5: Commit**

```bash
git add Assets/Editor/HudSceneAuthoring.cs Assets/Editor/PrototypeFlat3SceneBuilder.cs Assets/Scenes/
git commit -m "refactor(ui): bake GameplayCallouts prefab into gameplay scenes"
```

---

### Task 11: Slim ThrowController + delete facades

**Files:**
- Modify: `Assets/Scripts/Core/ThrowController.cs`
- Delete: legacy banner facade files listed in File Map

- [ ] **Step 1: Add calloutHost field to ThrowController**

```csharp
[SerializeField] GameplayCalloutHost calloutHost;

// Keep legacy fields temporarily with [FormerlySerializedAs] if needed for scene migration
```

Replace usages:

| Old | New |
|-----|-----|
| `lieLandingBanner?.Show(lie)` | `calloutHost.LieLanding.Show(sprite)` via helper |
| `onTheGreenBanner?.Show()` | `calloutHost.OnTheGreen.Show(ScoreBannerSprites.OnTheGreen)` |
| `throwResultBanner?.ShowThrowDistance(ft)` | `calloutHost.ThrowDistance.ShowThrowDistance(ft)` |
| `sweetSpotBanner?.Show()` | `calloutHost.SweetSpot.ApplySweetSpotStyle(); calloutHost.SweetSpot.ShowBriefly("SWEET!")` |
| `holeCompleteBanner?.Show(s,p)` | `calloutHost.HoleComplete.Show(s,p)` |
| `throwSummaryBanner?.ShowBriefly(...)` | `calloutHost.ThrowSummary.ShowBriefly(...)` |

Awake:

```csharp
calloutHost ??= GameplayCalloutHost.Ensure();
```

- [ ] **Step 2: Delete facade files**

Remove:
- `LieLandingBannerUI.cs`
- `OnTheGreenBannerUI.cs`
- `ThrowResultBannerUI.cs`
- `SweetSpotBannerUI.cs`
- `HoleCompleteBannerUI.cs`
- `ThrowSummaryBannerUI.cs`

Update any remaining references (grep project).

- [ ] **Step 3: Full behavior matrix QA**

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/Core/ThrowController.cs
git add Assets/Scripts/UI/Callouts/
git rm Assets/Scripts/UI/LieLandingBannerUI.cs Assets/Scripts/UI/OnTheGreenBannerUI.cs Assets/Scripts/UI/ThrowResultBannerUI.cs Assets/Scripts/UI/SweetSpotBannerUI.cs Assets/Scripts/UI/HoleCompleteBannerUI.cs Assets/Scripts/UI/ThrowSummaryBannerUI.cs
git commit -m "refactor(ui): ThrowController uses GameplayCalloutHost; remove banner facades"
```

---

### Task 12: Docs + final verification

**Files:**
- Modify: `Assets/README.md`

- [ ] **Step 1: Add README section**

```markdown
## HUD callouts

- All post-throw banners live under `GameplayHUD/GameplayCallouts` (prefab).
- **Disk Golf → HUD → Create Gameplay Callouts Prefab** — generates `Assets/Prefabs/UI/GameplayCallouts.prefab`
- **Disk Golf → HUD → Instantiate Gameplay Callouts In Scene** — adds to active scene
- **Disk Golf → HUD → Bake Score Banners** — ensures prefab + wires ThrowController
```

- [ ] **Step 2: Run EditMode tests**

Test Runner → EditMode → all tests green.

- [ ] **Step 3: LOC check**

Banner cluster should be ~900–1,100 lines (grep `Callouts/` + `ThrowSummaryPanel` + `HoleCompleteCutsceneUI` + `ScoreBannerSprites`).

- [ ] **Step 4: Commit**

```bash
git add Assets/README.md docs/superpowers/specs/2026-05-31-callout-banner-consolidation-design.md docs/superpowers/plans/2026-05-31-callout-banner-consolidation.md
git commit -m "docs: callout banner consolidation spec, plan, and README"
```

---

## Spec Coverage (self-review)

| Spec requirement | Task |
|------------------|------|
| HudCanvasUtility single lookup | Task 1 |
| CalloutLifecycle auto-hide | Task 1 |
| SpriteCalloutBanner | Task 2 |
| TextCalloutBanner | Task 3 |
| MultiSlotSpriteBanner | Task 4 |
| Strangler facades | Tasks 5, 7, 8 |
| GameplayCalloutHost | Task 6 |
| GameplayCallouts prefab | Task 9 |
| Scene bake / builders | Task 10 |
| ThrowController simplification | Task 11 |
| Behavior matrix QA | Tasks 5, 10, 11 |
| EditMode tests | Task 4 |
| README | Task 12 |

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-31-callout-banner-consolidation.md`.

**Two execution options:**

1. **Subagent-Driven (recommended)** — fresh subagent per task, review between tasks, fast iteration
2. **Inline Execution** — implement tasks in this session with checkpoints

Which approach?

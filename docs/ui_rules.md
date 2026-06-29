# Disk Golf UI Rules

Guidelines for building menus, modals, HUD chrome, and list rows in this project. Follow these when adding or changing UI in code (`MenuSceneBuilder`, `*HudController`, `*HubController`) or in scenes.

**Goal:** Layout should survive different text lengths, resolutions, and future content without overlap, clipping, or one-off manual positioning fixes.

---

## 1. Layout system choice

| Pattern | Use when | Avoid |
|--------|----------|--------|
| **VerticalLayoutGroup / HorizontalLayoutGroup** | Modals, wizard steps, toolbars, list rows | Mixing with manual `anchoredPosition` on siblings |
| **Anchored top/center headers** | Step titles inside a panel with reserved nav/footer bands | Floating headers at arbitrary Y offsets per screen |
| **Dedicated nav/footer band** | Cancel / Back / Next / Confirm rows | Placing nav buttons at absolute panel coordinates |
| **LayoutElement** | Fixed widths (buttons), min heights (text blocks), flex (names) | Guessing pixel positions |

**Rule:** If children share a parent with a layout group, **do not** set `anchoredPosition` on those children. Only the layout group positions them.

---

## 2. Modal & popup standards

### Sizes (reference resolution 1920×1080)

| Type | Panel size | Notes |
|------|------------|--------|
| **Wizard / multi-step** | 820 × 540 | Title band top, content middle, nav bar bottom (72px) |
| **Confirmation (delete, etc.)** | 640 × 320 min | Title + body + button row; body must grow with text |
| **Simple alert** | 560 × 220 min | Single message + 1–2 buttons |

### Structure (always top → bottom)

1. **Backdrop** — full-screen, semi-transparent, `raycastTarget = true`
2. **Panel** — centered, `VerticalLayoutGroup`
3. **Title** — one line, `LayoutElement.preferredHeight` ≈ 40px
4. **Body** — word wrap on, `ContentSizeFitter` vertical preferred **or** `minHeight` ≥ 96px
5. **Button row** — separate child **below** body, `preferredHeight` 48–52px, never overlapping body

### Spacing

- Panel padding: **28px** all sides (confirm) / **24px** (wizard)
- Gap between sections: **16–20px**
- Gap between buttons: **12–16px**
- Button label inset: **10px** horizontal, **6px** vertical

### Button row

- **Center** action buttons in confirm dialogs (Cancel + Delete).
- **Right-align** only when paired with a left title in the same row (rare).
- Minimum button width: **140px**; multi-word labels (**Start Building**) → **≥ 240px**.
- Labels: `enableWordWrapping = false`, `overflowMode = Overflow`, centered text.

### Text

- Titles: 32–34px, bold, centered or left per modal type.
- Body: 24px, **word wrap enabled**, `overflowMode = Overflow` (not Ellipsis) for warnings.
- Never rely on a fixed 72px body height for 2+ lines of copy.

### Layering

- Parent modals under the scene **Canvas**.
- Call `SetAsLastSibling()` when opening so popups draw above lists/wizards.

---

## 3. Wizard / stepped flows

- **One shared nav bar** at panel bottom (`CreateWizardNavBar` pattern).
- **One shared title helper** (`CreateWizardTitle`) — same Y, font size, width on every step.
- Step content area: `offsetMin (24, 88)`, `offsetMax (-24, -88)` to clear title + nav.
- Selection lists: grey row background + green checkmark (`CreateWizardTemplateToggle`).
- Input fields on name steps: **center-aligned** text.

---

## 4. Lists & rows (hub, selectors)

- **Name** — `flexibleWidth = 1`, `minWidth` ~140px.
- **Stats** — separate columns (e.g. Par, Yardage), fixed widths; yardage **≥ 112px**, no ellipsis on numbers.
- **Actions** — fixed-width buttons grouped at the **right**; no large flexible spacer between stats and buttons.
- Card padding: **16px left**, **8px right** (tight to scrollbar).
- Scroll viewport: reserve **~22px** for vertical scrollbar.

---

## 5. Top bars & tool panels

- Top bar height: **88px** content + **8px** top inset; **12px** vertical padding inside clusters.
- Button height in bars: **40px** (not flush to bar edges).
- Align right-side bar actions over the **right tool panel** (620px column, 12px margin).
- Left cluster: hole name (flex), par, yardage.
- Right cluster: **Exit** (first), Save, Undo, Redo, Play — grouped over the 620px tool panel (no Export in editor).
- **Action / Type** panel: Action row (Paint, Course Object, Erase) drives the Type row below (paint surfaces vs course objects).

---

## 6. Text overflow policy

| Context | Wrapping | Overflow |
|---------|----------|----------|
| Modal body / warnings | On | Overflow |
| List yardage / stats | Off | Overflow (widen column instead of ellipsis) |
| Hub course names | Off | Ellipsis OK (column flexes first) |
| Buttons | Off | Overflow + widen button |
| Top-bar hole names | **Off** | Ellipsis after flex width (~280px min) |

**Single-line rule:** Button labels, par values, yardage, and short identifiers (hole names, par counts) must stay on **one row**. Widen the control or parent column — never allow a 6-letter label like "Cancel" to wrap. Set `enableWordWrapping = false` on all button TMP labels.

**Layout-group children:** Buttons in a `HorizontalLayoutGroup` must use **left-stretch anchors** (`anchorMin.x = 0`, `anchorMax.x = 0`, `sizeDelta.x = preferredWidth`). Center anchors `(0.5, 0.5)` at position zero stack every child on top of each other.

---

## 7. Selection & theme styling

- Idle row: grey background `~(0.34, 0.36, 0.40)`.
- Selected checkmark: project accent green.
- Selected/hover: slightly lighter grey on row.
- Use the same toggle row for templates **and** themes, even for a single option.

---

## 8. Runtime vs scene-authored UI

- **Gameplay HUD** (`GameplayHUD`): scene-authored; see `.cursor/rules/scene-authored-hud.mdc`.
- **Course editor hub, wizard, editor HUD**: mostly runtime-built; must follow this doc.
- Controllers that build modals must resolve Canvas via `GetComponentInParent` **or** `FindObjectOfType<Canvas>()` — hub controller may sit outside canvas hierarchy.
- On `EditorHudCanvas`, ensure `localScale = 1` and full-screen anchors before parenting runtime HUD.

---

## 9. Pre-ship checklist (agent)

Before finishing UI work, verify:

- [ ] No overlapping text and buttons at 1920×1080 and 1280×720
- [ ] Longest expected label fits (e.g. "Start Building", "Back To Main Menu" → prefer short labels)
- [ ] Wrapped body text fits without clipping; button row is **below** body in hierarchy
- [ ] Nav/button rows use layout groups, not absolute positions
- [ ] Popups centered; backdrop blocks clicks to content behind
- [ ] List numeric columns show full values (e.g. `249 yd`, not `2...`)
- [ ] Consistent fonts via `HudTypography.BindFont`
- [ ] Destructive confirm uses clear copy; Delete button uses danger color

---

## 10. Known helpers (reuse, don't reinvent)

| Helper | Location |
|--------|----------|
| Wizard shell | `MenuSceneBuilder.CreateNewHoleWizardOverlay` |
| Wizard nav/title | `CreateWizardNavBar`, `CreateWizardTitle` |
| Template/theme toggle row | `CreateWizardTemplateToggle` |
| Hub list scroll | `MenuSceneBuilder.CreateHoleListScrollView` |
| Editor top bar / panel | `CourseEditorHudController.BuildTopBar`, `BuildRightEditorPanel` |

When adding a new modal, copy the **wizard nav bar** or **delete modal** structure rather than new absolute coordinates.

---

## Lessons from recent fixes (don't repeat)

1. **Delete modal** — 72px body + right-aligned buttons overlapped wrapped text → fixed with taller panel, `ContentSizeFitter` on body, centered button row.
2. **Wizard** — absolute Cancel/Back/Next positions misaligned → shared bottom nav `HorizontalLayoutGroup`.
3. **Hub list** — flexible spacer ate width; yardage used ellipsis → split par/yardage columns, flex name only.
4. **Theme step** — plain label instead of toggle row → use template toggle styling.
5. **Editor top bar** — actions floated mid-screen → fixed 620px right column aligned to tool panel.
6. **EditorHudCanvas scale 0** — invisible HUD → reset rect transform on load.

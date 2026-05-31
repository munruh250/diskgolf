# Assets folder guide

This project organizes content **by game feature**, not by file type at the top level. Textures, materials, and prefabs for the same feature live together.

## Folder map

```
Assets/
├── Art/                    # All authored visual content
│   ├── Characters/         # Player, NPC sprites & mats
│   ├── Environment/        # Course, foliage, props, skybox
│   ├── Gameplay/           # Disc and other gameplay visuals
│   └── UI/                 # UI sprites, fonts, reference mockups
├── Audio/                  # Music, SFX (add when needed)
├── Data/                   # ScriptableObjects (discs, courses, tuning)
├── Prefabs/
│   ├── Gameplay/           # Disc, Basket, …
│   └── Course/             # TeePad, hazards, …
├── Resources/              # ⚠ Runtime bootstrap ONLY (see below)
├── Scenes/
│   └── Prototype/          # Greybox / WIP scenes
├── Scripts/                # Code by domain (Core, UI, Gameplay, …)
├── Settings/               # URP, input, physics layers
├── ThirdParty/             # TextMesh Pro, imported packages
├── Editor/                 # Unity editor tools
└── Tests/                  # Edit Mode tests
```

## Where to put new files

| You are adding… | Put it here | Name it |
|-----------------|-------------|---------|
| Texture / sprite PNG | `Art/<feature>/` next to its material | `TEX_<Feature>_<Role>.png` |
| Material | Same folder as its textures | `MAT_<Feature>.mat` |
| Prefab | `Prefabs/Gameplay/` or `Prefabs/Course/` | `PF_<Name>.prefab` (optional prefix) |
| ScriptableObject | `Data/<category>/` | Descriptive name, e.g. `Buzzz.asset` |
| C# script | `Assets/Scripts/<domain>/` | Match existing domain folders |
| UI reference mockup | `Art/UI/Reference/` | Descriptive PNG |
| Editor tool | `Assets/Editor/` | |
| Third-party package | `Assets/ThirdParty/` | Keep vendor folder name |

**Co-location rule:** fairway albedo + fairway material both live under  
`Art/Environment/Course/Fairway/` — not split across `Materials/`, `Textures/`, and `Resources/`.

## Resources folder (important)

`Assets/Resources/` is **not** general storage. Unity packs everything under `Resources/` into builds and loads it by string path.

Allowed here:

- `Resources/GameplayArtCatalog.asset` — single bootstrap catalog referencing art elsewhere

Do **not** add new PNGs or materials to `Resources/` without a code review. Prefer:

1. `[SerializeField]` references on components/prefabs, or  
2. `GameplayArtCatalog` entries + `ProjectArtPaths` constants

After moving art or adding sprites, run **Disk Golf → Refresh Gameplay Art Catalog** in Unity.

## Code paths

Canonical paths live in `Assets/Scripts/Gameplay/ProjectArtPaths.cs`.  
Update that file when adding a new art root — do not hardcode `Assets/...` strings in multiple places.

Runtime loading goes through `RuntimeArt` / `GameplayArtCatalog`.

## Naming conventions

- **Folders:** `PascalCase` (`Fairway`, `GrassLight`)
- **Textures:** `TEX_<Feature>_<Role>.png`
- **Materials:** `MAT_<Feature>.mat`
- **Scenes:** under `Scenes/Prototype/` until production-ready

## Team workflow

1. New art → feature folder under `Art/`
2. Foliage atlas changes → **Disk Golf → Reimport Foliage Sheet** (auto-slices to `Art/Environment/Foliage/Sprites/`)
3. After art moves → **Disk Golf → Refresh Gameplay Art Catalog**
4. Scene hierarchy changes → **Disk Golf → Organize And Clean Up Scene**
5. Always commit `.meta` files with assets

## Cursor / AI agents

See `.cursor/rules/asset-structure.mdc` — agents must follow this layout when creating files.

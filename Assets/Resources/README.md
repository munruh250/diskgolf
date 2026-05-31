# Runtime bootstrap only

This folder must stay minimal.

## Allowed

| File | Purpose |
|------|---------|
| `GameplayArtCatalog.asset` | References sprites/materials loaded at runtime without direct scene wiring |

## Not allowed

- Raw PNGs, materials, or prefabs (use `Assets/Art/` instead)
- “Convenience” copies of art already elsewhere

## After adding or moving art

The catalog is refreshed automatically when Unity loads the project.

See `Assets/README.md` for the full folder guide.

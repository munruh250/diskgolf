"""Regenerate hole_01.json with a wider, natural par-3 test layout."""
import json
import math
from pathlib import Path

TILE = 2.0
ORIGIN = (0.0, 0.0)
HOLE_LENGTH_TILES = 118  # ~236 m

# Fairway corridor (tile indices)
FAIRWAY_X = range(7, 12)   # 5 tiles = 10 m wide
ROUGH_INNER_X = (5, 6, 12, 13)
ROUGH_OUTER_X = (3, 4, 14, 15)
TREE_LINE_X = (2, 16, 17)


def height_profile(world_z: float) -> float:
    """Along-hole elevation: climb a big hill, then throw downhill toward the basket."""
    if world_z < 30:
        return world_z * 0.08
    if world_z < 55:
        t = (world_z - 30) / 25
        return 2.4 + t * 13.6  # up to ~16 m
    if world_z < 120:
        t = (world_z - 55) / 65
        return 16.0 - t * 13.0  # steep drop to ~3 m
    return 3.0 + (world_z - 120) * 0.05


def sample_height(gx: int, gy: int) -> float:
    world_z = gy * TILE
    world_x = gx * TILE
    along = height_profile(world_z)
    fairway_center_x = 9.5 * TILE
    lateral = math.exp(-((world_x - fairway_center_x) ** 2) / 90.0) * 2.5
    return round(along + lateral, 2)


def build_surface_tiles():
    tiles = []
    for y in range(HOLE_LENGTH_TILES + 1):
        for x in ROUGH_OUTER_X:
            tiles.append({"x": x, "y": y, "type": "rough"})
        for x in ROUGH_INNER_X:
            tiles.append({"x": x, "y": y, "type": "rough"})
        for x in FAIRWAY_X:
            tile_type = "fairway"
            if y == 0 and x == 9:
                tile_type = "tee"
            elif y == HOLE_LENGTH_TILES and x == 9:
                tile_type = "green"
            tiles.append({"x": x, "y": y, "type": tile_type})
    return tiles


def build_elevation():
    width = 20
    height = HOLE_LENGTH_TILES + 4
    heights = []
    for gy in range(height):
        for gx in range(width):
            heights.append(sample_height(gx, gy))
    return {"width": width, "height": height, "heights": heights}


def build_placements():
    placements = []
    tree_id = 0
    left_x = [6.5, 8.5, 10.5]
    right_x = [27.5, 29.5, 31.5, 33.5]
    for z in range(16, HOLE_LENGTH_TILES * 2 - 8, 12):
        row = left_x if (tree_id // 2) % 2 == 0 else right_x
        for i, world_x in enumerate(row):
            if tree_id % 3 == i % 3:
                placements.append({
                    "archetype": "tree_round",
                    "x": world_x,
                    "z": float(z + (i % 2) * 3),
                    "yaw": (-22 + i * 8) if world_x < 15 else (18 - i * 6),
                    "scale": round(0.92 + (tree_id % 4) * 0.06, 2),
                })
                tree_id += 1
    return placements


def build_hazards():
    return [
        {
            "id": "water_valley",
            "type": "water",
            "vertices": [
                {"x": 6, "y": 44},
                {"x": 13, "y": 44},
                {"x": 13, "y": 48},
                {"x": 6, "y": 48},
            ],
        },
        {
            "id": "ob_left",
            "type": "ob",
            "vertices": [
                {"x": 0, "y": 0},
                {"x": 0, "y": HOLE_LENGTH_TILES + 2},
                {"x": 2, "y": HOLE_LENGTH_TILES + 2},
                {"x": 2, "y": 0},
            ],
        },
        {
            "id": "ob_right",
            "type": "ob",
            "vertices": [
                {"x": 18, "y": 0},
                {"x": 18, "y": HOLE_LENGTH_TILES + 2},
                {"x": 20, "y": HOLE_LENGTH_TILES + 2},
                {"x": 20, "y": 0},
            ],
        },
    ]


def main():
    tee_x = ORIGIN[0] + 9 * TILE + TILE * 0.5
    tee_z = ORIGIN[1] + 0 * TILE + TILE * 0.5
    basket_x = ORIGIN[0] + 9 * TILE + TILE * 0.5
    basket_z = ORIGIN[1] + HOLE_LENGTH_TILES * TILE + TILE * 0.5

    data = {
        "schemaVersion": 1,
        "id": "hole_01",
        "name": "Hole 1 — Ridgeline Par 3",
        "units": "meters",
        "tileSize": TILE,
        "origin": list(ORIGIN),
        "themeId": "temperate",
        "surfaceTiles": build_surface_tiles(),
        "hole": {
            "tee": [tee_x, tee_z],
            "basket": [basket_x, basket_z],
            "par": 3,
            "circleRadiusFt": 33.0,
        },
        "elevation": build_elevation(),
        "hazards": build_hazards(),
        "placements": build_placements(),
    }

    out = json.dumps(data, indent=2) + "\n"
    root = Path(__file__).resolve().parent
    for rel in ("hole_01.json", "../hole_01.json"):
        path = (root / rel).resolve()
        path.write_text(out, encoding="utf-8")
        print(f"Wrote {path} ({len(data['surfaceTiles'])} tiles, {len(data['placements'])} trees)")


if __name__ == "__main__":
    main()

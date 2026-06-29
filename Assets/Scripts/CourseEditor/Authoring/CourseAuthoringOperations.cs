using System.Collections.Generic;
using UnityEngine;

namespace DiskGolf.CourseEditor.Authoring
{
    public static class CourseAuthoringOperations
    {
        public static bool TryPaintTile(HoleData data, CourseAuthoringState state, int x, int y)
        {
            data.TryGetTile(x, y, out SurfaceTileType existing);
            if (existing == state.BrushType)
            {
                EnsureElevationGrid(data);
                return false;
            }

            data.SetTile(x, y, state.BrushType);
            EnsureElevationGrid(data);
            return true;
        }

        public static bool TryEraseTile(HoleData data, CourseAuthoringState state, int x, int y)
        {
            bool changed = false;

            if (data.TryGetTile(x, y, out _))
            {
                data.ClearTile(x, y);
                changed = true;
            }

            if (data.TryGetHazardTile(x, y, out _))
            {
                data.ClearHazardTile(x, y);
                changed = true;
            }

            return changed;
        }

        public static bool TryPaintHazardTile(HoleData data, CourseAuthoringState state, int x, int y)
        {
            if (data.TryGetHazardTile(x, y, out var existing) && existing == state.HazardBrushType)
            {
                return false;
            }

            data.SetHazardTile(x, y, state.HazardBrushType);
            return true;
        }

        public static bool TryEraseHazardTile(HoleData data, CourseAuthoringState state, int x, int y)
        {
            if (!data.TryGetHazardTile(x, y, out _))
            {
                return false;
            }

            data.ClearHazardTile(x, y);
            return true;
        }

        public static bool TryPlaceTee(HoleData data, CourseAuthoringState state, int x, int y)
        {
            Vector3 center = CourseAuthoringGrid.TileCenterWorld(data, x, y);
            Vector2 marker = new Vector2(center.x, center.z);
            if (data.Hole.Tee == marker)
            {
                return false;
            }

            data.Hole.Tee = marker;
            return true;
        }

        public static bool TryPlaceBasket(HoleData data, CourseAuthoringState state, int x, int y)
        {
            Vector3 center = CourseAuthoringGrid.TileCenterWorld(data, x, y);
            Vector2 marker = new Vector2(center.x, center.z);
            if (data.Hole.Basket == marker)
            {
                return false;
            }

            data.Hole.Basket = marker;
            return true;
        }

        public static bool TryAppendHazardVertex(HoleData data, CourseAuthoringState state, int tileX, int tileY)
        {
            var corner = new Vector2Int(tileX, tileY);
            if (state.HazardDraftVertices.Count > 0
                && state.HazardDraftVertices[state.HazardDraftVertices.Count - 1] == corner)
            {
                return false;
            }

            state.HazardDraftVertices.Add(corner);
            return true;
        }

        public static bool TryCommitHazardPolygon(HoleData data, CourseAuthoringState state)
        {
            if (state.HazardDraftVertices.Count < 3)
            {
                return false;
            }

            string prefix = state.HazardBrushType == HazardType.OB ? "ob" : "water";
            int index = (data.Hazards?.Count ?? 0) + 1;
            data.Hazards.Add(new HazardPolygon(
                $"{prefix}_{index}",
                state.HazardBrushType,
                new List<Vector2Int>(state.HazardDraftVertices)));
            state.HazardDraftVertices.Clear();
            return true;
        }

        public static bool TryCancelHazardDraft(HoleData data, CourseAuthoringState state)
        {
            if (state.HazardDraftVertices.Count == 0)
            {
                return false;
            }

            state.HazardDraftVertices.Clear();
            return true;
        }

        public static bool TryElevateAt(HoleData data, CourseAuthoringState state, int centerX, int centerY, bool subtract)
        {
            EnsureElevationGrid(data);

            float delta = state.ElevateStrength * (subtract ? -1f : 1f);
            int radius = Mathf.Clamp(state.ElevateRadius, 1, 5);
            bool changed = false;

            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > radius * radius)
                    {
                        continue;
                    }

                    int x = centerX + dx;
                    int y = centerY + dy;
                    if (x < 0 || y < 0 || x >= data.Elevation.Width || y >= data.Elevation.Height)
                    {
                        continue;
                    }

                    if (state.ElevateSmooth)
                    {
                        changed |= SmoothCell(data, x, y);
                    }
                    else
                    {
                        float next = data.Elevation.Get(x, y) + delta;
                        data.Elevation.Set(x, y, next);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        public static void EnsureElevationGrid(HoleData data)
        {
            var size = HeightGridSampler.GridSizeForHole(data);
            if (data.Elevation == null)
            {
                data.Elevation = new ElevationGrid(size.x, size.y);
                return;
            }

            data.Elevation.EnsureSize(size.x, size.y);
        }

        static bool SmoothCell(HoleData data, int x, int y)
        {
            float sum = 0f;
            int count = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int sx = x + dx;
                    int sy = y + dy;
                    if (sx < 0 || sy < 0 || sx >= data.Elevation.Width || sy >= data.Elevation.Height)
                    {
                        continue;
                    }

                    sum += data.Elevation.Get(sx, sy);
                    count++;
                }
            }

            if (count == 0)
            {
                return false;
            }

            float average = sum / count;
            if (Mathf.Approximately(data.Elevation.Get(x, y), average))
            {
                return false;
            }

            data.Elevation.Set(x, y, average);
            return true;
        }
    }
}

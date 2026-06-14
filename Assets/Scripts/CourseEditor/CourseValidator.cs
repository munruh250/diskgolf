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

            ValidateHazards(data, result);

            return result;
        }

        static void ValidateHazards(HoleData data, ValidationResult result)
        {
            if (data.Hazards == null || data.Hazards.Count == 0)
            {
                return;
            }

            var paintedBounds = data.ComputeBoundsMax();

            foreach (var hazard in data.Hazards)
            {
                if (hazard?.Vertices == null || hazard.Vertices.Count < 3)
                {
                    result.Messages.Add(new ValidationMessage("W003", ValidationSeverity.Warning,
                        $"Hazard '{hazard?.Id ?? "unknown"}' needs at least 3 vertices."));
                    continue;
                }

                if (HasSelfIntersection(hazard.Vertices))
                {
                    result.Messages.Add(new ValidationMessage("W003", ValidationSeverity.Warning,
                        $"Hazard '{hazard.Id}' polygon self-intersects."));
                }

                foreach (var vertex in hazard.Vertices)
                {
                    if (vertex.x < -1 || vertex.y < -1 || vertex.x > paintedBounds.x + 1 || vertex.y > paintedBounds.y + 1)
                    {
                        result.Messages.Add(new ValidationMessage("W003", ValidationSeverity.Warning,
                            $"Hazard '{hazard.Id}' has vertices outside painted bounds."));
                        break;
                    }
                }
            }
        }

        static bool HasSelfIntersection(IReadOnlyList<Vector2Int> vertices)
        {
            int count = vertices.Count;
            if (count < 4)
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                Vector2 a1 = ToPoint(vertices[i]);
                Vector2 a2 = ToPoint(vertices[(i + 1) % count]);

                for (int j = i + 1; j < count; j++)
                {
                    if (j == i || j == i + 1 || (i == 0 && j == count - 1))
                    {
                        continue;
                    }

                    Vector2 b1 = ToPoint(vertices[j]);
                    Vector2 b2 = ToPoint(vertices[(j + 1) % count]);
                    if (SegmentsIntersect(a1, a2, b1, b2))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static Vector2 ToPoint(Vector2Int vertex) => new Vector2(vertex.x, vertex.y);

        static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            float d1 = Cross(p3, p4, p1);
            float d2 = Cross(p3, p4, p2);
            float d3 = Cross(p1, p2, p3);
            float d4 = Cross(p1, p2, p4);

            if (((d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f))
                && ((d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f)))
            {
                return true;
            }

            return false;
        }

        static float Cross(Vector2 a, Vector2 b, Vector2 c) =>
            (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

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

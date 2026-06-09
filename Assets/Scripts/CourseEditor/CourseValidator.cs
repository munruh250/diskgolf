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

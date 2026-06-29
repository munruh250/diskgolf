using System.Collections.Generic;
using UnityEngine;

namespace DiskGolf.CourseEditor.Authoring
{
    public sealed class PaintTileCommand : IAuthoringCommand
    {
        readonly HoleData _data;
        readonly int _x;
        readonly int _y;
        readonly bool _hadPrevious;
        readonly SurfaceTileType _previousType;
        readonly SurfaceTileType _newType;

        public PaintTileCommand(HoleData data, int x, int y, SurfaceTileType newType)
        {
            _data = data;
            _x = x;
            _y = y;
            _newType = newType;
            _hadPrevious = data.TryGetTile(x, y, out _previousType);
        }

        public void Execute()
        {
            _data.SetTile(_x, _y, _newType);
            CourseAuthoringOperations.EnsureElevationGrid(_data);
        }

        public void Undo()
        {
            if (_hadPrevious)
            {
                _data.SetTile(_x, _y, _previousType);
            }
            else
            {
                _data.ClearTile(_x, _y);
            }

            CourseAuthoringOperations.EnsureElevationGrid(_data);
        }
    }

    public sealed class EraseTileCommand : IAuthoringCommand
    {
        readonly HoleData _data;
        readonly int _x;
        readonly int _y;
        readonly bool _hadSurface;
        readonly SurfaceTileType _previousType;
        readonly bool _hadHazard;
        readonly HazardType _previousHazard;

        public EraseTileCommand(HoleData data, int x, int y)
        {
            _data = data;
            _x = x;
            _y = y;
            _hadSurface = data.TryGetTile(x, y, out _previousType);
            _hadHazard = data.TryGetHazardTile(x, y, out _previousHazard);
        }

        public void Execute()
        {
            _data.ClearTile(_x, _y);
            _data.ClearHazardTile(_x, _y);
        }

        public void Undo()
        {
            if (_hadSurface)
                _data.SetTile(_x, _y, _previousType);

            if (_hadHazard)
                _data.SetHazardTile(_x, _y, _previousHazard);

            if (_hadSurface)
                CourseAuthoringOperations.EnsureElevationGrid(_data);
        }
    }

    public sealed class PaintStrokeCommand : IAuthoringCommand
    {
        struct TileEntry
        {
            public int X;
            public int Y;
            public bool HadPrevious;
            public SurfaceTileType PreviousType;
        }

        readonly HoleData _data;
        readonly SurfaceTileType _newType;
        readonly List<TileEntry> _tiles = new();

        public PaintStrokeCommand(HoleData data, IReadOnlyList<Vector2Int> coords, SurfaceTileType newType)
        {
            _data = data;
            _newType = newType;

            for (int i = 0; i < coords.Count; i++)
            {
                var coord = coords[i];
                var entry = new TileEntry { X = coord.x, Y = coord.y };
                entry.HadPrevious = data.TryGetTile(coord.x, coord.y, out entry.PreviousType);
                _tiles.Add(entry);
            }
        }

        public void Execute()
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                var tile = _tiles[i];
                _data.SetTile(tile.X, tile.Y, _newType);
            }

            CourseAuthoringOperations.EnsureElevationGrid(_data);
        }

        public void Undo()
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                var tile = _tiles[i];
                if (tile.HadPrevious)
                {
                    _data.SetTile(tile.X, tile.Y, tile.PreviousType);
                }
                else
                {
                    _data.ClearTile(tile.X, tile.Y);
                }
            }

            CourseAuthoringOperations.EnsureElevationGrid(_data);
        }
    }

    public sealed class HazardPaintStrokeCommand : IAuthoringCommand
    {
        struct TileEntry
        {
            public int X;
            public int Y;
            public bool HadPrevious;
            public HazardType PreviousType;
        }

        readonly HoleData _data;
        readonly HazardType _newType;
        readonly List<TileEntry> _tiles = new();

        public HazardPaintStrokeCommand(HoleData data, IReadOnlyList<Vector2Int> coords, HazardType newType)
        {
            _data = data;
            _newType = newType;

            for (int i = 0; i < coords.Count; i++)
            {
                var coord = coords[i];
                var entry = new TileEntry { X = coord.x, Y = coord.y };
                entry.HadPrevious = data.TryGetHazardTile(coord.x, coord.y, out entry.PreviousType);
                _tiles.Add(entry);
            }
        }

        public void Execute()
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                var tile = _tiles[i];
                _data.SetHazardTile(tile.X, tile.Y, _newType);
            }
        }

        public void Undo()
        {
            for (int i = 0; i < _tiles.Count; i++)
            {
                var tile = _tiles[i];
                if (tile.HadPrevious)
                    _data.SetHazardTile(tile.X, tile.Y, tile.PreviousType);
                else
                    _data.ClearHazardTile(tile.X, tile.Y);
            }
        }
    }

    public sealed class PlaceTeeCommand : IAuthoringCommand
    {
        readonly HoleData _data;
        readonly Vector2 _previousTee;
        readonly Vector2 _newTee;

        public PlaceTeeCommand(HoleData data, int x, int y)
        {
            _data = data;
            _previousTee = data.Hole.Tee;
            Vector3 center = CourseAuthoringGrid.TileCenterWorld(data, x, y);
            _newTee = new Vector2(center.x, center.z);
        }

        public void Execute()
        {
            _data.Hole.Tee = _newTee;
        }

        public void Undo()
        {
            _data.Hole.Tee = _previousTee;
        }
    }

    public sealed class PlaceBasketCommand : IAuthoringCommand
    {
        readonly HoleData _data;
        readonly Vector2 _previousBasket;
        readonly Vector2 _newBasket;

        public PlaceBasketCommand(HoleData data, int x, int y)
        {
            _data = data;
            _previousBasket = data.Hole.Basket;
            Vector3 center = CourseAuthoringGrid.TileCenterWorld(data, x, y);
            _newBasket = new Vector2(center.x, center.z);
        }

        public void Execute()
        {
            _data.Hole.Basket = _newBasket;
        }

        public void Undo()
        {
            _data.Hole.Basket = _previousBasket;
        }
    }

    public sealed class PlaceFoliageCommand : IAuthoringCommand
    {
        readonly HoleData _data;
        readonly FoliagePlacement _placement;
        int _index = -1;

        public PlaceFoliageCommand(HoleData data, string archetype, int x, int y)
        {
            _data = data;
            Vector3 center = CourseAuthoringGrid.TileCenterWorld(data, x, y);
            _placement = new FoliagePlacement(archetype, center.x, center.z);
        }

        public void Execute()
        {
            _data.Placements ??= new List<FoliagePlacement>();
            _data.Placements.Add(_placement);
            _index = _data.Placements.Count - 1;
        }

        public void Undo()
        {
            if (_index < 0 || _index >= _data.Placements.Count)
                return;

            _data.Placements.RemoveAt(_index);
            _index = -1;
        }
    }

    public sealed class RemoveFoliageCommand : IAuthoringCommand
    {
        readonly HoleData _data;
        readonly int _index;
        readonly FoliagePlacement _removed;
        int _restoredIndex = -1;

        public RemoveFoliageCommand(HoleData data, int index)
        {
            _data = data;
            _index = index;
            _removed = data.Placements[index];
        }

        public void Execute()
        {
            if (_index < 0 || _index >= _data.Placements.Count)
                return;

            _data.Placements.RemoveAt(_index);
            _restoredIndex = _index;
        }

        public void Undo()
        {
            if (_removed == null || _restoredIndex < 0)
                return;

            _data.Placements ??= new List<FoliagePlacement>();
            _data.Placements.Insert(Mathf.Min(_restoredIndex, _data.Placements.Count), _removed);
        }
    }

    public sealed class ClearTeeCommand : IAuthoringCommand
    {
        readonly HoleData _data;
        readonly Vector2 _previousTee;

        public ClearTeeCommand(HoleData data)
        {
            _data = data;
            _previousTee = data.Hole.Tee;
        }

        public void Execute() => _data.Hole.Tee = Vector2.zero;

        public void Undo() => _data.Hole.Tee = _previousTee;
    }

    public sealed class ClearBasketCommand : IAuthoringCommand
    {
        readonly HoleData _data;
        readonly Vector2 _previousBasket;

        public ClearBasketCommand(HoleData data)
        {
            _data = data;
            _previousBasket = data.Hole.Basket;
        }

        public void Execute() => _data.Hole.Basket = Vector2.zero;

        public void Undo() => _data.Hole.Basket = _previousBasket;
    }

    public sealed class SetSkyboxCommand : IAuthoringCommand
    {
        readonly HoleData _data;
        readonly string _previousSkyboxId;
        readonly string _newSkyboxId;

        public SetSkyboxCommand(HoleData data, string newSkyboxId)
        {
            _data = data;
            _previousSkyboxId = data.Hole.SkyboxId;
            _newSkyboxId = newSkyboxId;
        }

        public void Execute() => _data.Hole.SkyboxId = _newSkyboxId;

        public void Undo() => _data.Hole.SkyboxId = _previousSkyboxId;
    }

    public sealed class AddHazardCommand : IAuthoringCommand
    {
        readonly HoleData _data;
        readonly HazardPolygon _hazard;

        public AddHazardCommand(HoleData data, HazardPolygon hazard)
        {
            _data = data;
            _hazard = hazard;
        }

        public void Execute()
        {
            _data.Hazards.Add(_hazard);
        }

        public void Undo()
        {
            _data.Hazards.Remove(_hazard);
        }
    }

    public sealed class RemoveHazardCommand : IAuthoringCommand
    {
        readonly HoleData _data;
        readonly int _index;
        readonly HazardPolygon _removed;

        public RemoveHazardCommand(HoleData data, int index)
        {
            _data = data;
            _index = index;
            _removed = data.Hazards[index];
        }

        public void Execute()
        {
            _data.Hazards.RemoveAt(_index);
        }

        public void Undo()
        {
            _data.Hazards.Insert(_index, _removed);
        }
    }

    public sealed class AppendHazardVertexCommand : IAuthoringCommand
    {
        readonly CourseAuthoringState _state;
        readonly Vector2Int _vertex;
        int _insertedIndex = -1;

        public AppendHazardVertexCommand(CourseAuthoringState state, int tileX, int tileY)
        {
            _state = state;
            _vertex = new Vector2Int(tileX, tileY);
        }

        public void Execute()
        {
            if (_state.HazardDraftVertices.Count > 0
                && _state.HazardDraftVertices[_state.HazardDraftVertices.Count - 1] == _vertex)
            {
                _insertedIndex = -1;
                return;
            }

            _insertedIndex = _state.HazardDraftVertices.Count;
            _state.HazardDraftVertices.Add(_vertex);
        }

        public void Undo()
        {
            if (_insertedIndex < 0 || _insertedIndex >= _state.HazardDraftVertices.Count)
                return;

            _state.HazardDraftVertices.RemoveAt(_insertedIndex);
        }
    }

    public sealed class CommitHazardDraftCommand : IAuthoringCommand
    {
        readonly HoleData _data;
        readonly CourseAuthoringState _state;
        readonly List<Vector2Int> _draftBeforeCommit;
        HazardPolygon _createdHazard;

        public CommitHazardDraftCommand(HoleData data, CourseAuthoringState state)
        {
            _data = data;
            _state = state;
            _draftBeforeCommit = new List<Vector2Int>(state.HazardDraftVertices);
        }

        public void Execute()
        {
            if (_draftBeforeCommit.Count < 3)
                return;

            if (_createdHazard == null)
            {
                string prefix = _state.HazardBrushType == HazardType.OB ? "ob" : "water";
                int index = (_data.Hazards?.Count ?? 0) + 1;
                _createdHazard = new HazardPolygon(
                    $"{prefix}_{index}",
                    _state.HazardBrushType,
                    new List<Vector2Int>(_draftBeforeCommit));
            }

            _data.Hazards.Add(_createdHazard);
            _state.HazardDraftVertices.Clear();
        }

        public void Undo()
        {
            if (_createdHazard == null)
                return;

            _data.Hazards.Remove(_createdHazard);
            _state.HazardDraftVertices.Clear();
            _state.HazardDraftVertices.AddRange(_draftBeforeCommit);
        }
    }
}

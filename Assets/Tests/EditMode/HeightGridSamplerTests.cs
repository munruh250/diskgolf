using DiskGolf.CourseEditor;
using DiskGolf.CourseEditor.Authoring;
using NUnit.Framework;
using UnityEngine;

namespace DiskGolf.Tests.EditMode
{
    public sealed class HeightGridSamplerTests
    {
        [Test]
        public void SampleTileSurfaceWorldY_MatchesTileCornerBilinearInterpolation()
        {
            var data = new HoleData
            {
                Origin = Vector2.zero,
                TileSize = 2f
            };
            CourseAuthoringOperations.EnsureElevationGrid(data);
            data.Elevation.Set(0, 0, 1f);
            data.Elevation.Set(1, 0, 2f);
            data.Elevation.Set(0, 1, 3f);
            data.Elevation.Set(1, 1, 4f);

            float center = HeightGridSampler.SampleTileSurfaceWorldY(data, 1f, 1f);
            Assert.AreEqual(2.5f, center, 0.001f);
        }
    }
}

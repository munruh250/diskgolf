using DiskGolf.Disc;
using DiskGolf.Flight;
using NUnit.Framework;
using UnityEngine;

namespace DiskGolf.Tests
{
    public class FlightSimulatorTests
    {
        DiscProfile MakeDisc(int speed, int glide, int turn, int fade, float maxFt)
        {
            var d = ScriptableObject.CreateInstance<DiscProfile>();
            d.speed = speed;
            d.glide = glide;
            d.turn = turn;
            d.fade = fade;
            d.maxDistanceFt = maxFt;
            return d;
        }

        ThrowInput MakeInput(DiscProfile disc, ReleaseAngle angle, float power,
            ThrowHeight height, float windSpeed = 0f)
        {
            return new ThrowInput(
                disc, angle, power, height,
                new WindSettings { direction = Vector2.right, speedMph = windSpeed },
                Vector3.zero,
                Vector3.forward);
        }

        [Test]
        public void Compute_FullPowerMid_DistanceNearMax()
        {
            var buzzz = MakeDisc(5, 4, -1, 1, 250f);
            var path = FlightSimulator.Compute(MakeInput(buzzz, ReleaseAngle.Flat, 1f, ThrowHeight.Nice));
            Assert.That(path.TotalDistanceFt, Is.InRange(200f, 280f));
        }

        [Test]
        public void Compute_Anhyzer_MoreTurnThanHyzer()
        {
            var disc = MakeDisc(5, 4, -2, 1, 250f);
            var hyzer = FlightSimulator.Compute(MakeInput(disc, ReleaseAngle.Hyzer, 1f, ThrowHeight.Nice));
            var anhyzer = FlightSimulator.Compute(MakeInput(disc, ReleaseAngle.Anhyzer, 1f, ThrowHeight.Nice));
            float HyzerLateral(FlightPath p) => p.Waypoints[p.Waypoints.Count / 3].Position.x;
            Assert.That(Mathf.Abs(HyzerLateral(anhyzer)), Is.GreaterThan(Mathf.Abs(HyzerLateral(hyzer))));
        }

        [Test]
        public void Compute_UnderpoweredDriver_ShorterThanFullPower()
        {
            var destroyer = MakeDisc(12, 5, -1, 3, 420f);
            var full = FlightSimulator.Compute(MakeInput(destroyer, ReleaseAngle.Flat, 1f, ThrowHeight.Nice));
            var weak = FlightSimulator.Compute(MakeInput(destroyer, ReleaseAngle.Flat, 0.5f, ThrowHeight.Nice));
            Assert.That(weak.TotalDistanceFt, Is.LessThan(full.TotalDistanceFt * 0.75f));
        }

        [Test]
        public void Compute_WindDriftsDownwind()
        {
            var disc = MakeDisc(5, 4, 0, 1, 250f);
            var noWind = FlightSimulator.Compute(MakeInput(disc, ReleaseAngle.Flat, 1f, ThrowHeight.Nice, 0f));
            var wind = FlightSimulator.Compute(MakeInput(disc, ReleaseAngle.Flat, 1f, ThrowHeight.High, 10f));
            var noWindEnd = noWind.Waypoints[noWind.Waypoints.Count - 1].Position;
            var windEnd = wind.Waypoints[wind.Waypoints.Count - 1].Position;
            Assert.That(windEnd.x, Is.GreaterThan(noWindEnd.x + 1f));
        }
    }
}

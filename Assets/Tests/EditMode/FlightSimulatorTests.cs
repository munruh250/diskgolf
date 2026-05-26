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
            var buzzz = MakeDisc(5, 4, -1, 1, 320f);
            var path = FlightSimulator.Compute(MakeInput(buzzz, ReleaseAngle.Flat, 1f, ThrowHeight.Nice));
            Assert.That(path.TotalDistanceFt, Is.InRange(275f, 350f));
        }

        [Test]
        public void Compute_FullPowerMid_PeakHeightInStandardRange()
        {
            var buzzz = MakeDisc(5, 4, -1, 1, 320f);
            var path = FlightSimulator.Compute(MakeInput(buzzz, ReleaseAngle.Flat, 1f, ThrowHeight.Nice));
            float peakY = 0f;
            foreach (var wp in path.Waypoints)
                peakY = Mathf.Max(peakY, wp.Position.y);

            float peakFt = peakY / 0.3048f;
            Assert.That(peakFt, Is.InRange(15f, 42f));
        }

        [Test]
        public void Compute_FullPowerHigh_PeakHeightInBomberRange()
        {
            var destroyer = MakeDisc(12, 5, -1, 3, 450f);
            var path = FlightSimulator.Compute(MakeInput(destroyer, ReleaseAngle.Flat, 1f, ThrowHeight.High));
            float peakY = 0f;
            foreach (var wp in path.Waypoints)
                peakY = Mathf.Max(peakY, wp.Position.y);

            float peakFt = peakY / 0.3048f;
            Assert.That(peakFt, Is.InRange(50f, 92f));
        }

        [Test]
        public void Compute_FullPowerDriver_DistanceNearMax()
        {
            var destroyer = MakeDisc(12, 5, -1, 3, 450f);
            var path = FlightSimulator.Compute(MakeInput(destroyer, ReleaseAngle.Flat, 1f, ThrowHeight.Nice));
            Assert.That(path.TotalDistanceFt, Is.InRange(400f, 470f));
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
        [Test]
        public void Compute_ShortThrow_HasDiscLikeHangTime()
        {
            var buzzz = MakeDisc(5, 4, -1, 1, 320f);
            var path = FlightSimulator.Compute(MakeInput(buzzz, ReleaseAngle.Flat, 0.45f, ThrowHeight.Nice));
            float duration = path.Waypoints[path.Waypoints.Count - 1].Time;
            Assert.That(path.TotalDistanceFt, Is.InRange(100f, 160f));
            Assert.That(duration, Is.InRange(3.8f, 5.5f));
        }

        [Test]
        public void Compute_FullPowerDriver_FlightTimeScalesWithDistance()
        {
            var destroyer = MakeDisc(12, 5, -1, 3, 450f);
            var path = FlightSimulator.Compute(MakeInput(destroyer, ReleaseAngle.Flat, 1f, ThrowHeight.Nice));
            float duration = path.Waypoints[path.Waypoints.Count - 1].Time;
            Assert.That(duration, Is.GreaterThan(8f));
        }
    }
}

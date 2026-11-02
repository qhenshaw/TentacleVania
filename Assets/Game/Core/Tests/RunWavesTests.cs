using System.Collections.Generic;
using NUnit.Framework;
using TentacleVania.Core.Runs;

namespace TentacleVania.Core.Tests
{
    public sealed class RunWavesTests
    {
        [Test]
        public void EveryWaveHasPrey()
        {
            var rng = new System.Random(4);
            for (var step = 1; step < 12; step++)
            {
                var plan = RunWaves.Plan(rng, 5, 1, RoomRole.Combat, step);
                var prey = new HashSet<int>();
                foreach (var slot in plan)
                    if (slot.Prey)
                        prey.Add(slot.Wave);
                Assert.That(prey.Count, Is.EqualTo(RunWaves.WaveCount(RoomRole.Combat, step)));
            }
        }

        [Test]
        public void DeeperRoomsSpawnMore()
        {
            var shallow = RunWaves.Plan(new System.Random(1), 5, 1, RoomRole.Combat, 1).Count;
            var deep = RunWaves.Plan(new System.Random(1), 5, 1, RoomRole.Combat, 10).Count;
            Assert.That(deep, Is.GreaterThan(shallow));
            Assert.That(RunWaves.EnemiesIn(5, 100), Is.EqualTo(RunWaves.MaxCount));
        }

        [Test]
        public void EliteRoomsLeadEachWaveWithAnElite()
        {
            var plan = RunWaves.Plan(new System.Random(2), 5, 1, RoomRole.Elite, 6);
            var elites = 0;
            foreach (var slot in plan)
            {
                if (slot.Elite)
                    elites++;
                Assert.That(slot.Prey && slot.Elite, Is.False);
            }
            Assert.That(elites, Is.EqualTo(RunWaves.EliteWaves));
        }

        [Test]
        public void RoomDepthScalesLinearly()
        {
            Assert.That(Progression.DifficultyCurve.RoomScale(0, 0.04f), Is.EqualTo(1f));
            Assert.That(Progression.DifficultyCurve.RoomScale(10, 0.04f), Is.EqualTo(1.4f).Within(1e-5f));
            Assert.That(Progression.DifficultyCurve.RoomScale(-3, 0.04f), Is.EqualTo(1f));
        }

        [Test]
        public void EmptyPoolPlansNothing() => Assert.That(RunWaves.Plan(new System.Random(3), 0, 1, RoomRole.Combat, 3), Is.Empty);
    }
}

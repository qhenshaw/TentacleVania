using NUnit.Framework;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Tests
{
    public sealed class MapProgressTests
    {
        [SetUp]
        public void SetUp() => SaveSystem.NewGame(0);

        [Test]
        public void RoomsAreExploredOnceAndSaved()
        {
            Assert.That(MapProgress.Explore("SH_A1"), Is.True);
            Assert.That(MapProgress.Explore("SH_A1"), Is.False);
            Assert.That(MapProgress.IsExplored("SH_A1"), Is.True);
            Assert.That(SaveSystem.Data.World.ExploredRooms, Is.EquivalentTo(new[] { "SH_A1" }));
            MapProgress.Forget("SH_A1");
            Assert.That(MapProgress.IsExplored("SH_A1"), Is.False);
        }

        [Test]
        public void SetRespawnNeedsAGrownNestAndRaisesNoRest()
        {
            var rested = false;
            Nests.Rested += _ => rested = true;
            Assert.That(Nests.SetRespawn("SH/Nest2", "SpecimenHolding", Vector3.one, 90f), Is.False);
            Nests.Grow("SH/Nest2");
            Assert.That(Nests.SetRespawn("SH/Nest2", "SpecimenHolding", Vector3.one, 90f), Is.True);
            Assert.That(Nests.LastNest, Is.EqualTo("SH/Nest2"));
            Assert.That(Nests.TryGetRespawn("SpecimenHolding", out var position, out var yaw), Is.True);
            Assert.That(position, Is.EqualTo(Vector3.one));
            Assert.That(yaw, Is.EqualTo(90f));
            Assert.That(rested, Is.False);
        }
    }
}

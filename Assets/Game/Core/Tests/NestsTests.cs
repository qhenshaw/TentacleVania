using NUnit.Framework;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Tests
{
    public sealed class NestsTests
    {
        [SetUp]
        public void SetUp() => SaveSystem.NewGame(0);

        [Test]
        public void RestingNeedsAGrownNest()
        {
            Nests.Rest("SH_Entry", "SH_PodChamber", Vector3.one, 90f);
            Assert.That(Nests.LastNest, Is.Empty);
            Assert.That(Nests.Grow("SH_Entry"), Is.True);
            Assert.That(Nests.Grow("SH_Entry"), Is.False);
            Nests.Rest("SH_Entry", "SH_PodChamber", Vector3.one, 90f);
            Assert.That(Nests.LastNest, Is.EqualTo("SH_Entry"));
            Assert.That(Nests.RespawnScene, Is.EqualTo("SH_PodChamber"));
        }

        [Test]
        public void RespawnIsTheLastRestedNestInItsScene()
        {
            Assert.That(Nests.TryGetRespawn("SH_PodChamber", out _, out _), Is.False);
            Nests.Grow("SH_Entry");
            Nests.Grow("SH_PreBoss");
            Nests.Rest("SH_Entry", "SH_PodChamber", new Vector3(1f, 0f, 2f), 0f);
            Nests.Rest("SH_PreBoss", "SH_BossAnteroom", new Vector3(5f, 0f, 6f), 180f);
            Assert.That(Nests.TryGetRespawn("SH_PodChamber", out _, out _), Is.False);
            Assert.That(Nests.TryGetRespawn("SH_BossAnteroom", out var position, out var yaw), Is.True);
            Assert.That(position, Is.EqualTo(new Vector3(5f, 0f, 6f)));
            Assert.That(yaw, Is.EqualTo(180f));
        }

        [Test]
        public void OnlyBossArenasStayCleared()
        {
            Assert.That(Nests.StaysCleared(true), Is.True);
            Assert.That(Nests.StaysCleared(false), Is.False);
        }
    }
}

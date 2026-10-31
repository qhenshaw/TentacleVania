using NUnit.Framework;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;

namespace TentacleVania.Core.Tests
{
    public sealed class MutationLoadoutTests
    {
        [SetUp]
        public void SetUp() => SaveSystem.NewGame(0);

        [Test]
        public void PoolGrowsWithSizeStageAndBonusNotches()
        {
            Assert.That(MutationLoadout.PoolFor(0, 0), Is.EqualTo(3));
            Assert.That(MutationLoadout.PoolFor(1, 0), Is.EqualTo(5));
            Assert.That(MutationLoadout.PoolFor(3, 3), Is.EqualTo(12));
            Assert.That(MutationLoadout.NotchPool, Is.EqualTo(3));
        }

        [Test]
        public void OnlyOwnedMutationsThatFitCanBeEquipped()
        {
            Assert.That(MutationLoadout.Equip("Gluttony", 1, 0), Is.False);
            MutationLoadout.Grant("Gluttony");
            MutationLoadout.Grant("LungingMaw");
            MutationLoadout.Grant("WideDodge");
            Assert.That(MutationLoadout.Equip("Gluttony", 1, 0), Is.True);
            Assert.That(MutationLoadout.Equip("Gluttony", 1, 1), Is.False);
            Assert.That(MutationLoadout.Equip("LungingMaw", 2, 1), Is.True);
            Assert.That(MutationLoadout.Equip("WideDodge", 1, 3), Is.False);
            Assert.That(MutationLoadout.Unequip("LungingMaw"), Is.True);
            Assert.That(MutationLoadout.Equip("WideDodge", 1, 1), Is.True);
            Assert.That(SaveSystem.Data.Build.Mutations, Is.EquivalentTo(new[] { "Gluttony", "WideDodge" }));
        }

        [Test]
        public void GrantingTwiceOwnsOnce()
        {
            Assert.That(MutationLoadout.Grant("Gluttony"), Is.True);
            Assert.That(MutationLoadout.Grant("Gluttony"), Is.False);
            Assert.That(MutationLoadout.Owned.Count, Is.EqualTo(1));
        }
    }
}

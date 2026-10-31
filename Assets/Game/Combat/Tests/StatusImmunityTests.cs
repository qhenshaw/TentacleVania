using NUnit.Framework;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;

namespace TentacleVania.Combat.Tests
{
    public sealed class StatusImmunityTests
    {
        [Test]
        public void ControlImmunityBlocksRootAndSlowOnly()
        {
            Assert.That(StatusEffects.Blocked(StatusKind.Root, true), Is.True);
            Assert.That(StatusEffects.Blocked(StatusKind.Slow, true), Is.True);
            Assert.That(StatusEffects.Blocked(StatusKind.Root, false), Is.False);
        }
    }
}

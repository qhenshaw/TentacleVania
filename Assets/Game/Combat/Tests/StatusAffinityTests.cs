using NUnit.Framework;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;

namespace TentacleVania.Combat.Tests
{
    public sealed class StatusAffinityTests
    {
        [Test]
        public void WeakAndResistScaleByAQuarter()
        {
            var affinity = new StatusAffinity();
            affinity.Configure(new[] { StatusKind.Bleed, StatusKind.Burn }, new[] { StatusKind.Static });
            Assert.That(affinity.Scale(StatusKind.Bleed), Is.EqualTo(1.25f));
            Assert.That(affinity.Scale(StatusKind.Burn), Is.EqualTo(1.25f));
            Assert.That(affinity.Scale(StatusKind.Static), Is.EqualTo(0.75f));
            Assert.That(affinity.Scale(StatusKind.Corrode), Is.EqualTo(1f));
            Assert.That(affinity.IsWeak(StatusKind.Bleed), Is.True);
            Assert.That(affinity.Resists(StatusKind.Static), Is.True);
        }

        [Test]
        public void ReconfiguringClearsTheOldTable()
        {
            var affinity = new StatusAffinity();
            affinity.Configure(new[] { StatusKind.Bleed }, null);
            affinity.Configure(null, new[] { StatusKind.Parasite });
            Assert.That(affinity.Scale(StatusKind.Bleed), Is.EqualTo(1f));
            Assert.That(affinity.Scale(StatusKind.Parasite), Is.EqualTo(0.75f));
            Assert.That(affinity.Scale(StatusKind.None), Is.EqualTo(1f));
        }

        [Test]
        public void WeakBleedDealsAQuarterMore()
        {
            var normal = new DamageOverTime();
            var weak = new DamageOverTime();
            normal.Apply(3f, 5f, 5, 0f, 0.5f);
            weak.Apply(3f * StatusAffinity.Weak, 5f, 5, 0f, 0.5f);
            Assert.That(weak.Tick(5f, 0.5f), Is.EqualTo(normal.Tick(5f, 0.5f) * 1.25f).Within(1e-3f));
        }
    }
}

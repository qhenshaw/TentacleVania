using NUnit.Framework;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;

namespace TentacleVania.Combat.Tests
{
    public sealed class StatModifierTests
    {
        private const float Tolerance = 1e-3f;

        [SetUp]
        public void SetUp() => SaveSystem.NewGame(0);

        [TearDown]
        public void TearDown() => SaveSystem.NewGame(0);

        [Test]
        public void BrutalityStacksMultiplicativelyWithOtherBonuses()
        {
            Stats.Set(StatKind.Brutality, 21);
            var outgoing = new ModifierStack();
            outgoing.Add(new OutgoingStatModifier());
            outgoing.Add(new ScaleModifier { HealthScale = 1.5f });
            var pool = new HealthPool(100f);
            var result = DamagePipeline.Resolve(DamageInfo.Create(12f, Team.Creature, stagger: 10f), Team.Site, ref pool, outgoing, null);
            Assert.That(result.HealthDamage, Is.EqualTo(12f * 1.585f * 1.5f).Within(Tolerance));
            Assert.That(result.StaggerDamage, Is.EqualTo(10f * 1.585f).Within(Tolerance));
        }

        [Test]
        public void AbilityHitsUseCunningNotBrutality()
        {
            Stats.Set(StatKind.Brutality, 21);
            Stats.Set(StatKind.Cunning, 11);
            var outgoing = new ModifierStack();
            outgoing.Add(new OutgoingStatModifier());
            var pool = new HealthPool(100f);
            var info = DamageInfo.Create(25f, Team.Creature, stagger: 10f);
            info.Flags = HitFlags.Ability;
            var result = DamagePipeline.Resolve(info, Team.Site, ref pool, outgoing, null);
            Assert.That(result.HealthDamage, Is.EqualTo(25f * 1.2f).Within(Tolerance));
            Assert.That(result.StaggerDamage, Is.EqualTo(10f).Within(Tolerance));
        }

        [Test]
        public void TenacityReducesDamageTaken()
        {
            Stats.Set(StatKind.Tenacity, 21);
            var incoming = new ModifierStack();
            incoming.Add(new IncomingStatModifier());
            var pool = new HealthPool(100f);
            var result = DamagePipeline.Resolve(DamageInfo.Create(20f, Team.Site), Team.Creature, ref pool, null, incoming);
            Assert.That(result.HealthDamage, Is.EqualTo(20f * 0.9025f).Within(Tolerance));
        }
    }
}

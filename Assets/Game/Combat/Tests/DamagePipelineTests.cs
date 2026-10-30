using NUnit.Framework;
using TentacleVania.Combat.Damage;

namespace TentacleVania.Combat.Tests
{
    public class DamagePipelineTests
    {
        private const float Tolerance = 1e-4f;

        private static DamageResult Hit(ref HealthPool pool, DamageInfo info, Team target = Team.Site, ModifierStack outgoing = null, ModifierStack incoming = null,
            float dealt = 1f, float taken = 1f) =>
            DamagePipeline.Resolve(info, target, ref pool, outgoing, incoming, dealt, taken);

        [Test]
        public void PlainHitTakesHealth()
        {
            var pool = new HealthPool(40f);
            var result = Hit(ref pool, DamageInfo.Create(12f, Team.Creature, stagger: 5f));
            Assert.IsTrue(result.Applied);
            Assert.AreEqual(12f, result.HealthDamage, Tolerance);
            Assert.AreEqual(5f, result.StaggerDamage, Tolerance);
            Assert.AreEqual(28f, pool.Health, Tolerance);
            Assert.IsFalse(result.Killed);
        }

        [TestCase(Team.Creature, Team.Creature, false)]
        [TestCase(Team.Site, Team.Site, false)]
        [TestCase(Team.Creature, Team.Site, true)]
        [TestCase(Team.Site, Team.Creature, true)]
        [TestCase(Team.Environment, Team.Creature, true)]
        [TestCase(Team.Environment, Team.Site, true)]
        public void TeamRulesDecideWhoCanBeHurt(Team source, Team target, bool hurts)
        {
            var pool = new HealthPool(100f);
            var result = Hit(ref pool, DamageInfo.Create(10f, source, DamageType.Acid), target);
            Assert.AreEqual(hurts, result.Applied);
            Assert.AreEqual(hurts ? 90f : 100f, pool.Health, Tolerance);
        }

        [Test]
        public void FullArmorCutsHealthDamageBy30Percent()
        {
            var pool = new HealthPool(100f, 50f);
            var result = Hit(ref pool, DamageInfo.Create(20f, Team.Creature));
            Assert.AreEqual(14f, result.HealthDamage, Tolerance);
            Assert.AreEqual(20f, result.ArmorDamage, Tolerance);
            Assert.AreEqual(30f, pool.Armor, Tolerance);
        }

        [Test]
        public void ArmorReductionScalesWithRemainingArmor()
        {
            var pool = new HealthPool(100f, 50f) { Armor = 25f };
            var result = Hit(ref pool, DamageInfo.Create(20f, Team.Creature));
            Assert.AreEqual(20f * (1f - 0.15f), result.HealthDamage, Tolerance);
        }

        [Test]
        public void ArmorDamageScaleOnlyAffectsArmor()
        {
            var pool = new HealthPool(100f, 100f);
            var info = DamageInfo.Create(10f, Team.Creature);
            info.ArmorDamageScale = 3f;
            var result = Hit(ref pool, info);
            Assert.AreEqual(30f, result.ArmorDamage, Tolerance);
            Assert.AreEqual(7f, result.HealthDamage, Tolerance);
        }

        [Test]
        public void ModifiersStackMultiplicatively()
        {
            var incoming = new ModifierStack();
            incoming.Add(new ScaleModifier { HealthScale = 1.5f });
            incoming.Add(new ScaleModifier { HealthScale = 0.5f });
            var outgoing = new ModifierStack();
            outgoing.Add(new ScaleModifier { HealthScale = 2f, StaggerScale = 3f });
            var pool = new HealthPool(100f);
            var result = Hit(ref pool, DamageInfo.Create(10f, Team.Creature, stagger: 1f), outgoing: outgoing, incoming: incoming);
            Assert.AreEqual(15f, result.HealthDamage, Tolerance);
            Assert.AreEqual(3f, result.StaggerDamage, Tolerance);
        }

        [Test]
        public void TypedModifierIgnoresOtherTypes()
        {
            var incoming = new ModifierStack();
            incoming.Add(new ScaleModifier { HealthScale = 0f, FilterByType = true, Type = DamageType.Fire });
            var pool = new HealthPool(100f);
            Assert.AreEqual(0f, Hit(ref pool, DamageInfo.Create(10f, Team.Site, DamageType.Fire), Team.Creature, incoming: incoming).HealthDamage, Tolerance);
            Assert.AreEqual(10f, Hit(ref pool, DamageInfo.Create(10f, Team.Site, DamageType.Electric), Team.Creature, incoming: incoming).HealthDamage, Tolerance);
        }

        [Test]
        public void CancellingModifierBlocksTheHit()
        {
            var incoming = new ModifierStack();
            incoming.Add(new Cancel());
            var pool = new HealthPool(100f);
            var result = Hit(ref pool, DamageInfo.Create(50f, Team.Site), Team.Creature, incoming: incoming);
            Assert.AreEqual(DamageOutcome.Blocked, result.Outcome);
            Assert.AreEqual(100f, pool.Health, Tolerance);
        }

        [Test]
        public void ModifiersRunInOrder()
        {
            var stack = new ModifierStack();
            var log = new System.Text.StringBuilder();
            stack.Add(new Logger(5, "b", log));
            stack.Add(new Logger(-1, "a", log));
            stack.Add(new Logger(9, "c", log));
            var context = new DamageContext(DamageInfo.Create(1f, Team.Site), Team.Creature);
            stack.Apply(ref context);
            Assert.AreEqual("abc", log.ToString());
        }

        [Test]
        public void AssistScalesApplyByTeam()
        {
            var pool = new HealthPool(100f);
            Assert.AreEqual(5f, Hit(ref pool, DamageInfo.Create(10f, Team.Creature), Team.Site, dealt: 0.5f, taken: 2f).HealthDamage, Tolerance);
            pool = new HealthPool(100f);
            Assert.AreEqual(20f, Hit(ref pool, DamageInfo.Create(10f, Team.Site), Team.Creature, dealt: 0.5f, taken: 2f).HealthDamage, Tolerance);
        }

        [Test]
        public void KillingBlowReportsOnceAndOverkillIsClamped()
        {
            var pool = new HealthPool(20f);
            var result = Hit(ref pool, DamageInfo.Create(50f, Team.Creature));
            Assert.IsTrue(result.Killed);
            Assert.AreEqual(20f, result.HealthDamage, Tolerance);
            Assert.AreEqual(DamageOutcome.Ignored, Hit(ref pool, DamageInfo.Create(5f, Team.Creature)).Outcome);
        }

        private sealed class Cancel : IDamageModifier
        {
            public int Order => 0;
            public void Modify(ref DamageContext context) => context.Cancelled = true;
        }

        private sealed class Logger : IDamageModifier
        {
            private readonly string _name;
            private readonly System.Text.StringBuilder _log;

            public Logger(int order, string name, System.Text.StringBuilder log)
            {
                Order = order;
                _name = name;
                _log = log;
            }

            public int Order { get; }
            public void Modify(ref DamageContext context) => _log.Append(_name);
        }
    }
}

using NUnit.Framework;

namespace TentacleVania.Combat.Tests
{
    public class ChargeMeterTests
    {
        [Test]
        public void FillsAndClampsAtMax()
        {
            var meter = new ChargeMeter(100f);
            Assert.AreEqual(3f, meter.Add(3f), 1e-4f);
            meter.Add(200f);
            Assert.AreEqual(100f, meter.Current, 1e-4f);
            Assert.IsTrue(meter.IsFull);
            Assert.AreEqual(0f, meter.Add(5f), "a full meter gains nothing");
        }

        [Test]
        public void SpendingNeedsEnoughCharge()
        {
            var meter = new ChargeMeter(100f);
            meter.Add(40f);
            Assert.IsFalse(meter.TrySpend(50f));
            Assert.AreEqual(40f, meter.Current, 1e-4f);
            Assert.IsTrue(meter.TrySpend(35f));
            Assert.AreEqual(5f, meter.Current, 1e-4f);
        }

        [Test]
        public void GainScaleAppliesToGains()
        {
            var meter = new ChargeMeter(100f) { GainScale = 1.1f };
            meter.Add(20f);
            Assert.AreEqual(22f, meter.Current, 1e-4f);
        }

        [Test]
        public void SevenWeaponHitsAndAConsumeReachAnAbility()
        {
            var meter = new ChargeMeter(100f);
            for (var i = 0; i < 7; i++)
                meter.Add(3f);
            meter.Add(20f);
            Assert.IsTrue(meter.TrySpend(35f), "41 charge covers the cheapest ability (35)");
        }

        [Test]
        public void ResizeClampsTheCurrentCharge()
        {
            var meter = new ChargeMeter(100f);
            meter.Set(90f);
            meter.Resize(150f);
            Assert.That(meter.Max, Is.EqualTo(150f));
            Assert.That(meter.Current, Is.EqualTo(90f));
            meter.Resize(60f);
            Assert.That(meter.Current, Is.EqualTo(60f));
        }
    }
}

using NUnit.Framework;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Core.Tests
{
    public sealed class ShortcutProgressTests
    {
        [SetUp]
        public void SetUp() => SaveSystem.NewGame(0);

        [Test]
        public void OpensOnceAndStaysOpen()
        {
            var opened = 0;
            void OnOpened(string id) => opened++;
            ShortcutProgress.Opened += OnOpened;
            try
            {
                Assert.That(ShortcutProgress.Open("SH/T1"), Is.True);
                Assert.That(ShortcutProgress.Open("SH/T1"), Is.False);
                Assert.That(ShortcutProgress.IsOpen("SH/T1"), Is.True);
                Assert.That(ShortcutProgress.IsOpen("SH/T2"), Is.False);
                Assert.That(opened, Is.EqualTo(1));
            }
            finally
            {
                ShortcutProgress.Opened -= OnOpened;
            }
        }

        [Test]
        public void FarSideIsAlongTheFarDirection()
        {
            var at = new Vector3(10f, 0f, 10f);
            Assert.That(ShortcutProgress.IsFarSide(at, Vector3.forward, new Vector3(10f, 5f, 11f)), Is.True);
            Assert.That(ShortcutProgress.IsFarSide(at, Vector3.forward, new Vector3(12f, 0f, 9.5f)), Is.False);
        }
    }
}

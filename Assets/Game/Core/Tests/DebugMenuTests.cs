using NUnit.Framework;
using TentacleVania.Core.Debugging;

namespace TentacleVania.Core.Tests
{
    public class DebugMenuTests
    {
        [SetUp]
        public void SetUp() => DebugMenu.ClearForTests();

        [TearDown]
        public void TearDown() => DebugMenu.ClearForTests();

        [Test]
        public void TabsFollowThePreferredOrderWithNewAreasAppended()
        {
            DebugMenu.Button("Mutations", "a", () => { });
            DebugMenu.Button(DebugMenu.Enemies, "a", () => { });
            DebugMenu.Button(DebugMenu.Game, "a", () => { });
            DebugMenu.Button(DebugMenu.Creature, "a", () => { });
            CollectionAssert.AreEqual(new[] { DebugMenu.Game, DebugMenu.Creature, DebugMenu.Enemies, "Mutations" }, DebugMenu.Tabs);
        }

        [Test]
        public void EntriesStayInRegistrationOrder()
        {
            DebugMenu.Button(DebugMenu.Creature, "first", () => { });
            DebugMenu.Toggle(DebugMenu.Creature, "second", () => true, _ => { });
            var entries = DebugMenu.EntriesFor(DebugMenu.Creature);
            Assert.AreEqual("first", entries[0].Label);
            Assert.AreEqual(DebugEntryKind.Toggle, entries[1].Kind);
        }

        [Test]
        public void RegisteringOrRemovingBumpsTheVersion()
        {
            var before = DebugMenu.Version;
            var entry = DebugMenu.Value(DebugMenu.Game, "fps", () => "60");
            Assert.Greater(DebugMenu.Version, before);
            var afterAdd = DebugMenu.Version;
            DebugMenu.Remove(entry);
            Assert.Greater(DebugMenu.Version, afterAdd);
            Assert.AreEqual(0, DebugMenu.EntriesFor(DebugMenu.Game).Count);
        }

        [Test]
        public void OpenChangedFiresOnlyOnChange()
        {
            var calls = 0;
            DebugMenu.OpenChanged += _ => calls++;
            DebugMenu.IsOpen = true;
            DebugMenu.IsOpen = true;
            DebugMenu.IsOpen = false;
            Assert.AreEqual(2, calls);
        }
    }
}

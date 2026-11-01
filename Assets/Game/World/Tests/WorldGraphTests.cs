using NUnit.Framework;
using TentacleVania.World.Map;

namespace TentacleVania.World.Tests
{
    public class WorldGraphTests
    {
        [Test]
        public void EmptyRequirementsAreOpen() =>
            Assert.IsTrue(WorldGraph.IsOpen(new WorldConnection(), _ => false, _ => false));

        [Test]
        public void EveryRequirementMustBeMet()
        {
            var connection = new WorldConnection { Requires = new[] { "FireResistance", "Breach" } };
            Assert.IsFalse(WorldGraph.IsOpen(connection, id => id == "Breach", _ => false));
            Assert.IsTrue(WorldGraph.IsOpen(connection, _ => true, _ => false));
        }

        [Test]
        public void BossRequirementsCheckDefeatedBosses()
        {
            var connection = new WorldConnection { Requires = new[] { "boss:SH/WardenDrone" } };
            Assert.IsFalse(WorldGraph.IsOpen(connection, _ => true, _ => false));
            Assert.IsTrue(WorldGraph.IsOpen(connection, _ => false, id => id == "SH/WardenDrone"));
        }
    }
}

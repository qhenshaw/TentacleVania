using NUnit.Framework;
using TentacleVania.Core.Progression;
using UnityEngine;

namespace TentacleVania.Core.Tests
{
    public sealed class DifficultyCurveTests
    {
        [Test]
        public void UnknownSectorsAreNeutral()
        {
            var scale = DifficultyCurve.For("Nowhere");
            Assert.That(scale.Health, Is.EqualTo(1f));
            Assert.That(scale.Damage, Is.EqualTo(1f));
            Assert.That(scale.Biomass, Is.EqualTo(1f));
        }

        [Test]
        public void ShippedCurveRisesWithDepth()
        {
            var start = DifficultyCurve.For("SH");
            var deep = DifficultyCurve.For("BS");
            Assert.That(deep.Health, Is.GreaterThan(start.Health));
            Assert.That(deep.Damage, Is.GreaterThan(start.Damage));
            Assert.That(deep.Biomass, Is.GreaterThan(start.Biomass));
            Assert.That(deep.Cache, Is.GreaterThan(start.Cache));
        }

        [Test]
        public void FindMatchesBySectorId()
        {
            var curve = ScriptableObject.CreateInstance<DifficultyCurve>();
            try
            {
                Assert.That(curve.Find("QU").Health, Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(curve);
            }
        }
    }
}

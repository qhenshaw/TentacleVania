using System;
using System.Collections.Generic;
using NUnit.Framework;
using TentacleVania.Core.Characters;
using UnityEngine;

namespace TentacleVania.Core.Tests
{
    public class ChainsTests
    {
        private sealed class FakeSource : IChainSource
        {
            private readonly float _id;

            public FakeSource(int order, int count, float id, int joints = Chains.DefaultJoints)
            {
                ChainOrder = order;
                ChainCount = count;
                ChainJoints = joints;
                _id = id;
            }

            public int ChainOrder { get; }
            public int ChainCount { get; }
            public int ChainJoints { get; }

            public void WriteChain(int chain, Span<Vector4> joints)
            {
                for (var i = 0; i < joints.Length; i++)
                    joints[i] = new(_id, chain, i, 1f);
            }
        }

        [Test]
        public void PackWritesOneRowPerChainInSourceOrder()
        {
            var sources = new List<IChainSource> { new FakeSource(0, 2, 1f), new FakeSource(10, 1, 2f) };
            var pixels = new Vector4[3 * Chains.DefaultJoints];
            var rows = Chains.Pack(sources, pixels, 3, Chains.DefaultJoints);
            Assert.AreEqual(3, rows);
            Assert.AreEqual(3, Chains.CountChains(sources));
            Assert.AreEqual(new Vector4(1f, 0f, 0f, 1f), pixels[0]);
            Assert.AreEqual(new Vector4(1f, 1f, 5f, 1f), pixels[Chains.DefaultJoints + 5]);
            Assert.AreEqual(new Vector4(2f, 0f, 3f, 1f), pixels[2 * Chains.DefaultJoints + 3]);
        }

        [Test]
        public void PackStopsAtMaxChains()
        {
            var sources = new List<IChainSource> { new FakeSource(0, 4, 1f) };
            var pixels = new Vector4[2 * Chains.DefaultJoints];
            Assert.AreEqual(2, Chains.Pack(sources, pixels, 2, Chains.DefaultJoints));
        }

        [Test]
        public void PackPadsShorterChainsWithTheirTipToTheTextureWidth()
        {
            var sources = new List<IChainSource> { new FakeSource(0, 1, 1f, 4), new FakeSource(5, 1, 2f, 12) };
            var width = Chains.Width(sources);
            var pixels = new Vector4[2 * width];
            Assert.AreEqual(12, width);
            Assert.AreEqual(2, Chains.Pack(sources, pixels, 2, width));
            Assert.AreEqual(new Vector4(1f, 0f, 3f, 1f), pixels[3]);
            Assert.AreEqual(new Vector4(1f, 0f, 3f, 1f), pixels[11]);
            Assert.AreEqual(new Vector4(2f, 0f, 11f, 1f), pixels[width + 11]);
        }

        [Test]
        public void WriteTapersWidthFromRootToTip()
        {
            var positions = new Vector3[Chains.DefaultJoints];
            for (var i = 0; i < positions.Length; i++)
                positions[i] = new(i, 0f, 0f);
            var joints = new Vector4[Chains.DefaultJoints];
            Chains.Write(joints, positions, 0.1f, 0.02f);
            Assert.AreEqual(0.1f, joints[0].w, 1e-6f);
            Assert.AreEqual(0.02f, joints[Chains.DefaultJoints - 1].w, 1e-6f);
            Assert.AreEqual(3f, joints[3].x, 1e-6f);
        }

        [Test]
        public void CollapsePutsEveryJointOnTheRootWithZeroWidth()
        {
            var joints = new Vector4[Chains.DefaultJoints];
            Chains.Collapse(joints, new Vector3(1f, 2f, 3f));
            foreach (var joint in joints)
                Assert.AreEqual(new Vector4(1f, 2f, 3f, 0f), joint);
        }
    }
}

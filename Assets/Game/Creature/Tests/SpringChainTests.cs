using NUnit.Framework;
using TentacleVania.Creature.Rig;
using UnityEngine;

namespace TentacleVania.Creature.Tests
{
    public class SpringChainTests
    {
        private const int Joints = 6;
        private const float Bone = 0.2f;
        private const float Dt = 1f / 60f;

        private static Vector3[] StraightPose(Vector3 root, Vector3 direction)
        {
            var pose = new Vector3[Joints];
            for (var i = 0; i < Joints; i++)
                pose[i] = root + direction * (Bone * i);
            return pose;
        }

        private static SpringChainSettings Settings(float gravity = 0f) => new()
        {
            PoseStiffness = 20f,
            TipLooseness = 0.5f,
            Damping = 0.1f,
            Gravity = gravity,
            Iterations = 8,
        };

        [Test]
        public void SettlesOntoItsPose()
        {
            var chain = new SpringChain(Joints);
            chain.Collapse(Vector3.zero);
            var pose = StraightPose(Vector3.zero, Vector3.forward);
            for (var f = 0; f < 300; f++)
                chain.Step(Settings(), Dt, pose, Bone, false);
            for (var i = 0; i < Joints; i++)
                Assert.AreEqual(0f, Vector3.Distance(pose[i], chain.Positions[i]), 0.01f, $"joint {i}");
        }

        [Test]
        public void RootFollowsThePoseEveryStep()
        {
            var chain = new SpringChain(Joints);
            var pose = StraightPose(Vector3.zero, Vector3.forward);
            chain.Reset(pose);
            pose = StraightPose(new Vector3(3f, 1f, -2f), Vector3.forward);
            chain.Step(Settings(), Dt, pose, Bone, false);
            Assert.AreEqual(pose[0], chain.Positions[0]);
        }

        [Test]
        public void PinnedTipSitsExactlyOnTheTarget()
        {
            var chain = new SpringChain(Joints);
            var pose = StraightPose(Vector3.zero, Vector3.forward);
            chain.Reset(pose);
            pose[Joints - 1] = new Vector3(0.4f, 0.2f, 0.7f);
            chain.Step(Settings(), Dt, pose, Bone, true);
            Assert.AreEqual(pose[Joints - 1], chain.Tip);
        }

        [Test]
        public void BonesKeepTheirLengthUnderGravity()
        {
            var chain = new SpringChain(Joints);
            var pose = StraightPose(Vector3.zero, Vector3.right);
            chain.Reset(pose);
            var loose = Settings(gravity: 9.8f);
            loose.PoseStiffness = 0f;
            for (var f = 0; f < 120; f++)
                chain.Step(loose, Dt, pose, Bone, false);
            for (var i = 0; i < Joints - 1; i++)
                Assert.AreEqual(Bone, Vector3.Distance(chain.Positions[i], chain.Positions[i + 1]), 0.02f, $"bone {i}");
            Assert.Less(chain.Tip.y, -0.5f, "a chain with no pose spring hangs down");
        }

        [Test]
        public void MovingRootMakesTheTipLagBehind()
        {
            var chain = new SpringChain(Joints);
            chain.Reset(StraightPose(Vector3.zero, Vector3.up));
            var moved = StraightPose(new Vector3(0.5f, 0f, 0f), Vector3.up);
            chain.Step(Settings(), Dt, moved, Bone, false);
            Assert.Less(chain.Tip.x, 0.5f);
        }

        [Test]
        public void LargeTimeStepsStayStable()
        {
            var chain = new SpringChain(Joints);
            var pose = StraightPose(Vector3.zero, Vector3.forward);
            chain.Reset(pose);
            for (var f = 0; f < 50; f++)
                chain.Step(Settings(gravity: 9.8f), 0.5f, pose, Bone, false);
            foreach (var p in chain.Positions)
                Assert.Less(p.magnitude, 5f);
        }
    }
}

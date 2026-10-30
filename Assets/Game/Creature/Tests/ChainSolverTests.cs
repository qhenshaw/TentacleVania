using NUnit.Framework;
using TentacleVania.Creature.Rig;
using UnityEngine;

namespace TentacleVania.Creature.Tests
{
    public class ChainSolverTests
    {
        private const float Bone = 0.18f;
        private const int Joints = 6;

        private static Vector3[] Solve(Vector3 root, Vector3 target, float arch = 0.3f)
        {
            var joints = new Vector3[Joints];
            ChainSolver.SeedArch(joints, root, target, Vector3.up, arch);
            ChainSolver.Solve(joints, Bone, root, target, Vector3.up, 10);
            return joints;
        }

        private static void AssertBoneLengths(Vector3[] joints)
        {
            for (var i = 0; i < joints.Length - 1; i++)
                Assert.AreEqual(Bone, Vector3.Distance(joints[i], joints[i + 1]), 1e-3f, $"bone {i}");
        }

        [Test]
        public void ReachableTargetIsReachedWithBoneLengthsKept()
        {
            var root = new Vector3(0f, 0.42f, 0f);
            var target = new Vector3(0.6f, 0f, 0f);
            var joints = Solve(root, target);
            Assert.AreEqual(root, joints[0]);
            Assert.Less(Vector3.Distance(joints[Joints - 1], target), 0.01f);
            AssertBoneLengths(joints);
        }

        [Test]
        public void SlackChainArchesUpward()
        {
            var joints = Solve(new Vector3(0f, 0.42f, 0f), new Vector3(0.5f, 0f, 0f));
            var knee = float.MinValue;
            for (var i = 1; i < Joints - 1; i++)
                knee = Mathf.Max(knee, joints[i].y);
            Assert.Greater(knee, 0.42f, "the slack leg's knee should rise above its root");
        }

        [Test]
        public void UnreachableTargetStretchesStraightTowardIt()
        {
            var root = Vector3.zero;
            var target = new Vector3(5f, 0f, 0f);
            var joints = Solve(root, target);
            for (var i = 0; i < Joints; i++)
                Assert.Less(Vector3.Distance(new Vector3(Bone * i, 0f, 0f), joints[i]), 1e-4f);
        }

        [Test]
        public void TargetOnTheRootDoesNotProduceNaN()
        {
            var joints = Solve(Vector3.zero, Vector3.zero, 0f);
            foreach (var joint in joints)
                Assert.IsFalse(float.IsNaN(joint.x) || float.IsNaN(joint.y) || float.IsNaN(joint.z));
        }
    }
}

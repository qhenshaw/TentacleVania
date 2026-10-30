using System;
using UnityEngine;

namespace TentacleVania.Creature.Rig
{
    public static class ChainSolver
    {
        private const float KneeAlong = 0.3f;
        private const float KneeJointAlong = 0.4f;

        public static void SeedArch(Span<Vector3> joints, Vector3 root, Vector3 target, Vector3 up, float archHeight)
        {
            var knee = Vector3.Lerp(root, target, KneeAlong);
            knee += up * (Mathf.Max(0f, Vector3.Dot(root - knee, up)) + archHeight);
            var last = joints.Length - 1;
            var kneeJoint = Mathf.Clamp(Mathf.RoundToInt(last * KneeJointAlong), 1, last - 1);
            for (var i = 0; i <= last; i++)
            {
                joints[i] = i <= kneeJoint
                    ? Vector3.Lerp(root, knee, (float)i / kneeJoint)
                    : Vector3.Lerp(knee, target, (float)(i - kneeJoint) / (last - kneeJoint));
            }
        }

        public static float Solve(Span<Vector3> joints, float boneLength, Vector3 root, Vector3 target, Vector3 up, int iterations, float tolerance = 0.001f)
        {
            var last = joints.Length - 1;
            var reach = boneLength * last;
            var toTarget = target - root;

            if (toTarget.sqrMagnitude >= reach * reach)
            {
                var direction = Direction(toTarget, up);
                for (var i = 0; i <= last; i++)
                    joints[i] = root + direction * (boneLength * i);
                return toTarget.magnitude - reach;
            }

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                joints[last] = target;
                for (var i = last - 1; i >= 0; i--)
                    joints[i] = joints[i + 1] + Direction(joints[i] - joints[i + 1], up) * boneLength;

                joints[0] = root;
                for (var i = 0; i < last; i++)
                    joints[i + 1] = joints[i] + Direction(joints[i + 1] - joints[i], up) * boneLength;

                if ((joints[last] - target).sqrMagnitude <= tolerance * tolerance)
                    break;
            }
            return (joints[last] - target).magnitude;
        }

        private static Vector3 Direction(Vector3 v, Vector3 fallback)
        {
            var sqr = v.sqrMagnitude;
            return sqr > 1e-12f ? v / Mathf.Sqrt(sqr) : fallback;
        }
    }
}

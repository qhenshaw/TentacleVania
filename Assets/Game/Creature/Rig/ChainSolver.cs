using System;
using UnityEngine;

namespace TentacleVania.Creature.Rig
{
    public static class ChainSolver
    {
        private const float KneeAlong = 0.35f;

        public static void SeedArch(Span<Vector3> joints, Vector3 root, Vector3 target, Vector3 up, float archHeight)
        {
            var control = Vector3.Lerp(root, target, KneeAlong);
            control += up * (Mathf.Max(0f, Vector3.Dot(root - control, up)) + archHeight);
            var last = joints.Length - 1;
            for (var i = 0; i <= last; i++)
            {
                var t = (float)i / last;
                var u = 1f - t;
                joints[i] = u * u * root + 2f * u * t * control + t * t * target;
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

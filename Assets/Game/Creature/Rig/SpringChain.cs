using System;
using UnityEngine;

namespace TentacleVania.Creature.Rig
{
    [Serializable]
    public struct SpringChainSettings
    {
        [Tooltip("How fast joints pull back to their pose (1/s) at the root; it fades toward the tip by TipLooseness.")]
        [Min(0f)] public float PoseStiffness;
        [Range(0f, 1f)] public float TipLooseness;
        [Tooltip("Fraction of velocity lost per 1/60 s.")]
        [Range(0f, 1f)] public float Damping;
        [Min(0f)] public float Gravity;
        [Range(1, 12)] public int Iterations;

        public static SpringChainSettings Default => new()
        {
            PoseStiffness = 18f,
            TipLooseness = 0.7f,
            Damping = 0.12f,
            Gravity = 4f,
            Iterations = 4,
        };
    }

    public sealed class SpringChain
    {
        private const float MaxStep = 1f / 30f;
        private const float ReferenceStep = 1f / 60f;
        private const float MaxFoldCos = -0.17f;

        private readonly Vector3[] _positions;
        private readonly Vector3[] _previous;

        public SpringChain(int joints)
        {
            _positions = new Vector3[joints];
            _previous = new Vector3[joints];
        }

        public int Count => _positions.Length;
        public Vector3[] Positions => _positions;
        public Vector3 Tip => _positions[^1];

        public void Reset(ReadOnlySpan<Vector3> pose)
        {
            for (var i = 0; i < _positions.Length; i++)
                _previous[i] = _positions[i] = pose[i];
        }

        public void Collapse(Vector3 point)
        {
            for (var i = 0; i < _positions.Length; i++)
                _previous[i] = _positions[i] = point;
        }

        public void Step(in SpringChainSettings settings, float dt, ReadOnlySpan<Vector3> pose, float boneLength, bool pinTip)
        {
            dt = Mathf.Min(dt, MaxStep);
            if (dt <= 0f)
                return;

            var last = _positions.Length - 1;
            var keep = Mathf.Pow(1f - settings.Damping, dt / ReferenceStep);
            var fall = Vector3.down * (settings.Gravity * dt * dt);
            for (var i = 1; i <= last; i++)
            {
                var p = _positions[i];
                var velocity = (p - _previous[i]) * keep;
                _previous[i] = p;
                p += velocity + fall;
                var along = (float)i / last;
                var stiffness = settings.PoseStiffness * (1f - settings.TipLooseness * along);
                _positions[i] = Vector3.Lerp(p, pose[i], 1f - Mathf.Exp(-stiffness * dt));
            }

            _positions[0] = pose[0];
            _previous[0] = pose[0];
            if (pinTip)
                _positions[last] = pose[last];

            if (!pinTip)
            {
                FollowFromRoot(pose, boneLength);
                return;
            }

            var target = pose[last];
            for (var iteration = 0; iteration < settings.Iterations; iteration++)
            {
                _positions[last] = target;
                for (var i = last - 1; i > 0; i--)
                    _positions[i] = _positions[i + 1] + Direction(_positions[i] - _positions[i + 1], pose[i] - pose[i + 1]) * boneLength;
                FollowFromRoot(pose, boneLength);
            }
            _positions[last] = target;
        }

        private void FollowFromRoot(ReadOnlySpan<Vector3> pose, float boneLength)
        {
            for (var i = 1; i < _positions.Length; i++)
                _positions[i] = _positions[i - 1] + Direction(_positions[i] - _positions[i - 1], pose[i] - pose[i - 1]) * boneLength;
        }

        private static Vector3 Direction(Vector3 v, Vector3 pose)
        {
            var sqr = v.sqrMagnitude;
            if (sqr > 1e-10f && Vector3.Dot(v, pose) >= MaxFoldCos * Mathf.Sqrt(sqr * pose.sqrMagnitude))
                return v / Mathf.Sqrt(sqr);
            sqr = pose.sqrMagnitude;
            if (sqr > 1e-10f)
                return pose / Mathf.Sqrt(sqr);
            sqr = v.sqrMagnitude;
            return sqr > 1e-10f ? v / Mathf.Sqrt(sqr) : Vector3.forward;
        }

        public static float BoneLengthFor(int joints, float length) => length / Mathf.Max(1, joints - 1);
    }
}

using UnityEngine;

namespace TentacleVania.Creature.Rig
{
    public enum MawPhase
    {
        Hidden,
        Open,
        Close,
        Swallow,
        Retract,
    }

    public readonly struct MawTimeline
    {
        public readonly float Open;
        public readonly float Close;
        public readonly float Swallow;
        public readonly float Retract;

        public MawTimeline(float open, float close, float swallow, float retract)
        {
            Open = Mathf.Max(0.01f, open);
            Close = Mathf.Max(0.01f, close);
            Swallow = Mathf.Max(0.01f, swallow);
            Retract = Mathf.Max(0.01f, retract);
        }

        public float Engulf => Open + Close + Swallow;
        public float Total => Engulf + Retract;

        public MawPhase Evaluate(float elapsed, out float t)
        {
            if (elapsed < 0f)
            {
                t = 0f;
                return MawPhase.Hidden;
            }
            if (elapsed < Open)
            {
                t = elapsed / Open;
                return MawPhase.Open;
            }
            elapsed -= Open;
            if (elapsed < Close)
            {
                t = elapsed / Close;
                return MawPhase.Close;
            }
            elapsed -= Close;
            if (elapsed < Swallow)
            {
                t = elapsed / Swallow;
                return MawPhase.Swallow;
            }
            elapsed -= Swallow;
            if (elapsed < Retract)
            {
                t = elapsed / Retract;
                return MawPhase.Retract;
            }
            t = 1f;
            return MawPhase.Hidden;
        }
    }

    public static class MawPose
    {
        public static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        public static float EaseOutBack(float t, float overshoot)
        {
            t = Mathf.Clamp01(t) - 1f;
            return 1f + (overshoot + 1f) * t * t * t + overshoot * t * t;
        }

        public static float Stagger(float t, float delay)
        {
            if (delay >= 1f)
                return t >= 1f ? 1f : 0f;
            return Mathf.Clamp01((t - delay) / (1f - delay));
        }

        public static float Angle(int index, int count) => 360f / Mathf.Max(1, count) * index + 90f;

        public static Vector3 Radial(float angle, Quaternion frame)
        {
            var radians = angle * Mathf.Deg2Rad;
            return frame * new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
        }

        public static Quaternion Frame(Vector3 axis, Vector3 up)
        {
            if (axis.sqrMagnitude < 1e-6f)
                axis = Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(axis.normalized, up.normalized)) > 0.99f)
                up = Vector3.forward;
            return Quaternion.LookRotation(axis, up);
        }

        public static Vector3 Root(float angle, Vector3 mouth, Quaternion facing, float rootRadius) =>
            mouth + Radial(angle, facing) * rootRadius;

        public static Vector3 OpenTip(float angle, Vector3 mouth, Vector3 target, float openRadius, float reach)
        {
            var toTarget = target - mouth;
            return mouth + toTarget * reach + Radial(angle, Frame(toTarget, Vector3.up)) * openRadius;
        }

        public static Vector3 CloseTip(float angle, Vector3 mouth, Vector3 target, float targetRadius, float pinch)
        {
            var frame = Frame(target - mouth, Vector3.up);
            return target + frame * Vector3.forward * targetRadius + Radial(angle, frame) * pinch;
        }

        public static Vector3 PinTip(float angle, Vector3 mouth, Vector3 target, float targetRadius, float hug)
        {
            var frame = Frame(target - mouth, Vector3.up);
            return target + Radial(angle, frame) * (targetRadius * hug);
        }

        public static float ArcLength(float span, float archHeight) =>
            2f * Mathf.Sqrt(span * span * 0.25f + archHeight * archHeight);
    }
}

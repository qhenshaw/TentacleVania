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

        public static Vector3 Radial(int index, int count, Quaternion frame)
        {
            var angle = (360f / Mathf.Max(1, count) * index + 90f) * Mathf.Deg2Rad;
            return frame * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        }

        public static Quaternion Frame(Vector3 axis, Vector3 up)
        {
            if (axis.sqrMagnitude < 1e-6f)
                axis = Vector3.forward;
            if (Mathf.Abs(Vector3.Dot(axis.normalized, up.normalized)) > 0.99f)
                up = Vector3.forward;
            return Quaternion.LookRotation(axis, up);
        }

        public static Vector3 Root(int index, int count, Vector3 mouth, Quaternion facing, float rootRadius) =>
            mouth + Radial(index, count, facing) * rootRadius;

        public static Vector3 OpenTip(int index, int count, Vector3 mouth, Vector3 target, float openRadius, float reach)
        {
            var toTarget = target - mouth;
            var frame = Frame(toTarget, Vector3.up);
            return mouth + toTarget * reach + Radial(index, count, frame) * openRadius;
        }

        public static Vector3 CloseTip(int index, int count, Vector3 mouth, Vector3 target, float targetRadius, float pinch)
        {
            var toTarget = target - mouth;
            var frame = Frame(toTarget, Vector3.up);
            var axis = frame * Vector3.forward;
            return target + axis * targetRadius + Radial(index, count, frame) * pinch;
        }

        public static float ArcLength(float span, float archHeight) =>
            2f * Mathf.Sqrt(span * span * 0.25f + archHeight * archHeight);
    }
}

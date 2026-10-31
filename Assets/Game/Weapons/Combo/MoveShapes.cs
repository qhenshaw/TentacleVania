using TentacleVania.Weapons.Data;
using UnityEngine;

namespace TentacleVania.Weapons.Combo
{
    public enum MovePhase
    {
        WindUp,
        Strike,
        Recovery,
        Done,
    }

    public static class MoveShapes
    {
        private const float RaisedHeight = 1.3f;
        private const float RestReach = 0.35f;
        private const float MaxSweepStepDegrees = 10f;

        public static MovePhase Phase(WeaponMove move, float elapsed)
        {
            if (elapsed < move.WindUp)
                return MovePhase.WindUp;
            if (elapsed < move.WindUp + move.Strike)
                return MovePhase.Strike;
            return elapsed < move.Duration ? MovePhase.Recovery : MovePhase.Done;
        }

        public static float StrikeProgress(WeaponMove move, float elapsed) => Mathf.Clamp01((elapsed - move.WindUp) / move.Strike);

        public static Vector3 SweepDirection(Vector3 forward, WeaponMove move, float t) =>
            Quaternion.AngleAxis(Mathf.Lerp(move.StartAngle, move.EndAngle, t), Vector3.up) * forward;

        public static int SweepSteps(WeaponMove move, float fromT, float toT) =>
            Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(toT - fromT) * Mathf.Abs(move.EndAngle - move.StartAngle) / MaxSweepStepDegrees));

        public static Vector3 AreaCenter(Vector3 position, Vector3 forward, WeaponMove move) => position + forward * move.AreaOffset;

        public static bool InArea(Vector3 center, Vector3 forward, WeaponMove move, Vector3 point)
        {
            var offset = point - center;
            offset.y = 0f;
            if (offset.sqrMagnitude > move.Radius * move.Radius)
                return false;
            return move.SectorAngle >= 360f || offset.sqrMagnitude < 1e-4f || Vector3.Angle(forward, offset) <= move.SectorAngle * 0.5f;
        }

        public static Vector3 StrikeTip(Vector3 origin, Vector3 forward, WeaponMove move, float t)
        {
            switch (move.Shape)
            {
                case MoveShape.Area:
                    var center = AreaCenter(origin, forward, move);
                    center.y = origin.y - move.Height;
                    var raised = Vector3.Lerp(origin, center, 0.5f) + Vector3.up * RaisedHeight;
                    return Vector3.Lerp(raised, center, Mathf.Clamp01(t * 3f));
                case MoveShape.Body:
                    return origin + Quaternion.AngleAxis(t * 720f, Vector3.up) * forward * (move.Radius + 0.4f);
                default:
                    return origin + SweepDirection(forward, move, t) * move.Reach;
            }
        }

        public static Vector3 ChargeTip(Vector3 origin, Vector3 forward, WeaponMove move, float t)
        {
            var rest = origin + forward * RestReach;
            Vector3 pose;
            switch (move.Shape)
            {
                case MoveShape.Area:
                    pose = origin - forward * 0.5f + Vector3.up * (RaisedHeight + 0.6f);
                    break;
                case MoveShape.Body:
                    pose = origin + Quaternion.AngleAxis(-150f, Vector3.up) * forward * Mathf.Max(0.6f, move.Radius * 0.6f) + Vector3.up * 0.4f;
                    break;
                default:
                    var back = move.EndAngle >= move.StartAngle ? -45f : 45f;
                    var direction = Quaternion.AngleAxis(back, Vector3.up) * SweepDirection(forward, move, 0f);
                    pose = origin + direction * (move.Reach * 0.75f) + Vector3.up * 0.9f;
                    break;
            }
            var eased = t * t * (3f - 2f * t);
            return Vector3.Lerp(rest, pose, eased);
        }

        public static Vector3 Tip(Vector3 origin, Vector3 forward, WeaponMove move, float elapsed)
        {
            var rest = origin + forward * RestReach;
            switch (Phase(move, elapsed))
            {
                case MovePhase.WindUp:
                    var windT = move.WindUp > 0f ? elapsed / move.WindUp : 1f;
                    return Vector3.Lerp(rest, StrikeTip(origin, forward, move, 0f), windT);
                case MovePhase.Strike:
                    return StrikeTip(origin, forward, move, StrikeProgress(move, elapsed));
                case MovePhase.Recovery:
                    var recoverT = move.Recovery > 0f ? (elapsed - move.WindUp - move.Strike) / move.Recovery : 1f;
                    return Vector3.Lerp(StrikeTip(origin, forward, move, 1f), rest, recoverT);
                default:
                    return rest;
            }
        }
    }
}

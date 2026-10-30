using TentacleVania.Enemies.Data;
using UnityEngine;

namespace TentacleVania.Enemies.AI
{
    public enum AttackPhase
    {
        WindUp,
        Active,
        Recovery,
        Done,
    }

    public static class AttackMath
    {
        public static AttackPhase Phase(EnemyAttack attack, float elapsed)
        {
            if (elapsed < attack.WindUp)
                return AttackPhase.WindUp;
            if (elapsed < attack.WindUp + attack.Active)
                return AttackPhase.Active;
            return elapsed < attack.Duration ? AttackPhase.Recovery : AttackPhase.Done;
        }

        public static float WindUpProgress(EnemyAttack attack, float elapsed) =>
            attack.WindUp > 0f ? Mathf.Clamp01(elapsed / attack.WindUp) : 1f;

        public static float ActiveProgress(EnemyAttack attack, float elapsed) =>
            Mathf.Clamp01((elapsed - attack.WindUp) / attack.Active);

        public static Vector3 ArcDirection(Vector3 forward, float arc, float t) =>
            Quaternion.AngleAxis(Mathf.Lerp(-arc * 0.5f, arc * 0.5f, t), Vector3.up) * forward;

        public static int SweepSteps(float arc, float fromT, float toT, float maxStepDegrees = 10f) =>
            Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(toT - fromT) * arc / maxStepDegrees));

        public static bool InsideArea(Vector3 center, float radius, Vector3 point)
        {
            var offset = point - center;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }
    }
}

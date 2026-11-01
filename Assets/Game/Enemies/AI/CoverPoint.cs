using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Enemies.AI
{
    public sealed class CoverPoint : MonoBehaviour
    {
        private static readonly List<CoverPoint> Points = new();

        public static IReadOnlyList<CoverPoint> All => Points;

        public Enemy Claimant { get; set; }
        public Vector3 Position => transform.position;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Points.Clear();

        public static bool Covers(Vector3 cover, Vector3 forward, Vector3 target, Vector2 range, float minFacing)
        {
            var toTarget = target - cover;
            toTarget.y = 0f;
            forward.y = 0f;
            var distance = toTarget.magnitude;
            if (distance < range.x || distance > range.y || forward.sqrMagnitude < 1e-6f)
                return false;
            return Vector3.Dot(forward.normalized, toTarget / distance) >= minFacing;
        }

        public bool Covers(Vector3 target, Vector2 range, float minFacing) => Covers(Position, transform.forward, target, range, minFacing);

        private void OnEnable() => Points.Add(this);

        private void OnDisable()
        {
            Points.Remove(this);
            Claimant = null;
        }
    }
}

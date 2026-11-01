using System.Collections.Generic;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class OfficerEnemy : GuardEnemy
    {
        private static readonly List<OfficerEnemy> Officers = new();
        private static readonly ScaleModifier Rally = new(500) { HealthScale = 1.3f, StaggerScale = 1.3f };
        private static int _refreshedFrame = -1;

        [Header("Rally (design: buffs nearby soldiers; kill it first)")]
        [SerializeField, Min(1f)] private float _rallyRange = 10f;

        public static ScaleModifier RallyModifier => Rally;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Officers.Clear();
            _refreshedFrame = -1;
        }

        public static bool Rallies(Vector3 officer, Vector3 ally, float range)
        {
            var offset = ally - officer;
            offset.y = 0f;
            return offset.sqrMagnitude <= range * range;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Officers.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Officers.Remove(this);
            Refresh(true);
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            Refresh(true);
        }

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            Refresh(false);
        }

        private static void Refresh(bool force)
        {
            if (!force && _refreshedFrame == Time.frameCount)
                return;
            _refreshedFrame = Time.frameCount;
            var manager = EnemyManager.Current;
            if (manager == null)
                return;
            var enemies = manager.Enemies;
            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || enemy.Health == null)
                    continue;
                if (Covered(enemy))
                    enemy.Health.Outgoing.Add(Rally);
                else
                    enemy.Health.Outgoing.Remove(Rally);
            }
        }

        private static bool Covered(Enemy enemy)
        {
            if (enemy is OfficerEnemy || enemy.IsBoss || !enemy.Health.IsAlive)
                return false;
            for (var i = 0; i < Officers.Count; i++)
            {
                var officer = Officers[i];
                if (officer != null && officer.Health.IsAlive && officer.State != EnemyState.Consumed && Rallies(officer.Position, enemy.transform.position, officer._rallyRange))
                    return true;
            }
            return false;
        }
    }
}

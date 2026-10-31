using TentacleVania.Combat;
using TentacleVania.Enemies;
using UnityEngine;
using UnityEngine.AI;

namespace TentacleVania.World.Map
{
    [AddComponentMenu("")]
    public sealed class FallRecovery : MonoBehaviour
    {
        public const float KillHeight = -8f;
        private const float SampleInterval = 0.25f;
        private const float SafeSnap = 0.6f;
        private const float EnemyRescueRadius = 6f;

        private static FallRecovery _instance;

        private Vector3 _safe;
        private float _safeYaw;
        private bool _hasSafe;
        private float _nextSample;
        private Transform _tracked;

        public static bool Enabled { get; set; } = true;
        public static int CreatureRecoveries { get; private set; }
        public static int EnemyRecoveries { get; private set; }
        public static Vector3 SafePoint => _instance != null ? _instance._safe : Vector3.zero;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            Enabled = true;
            CreatureRecoveries = 0;
            EnemyRecoveries = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (_instance != null || !Application.isPlaying)
                return;
            var host = new GameObject("FallRecovery");
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<FallRecovery>();
        }

        private void Update()
        {
            if (!Enabled || FastTravel.Phase != TravelPhase.Idle)
                return;
            var creature = CombatTargets.Creature;
            if (creature != null && creature.IsAlive)
                TickCreature(creature.transform);
            TickEnemies();
        }

        private void TickCreature(Transform creature)
        {
            if (creature != _tracked)
            {
                _tracked = creature;
                _hasSafe = false;
            }
            var position = creature.position;
            if (position.y < KillHeight)
            {
                if (_hasSafe && FastTravel.Recover(_safe, _safeYaw))
                    CreatureRecoveries++;
                return;
            }
            if (Time.time < _nextSample)
                return;
            _nextSample = Time.time + SampleInterval;
            if (!creature.TryGetComponent(out CharacterController controller) || !controller.enabled || !controller.isGrounded)
                return;
            if (!NavMesh.SamplePosition(position, out var hit, SafeSnap, NavMesh.AllAreas))
                return;
            _safe = hit.position;
            _safeYaw = creature.eulerAngles.y;
            _hasSafe = true;
        }

        private static void TickEnemies()
        {
            var manager = EnemyManager.Current;
            if (manager == null)
                return;
            var enemies = manager.Enemies;
            for (var i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                if (enemy == null || enemy.transform.position.y >= KillHeight || !enemy.Health.IsAlive)
                    continue;
                EnemyRecoveries++;
                var probe = enemy.transform.position;
                probe.y = 0f;
                if (enemy.TryGetComponent(out NavMeshAgent agent) && NavMesh.SamplePosition(probe, out var hit, EnemyRescueRadius, NavMesh.AllAreas))
                {
                    if (agent.enabled)
                        agent.Warp(hit.position);
                    else
                        enemy.transform.position = hit.position;
                    continue;
                }
                enemy.Health.Kill();
            }
        }
    }
}

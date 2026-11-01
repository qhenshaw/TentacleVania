using FMODUnity;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Feedback;
using UnityEngine;

namespace TentacleVania.Combat.Status
{
    public sealed class ParasiteSwarm : MonoBehaviour
    {
        private const int BurstPoolSize = 6;
        private static readonly Vector3 Parked = new(0f, -1000f, 0f);

        private static ParasiteSwarm _instance;

        private StatusEffectSettings _settings;
        private VfxPool _bursts;
        private Transform[] _bodies;
        private MinionSlots _slots;
        private Vector3[] _positions;
        private StatusEffects[] _targets;

        public static int Active => _instance != null ? _instance._slots.ActiveCount : 0;

        public static void Prewarm(StatusEffectSettings settings)
        {
            if (_instance != null || !Application.isPlaying)
                return;
            var root = new GameObject("ParasiteSwarm");
            _instance = root.AddComponent<ParasiteSwarm>();
            _instance.Build(settings);
        }

        public static void PlayBurst(Vector3 point)
        {
            if (_instance != null)
                _instance._bursts.Play(point, Vector3.up, 1f);
        }

        public static int Spawn(Vector3 position, int count, Team team)
        {
            if (_instance == null)
                Prewarm(StatusEffectSettings.Instance);
            return _instance != null ? _instance.Place(position, count, team) : 0;
        }

        public static void ClearAll()
        {
            if (_instance == null)
                return;
            for (var i = 0; i < _instance._slots.Count; i++)
                _instance.Hide(i);
            _instance._slots.Clear();
        }

        private void Build(StatusEffectSettings settings)
        {
            _settings = settings;
            _bursts = new VfxPool(settings.ParasiteBurstEffect, BurstPoolSize, transform);
            _slots = new(settings.MinionCount);
            _bodies = new Transform[_slots.Count];
            _positions = new Vector3[_slots.Count];
            _targets = new StatusEffects[_slots.Count];
            for (var i = 0; i < _slots.Count; i++)
            {
                var body = settings.MinionPrefab != null ? Instantiate(settings.MinionPrefab, transform) : new GameObject("Minion");
                _bodies[i] = body.transform;
                Hide(i);
            }
            enabled = false;
        }

        private int Place(Vector3 position, int count, Team team)
        {
            var now = Time.time;
            for (var n = 0; n < count; n++)
            {
                var index = _slots.Spawn(team, now, _settings.MinionLifetime);
                var angle = (n + 0.5f) / count * Mathf.PI * 2f;
                _positions[index] = position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.6f;
                _targets[index] = null;
                _bodies[index].gameObject.SetActive(true);
                _bodies[index].position = _positions[index];
            }
            enabled = true;
            return count;
        }

        private void Hide(int index)
        {
            _targets[index] = null;
            _bodies[index].position = Parked;
            _bodies[index].gameObject.SetActive(false);
        }

        private void Update()
        {
            var now = Time.time;
            var dt = Time.deltaTime;
            var any = false;
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots.Expire(i, now))
                {
                    Hide(i);
                    continue;
                }
                if (!_slots[i].Active)
                    continue;
                any = true;
                Step(i, now, dt);
            }
            if (!any)
                enabled = false;
        }

        private void Step(int index, float now, float dt)
        {
            ref var slot = ref _slots[index];
            var target = _targets[index];
            if (target == null || target.Health == null || !target.Health.IsAlive)
            {
                target = StatusEffects.NearestHostile(_positions[index], slot.Team, _settings.MinionSeekRadius);
                _targets[index] = target;
            }
            if (target == null)
                return;
            var goal = target.transform.position;
            var offset = goal - _positions[index];
            offset.y = 0f;
            var distance = offset.magnitude;
            if (distance > _settings.MinionBiteRange)
            {
                var step = Mathf.Min(_settings.MinionSpeed * dt, distance - _settings.MinionBiteRange * 0.8f);
                _positions[index] += offset / distance * step;
                _positions[index].y = goal.y;
                _bodies[index].SetPositionAndRotation(_positions[index], Quaternion.LookRotation(offset));
                return;
            }
            if (!_slots.BiteDue(index, now, _settings.MinionBiteInterval))
                return;
            var info = DamageInfo.Create(_settings.MinionBiteDamage, slot.Team);
            info.Status = StatusKind.Parasite;
            info.Point = goal + Vector3.up;
            info.Direction = offset;
            target.Health.TakeDamage(info);
            if (!_settings.MinionBiteSound.IsNull)
                RuntimeManager.PlayOneShot(_settings.MinionBiteSound, goal);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}

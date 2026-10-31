using System;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Characters;
using UnityEngine;

namespace TentacleVania.Combat.Status
{
    [RequireComponent(typeof(Health))]
    public sealed class StatusEffects : MonoBehaviour
    {
        [Serializable]
        public sealed class Visual
        {
            [ColorUsage(false, true)] public Color Color = Color.white;
            [Min(0f)] public float Amount = 0.8f;
            [Min(0f)] public float Pulse = 1.5f;
        }

        [SerializeField] private Visual _slow = new() { Color = new Color(0.55f, 0.35f, 1.6f) };
        [SerializeField] private Visual _root = new() { Color = new Color(1.4f, 1.1f, 0.2f), Pulse = 3f };

        private readonly StatusTimers _timers = new();
        private readonly bool[] _shown = new bool[3];

        private Health _health;
        private CharacterMaterials _materials;

        public event Action<StatusEffects, StatusKind> Started;
        public event Action<StatusEffects, StatusKind> Ended;

        public float MoveScale => _timers.MoveScale(Time.time);
        public bool IsRooted => _timers.IsActive(StatusKind.Root, Time.time);
        public bool IsSlowed => _timers.IsActive(StatusKind.Slow, Time.time);

        public bool IsActive(StatusKind kind) => _timers.IsActive(kind, Time.time);
        public float Remaining(StatusKind kind) => _timers.Remaining(kind, Time.time);

        private void Awake()
        {
            _health = GetComponent<Health>();
            _materials = GetComponent<CharacterMaterials>();
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        public bool ControlImmune { get; set; }

        public static bool Blocked(StatusKind kind, bool controlImmune) => controlImmune && kind is StatusKind.Root or StatusKind.Slow;

        public void Apply(StatusKind kind, float magnitude, float duration)
        {
            if (!_health.IsAlive || Blocked(kind, ControlImmune))
                return;
            _timers.Apply(kind, magnitude, duration, Time.time);
            Refresh();
        }

        public void ClearAll()
        {
            _timers.Clear(StatusKind.Slow);
            _timers.Clear(StatusKind.Root);
            Refresh();
        }

        private void Update() => Refresh();

        private void OnDamaged(Health health, in DamageInfo info, in DamageResult result)
        {
            if (info.Status != StatusKind.None)
                Apply(info.Status, info.StatusMagnitude, info.StatusDuration);
        }

        private void OnDied(Health health) => ClearAll();

        private void Refresh()
        {
            var changed = false;
            for (var i = 1; i < _shown.Length; i++)
            {
                var kind = (StatusKind)i;
                var active = _timers.IsActive(kind, Time.time);
                if (active == _shown[i])
                    continue;
                _shown[i] = active;
                changed = true;
                if (active)
                    Started?.Invoke(this, kind);
                else
                    Ended?.Invoke(this, kind);
            }
            if (changed)
                ApplyGlow();
        }

        private void ApplyGlow()
        {
            if (_materials == null)
                return;
            var visual = _shown[(int)StatusKind.Root] ? _root : _shown[(int)StatusKind.Slow] ? _slow : null;
            if (visual != null)
                _materials.SetStatus(visual.Color, visual.Amount, visual.Pulse);
            else
                _materials.ClearStatus();
        }
    }
}

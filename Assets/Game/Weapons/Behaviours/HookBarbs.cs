using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Saves;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using TentacleVania.Weapons.Data;
using UnityEngine;
using UnityEngine.AI;

namespace TentacleVania.Weapons.Behaviours
{
    public sealed class HookBarbs : MonoBehaviour
    {
        private const int MaxBarbed = 8;
        private const int MaxPulls = 4;

        private struct Barbed
        {
            public Health Target;
            public float Until;
        }

        private struct Pull
        {
            public NavMeshAgent Agent;
            public Health Target;
            public float Until;
            public bool Grab;
        }

        [SerializeField] private WeaponController _weapons;
        [SerializeField] private CreatureMotor _motor;
        [SerializeField] private CreatureConsume _consume;
        [SerializeField] private CreatureCharge _charge;
        [Tooltip("Weapon id (ContentCatalog asset name) these rules apply to.")]
        [SerializeField] private string _weaponId = "HookBarbs";
        [SerializeField, Min(0.5f)] private float _barbLife = 6f;
        [SerializeField, Min(0.05f)] private float _pullTime = 0.25f;
        [Tooltip("Pulls stop this far in front of the creature.")]
        [SerializeField, Min(0.5f)] private float _pullStop = 1.3f;
        [Tooltip("Targets with more max health than this (bosses) are never dragged.")]
        [SerializeField, Min(1f)] private float _maxPullHealth = 800f;
        [SerializeField, Min(0.05f)] private float _zipTime = 0.2f;
        [SerializeField] private EventReference _pullSound;

        private readonly Barbed[] _barbed = new Barbed[MaxBarbed];
        private readonly Pull[] _pulls = new Pull[MaxPulls];
        private Health _health;

        public int BarbsOn(Health target)
        {
            var now = Time.time;
            for (var i = 0; i < MaxBarbed; i++)
                if (_barbed[i].Target == target && _barbed[i].Until > now)
                    return 1;
            return 0;
        }

        private void Awake() => _health = GetComponent<Health>();

        private void OnEnable() => CombatEvents.HitLanded += OnHit;

        private void OnDisable() => CombatEvents.HitLanded -= OnHit;

        private bool Active =>
            _weapons != null && _weapons.ActiveSlot >= 0 && ContentCatalog.IdOf(_weapons.Weapon(_weapons.ActiveSlot)) == _weaponId;

        private void OnHit(Health target, in DamageInfo info, in DamageResult result)
        {
            if (info.Attacker != _health || target == _health || !Active || !target.IsAlive)
                return;
            var modifier = _weapons.Modifier(_weapons.ActiveSlot);
            if ((info.Flags & HitFlags.DashAttack) != 0)
            {
                Zip(target);
                return;
            }
            if (_weapons.ActiveKind == AttackKind.Heavy)
            {
                Drag(target, modifier);
                return;
            }
            if ((info.Flags & HitFlags.Finisher) != 0)
            {
                if (BarbsOn(target) == 0)
                    return;
                Drag(target, modifier);
                if (modifier != null && modifier.Kind == ModifierKind.ChainHook)
                    DragSecond(target, modifier);
                return;
            }
            Plant(target);
        }

        private void Plant(Health target)
        {
            var now = Time.time;
            var free = -1;
            for (var i = 0; i < MaxBarbed; i++)
            {
                if (_barbed[i].Target == target)
                {
                    _barbed[i].Until = now + _barbLife;
                    return;
                }
                if (free < 0 && (_barbed[i].Target == null || _barbed[i].Until <= now))
                    free = i;
            }
            if (free >= 0)
                _barbed[free] = new Barbed { Target = target, Until = now + _barbLife };
        }

        private void DragSecond(Health first, WeaponModifier modifier)
        {
            var now = Time.time;
            for (var i = 0; i < MaxBarbed; i++)
                if (_barbed[i].Target != null && _barbed[i].Target != first && _barbed[i].Until > now && _barbed[i].Target.IsAlive)
                {
                    Drag(_barbed[i].Target, modifier);
                    return;
                }
        }

        private void Drag(Health target, WeaponModifier modifier)
        {
            if (target.Max > _maxPullHealth || !target.TryGetComponent<NavMeshAgent>(out var agent) || !agent.enabled)
                return;
            for (var i = 0; i < MaxPulls; i++)
            {
                if (_pulls[i].Target != null && _pulls[i].Until > Time.time)
                    continue;
                _pulls[i] = new Pull { Agent = agent, Target = target, Until = Time.time + _pullTime, Grab = true };
                if (!_pullSound.IsNull)
                    RuntimeManager.PlayOneShot(_pullSound, target.transform.position);
                if (modifier != null && modifier.Kind == ModifierKind.FeedingLine && _charge != null)
                    _charge.Add(modifier.Value);
                for (var b = 0; b < MaxBarbed; b++)
                    if (_barbed[b].Target == target)
                        _barbed[b].Target = null;
                return;
            }
        }

        private void Zip(Health target)
        {
            if (_motor == null)
                return;
            var to = target.transform.position - transform.position;
            to.y = 0f;
            var distance = to.magnitude - _pullStop;
            if (distance > 0.2f)
                _motor.Lunge(to / to.magnitude, distance, _zipTime);
        }

        private void Update()
        {
            var now = Time.time;
            var dt = Time.deltaTime;
            for (var i = 0; i < MaxPulls; i++)
            {
                ref var pull = ref _pulls[i];
                if (pull.Target == null)
                    continue;
                if (pull.Agent == null || !pull.Agent.enabled || !pull.Agent.isOnNavMesh || !pull.Target.IsAlive)
                {
                    pull.Target = null;
                    continue;
                }
                var goal = transform.position + transform.forward * _pullStop;
                var offset = goal - pull.Target.transform.position;
                offset.y = 0f;
                var remaining = Mathf.Max(0.01f, pull.Until - now);
                pull.Agent.Move(offset * Mathf.Clamp01(dt / remaining));
                if (now < pull.Until)
                    continue;
                if (pull.Grab && _consume != null && pull.Target.TryGetComponent<Consumable>(out var consumable) && consumable.IsGrabbable)
                    _consume.GrabTarget(consumable);
                pull.Target = null;
            }
        }
    }
}

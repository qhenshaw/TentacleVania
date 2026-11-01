using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Core;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using TentacleVania.Enemies;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

namespace TentacleVania.Bosses
{
    public class BossEnemy : Enemy
    {
        private const float FinishingStun = 100000f;

        [SerializeField] private BossDefinition _boss;
        [SerializeField] private Transform _essence;
        [SerializeField] private VisualEffect _absorbEffect;
        [Tooltip("Intro length on attempts after the creature has died to this boss once.")]
        [SerializeField, Min(0f)] private float _shortIntroTime = 1f;

        private readonly List<Enemy> _adds = new(8);
        private int _phase = -1;
        private float _nextAdd;
        private bool _introDone;
        private bool _framing;
        private bool _shortIntro;
        private Transform _restoreFocus;
        private Vector3 _essenceHome;
        private bool _finishing;
        private int _pendingStartAdds;

        public static BossEnemy Active { get; private set; }
        public static event Action<BossEnemy> IntroStarted;
        public static event Action<BossEnemy> PhaseChanged;
        public static event Action<BossEnemy> Absorbed;
        public static event Action<BossEnemy> Finishable;

        public BossDefinition Boss => _boss;
        public int Phase => _phase;
        public override bool IsBoss => true;
        protected override float SpawnDelay => _shortIntro ? Mathf.Min(_shortIntroTime, base.SpawnDelay) : base.SpawnDelay;
        protected override bool FeedProtection => false;
        public bool IsFinishing => _finishing;
        public IReadOnlyList<Enemy> Adds => _adds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = null;
            IntroStarted = null;
            PhaseChanged = null;
            Absorbed = null;
            Finishable = null;
        }

        protected override void Awake()
        {
            base.Awake();
            if (TryGetComponent<Combat.Consumable>(out var consumable))
            {
                consumable.CoreBleeds = true;
                consumable.Configure(1f, consumable.Biomass);
            }
            if (_essence != null)
            {
                _essenceHome = _essence.localPosition;
                _essence.gameObject.SetActive(false);
            }
            SetIntroCamera(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (BossProgress.IsDefeated(_boss.Id))
            {
                Destroy(gameObject);
                return;
            }
            Active = this;
            _shortIntro = BossProgress.HasDiedTo(_boss.Id);
            CombatEvents.Died += OnSomethingDied;
            _phase = -1;
            _introDone = false;
            _adds.Clear();
            _finishing = false;
            Health.Floor = 1f;
            if (ConsumeTarget != null)
                ConsumeTarget.Locked = true;
            EnterPhase(0, Time.time);
            SetIntroCamera(true);
            Play(_boss.IntroSound);
            IntroStarted?.Invoke(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            CombatEvents.Died -= OnSomethingDied;
            SetIntroCamera(false);
            if (Active == this)
                Active = null;
        }

        protected override bool AttackAllowed(int index)
        {
            var mask = _boss.Phases[Mathf.Max(0, _phase)].AttackMask;
            return mask == 0 || (mask & (1 << index)) != 0;
        }

        protected override void OnTick(float now, float dt)
        {
            if (!_introDone && State != EnemyState.Spawning)
            {
                _introDone = true;
                SetIntroCamera(false);
            }
            if (!Health.IsAlive || State == EnemyState.Dead || State == EnemyState.Consumed)
                return;
            if (!_finishing && Health.AtFloor)
                BeginFinishing();
            if (_finishing)
                return;
            var phase = BossProgress.PhaseFor(Health.Normalized, _boss.Thresholds);
            if (phase != _phase)
                EnterPhase(phase, now);
            if (_introDone)
                TickAdds(now);
        }

        public void BeginFinishing()
        {
            if (_finishing || !Health.IsAlive)
                return;
            _finishing = true;
            KillAdds();
            Play(_boss.DeathSound);
            if (ConsumeTarget == null)
            {
                Health.Floor = 0f;
                Health.Kill();
                return;
            }
            ConsumeTarget.Locked = false;
            Stun(FinishingStun);
            Finishable?.Invoke(this);
        }

        protected override void OnDeath()
        {
            KillAdds();
            BossProgress.Defeat(_boss.Id, ContentCatalog.IdOf(_boss.RewardAbility), ContentCatalog.IdOf(_boss.SecondReward));
            if (_boss.DropPrefab != null)
                Instantiate(_boss.DropPrefab, transform.position, Quaternion.identity).name = _boss.DropPrefab.name;
            if (!_finishing)
                Play(_boss.DeathSound);
            StartCoroutine(AbsorbRoutine(!_finishing));
        }

        private void KillAdds()
        {
            for (var i = _adds.Count - 1; i >= 0; i--)
                if (_adds[i] != null && _adds[i].Health.IsAlive)
                    _adds[i].Health.Kill();
            _adds.Clear();
        }

        private void EnterPhase(int phase, float now)
        {
            var changed = _phase >= 0;
            _phase = phase;
            var data = _boss.Phases[phase];
            BaseSpeed = Definition.MoveSpeed * data.MoveSpeedScale;
            _nextAdd = now + data.AddInterval;
            _pendingStartAdds = data.AddsOnStart;
            if (!changed)
                return;
            Play(data.Sting);
            PhaseChanged?.Invoke(this);
        }

        private void TickAdds(float now)
        {
            for (var i = _adds.Count - 1; i >= 0; i--)
            {
                var add = _adds[i];
                if (add == null || !add.Health.IsAlive || add.State == EnemyState.Consumed)
                    _adds.RemoveAt(i);
            }
            var data = _boss.Phases[_phase];
            while (_pendingStartAdds > 0 && data.AddPrefab != null && _adds.Count < data.MaxAdds)
            {
                _pendingStartAdds--;
                SpawnAdd(data.AddPrefab);
            }
            _pendingStartAdds = 0;
            if (data.AddPrefab == null || _adds.Count >= data.MaxAdds || now < _nextAdd)
                return;
            _nextAdd = now + data.AddInterval;
            SpawnAdd(data.AddPrefab);
        }

        public Enemy SpawnAdd(Enemy prefab)
        {
            var direction = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f) * Vector3.forward;
            var position = transform.position + direction * _boss.AddSpawnDistance;
            if (NavMesh.SamplePosition(position, out var hit, _boss.AddSpawnDistance, NavMesh.AllAreas))
                position = hit.position;
            var add = Instantiate(prefab, position, Quaternion.LookRotation(direction));
            _adds.Add(add);
            return add;
        }

        public Enemy SpawnAdd(Enemy prefab, Vector3 position)
        {
            if (NavMesh.SamplePosition(position, out var hit, 2f, NavMesh.AllAreas))
                position = hit.position;
            var away = position - transform.position;
            away.y = 0f;
            var add = Instantiate(prefab, position, away.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(away) : Quaternion.identity);
            _adds.Add(add);
            return add;
        }

        private IEnumerator AbsorbRoutine(bool flyEssence)
        {
            yield return new WaitForSeconds(flyEssence ? _boss.DeathPause : 0.2f);
            var creature = CombatTargets.Creature;
            if (flyEssence && _essence != null && creature != null)
            {
                _essence.SetParent(null, true);
                _essence.gameObject.SetActive(true);
                var start = _essence.position;
                var elapsed = 0f;
                while (elapsed < _boss.AbsorbDuration && creature != null)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / _boss.AbsorbDuration);
                    var eased = t * t * (3f - 2f * t);
                    var end = creature.transform.position + Vector3.up;
                    _essence.position = Vector3.Lerp(start, end, eased) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 2.5f);
                    yield return null;
                }
                _essence.gameObject.SetActive(false);
                _essence.SetParent(transform, false);
                _essence.localPosition = _essenceHome;
            }
            if (_absorbEffect != null && creature != null)
            {
                _absorbEffect.transform.SetParent(null, true);
                _absorbEffect.transform.position = creature.transform.position + Vector3.up;
                _absorbEffect.Reinit();
                _absorbEffect.Play();
                Destroy(_absorbEffect.gameObject, 3f);
            }
            if (creature != null)
                Play(_boss.AbsorbSound, creature.transform.position);
            Absorbed?.Invoke(this);
            if (Active == this)
                Active = null;
        }

        private void OnSomethingDied(Health health)
        {
            if (health == CombatTargets.Creature && Active == this && Health.IsAlive)
                BossProgress.RecordDeath(_boss.Id);
        }

        private void SetIntroCamera(bool on)
        {
            if (on && !_framing)
            {
                _restoreFocus = CameraFocus.Target;
                CameraFocus.Target = transform;
                Cutscene.Begin(this);
                _framing = true;
            }
            else if (!on && _framing)
            {
                if (CameraFocus.Target == transform)
                    CameraFocus.Target = _restoreFocus;
                Cutscene.End(this);
                _framing = false;
            }
        }

        private void Play(EventReference sound) => Play(sound, transform.position);

        private static void Play(EventReference sound, Vector3 position)
        {
            if (!sound.IsNull)
                RuntimeManager.PlayOneShot(sound, position);
        }
    }
}

using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Enemies;
using TentacleVania.Enemies.Data;
using TentacleVania.Enemies.Traps;
using UnityEngine;

namespace TentacleVania.Bosses.Crane
{
    public sealed class CraneBoss : BossEnemy
    {
        public enum LimbKind
        {
            Slam,
            Sweep,
            Grab,
        }

        private sealed class HeartGuard : IDamageModifier
        {
            public CraneBoss Owner;
            public int Order => 950;

            public void Modify(ref DamageContext context)
            {
                if (Owner.Vulnerable)
                    return;
                context.HealthScale = 0f;
                context.ArmorScale = 0f;
                context.StaggerScale = 0f;
            }
        }

        public const int LimbCount = 3;

        [Header("Phase 1: lab rig")]
        [SerializeField] private string _mistAttack = "Sedative Mist";
        [SerializeField] private FireZone _mist;
        [SerializeField, Min(0.1f)] private float _mistRadius = 3f;
        [SerializeField, Min(0.1f)] private float _mistDuration = 6f;

        [Header("Phase 2: hive fusion")]
        [SerializeField] private string _eruptionAttack = "Tissue Eruptions";
        [SerializeField, Min(0)] private int _extraEruptions = 3;
        [SerializeField, Min(0f)] private float _eruptionSpread = 5f;
        [SerializeField] private Transform _model;
        [SerializeField] private GameObject _rig;
        [SerializeField, Min(1)] private int _fusionPhase = 1;
        [SerializeField, Min(0.1f)] private float _fusionScale = 1.25f;

        [Header("Phase 3: arena-filling horror (sever limbs, then the heart)")]
        [SerializeField, Min(1)] private int _horrorPhase = 2;
        [SerializeField, Min(0.1f)] private float _horrorScale = 2f;
        [SerializeField] private BossPart _limbPrefab;
        [SerializeField, Min(1f)] private float _limbRing = 5f;
        [SerializeField, Min(0.5f)] private float _limbInterval = 2.5f;
        [SerializeField] private AcidBurst _slam;
        [SerializeField, Min(0f)] private float _slamDamage = 28f;
        [SerializeField] private FireZone _sweep;
        [SerializeField] private FireZone _grab;
        [Tooltip("Seconds the heart stays exposed once every limb is severed; a limb regrows when it runs out.")]
        [SerializeField, Min(1f)] private float _heartWindow = 12f;
        [SerializeField] private GameObject _heart;
        [SerializeField] private EventReference _severSound;
        [SerializeField] private EventReference _regrowSound;

        private readonly HeartGuard _guard = new();
        private readonly BossPart[] _limbs = new BossPart[LimbCount];
        private Vector3 _modelScale;
        private bool _fused;
        private bool _horror;
        private float _nextLimb;
        private float _heartUntil;
        private int _nextLimbIndex;

        public bool Horror => _horror;
        public bool HeartExposed => _horror && SeveredLimbs >= LimbCount;
        public bool Vulnerable => !_horror || HeartExposed || IsFinishing;
        public BossPart Limb(int index) => _limbs[index];

        public int SeveredLimbs
        {
            get
            {
                var severed = 0;
                foreach (var limb in _limbs)
                    if (limb != null && !limb.Inserted)
                        severed++;
                return severed;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _guard.Owner = this;
            if (_model != null)
                _modelScale = _model.localScale;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Health.Incoming.Add(_guard);
            _fused = false;
            _horror = false;
            _heartUntil = float.MaxValue;
            if (_model != null)
                _model.localScale = _modelScale;
            if (_rig != null)
                _rig.SetActive(true);
            if (_heart != null)
                _heart.SetActive(false);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Health.Incoming.Remove(_guard);
            ClearLimbs();
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            ClearLimbs();
        }

        protected override Vector3 ChaseDestination(Vector3 targetPosition) => _horror ? Position : base.ChaseDestination(targetPosition);

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => _horror ? Position : base.CircleDestination(targetPosition, dt);

        protected override bool AttackAllowed(int index) => !_horror && base.AttackAllowed(index);

        protected override void OnActiveStart(EnemyAttack attack)
        {
            base.OnActiveStart(attack);
            if (attack.Name == _mistAttack && _mist != null)
                EnemyManager.PlaceFire(_mist, AreaCenter, Vector3.forward, _mistRadius, 360f, 0.4f, _mistDuration, 2f, Health);
            if (attack.Name != _eruptionAttack || attack.BurstPrefab == null)
                return;
            var info = DamageInfo.Create(attack.Damage, Team.Site, attack.Type, attack.Stagger);
            info.Attacker = Health;
            for (var i = 0; i < _extraEruptions; i++)
            {
                var spot = AreaCenter + Quaternion.Euler(0f, i * 360f / Mathf.Max(1, _extraEruptions) + Random.Range(0f, 40f), 0f) * Vector3.forward * Random.Range(attack.AreaRadius * 1.5f, _eruptionSpread);
                EnemyManager.PlaceBurst(attack.BurstPrefab, spot, attack.AreaRadius, attack.BurstFuse + 0.15f * (i + 1), info);
            }
        }

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            if (!Health.IsAlive || State == EnemyState.Dead || State == EnemyState.Consumed || State == EnemyState.Spawning || IsFinishing)
                return;
            if (!_fused && Phase >= _fusionPhase)
                Fuse();
            if (!_horror && Phase >= _horrorPhase)
                BecomeHorror(now);
            if (!_horror)
                return;
            BaseSpeed = 0f;
            if (_heart != null && _heart.activeSelf != HeartExposed)
                _heart.SetActive(HeartExposed);
            if (HeartExposed)
            {
                if (_heartUntil == float.MaxValue)
                    _heartUntil = now + _heartWindow;
                if (now >= _heartUntil)
                    Regrow();
                return;
            }
            _heartUntil = float.MaxValue;
            if (now >= _nextLimb)
            {
                _nextLimb = now + _limbInterval;
                LimbAttack();
            }
        }

        private void Fuse()
        {
            _fused = true;
            if (_rig != null)
                _rig.SetActive(false);
            if (_model != null)
                _model.localScale = _modelScale * _fusionScale;
        }

        private void BecomeHorror(float now)
        {
            _horror = true;
            if (_model != null)
                _model.localScale = _modelScale * _horrorScale;
            _nextLimb = now + 2f;
            if (_limbPrefab == null)
                return;
            for (var i = 0; i < LimbCount; i++)
            {
                var spot = Position + Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * _limbRing;
                _limbs[i] = Instantiate(_limbPrefab, spot, Quaternion.identity);
                _limbs[i].name = ((LimbKind)i) + "Limb";
                _limbs[i].Pulled += OnSevered;
            }
        }

        private void OnSevered(BossPart limb)
        {
            if (!_severSound.IsNull)
                RuntimeManager.PlayOneShot(_severSound, limb.transform.position);
        }

        private void Regrow()
        {
            _heartUntil = float.MaxValue;
            var limb = _limbs[Random.Range(0, LimbCount)];
            if (limb != null)
                limb.Restore();
            if (!_regrowSound.IsNull)
                RuntimeManager.PlayOneShot(_regrowSound, Position);
        }

        private void LimbAttack()
        {
            var creature = CombatTargets.Creature;
            if (creature == null || !creature.IsAlive)
                return;
            for (var tries = 0; tries < LimbCount; tries++)
            {
                var index = _nextLimbIndex;
                _nextLimbIndex = (_nextLimbIndex + 1) % LimbCount;
                if (_limbs[index] == null || !_limbs[index].Inserted)
                    continue;
                var target = creature.transform.position;
                switch ((LimbKind)index)
                {
                    case LimbKind.Slam:
                        if (_slam != null)
                        {
                            var info = DamageInfo.Create(_slamDamage, Team.Site, DamageType.Physical);
                            info.Attacker = Health;
                            EnemyManager.PlaceBurst(_slam, target, 2.5f, 1.2f, info);
                        }
                        break;
                    case LimbKind.Sweep:
                        if (_sweep != null)
                            EnemyManager.PlaceFire(_sweep, Position, target - Position, 14f, 110f, 1f, 0.3f, 60f, Health);
                        break;
                    case LimbKind.Grab:
                        if (_grab != null)
                            EnemyManager.PlaceFire(_grab, target, Vector3.forward, 1.5f, 360f, 0.9f, 0.3f, 40f, Health);
                        break;
                }
                return;
            }
        }

        private void ClearLimbs()
        {
            for (var i = 0; i < LimbCount; i++)
            {
                if (_limbs[i] != null)
                {
                    _limbs[i].Pulled -= OnSevered;
                    Destroy(_limbs[i].gameObject);
                }
                _limbs[i] = null;
            }
        }
    }
}

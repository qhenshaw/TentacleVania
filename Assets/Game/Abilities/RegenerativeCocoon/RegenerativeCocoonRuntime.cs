using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using TentacleVania.Creature.Movement;
using UnityEngine;

namespace TentacleVania.Abilities.RegenerativeCocoon
{
    public sealed class RegenerativeCocoonRuntime : AbilityRuntime, IDamageModifier
    {
        private readonly RegenerativeCocoonDefinition _definition;
        private readonly AbilityContext _context;
        private readonly CreatureMotor _motor;
        private readonly Transform _shell;
        private float _remaining;
        private float _healPerSecond;
        private bool _hardened;
        private bool _locked;

        public int Breaks { get; private set; }
        public int Absorbed { get; private set; }

        public RegenerativeCocoonRuntime(RegenerativeCocoonDefinition definition, AbilityContext context)
        {
            _definition = definition;
            _context = context;
            _motor = context.Owner.GetComponent<CreatureMotor>();
            if (definition.Shell != null)
            {
                _shell = Object.Instantiate(definition.Shell);
                _shell.gameObject.SetActive(false);
            }
        }

        public int Order => 960;

        public override bool IsActive => _remaining > 0f;

        public override void Activate()
        {
            var tier = Upgrades.AbilityTier(ContentCatalog.IdOf(_definition));
            _healPerSecond = _context.Health.Max * _definition.HealPerSecondAt(tier);
            _hardened = tier >= _definition.HardenedTier;
            _remaining = _definition.MaxDuration;
            if (_motor != null && !_locked)
            {
                _motor.LockControl();
                _locked = true;
            }
            _context.Health.Incoming.Add(this);
            _context.Health.Damaged += OnDamaged;
            if (_shell != null)
            {
                _shell.position = _context.Owner.position;
                _shell.gameObject.SetActive(true);
            }
            if (!_definition.ActivateSound.IsNull)
                RuntimeManager.PlayOneShot(_definition.ActivateSound, _context.Owner.position);
        }

        public void Modify(ref DamageContext context)
        {
            if (!_hardened || context.Info.Amount <= 0f || (context.Info.Flags & HitFlags.DamageOverTime) != 0)
                return;
            _hardened = false;
            Absorbed++;
            context.Cancelled = true;
        }

        private void OnDamaged(Health health, in DamageInfo info, in DamageResult result)
        {
            if (result.HealthDamage <= 0f)
                return;
            Breaks++;
            if (!_definition.BreakSound.IsNull)
                RuntimeManager.PlayOneShot(_definition.BreakSound, _context.Owner.position);
            End();
        }

        public override void Tick(float dt)
        {
            if (_remaining <= 0f)
                return;
            _remaining -= dt;
            if (_shell != null)
                _shell.position = _context.Owner.position;
            _context.Health.Heal(_healPerSecond * dt);
            if (_remaining <= 0f || _context.Health.Current >= _context.Health.Max || !_context.Health.IsAlive)
                End();
        }

        public override void Cancel() => End();

        public override void Dispose()
        {
            End();
            if (_shell != null)
                Object.Destroy(_shell.gameObject);
        }

        private void End()
        {
            _remaining = 0f;
            _context.Health.Incoming.Remove(this);
            _context.Health.Damaged -= OnDamaged;
            if (_shell != null)
                _shell.gameObject.SetActive(false);
            if (_locked && _motor != null)
                _motor.UnlockControl();
            _locked = false;
        }
    }
}

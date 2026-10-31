using System;
using TentacleVania.Combat;
using TentacleVania.Core.Characters;
using TentacleVania.Core.Input;
using TentacleVania.Core.Saves;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using UnityEngine;

namespace TentacleVania.Abilities
{
    [RequireComponent(typeof(Health))]
    public sealed class AbilityController : MonoBehaviour, ISaveParticipant
    {
        [SerializeField] private AbilityDefinition _equipped;
        [SerializeField] private ContentCatalog _catalog;

        private Health _health;
        private CreatureCharge _charge;
        private CreatureMotor _motor;
        private AbilityContext _context;

        public static bool FreeAbilities { get; set; }

        public event Action<AbilityController> Activated;

        public AbilityDefinition Equipped => _equipped;
        public AbilityRuntime Runtime { get; private set; }
        public bool IsActive => Runtime != null && Runtime.IsActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => FreeAbilities = false;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _charge = GetComponent<CreatureCharge>();
            _motor = GetComponent<CreatureMotor>();
            TryGetComponent<CharacterMaterials>(out var materials);
            _context = new AbilityContext(transform, _health, materials, RefreshStatus);
            Equip(_equipped);
        }

        private void OnEnable() => SaveSystem.Register(this);

        private void OnDisable()
        {
            SaveSystem.Unregister(this);
            if (Runtime != null)
                Runtime.Cancel();
        }

        public void Capture(SaveData data) => data.Build.Ability = ContentCatalog.IdOf(_equipped);

        public void Restore(SaveData data)
        {
            if (_catalog == null)
                return;
            var ability = _catalog.Find<AbilityDefinition>(data.Build.Ability);
            if (ability != null && ability != _equipped)
                Equip(ability);
        }

        private void OnDestroy()
        {
            if (Runtime != null)
                Runtime.Dispose();
        }

        public void Equip(AbilityDefinition ability)
        {
            if (Runtime != null)
                Runtime.Dispose();
            _equipped = ability;
            Runtime = ability != null ? ability.CreateRuntime(_context) : null;
        }

        public bool TryActivate()
        {
            if (Runtime == null || Runtime.IsActive || !_health.IsAlive)
                return false;
            if (!FreeAbilities && (_charge == null || !_charge.TrySpend(_equipped.Cost)))
                return false;
            Runtime.Activate();
            Activated?.Invoke(this);
            return true;
        }

        private void Update()
        {
            if (Runtime == null)
                return;
            if (!_health.IsAlive && Runtime.IsActive)
                Runtime.Cancel();
            Runtime.Tick(Time.deltaTime);
            if (_motor != null && !_motor.InputEnabled)
                return;
            if (GameplayInput.Default.Ability.Pressed)
                TryActivate();
        }

        private void RefreshStatus()
        {
            if (_charge != null)
                _charge.RefreshGlow();
        }
    }
}

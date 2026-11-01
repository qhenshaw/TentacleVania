using FMODUnity;
using TentacleVania.Combat.Damage;
using TentacleVania.Combat.Status;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Abilities.SpawnBrood
{
    public sealed class SpawnBroodRuntime : AbilityRuntime
    {
        private readonly SpawnBroodDefinition _definition;
        private readonly AbilityContext _context;
        private float _remaining;

        public int LastSpawned { get; private set; }

        public SpawnBroodRuntime(SpawnBroodDefinition definition, AbilityContext context)
        {
            _definition = definition;
            _context = context;
        }

        public override bool IsActive => _remaining > 0f;

        public override void Activate()
        {
            var tier = Upgrades.AbilityTier(ContentCatalog.IdOf(_definition));
            var position = _context.Owner.position;
            _remaining = _definition.ActiveTime;
            if (!_definition.ActivateSound.IsNull)
                RuntimeManager.PlayOneShot(_definition.ActivateSound, position);
            ParasiteSwarm.PlayBurst(position + Vector3.up * 0.5f);
            LastSpawned = ParasiteSwarm.Spawn(position, _definition.MinionsAt(tier), Team.Creature);
        }

        public override void Tick(float dt) => _remaining = Mathf.Max(0f, _remaining - dt);

        public override void Cancel() => _remaining = 0f;
    }
}

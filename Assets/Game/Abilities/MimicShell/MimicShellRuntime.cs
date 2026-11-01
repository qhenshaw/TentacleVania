using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using UnityEngine;

namespace TentacleVania.Abilities.MimicShell
{
    public sealed class MimicShellRuntime : AbilityRuntime, IDamageModifier
    {
        private readonly MimicShellDefinition _definition;
        private readonly AbilityContext _context;
        private float _remaining;
        private float _ambush;
        private int _ambushFrame = -1;

        public int Ambushes { get; private set; }

        public MimicShellRuntime(MimicShellDefinition definition, AbilityContext context)
        {
            _definition = definition;
            _context = context;
        }

        public int Order => 350;

        public override bool IsActive => _remaining > 0f;

        public override void Activate()
        {
            var tier = Upgrades.AbilityTier(ContentCatalog.IdOf(_definition));
            _remaining = _definition.DurationAt(tier);
            _ambush = _definition.AmbushAt(tier);
            CombatTargets.Hidden = true;
            _context.Health.Outgoing.Add(this);
            if (_context.Materials != null)
                _context.Materials.SetGlow(_definition.DisguiseGlow, _definition.DisguiseAmount);
            if (!_definition.ActivateSound.IsNull)
                RuntimeManager.PlayOneShot(_definition.ActivateSound, _context.Owner.position);
        }

        public void Modify(ref DamageContext context)
        {
            if (context.TargetTeam == _context.Health.Team || (context.Info.Flags & (HitFlags.DamageOverTime | HitFlags.Hazard)) != 0)
                return;
            if (_remaining <= 0f && Time.frameCount != _ambushFrame)
                return;
            context.HealthScale *= _ambush;
            context.StaggerScale *= _definition.AmbushStagger;
            if (_remaining <= 0f)
                return;
            _ambushFrame = Time.frameCount;
            Ambushes++;
            if (!_definition.AmbushSound.IsNull)
                RuntimeManager.PlayOneShot(_definition.AmbushSound, _context.Owner.position);
            End(false);
        }

        public override void Tick(float dt)
        {
            if (_remaining > 0f)
            {
                _remaining -= dt;
                if (_remaining <= 0f)
                    End(true);
            }
            else if (_ambushFrame >= 0 && Time.frameCount != _ambushFrame)
            {
                _ambushFrame = -1;
                _context.Health.Outgoing.Remove(this);
            }
        }

        public override void Cancel() => End(true);

        public override void Dispose() => End(true);

        private void End(bool removeNow)
        {
            _remaining = 0f;
            CombatTargets.Hidden = false;
            if (removeNow)
            {
                _ambushFrame = -1;
                _context.Health.Outgoing.Remove(this);
            }
            if (_context.Materials != null)
                _context.Materials.ClearGlow();
        }
    }
}

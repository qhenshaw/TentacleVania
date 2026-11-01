using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using TentacleVania.Core.Progression;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Last Gasp")]
    public sealed class LastGaspMutation : MutationDefinition
    {
        [SerializeField, Min(0.1f)] private float _frenzySeconds = 5f;
        [SerializeField, Min(1f)] private float _frenzyDamage = 1.5f;
        [SerializeField, Min(1f)] private float _frenzySpeed = 1.3f;

        public static bool Ready { get; private set; }

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly LastGaspMutation _data;
            private readonly ScaleModifier _frenzy = new(330);
            private MutationHost _host;
            private float _until = -1f;

            public Effect(LastGaspMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                _host = host;
                Nests.Rested += OnRested;
                if (host.Health == null)
                    return;
                host.Health.Damaged += OnDamaged;
                Arm();
            }

            public override void Disable(MutationHost host)
            {
                Nests.Rested -= OnRested;
                EndFrenzy();
                if (host.Health == null)
                    return;
                host.Health.Damaged -= OnDamaged;
                if (Ready)
                    host.Health.Floor = 0f;
                Ready = false;
            }

            public override void Tick(MutationHost host, float now)
            {
                if (_until >= 0f && now >= _until)
                    EndFrenzy();
            }

            private void OnRested(string nest) => Arm();

            private void Arm()
            {
                if (_host.Health == null)
                    return;
                Ready = true;
                _host.Health.Floor = Mathf.Max(_host.Health.Floor, 1f);
            }

            private void OnDamaged(Health target, in DamageInfo info, in DamageResult result)
            {
                if (!Ready || !target.AtFloor)
                    return;
                Ready = false;
                target.Floor = 0f;
                if (_until < 0f)
                {
                    target.Outgoing.Add(_frenzy);
                    if (_host.Motor != null)
                        _host.Motor.SpeedMultiplier *= _data._frenzySpeed;
                }
                _frenzy.HealthScale = _data._frenzyDamage;
                _until = Time.time + _data._frenzySeconds;
            }

            private void EndFrenzy()
            {
                if (_until < 0f)
                    return;
                _until = -1f;
                if (_host.Health != null)
                    _host.Health.Outgoing.Remove(_frenzy);
                if (_host.Motor != null)
                    _host.Motor.SpeedMultiplier /= _data._frenzySpeed;
            }
        }
    }
}

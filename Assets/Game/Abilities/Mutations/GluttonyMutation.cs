using TentacleVania.Combat;
using TentacleVania.Creature;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [CreateAssetMenu(menuName = "TentacleVania/Mutations/Gluttony")]
    public sealed class GluttonyMutation : MutationDefinition
    {
        [SerializeField, Min(0f)] private float _window = 6f;
        [SerializeField, Min(1f)] private float _speed = 1.3f;
        [SerializeField, Min(1f)] private float _heal = 1.5f;

        public override MutationEffect CreateEffect() => new Effect(this);

        private sealed class Effect : MutationEffect
        {
            private readonly GluttonyMutation _data;
            private MutationHost _host;
            private float _until = -1f;

            public Effect(GluttonyMutation data) => _data = data;

            public override void Enable(MutationHost host)
            {
                _host = host;
                host.Consume.Consuming += OnConsuming;
            }

            public override void Disable(MutationHost host)
            {
                host.Consume.Consuming -= OnConsuming;
                Clear();
            }

            public override void Tick(MutationHost host, float now)
            {
                if (_until >= 0f && now > _until)
                    Clear();
            }

            private void OnConsuming(CreatureConsume consume, Consumable victim)
            {
                consume.NextConsumeSpeed = _data._speed;
                consume.NextHealScale = _data._heal;
                _until = Time.time + _data._window;
            }

            private void Clear()
            {
                _until = -1f;
                if (_host == null)
                    return;
                _host.Consume.NextConsumeSpeed = 1f;
                _host.Consume.NextHealScale = 1f;
            }
        }
    }
}

using TentacleVania.Abilities.Grapple;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    public sealed class MutationHost
    {
        public MutationHost(CreatureMotor motor, CreatureConsume consume, TentacleGrapple grapple)
        {
            Motor = motor;
            Consume = consume;
            Grapple = grapple;
        }

        public CreatureMotor Motor { get; }
        public CreatureConsume Consume { get; }
        public TentacleGrapple Grapple { get; }
    }

    public abstract class MutationEffect
    {
        public abstract void Enable(MutationHost host);
        public abstract void Disable(MutationHost host);
        public virtual void Tick(MutationHost host, float now) { }
    }

    public abstract class MutationDefinition : ScriptableObject
    {
        [Tooltip("UI table key prefix: <key>.name and <key>.effect.")]
        [SerializeField] private string _textKey = "mutation.gluttony";
        [SerializeField, Range(1, 3)] private int _notches = 1;

        public string TextKey => _textKey;
        public int Notches => _notches;

        public abstract MutationEffect CreateEffect();
    }
}

using FMODUnity;
using UnityEngine;

namespace TentacleVania.Abilities.Breach
{
    [CreateAssetMenu(menuName = "TentacleVania/Abilities/Breach")]
    public sealed class BreachDefinition : AbilityDefinition
    {
        [Header("Breach (design: Breach page; values are placeholders)")]
        [Tooltip("How close a weak wall must be (to its nearest point) for LT to smash it.")]
        [SerializeField, Min(0.5f)] private float _range = 2.5f;
        [SerializeField, Range(10f, 180f)] private float _maxAngle = 75f;
        [SerializeField, Min(0.01f)] private float _lungeTime = 0.18f;
        [Tooltip("The lunge stops this far short of the wall.")]
        [SerializeField, Min(0f)] private float _stopDistance = 0.9f;
        [SerializeField, Min(0f)] private float _recoverTime = 0.15f;
        [SerializeField, Min(0f)] private float _hitStop = 0.08f;
        [SerializeField, Min(0f)] private float _shake = 0.8f;
        [SerializeField] private Vector2 _rumble = new(0.6f, 0.9f);
        [SerializeField, Min(0f)] private float _rumbleTime = 0.25f;
        [SerializeField] private EventReference _lungeSound;

        public override bool SlotAbility => false;
        public float Range => _range;
        public float MaxAngle => _maxAngle;
        public float LungeTime => _lungeTime;
        public float StopDistance => _stopDistance;
        public float RecoverTime => _recoverTime;
        public float HitStop => _hitStop;
        public float Shake => _shake;
        public Vector2 Rumble => _rumble;
        public float RumbleTime => _rumbleTime;
        public EventReference LungeSound => _lungeSound;

        public override AbilityRuntime CreateRuntime(AbilityContext context) => null;
    }
}

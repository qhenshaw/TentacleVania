using TentacleVania.Combat;
using TentacleVania.Core;
using TentacleVania.Core.Rendering;
using UnityEngine;

namespace TentacleVania.UI.Hud
{
    public sealed class LowHealthScreenView : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _creepSpeed = 0.6f;
        [SerializeField, Min(0.01f)] private float _recedeSpeed = 1.5f;
        [SerializeField, Min(1f)] private float _lightBpm = 70f;
        [SerializeField, Min(1f)] private float _heavyBpm = 110f;
        [SerializeField, Range(0f, 1f)] private float _lurchDepth = 0.8f;
        [SerializeField, Min(0.05f)] private float _lurchDuration = 0.9f;

        private float _amount;
        private float _consumeStarted = -100f;

        public static int ForceStep { get; set; } = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ForceStep = -1;

        private void OnEnable()
        {
            _amount = 0f;
            CombatEvents.ConsumeStarted += OnConsumeStarted;
        }

        private void OnDisable()
        {
            CombatEvents.ConsumeStarted -= OnConsumeStarted;
            LowHealthTendrilsFeature.Amount = 0f;
            LowHealthTendrilsFeature.Pulse = 0f;
        }

        private void LateUpdate()
        {
            var creature = CombatTargets.Creature;
            var target = ForceStep switch
            {
                0 => 0f,
                1 => LowHealthRules.LightAmount,
                2 => LowHealthRules.HeavyAmount,
                _ => creature != null ? LowHealthRules.Target(creature.Normalized, creature.IsAlive) : 0f,
            };
            var dt = Time.unscaledDeltaTime;
            _amount = Mathf.MoveTowards(_amount, target, (target > _amount ? _creepSpeed : _recedeSpeed) * dt);
            var lurch = LowHealthRules.Lurch(Time.unscaledTime - _consumeStarted, _lurchDuration);
            var bpm = target >= LowHealthRules.HeavyAmount ? _heavyBpm : _lightBpm;
            LowHealthTendrilsFeature.Amount = _amount * (1f - _lurchDepth * lurch);
            LowHealthTendrilsFeature.Pulse = GameSettings.Flashes ? LowHealthRules.Heartbeat(Time.unscaledTime, bpm) : 0f;
        }

        private void OnConsumeStarted(Consumable consumable) => _consumeStarted = Time.unscaledTime;
    }
}

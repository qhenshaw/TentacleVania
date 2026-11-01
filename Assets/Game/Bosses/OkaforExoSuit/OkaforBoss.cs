using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Bosses.Okafor
{
    public sealed class OkaforBoss : BossEnemy
    {
        private sealed class SuitPlating : IDamageModifier
        {
            public OkaforBoss Owner;
            public int Order => 940;

            public void Modify(ref DamageContext context) =>
                context.HealthScale *= Owner.IsStaggerBroken ? Owner._exposedScale : Owner._platedScale;
        }

        [Header("Exo-suit (design: stagger breaks expose the pilot)")]
        [Tooltip("Health damage multiplier while the suit is closed.")]
        [SerializeField, Range(0f, 1f)] private float _platedScale = 0.5f;
        [Tooltip("Health damage multiplier while the stagger break exposes the pilot (on top of the usual stagger window).")]
        [SerializeField, Min(1f)] private float _exposedScale = 2f;
        [SerializeField] private GameObject _cockpit;

        private readonly SuitPlating _plating = new();

        protected override void Awake()
        {
            base.Awake();
            _plating.Owner = this;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Health.Incoming.Add(_plating);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Health.Incoming.Remove(_plating);
        }

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            if (_cockpit != null && _cockpit.activeSelf == IsStaggerBroken)
                _cockpit.SetActive(!IsStaggerBroken);
        }
    }
}

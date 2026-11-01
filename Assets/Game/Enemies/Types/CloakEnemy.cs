using System.Collections.Generic;
using FMODUnity;
using TentacleVania.Combat;
using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class CloakEnemy : GuardEnemy
    {
        [Header("Cloak (design: cloaks and ambushes)")]
        [Tooltip("Seconds without attacking or being hit before it cloaks again.")]
        [SerializeField, Min(0.5f)] private float _recloakDelay = 5f;
        [SerializeField] private EventReference _decloakSound;

        private readonly List<Renderer> _renderers = new();
        private bool _cloaked;
        private float _visibleUntil;

        public bool Cloaked => _cloaked;

        protected override void Awake()
        {
            base.Awake();
            GetComponentsInChildren(true, _renderers);
            for (var i = _renderers.Count - 1; i >= 0; i--)
                if (_renderers[i] is LineRenderer || _renderers[i].GetComponentInParent<Telegraphs.GroundIndicator>() != null)
                    _renderers.RemoveAt(i);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Health.Damaged += OnHit;
            SetCloak(true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Health.Damaged -= OnHit;
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            SetCloak(false);
        }

        private void OnHit(Health health, in DamageInfo info, in DamageResult result) => Reveal();

        protected override void OnTick(float now, float dt)
        {
            base.OnTick(now, dt);
            if (State == EnemyState.Attacking || State == EnemyState.Stunned)
                Reveal();
            else if (!_cloaked && now >= _visibleUntil && State != EnemyState.Dead && State != EnemyState.Consumed)
                SetCloak(true);
        }

        private void Reveal()
        {
            _visibleUntil = Time.time + _recloakDelay;
            if (!_cloaked)
                return;
            SetCloak(false);
            if (!_decloakSound.IsNull)
                RuntimeManager.PlayOneShot(_decloakSound, Position);
        }

        private void SetCloak(bool cloaked)
        {
            _cloaked = cloaked;
            foreach (var renderer in _renderers)
                if (renderer != null)
                    renderer.enabled = !cloaked;
        }
    }
}

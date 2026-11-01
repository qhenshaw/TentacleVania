using TentacleVania.Enemies.AI;
using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class RiflemanEnemy : GuardEnemy
    {
        [Header("Cover (design: fires from behind barricades)")]
        [SerializeField, Min(1f)] private float _coverSearch = 16f;
        [Tooltip("Creature distance from the cover point at which it's worth holding.")]
        [SerializeField] private Vector2 _coverRange = new(3f, 8f);
        [Tooltip("The creature must be at least this far in front of the barricade (dot of its facing).")]
        [SerializeField, Range(-1f, 1f)] private float _minFacing = 0.3f;

        private CoverPoint _cover;

        public CoverPoint Cover => _cover;
        public bool InCover
        {
            get
            {
                if (_cover == null)
                    return false;
                var offset = Position - _cover.Position;
                offset.y = 0f;
                var reach = Mathf.Max(0.6f, Agent.stoppingDistance + 0.2f);
                return offset.sqrMagnitude < reach * reach;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Release();
        }

        protected override void OnDeath()
        {
            base.OnDeath();
            Release();
        }

        private void Release()
        {
            if (_cover != null && _cover.Claimant == this)
                _cover.Claimant = null;
            _cover = null;
        }

        protected override Vector3 ChaseDestination(Vector3 targetPosition)
        {
            if (_cover == null || !_cover.isActiveAndEnabled || !_cover.Covers(targetPosition, _coverRange, _minFacing))
            {
                Release();
                FindCover(targetPosition);
            }
            return _cover != null ? _cover.Position : base.ChaseDestination(targetPosition);
        }

        protected override Vector3 CircleDestination(Vector3 targetPosition, float dt) => ChaseDestination(targetPosition);

        private void FindCover(Vector3 targetPosition)
        {
            var best = (CoverPoint)null;
            var bestDistance = _coverSearch * _coverSearch;
            var points = CoverPoint.All;
            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (point.Claimant != null && point.Claimant != this && point.Claimant.Health.IsAlive)
                    continue;
                var offset = point.Position - Position;
                offset.y = 0f;
                var distance = offset.sqrMagnitude;
                if (distance > bestDistance || !point.Covers(targetPosition, _coverRange, _minFacing))
                    continue;
                best = point;
                bestDistance = distance;
            }
            if (best == null)
                return;
            _cover = best;
            _cover.Claimant = this;
        }
    }
}

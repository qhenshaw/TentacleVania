using TentacleVania.Enemies.Types;
using UnityEngine;

namespace TentacleVania.World.Hazards
{
    public sealed class ArenaPylon : MonoBehaviour
    {
        public enum PylonState
        {
            Dormant,
            Off,
            Warning,
            On,
        }

        [Tooltip("The electrified floor this pylon powers (an Electric HazardZone, disabled while off).")]
        [SerializeField] private HazardZone _floor;
        [Tooltip("Pylon Guards this close keep the pylon powered (design: kill them to shut it down).")]
        [SerializeField, Min(1f)] private float _guardRange = 20f;
        [SerializeField, Min(0f)] private float _offTime = 3f;
        [SerializeField, Min(0.1f)] private float _warningTime = 1.2f;
        [SerializeField, Min(0.1f)] private float _onTime = 2.5f;
        [SerializeField, Min(1f)] private float _blinkRate = 8f;
        [SerializeField] private Renderer _lamp;
        [SerializeField] private Material _poweredMaterial;
        [SerializeField] private Material _deadMaterial;

        private Transform _surface;
        private float _phaseEnds;

        public PylonState State { get; private set; } = PylonState.Dormant;

        private void Awake()
        {
            if (_floor != null)
                _surface = _floor.transform.Find("Surface");
            SetState(PylonState.Dormant);
        }

        private void Update()
        {
            var powered = PylonGuardEnemy.AnyAliveNear(transform.position, _guardRange);
            var now = Time.time;
            if (!powered)
            {
                if (State != PylonState.Dormant)
                    SetState(PylonState.Dormant);
                return;
            }
            if (State == PylonState.Dormant)
            {
                SetState(PylonState.Off);
                _phaseEnds = now + _offTime;
            }
            if (now >= _phaseEnds)
            {
                var next = State switch { PylonState.Off => PylonState.Warning, PylonState.Warning => PylonState.On, _ => PylonState.Off };
                SetState(next);
                _phaseEnds = now + (next == PylonState.Warning ? _warningTime : next == PylonState.On ? _onTime : _offTime);
            }
            if (State == PylonState.Warning && _surface != null)
                _surface.gameObject.SetActive(Mathf.Repeat(now * _blinkRate, 1f) < 0.5f);
        }

        private void SetState(PylonState state)
        {
            State = state;
            if (_floor != null)
                _floor.enabled = state == PylonState.On;
            if (_surface != null)
                _surface.gameObject.SetActive(state is PylonState.On or PylonState.Warning);
            var material = state == PylonState.Dormant ? _deadMaterial : _poweredMaterial;
            if (_lamp != null && material != null)
                _lamp.sharedMaterial = material;
        }
    }
}

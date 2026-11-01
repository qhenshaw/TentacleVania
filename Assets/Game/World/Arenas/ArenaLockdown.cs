using System.Collections.Generic;
using FMODUnity;
using PrimeTween;
using UnityEngine;

namespace TentacleVania.World.Arenas
{
    [RequireComponent(typeof(ArenaEncounter))]
    public sealed class ArenaLockdown : MonoBehaviour
    {
        private const float DimmedScale = 0.4f;
        private const float DimTime = 0.4f;
        private const float RestoreTime = 1f;
        private const float KlaxonGap = 0.45f;

        [SerializeField] private EventReference _klaxon;
        [SerializeField] private EventReference _allClear;

        private static Light _sun;

        private Tween _klaxonDelay;
        private static float _sunIntensity;
        private static int _dimmers;

        private readonly List<ArenaBeacon> _beacons = new();
        private ArenaEncounter _arena;
        private ArenaState _last;
        private bool _dimming;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _sun = null;
            _dimmers = 0;
        }

        private void Awake()
        {
            _arena = GetComponent<ArenaEncounter>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
                _beacons.AddRange(root.GetComponentsInChildren<ArenaBeacon>(true));
        }

        private void OnEnable()
        {
            _arena.StateChanged += OnStateChanged;
            _last = _arena.State;
            Apply(_arena.State == ArenaState.Cleared ? BeaconState.Clear : BeaconState.Off);
        }

        private void OnDisable()
        {
            _arena.StateChanged -= OnStateChanged;
            _klaxonDelay.Stop();
            SetDim(false);
        }

        private void OnStateChanged(ArenaEncounter arena)
        {
            var state = arena.State;
            if (_last == ArenaState.Waiting && state == ArenaState.Spawning)
            {
                Apply(BeaconState.Alarm);
                SetDim(true);
                Play(_klaxon);
                Core.Characters.CreatureSignals.RaiseLockdown();
                _klaxonDelay = Tween.Delay(this, KlaxonGap, lockdown => lockdown.Play(lockdown._klaxon));
            }
            else if (state == ArenaState.Cleared && _last != ArenaState.Cleared)
            {
                Apply(BeaconState.Clear);
                SetDim(false);
                Play(_allClear);
            }
            else if (state == ArenaState.Waiting)
            {
                Apply(BeaconState.Off);
                SetDim(false);
            }
            _last = state;
        }

        private void Apply(BeaconState state)
        {
            foreach (var beacon in _beacons)
                if (beacon != null)
                    beacon.SetState(state);
        }

        private void SetDim(bool dim)
        {
            if (dim == _dimming)
                return;
            _dimming = dim;
            if (_sun == null)
            {
                _sun = RenderSettings.sun;
                if (_sun == null)
                    foreach (var light in FindObjectsByType<Light>())
                        if (light.type == LightType.Directional)
                            _sun = light;
                if (_sun == null)
                    return;
                _sunIntensity = _sun.intensity;
            }
            _dimmers = Mathf.Max(0, _dimmers + (dim ? 1 : -1));
            var target = _dimmers > 0 ? _sunIntensity * DimmedScale : _sunIntensity;
            Tween.LightIntensity(_sun, target, dim ? DimTime : RestoreTime);
        }

        private void Play(EventReference sound)
        {
            if (!sound.IsNull)
                RuntimeManager.PlayOneShot(sound, transform.position);
        }
    }
}

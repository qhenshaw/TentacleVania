using TentacleVania.Core.Scenes;
using UnityEngine;

namespace TentacleVania.World.Map
{
    [AddComponentMenu("")]
    internal sealed class FastTravelRunner : MonoBehaviour
    {
        private static FastTravelRunner _instance;

        private float _phaseStarted;

        public static void Ensure()
        {
            if (_instance != null)
                return;
            var host = new GameObject("FastTravelRunner");
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<FastTravelRunner>();
        }

        private void OnEnable() => FastTravel.PhaseChanged += OnPhaseChanged;

        private void OnDisable() => FastTravel.PhaseChanged -= OnPhaseChanged;

        private void OnPhaseChanged(TravelPhase phase) => _phaseStarted = Time.unscaledTime;

        private void Update()
        {
            var elapsed = Time.unscaledTime - _phaseStarted;
            switch (FastTravel.Phase)
            {
                case TravelPhase.Covering:
                    FastTravel.Cover = Mathf.Clamp01(elapsed / FastTravel.CoverTime);
                    if (elapsed >= FastTravel.CoverTime)
                    {
                        Time.timeScale = 1f;
                        SceneFlow.Load(FastTravel.PendingScene);
                        FastTravel.SetPhase(TravelPhase.Loading);
                    }
                    break;
                case TravelPhase.Loading:
                    FastTravel.Cover = 1f;
                    if (elapsed > 0.2f && !SceneFlow.IsLoading && LoadGate.IsOpen)
                        FastTravel.SetPhase(TravelPhase.Revealing);
                    break;
                case TravelPhase.Revealing:
                    FastTravel.Cover = 1f - Mathf.Clamp01(elapsed / FastTravel.RevealTime);
                    if (elapsed >= FastTravel.RevealTime)
                        FastTravel.SetPhase(TravelPhase.Idle);
                    break;
            }
        }
    }
}

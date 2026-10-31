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

        private static void Teleport(Vector3 position, float yaw)
        {
            var creature = Combat.CombatTargets.Creature;
            if (creature == null)
                return;
            var controller = creature.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;
            creature.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            if (controller != null)
                controller.enabled = true;
        }

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
                        if (FastTravel.Kind == TravelKind.Crawl)
                            Streaming.RoomStreamer.FocusOverride = FastTravel.CrawlTarget;
                        else
                            SceneFlow.Load(FastTravel.PendingScene);
                        FastTravel.SetPhase(TravelPhase.Loading);
                    }
                    break;
                case TravelPhase.Loading:
                    FastTravel.Cover = 1f;
                    if (elapsed > 0.6f && !SceneFlow.IsLoading && LoadGate.IsOpen && (Streaming.RoomStreamer.Active == null || Streaming.RoomStreamer.Active.InFlight == 0))
                    {
                        if (FastTravel.Kind == TravelKind.Crawl)
                        {
                            Teleport(FastTravel.CrawlTarget, FastTravel.CrawlYaw);
                            Streaming.RoomStreamer.FocusOverride = null;
                        }
                        FastTravel.SetPhase(TravelPhase.Revealing);
                    }
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

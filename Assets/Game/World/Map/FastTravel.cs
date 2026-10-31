using System;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using TentacleVania.Core.Scenes;
using UnityEngine;

namespace TentacleVania.World.Map
{
    public enum TravelPhase
    {
        Idle,
        Covering,
        Loading,
        Revealing,
    }

    public static class FastTravel
    {
        public const float CoverTime = 0.6f;
        public const float RevealTime = 0.8f;

        public static event Action<TravelPhase> PhaseChanged;

        public static TravelPhase Phase { get; private set; }
        public static float Cover { get; internal set; }
        public static string Destination { get; private set; }

        internal static string PendingScene { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            PhaseChanged = null;
            Phase = TravelPhase.Idle;
            Cover = 0f;
            Destination = null;
            PendingScene = null;
        }

        public static bool CanTravelTo(string nestId) =>
            Phase == TravelPhase.Idle && Nests.IsGrown(nestId) && MapAtlas.Instance != null && MapAtlas.Instance.TryFindNest(nestId, out var registry, out _) && !string.IsNullOrEmpty(registry.RootScene);

        public static bool Travel(string nestId)
        {
            if (!CanTravelTo(nestId) || SceneFlow.IsLoading)
                return false;
            MapAtlas.Instance.TryFindNest(nestId, out var registry, out var marker);
            if (!Nests.SetRespawn(nestId, registry.RootScene, marker.Position, marker.Yaw))
                return false;
            Destination = nestId;
            PendingScene = registry.RootScene;
            FastTravelRunner.Ensure();
            SetPhase(TravelPhase.Covering);
            return true;
        }

        internal static void SetPhase(TravelPhase phase)
        {
            Phase = phase;
            if (phase == TravelPhase.Idle)
            {
                Destination = null;
                PendingScene = null;
                Autosave.Request(AutosaveReason.NestRest);
            }
            PhaseChanged?.Invoke(phase);
        }
    }
}

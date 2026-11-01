using System;
using TentacleVania.Core;
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

    public enum TravelKind
    {
        Nest,
        Elevator,
        Crawl,
        Tunnel,
        Recover,
    }

    public static class FastTravel
    {
        public const float CoverTime = 0.6f;
        public const float RevealTime = 0.8f;

        public static event Action<TravelPhase> PhaseChanged;

        public static TravelPhase Phase { get; private set; }
        public static float Cover { get; internal set; }
        public static string Destination { get; private set; }
        public static TravelKind Kind { get; private set; }

        private static readonly object CutsceneOwner = new();

        internal static string PendingScene { get; private set; }
        internal static Vector3 CrawlTarget { get; private set; }
        internal static float CrawlYaw { get; private set; }
        internal static readonly System.Collections.Generic.List<Vector3> TunnelPath = new(16);
        public const float TunnelSpeed = 40f;
        public const float TunnelDepth = -3f;

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
            Begin(TravelKind.Nest, registry.RootScene);
            return true;
        }

        public static bool Transit(string scene, Vector3 arrival, float yaw)
        {
            if (Phase != TravelPhase.Idle || SceneFlow.IsLoading || string.IsNullOrEmpty(scene))
                return false;
            SpawnOverride.Set(scene, arrival, yaw);
            Destination = scene;
            Begin(TravelKind.Elevator, scene);
            return true;
        }

        public static bool Crawl(Vector3 target, float yaw) => Teleport(target, yaw, TravelKind.Crawl);

        public static bool Recover(Vector3 target, float yaw) => Teleport(target, yaw, TravelKind.Recover);

        private static bool Teleport(Vector3 target, float yaw, TravelKind kind)
        {
            if (Phase != TravelPhase.Idle || SceneFlow.IsLoading)
                return false;
            CrawlTarget = target;
            CrawlYaw = yaw;
            Destination = null;
            Begin(kind, null);
            return true;
        }

        public static bool Tunnel(System.Collections.Generic.IReadOnlyList<Vector3> path, Vector3 exit, float yaw)
        {
            if (Phase != TravelPhase.Idle || SceneFlow.IsLoading || path == null || path.Count == 0)
                return false;
            TunnelPath.Clear();
            for (var i = 0; i < path.Count; i++)
                TunnelPath.Add(new Vector3(path[i].x, TunnelDepth, path[i].z));
            TunnelPath.Add(new Vector3(exit.x, TunnelDepth, exit.z));
            CrawlTarget = exit;
            CrawlYaw = yaw;
            Destination = null;
            Begin(TravelKind.Tunnel, null);
            return true;
        }

        private static void Begin(TravelKind kind, string scene)
        {
            Kind = kind;
            PendingScene = scene;
            FastTravelRunner.Ensure();
            SetPhase(TravelPhase.Covering);
        }

        internal static void SetPhase(TravelPhase phase)
        {
            Phase = phase;
            if (phase == TravelPhase.Covering && Kind != TravelKind.Recover)
                Cutscene.Begin(CutsceneOwner);
            if (phase == TravelPhase.Idle)
            {
                Cutscene.End(CutsceneOwner);
                Destination = null;
                PendingScene = null;
                Autosave.Request(AutosaveReason.NestRest);
            }
            PhaseChanged?.Invoke(phase);
        }
    }
}

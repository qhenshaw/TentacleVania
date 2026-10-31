using TentacleVania.Core.Characters;
using TentacleVania.Core.Scenes;
using UnityEngine;

namespace TentacleVania.World.Map
{
    [AddComponentMenu("")]
    internal sealed class FastTravelRunner : MonoBehaviour
    {
        private const float TunnelCoverTime = 0.25f;
        private const float TunnelRevealTime = 0.3f;
        private const string TrailMaterialPath = "TunnelTrail";

        private static FastTravelRunner _instance;

        private float _phaseStarted;
        private int _segment;
        private TrailRenderer _trail;

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

        private static Transform Creature => Combat.CombatTargets.Creature != null ? Combat.CombatTargets.Creature.transform : null;

        private static void Teleport(Vector3 position, float yaw)
        {
            var creature = Creature;
            if (creature == null)
                return;
            var controller = creature.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;
            creature.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            if (controller != null)
                controller.enabled = true;
        }

        private void OnPhaseChanged(TravelPhase phase) => _phaseStarted = Time.unscaledTime;

        private void Update()
        {
            var elapsed = Time.unscaledTime - _phaseStarted;
            var tunnel = FastTravel.Kind == TravelKind.Tunnel;
            switch (FastTravel.Phase)
            {
                case TravelPhase.Covering:
                {
                    var coverTime = tunnel ? TunnelCoverTime : FastTravel.CoverTime;
                    FastTravel.Cover = Mathf.Clamp01(elapsed / coverTime);
                    if (elapsed < coverTime)
                        break;
                    Time.timeScale = 1f;
                    if (tunnel)
                        EnterTunnel();
                    else if (FastTravel.Kind is TravelKind.Crawl or TravelKind.Recover)
                        Streaming.RoomStreamer.FocusOverride = FastTravel.CrawlTarget;
                    else
                        SceneFlow.Load(FastTravel.PendingScene);
                    FastTravel.SetPhase(TravelPhase.Loading);
                    break;
                }
                case TravelPhase.Loading:
                    FastTravel.Cover = 1f;
                    if (tunnel)
                    {
                        if (TickTunnel(Time.unscaledDeltaTime) && StreamerIdle())
                        {
                            ExitTunnel();
                            FastTravel.SetPhase(TravelPhase.Revealing);
                        }
                        break;
                    }
                    if (elapsed > 0.6f && !SceneFlow.IsLoading && LoadGate.IsOpen && StreamerIdle())
                    {
                        if (FastTravel.Kind is TravelKind.Crawl or TravelKind.Recover)
                        {
                            Teleport(FastTravel.CrawlTarget, FastTravel.CrawlYaw);
                            Streaming.RoomStreamer.FocusOverride = null;
                        }
                        FastTravel.SetPhase(TravelPhase.Revealing);
                    }
                    break;
                case TravelPhase.Revealing:
                {
                    var revealTime = tunnel ? TunnelRevealTime : FastTravel.RevealTime;
                    FastTravel.Cover = 1f - Mathf.Clamp01(elapsed / revealTime);
                    if (elapsed >= revealTime)
                        FastTravel.SetPhase(TravelPhase.Idle);
                    break;
                }
            }
        }

        private static bool StreamerIdle() => Streaming.RoomStreamer.Active == null || Streaming.RoomStreamer.Active.InFlight == 0;

        private void EnterTunnel()
        {
            var creature = Creature;
            if (creature == null)
                return;
            if (creature.TryGetComponent(out ICreatureTransit transit))
                transit.BeginTransit();
            creature.position = FastTravel.TunnelPath[0];
            _segment = 1;
            EnsureTrail();
            _trail.transform.position = SurfacePoint(creature.position);
            _trail.Clear();
            _trail.emitting = true;
        }

        private bool TickTunnel(float dt)
        {
            var creature = Creature;
            if (creature == null)
                return true;
            var path = FastTravel.TunnelPath;
            var step = FastTravel.TunnelSpeed * dt;
            while (step > 0f && _segment < path.Count)
            {
                var target = path[_segment];
                var offset = target - creature.position;
                var distance = offset.magnitude;
                if (distance <= step)
                {
                    creature.position = target;
                    step -= distance;
                    _segment++;
                    continue;
                }
                creature.position += offset / distance * step;
                var flat = new Vector3(offset.x, 0f, offset.z);
                if (flat.sqrMagnitude > 1e-6f)
                    creature.rotation = Quaternion.LookRotation(flat);
                step = 0f;
            }
            if (_trail != null)
                _trail.transform.position = SurfacePoint(creature.position);
            return _segment >= path.Count;
        }

        private void ExitTunnel()
        {
            var creature = Creature;
            if (_trail != null)
                _trail.emitting = false;
            if (creature == null)
                return;
            creature.SetPositionAndRotation(FastTravel.CrawlTarget, Quaternion.Euler(0f, FastTravel.CrawlYaw, 0f));
            if (creature.TryGetComponent(out ICreatureTransit transit))
                transit.EndTransit();
        }

        private static Vector3 SurfacePoint(Vector3 point) => new(point.x, 0.08f, point.z);

        private void EnsureTrail()
        {
            if (_trail != null)
                return;
            var go = new GameObject("TunnelTrail");
            go.transform.SetParent(transform, false);
            _trail = go.AddComponent<TrailRenderer>();
            _trail.time = 0.8f;
            _trail.minVertexDistance = 0.4f;
            _trail.widthMultiplier = 0.9f;
            _trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            _trail.alignment = LineAlignment.View;
            _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _trail.sharedMaterial = Resources.Load<Material>(TrailMaterialPath);
        }
    }
}

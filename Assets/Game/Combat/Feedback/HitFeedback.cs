using TentacleVania.Combat.Damage;
using TentacleVania.Core.Feedback;
using UnityEngine;

namespace TentacleVania.Combat.Feedback
{
    [RequireComponent(typeof(Health))]
    public sealed class HitFeedback : MonoBehaviour
    {
        [SerializeField] private HitFeedbackSettings _settings;

        private Health _self;
        private HitFeedbackKind _pending;
        private Vector3 _pendingPoint;

        public static HitFeedbackSettings Settings { get; private set; }
        public static HitFeedbackKind LastKind { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Settings = null;
            LastKind = HitFeedbackKind.None;
        }

        public static HitFeedbackKind ClassifyForPlayer(in DamageInfo info, Health target)
        {
            var player = CombatTargets.Creature;
            var attackerIsPlayer = player != null && info.Attacker == player;
            var targetIsPlayer = player != null && target == player;
            return HitFeedbackRules.Classify(info.Flags, attackerIsPlayer, targetIsPlayer);
        }

        private void Awake() => _self = GetComponent<Health>();

        private void OnEnable()
        {
            Settings = _settings;
            CombatEvents.HitLanded += OnHit;
            CombatEvents.StaggerBroken += OnStaggerBroken;
        }

        private void OnDisable()
        {
            CombatEvents.HitLanded -= OnHit;
            CombatEvents.StaggerBroken -= OnStaggerBroken;
            if (Settings == _settings)
                Settings = null;
        }

        private void OnHit(Health target, in DamageInfo info, in DamageResult result)
        {
            if (result.Outcome != DamageOutcome.Applied)
                return;
            var attackerIsSelf = info.Attacker != null && info.Attacker == _self;
            var kind = HitFeedbackRules.Classify(info.Flags, attackerIsSelf, target == _self);
            Queue(kind, info.Point.sqrMagnitude > 0f ? info.Point : target.transform.position);
        }

        private void OnStaggerBroken(Health target) => Queue(HitFeedbackKind.StaggerBreak, target.transform.position);

        private void Queue(HitFeedbackKind kind, Vector3 point)
        {
            if (!HitFeedbackRules.Outranks(kind, _pending))
                return;
            _pending = kind;
            _pendingPoint = point;
        }

        private void LateUpdate()
        {
            if (_pending == HitFeedbackKind.None || _settings == null)
                return;
            var level = _settings.Level(_pending);
            HitStop.Request(level.HitStop);
            ScreenShake.Request(_pendingPoint, level.Shake);
            Rumble.Play(level.RumbleLow, level.RumbleHigh, level.RumbleDuration);
            LastKind = _pending;
            _pending = HitFeedbackKind.None;
        }
    }
}

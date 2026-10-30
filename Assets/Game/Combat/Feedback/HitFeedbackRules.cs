using TentacleVania.Combat.Damage;

namespace TentacleVania.Combat.Feedback
{
    public static class HitFeedbackRules
    {
        public static HitFeedbackKind Classify(HitFlags flags, bool attackerIsPlayer, bool targetIsPlayer)
        {
            if (targetIsPlayer)
                return HitFeedbackKind.CreatureHurt;
            if (!attackerIsPlayer || (flags & HitFlags.Hazard) != 0)
                return HitFeedbackKind.None;
            if ((flags & HitFlags.Heavy) != 0)
                return HitFeedbackKind.Heavy;
            if ((flags & HitFlags.Finisher) != 0)
                return HitFeedbackKind.Finisher;
            if ((flags & HitFlags.DashAttack) != 0)
                return HitFeedbackKind.DashAttack;
            return HitFeedbackKind.Light;
        }

        public static bool Outranks(HitFeedbackKind candidate, HitFeedbackKind current) => Rank(candidate) > Rank(current);

        private static int Rank(HitFeedbackKind kind) => kind switch
        {
            HitFeedbackKind.StaggerBreak => 6,
            HitFeedbackKind.Heavy => 5,
            HitFeedbackKind.Finisher => 4,
            HitFeedbackKind.CreatureHurt => 3,
            HitFeedbackKind.DashAttack => 2,
            HitFeedbackKind.Light => 1,
            _ => 0,
        };
    }
}

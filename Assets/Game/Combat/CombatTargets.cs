using UnityEngine;

namespace TentacleVania.Combat
{
    public static class CombatTargets
    {
        public static Health Creature { get; private set; }
        public static float CreatureAimHeight { get; private set; }

        public static Vector3 CreatureAimPoint => Creature != null ? Creature.transform.position + Vector3.up * CreatureAimHeight : Vector3.zero;

        public static void RegisterCreature(Health health, float aimHeight)
        {
            Creature = health;
            CreatureAimHeight = aimHeight;
        }

        public static void UnregisterCreature(Health health)
        {
            if (Creature == health)
                Creature = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Creature = null;
    }
}

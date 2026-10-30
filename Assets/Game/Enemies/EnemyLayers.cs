using UnityEngine;

namespace TentacleVania.Enemies
{
    public static class EnemyLayers
    {
        public static readonly int Enemy = LayerMask.NameToLayer("Enemy");
        public static readonly int Creature = LayerMask.NameToLayer("Creature");
        public static readonly int HurtboxMask = (1 << Enemy) | (1 << Creature);
    }
}

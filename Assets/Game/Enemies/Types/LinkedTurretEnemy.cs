using TentacleVania.Enemies.AI;
using UnityEngine;

namespace TentacleVania.Enemies.Types
{
    public sealed class LinkedTurretEnemy : TurretEnemy
    {
        [Header("Console link (design: Overload or destroying the console disables them)")]
        [SerializeField, Min(1f)] private float _linkRange = 20f;

        public bool Powered => TurretConsole.Powers(Position, _linkRange);

        protected override bool AttackAllowed(int index) => Powered && base.AttackAllowed(index);
    }
}

using System.Collections.Generic;
using TentacleVania.Enemies.Data;

namespace TentacleVania.Enemies.AI
{
    public sealed class AttackTokens
    {
        private readonly List<object> _melee = new(4);
        private readonly List<object> _ranged = new(4);

        public AttackTokens(int melee, int ranged)
        {
            MeleeCapacity = melee;
            RangedCapacity = ranged;
        }

        public int MeleeCapacity { get; set; }
        public int RangedCapacity { get; set; }
        public int MeleeHeld => _melee.Count;
        public int RangedHeld => _ranged.Count;

        public bool TryAcquire(object holder, AttackTokenKind kind)
        {
            var list = kind == AttackTokenKind.Melee ? _melee : _ranged;
            if (list.Contains(holder))
                return true;
            if (list.Count >= (kind == AttackTokenKind.Melee ? MeleeCapacity : RangedCapacity))
                return false;
            list.Add(holder);
            return true;
        }

        public bool Holds(object holder) => _melee.Contains(holder) || _ranged.Contains(holder);

        public void Release(object holder)
        {
            _melee.Remove(holder);
            _ranged.Remove(holder);
        }

        public void Clear()
        {
            _melee.Clear();
            _ranged.Clear();
        }
    }
}

using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Combat.Status
{
    public sealed class AcidPoolSlots
    {
        public struct Slot
        {
            public bool Active;
            public Vector3 Position;
            public float Radius;
            public float StartedAt;
            public float EndsAt;
            public float NextTick;
            public float DamagePerSecond;
            public Team Team;
        }

        private readonly Slot[] _slots;

        public AcidPoolSlots(int count) => _slots = new Slot[Mathf.Max(1, count)];

        public int Count => _slots.Length;

        public ref Slot this[int index] => ref _slots[index];

        public int ActiveCount
        {
            get
            {
                var count = 0;
                foreach (var slot in _slots)
                    if (slot.Active)
                        count++;
                return count;
            }
        }

        public int Spawn(Vector3 position, float radius, float duration, float damagePerSecond, Team team, float now)
        {
            var index = 0;
            for (var i = 0; i < _slots.Length; i++)
            {
                if (!_slots[i].Active)
                {
                    index = i;
                    break;
                }
                if (_slots[i].EndsAt < _slots[index].EndsAt)
                    index = i;
            }
            _slots[index] = new Slot
            {
                Active = true,
                Position = position,
                Radius = radius,
                StartedAt = now,
                EndsAt = now + duration,
                NextTick = now,
                DamagePerSecond = damagePerSecond,
                Team = team,
            };
            return index;
        }

        public bool TickDue(int index, float now, float interval)
        {
            ref var slot = ref _slots[index];
            if (!slot.Active || now < slot.NextTick || now >= slot.EndsAt)
                return false;
            slot.NextTick = Mathf.Max(slot.NextTick + interval, now);
            return true;
        }

        public bool Expire(int index, float now)
        {
            ref var slot = ref _slots[index];
            if (!slot.Active || now < slot.EndsAt)
                return false;
            slot.Active = false;
            return true;
        }

        public void Clear()
        {
            for (var i = 0; i < _slots.Length; i++)
                _slots[i].Active = false;
        }
    }
}

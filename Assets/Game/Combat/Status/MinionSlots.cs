using TentacleVania.Combat.Damage;
using UnityEngine;

namespace TentacleVania.Combat.Status
{
    public sealed class MinionSlots
    {
        public struct Slot
        {
            public bool Active;
            public Team Team;
            public float SpawnedAt;
            public float EndsAt;
            public float NextBite;
        }

        private readonly Slot[] _slots;

        public MinionSlots(int count) => _slots = new Slot[Mathf.Max(1, count)];

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

        public int Spawn(Team team, float now, float lifetime)
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
            _slots[index] = new Slot { Active = true, Team = team, SpawnedAt = now, EndsAt = now + lifetime, NextBite = now };
            return index;
        }

        public bool BiteDue(int index, float now, float interval)
        {
            ref var slot = ref _slots[index];
            if (!slot.Active || now < slot.NextBite)
                return false;
            slot.NextBite = now + interval;
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

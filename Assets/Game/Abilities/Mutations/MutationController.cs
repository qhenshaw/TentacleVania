using System.Collections.Generic;
using TentacleVania.Abilities.Grapple;
using TentacleVania.Core.Progression;
using TentacleVania.Core.Saves;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using UnityEngine;

namespace TentacleVania.Abilities.Mutations
{
    [RequireComponent(typeof(CreatureMotor), typeof(CreatureConsume))]
    public sealed class MutationController : MonoBehaviour
    {
        [SerializeField] private ContentCatalog _catalog;

        private readonly List<MutationEffect> _active = new(8);
        private MutationHost _host;

        public int ActiveCount => _active.Count;

        private void Awake() => _host = new(GetComponent<CreatureMotor>(), GetComponent<CreatureConsume>(), GetComponent<TentacleGrapple>());

        private void OnEnable()
        {
            MutationLoadout.Changed += Rebuild;
            SaveSystem.Loaded += OnLoaded;
            Rebuild();
        }

        private void OnDisable()
        {
            MutationLoadout.Changed -= Rebuild;
            SaveSystem.Loaded -= OnLoaded;
            Clear();
        }

        private void Update()
        {
            var now = Time.time;
            for (var i = 0; i < _active.Count; i++)
                _active[i].Tick(_host, now);
        }

        public MutationDefinition Find(string id) => _catalog != null ? _catalog.Find<MutationDefinition>(id) : null;

        public int UsedNotches()
        {
            var used = 0;
            foreach (var id in MutationLoadout.Equipped)
            {
                var definition = Find(id);
                if (definition != null)
                    used += definition.Notches;
            }
            return used;
        }

        public bool Equip(string id)
        {
            var definition = Find(id);
            return definition != null && MutationLoadout.Equip(id, definition.Notches, UsedNotches());
        }

        private void OnLoaded(SaveData data) => Rebuild();

        private void Rebuild()
        {
            Clear();
            foreach (var id in MutationLoadout.Equipped)
            {
                var definition = Find(id);
                if (definition == null)
                    continue;
                var effect = definition.CreateEffect();
                effect.Enable(_host);
                _active.Add(effect);
            }
        }

        private void Clear()
        {
            for (var i = _active.Count - 1; i >= 0; i--)
                _active[i].Disable(_host);
            _active.Clear();
        }
    }
}

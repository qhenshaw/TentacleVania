using System;
using System.Collections.Generic;
using TentacleVania.Combat;
using TentacleVania.Core.Input;
using TentacleVania.Core.Interaction;
using TentacleVania.Core.Progression;
using TentacleVania.Creature.Movement;
using UnityEngine;

namespace TentacleVania.Abilities
{
    [DefaultExecutionOrder(-40)]
    [RequireComponent(typeof(CreatureMotor))]
    public sealed class CreatureTraversal : MonoBehaviour
    {
        private static readonly HashSet<string> Forced = new();

        private CreatureMotor _motor;
        private Health _health;
        private Func<string, bool> _unlocked;

        public static int Used { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Forced.Clear();
            Used = 0;
        }

        public static bool IsForced(string abilityId) => Forced.Contains(abilityId);

        public static void SetForced(string abilityId, bool forced)
        {
            if (forced)
                Forced.Add(abilityId);
            else
                Forced.Remove(abilityId);
        }

        public static bool IsUnlocked(string abilityId) =>
            string.IsNullOrEmpty(abilityId) || Forced.Contains(abilityId) || BossProgress.IsAbilityUnlocked(abilityId);

        private void Awake()
        {
            _motor = GetComponent<CreatureMotor>();
            _health = GetComponent<Health>();
            _unlocked = IsUnlocked;
        }

        private void Update()
        {
            if (!GameplayInput.Default.Traverse.Pressed || Traversals.ClaimedThisFrame || !_motor.InputEnabled)
                return;
            if (_health != null && !_health.IsAlive)
                return;
            if (TryTraverse())
                Traversals.Claim();
        }

        public ITraversal Available() => Traversals.Nearest(transform.position, _unlocked);

        public bool TryTraverse()
        {
            var traversal = Available();
            if (traversal == null)
                return false;
            traversal.Traverse();
            Used++;
            return true;
        }
    }
}

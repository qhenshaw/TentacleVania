using TentacleVania.Combat;
using TentacleVania.Core.Debugging;
using TentacleVania.Core.Progression;
using TentacleVania.World.Arenas;
using TentacleVania.World.BioNests;
using TentacleVania.World.Security;
using TentacleVania.World.Shortcuts;
using TentacleVania.Core.Saves;
using TentacleVania.World.Streaming;
using UnityEngine;

namespace TentacleVania.World
{
    public static class WorldDebugCommands
    {
        public const string Tab = "World";

        private static void SpawnHazard(Hazards.HazardKind kind)
        {
            var creature = Combat.CombatTargets.Creature;
            if (creature == null)
                return;
            var t = creature.transform;
            var host = new GameObject("DebugHazard_" + kind);
            host.transform.SetPositionAndRotation(t.position + t.forward * 4f, Quaternion.LookRotation(t.forward));
            var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(surface.GetComponent<Collider>());
            surface.transform.SetParent(host.transform, false);
            surface.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            surface.transform.localScale = new Vector3(4f, 0.04f, 4f);
            host.AddComponent<Hazards.HazardZone>().Configure(kind, new Vector3(4f, 2f, 4f), surface.transform);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            DebugMenu.Value(Tab, "Opening", () => Opening.OpeningSequence.Playing
                ? $"playing {Opening.OpeningSequence.Current.Elapsed:F1} s"
                : Opening.OpeningSequence.Seen ? "seen" : "not seen");
            DebugMenu.Value(Tab, "Fall recoveries", () => $"creature {Map.FallRecovery.CreatureRecoveries}, enemies {Map.FallRecovery.EnemyRecoveries}, safe {Map.FallRecovery.SafePoint:F1}");
            foreach (var kind in new[] { Hazards.HazardKind.Electric, Hazards.HazardKind.Fire, Hazards.HazardKind.Acid, Hazards.HazardKind.Radiation })
            {
                var resistance = Hazards.HazardRules.ResistanceFor(kind);
                DebugMenu.Toggle(Tab, resistance + " (debug)", () => Hazards.HazardRules.IsForced(resistance), v => Hazards.HazardRules.SetForced(resistance, v));
            }
            foreach (var kind in (Hazards.HazardKind[])System.Enum.GetValues(typeof(Hazards.HazardKind)))
                DebugMenu.Button(Tab, "Spawn " + kind + " hazard ahead", () => SpawnHazard(kind));
            DebugMenu.Button(Tab, "Drop creature out of the map", () =>
            {
                var creature = CombatTargets.Creature;
                if (creature == null)
                    return;
                var controller = creature.GetComponent<CharacterController>();
                if (controller != null)
                    controller.enabled = false;
                creature.transform.position += new Vector3(0f, -12f, 0f);
                if (controller != null)
                    controller.enabled = true;
            });
            DebugMenu.Button(Tab, "Flare nearest nest tentacles", () =>
            {
                var creature = CombatTargets.Creature;
                BioNest best = null;
                var bestDistance = float.MaxValue;
                foreach (var nest in Object.FindObjectsByType<BioNest>())
                {
                    var distance = creature != null ? (nest.transform.position - creature.transform.position).sqrMagnitude : 0f;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = nest;
                    }
                }
                var tentacles = best != null ? best.GetComponentInChildren<Core.Characters.INestTentacles>(true) : null;
                if (tentacles != null)
                    tentacles.Flare();
            });
            DebugMenu.Button(Tab, "Replay opening", () =>
            {
                if (Opening.OpeningSequence.Current != null)
                    Opening.OpeningSequence.Current.Replay();
            });
            DebugMenu.Button(Tab, "Skip opening", () =>
            {
                if (Opening.OpeningSequence.Current != null)
                    Opening.OpeningSequence.Current.Skip();
            });
            DebugMenu.Value(Tab, "Arena", () =>
            {
                var arena = ArenaEncounter.Active;
                if (arena == null)
                    return "none";
                var elapsed = arena.State == ArenaState.Cleared ? arena.ClearedAt - arena.StartedAt : Time.time - arena.StartedAt;
                return arena.State == ArenaState.Waiting
                    ? "waiting for the creature"
                    : $"{arena.State}, wave {arena.WaveNumber}/{arena.WaveCount}, {arena.AliveCount} alive, {elapsed:F0} s";
            });
            DebugMenu.Value(Tab, "Last nest", () => string.IsNullOrEmpty(Nests.LastNest) ? "none" : $"{Nests.LastNest} ({Nests.RespawnScene})");
            DebugMenu.Button(Tab, "Grow all nests", () =>
            {
                foreach (var nest in Object.FindObjectsByType<BioNest>())
                    nest.Grow();
            });
            DebugMenu.Button(Tab, "Rest at nearest nest", () =>
            {
                var nest = NearestNest();
                if (nest == null)
                    return;
                nest.Grow();
                nest.Rest();
            });
            DebugMenu.Button(Tab, "Teleport to nearest nest", () =>
            {
                var nest = NearestNest();
                var creature = CombatTargets.Creature;
                if (nest == null || creature == null)
                    return;
                var controller = creature.GetComponent<CharacterController>();
                if (controller != null)
                    controller.enabled = false;
                creature.transform.SetPositionAndRotation(nest.RespawnPoint.position, nest.RespawnPoint.rotation);
                if (controller != null)
                    controller.enabled = true;
            });
            DebugMenu.Button(Tab, "Start arena now", () =>
            {
                var arena = ArenaEncounter.Active;
                if (arena != null && arena.State == ArenaState.Waiting)
                    arena.Begin(Time.time);
            });
            DebugMenu.Button(Tab, "Skip wave (kill all)", () =>
            {
                var arena = ArenaEncounter.Active;
                if (arena != null)
                    arena.SkipWave();
            });
            DebugMenu.Value(Tab, "Security", DescribeSecurity);
            DebugMenu.Button(Tab, "Spawn security test rig", SpawnSecurityRig);
            DebugMenu.Button(Tab, "Destroy nearest relay", () =>
            {
                var relay = NearestRelay();
                if (relay != null)
                    relay.DestroyRelay();
            });
            DebugMenu.Button(Tab, "Reset relays", ResetRelays);
            DebugMenu.Value(Tab, "Shortcuts open", () => SaveSystem.Data.World.OpenedShortcuts.Count.ToString());
            DebugMenu.Button(Tab, "Open all shortcuts", () =>
            {
                foreach (var shortcut in Object.FindObjectsByType<Shortcut>())
                    shortcut.Open();
            });
            DebugMenu.Button(Tab, "Close all shortcuts", () =>
            {
                ShortcutProgress.CloseAll();
                foreach (var shortcut in Object.FindObjectsByType<Shortcut>())
                    shortcut.ResetClosed();
            });
            DebugMenu.Button(Tab, "Spawn shortcut test rig", () => SpawnRig(ShortcutRigResource));
            DebugMenu.Value(Tab, "Rooms", DescribeRooms);
            DebugMenu.Toggle(Tab, "Pause room streaming", () => RoomStreamer.Paused, v => RoomStreamer.Paused = v);
            DebugMenu.Button(Tab, "Load all rooms", () =>
            {
                if (RoomStreamer.Active != null)
                    RoomStreamer.Active.LoadAll();
            });
            DebugMenu.Button(Tab, "Unload distant rooms", () =>
            {
                if (RoomStreamer.Active != null)
                    RoomStreamer.Active.UnloadDistant();
            });
        }

        private static string DescribeRooms()
        {
            var streamer = RoomStreamer.Active;
            if (streamer == null)
                return "no room streamer";
            var rooms = streamer.Registry.Rooms;
            var text = new System.Text.StringBuilder();
            text.Append("in ").Append(RoomStreamer.CurrentScene ?? "none").Append(", in flight ").Append(streamer.InFlight);
            for (var i = 0; i < rooms.Count; i++)
                text.Append('\n').Append(rooms[i].Scene).Append(": ").Append(streamer.State(i));
            return text.ToString();
        }

        public const string SecurityRigResource = "DebugSecurityRig";

        public const string ShortcutRigResource = "DebugShortcutRig";

        public static void SpawnSecurityRig() => SpawnRig(SecurityRigResource);

        public static void SpawnRig(string resource)
        {
            var prefab = Resources.Load<GameObject>(resource);
            var creature = CombatTargets.Creature;
            if (prefab == null || creature == null)
                return;
            var forward = creature.transform.forward;
            forward.y = 0f;
            Object.Instantiate(prefab, creature.transform.position + forward.normalized * 4f, Quaternion.LookRotation(forward.normalized));
        }

        public static void ResetRelays()
        {
            foreach (var door in Object.FindObjectsByType<SecurityDoor>())
                SecurityRelays.Reset(door.System);
            foreach (var relay in SecurityRelay.All)
            {
                SecurityRelays.Reset(relay.System);
                relay.Restore();
            }
            foreach (var door in Object.FindObjectsByType<SecurityDoor>())
                door.Refresh(false);
        }

        private static string DescribeSecurity()
        {
            var text = "";
            foreach (var door in Object.FindObjectsByType<SecurityDoor>())
                text += (text.Length > 0 ? "\n" : "") + $"{door.System}: {SecurityRelays.Count(door.System)}/{door.Required} relays, {door.Power}";
            return text.Length > 0 ? text : "no security door";
        }

        private static SecurityRelay NearestRelay()
        {
            var creature = CombatTargets.Creature;
            var origin = creature != null ? creature.transform.position : Vector3.zero;
            SecurityRelay best = null;
            var bestDistance = float.MaxValue;
            foreach (var relay in SecurityRelay.All)
            {
                var distance = (relay.transform.position - origin).sqrMagnitude;
                if (!relay.IsDestroyed && distance < bestDistance)
                {
                    best = relay;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static BioNest NearestNest()
        {
            var creature = CombatTargets.Creature;
            var origin = creature != null ? creature.transform.position : Vector3.zero;
            BioNest best = null;
            var bestDistance = float.MaxValue;
            foreach (var nest in Object.FindObjectsByType<BioNest>())
            {
                var distance = (nest.transform.position - origin).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = nest;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}

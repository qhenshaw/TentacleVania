using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TentacleVania.World.Editor
{
    public static class SectorHooks
    {
        private static readonly Dictionary<string, Action<RoomContext>> Hooks = new()
        {
            ["SH.Opening"] = Opening,
            ["SH.WardenHall"] = WardenHall,
            ["SH.GrappleGate"] = GrappleGate,
            ["BL.AutomatonTheatre"] = AutomatonTheatre,
            ["RD.TestChamber"] = TestChamber,
        };

        public static bool TryGet(string name, out Action<RoomContext> hook) => Hooks.TryGetValue(name ?? "", out hook);

        private static void Opening(RoomContext c)
        {
            var pod = c.Spawn("Assets/Game/World/Opening/CreaturePod.prefab", RoomContext.CellCenter(3f, 38.5f), 180f, "CreaturePod");
            c.Spawn("Assets/Game/World/Opening/SpecimenPod.prefab", RoomContext.CellCenter(1.9f, 38.5f), 180f, "SpecimenPod_W");
            c.Spawn("Assets/Game/World/Opening/SpecimenPod.prefab", RoomContext.CellCenter(4.1f, 38.5f), 180f, "SpecimenPod_E");
            var consoleW = c.Spawn("Assets/Game/World/Opening/LabConsole.prefab", RoomContext.CellCenter(2.2f, 40.1f), 0f, "LabConsole_W");
            var consoleE = c.Spawn("Assets/Game/World/Opening/LabConsole.prefab", RoomContext.CellCenter(4.3f, 40.1f), 0f, "LabConsole_E");
            RoomContext.Set(pod.GetComponent<Opening.OpeningSequence>(), "_workSpots", new UnityEngine.Object[] { consoleW.transform.Find("WorkSpot"), consoleE.transform.Find("WorkSpot") });
        }

        private static void WardenHall(RoomContext c)
        {
            var hall = c.AddByName(c.Empty("WardenHall", RoomContext.Cell(39f, 3f), 0f), "TentacleVania.Bosses.Warden.WardenHall");
            RoomContext.Set(hall, "_size", new Vector2(24f, 18f));
        }

        private static void GrappleGate(RoomContext c)
        {
            var z = 15.5f * SectorBuilder.CellSize;
            var chasm = c.Spawn("Assets/Game/Abilities/Grapple/Chasm.prefab", new Vector3(-7.5f, 0f, z), 90f, "Chasm");
            chasm.transform.localScale = new Vector3(0.3f, 1f, 1f);
            var pit = chasm.transform.Find("Pit");
            if (pit != null)
                pit.localPosition = new Vector3(pit.localPosition.x, 0.03f, pit.localPosition.z);
            c.Spawn("Assets/Game/Abilities/Grapple/GrappleAnchor.prefab", new Vector3(-12.5f, 0f, z), 90f, "AnchorFar");
            c.Spawn("Assets/Game/Abilities/Grapple/GrappleAnchor.prefab", new Vector3(-2.5f, 0f, z), 270f, "AnchorNear");
            c.Empty("MapGate", new Vector3(-4.5f, 0f, z), 90f).AddComponent<Map.MapGate>();
        }

        private static void TestChamber(RoomContext c)
        {
            var host = c.Empty("FloorPattern", RoomContext.Cell(51f, 3f), 0f);
            var zones = new Hazards.HazardZone[6];
            for (var i = 0; i < zones.Length; i++)
            {
                var stripe = c.Spawn("Assets/Game/World/Hazards/Hazard_Electric.prefab", RoomContext.Cell(47f, 3f) + new Vector3(2f + i * 4f, 0f, 0f), 0f, $"Stripe{i}");
                stripe.transform.SetParent(host.transform, true);
                zones[i] = stripe.GetComponent<Hazards.HazardZone>();
                zones[i].Configure(Hazards.HazardKind.Electric, new Vector3(4f, 2f, 24f));
            }
            var pattern = host.AddComponent<Hazards.FloorPattern>();
            pattern.Configure(zones, null);
            RoomContext.Set(pattern, "_zones", zones);
        }

        private static void AutomatonTheatre(RoomContext c)
        {
            var center = RoomContext.Cell(51f, 3f);
            var theatre = c.AddByName(c.Empty("AutomatonTheatre", center, 0f), "TentacleVania.Bosses.Automaton.AutomatonTheatre");
            RoomContext.Set(theatre, "_radius", 11f);
            RoomContext.Set(theatre, "_tableRadius", 2.5f);
            var table = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            table.name = "OperatingTable";
            table.transform.SetParent(c.Content, false);
            table.transform.position = center + Vector3.up * 0.5f;
            table.transform.localScale = new Vector3(5f, 0.5f, 5f);
            table.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/World/Materials/Greybox_Pillar.mat");
            var obstacle = table.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Capsule;
            obstacle.carving = true;
        }
    }
}

using TentacleVania.World.Layout;
using UnityEditor;
using UnityEngine;

namespace TentacleVania.World.Editor
{
    public static class SpecimenHoldingPlan
    {
        public const string RootScene = "SpecimenHolding";
        public static readonly Vector3 ElevatorPosition = RoomContext.CellCenter(41.5f, 4f);
        private const string Pickups = "Assets/Game/World/Pickups/";
        private const string Logs = "Assets/Game/World/Lore/Data/";

        [MenuItem("TentacleVania/World/Build Specimen Holding")]
        private static void BuildFromMenu() => Debug.Log(Build());

        public static string Build(string onlyScene = null) => SectorBuilder.Build(Create(), onlyScene);

        private static Vector2Int P(int x, int y) => new(x, y);

        private static WaveEntryPlan E(string prefab, int count) => new(prefab, count);

        public static SectorPlan Create()
        {
            var plan = new SectorPlan
            {
                Sector = "SH",
                RootScene = "SpecimenHolding",
                Folder = "Assets/Game/Scenes/SpecimenHolding",
                AddressablesGroup = "SpecimenHolding",
                Color = new Color(0.55f, 0.75f, 0.95f, 1f),
                Spawn = RoomContext.CellCenter(3f, 38.5f),
                SpawnYaw = 180f,
            };
            var rooms = plan.Rooms;

            rooms.Add(new RoomPlan("SH_PodChamber", PlanRole.Arena, new RectInt(1, 36, 6, 5))
            {
                OneTime = true,
                Place = c =>
                {
                    Nest(c, "SH/PodNest", RoomContext.CellCenter(5f, 39f), 180f);
                    var door = c.Spawn("Assets/Game/World/BioNests/NestDoor.prefab", RoomContext.Cell(3.5f, 36f), 0f, "PodExitDoor");
                    RoomContext.Set(door.GetComponent<BioNests.NestDoor>(), "_nestId", "SH/PodNest");
                    Terminal(c, "PodHistory", RoomContext.CellCenter(1.2f, 40f), 0f);
                    Terminal(c, "PodLabel", RoomContext.CellCenter(1.2f, 37f), 0f);
                    Crawl(c, "SH/T1", false, "", RoomContext.CellCenter(1f, 40f), 180f, RoomContext.CellCenter(24f, 1f), 0f);
                    var zone = c.AddByName(c.Empty("PaZone", RoomContext.CellCenter(3.5f, 38.5f), 0f), "TentacleVania.Audio.PaZone");
                    RoomContext.Set(zone, "_sequence", "sh.entry");
                    RoomContext.Set(zone, "_size", new Vector3(16f, 4f, 13f));
                },
            }.Hall(P(3, 35), P(3, 31)).Door(3, 31, Side.South).Wave(E("Scientist", 2)));

            rooms.Add(new RoomPlan("SH_A1", PlanRole.Arena, new RectInt(0, 24, 7, 7))
            {
                Place = c =>
                {
                    Terminal(c, "NightShiftChat", RoomContext.CellCenter(9f, 27.35f), 180f);
                    Crawl(c, "SH/T2", false, "", RoomContext.CellCenter(6f, 24f), 0f, RoomContext.CellCenter(25f, 18f), 180f);
                },
            }.Hall(P(7, 27), P(12, 27)).Hall(P(3, 23), P(3, 18))
                .Door(3, 30, Side.North, true).Door(6, 27, Side.East, true).Door(3, 24, Side.South, true)
                .Door(12, 27, Side.East).Door(3, 18, Side.South)
                .Wave(E("Scientist", 3)).Wave(E("SecurityGuard", 2)));

            rooms.Add(new RoomPlan("SH_A2", PlanRole.Arena, new RectInt(13, 25, 5, 5)) { WallMounts = true }
                .Hall(P(18, 27), P(23, 27)).Hall(P(15, 30), P(15, 35))
                .Door(13, 27, Side.West, true).Door(17, 27, Side.East, true).Door(15, 29, Side.North, true)
                .Door(23, 27, Side.East).Door(15, 35, Side.North)
                .Wave(E("SecurityGuard", 2), E("Turret", 1)));

            rooms.Add(new RoomPlan("SH_A3", PlanRole.Arena, new RectInt(24, 24, 7, 7))
            {
                Place = c => Relay(c, "A3", RoomContext.CellCenter(29.5f, 29.5f)),
            }.Hall(P(27, 23), P(27, 19))
                .Door(24, 27, Side.West, true).Door(27, 24, Side.South, true).Door(27, 19, Side.South)
                .Wave(E("SecurityGuard", 2)).Wave(E("Handler", 1), E("Scientist", 2)));

            rooms.Add(new RoomPlan("SH_A4", PlanRole.Arena, new RectInt(1, 13, 5, 5))
                .Hall(P(6, 15), P(11, 15)).Hall(P(3, 12), P(3, 7)).Hall(P(0, 15), P(-5, 15))
                .Door(3, 17, Side.North, true).Door(5, 15, Side.East, true).Door(3, 13, Side.South, true).Door(1, 15, Side.West, true)
                .Door(11, 15, Side.East).Door(3, 7, Side.South).Door(-5, 15, Side.West)
                .Wave(E("Scientist", 3), E("SecurityGuard", 1)));
            rooms[rooms.Count - 1].Place = GrappleGate;

            rooms.Add(new RoomPlan("SH_A5", PlanRole.Arena, new RectInt(12, 12, 7, 7))
            {
                Place = c => Relay(c, "A5", RoomContext.CellCenter(17.5f, 17.5f)),
            }.Hall(P(19, 15), P(23, 15)).Hall(P(15, 11), P(15, 7))
                .Door(12, 15, Side.West, true).Door(18, 15, Side.East, true).Door(15, 12, Side.South, true)
                .Door(23, 15, Side.East).Door(15, 7, Side.South)
                .Wave(E("Handler", 2), E("SecurityGuard", 2)));

            rooms.Add(new RoomPlan("SH_A6", PlanRole.Arena, new RectInt(24, 12, 7, 7))
            {
                WallMounts = true,
                Place = c => Crawl(c, "SH/T2", true, "", RoomContext.CellCenter(25f, 18f), 180f, RoomContext.CellCenter(6f, 24f), 0f),
            }
                .Hall(P(31, 15), P(35, 15))
                .Door(27, 18, Side.North, true).Door(24, 15, Side.West, true).Door(30, 15, Side.East, true)
                .Door(35, 15, Side.East)
                .Wave(E("Turret", 2), E("SecurityGuard", 3)));

            rooms.Add(new RoomPlan("SH_A7", PlanRole.Arena, new RectInt(0, 0, 7, 7))
            {
                WallMounts = true,
                Place = c => Relay(c, "A7", RoomContext.CellCenter(1f, 1f)),
            }.Hall(P(7, 3), P(11, 3))
                .Door(3, 6, Side.North, true).Door(6, 3, Side.East, true).Door(11, 3, Side.East)
                .Wave(E("Handler", 1), E("SecurityGuard", 3)).Wave(E("Turret", 1)));

            rooms.Add(new RoomPlan("SH_A8", PlanRole.Arena, new RectInt(12, 0, 7, 7))
            {
                Place = c => c.Spawn(Pickups + "CrusherMassPickup.prefab", RoomContext.CellCenter(13f, 1f), 0f),
            }.Hall(P(19, 3), P(23, 3))
                .Door(15, 6, Side.North, true).Door(12, 3, Side.West, true).Door(18, 3, Side.East, true).Door(23, 3, Side.East)
                .Wave(E("Handler", 2), E("SecurityGuard", 3)));

            rooms.Add(new RoomPlan("SH_PreBossNest", PlanRole.Plain, new RectInt(24, 1, 5, 5))
            {
                Place = c =>
                {
                    Nest(c, "SH/PreBossNest", RoomContext.CellCenter(26f, 4f), 180f);
                    Crawl(c, "SH/T1", true, "SH/PreBossNest", RoomContext.CellCenter(24f, 1f), 0f, RoomContext.CellCenter(1f, 40f), 180f);
                    var door = c.Spawn("Assets/Game/World/Security/SecurityDoor.prefab", RoomContext.CellCenter(31f, 3f), 90f, "SecurityDoor");
                    RoomContext.Set(door.GetComponent<Security.SecurityDoor>(), "_system", "SH");
                },
            }.Hall(P(29, 3), P(34, 3)).Door(24, 3, Side.West).Door(34, 3, Side.East));

            rooms.Add(new RoomPlan("SH_WardenArena", PlanRole.Boss, new RectInt(35, 0, 8, 6))
            {
                Place = c =>
                {
                    var center = RoomContext.Cell(39f, 3f);
                    var hall = c.AddByName(c.Empty("WardenHall", center, 0f), "TentacleVania.Bosses.Warden.WardenHall");
                    RoomContext.Set(hall, "_size", new Vector2(24f, 18f));
                    var elevator = c.Spawn("Assets/Game/World/Transit/FreightElevator.prefab", ElevatorPosition, 270f, "FreightElevator");
                    BioLabsPlan.Elevator(elevator.GetComponent<Transit.SectorElevator>(), BioLabsPlan.RootScene, BioLabsPlan.ElevatorPosition + Vector3.right * 1.5f, 90f, "SH/WardenDrone");
                },
            }.Door(35, 3, Side.West, true).Wave(E("WardenDrone", 1)));

            rooms.Add(new RoomPlan("SH_R1", PlanRole.Plain, new RectInt(13, 36, 5, 4))
            {
                Place = c => c.Spawn(Pickups + "TrinketPickup.prefab", RoomContext.CellCenter(15f, 38f), 0f, "PodReleaseKey"),
            }.Door(15, 36, Side.South));

            rooms.Add(new RoomPlan("SH_R2", PlanRole.Plain, new RectInt(36, 13, 5, 5))
            {
                Place = c =>
                {
                    c.Spawn(Pickups + "BiomassCache.prefab", RoomContext.CellCenter(38f, 15f), 0f);
                    Terminal(c, "IntakeMemo", RoomContext.CellCenter(40.2f, 15f), 270f);
                },
            }.Door(36, 15, Side.West));

            rooms.Add(new RoomPlan("SH_GrappleRoom", PlanRole.Plain, new RectInt(-10, 13, 5, 5))
            {
                Place = c => c.Spawn(Pickups + "MutagenPickup.prefab", RoomContext.CellCenter(-8f, 15f), 0f),
            }.Door(-6, 15, Side.East));

            return plan;
        }

        private static void Nest(RoomContext c, string id, Vector3 position, float yaw)
        {
            var nest = c.Spawn("Assets/Game/World/BioNests/BioNest.prefab", position, yaw, id.Replace('/', '_'));
            RoomContext.Set(nest.GetComponent<BioNests.BioNest>(), "_id", id);
        }

        private static void Terminal(RoomContext c, string log, Vector3 position, float yaw)
        {
            var terminal = c.Spawn("Assets/Game/World/Lore/LoreTerminal.prefab", position, yaw, "Lore_" + log);
            RoomContext.Set(terminal.GetComponent<Lore.LoreTerminal>(), "_log", AssetDatabase.LoadAssetAtPath<Lore.LoreLog>(Logs + log + ".asset"));
        }

        private static void Relay(RoomContext c, string id, Vector3 position)
        {
            var relay = c.Spawn("Assets/Game/World/Security/SecurityRelay.prefab", position, 0f, "Relay_" + id);
            var component = relay.GetComponent<Security.SecurityRelay>();
            RoomContext.Set(component, "_system", "SH");
            RoomContext.Set(component, "_id", id);
        }

        private static void Crawl(RoomContext c, string id, bool opensHere, string nest, Vector3 position, float yaw, Vector3 otherEnd, float otherYaw)
        {
            var end = c.Spawn("Assets/Game/World/Shortcuts/Crawlspace.prefab", position, yaw, id.Replace('/', '_') + (opensHere ? "_Far" : "_Near"));
            var crawl = end.GetComponent<Shortcuts.Crawlspace>();
            RoomContext.Set(crawl, "_id", id);
            RoomContext.Set(crawl, "_opensHere", opensHere);
            RoomContext.Set(crawl, "_openOnNestGrown", nest);
            RoomContext.Set(crawl, "_exitPosition", otherEnd + Quaternion.Euler(0f, otherYaw, 0f) * Vector3.forward * 1.2f);
            RoomContext.Set(crawl, "_exitYaw", otherYaw);
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
            var gate = c.Empty("MapGate", new Vector3(-4.5f, 0f, z), 90f);
            gate.AddComponent<Map.MapGate>();
        }
    }
}

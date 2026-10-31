using TentacleVania.World.Layout;
using UnityEditor;
using UnityEngine;

namespace TentacleVania.World.Editor
{
    public static class BioLabsPlan
    {
        public const string RootScene = "BioLabs";
        public static readonly Vector3 ElevatorPosition = RoomContext.CellCenter(1.6f, 36.6f);
        private const string Pickups = "Assets/Game/World/Pickups/";
        private const string Logs = "Assets/Game/World/Lore/Data/";
        private const string CrawlspacePath = "Assets/Game/World/Shortcuts/Crawlspace.prefab";

        [MenuItem("TentacleVania/World/Build Bio-Labs")]
        private static void BuildFromMenu() => Debug.Log(Build());

        public static string Build(string onlyScene = null) => SectorBuilder.Build(Create(), onlyScene);

        private static Vector2Int P(int x, int y) => new(x, y);

        private static WaveEntryPlan E(string prefab, int count) => new(prefab, count);

        public static SectorPlan Create()
        {
            var plan = new SectorPlan
            {
                Sector = "BL",
                RootScene = RootScene,
                Folder = "Assets/Game/Scenes/BioLabs",
                AddressablesGroup = "BioLabs",
                Color = new Color(0.45f, 0.9f, 0.6f, 1f),
                Spawn = RoomContext.CellCenter(3.5f, 38f),
                SpawnYaw = 180f,
            };
            var rooms = plan.Rooms;

            rooms.Add(new RoomPlan("BL_EntryNest", PlanRole.Plain, new RectInt(1, 36, 6, 5))
            {
                Place = c =>
                {
                    Nest(c, "BL/EntryNest", RoomContext.CellCenter(3.5f, 39f), 180f);
                    c.Spawn("Assets/Game/World/Hoarder/Hoarder.prefab", RoomContext.CellCenter(6.3f, 38f), 270f, "Hoarder");
                    var elevator = c.Spawn("Assets/Game/World/Transit/FreightElevator.prefab", ElevatorPosition, 90f, "FreightElevator");
                    Elevator(elevator.GetComponent<Transit.SectorElevator>(), SpecimenHoldingPlan.RootScene, SpecimenHoldingPlan.ElevatorPosition + Vector3.left * 1.5f, 270f, "");
                    var zone = c.AddByName(c.Empty("PaZone", RoomContext.CellCenter(3.5f, 38.5f), 0f), "TentacleVania.Audio.PaZone");
                    RoomContext.Set(zone, "_sequence", "bl.entry");
                    RoomContext.Set(zone, "_size", new Vector3(16f, 4f, 13f));
                    Crawl(c, "BL/T1", false, RoomContext.CellCenter(6f, 40f), 180f, RoomContext.CellCenter(13f, 18f), 180f);
                },
            }.Hall(P(3, 35), P(3, 31)).Door(3, 31, Side.South));

            rooms.Add(new RoomPlan("BL_A1", PlanRole.Arena, new RectInt(0, 24, 7, 7))
                .Hall(P(7, 27), P(12, 27)).Hall(P(3, 23), P(3, 19))
                .Door(3, 30, Side.North, true).Door(6, 27, Side.East, true).Door(3, 24, Side.South, true)
                .Door(12, 27, Side.East).Door(3, 19, Side.South)
                .Wave(E("Surgeon", 2), E("Scientist", 2)));

            rooms.Add(new RoomPlan("BL_A2", PlanRole.Arena, new RectInt(13, 25, 5, 5))
                .Hall(P(18, 27), P(23, 27)).Hall(P(15, 24), P(15, 19)).Hall(P(15, 30), P(15, 35))
                .Door(13, 27, Side.West, true).Door(17, 27, Side.East, true).Door(15, 25, Side.South, true).Door(15, 29, Side.North, true)
                .Door(23, 27, Side.East).Door(15, 19, Side.South).Door(15, 35, Side.North)
                .Wave(E("LabSecurity", 2), E("MedDrone", 1)));

            rooms.Add(new RoomPlan("BL_A3", PlanRole.Arena, new RectInt(24, 24, 7, 7))
            {
                Place = c =>
                {
                    Relay(c, "A3", RoomContext.CellCenter(29.5f, 25f));
                    Crawl(c, "BL/T2", false, RoomContext.CellCenter(30f, 30f), 180f, RoomContext.CellCenter(30f, 0f), 0f);
                },
            }.Hall(P(27, 23), P(27, 18))
                .Door(24, 27, Side.West, true).Door(27, 24, Side.South, true).Door(27, 18, Side.South)
                .Wave(E("Hazmat", 2), E("Surgeon", 1)).Wave(E("LabSecurity", 2)));

            rooms.Add(new RoomPlan("BL_A4", PlanRole.Arena, new RectInt(0, 12, 7, 7))
            {
                Place = c => Crawl(c, "BL/T3", false, RoomContext.CellCenter(0f, 12f), 0f, RoomContext.CellCenter(40f, 5f), 180f),
            }.Hall(P(7, 15), P(11, 15)).Hall(P(3, 11), P(3, 7)).Hall(P(-1, 15), P(-4, 15))
                .Door(3, 18, Side.North, true).Door(6, 15, Side.East, true).Door(3, 12, Side.South, true).Door(0, 15, Side.West, true)
                .Door(11, 15, Side.East).Door(3, 7, Side.South).Door(-4, 15, Side.West)
                .Wave(E("Surgeon", 3), E("MedDrone", 1)));
            rooms[rooms.Count - 1].Place += c => Terminal(c, "TissueSampleSource", RoomContext.CellCenter(9f, 15.35f), 180f);

            rooms.Add(new RoomPlan("BL_A5", PlanRole.Arena, new RectInt(12, 12, 7, 7))
            {
                Place = c =>
                {
                    Terminal(c, "SiblingTermination", RoomContext.CellCenter(16.5f, 17.7f), 180f);
                    Terminal(c, "SurgeonsBettingPool", RoomContext.CellCenter(17.7f, 13.5f), 270f);
                    Crawl(c, "BL/T1", true, RoomContext.CellCenter(13f, 18f), 180f, RoomContext.CellCenter(6f, 40f), 180f);
                },
            }.Hall(P(19, 15), P(24, 15))
                .Door(15, 18, Side.North, true).Door(12, 15, Side.West, true).Door(18, 15, Side.East, true).Door(24, 15, Side.East)
                .Wave(E("LabSecurity", 2), E("Hazmat", 2)).Wave(E("MedDrone", 2)));

            rooms.Add(new RoomPlan("BL_A6", PlanRole.Arena, new RectInt(25, 13, 5, 5))
            {
                Place = c =>
                {
                    var grate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    grate.name = "VentGrate";
                    grate.transform.SetParent(c.Content, false);
                    grate.transform.position = RoomContext.CellCenter(32f, 15f) + Vector3.up * 1.5f;
                    grate.transform.localScale = new Vector3(0.3f, 3f, 3f);
                    grate.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/World/Materials/Greybox_Hatch.mat");
                    c.Empty("MapGate", RoomContext.CellCenter(32f, 15f), 90f).AddComponent<Map.MapGate>();
                },
            }.Hall(P(27, 12), P(27, 7)).Hall(P(30, 15), P(34, 15))
                .Door(27, 17, Side.North, true).Door(25, 15, Side.West, true).Door(27, 13, Side.South, true).Door(29, 15, Side.East, true)
                .Door(27, 7, Side.South).Door(34, 15, Side.East)
                .Wave(E("Surgeon", 3)));

            rooms.Add(new RoomPlan("BL_A7", PlanRole.Arena, new RectInt(0, 0, 7, 7))
            {
                Place = c => Relay(c, "A7", RoomContext.CellCenter(1f, 1f)),
            }.Hall(P(7, 3), P(11, 3))
                .Door(3, 6, Side.North, true).Door(6, 3, Side.East, true).Door(11, 3, Side.East)
                .Wave(E("LabSecurity", 3), E("MedDrone", 1)));

            rooms.Add(new RoomPlan("BL_A8", PlanRole.Arena, new RectInt(12, 0, 7, 7))
                .Hall(P(19, 3), P(23, 3)).Hall(P(15, -1), P(15, -4))
                .Door(12, 3, Side.West, true).Door(18, 3, Side.East, true).Door(15, 0, Side.South, true)
                .Door(23, 3, Side.East).Door(15, -4, Side.South)
                .Wave(E("Hazmat", 2), E("Surgeon", 2)).Wave(E("LabSecurity", 2)));

            rooms.Add(new RoomPlan("BL_A9", PlanRole.Arena, new RectInt(24, 0, 7, 7))
            {
                Place = c =>
                {
                    Relay(c, "A9", RoomContext.CellCenter(29.5f, 5.5f));
                    Crawl(c, "BL/T2", true, RoomContext.CellCenter(30f, 0f), 0f, RoomContext.CellCenter(30f, 30f), 180f);
                },
            }.Hall(P(31, 3), P(35, 3))
                .Door(27, 6, Side.North, true).Door(24, 3, Side.West, true).Door(30, 3, Side.East, true).Door(35, 3, Side.East)
                .Wave(E("MedDrone", 2), E("Hazmat", 2), E("Surgeon", 2)));

            rooms.Add(new RoomPlan("BL_PreBossNest", PlanRole.Plain, new RectInt(36, 1, 5, 5))
            {
                Place = c =>
                {
                    Nest(c, "BL/PreBossNest", RoomContext.CellCenter(38f, 4f), 180f);
                    var door = c.Spawn("Assets/Game/World/Security/SecurityDoor.prefab", RoomContext.CellCenter(43f, 3f), 90f, "SecurityDoor");
                    RoomContext.Set(door.GetComponent<Security.SecurityDoor>(), "_system", "BL");
                    Crawl(c, "BL/T3", true, RoomContext.CellCenter(40f, 5f), 180f, RoomContext.CellCenter(0f, 12f), 0f);
                },
            }.Hall(P(41, 3), P(46, 3)).Door(36, 3, Side.West).Door(46, 3, Side.East));

            rooms.Add(new RoomPlan("BL_AutomatonTheatre", PlanRole.Boss, new RectInt(47, -1, 8, 8))
            {
                Place = c =>
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
                },
            }.Door(47, 3, Side.West, true).Wave(E("SurgicalAutomaton", 1)));

            rooms.Add(new RoomPlan("BL_R1", PlanRole.Plain, new RectInt(13, 36, 5, 4))
            {
                Place = c =>
                {
                    var trinket = c.Spawn(Pickups + "TrinketPickup.prefab", RoomContext.CellCenter(15f, 38f), 0f, "TaggedLockerKey");
                    RoomContext.Set(trinket.GetComponent<Pickups.Pickup>(), "_itemId", "TaggedLockerKey");
                },
            }.Door(15, 36, Side.South));

            rooms.Add(new RoomPlan("BL_R2", PlanRole.Plain, new RectInt(-9, 13, 5, 5))
            {
                Place = c =>
                {
                    c.Spawn(Pickups + "BiomassCache.prefab", RoomContext.CellCenter(-7f, 15f), 0f);
                    Terminal(c, "DissectionSchedule", RoomContext.CellCenter(-8.8f, 15f), 90f);
                },
            }.Door(-5, 15, Side.East));

            rooms.Add(new RoomPlan("BL_R3", PlanRole.Plain, new RectInt(13, -9, 5, 5))
            {
                Place = c =>
                {
                    var mutation = c.Spawn(Pickups + "MutationPickup.prefab", RoomContext.CellCenter(15f, -7f), 0f, "LungingMaw");
                    RoomContext.Set(mutation.GetComponent<Pickups.Pickup>(), "_itemId", "LungingMaw");
                },
            }.Door(15, -5, Side.North));

            rooms.Add(new RoomPlan("BL_VentRoom", PlanRole.Plain, new RectInt(35, 13, 5, 5))
            {
                Place = c => c.Spawn(Pickups + "MutagenPickup.prefab", RoomContext.CellCenter(37f, 15f), 0f),
            }.Door(35, 15, Side.West));

            return plan;
        }

        public static void Elevator(Transit.SectorElevator elevator, string scene, Vector3 arrival, float yaw, string boss)
        {
            RoomContext.Set(elevator, "_destinationScene", scene);
            RoomContext.Set(elevator, "_arrivalPosition", arrival);
            RoomContext.Set(elevator, "_arrivalYaw", yaw);
            RoomContext.Set(elevator, "_requiredBoss", boss);
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
            RoomContext.Set(component, "_system", "BL");
            RoomContext.Set(component, "_id", id);
        }

        private static void Crawl(RoomContext c, string id, bool opensHere, Vector3 position, float yaw, Vector3 otherEnd, float otherYaw)
        {
            var end = c.Spawn(CrawlspacePath, position, yaw, id.Replace('/', '_') + (opensHere ? "_Far" : "_Near"));
            var exit = otherEnd + Quaternion.Euler(0f, otherYaw, 0f) * Vector3.forward * 1.2f;
            var crawl = end.GetComponent<Shortcuts.Crawlspace>();
            RoomContext.Set(crawl, "_id", id);
            RoomContext.Set(crawl, "_opensHere", opensHere);
            RoomContext.Set(crawl, "_exitPosition", exit);
            RoomContext.Set(crawl, "_exitYaw", otherYaw);
        }
    }
}

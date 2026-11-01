using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TentacleVania.World.Layout;
using UnityEditor;
using UnityEngine;

namespace TentacleVania.World.Editor
{
    public static class SectorSpecLoader
    {
        public const string SpecFolder = "Assets/Game/World/Data/Sectors";
        public const string SpecSuffix = ".sector.json";
        private const string Pickups = "Assets/Game/World/Pickups/";
        private const string Logs = "Assets/Game/World/Lore/Data/";

        public static string[] SpecPaths() =>
            Directory.GetFiles(SpecFolder, "*" + SpecSuffix).Select(p => p.Replace('\\', '/')).OrderBy(p => p, StringComparer.Ordinal).ToArray();

        public static SectorSpec Read(string path) => JsonConvert.DeserializeObject<SectorSpec>(File.ReadAllText(path));

        public static SectorPlan Load(string path)
        {
            var spec = Read(path);
            var plan = new SectorPlan
            {
                Sector = spec.Sector,
                RootScene = spec.RootScene,
                Folder = spec.Folder,
                AddressablesGroup = spec.AddressablesGroup,
                Color = new Color(spec.Color[0], spec.Color[1], spec.Color[2], spec.Color.Length > 3 ? spec.Color[3] : 1f),
                Spawn = RoomContext.CellCenter(spec.Spawn[0], spec.Spawn[1]),
                SpawnYaw = spec.SpawnYaw,
            };
            foreach (var room in spec.Rooms)
                plan.Rooms.Add(Room(spec, room));
            return plan;
        }

        private static RoomPlan Room(SectorSpec sector, RoomSpec spec)
        {
            var role = (PlanRole)Enum.Parse(typeof(PlanRole), spec.Role);
            RectInt? rect = spec.Rect != null ? new RectInt(spec.Rect[0], spec.Rect[1], spec.Rect[2], spec.Rect[3]) : null;
            var room = new RoomPlan(spec.Scene, role, rect) { WallMounts = spec.WallMounts, OneTime = spec.OneTime };
            foreach (var hall in spec.Halls)
                room.Hall(hall.Select(p => new Vector2Int(p[0], p[1])).ToArray());
            foreach (var door in spec.Doors)
                room.Door(door.X, door.Y, (Side)Enum.Parse(typeof(Side), door.Side), door.Arena);
            foreach (var wave in spec.Waves)
                room.Wave(wave.Select(e => new WaveEntryPlan(e.Enemy, e.Count)).ToArray());
            foreach (var placement in spec.Place)
            {
                var place = placement;
                room.Place += c => Apply(c, sector, place);
            }
            return room;
        }

        public static Vector3 Position(PlacementSpec spec) =>
            spec.At != null ? RoomContext.CellCenter(spec.At[0], spec.At[1])
            : spec.Cell != null ? RoomContext.Cell(spec.Cell[0], spec.Cell[1])
            : spec.World != null ? new Vector3(spec.World[0], spec.World[1], spec.World[2])
            : Vector3.zero;

        private static void Apply(RoomContext c, SectorSpec sector, PlacementSpec spec)
        {
            var position = Position(spec);
            switch (spec.Type)
            {
                case "nest":
                    var nest = c.Spawn("Assets/Game/World/BioNests/BioNest.prefab", position, spec.Yaw, spec.Id.Replace('/', '_'));
                    RoomContext.Set(nest.GetComponent<BioNests.BioNest>(), "_id", spec.Id);
                    break;
                case "terminal":
                    var terminal = c.Spawn("Assets/Game/World/Lore/LoreTerminal.prefab", position, spec.Yaw, "Lore_" + spec.Log);
                    RoomContext.Set(terminal.GetComponent<Lore.LoreTerminal>(), "_log", AssetDatabase.LoadAssetAtPath<Lore.LoreLog>(Logs + spec.Log + ".asset"));
                    break;
                case "relay":
                    var relay = c.Spawn("Assets/Game/World/Security/SecurityRelay.prefab", position, 0f, "Relay_" + spec.Id).GetComponent<Security.SecurityRelay>();
                    RoomContext.Set(relay, "_system", spec.System ?? sector.Sector);
                    RoomContext.Set(relay, "_id", spec.Id);
                    break;
                case "securityDoor":
                    var door = c.Spawn("Assets/Game/World/Security/SecurityDoor.prefab", position, spec.Yaw, spec.Name ?? "SecurityDoor");
                    RoomContext.Set(door.GetComponent<Security.SecurityDoor>(), "_system", spec.System ?? sector.Sector);
                    break;
                case "nestDoor":
                    var nestDoor = c.Spawn("Assets/Game/World/BioNests/NestDoor.prefab", position, spec.Yaw, spec.Name ?? "NestDoor");
                    RoomContext.Set(nestDoor.GetComponent<BioNests.NestDoor>(), "_nestId", spec.Nest);
                    break;
                case "crawl":
                    var other = RoomContext.CellCenter(spec.OtherAt[0], spec.OtherAt[1]);
                    var end = c.Spawn("Assets/Game/World/Shortcuts/Crawlspace.prefab", position, spec.Yaw, spec.Id.Replace('/', '_') + (spec.OpensHere ? "_Far" : "_Near"));
                    var crawl = end.GetComponent<Shortcuts.Crawlspace>();
                    RoomContext.Set(crawl, "_id", spec.Id);
                    RoomContext.Set(crawl, "_opensHere", spec.OpensHere);
                    RoomContext.Set(crawl, "_openOnNestGrown", spec.Nest ?? "");
                    RoomContext.Set(crawl, "_exitPosition", other + Quaternion.Euler(0f, spec.OtherYaw, 0f) * Vector3.forward * 1.2f);
                    RoomContext.Set(crawl, "_exitYaw", spec.OtherYaw);
                    break;
                case "pickup":
                    var pickup = c.Spawn(Pickups + spec.Prefab + ".prefab", position, spec.Yaw, spec.Name);
                    if (!string.IsNullOrEmpty(spec.ItemId))
                        RoomContext.Set(pickup.GetComponent<Pickups.Pickup>(), "_itemId", spec.ItemId);
                    break;
                case "prefab":
                    c.Spawn(spec.Prefab, position, spec.Yaw, spec.Name);
                    break;
                case "paZone":
                    var zone = c.AddByName(c.Empty("PaZone", position, 0f), "TentacleVania.Audio.PaZone");
                    RoomContext.Set(zone, "_sequence", spec.Sequence);
                    RoomContext.Set(zone, "_size", new Vector3(spec.Size[0], spec.Size[1], spec.Size[2]));
                    if (spec.Delay > 0f)
                        RoomContext.Set(zone, "_delay", spec.Delay);
                    break;
                case "elevator":
                    var elevator = c.Spawn("Assets/Game/World/Transit/FreightElevator.prefab", position, spec.Yaw, spec.Name ?? "FreightElevator").GetComponent<Transit.SectorElevator>();
                    var arrival = RoomContext.CellCenter(spec.ArrivalAt[0], spec.ArrivalAt[1]);
                    if (spec.ArrivalOffset != null)
                        arrival += new Vector3(spec.ArrivalOffset[0], spec.ArrivalOffset[1], spec.ArrivalOffset[2]);
                    RoomContext.Set(elevator, "_destinationScene", spec.Destination);
                    RoomContext.Set(elevator, "_arrivalPosition", arrival);
                    RoomContext.Set(elevator, "_arrivalYaw", spec.ArrivalYaw);
                    RoomContext.Set(elevator, "_requiredBoss", spec.RequiredBoss ?? "");
                    break;
                case "hazard":
                    var kind = (Hazards.HazardKind)Enum.Parse(typeof(Hazards.HazardKind), spec.Kind);
                    var hazard = c.Spawn($"Assets/Game/World/Hazards/Hazard_{kind}.prefab", position, spec.Yaw, spec.Name ?? "Hazard_" + kind);
                    var size = spec.Size != null ? new Vector3(spec.Size[0], spec.Size.Length > 2 ? spec.Size[1] : 2f, spec.Size[spec.Size.Length > 2 ? 2 : 1]) : new Vector3(6f, 2f, 6f);
                    hazard.GetComponent<Hazards.HazardZone>().Configure(kind, size);
                    break;
                case "hook":
                    if (!SectorHooks.TryGet(spec.Hook, out var hook))
                        throw new ArgumentException($"{c.Plan.Scene}: unknown sector hook '{spec.Hook}' (add it to SectorHooks)");
                    hook(c);
                    break;
                default:
                    throw new ArgumentException($"{c.Plan.Scene}: unknown placement type '{spec.Type}'");
            }
        }
    }
}

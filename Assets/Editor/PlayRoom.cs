using System.IO;
using System.Linq;
using TentacleVania.Abilities.Grapple;
using TentacleVania.Creature;
using TentacleVania.Creature.Movement;
using TentacleVania.World.Arenas;
using TentacleVania.World.Streaming;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static partial class PlayRoom
{
    private const string RoomKey = "PlayRoom.Room";
    private const string CheatsKey = "PlayRoom.Cheats";
    private const string WaveKey = "PlayRoom.Wave";
    private const string BypassKey = "PlayRoom.SetBypass";
    public const string LogKey = "PlayRoom.Log";
    private const float Timeout = 30f;

    private static float _deadline;
    private static bool _moved;

    [OnCodeLoaded]
    private static void OnLoad() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

    [MenuItem("TentacleVania/World/Play Active Room")]
    private static void PlayActiveRoom() => Debug.Log(Start(SceneManager.GetActiveScene().name, true, 0));

    public static string Start(string room, bool cheats, int wave)
    {
        if (EditorApplication.isPlaying)
            return "ERROR already in play mode";
        var registry = AssetDatabase.FindAssets("t:RoomRegistry")
            .Select(g => AssetDatabase.LoadAssetAtPath<RoomRegistry>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(r => r != null && r.IndexOf(room) >= 0);
        if (registry == null)
            return $"ERROR no room registry has a room named {room}";
        var root = AssetDatabase.FindAssets($"t:Scene {registry.RootScene}").Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == registry.RootScene);
        if (root == null)
            return $"ERROR root scene {registry.RootScene} not found";
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return "ERROR cancelled";
        EditorSceneManager.OpenScene(root, OpenSceneMode.Single);
        SessionState.SetString(RoomKey, room);
        SessionState.SetBool(CheatsKey, cheats);
        SessionState.SetInt(WaveKey, wave);
        SessionState.SetString(LogKey, "starting");
        if (!PlayFromMainMenu.Bypass)
        {
            SessionState.SetBool(BypassKey, true);
            PlayFromMainMenu.Bypass = true;
        }
        EditorApplication.EnterPlaymode();
        return $"playing {registry.RootScene} from {room}" + (cheats ? " with cheats" : "") + (wave > 0 ? $" at wave {wave}" : "");
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode && !string.IsNullOrEmpty(SessionState.GetString(RoomKey, "")))
        {
            _deadline = Time.realtimeSinceStartup + Timeout;
            _moved = false;
            EditorApplication.update += Tick;
        }
        else if (change == PlayModeStateChange.ExitingPlayMode)
        {
            EditorApplication.update -= Tick;
            SessionState.EraseString(RoomKey);
            if (SessionState.GetBool(BypassKey, false))
            {
                SessionState.EraseBool(BypassKey);
                PlayFromMainMenu.Bypass = false;
            }
        }
    }

    private static void Tick()
    {
        var room = SessionState.GetString(RoomKey, "");
        if (!EditorApplication.isPlaying || string.IsNullOrEmpty(room))
        {
            EditorApplication.update -= Tick;
            return;
        }
        if (Time.realtimeSinceStartup > _deadline)
        {
            RoomStreamer.FocusOverride = null;
            Finish($"ERROR timed out moving into {room}");
            return;
        }
        var creature = CreatureSpawner.Spawned;
        var streamer = RoomStreamer.Active;
        if (creature == null || streamer == null)
            return;
        var entry = streamer.Registry.Rooms[streamer.Registry.IndexOf(room)];
        var target = FloorPoint(entry);
        if (!_moved)
        {
            var motor = creature.GetComponent<CreatureMotor>();
            RoomStreamer.FocusOverride = target;
            motor.BeginTransit();
            if (!SceneManager.GetSceneByName(room).isLoaded)
                return;
            creature.transform.position = target;
            motor.EndTransit();
            RoomStreamer.FocusOverride = null;
            _moved = true;
        }

        if (NavMesh.SamplePosition(creature.transform.position, out var hit, 8f, NavMesh.AllAreas)
            && (hit.position - creature.transform.position).sqrMagnitude > 0.25f)
            Teleport(creature, hit.position);
        var log = $"in {room}";
        if (SessionState.GetBool(CheatsKey, false))
        {
            CreatureVitals.GodMode = true;
            TentacleGrapple.ForceUnlocked = true;
            log += ", god mode, grapple unlocked";
        }
        var wave = SessionState.GetInt(WaveKey, 0);
        if (wave > 0)
        {
            var arena = Object.FindObjectsByType<ArenaEncounter>().FirstOrDefault(a => a.gameObject.scene.name == room);
            if (arena == null)
                log += $", ERROR {room} has no arena";
            else
            {
                arena.BeginAt(wave - 1, Time.time);
                log += $", arena at wave {arena.WaveNumber}/{arena.WaveCount}" + (arena.WaveNumber != wave ? $" (it has no wave {wave})" : "");
            }
        }
        Finish(log);
    }

    public static Vector3 FloorPoint(RoomEntry room)
    {
        var best = new Rect(room.Center.x, room.Center.z, 0f, 0f);
        foreach (var shape in room.MapShapes)
            if (shape.width * shape.height > best.width * best.height)
                best = shape;
        return new Vector3(best.center.x, 0.1f, best.center.y);
    }

    private static void Teleport(GameObject creature, Vector3 position)
    {
        var controller = creature.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;
        creature.transform.position = position;
        if (controller != null)
            controller.enabled = true;
    }

    private static void Finish(string log)
    {
        EditorApplication.update -= Tick;
        SessionState.EraseString(RoomKey);
        SessionState.SetString(LogKey, log);
        Debug.Log("PlayRoom: " + log);
    }
}

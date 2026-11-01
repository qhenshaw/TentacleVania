TentacleVania.Creature.CreatureVitals.GodMode = true;
var registry = TentacleVania.World.Streaming.RoomStreamer.Active.Registry;
var creature = TentacleVania.Creature.CreatureSpawner.Spawned;
var motor = creature.GetComponent<TentacleVania.Creature.Movement.CreatureMotor>();
var rooms = registry.Rooms;
var index = 0;
var waiting = false;
var waitUntil = 0f;
var next = UnityEngine.Time.realtimeSinceStartup + 0.5f;
var log = "";
var dwell = UnityEditor.SessionState.GetFloat("walkDwell", 2f);
var recoveries = TentacleVania.World.Map.FallRecovery.CreatureRecoveries;
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (creature == null) { TentacleVania.World.Streaming.RoomStreamer.FocusOverride = null; UnityEditor.SessionState.SetString("walkLog", log + " creature lost"); UnityEditor.EditorApplication.update -= tick; return; }
    if (waiting)
    {
        var room = rooms[index];
        if (!UnityEngine.SceneManagement.SceneManager.GetSceneByName(room.Scene).isLoaded && UnityEngine.Time.realtimeSinceStartup < waitUntil) return;
        creature.transform.position = PlayRoom.FloorPoint(room);
        motor.EndTransit();
        TentacleVania.World.Streaming.RoomStreamer.FocusOverride = null;
        waiting = false;
        index++;
        next = UnityEngine.Time.realtimeSinceStartup + dwell;
        return;
    }
    if (UnityEngine.Time.realtimeSinceStartup < next) return;
    if (index > 0)
    {
        var expected = rooms[index - 1].Scene;
        var current = TentacleVania.World.Streaming.RoomStreamer.CurrentScene;
        var loaded = UnityEngine.SceneManagement.SceneManager.GetSceneByName(expected).isLoaded;
        var fell = TentacleVania.World.Map.FallRecovery.CreatureRecoveries != recoveries;
        recoveries = TentacleVania.World.Map.FallRecovery.CreatureRecoveries;
        log += (current == expected && loaded && !fell ? "ok " : "FAIL ") + expected + (current != expected ? " (current " + current + ")" : "") + (loaded ? "" : " (not loaded)") + (fell ? " (fell)" : "") + "; ";
    }
    if (index >= rooms.Count)
    {
        UnityEditor.SessionState.SetString("walkLog", log + "done");
        UnityEditor.EditorApplication.update -= tick;
        return;
    }
    var target = rooms[index];
    TentacleVania.World.Streaming.RoomStreamer.FocusOverride = PlayRoom.FloorPoint(target);
    motor.BeginTransit();
    waiting = true;
    waitUntil = UnityEngine.Time.realtimeSinceStartup + 10f;
};
UnityEditor.EditorApplication.update += tick;
return "walking " + rooms.Count + " rooms";

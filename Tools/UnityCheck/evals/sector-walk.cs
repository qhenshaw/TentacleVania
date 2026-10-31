TentacleVania.Creature.CreatureVitals.GodMode = true;
var registry = TentacleVania.World.Streaming.RoomStreamer.Active.Registry;
var creature = TentacleVania.Creature.CreatureSpawner.Spawned;
var controller = creature.GetComponent<UnityEngine.CharacterController>();
var rooms = registry.Rooms;
var index = 0;
var next = UnityEngine.Time.realtimeSinceStartup + 0.5f;
var log = "";
var dwell = UnityEditor.SessionState.GetFloat("walkDwell", 2f);
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (creature == null) { UnityEditor.SessionState.SetString("walkLog", log + " creature lost"); UnityEditor.EditorApplication.update -= tick; return; }
    if (UnityEngine.Time.realtimeSinceStartup < next) return;
    if (index > 0)
    {
        var expected = rooms[index - 1].Scene;
        var current = TentacleVania.World.Streaming.RoomStreamer.CurrentScene;
        var loaded = UnityEngine.SceneManagement.SceneManager.GetSceneByName(expected).isLoaded;
        log += (current == expected && loaded ? "ok " : "FAIL ") + expected + (current != expected ? " (current " + current + ")" : "") + (loaded ? "" : " (not loaded)") + "; ";
    }
    if (index >= rooms.Count)
    {
        UnityEditor.SessionState.SetString("walkLog", log + "done");
        UnityEditor.EditorApplication.update -= tick;
        return;
    }
    var room = rooms[index];
    var target = new UnityEngine.Vector3(room.Center.x, 0.1f, room.Center.z);
    controller.enabled = false;
    creature.transform.position = target;
    controller.enabled = true;
    index++;
    next = UnityEngine.Time.realtimeSinceStartup + dwell;
};
UnityEditor.EditorApplication.update += tick;
return "walking " + rooms.Count + " rooms";

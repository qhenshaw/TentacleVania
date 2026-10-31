TentacleVania.World.Streaming.RoomStreamer.Paused = true; TentacleVania.World.Streaming.RoomStreamer.Active.LoadAll();
var plan = TentacleVania.World.Streaming.RoomStreamer.Active.Registry.Sector == "BL" ? TentacleVania.World.Editor.BioLabsPlan.Create() : TentacleVania.World.Editor.SpecimenHoldingPlan.Create();
var t0 = UnityEngine.Time.realtimeSinceStartup;
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (UnityEngine.Time.realtimeSinceStartup - t0 < 6f) return;
    UnityEditor.EditorApplication.update -= tick;
    var loadedRooms = 0;
    for (var s = 0; s < UnityEngine.SceneManagement.SceneManager.sceneCount; s++) if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(s).name.Contains("_")) loadedRooms++;
    var log = "";
    var checkedCount = 0;
    foreach (var room in plan.Rooms)
    {
        foreach (var door in room.Doors)
        {
            var center = TentacleVania.World.Editor.RoomContext.CellCenter(door.Cell.x, door.Cell.y);
            var dir = door.Side == TentacleVania.World.Layout.Side.North ? UnityEngine.Vector3.forward : door.Side == TentacleVania.World.Layout.Side.South ? UnityEngine.Vector3.back : door.Side == TentacleVania.World.Layout.Side.East ? UnityEngine.Vector3.right : UnityEngine.Vector3.left;
            var from = center + UnityEngine.Vector3.up * 0.6f;
            checkedCount++;
            foreach (var hit in UnityEngine.Physics.RaycastAll(from, dir, 3f, ~0, UnityEngine.QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<TentacleVania.World.Arenas.ArenaDoor>() != null) continue;
                if (hit.collider.GetComponentInParent<TentacleVania.World.Shortcuts.Shortcut>() != null) continue;
                if (hit.collider.GetComponentInParent<TentacleVania.Creature.Movement.CreatureMotor>() != null) continue;
                log += room.Scene + " " + door.Cell + " " + door.Side + " blocked by " + hit.collider.name + " (" + hit.collider.gameObject.scene.name + "); ";
            }
        }
    }
    TentacleVania.World.Streaming.RoomStreamer.Paused = false;
    UnityEditor.SessionState.SetString("doorsLog", "checked " + checkedCount + " doors with " + loadedRooms + " rooms loaded; " + (log.Length == 0 ? "all clear" : log));
};
UnityEditor.EditorApplication.update += tick;
return "loading";

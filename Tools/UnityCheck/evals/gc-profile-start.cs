TentacleVania.Creature.CreatureVitals.GodMode = true;
var arena = TentacleVania.World.Arenas.ArenaEncounter.Active;
if (arena != null && arena.State == TentacleVania.World.Arenas.ArenaState.Waiting)
    arena.Begin(UnityEngine.Time.time);
UnityEditorInternal.ProfilerDriver.ClearAllFrames();
UnityEditorInternal.ProfilerDriver.enabled = true;
UnityEngine.Profiling.Profiler.enabled = true;
return "profiling";

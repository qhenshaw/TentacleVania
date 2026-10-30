# UI

Runtime UI (`TentacleVania.UI`), all UI Toolkit.

## Debug menu

- F5 toggles it in the editor and development builds (`Debug.isDebugBuild`); release builds never create it. `DebugMenuHost` is created after the first scene loads (`DontDestroyOnLoad`) from `Debug/Resources/DebugMenu/` (`DebugPanelSettings`, `DebugTheme.tss`, `DebugMenu.uss`) and builds its tabs in code. While it's open, `GameplayInput` is disabled so clicks don't attack; the game keeps running (use the Game tab's time scale to slow or freeze it).
- Areas register commands through `Core.Debugging.DebugMenu` without referencing this assembly: `Button`, `Toggle`, `Slider`, `Value` (live text, refreshed 5× a second while open) and `Choice` (dropdown plus Go). Register from a static `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` method in a `<Area>DebugCommands` class, and resolve the current objects inside the callbacks (`CombatTargets.Creature`, `EnemyManager.Instance`), since scenes reload. Entries registered later (or removed with `DebugMenu.Remove`) rebuild the open menu.
- Tabs appear in the order Game, Creature, Combat, Enemies, Camera; new area names are appended. Current commands:
  - Game: version, FPS, scene, time scale, restart scene.
  - Creature: health, position, speed, dash chain, legs stepping, god mode, heal, take 25 damage, kill, teleport to spawn.
  - Combat: assist sliders (damage dealt and taken), hits landed, last hit.
  - Enemies: spawn (from `Enemies/Debug/Resources/DebugEnemySpawnList`; add new enemy prefabs to it), alive count, token use, freeze AI, stun all, bring all to 20% HP, kill all.
  - Camera: screen shake and flash toggles, room bounds.
- To open it from an eval, call `TentacleVania.UI.Debug.DebugMenuHost.Toggle()`, or queue an F5 `KeyboardState` on `Keyboard.current`.

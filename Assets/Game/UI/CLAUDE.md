# UI

Runtime UI (`TentacleVania.UI`), all UI Toolkit.

## Main menu

- `Menu/MainMenu.prefab` (in the `MainMenu` scene) is a `UIDocument` on `MainMenuPanelSettings` (1920×1080 scale-with-screen) with `MainMenuScreen`, which binds the UXML's named elements to `UI` string table keys (`menu.title`, `menu.play`, `menu.quit`) through `LocalizedString.StringChanged`, focuses Play for gamepad navigation, and loads `SceneFlow.FirstGameplay` on Play. From an eval: `TentacleVania.UI.Menu.MainMenuScreen.Instance.Play()`.
- Player-facing text comes from string tables (`Assets/Game/Localization/`); set a key in UXML by name and bind it in code, never write the text in UXML or C#. Debug menu labels are exempt.

## Combat HUD

- `Hud/CombatHud.prefab` (one per gameplay scene, in `CombatArena`) is a `UIDocument` on `HudPanelSettings` with `CombatHud`, which updates in `LateUpdate` without allocating (enemy bars are a fixed pool of 32 built in `OnEnable`). Developer decisions: player health over charge at bottom-centre with no text; boss bars will go at the top; no combo-step display.
- Player health uses `BarTrail` (unit tested): damage drops the fill at once and leaves a pale chunk that drains after 0.5 s; heals show a green segment that the fill catches up to. Charge turns bright cyan and pulses when full.
- Enemy bars (HP with a grey armour strip, amber stagger under it) follow `EnemyManager.Current` enemies at agent height + 0.5 m, appear on any HP, armour or stagger change and fade 4 s later. An enemy under the grab threshold (`Enemy.IsFeedable`) keeps its bar shown with a pulsing green fill.
- Shared runtime theme: `RuntimeTheme.tss`; both the menu and HUD panel settings use it.
- `Hud/InteractionPromptView` (on `CombatHud.prefab`) shows `Core.Interaction.Interactions.Current`'s `PromptKey` from the `UI` table in the `prompt` label above the player bars, hidden (`hud__prompt--hidden`) when there is none; keys so far: `prompt.nest.grow`, `prompt.nest.rest`.
- `Hud/SaveIndicatorView` (on `CombatHud.prefab`) fades in the `saving` ring (bottom right, icon only) for 1.2 s after each autosave. `MainMenuScreen.Play` continues slot 1 when it holds a save (load it, then `SceneFlow.Load(SaveSystem.ContinueScene(...))`), else starts a new game in slot 1; the full slot menu is TV-83.
- `Hud/BossBarView` (on `CombatHud.prefab`) shows `Bosses.BossEnemy.Active` at the top: name from the `UI` table (`BossDefinition.NameKey`), health with a drain trail and phase ticks, and a stagger bar (pale blue while broken); it fades out after the boss dies. The `reward` label shows `boss.reward` ("Absorbed: {0}") for 3.5 s on `BossEnemy.Absorbed`.
- `Nest/NestMenuScreen` (`NestMenu` child of `CombatHud.prefab`, its own `UIDocument` with sorting order 10) is the nest menu, a side panel that slides in from the right on `Nests.MenuRequested` and locks `CreatureMotor` control. Stats tab: biomass, next level cost and level, plus one row per stat with its live effect (`nest.effect.*` format strings) and a + button (`Stats.TryLevelUp`). Respec edits a `Core.Progression.RespecDraft` until every point is assigned, then `Stats.Respec`. Mutations is a locked tab until mutations exist. Back cancels the respec or closes the menu; control unlocks only once Grab is released, so the button that opened it doesn't grab. `InteractionPromptView` hides while it's open. From an eval: `NestMenuScreen.Instance.Plus(i)` / `BeginRespec()` / `ConfirmRespec()` / `Close()`.
- `Hud/StatusIconsView` (on `CombatHud.prefab`) shows the creature's active statuses from `Combat.Status.StatusEffects` above the player bars: `status-slow` (purple square) and `status-root` (gold diamond), placeholder shapes with no text.

## Debug menu

- F5 toggles it in the editor and development builds (`Debug.isDebugBuild`); release builds never create it. `DebugMenuHost` is created after the first scene loads (`DontDestroyOnLoad`) from `Debug/Resources/DebugMenu/` (`DebugPanelSettings`, `DebugTheme.tss`, `DebugMenu.uss`) and builds its tabs in code. While it's open, `GameplayInput` is disabled so clicks don't attack; the game keeps running (use the Game tab's time scale to slow or freeze it).
- Areas register commands through `Core.Debugging.DebugMenu` without referencing this assembly: `Button`, `Toggle`, `Slider`, `Value` (live text, refreshed 5× a second while open) and `Choice` (dropdown plus Go). Register from a static `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` method in a `<Area>DebugCommands` class, and resolve the current objects inside the callbacks (`CombatTargets.Creature`, `EnemyManager.Instance`), since scenes reload. Entries registered later (or removed with `DebugMenu.Remove`) rebuild the open menu.
- Tabs appear in the order Game, Creature, Combat, Enemies, Camera, HUD; new area names are appended. Current commands:
  - Game: version, FPS, scene, time scale, restart scene.
  - Creature: health, position, speed, dash chain, legs stepping, god mode, heal, take 25 damage, kill, teleport to spawn.
  - Combat: assist sliders (damage dealt and taken), hits landed, last hit.
  - Enemies: spawn (from `Enemies/Debug/Resources/DebugEnemySpawnList`; add new enemy prefabs to it), alive count, token use, freeze AI, stun all, bring all to 20% HP, kill all.
  - Camera: screen shake and flash toggles, room bounds.
  - HUD: show HUD, always show enemy bars.
- To open it from an eval, call `TentacleVania.UI.Debug.DebugMenuHost.Toggle()`, or queue an F5 `KeyboardState` on `Keyboard.current`.

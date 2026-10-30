# Core

Shared code every area may reference (`TentacleVania.Core`). Keep it free of gameplay rules.

## Character master shader

- `Shaders/CharacterMaster.shadergraph` (built from `CharacterMaster.shadergraph.json` + `.hlsl` with `Tools/shadergraph-build.sh`) is the Lit master for every character: `_BaseMap`, `_BaseColor`, `_Smoothness`, `_EmissionColor`, plus effect channels `_GlowColor`/`_GlowAmount` (telegraphs, rim-weighted), `_FlashColor`/`_FlashAmount` (hit flash) and `_StatusColor`/`_StatusAmount`/`_StatusPulse` (stun, charge full, status effects; pulses when `_StatusPulse` > 0).
- `Characters/CharacterMaterials` sits on each character root, instances the materials of its listed renderers (and of runtime renderers passed to `Register`, e.g. leg and tentacle lines) and is the only thing that sets those properties: `SetGlow`, `Flash` (fades out over its duration, dimmed when `GameSettings.Flashes` is off), `SetStatus`. No `MaterialPropertyBlock`s.

## Scenes

- `Scenes/SceneFlow` keeps `Bootstrap` loaded and swaps every other scene: `Load(name)` unloads the rest, loads the scene additively and makes it active (so lighting and runtime spawns land in it), `Reload()` reloads the active scene, `Loaded` fires after. `GameBootstrap` (prefab in the `Bootstrap` scene) calls `Load(MainMenu)` on start.

## Input

- `Assets/InputSystem_Actions.inputactions` is the project-wide actions asset (Project Settings > Input System). Its `Gameplay` map follows the Controls table on Core Mechanics & Controls: `Move`, `AimStick` (gamepad), `AimPointer` (mouse position), `Weapon1`, `Weapon2`, `Dash`, `Grab`, `Ability`. The `UI` map drives UI Toolkit navigation.
- Read gameplay input through `Input/GameplayInput.Default`, never `Keyboard.current` or `Gamepad.current`, so rebinding keeps working. `Default` is rebuilt on every play start and whenever `InputSystem.actions` changes (domain reload is off, so a static wrapper would otherwise outlive a reimport of the actions asset and throw); never cache it in a field across plays. Buttons expose `Pressed`, `Held` and `Released` for tap, hold and charge logic. `LastDevice` says whether the gamepad or keyboard and mouse was used last, for choosing stick or pointer aim.
- Change actions or bindings through the `InputActionSetupExtensions` API in an eval (load with `InputActionAsset.FromJson`, write `ToJson()` back, reimport), not by hand-editing the JSON. `Tests/GameplayInputTests` checks every action keeps a gamepad and a keyboard and mouse binding.

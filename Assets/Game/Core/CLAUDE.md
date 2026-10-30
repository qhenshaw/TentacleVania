# Core

Shared code every area may reference (`TentacleVania.Core`). Keep it free of gameplay rules.

## Character master shader

- `Shaders/CharacterMaster.shadergraph` (built from `CharacterMaster.shadergraph.json` + `.hlsl` with `Tools/shadergraph-build.sh`) is the Lit master for every character: `_BaseMap`, `_BaseColor`, `_Smoothness`, `_EmissionColor`, plus effect channels `_GlowColor`/`_GlowAmount`/`_GlowFresnel`/`_GlowFill` (yellow/red attack telegraphs: the same fresnel rim plus fill as status, without the pulse; `SetGlow(color, amount, fresnel, fill)`, tuned per telegraph colour on `TelegraphGlow`), `_FlashColor`/`_FlashAmount` (hit flash) and `_StatusColor`/`_StatusAmount`/`_StatusPulse`/`_StatusFresnel`/`_StatusFill` (stun, charge full, feedable, Bio-Shield, status effects; developer decision: a fresnel rim `pow(1 - N·V, _StatusFresnel)` (default 3; 0 falls back to 3) whose brightness pulses at `_StatusPulse` Hz when > 0, plus a steady `_StatusFill` (default 0.15) on camera-facing surfaces).
- `Characters/CharacterMaterials` sits on each character root, instances the materials of its listed renderers (and of runtime renderers passed to `Register`, e.g. leg and tentacle lines) and is the only thing that sets those properties: `SetGlow`, `Flash` (fades out over its duration, dimmed when `GameSettings.Flashes` is off), `SetStatus`. No `MaterialPropertyBlock`s. `Register(VisualEffect)` forwards `SetStatus`/`ClearStatus` to an effect's exposed `StatusColor`/`StatusAmount`/`StatusPulse` and, when the graph exposes them, `StatusFresnel`/`StatusFill` (the creature's tentacle strips). `SetStatus(color, amount, pulseHz, fresnel, fill)`: each caller has inspector fields for all of them; glow and flash don't reach effects.

## Tentacle merge (rendering)

- `Rendering/TentacleMergeFeature` (first renderer feature on `PC_Renderer`, before SSAO) smooths deferred normals where creature surfaces overlap so the mass shades as one surface: after the GBuffer it copies `_GBuffer2`, blurs the normals of pixels on the `TentacleMerge` rendering layer at half resolution (depth-aware, `_radius` in metres, `_depthFalloff`, `_strength`) and writes them back before lighting. Shading only: silhouettes and shadows don't merge.
- Creature pixels come from the rendering-layer GBuffer target (`R16_UInt`, written because the RP asset has Use Rendering Layers on), not from re-drawing the renderers: re-drawing the dense VFX strips cost ~1 ms at 1080p. Put a renderer in the merge by adding the `TentacleMerge` rendering layer (the creature's body, maw and VFX renderer have it). It needs the deferred path; in forward it does nothing.
- `Rendering/TentacleMerge.shadergraph` is a Fullscreen graph whose `_MergePass` picks downsample, horizontal blur, vertical blur or composite; the pass keeps one material per stage because a material's values are read when the command buffer runs. The uint rendering-layer texture is a plain HLSL global (`_TentacleMergeLayers`), since graph texture properties are float. `GameSettings.TentacleMerge` turns it off (F5 Creature tab).

## Chains

- `Characters/Chains`: `IChainSource` (`ChainOrder`, `ChainCount`, `WriteChain(chain, Span<Vector4>)`) is how a character's tentacle systems feed the VFX chain texture without the texture knowing their assemblies; `Chains.Write` fills a row with positions and a tapered width, `Collapse` hides one, `Pack` lays sources out as rows (unit tested). `Chains.ShowPlaceholderLines` gates the placeholder `LineRenderer`s.

## Feedback

- `Feedback/FeedbackRunner.cs`: `HitStop.Request(seconds)` drops `Time.timeScale` to 2% of its value and restores it on unscaled time (overlapping requests take the longest; `HitStop.Enabled` is the debug toggle); `Rumble.Play(low, high, seconds)` drives `Gamepad.current` motors, gated by `GameSettings.Rumble` and stopped on focus loss; `ScreenShake.Request(origin, strength)` is an event the Camera area turns into impulses. One hidden-in-play `FeedbackRunner` object is created on first use.

## Scenes

- `Scenes/SceneFlow` keeps `Bootstrap` loaded and swaps every other scene: `Load(name)` unloads the rest, loads the scene additively and makes it active (so lighting and runtime spawns land in it), `Reload()` reloads the active scene, `Loaded` fires after. `GameBootstrap` (prefab in the `Bootstrap` scene) calls `Load(MainMenu)` on start.

## Input

- `Assets/InputSystem_Actions.inputactions` is the project-wide actions asset (Project Settings > Input System). Its `Gameplay` map follows the Controls table on Core Mechanics & Controls: `Move`, `AimStick` (gamepad), `AimPointer` (mouse position), `Weapon1`, `Weapon2`, `Dash`, `Grab`, `Ability`. The `UI` map drives UI Toolkit navigation.
- Read gameplay input through `Input/GameplayInput.Default`, never `Keyboard.current` or `Gamepad.current`, so rebinding keeps working. `Default` is rebuilt on every play start and whenever `InputSystem.actions` changes (domain reload is off, so a static wrapper would otherwise outlive a reimport of the actions asset and throw); never cache it in a field across plays. Buttons expose `Pressed`, `Held` and `Released` for tap, hold and charge logic. `LastDevice` says whether the gamepad or keyboard and mouse was used last, for choosing stick or pointer aim.
- Change actions or bindings through the `InputActionSetupExtensions` API in an eval (load with `InputActionAsset.FromJson`, write `ToJson()` back, reimport), not by hand-editing the JSON. `Tests/GameplayInputTests` checks every action keeps a gamepad and a keyboard and mouse binding.

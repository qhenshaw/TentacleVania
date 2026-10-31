# TentacleVania

A top-down Soulslike Metroidvania rendered in 3D with a 3/4 camera. The player is a writhing alien mass, offspring of a crash-landed alien queen, breaking out of Site Beta, an underground military research base, devouring scientists, soldiers, machines and other specimens on the way down to the deepest vault where its Mother is held. Grotesque body-horror power fantasy; single player, PC (Steam) first with a 60 FPS Steam Deck Verified target, consoles later. Large scope (20+ hours, 12 sectors, 15 bosses) for a team of 2–5, currently in pre-production.

## Working autonomously

Use the design wiki and the Tasks database to drive the work without waiting for instructions:

1. Fix open `S1` and `S2` bugs before starting new tasks.
2. Pick the next `Ready` task from the earliest milestone that still has open tasks, highest `Priority` first.
3. Read its `Design Page`, set it to `In Progress`, and do the work.
4. When it's done, commit and merge it (see [Git workflow](#git-workflow)), set the task to `Done`, and note what changed in `Notes`.
5. When every task in a milestone is `Done` or `In Review`, move the next milestone's `Backlog` tasks to `Ready` and continue.

At the end of each milestone, run `Tools/UnityCheck/unity-build.sh <milestone>` (a Windows development player into `Builds/<milestone>/`, ignored by Git; `--release` for a non-development build; it also zips the build and uploads it to Google Drive, see `unity-build-upload.sh`), then `Tools/UnityCheck/unity-build-smoke.sh <milestone>`, and read its screenshot. File a bug for each build failure, runtime error or visual problem (magenta means a stripped shader).

Stop and report when no unblocked `Ready` tasks remain, or before starting the `Demo: Next Fest` and `Launch` milestones, which are public-facing and need the developer's sign-off.

If a task is blocked, because the design is ambiguous or it needs the developer (buying assets, Steam setup, testing on a Steam Deck), don't guess. Set it to `In Review`, write what's needed in `Notes`, and move on to the next task. When you stop working, list every `In Review` task and what it needs.

If a task can't be finished without purchased assets or the developer's input (common for Art, Audio, Design and Production tasks), build everything possible with placeholders first, then block it the same way, naming the asset or decision needed.

If you find a bug outside the current task, file it in the Bugs database instead of fixing it on the side, then continue.

### Reviewing after a feature

Whenever a feature branch merges into `main`, review the work on it before picking the next task. Look for:

- commands that failed and had to be retried, and why
- steps repeated within the work or across recent tasks
- files read or searched only to find something a note could have pointed to
- editor or CLI round trips a single script could have replaced

Turn each finding into a fix: a short note in the CLAUDE.md file it belongs to, a new or updated script in `Tools/`, or both. Prefer a script for anything that repeats. Document every new script in [Tools](#tools), and commit the review's changes on `main` before pushing (in an agent worktree, on a small branch through `task-finish.sh`).

### Parallel agents

Up to three agents work at once, each in its own Git worktree with its own Unity Editor: the main folder `C:\Repos\Personal\TentacleVania` and the worktrees `C:\Repos\Personal\TentacleVania-<name>` made by `Tools/worktree-add.sh <name>`. The Tools scripts drive the editor of the folder they run from. Work only in your own folder. A worktree's editor rewrites `.vscode/settings.json` (its own solution name; `worktree-add.sh` marks it skip-worktree) and any editor (worktree or main) empties `m_RuntimeSettings` in `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` when it imports or saves that asset: discard that with `git checkout`, never commit it, and change its settings as a text edit followed by a reimport, not through a `SerializedObject`.

- **Claim before you start.** Query the Tasks database just before picking: skip tasks another agent has `In Progress`, and prefer an `Area` no other agent has in progress, so you don't edit the same scenes, prefabs or assets. Set the task `In Progress` and start `Notes` with `Claimed by <folder name>`. If two agents claim the same task, the one whose folder name sorts later backs off.
- **Shared files:** treat `ProjectSettings/`, `Packages/manifest.json`, Build Settings, the input actions, `CLAUDE.md` and shared scenes (`Bootstrap`, `CombatArena`) as contended. Keep changes to them small and merge them promptly. Add a note to an area `CLAUDE.md` next to the line it relates to, not at the end of the file, where every agent appends and the merges conflict.
- **Merging:** `Tools/task-finish.sh` pulls or fetches the latest main before merging, because other agents push to it. On a conflict it stops on your branch: merge `origin/main` into the branch, resolve, run the checks and finish again. In a worktree `main` is checked out in the main folder, so every change goes through a feature branch, and the worktree stays detached on main between tasks.
- **Milestones:** the agent that finds every task of a milestone `Done` or `In Review` does the milestone build and moves the next milestone's tasks to `Ready`. The others continue with Ready tasks.
- Bugs, `In Review` tasks and the developer's decisions live in Notion, so every agent sees them. When you stop, report only your own `In Review` tasks.

## Design wiki (source of truth)

All design documentation and production tracking lives in the Notion **TentacleVania — Game Design Wiki**:
https://app.notion.com/p/407c27b4a69a81c2a9eae82d852ba80a

The game is in pre-production, so design pages still change; read the relevant page at the start of each task rather than relying on an earlier read. Before implementing a feature, follow its design page. If the code needs to diverge from the design, or the design is ambiguous, contradictory or missing, treat the task as blocked (see above) instead of guessing, and don't edit design pages unless asked. Major decisions are logged in Changelog & Decisions.

| Page | Covers |
| --- | --- |
| [Game Overview & Pillars](https://app.notion.com/p/407c27b4a69a8111b92ae4f0d878edac) | Pitch, pillars, tone |
| [Core Mechanics & Controls](https://app.notion.com/p/407c27b4a69a817b9079f45c5375f0e2) | Movement, combat, grab & consume, controls |
| [Progression & Economy](https://app.notion.com/p/407c27b4a69a81eb853ed4c0e379a5b6) | Biomass, nests, leveling, size stages |
| [Balance Sheet (Baselines)](https://app.notion.com/p/407c27b4a69a81a3a20ae43d25e9154f) | Baseline numbers for stats, damage and costs |
| [Onboarding: First 30 Minutes](https://app.notion.com/p/407c27b4a69a8129948bf66339ef5b16) | Opening sequence and teaching order |
| [World, Story & Lore](https://app.notion.com/p/407c27b4a69a8171b168f4992cb20cdd) | Site Beta, sectors, story |
| [Art Direction](https://app.notion.com/p/407c27b4a69a813f880ac6111681046c) | Visual style, creature and enemy design rules |
| [Audio Direction](https://app.notion.com/p/407c27b4a69a819eaaf5ec949d06d194) | Sound and music direction |
| [UI / UX](https://app.notion.com/p/407c27b4a69a816eaca1f8b5cdbc5709) | HUD, menus, map |
| [Technical Design](https://app.notion.com/p/407c27b4a69a811f8a42d9693bb658f9) | Tech stack, procedural creature animation, combat architecture, streaming, saves |
| [Production Plan](https://app.notion.com/p/407c27b4a69a81f3bb98d57c11b0d7d4) | Milestones, their scope and exit criteria |
| [Changelog & Decisions](https://app.notion.com/p/407c27b4a69a81ababf3f9cd3086cc7f) | Major design decisions |
| [Room Dressing Tool](https://app.notion.com/p/408c27b4a69a8168af0be786ab56ce66) | Room dressing tool spec |

Content databases in the wiki: Abilities, Weapons, Mutations, Lore Logs, Areas & Biomes, Characters & NPCs, Enemies & Bosses, Items & Collectibles. Each row is the design for one piece of content.

### Tasks database

`collection://b96f6c4c-16b4-41d9-b275-c32a4821b25a` ([open in Notion](https://app.notion.com/p/7e22bd9a10654b7f9c8c220d21168107))

- Status flow: `Backlog` → `Ready` → `In Progress` → `Done`. `In Review` means blocked, waiting on the developer; `Notes` says what's needed.
- Fields: `Design Page`, `Milestone` (Combat Prototype, Vertical Slice, Demo: Next Fest, Content Complete, Beta, Launch), `Area`, `Discipline`, `Priority` (P0–P3), `Estimate (days)`, `Assignee`, `Notes`, and `ID` (`TV-<n>`).
- The team is 2–5 people: skip tasks with an `Assignee` other than you.
- The Notion connection limits SQL and rows-mode queries per hour; when one fails with `usage_limit_reached`, fetch known task pages by URL (fetch still works) instead of retrying the query.

### Bugs database

`collection://e9d00455-9ccb-4272-a392-40d1b5f8043d` ([open in Notion](https://app.notion.com/p/99f773a36e4142a5a21349681a77891c))

When filing a bug, always fill in `Severity`, `Platform`, `Build`, `Steps to Reproduce`, `Expected` and `Actual`, plus `Location` (sector and room scene) for anything in the world. Link the `Related Task`. `Build` is the commit hash the bug was found on.

When you fix a bug, set it to `Fixed` and note the fix; the developer sets `Verified` after testing. Fix `S3` and `S4` bugs when already working in their area.

| Severity | Meaning |
| --- | --- |
| S1 Crash / Blocker | Crash, save loss, progression blocked, or Steam Deck cert failure |
| S2 Major | Feature broken or badly wrong, but a workaround exists |
| S3 Minor | Noticeable issue with little gameplay impact |
| S4 Cosmetic | Visual or audio polish |

### Asset Ledger database

`collection://beceba08-0b9b-4d51-8e9c-e414d1c4fe13` ([open in Notion](https://app.notion.com/p/2e9840d8a0bb49e9bd06dd0b8332f418))

Every third-party pack, plugin or package gets a row before it's imported: license, commercial use, Steam distribution, modification, attribution, project folder, and what it's used for. Keep `Status` and `Used for` current when a pack's use changes.

## Technical constraints

Summarized from [Technical Design](https://app.notion.com/p/407c27b4a69a811f8a42d9693bb658f9); the wiki page has the full detail.

- **Engine:** Unity 6000.6.6f1 (tech stream; lock the version before content-heavy production and move to the matching LTS before launch), URP, Input System (gamepad-first action maps, remappable), Cinemachine for cameras (3/4 top-down), Addressables for room and asset loading, Localization package for all player-facing text (no hard-coded strings; CJK fonts need full coverage in UI Toolkit).
- **Creature:** mostly procedural. Leg tentacles use IK stepping, free and weapon tentacles use Verlet/spring chains, and attacks follow data-driven tip-path curves. At most 20 chains even at the largest size stage, plus a separate consume maw group of up to 20, each group with its own joint count (2–15 bones; body chains 5, the maw 11); visual density comes from VFX Graph strips along the chains, rendered as tubes, with a custom URP render feature that merges overlapping tentacles. Hitboxes, grabs and attack contact come from the CPU-side chains, never from the VFX strips.
- **Combat:** creature and enemy melee use swept shape casts through the strike window; the creature has one body hurtbox scaled per size stage; projectiles are physics objects; area attacks are overlap checks matching their ground indicators. Status effects, mutations and stat bonuses all go through one event/modifier system. Enemy AI is a code-driven state machine per enemy.
- **World:** every arena and corridor is its own scene, loaded additively through Addressables by distance from the player; fast routes cover loads with a themed transition screen.
- **Saves:** JSON, 5 slots, Steam Cloud, event-based autosave (no manual save; quitting mid-arena resumes at the last nest).
- **Zero allocations during gameplay.** Pool every spawned thing. Use Burst-compiled C# Jobs over `NativeArray`s where it helps (tentacle chain simulation).
- **Data-driven content:** enemies, weapons, abilities, mutations, items and attack paths are ScriptableObjects (edited with Odin Inspector once the developer installs it).
- **Performance budget:** 60 FPS on Steam Deck (16.6 ms). Per-system budgets aren't set yet; propose them in the task that first needs one. Don't try to measure this in the editor; the developer tests on a Steam Deck and files bugs with `Area` set to `Performance`.
- **Platforms:** Windows only for now; Steam Deck runs the Windows build through Proton, so there is no Linux build. Consoles come later, so keep platform-specific code (Steam, file paths) behind small interfaces. No networking.
- **Steam:** Steamworks.NET (`com.rlabrecque.steamworks.net`, pinned to a release tag in `Packages/manifest.json`) for achievements, cloud saves and stats. `steam_appid.txt` at the project root holds `480` (Valve's test app) until the game has its own App ID.
- **Effects:** VFX Graph (`com.unity.visualeffectgraph`) for all effects, never the legacy `ParticleSystem`.
- **Shaders:** Shader Graph. Write the logic in an HLSL file and generate the graph from a `Name.shadergraph.json` spec with `Tools/shadergraph-build.sh`, which wires `<Function>_float` custom function nodes to the outputs (property type `NormalMap` is a texture defaulting to a flat normal; `"transparent": true` adds an `OutAlpha` output and makes the target transparent; `"depthTest": "Always"` sets the ZTest, for effects that must stay visible over geometry; `"target": "Fullscreen"` builds a URP Fullscreen graph whose function gets only `UV0` and outputs colour and alpha, for `FullScreenPassRendererFeature` materials or `Blitter.BlitTexture` in a renderer feature (it already declares `_GBUFFER_NORMALS_OCT`); `"vfx": true` enables VFX Graph support so a VFX output can use the graph; `"normalTS": true` adds an `OutNormalTS` output on Lit graphs; `"target": "Decal"` builds a URP Decal graph affecting albedo and emission only, whose function gets the decal's `UV0` and outputs colour, alpha and emission, for `DecalProjector` materials). Rebuild after changing the spec; the `.shadergraph` is overwritten in place. Property names minus the underscore become HLSL parameter names, so avoid HLSL keywords (`_Pass` failed on D3D12 only).
- **Post-processing:** add effects as new URP renderer features.
- **Not installed yet:** Odin Inspector and Steamworks.NET come from outside the Unity registry, so the developer installs them. Until then, block a task that needs one, after doing everything possible without it. Remove this line once they're in.
- **Packages:** add Unity registry packages the design calls for (such as Timeline) as needed, matching the versions of related installed packages. Any other package, or anything paid, blocks the task for the developer's approval.
- **UI:** UI Toolkit for all runtime UI (see the UI area's CLAUDE.md).
- **Audio:** FMOD (FMOD for Unity 2.03.14 in `Assets/Plugins/FMOD/`, FMOD Studio 2.03.02). The Studio project is in `FMOD/`; its built banks in `FMOD/Build/` are committed.

## Project layout

Scripts and assets are organized by game area under `Assets/Game/<Area>/`, each with its own `CLAUDE.md` covering that area's systems, assets and pitfalls. Add new areas to this list.

Each area has its own assembly definition, `TentacleVania.<Area>`, and the matching namespace. Shared code lives in `Assets/Game/Core/` (`TentacleVania.Core`). Areas reference only the assemblies they need, never each other in a cycle. Editor-only code goes in an `Editor/` folder with its own `TentacleVania.<Area>.Editor` assembly.

| Area | Folder |
| --- | --- |
| Core | `Assets/Game/Core/` (input, scene flow, versioning, diagnostics) |
| Creature | `Assets/Game/Creature/` (player creature: movement, dash, procedural rig) |
| Abilities | `Assets/Game/Abilities/` (active ability slot, charge cost, Bio-Shield) |
| Bosses | `Assets/Game/Bosses/` (boss framework on the enemy state machine: phases, adds, intro, saved kill, ability absorb) |
| Camera | `Assets/Game/Camera/` (Cinemachine gameplay camera, room bounds) |
| Combat | `Assets/Game/Combat/` (health, typed damage pipeline, modifiers, hurtboxes) |
| Enemies | `Assets/Game/Enemies/` (enemy state machine, attacks, telegraphs, NavMesh, tokens) |
| Localization | `Assets/Game/Localization/` (Localization settings, locales, string tables; no code) |
| UI | `Assets/Game/UI/` (UI Toolkit runtime UI, main menu, combat HUD, F5 debug menu) |
| VFX | `Assets/Game/VFX/` (VFX Graph assets and their Shader Graphs, no C#; the creature tentacle effect is fed by `Creature/Rig/ChainTexture`; tentacle tube shader) |
| Weapons | `Assets/Game/Weapons/` (weapon data, shared combo, charged heavy, dash attack, soft-lock) |
| World | `Assets/Game/World/` (rooms, arena doors, lock-in encounters and waves, the editor-only level layout tool) |

Create other areas as their first task needs them, named after the Tasks `Area` values (`Creature`, `Combat`, `Weapons`, `Abilities`, `Enemies`, `Bosses`, `World`, `Progression`, `UI`, `Audio`, `VFX`).

### Scenes

A small `Bootstrap` scene is the entry point: it holds the persistent managers (the `GameBootstrap` prefab) and stays loaded while `Core.Scenes.SceneFlow` swaps the menu and gameplay scenes additively. Load and reload scenes through `SceneFlow.Load` / `Reload`, never `SceneManager.LoadScene`, which would unload `Bootstrap`; when `Bootstrap` isn't loaded (play-check tools on an open scene) it falls back to a single-scene load. It's first in the build list. The game always launches from the main menu: the editor's Play button starts from `Bootstrap` whatever scene is open (`Assets/Editor/PlayFromMainMenu` sets `EditorSceneManager.playModeStartScene`). The play-check tools set `PlayFromMainMenu.Bypass` while they run, so they still play the open scene: to check gameplay, open `CombatArena` and use them; to check the full flow, open `Bootstrap`. Scenes live in `Assets/Game/Scenes/`. Add each scene to this table.

| Scene | Purpose |
| --- | --- |
| `Bootstrap` | Entry point; `GameBootstrap` loads `MainMenu` additively |
| `MainMenu` | Placeholder title screen (`UI/Menu`); Play loads `CombatArena` |
| `LayoutSandbox` | Level layout tool test room built from `SciFiTopDown` (`World/CLAUDE.md`); not in the build |
| `CombatArena` | Greybox lock-in arena with four enemy waves (the Combat Prototype test scene; the creature starts in the south corridor and walking in starts it); the play-check tools open it |

## Code style

- No comments in C# or shaders. Scripts in `Tools/` keep a header comment saying what they do and how to call them.
- `_camelCase` private fields, `[SerializeField] private` for inspector fields, `PascalCase` constants.
- Use target-typed `new()` and expression-bodied members for one-liners.
- Containers passed to a scheduled job need `Allocator.TempJob` or `Persistent`, also in tests; `Allocator.Temp` throws when the job is scheduled.
- Burst, the Jobs API and `Unity.Mathematics` are engine modules in this Unity version, so asmdefs don't reference them; only `Unity.Collections` (for `NativeList` and friends) needs a reference. Test asmdefs reference `UnityEngine.TestRunner` and `UnityEditor.TestRunner` and use `nunit.framework.dll` as a precompiled reference.
- Characters (the creature, enemies, bosses) use materials on the `TentacleVania/CharacterMaster` Shader Graph (`Assets/Game/Core/Shaders/`). Every character glow and effect (telegraphs, hit flash, stun, charge, status effects, low health) goes through the character's `Core.Characters.CharacterMaterials`, which sets the shader's properties directly on its material instances. Never use `MaterialPropertyBlock` (developer rule). Add new effect properties to the master shader rather than making per-effect materials.
- Reference shaders and materials through serialized fields, never `Shader.Find`: player builds strip shaders nothing references. That includes engine defaults picked at runtime, such as the terrain material on a `Terrain` created from code.
- Editor-load hooks use `[OnCodeLoaded]` from `Unity.Scripting.LifecycleManagement`, not `[InitializeOnLoad]`. The containing class must be `partial`, or compilation fails with `UAC0031`.

## Scenes and content

- Build with prefabs and nested prefabs so work lands in separate files and scenes stay small, which avoids merge conflicts. Scenes should mostly hold prefab instances.
- Never place players or characters in scenes. Spawn them at runtime.
- Purchased art and audio packs go in `Assets/ThirdParty/<Pack>/` (create the folder with the first pack; Git can't track it empty) and are never edited, so they can be updated cleanly. Adapt them from `Assets/Game/` with prefab variants, material variants or copies. Code plugins such as FMOD and PrimeTween stay in `Assets/Plugins/`.
- Every new feature gets debug commands in the in-game debug menu (F5 in the editor and development builds; register commands from the feature's own area through `Core.Debugging.DebugMenu`, see `Assets/Game/UI/CLAUDE.md`): register buttons, toggles, values, dropdowns or checklists in the matching tab from the feature's own area, so it can be tested in play without evals.
- Give new features placeholder art, SFX, VFX and animation where it makes sense, so they can be seen and heard in play:
  - **Art:** primitives or simple meshes with a placeholder material.
  - **SFX:** FMOD events, never `AudioSource`. Create them with `Tools/FMOD/fmod-placeholder.sh` (see [Tools](#tools)).
  - **VFX:** VFX Graph.
  - **Animation:** PrimeTween for simple procedural motion.

## Working with the Unity Editor

The Unity Editor is usually open on this project. Drive it with the `unity` CLI instead of reading or hand-editing `.unity`, `.prefab` or `.asset` YAML:

```
unity command eval --caller plugin --skill unity-cli '<C# statements>; return <value>;'
```

- Never read `Library/`, `*.meta` files, scenes or terrain assets directly; get a GUID with `AssetDatabase.AssetPathToGUID`.
- Save any scene you edit with `EditorSceneManager.SaveScene` so the changes can be staged.
- `OpenScene` unloads unused assets, so a C# reference to an asset the same script created or loaded before opening the scene is dead afterwards and assigns as null without an error. Load assets with `AssetDatabase.LoadAssetAtPath` after `OpenScene`, and return the references you set (or grep the saved scene for `{fileID: 0}`) to confirm them.
- For input with exact timing (taps, holds, combos), script it from an eval with a closure on `UnityEditor.EditorApplication.update` that calls `TestGamepad.Set` / `Release` at elapsed `Time.time` marks, and store results with `SessionState`. Separate `--at` files land up to seconds apart, and events queued from `InputSystem.onAfterUpdate` never reach the game.
- In this Unity version `FindFirstObjectByType` and the `FindObjectsByType(FindObjectsSortMode)` overloads are obsolete (game code only warns, evals fail to compile): use `FindAnyObjectByType<T>()` and `FindObjectsByType<T>()`.
- Eval code has no `using` directives, so extension methods don't resolve as methods: call them statically (`InputActionSetupExtensions.AddAction(map, ...)`).
- Don't name a MonoBehaviour method after a Unity message (`Reset`, `Awake`, `OnValidate`...) unless it is that message: a `static Reset()` used for `[RuntimeInitializeOnLoadMethod]` logs "Failed to call static function Reset" whenever the component is added.
- Never use `??` or `?.` on Unity objects (`GetComponent<T>() ?? AddComponent<T>()`): a missing component is a fake null that `??` treats as present. Compare with `== null`.
- Removing or renaming a serialized field leaves its old value in every prefab and asset YAML until that file is saved again: after moving fields, re-save the affected prefabs (`LoadPrefabContents` + `SaveAsPrefabAsset`) so the stale values are dropped, and diff them.
- CLI command names change between CLI versions (for example `editor.playmode start`, `console get`). If a command isn't found, list the current names with `unity command --tag <tag>`.

### Shell pitfalls

- Python's `open(path, 'w')` writes the Windows ANSI code page: pass `encoding='utf-8'` (or write bytes), or non-ASCII text such as `·` breaks UXML parsing. After fixing a UXML that failed to import, reimport it with `ImportAssetOptions.ForceUpdate`, or it can keep loading without its stylesheets.
- Never put C# or comments containing apostrophes or backslashes (`'\\'` char literals come through mangled) in a shell heredoc: the quote breaks it. The same goes for `printf` strings with `\n` escapes. Write the script with the Write tool and run it (`python -I <file>`, or `Tools/UnityCheck/unity-long-eval.sh <file>`).
- Scripts that rewrite a file must keep its line endings: C#, shaders, USS, UXML and Markdown are CRLF in the working tree (`text=auto`). New files written with LF (the Write tool) need no conversion: Git normalizes them on commit.
- Make replacements in C# and shell scripts with the Edit tool, not a Python search-and-replace: backslash sequences (`\n` in C# strings, `\1` and `\{` in sed expressions) made scripted replacements miss their target or write control characters. If an Edit fails to match a line, check it for hidden control characters. The same goes for Markdown: many files here are CRLF, so a Python replace written with `\n` misses them.
- Never put `Tools/task-finish.sh` after other steps on separate lines of one Bash command: a failed step doesn't stop the next line, so it commits half-done edits. Chain with `&&` or run it on its own.
- When timing Burst jobs in the editor, warm up several runs first. Burst safety checks and the job debugger make jobs about 2× slower; for player-like numbers, turn off `BurstCompiler.Options.EnableBurstSafetyChecks` and `JobsUtility.JobDebuggerEnabled` inside a `try`/`finally` that restores them.

### Tools

Use the scripts in `Tools/`, and add new ones for tasks that repeat often. Project-wide values the scripts share (product name, root namespace, test scenes, build upload folder) live in `Tools/project.sh`. The `Tools/UnityCheck/` scripts work while the editor is unfocused. Exit codes: 0 clean, 1 errors found, 2 editor unreachable or the check couldn't run.

- **After editing C# or shaders**, run `Tools/UnityCheck/unity-check.sh` and fix any errors before calling the work done. It refreshes the asset database, waits for compilation, and lists compile and shader errors.
- **Before every commit**, run `Tools/UnityCheck/unity-test.sh` (edit-mode `TentacleVania.*` tests; `--mode PlayMode` and `--filter <text>` are optional; the filter is plain text matched against test names, so `A|B` matches nothing: run one filter per call or the whole suite) and fix failures. Write edit-mode tests for pure logic and data systems (damage pipeline, save versioning, seeded generation, spatial structures), in `Assets/Game/<Area>/Tests/` with a `TentacleVania.<Area>.Tests` assembly. Don't test feel or visuals. Tests must pass when rerun: static state survives between runs in the editor, so look up shared registrations by name instead of assuming they're new. Event subscriptions made at play start (`RuntimeInitializeOnLoadMethod`) also stay hooked in edit mode afterwards, so handlers that create objects or touch play-only APIs (`DontDestroyOnLoad`) must return when `!Application.isPlaying`, or edit-mode tests that raise those events fail.
- **After gameplay changes**, also run `Tools/UnityCheck/unity-play-check.sh [seconds]` (default 10). It plays the open scene and reports any errors or exceptions logged.
- `Tools/UnityCheck/unity-restart.sh [timeout]` force-closes every Unity Editor (all worktrees), reopens this folder's project and waits until it's idle; use it when checks or evals hang on an unresponsive editor. Unsaved scene changes are lost.
- `Tools/UnityCheck/unity-console.sh [--warnings] [--all]` lists console errors logged since its last run, one line per distinct message. Compile errors from earlier `unity-check.sh` passes show up there too until it runs once, so run it before a play check rather than reading old errors as new.
- `Tools/UnityCheck/unity-capture.sh [--at x,y,z | --object Name] [--size m] [--yaw deg] [--pitch deg]` saves a Scene View screenshot under `Temp/Captures/` and prints its path. Read the PNG to check visual changes. It works only in edit mode (the Scene View is gone while `unity-play-capture.sh` plays), and the camera sits behind the pivot, so aim from an open side or stage objects away from the room (e.g. at 200,0,200).
- `Tools/UnityCheck/unity-prefab-capture.sh [--clip <clip or .fbx>] [--time s] [--face-away] [--size m] [--yaw deg] [--pitch deg] <prefab>... [-o out.png]` stages prefabs in a row at 200,0,200 (edit mode, never saved), optionally poses their Animators with a clip, captures the Scene View and removes them. Use it to check character models, skins, props and attack poses without playing.
- `Tools/UnityCheck/unity-play-capture.sh [--frames 150] [--w 960] [--h 540] [--setup file.cs] [--at <frame> file.cs]... [out.png]` plays the open scene for a number of frames, captures the Game view under `Temp/Captures/` and prints its path. Use it to check runtime visuals (spawned content, animation, UI). `--setup` runs a C# eval file at frame 20, e.g. to turn on debug cheats or skip ahead; each `--at` runs another file at a later frame and prints its result, for repros that change state and read it back (private fields through reflection), or to drive input with `TestGamepad` (`Assets/Editor/`: `TestGamepad.Set(move, aim, buttons...)` holds sticks and buttons on a virtual pad, `Release()` lets go, and the pad is removed when play mode exits). Play runs in real time at the editor's 60 FPS cap, so 60 frames is about a second: Player Settings' Run In Background is on (keep it on) and the script sets the editor preference Interaction Mode to No Throttling, which together keep an unfocused editor ticking; if frames still stall it steps a few and unpauses. Each poll takes about a second, so `--at` files and the capture land up to a few hundred frames after their targets; to catch a short-lived state (a heal, a beam), set `Time.timeScale` low in the eval that starts it; check timed flows (transitions, timers) by printing state with `--at` rather than by timing a single capture, and give the Bash call a `timeout` above 120 s for runs over ~1000 frames. Reusable eval files live in `Tools/UnityCheck/evals/` (list new ones here; they take parameters through `UnityEditor.SessionState`, so set those from an earlier `--at` file): `creature-closeup.cs` renders the main camera from `closeupOffset` ("x,y,z" from the creature, or from the object named `closeupTarget`, default 2.2,2,-3) at `closeupFov` into `Temp/Captures/<closeupName>.png`, since the gameplay camera is too far to judge the creature; `press-and-capture.cs` holds `pressButton` (a `GamepadButton` name, default West) for `pressHold` s and renders close-ups at each of the comma-separated `pressShots` times into `<pressName><n>.png`, logging the weapon phase to `pressLog` (weapon taps start on release); `spawn-fight.cs` moves the creature into the `CombatArena` room (enemies spawned in the corridor can't path to the arena NavMesh and never attack) and disables the arena encounter (or its waves join in), spawns `fightPrefab` 6 m away with its AI running, then after `fightSeconds` (default 8) stores every hit it took (time, damage, status), the creature's health and move scale and the enemy's state for `spawn-fight-read.cs` to print; `spawn-feedable.cs` freezes the AI and spawns `feedablePrefab` (default Scientist) 1.6 m in front of the creature under the grab threshold; `merge-ab.cs` renders the same frame with the tentacle merge on and off into `<closeupName>-on/-off/-diff.png` (closeup camera, or the gameplay camera when `closeupOffset` isn't x,y,z) and prints how many pixels changed; `gpu-ab-start.cs` flips the static bool named in `gpuToggle` (default `TentacleVania.Core.GameSettings.TentacleMerge`) every 60 frames for 600 frames and `gpu-ab-read.cs` prints the average GPU frame time on and off, which is how to cost a rendering feature (per-pass `Recorder` GPU times don't split under Render Graph, and Render Graph culls passes whose output nothing reads, so a half-built chain times as free). An eval file that fails to compile prints `ERROR` and the compiler message, and one that throws prints `EXCEPTION` with the exception (the script wraps each `--at` file in a `try`); namespaces follow folders, so grep `^namespace` in the class's file rather than guessing.
- `Tools/UnityCheck/unity-build-smoke.sh <label> [seconds]` runs `Builds/<label>/` windowed with `-logFile`, saves a screenshot after the given seconds (default 15) through `CommandLineCapture` (`Assets/Game/Core/Diagnostics/`) and quits, then lists log errors and prints the screenshot path. The editor play captures can't show build-only problems such as stripped shaders.
- `Tools/UnityCheck/unity-build-upload.sh <label> [--release] [--zip-only]` (run by `unity-build.sh` after every successful build unless it gets `--no-upload`) zips `Builds/<label>/` (without the DoNotShip folders and smoke files) into `Builds/TentacleVania-v0.<commits>[-release].zip`, named from the `version.txt` the build writes (`Assets/Editor/GameVersionBuild`, `Core/GameVersion`), and uploads it to the developer's My Drive `BUILD_UPLOAD_FOLDER` (`Tools/project.sh`): through Google Drive for Desktop when it has a drive mounted (`<letter>:/My Drive`, or `BUILD_DRIVE_DIR`), else `rclone` with a remote named `gdrive` (`BUILD_RCLONE_REMOTE`). Exit 2 means neither is set up; a failed upload doesn't fail the build. The claude.ai Google Drive connector can't carry a build: it takes file content inline as base64.
- `Tools/UnityCheck/unity-build-benchmark.sh <label> [seconds]` runs a build with `-benchmark <seconds> <report>` (the runtime benchmark component isn't built yet: it should set up a repeatable worst-case scene, such as the creature at its largest size stage in `CombatArena` with a full enemy wave, then warm up for 15 s) and prints its report: frame-time average and percentiles, frames over 16.6 ms and, in development builds, GC allocations. Desktop numbers don't stand in for the Deck; the developer runs it there with the Steam launch options `-benchmark 60` (the report is `benchmark.txt` in the save folder). `unity-build.sh` takes `--release` anywhere in its arguments.
- `Tools/UnityCheck/unity-gc-profile.sh [frames]` plays the open scene (`CombatArena`) under the worst-case load set up in `evals/gc-profile-start.cs` with the Profiler on and prints the GC.Alloc samples of ~120 frames grouped by calling markers; run it after gameplay changes to keep allocations at zero (the eval files live in `Tools/UnityCheck/evals/`).
- `Tools/UnityCheck/unity-regression.sh [content|flow|all]` is the release-candidate sweep: `CombatArena` once per content asset (not configured yet, so it skips that part: fill in the setup, report and content hooks in the script once there are weapon or enemy assets to sweep), then `Bootstrap` through the main menu into gameplay; PASS or FAIL per case with the console errors logged. `unity-play-capture.sh` waits up to frames/30 + 120 s, so long runs (use `Time.timeScale = 2` in the setup to halve them) don't time out. Don't edit C# while it runs: the recompile drops the editor connection and fails the remaining cases. Stopping its background task leaves the script's own bash loop running and re-entering play mode; kill it (`ps -ef | grep unity-regression`).
- `Tools/UnityCheck/unity-asset-download.sh <productId>...` downloads purchased Asset Store packs through the open editor (the unity CLI has no download command) into its cache, `C:/Users/qhens/AppData/Roaming/Unity/Asset Store-5.x/<publisher>/...`. `python -I Tools/unitypackage-extract.py <package> --list|--strip <prefix> --dest <Assets/...>|--raw <dir> <regex>...` copies only the matching assets (with their .meta files, so GUIDs match) out of a .unitypackage: purchased packs are often several GB, so import just what the game uses into `Assets/ThirdParty/<Pack>/` and put downscaled texture copies in `Assets/Game/` rather than the 4K originals.
- `Tools/UnityCheck/unity-window-capture.sh <WindowTypeName> [out.png]` saves an open editor window from its own backbuffer. Never capture with `ReadScreenPixel`: it reads the desktop, not the editor.
- `Tools/worktree-add.sh <name>` creates the agent worktree `../TentacleVania-<name>` at the latest `origin/main` and opens it in its own Unity Editor (see [Parallel agents](#parallel-agents)).
- `Tools/shadergraph-build.sh <Name.shadergraph.json>` generates `Name.shadergraph` from its spec and reports shader compile messages. The builder lives in `Assets/Editor/ShaderGraphTool/`; its asmdef is named `Unity.ShaderGraphTool.Editor` because that name has access to Shader Graph internals, so keep the name.
- `Tools/UnityCheck/unity-long-eval.sh <file.cs> [timeout]` runs C# in the editor without the CLI's 30-second limit. The file holds statements ending in `return <value>;`, with fully qualified names and no `using` directives.
- `python -I Tools/apply-edits.py <plan.md> [apply] [multi]` applies a helper agent's Markdown edit plan (one file per `##`/`###` heading, exact `old:`/`new:` code blocks, or a New file / whole file block). It dry-runs first and reports any old text that doesn't match exactly once; `multi` allows replace-all. Ask helpers for that format, quoting current main exactly.
- `Tools/VFX/vfx-placeholder.sh <Assets/.../Name.vfx> [burst|loop] [--float Name]...` creates a placeholder VFX Graph asset from the package's Simple Burst (made one-shot: play it with `Reinit()` + `Play()` and give its prefab an empty `initialEventName`) or Simple Loop template, with optional exposed float properties for the code to set; the developer authors the real effect in it later. It leaves an existing asset untouched.
- `Tools/FMOD/fmod-placeholder.sh <event:/Path/Name> [--type blip|impact|whoosh|loop] [--bank Master] [--no-build]` creates a placeholder FMOD event: a synthesized sound in `FMOD/Assets/Placeholder/`, the event and any missing folders, its bank assignment, a bank build, and a refresh of FMOD's event list in Unity. `loop` events loop until stopped, so use them only for sounds code starts and stops itself (an event instance), never with `PlayOneShot`, where they never end; there is no delete option, so remove a wrong event with an `fmodstudiocl -script` that calls `studio.project.deleteObject(studio.project.lookup(path))` and `studio.project.save()`, and delete its wav. Pass `--no-build` on all but the last of several events. FMOD Studio must be closed; the script refuses to run while it's open. To assign an event to an `EventReference` field from an editor eval, look it up with `FMODUnity.EventManager.EventFromPath(path)` and build `new EventReference { Guid, Path }`; `RuntimeManager.PathToEventReference` only works in play mode. Built banks are binary and can't merge, so any agent may add events, but finish and merge that branch right after building the banks.
- `Tools/FMOD/fmod-swap-sound.sh <event:/Path/Name> <audio file>... [--volume dB] [--layer] [--track n] [--no-build] [--dry-run]` replaces an existing event's sound (placeholder or not) with real audio, keeping its path, banks, loop region and track automation, so code needs no change: one file makes a Single Instrument, several a random Multi Instrument; `--layer` adds them on an extra track instead (a servo under a footstep); `--track n` replaces track n (a music stem). Copy pack files into `FMOD/Assets/<Pack>/` first, or FMOD copies them loose into `FMOD/Assets/`. Same FMOD Studio and bank rules as above; `--dry-run` checks the paths without saving.
- `Tools/FMOD/fmod-music-event.sh <event:/Music/Name> <stem>... [--fades a-b,...] [--bank Master] [--no-build] [--dry-run]` builds a layered looping music event: one track per stem, the global `Intensity` parameter, and volume automation that fades stems 2..n in over their ranges. `python -I Tools/FMOD/placeholder_music.py <dir> <name> <bpm> <root hz> [bars] [stage|boss|endless]` synthesizes four loopable placeholder stems for it.
- `Tools/FMOD/fmod-route-buses.sh [--no-build] [--dry-run]` routes every FMOD event to `bus:/Music` (`event:/Music/...`) or `bus:/SFX` (all others), creating the buses when missing; the volume settings act on them. `fmod-placeholder.sh` and `fmod-music-event.sh` route new events themselves.
- Edit the FMOD project only through the FMOD Studio command-line tool (`fmodstudiocl.exe -script <file.js>` and `-build`), never by writing its XML directly. To make an event loop, add a `LoopRegion` to its timeline; a looping async instrument still ends on its own.

## Git workflow

- Before a larger feature (multi-file or multi-step work, a new system), run `Tools/task-start.sh feature/<short-name>` (clean tree, updated `main`, new branch). Do it before the first edit; if edits already started, or the developer left uncommitted work for the task (a new asset, a prefab change), add `--carry` to stash them, branch and restore them. The open editor can write the stashed-away version of a carried prefab or asset back over the restored one (a carried `Creature.prefab` lost the developer's new child this way), so check `git status` and the carried files' content right before `task-finish.sh`. The same happens without `--carry`: after `task-start.sh` pulls someone's prefab change, the editor can save its older copy over it (`Creature.prefab` lost the developer's VFX tweaks twice). If `git status` lists an asset the task didn't edit, `git checkout` it, reimport it with `ImportAssetOptions.ForceUpdate`, and check again before finishing. Separately, `LoadPrefabContents` on `Creature.prefab` loads its tentacle `VisualEffect` renderers with shadow casting On although the file has Off: an eval that saves that prefab must set them back to `ShadowCastingMode.Off`, then diff the prefab for `m_CastShadows`. Without `--carry` a dirty tree makes it exit 1 and stay on `main`: check its exit code rather than piping it through `tail`. Small fixes stay on the current branch; when that's `main`, pull first and push right after committing (in an agent worktree, use a branch).
- Local-only changes the developer keeps uncommitted (such as a renderer setting tweaked for their machine) go one path per line in `.git/info/keep-local`: `task-finish.sh` never stages them and `task-start.sh` doesn't count them as dirty, so no stashing around commits.
- When a task is done, run `Tools/task-finish.sh "<subject>" ["body"]`: it runs `unity-check.sh`, `unity-test.sh` and `lfs-check.sh --fix`, commits (adding the `Co-Authored-By` trailer itself, so leave it out of the body), merges a feature branch into `main` with `--no-ff`, deletes the branch, and pushes `main`. It stops at the first failure. Give the Bash call a 600 s timeout; while another agent's editor runs a long play check the test run can take longer, and a timed-out call keeps running in the background (wait for it rather than starting a second finish).
- Commit subjects are imperative and name the task by its `ID`, like `Add tentacle IK stepping (TV-12)`.
- After staging and before every commit, run `Tools/lfs-check.sh --fix`. It moves staged files of 10 MB or more that aren't in LFS into LFS, adding a `.gitattributes` rule for each, so pushes to GitHub don't fail on large files. If a push is still rejected for a file in an earlier unpushed commit, move it with `git lfs migrate import --include="<path>"` on the unpushed commits only, never on commits already on GitHub.
- Remote: `github.com/qhenshaw/TentacleVania`.
- No CI (developer decision): builds and checks run locally through `task-finish.sh`, the milestone build and smoke scripts, the benchmark and the regression sweep.
- Scenes, prefabs and assets are text, marked `merge=unityyamlmerge` in `.gitattributes` and merged by Unity Smart Merge. [GitMergeSetup.cs](Assets/Editor/GitMergeSetup.cs) registers the merge driver automatically whenever the project opens in Unity.
- Binary files (images, audio, models, fonts, native libraries, FMOD banks) and generated data (lighting, NavMesh, probe volume `.bytes`, terrain data) go in LFS through `.gitattributes`. LFS matches terrain data by name, so saved `TerrainData` assets must have `TerrainData` in their file name. Commit `.meta` files alongside their assets.

## Keeping CLAUDE.md files current

When a task changes something this file or an area `CLAUDE.md` describes, or should describe (a system added or removed, scenes, input actions, renderer features, packages, a script added to `Tools/`, a workflow rule the developer gave, a pitfall that cost time), update the file it belongs to in the same task and stage it with the rest. Project-wide things go here; a new area also goes in the area list above. Edit the existing lines rather than appending a changelog, keep entries as short as the ones around them, and leave out what the code already shows.

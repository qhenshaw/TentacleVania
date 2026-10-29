# Unity + Claude Code project template

Claude configuration, tooling and editor scripts extracted from MechSurvivors for reuse in a new Unity project. Everything project-specific is a `{{Placeholder}}`; some carry a hint after a colon (`{{SteamAppId: 480 ...}}`). Replace the whole `{{...}}` token. This file is not part of the template; don't copy it.

## Import

1. Copy everything except this file into the new project's root (`CLAUDE.md`, `Tools/`, `Assets/`, `.gitattributes`, `.gitignore`; merge the last two with any existing ones).
2. Fill in `Tools/project.sh`. The shell scripts read their project values from it.
3. Replace the placeholders below. `grep -rn '{{' CLAUDE.md Tools Assets` lists any left; C# and eval files won't compile until theirs are filled.
4. Rename `Tools/task-finish.sh`'s `Co-Authored-By` trailer if the model changes.
5. Open the project in Unity. `GitMergeSetup` registers the Smart Merge driver on load.

## Placeholders

### Tools/project.sh

| Placeholder | Used for |
| --- | --- |
| `{{ProjectName}}` | Player exe name, build zip names, Drive folder, worktree folder prefix |
| `{{RootNamespace}}` | Assembly and namespace prefix; `unity-test.sh` filter; Shader Graph builder namespace |
| `{{GameplayTestScene}}` | Scene name the GC profile and regression sweep open (under `Assets/Game/Scenes/`) |

### C# (same values as above)

| Placeholder | Files |
| --- | --- |
| `{{RootNamespace}}` | `Assets/Editor/GameVersionBuild.cs`, `PlayFromMainMenu.cs`, `ShaderGraphTool/*` (including the asmdef `rootNamespace`), `Assets/Game/Core/GameVersion.cs`, `Assets/Game/Core/Diagnostics/CommandLineCapture.cs` |
| `{{ProjectName}}` | `ShaderGraphBuilder.cs` (the Shader Graph menu path) |

`GameVersion.cs` and `CommandLineCapture.cs` are runtime companions to the tools: `GameVersionBuild` writes the version that `unity-build-upload.sh` names zips with, and `unity-build-smoke.sh` needs `-captureAfter`. They go in the project's Core assembly.

### Gameplay hooks in the check scripts

| Placeholder | File | Fill with |
| --- | --- | --- |
| `{{GcProfileStressSetup}}` | `Tools/UnityCheck/evals/gc-profile-start.cs` | Eval statements that put the scene under worst-case load (invincible player, auto-play, max spawns) |
| `{{RegressionSetup}}` | `Tools/UnityCheck/unity-regression.sh` | Statements run at frame 20 of each content case (the same kind of setup) |
| `{{RegressionReport}}` | same | Statements ending in `return "<status string>";` read near the end of each case |
| `{{RegressionContentGlob}}` | same | Asset glob to sweep, e.g. `Assets/Game/Characters/Data/*.asset`; duplicate the block per content kind |
| `{{RegressionApplyContent}}` | same | Statement that applies `asset` to the open scene (a `SerializedObject` assignment on a spawner) |
| `{{RegressionFlowStart}}` | same | Statement that starts a run from the main menu (e.g. invoke the menu's Play) |
| `{{BenchmarkComponent}}`, `{{BenchmarkScenario}}` | `unity-build-benchmark.sh`, `CLAUDE.md` | The runtime component that handles `-benchmark <seconds> <report>`, and the scene it sets up. Not included: MechSurvivors' `PerfBenchmark` was game-specific |

### CLAUDE.md

| Placeholder | Fill with |
| --- | --- |
| `{{ProjectPitch}}` | One-paragraph game summary |
| `{{PublicMilestones}}` | Milestones that need sign-off before starting |
| `{{MainProjectFolder}}` | Absolute path of the main checkout (worktrees are siblings) |
| `{{DesignWikiUrl}}`, `{{DesignPageTitle}}`, `{{DesignPageUrl}}`, `{{What the page covers}}`, `{{TechnicalDesignUrl}}`, `{{RoadmapUrl}}` | Notion wiki and its pages, one table row per page |
| `{{TasksCollectionId}}`, `{{TasksDatabaseUrl}}`, `{{BugsCollectionId}}`, `{{BugsDatabaseUrl}}`, `{{AssetLedgerCollectionId}}`, `{{AssetLedgerDatabaseUrl}}` | Notion databases (`collection://` IDs from `notion-fetch`); they need the same fields and statuses as before |
| `{{UnityVersion}}`, `{{OtherEngineFeatures}}`, `{{ArchitectureConstraints}}`, `{{ContentTypes}}`, `{{FrameBudgetBreakdown}}`, `{{SteamAppId}}` | Technical constraints |
| `{{Area}}` | Area table rows (add one per `Assets/Game/<Area>/`, each with its own `CLAUDE.md`) |
| `{{The playable test scene ...}}` | Scenes table row |
| `{{DebugMenu}}` | How the in-game debug menu opens and where it's documented |
| `{{PlayCaptureRecipes}}` | Project eval files and shortcuts for `unity-play-capture.sh` |
| `{{RegressionContent}}` | What the regression sweep covers |
| `{{AssetStoreCacheFolder}}` | Unity's Asset Store download cache folder |
| `{{GitRemote}}` | GitHub repository |

## Left out

Game-specific tools that don't carry over: the balance simulator (`unity-balance.sh`, `BalanceReport.cs`), `PlaytestProbe.cs`, `terrain-textures.py`, the vendor and enemy evals, and the 16 area `CLAUDE.md` files. The area convention is kept in `CLAUDE.md`.

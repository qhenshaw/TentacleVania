# Audio

Music (`TentacleVania.Audio`). Design: Audio Direction (ambient dread: silence while exploring, arena layers that rise with the waves and fade after the clear, unique tracks for four bosses and a shared theme for the rest). FMOD events live in `FMOD/`; see the root CLAUDE.md for the `Tools/FMOD/` scripts.

- `MusicDirector` creates itself after the first scene loads (`DontDestroyOnLoad`, play mode only) from `Resources/MusicSettings.asset`, and polls the game each frame. `MusicRules` (unit tested) picks the state, in this order of priority:
  - **Boss** while `BossEnemy.Active` is alive, from its intro on. It plays `BossDefinition.Music`, or the shared `BossTheme`. Intensity goes from 0.5 in the first phase to 1 in the last.
  - **Arena** while `ArenaEncounter.Active` is spawning or fighting. Intensity goes from 0.25 on wave 1 to 1 on the last wave.
  - **NestRest** after `Nests.Rested`, until the creature walks 3 m away.
  - **Silent** otherwise, and while the creature is dead.
- **Tracks:** each fades in over 1.5 s and out over 4 s by instance volume, then is released. Intensity eases at 0.35 a second and decays to 0 once no music plays. It drives the global FMOD parameter `Intensity`, falling back to the instance parameter if it isn't global.
- **Sting:** `event:/Music/Stings/RoomClear` plays when an arena clears.
- **Placeholder events:** stems made with `placeholder_music.py` and built with `fmod-music-event.sh`.
  - `event:/Music/Arena`: 110 bpm, layers fading in at 0.15–0.35, 0.4–0.6 and 0.7–0.9.
  - `event:/Music/BossWarden`: for the Warden Drone, TV-63.
  - `event:/Music/BossTheme`.
  - `event:/Music/NestRest`: a loop.
  Swap in licensed stems with `fmod-swap-sound.sh --track n`.
- `MusicDirector.Muted` silences it (tests).

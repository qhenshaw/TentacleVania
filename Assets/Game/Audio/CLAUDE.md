# Audio

Music and PA/radio voice (`TentacleVania.Audio`). Design: Audio Direction (ambient dread: silence while exploring, arena layers that rise with the waves and fade after the clear, unique tracks for four bosses and a shared theme for the rest). FMOD events live in `FMOD/`; see the root CLAUDE.md for the `Tools/FMOD/` scripts.

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
- `AudioMix` sets the FMOD bus volumes as user volume × duck factor for `bus:/`, `bus:/Music`, `bus:/SFX` and `bus:/VO`. The settings menu should call `SetUserVolume`, never `Bus.setVolume`, or ducking and settings overwrite each other.

## PA and radio (design: Vertical Slice: Script & Audio)

- `Resources/PaScript.asset` (`PaScript`): the PA chime, a placeholder voice event per channel (PA, Radio, Crane), timing (0.055 s per character, at least 2 s, 0.4 s between lines, 0.9 s chime), duck levels (music 0.45, SFX 0.7) and the `PaSequence`s:
  - `sh.entry` (lines 1–3) and `bl.entry` (10–12) are `Manual`, played by a `PaZone`;
  - `sh.warden` (4–5, then Crane 6–9) and `bl.automaton` (13–15, then Crane 19–21) are `BossDefeated`, keyed by boss save id `SH/WardenDrone` and `BL/SurgicalAutomaton` (the boss tasks must use these ids);
  - `relay.1`–`3` (16–18) are `RelayCount` and `PerSector`.
- **Text:** subtitles are in the `Dialogue` string table (`line.01`–`line.21`, `sector.<system>` swapped into the relay lines' `{0}`, `speaker.pa`/`radio`/`crane`).
- **Director:** `PaDirector` (self-created like the music director) queues lines so they never overlap. It plays the chime before a PA line that doesn't follow another PA line, holds each line for the longer of its voice event and its reading time, and ducks music and SFX while anything is queued. It listens to `BossProgress.Defeated` and `SecurityRelays.RelayDestroyed`. `Play(sequence, sector)` saves the sequence in `WorldFlag.PlayedLine` (relay lines as `relay.n@<system>`) and returns false if it already played. `LineStarted` and `LineEnded` drive `UI/Hud/SubtitleView` (hidden when `GameSettings.Subtitles` is off).
- **Zones:** `PaZone` plays its sequence once the creature has stood in its box for `_delay` (sector entries). Place them when the slice rooms exist.
- **FMOD:** `event:/VO/` events go to `bus:/VO` (`fmod-route-buses.sh`). The placeholder `event:/VO/PA/Voice` has an SFX Reverb, and `event:/VO/Radio/Voice` a high-pass at 400 Hz and a low-pass at 3.2 kHz. Recorded lines go in `PaLine.Voice` and need the same effects (or move them onto `bus:/VO` sub-buses).
- **Debug:** F5 Audio tab: music state, Mute music, PA readout, Play PA sequence (clears played lines first), Skip PA lines, Reset played PA lines, Subtitles.

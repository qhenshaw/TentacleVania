# Core

Shared code every area may reference (`TentacleVania.Core`). Keep it free of gameplay rules.

## Input

- `Assets/InputSystem_Actions.inputactions` is the project-wide actions asset (Project Settings > Input System). Its `Gameplay` map follows the Controls table on Core Mechanics & Controls: `Move`, `AimStick` (gamepad), `AimPointer` (mouse position), `Weapon1`, `Weapon2`, `Dash`, `Grab`, `Ability`. The `UI` map drives UI Toolkit navigation.
- Read gameplay input through `Input/GameplayInput.Default`, never `Keyboard.current` or `Gamepad.current`, so rebinding keeps working. Buttons expose `Pressed`, `Held` and `Released` for tap, hold and charge logic. `LastDevice` says whether the gamepad or keyboard and mouse was used last, for choosing stick or pointer aim.
- Change actions or bindings through the `InputActionSetupExtensions` API in an eval (load with `InputActionAsset.FromJson`, write `ToJson()` back, reimport), not by hand-editing the JSON. `Tests/GameplayInputTests` checks every action keeps a gamepad and a keyboard and mouse binding.

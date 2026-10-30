# Camera

Gameplay camera (`TentacleVania.Camera`). Every gameplay scene holds one `Prefabs/GameplayCamera.prefab` (a `Main Camera` with the `CinemachineBrain`, plus a `Virtual Camera`) and one `CameraBounds`.

- Framing (developer decisions): perspective at a 60° pitch, 16 m from the creature, 35° vertical FOV, so about 18 m of floor is visible across the screen at 16:9. It follows the creature with 0.15 s damping and no look-ahead.
- The target comes from `Core.CameraFocus.Target` (set by `CreatureSpawner`); `GameplayCameraRig` snaps to a new target instead of sweeping across the room.
- `CameraBounds` (an XZ rectangle at floor height, drawn as a cyan gizmo) is the room's playable floor. `ConfineToRoom` is disabled on the prefab for now (developer decision: the camera always follows the creature); when on, it keeps the whole visible floor inside it: it projects the view's corners onto the floor (`FloorFraming`, unit tested) and shifts the camera, centring rooms smaller than the view. Size it to each room's walls; the last enabled `CameraBounds` wins, so room streaming will need to switch it on entering a room.
- Shake goes through the `CinemachineImpulseListener` on the virtual camera, gated by `Core.GameSettings.ScreenShake` (the accessibility toggle). Add `CinemachineImpulseSource`s where hits happen.

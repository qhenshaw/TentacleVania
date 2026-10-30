var button = (UnityEngine.InputSystem.LowLevel.GamepadButton)System.Enum.Parse(typeof(UnityEngine.InputSystem.LowLevel.GamepadButton), UnityEditor.SessionState.GetString("pressButton", "West"));
var hold = UnityEditor.SessionState.GetFloat("pressHold", 0.05f);
var shots = UnityEditor.SessionState.GetString("pressShots", "0.15,0.3").Split(',');
var prefix = UnityEditor.SessionState.GetString("pressName", "press");
var start = UnityEngine.Time.time;
var next = 0;
var released = false;
var log = new System.Text.StringBuilder();
UnityEditor.SessionState.SetString("pressLog", "");
TestGamepad.Set(UnityEngine.Vector2.zero, UnityEngine.Vector2.zero, button);
System.Action<string> shoot = shotName =>
{
    var creature = TentacleVania.Creature.CreatureSpawner.Spawned.transform;
    var target = creature.position + UnityEngine.Vector3.up * 0.5f;
    var cam = UnityEngine.Camera.main;
    var oldPos = cam.transform.position;
    var oldRot = cam.transform.rotation;
    var oldFov = cam.fieldOfView;
    var offset = UnityEditor.SessionState.GetString("closeupOffset", "0,4.5,-3.2").Split(',');
    var culture = System.Globalization.CultureInfo.InvariantCulture;
    cam.transform.position = target + new UnityEngine.Vector3(float.Parse(offset[0], culture), float.Parse(offset[1], culture), float.Parse(offset[2], culture));
    cam.transform.LookAt(target);
    cam.fieldOfView = UnityEditor.SessionState.GetFloat("closeupFov", 45f);
    var rt = new UnityEngine.RenderTexture(960, 540, 24);
    cam.targetTexture = rt;
    cam.Render();
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(960, 540, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 960, 540), 0, 0);
    tex.Apply();
    UnityEngine.RenderTexture.active = null;
    cam.targetTexture = null;
    cam.transform.SetPositionAndRotation(oldPos, oldRot);
    cam.fieldOfView = oldFov;
    System.IO.Directory.CreateDirectory("Temp/Captures");
    System.IO.File.WriteAllBytes("Temp/Captures/" + shotName + ".png", UnityEngine.ImageConversion.EncodeToPNG(tex));
    UnityEngine.Object.Destroy(rt);
    UnityEngine.Object.Destroy(tex);
};
UnityEditor.EditorApplication.CallbackFunction tick = null;
tick = () =>
{
    if (!UnityEngine.Application.isPlaying)
    {
        UnityEditor.EditorApplication.update -= tick;
        return;
    }
    var t = UnityEngine.Time.time - start;
    if (!released && t >= hold)
    {
        TestGamepad.Release();
        released = true;
    }
    if (next < shots.Length && t >= float.Parse(shots[next], System.Globalization.CultureInfo.InvariantCulture))
    {
        var weapons = TentacleVania.Creature.CreatureSpawner.Spawned.GetComponent<TentacleVania.Weapons.WeaponController>();
        log.Append(prefix).Append(next).Append(" t=").Append(t.ToString("F2"))
            .Append(" phase=").Append(weapons != null ? weapons.Phase.ToString() : "-").Append("; ");
        UnityEditor.SessionState.SetString("pressLog", log.ToString());
        shoot(prefix + next);
        next++;
    }
    if (next >= shots.Length && released)
        UnityEditor.EditorApplication.update -= tick;
};
UnityEditor.EditorApplication.update += tick;
return "pressing " + button;

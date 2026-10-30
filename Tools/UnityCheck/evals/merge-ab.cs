var name = UnityEditor.SessionState.GetString("closeupName", "merge");
var offset = UnityEditor.SessionState.GetString("closeupOffset", "2.2,2,-3").Split(',');
var creature = TentacleVania.Creature.CreatureSpawner.Spawned.transform;
var target = creature.position + UnityEngine.Vector3.up * 0.5f;
var cam = UnityEngine.Camera.main;
var oldPos = cam.transform.position;
var oldRot = cam.transform.rotation;
var oldFov = cam.fieldOfView;
var oldMerge = TentacleVania.Core.GameSettings.TentacleMerge;
var culture = System.Globalization.CultureInfo.InvariantCulture;
if (offset.Length == 3)
{
    cam.transform.position = target + new UnityEngine.Vector3(float.Parse(offset[0], culture), float.Parse(offset[1], culture), float.Parse(offset[2], culture));
    cam.transform.LookAt(target);
    cam.fieldOfView = UnityEditor.SessionState.GetFloat("closeupFov", 35f);
}
var rt = new UnityEngine.RenderTexture(960, 540, 24);
var shots = new UnityEngine.Texture2D[2];
for (var i = 0; i < 2; i++)
{
    TentacleVania.Core.GameSettings.TentacleMerge = i == 0;
    cam.targetTexture = rt;
    cam.Render();
    UnityEngine.RenderTexture.active = rt;
    shots[i] = new UnityEngine.Texture2D(960, 540, UnityEngine.TextureFormat.RGB24, false);
    shots[i].ReadPixels(new UnityEngine.Rect(0, 0, 960, 540), 0, 0);
    shots[i].Apply();
    UnityEngine.RenderTexture.active = null;
}
cam.targetTexture = null;
cam.transform.SetPositionAndRotation(oldPos, oldRot);
cam.fieldOfView = oldFov;
TentacleVania.Core.GameSettings.TentacleMerge = oldMerge;
var on = shots[0].GetPixels32();
var off = shots[1].GetPixels32();
var diff = new UnityEngine.Color32[on.Length];
var changed = 0;
for (var p = 0; p < on.Length; p++)
{
    var d = System.Math.Abs(on[p].r - off[p].r) + System.Math.Abs(on[p].g - off[p].g) + System.Math.Abs(on[p].b - off[p].b);
    if (d > 6) changed++;
    var v = (byte)System.Math.Min(255, d * 8);
    diff[p] = new UnityEngine.Color32(v, v, v, 255);
}
var diffTex = new UnityEngine.Texture2D(960, 540, UnityEngine.TextureFormat.RGB24, false);
diffTex.SetPixels32(diff);
System.IO.Directory.CreateDirectory("Temp/Captures");
System.IO.File.WriteAllBytes("Temp/Captures/" + name + "-on.png", UnityEngine.ImageConversion.EncodeToPNG(shots[0]));
System.IO.File.WriteAllBytes("Temp/Captures/" + name + "-off.png", UnityEngine.ImageConversion.EncodeToPNG(shots[1]));
System.IO.File.WriteAllBytes("Temp/Captures/" + name + "-diff.png", UnityEngine.ImageConversion.EncodeToPNG(diffTex));
UnityEngine.Object.Destroy(rt);
UnityEngine.Object.Destroy(shots[0]);
UnityEngine.Object.Destroy(shots[1]);
UnityEngine.Object.Destroy(diffTex);
return "Temp/Captures/" + name + "-{on,off,diff}.png, " + changed + " pixels changed";

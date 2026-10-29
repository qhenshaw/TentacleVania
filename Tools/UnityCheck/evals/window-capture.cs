var typeName = "__TYPE__";
UnityEditor.EditorWindow window = null;
foreach (var candidate in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>())
    if (candidate.GetType().Name == typeName || candidate.GetType().FullName == typeName)
        window = candidate;
if (window == null)
    return "ERROR no open window of type " + typeName;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
window.Repaint();
typeof(UnityEditor.EditorWindow).GetMethod("RepaintImmediately", flags)?.Invoke(window, null);
var parent = typeof(UnityEditor.EditorWindow).GetField("m_Parent", flags).GetValue(window);
var scale = UnityEditor.EditorGUIUtility.pixelsPerPoint;
int width = (int)(window.position.width * scale), height = (int)(window.position.height * scale);
var target = new UnityEngine.RenderTexture(width, height, 0);
parent.GetType().GetMethod("GrabPixels", flags).Invoke(parent, new object[] { target, new UnityEngine.Rect(0, 0, width, height) });
UnityEngine.RenderTexture.active = target;
var texture = new UnityEngine.Texture2D(width, height, UnityEngine.TextureFormat.RGB24, false);
texture.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
UnityEngine.RenderTexture.active = null;
var pixels = texture.GetPixels32();
var flipped = new UnityEngine.Color32[pixels.Length];
for (var y = 0; y < height; y++)
    System.Array.Copy(pixels, y * width, flipped, (height - 1 - y) * width, width);
texture.SetPixels32(flipped);
texture.Apply();
UnityEngine.Object.DestroyImmediate(target);
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName("__OUT__"));
System.IO.File.WriteAllBytes("__OUT__", UnityEngine.ImageConversion.EncodeToPNG(texture));
UnityEngine.Object.DestroyImmediate(texture);
return "__OUT__";

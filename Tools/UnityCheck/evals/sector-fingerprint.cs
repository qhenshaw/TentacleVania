var registryPath = UnityEditor.SessionState.GetString("fpRegistry", "Assets/Game/Scenes/SpecimenHolding/SpecimenHoldingRooms.asset");
var outPath = UnityEditor.SessionState.GetString("fpOut", "Temp/fingerprint.txt");
var registry = UnityEditor.AssetDatabase.LoadAssetAtPath<TentacleVania.World.Streaming.RoomRegistry>(registryPath);
var folder = System.IO.Path.GetDirectoryName(registryPath).Replace('\\', '/');
var lines = new System.Collections.Generic.List<string>();
string F(float v) => System.Math.Round(v, 2).ToString(System.Globalization.CultureInfo.InvariantCulture);
string V(UnityEngine.Vector3 v) => F(v.x) + "," + F(v.y) + "," + F(v.z);
foreach (var room in registry.Rooms)
{
    var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(folder + "/" + room.Scene + ".unity", UnityEditor.SceneManagement.OpenSceneMode.Additive);
    var entries = new System.Collections.Generic.List<string>();
    foreach (var root in scene.GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<UnityEngine.Transform>(true))
        {
            var path = t.name;
            for (var p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            var line = room.Scene + " | " + path + " | " + V(t.position) + " | " + F(t.eulerAngles.y) + " | " + V(t.lossyScale) + " |";
            foreach (var c in t.GetComponents<UnityEngine.Component>())
            {
                if (c == null) { line += " MISSING"; continue; }
                line += " " + c.GetType().Name;
                if (c is UnityEngine.MonoBehaviour && c.GetType().Namespace != null && c.GetType().Namespace.StartsWith("TentacleVania"))
                {
                    var it = new UnityEditor.SerializedObject(c).GetIterator();
                    var enter = true;
                    while (it.NextVisible(enter))
                    {
                        enter = false;
                        if (it.propertyPath == "m_Script") continue;
                        string value;
                        switch (it.propertyType)
                        {
                            case UnityEditor.SerializedPropertyType.String: value = it.stringValue; break;
                            case UnityEditor.SerializedPropertyType.Float: value = F(it.floatValue); break;
                            case UnityEditor.SerializedPropertyType.Integer: value = it.intValue.ToString(); break;
                            case UnityEditor.SerializedPropertyType.Boolean: value = it.boolValue.ToString(); break;
                            case UnityEditor.SerializedPropertyType.Vector3: value = V(it.vector3Value); break;
                            case UnityEditor.SerializedPropertyType.Vector2: value = F(it.vector2Value.x) + "," + F(it.vector2Value.y); break;
                            case UnityEditor.SerializedPropertyType.ObjectReference: value = it.objectReferenceValue != null ? it.objectReferenceValue.name : "null"; break;
                            default: value = it.propertyType.ToString(); break;
                        }
                        line += " " + it.propertyPath + "=" + value;
                    }
                }
            }
            entries.Add(line);
        }
    entries.Sort(System.StringComparer.Ordinal);
    lines.AddRange(entries);
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
}
System.IO.File.WriteAllLines(outPath, lines);
return "wrote " + lines.Count + " lines to " + outPath;

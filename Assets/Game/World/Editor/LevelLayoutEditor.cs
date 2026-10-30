using System.Collections.Generic;
using TentacleVania.World.Layout;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace TentacleVania.World.Editor
{
    [CustomEditor(typeof(LevelLayout))]
    public sealed class LevelLayoutEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
                LayoutEditing.RequestRebuild((LevelLayout)target);
            EditorGUILayout.Space();
            LayoutButtons.Draw((LevelLayout)target);
        }
    }

    public static class LayoutButtons
    {
        public static void Draw(LevelLayout layout)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Room"))
                    LayoutEditing.AddShape<LayoutRoom>(layout, "Room");
                if (GUILayout.Button("Hallway"))
                    LayoutEditing.AddShape<LayoutHallway>(layout, "Hallway");
                if (GUILayout.Button("Opening"))
                    LayoutEditing.AddShape<LayoutOpening>(layout, "Opening");
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Rebuild"))
                    LayoutEditing.Rebuild(layout, false);
                if (GUILayout.Button("Rebuild + bake"))
                    LayoutEditing.Rebuild(layout, true);
                if (GUILayout.Button("Reroll all"))
                {
                    var spaces = new List<LayoutSpace>();
                    layout.GetSpaces(spaces);
                    LayoutEditing.Reroll(layout, spaces);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Lock props"))
                    Debug.Log($"Locked {LayoutEditing.LockSelectedProps(layout)} props.");
                if (GUILayout.Button("Unlock props"))
                {
                    Debug.Log($"Unlocked {LayoutEditing.UnlockSelectedProps(layout)} props.");
                    LayoutEditing.Rebuild(layout, false);
                }
                if (GUILayout.Button("Manual group"))
                    Selection.activeGameObject = LayoutGenerator.Child(layout.transform, LevelLayout.ManualName).gameObject;
            }
        }
    }

    [Overlay(typeof(SceneView), "Level Layout", true)]
    public sealed class LevelLayoutOverlay : IMGUIOverlay, ITransientOverlay
    {
        public bool visible => LayoutEditing.Selected() != null;

        public override void OnGUI()
        {
            var layout = LayoutEditing.Selected();
            if (layout == null)
                return;
            GUILayout.Label(layout.name, EditorStyles.boldLabel);
            LayoutButtons.Draw(layout);
        }
    }
}

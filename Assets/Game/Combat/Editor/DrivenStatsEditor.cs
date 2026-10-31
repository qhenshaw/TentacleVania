using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TentacleVania.Combat.Editor
{
    public abstract class DrivenStatsEditor : UnityEditor.Editor
    {
        protected abstract string[] DrivenFields { get; }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var source = FindSource();
            if (source != null)
            {
                var name = source.StatsAsset != null ? source.StatsAsset.name : "its settings asset";
                root.Add(new HelpBox($"Set at runtime from {name} ({source.GetType().Name}); tune it there.", HelpBoxMessageType.Info));
            }
            var property = serializedObject.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
            {
                var field = new PropertyField(property.Copy());
                if (source != null && System.Array.IndexOf(DrivenFields, property.name) >= 0)
                    field.SetEnabled(false);
                root.Add(field);
            }
            return root;
        }

        private IStatsSource FindSource()
        {
            var component = (Component)target;
            foreach (var behaviour in component.GetComponents<MonoBehaviour>())
                if (behaviour is IStatsSource source)
                    return source;
            return null;
        }
    }

    [CustomEditor(typeof(Health))]
    [CanEditMultipleObjects]
    public sealed class HealthEditor : DrivenStatsEditor
    {
        protected override string[] DrivenFields { get; } = { "_team", "_maxHealth", "_maxArmor" };
    }

    [CustomEditor(typeof(Consumable))]
    [CanEditMultipleObjects]
    public sealed class ConsumableEditor : DrivenStatsEditor
    {
        protected override string[] DrivenFields { get; } = { "_healFraction", "_biomass" };
    }
}

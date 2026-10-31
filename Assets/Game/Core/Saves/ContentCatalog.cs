using UnityEngine;

namespace TentacleVania.Core.Saves
{
    [CreateAssetMenu(menuName = "TentacleVania/Core/Content Catalog")]
    public sealed class ContentCatalog : ScriptableObject
    {
        [Tooltip("Every asset a save refers to by name (weapons, abilities, mutations). Names must be unique.")]
        [SerializeField] private ScriptableObject[] _entries = System.Array.Empty<ScriptableObject>();

        public T Find<T>(string id) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(id))
                return null;
            foreach (var entry in _entries)
                if (entry is T match && entry.name == id)
                    return match;
            return null;
        }

        public static string IdOf(Object asset) => asset != null ? asset.name : "";
    }
}

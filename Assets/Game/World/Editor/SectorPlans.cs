using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TentacleVania.World.Editor
{
    public static class SectorPlans
    {
        public static IEnumerable<SectorPlan> All => SectorSpecLoader.SpecPaths().Select(SectorSpecLoader.Load);

        public static SectorPlan For(string sector)
        {
            var path = PathFor(sector);
            return path != null ? SectorSpecLoader.Load(path) : null;
        }

        public static string PathFor(string sector) =>
            SectorSpecLoader.SpecPaths().FirstOrDefault(p => SectorSpecLoader.Read(p).Sector == sector);

        public static string Build(string sector, string onlyScene = null)
        {
            var plan = For(sector);
            return plan != null ? SectorBuilder.Build(plan, onlyScene) : $"No sector spec for {sector} in {SectorSpecLoader.SpecFolder}";
        }

        [MenuItem("TentacleVania/World/Build Selected Sector Spec")]
        private static void BuildSelected()
        {
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!path.EndsWith(SectorSpecLoader.SpecSuffix, System.StringComparison.Ordinal))
            {
                Debug.LogWarning($"Select a *{SectorSpecLoader.SpecSuffix} file in {SectorSpecLoader.SpecFolder} first.");
                return;
            }
            Debug.Log(SectorBuilder.Build(SectorSpecLoader.Load(path)));
        }

        [MenuItem("TentacleVania/World/Build Selected Sector Spec", true)]
        private static bool CanBuildSelected() =>
            AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(SectorSpecLoader.SpecSuffix, System.StringComparison.Ordinal);
    }
}

using UnityEngine.UIElements;

namespace TentacleVania.UI.Hud
{
    public static class StatusPips
    {
        public const int Count = 5;
        private const string PipClass = "status-pip";
        private const string OnClass = "status-pip--on";

        public static VisualElement[] Build(VisualElement parent)
        {
            var pips = new VisualElement[Count];
            for (var i = 0; i < Count; i++)
            {
                pips[i] = new VisualElement { pickingMode = PickingMode.Ignore };
                pips[i].AddToClassList(PipClass);
                parent.Add(pips[i]);
            }
            return pips;
        }

        public static void Set(VisualElement[] pips, int stacks)
        {
            for (var i = 0; i < pips.Length; i++)
                pips[i].EnableInClassList(OnClass, i < stacks);
        }
    }
}

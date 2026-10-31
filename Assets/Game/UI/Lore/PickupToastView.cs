using TentacleVania.Core.Progression;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace TentacleVania.UI.Lore
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class PickupToastView : MonoBehaviour
    {
        private const string Table = "UI";
        private const string ShownClass = "toast--shown";
        private const float ShowTime = 2.5f;

        private VisualElement _toast;
        private Label _label;
        private float _hideAt = float.NegativeInfinity;

        public string LastText { get; private set; }

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.StretchToParentSize();
            root.pickingMode = PickingMode.Ignore;
            _toast = new VisualElement { pickingMode = PickingMode.Ignore };
            _toast.AddToClassList("toast");
            _label = new Label { pickingMode = PickingMode.Ignore };
            _label.AddToClassList("toast__label");
            _toast.Add(_label);
            root.Q("reader").parent.Add(_toast);
            Collectibles.Found += OnFound;
            LoreLogs.Collected += OnLogCollected;
        }

        private void OnDisable()
        {
            Collectibles.Found -= OnFound;
            LoreLogs.Collected -= OnLogCollected;
            _toast?.RemoveFromHierarchy();
        }

        private void Update()
        {
            if (Time.unscaledTime >= _hideAt && _toast.ClassListContains(ShownClass))
                _toast.RemoveFromClassList(ShownClass);
        }

        private void OnFound(PickupKind kind, string itemId, int amount)
        {
            var text = kind switch
            {
                PickupKind.Biomass => string.Format(T("pickup.biomass"), amount),
                PickupKind.Mutagen => string.Format(T("pickup.mutagen"), amount),
                PickupKind.Notch => T("pickup.notch"),
                PickupKind.Weapon => string.Format(T("pickup.weapon"), T("weapon." + itemId + ".name")),
                PickupKind.Mutation => string.Format(T("pickup.mutation"), T("mutation." + Camel(itemId) + ".name")),
                _ => string.Format(T("pickup.trinket"), T("trinket." + itemId + ".name")),
            };
            Show(text);
        }

        private void OnLogCollected(string id) => Show(T("pickup.log"));

        private void Show(string text)
        {
            LastText = text;
            _label.text = text;
            _toast.AddToClassList(ShownClass);
            _hideAt = Time.unscaledTime + ShowTime;
        }

        private static string Camel(string id) => string.IsNullOrEmpty(id) ? id : char.ToLowerInvariant(id[0]) + id.Substring(1);

        private static string T(string key) => LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
    }
}

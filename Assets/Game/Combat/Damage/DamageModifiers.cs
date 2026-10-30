using System.Collections.Generic;

namespace TentacleVania.Combat.Damage
{
    public interface IDamageModifier
    {
        int Order { get; }
        void Modify(ref DamageContext context);
    }

    public sealed class ModifierStack
    {
        private readonly List<IDamageModifier> _modifiers = new(8);

        public int Count => _modifiers.Count;

        public void Add(IDamageModifier modifier)
        {
            if (_modifiers.Contains(modifier))
                return;
            var index = 0;
            while (index < _modifiers.Count && _modifiers[index].Order <= modifier.Order)
                index++;
            _modifiers.Insert(index, modifier);
        }

        public bool Remove(IDamageModifier modifier) => _modifiers.Remove(modifier);

        public void Apply(ref DamageContext context)
        {
            for (var i = 0; i < _modifiers.Count && !context.Cancelled; i++)
                _modifiers[i].Modify(ref context);
        }
    }

    public sealed class ScaleModifier : IDamageModifier
    {
        public ScaleModifier(int order = 0) => Order = order;

        public int Order { get; }
        public float HealthScale { get; set; } = 1f;
        public float ArmorScale { get; set; } = 1f;
        public float StaggerScale { get; set; } = 1f;
        public bool FilterByType { get; set; }
        public DamageType Type { get; set; }

        public void Modify(ref DamageContext context)
        {
            if (FilterByType && context.Info.Type != Type)
                return;
            context.HealthScale *= HealthScale;
            context.ArmorScale *= ArmorScale;
            context.StaggerScale *= StaggerScale;
        }
    }
}

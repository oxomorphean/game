using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;

namespace LinhThu.Domain.Combat
{
    public readonly struct ElementInteraction
    {
        public ElementType Attack { get; }

        public ElementType Defense { get; }

        public float Factor { get; }

        public ElementInteraction(ElementType attack, ElementType defense, float factor)
        {
            Attack = Guard.EnumValue(attack);
            Defense = Guard.EnumValue(defense);
            Factor = Guard.Number(factor, nameof(factor), 0, 100);
        }
    }

    public sealed class ElementInteractionTable
    {
        private readonly Dictionary<(ElementType, ElementType), float> factors = new Dictionary<(ElementType, ElementType), float>();
        public ElementInteractionTable(IEnumerable<ElementInteraction> interactions)
        {
            foreach (var i in interactions ?? throw new ArgumentNullException(nameof(interactions)))
                factors.Add((i.Attack, i.Defense), i.Factor);
        }

        internal IEnumerable<ElementInteraction> Entries => factors.Select(x => new ElementInteraction(x.Key.Item1, x.Key.Item2, x.Value));
        public float Get(ElementType attack, ElementType defense)
        {
            Guard.EnumValue(attack);
            Guard.EnumValue(defense);
            return attack == ElementType.Physical ? 1 : factors.TryGetValue((attack, defense), out var value) ? value : 1;
        }

        public static StatId Affinity(ElementType element)
        {
            switch (element)
            {
                case ElementType.Metal:
                    return StatId.MetalAffinity;
                case ElementType.Wood:
                    return StatId.WoodAffinity;
                case ElementType.Water:
                    return StatId.WaterAffinity;
                case ElementType.Fire:
                    return StatId.FireAffinity;
                case ElementType.Earth:
                    return StatId.EarthAffinity;
                default:
                    throw new ArgumentException("Physical has no affinity.");
            }
        }
    }

    public sealed class CombatRules
    {
        public float DefenseScale { get; }

        public float OffenseScale { get; }

        public float ResistanceScale { get; }

        public float CooldownReductionCap { get; }

        public ElementInteractionTable Elements { get; }

        public CombatRules(ElementInteractionTable elements, float defenseScale = 1, float offenseScale = 1, float resistanceScale = 1, float cooldownReductionCap = .8f)
        {
            Elements = elements ?? throw new ArgumentNullException(nameof(elements));
            DefenseScale = Guard.Number(defenseScale, nameof(defenseScale), 0, 100);
            OffenseScale = Guard.Number(offenseScale, nameof(offenseScale), 0, 100);
            ResistanceScale = Guard.Number(resistanceScale, nameof(resistanceScale), 0, 100);
            CooldownReductionCap = Guard.Number(cooldownReductionCap, nameof(cooldownReductionCap), 0, .99f);
        }
    }
}

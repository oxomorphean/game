using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;
using LinhThu.Domain.Combat;

namespace LinhThu.Domain.Inventory
{
    public enum ItemCategory
    {
        SpiritStone,
        Herb,
        Ore,
        Pill,
        Artifact,
        BeastEgg,
        TechniqueScroll,
        Fish
    }

    public sealed class ItemSpec
    {
        public string DefinitionId { get; }

        public ItemCategory Category { get; }

        public int MaxStack { get; }

        public string EquipmentSlot { get; }

        public IReadOnlyList<StatModifierSpec> Modifiers { get; }

        public IReadOnlyList<AbilityEffectSpec> UseEffects { get; }

        public string TechniqueId { get; }

        public string BeastSpeciesId { get; }
        public string OutfitId { get; }

        public ItemSpec(string definitionId, ItemCategory category, int maxStack = 99, string equipmentSlot = null, IEnumerable<StatModifierSpec> modifiers = null, IEnumerable<AbilityEffectSpec> useEffects = null, string techniqueId = null, string beastSpeciesId = null, string outfitId = null)
        {
            DefinitionId = Guard.Id(definitionId);
            Category = Guard.EnumValue(category);
            if (maxStack < 1 || maxStack > 1000000)
                throw new ArgumentException(nameof(maxStack));
            MaxStack = maxStack;
            EquipmentSlot = equipmentSlot == null ? null : Guard.Id(equipmentSlot);
            Modifiers = Guard.Copy(modifiers ?? Array.Empty<StatModifierSpec>());
            UseEffects = Guard.Copy(useEffects ?? Array.Empty<AbilityEffectSpec>());
            TechniqueId = techniqueId == null ? null : Guard.Id(techniqueId);
            BeastSpeciesId = beastSpeciesId == null ? null : Guard.Id(beastSpeciesId);
            OutfitId = outfitId == null ? null : Guard.Id(outfitId);
            if (OutfitId != null && category != ItemCategory.Artifact)
                throw new ArgumentException("Only equipment can change an outfit.");
            if (UseEffects.Any(x => x == null) || (category == ItemCategory.Artifact && EquipmentSlot == null) || (category == ItemCategory.TechniqueScroll && TechniqueId == null) || (category == ItemCategory.BeastEgg && BeastSpeciesId == null))
                throw new ArgumentException("Incomplete item definition.");
            foreach (var modifier in Modifiers)
                modifier.FromSource(1);
        }
    }

    public readonly struct ItemStack
    {
        public string DefinitionId { get; }

        public int Count { get; }

        public ItemStack(string definitionId, int count)
        {
            DefinitionId = Guard.Id(definitionId);
            if (count <= 0)
                throw new ArgumentException(nameof(count));
            Count = count;
        }
    }
}

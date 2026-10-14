using System;
using System.Linq;
using LinhThu.Domain.Inventory;
using UnityEngine;

namespace LinhThu.Client.Configs
{
    [CreateAssetMenu(menuName = "Linh Thu/Item")]
    public sealed class ItemConfigSO : ContentConfigSO
    {
        public ItemCategory category;
        [Min(1)]
        public int maxStack = 99;
        public string equipmentSlot;
        public Sprite icon;
        public OutfitConfigSO outfit;
        public StatModifierData[] modifiers = Array.Empty<StatModifierData>();
        [SerializeReference]
        public AbilityEffectData[] useEffects = Array.Empty<AbilityEffectData>();
        public TechniqueConfigSO technique;
        public BeastSpeciesConfigSO beastSpecies;
        public ItemSpec ToSpec() => new ItemSpec(Id, category, maxStack, string.IsNullOrWhiteSpace(equipmentSlot) ? null : equipmentSlot, modifiers.Select(m => m.ToSpec()), useEffects.Select(e => e != null ? e.ToSpec() : throw new InvalidOperationException(name + " has a null effect.")), technique != null ? technique.Id : null, beastSpecies != null ? beastSpecies.Id : null, outfit != null ? outfit.appearanceId : null);
    }
}

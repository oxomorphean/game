using System;
using System.Linq;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Inventory;
using UnityEngine;

namespace LinhThu.Client.Configs
{
    [CreateAssetMenu(menuName = "Linh Thu/Wild Beast")]
    public sealed class WildBeastConfigSO : ContentConfigSO
    {
        public ActorBaseConfigSO actor;
        public BeastSpeciesConfigSO tameSpecies;
        public TechniqueConfigSO attack;
        public ActorFaction faction = ActorFaction.WildBeast;
        public float aggroRadius = 5, leashRadius = 8, patrolRadius = 2, respawnSeconds = 15;
        [Range(.01f, 1)] public float tameHpRatio = .3f;
        public float tameRange = 2;
        public int experience = 25;
        public ItemAmountData[] loot = Array.Empty<ItemAmountData>();
        public WildBeastSpec ToSpec() => new WildBeastSpec(Id, ConfigData.Required(actor, "wild actor").Id,
            tameSpecies != null ? tameSpecies.Id : null, ConfigData.Required(attack, "wild attack").Id,
            loot.Select(i => new ItemStack(ConfigData.Required(i.item, "wild loot").Id, i.count)),
            experience, aggroRadius, leashRadius, patrolRadius, respawnSeconds, tameHpRatio, tameRange, faction);
    }
}

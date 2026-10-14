using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Client.Configs;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Core;
using LinhThu.Domain.World;
using UnityEngine;

namespace LinhThu.Client.Bootstrap
{
    [Serializable]
    public struct ElementRuleData
    {
        public ElementType attack, defense;
        public float factor;
    }

    [Serializable]
    public struct FactionRuleData
    {
        public ActorFaction from, to;
    }

    [CreateAssetMenu(menuName = "Linh Thu/Content Registry")]
    public sealed class ContentRegistrySO : ScriptableObject
    {
        public ActorBaseConfigSO[] actors = Array.Empty<ActorBaseConfigSO>();
        public RealmConfigSO[] realms = Array.Empty<RealmConfigSO>();
        public TechniqueConfigSO[] techniques = Array.Empty<TechniqueConfigSO>();
        public StatusEffectConfigSO[] statuses = Array.Empty<StatusEffectConfigSO>();
        public BeastSpeciesConfigSO[] beasts = Array.Empty<BeastSpeciesConfigSO>();
        public ItemConfigSO[] items = Array.Empty<ItemConfigSO>();
        public WorldNodeConfigSO[] nodes = Array.Empty<WorldNodeConfigSO>();
        public OutfitConfigSO[] outfits = Array.Empty<OutfitConfigSO>();
        public ActorBaseConfigSO player;
        public RealmConfigSO startingRealm;
        public BeastSpeciesConfigSO startingBeast;
        public ItemConfigSO startingOutfit;
        public TechniqueConfigSO[] hotbar = Array.Empty<TechniqueConfigSO>();
        public ItemAmountData[] startingItems = Array.Empty<ItemAmountData>();
        public WorldNodeConfigSO fieldCrop;
        public string mapDefinitionId = "meadow";
        public int inventoryCapacity = 24;
        public uint randomSeed = 7;
        public float defenseScale = 1, offenseScale = 1, resistanceScale = 1, cooldownReductionCap = .8f;
        public ElementRuleData[] elementRules = Array.Empty<ElementRuleData>();
        public FactionRuleData[] factionRules = Array.Empty<FactionRuleData>();
        public CombatRules CreateRules() => new CombatRules(new ElementInteractionTable(elementRules.Select(r => new ElementInteraction(r.attack, r.defense, r.factor))), defenseScale, offenseScale, resistanceScale, cooldownReductionCap);
        public FactionRelations CreateRelations() => new FactionRelations(factionRules.Select(r => new FactionPair(r.from, r.to)));
        public IReadOnlyList<BeastSeed> CreateBeastSeeds() => startingBeast == null ? Array.Empty<BeastSeed>() : new[]{new BeastSeed(startingBeast.Id, startingBeast.initialQualityId, startingBeast.initialBloodlineId, startingBeast.initialBond)};
        public DomainContent BuildContent()
        {
            var all = actors.Cast<ContentConfigSO>().Concat(realms).Concat(techniques).Concat(statuses).Concat(beasts).Concat(items).Concat(nodes).Concat(outfits).ToArray();
            if (all.Any(c => c == null) || all.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count() != all.Length)
                throw new InvalidOperationException("Registry contains missing configs or duplicate IDs.");
            var included = new HashSet<ContentConfigSO>(all);
            void Reference(ContentConfigSO value, string context)
            {
                if (value == null || !included.Contains(value))
                    throw new InvalidOperationException("Registry missing " + context);
            }

            void Effects(IEnumerable<AbilityEffectData> effects)
            {
                foreach (var effect in effects)
                {
                    if (effect == null)
                        throw new InvalidOperationException("Null effect in registry.");
                    if (effect is ApplyStatusEffectData status)
                        Reference(status.status, "status effect");
                }
            }

            Reference(player, "player");
            Reference(startingRealm, "starting realm");
            Reference(startingOutfit, "starting outfit");
            Reference(fieldCrop, "field crop");
            if (startingBeast != null)
                Reference(startingBeast, "starting beast");
            foreach (var t in hotbar)
            {
                Reference(t, "hotbar technique");
                if (!player.abilities.Contains(t))
                    throw new InvalidOperationException("Player does not know hotbar technique.");
            }

            foreach (var a in actors)
                foreach (var t in a.abilities)
                    Reference(t, "actor technique");
            foreach (var r in realms)
                if (r.nextRealm != null)
                    Reference(r.nextRealm, "next realm");
            foreach (var t in techniques)
                Effects(t.effects);
            foreach (var s in statuses)
                Effects(s.tickEffects);
            foreach (var b in beasts)
            {
                Reference(b.actor, "beast actor");
                foreach (var t in b.assistTechniques)
                {
                    Reference(t, "assist technique");
                    if (!b.actor.abilities.Contains(t))
                        throw new InvalidOperationException("Beast does not know assist technique.");
                }
            }

            foreach (var i in items)
            {
                if (i.technique != null)
                    Reference(i.technique, "scroll technique");
                if (i.beastSpecies != null)
                    Reference(i.beastSpecies, "egg species");
                if (i.outfit != null)
                    Reference(i.outfit, "outfit");
                Effects(i.useEffects);
            }

            foreach (var n in nodes)
            {
                foreach (var r in n.rewards)
                    Reference(r.item, "node reward");
                if (n.seedItem != null)
                    Reference(n.seedItem, "crop seed");
            }

            foreach (var i in startingItems)
                Reference(i.item, "starting inventory");
            foreach (var o in outfits)
                o.ValidateSlots();
            var content = new DomainContent();
            foreach (var s in statuses)
                content.Add(s.ToSpec());
            foreach (var t in techniques)
                content.Add(t.ToSpec());
            foreach (var a in actors)
                content.Add(a.ToSpec());
            foreach (var r in realms)
                content.Add(r.ToSpec());
            foreach (var b in beasts)
                content.Add(b.ToSpec());
            foreach (var i in items)
                content.Add(i.ToSpec());
            foreach (var n in nodes)
                content.Add(n.ToSpec());
            return content;
        }

        public ActorBaseConfigSO Actor(string id) => actors.FirstOrDefault(c => c.Id == id) ?? throw new InvalidOperationException("No actor config: " + id);
        public ItemConfigSO Item(string id) => items.FirstOrDefault(c => c.Id == id) ?? throw new InvalidOperationException("No item config: " + id);
        public TechniqueConfigSO Technique(string id) => techniques.FirstOrDefault(c => c.Id == id) ?? throw new InvalidOperationException("No technique config: " + id);
        public RealmConfigSO Realm(string id) => realms.FirstOrDefault(c => c.Id == id) ?? throw new InvalidOperationException("No realm config: " + id);
    }
}

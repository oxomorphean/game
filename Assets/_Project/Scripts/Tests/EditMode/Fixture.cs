using System;
using System.Collections.Generic;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Stats;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Combat.Effects;
using LinhThu.Domain.Cultivation;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.World;
using LinhThu.Domain.Navigation;

namespace LinhThu.Tests.EditMode
{
    internal static class Fixture
    {
        internal static DomainContent Content()
        {
            var c = new DomainContent();
            var stats = new Dictionary<StatId, float>{{StatId.MaxHp, 200}, {StatId.MaxQi, 20}, {StatId.Attack, 20}, {StatId.MoveSpeed, 4}, {StatId.BeastCommandCap, 1}, {StatId.BeastStatLinkRatio, .1f}, {StatId.CritDamage, 2}};
            c.Add(new ActorSpec("player", ElementType.Wood, stats, new[]{"strike", "projectile", "channel", "windup"}, appearance: new AppearanceSpec(new ActorAppearance("classic", "ink", "amber"), new[] { "classic", "jade", "basic" }, new[] { "ink", "silver" }, new[] { "amber", "jade" }), lifeActivities: new LifeActivitySpec()));
            c.Add(new ActorSpec("target", ElementType.Wood, stats, Array.Empty<string>()));
            c.Add(new ActorSpec("turtle-actor", ElementType.Water, stats, new[]{"strike"}));
            c.Add(new RealmSpec("qi", "foundation", new long[]{0, 20, 50, 100}, 100, new[]{new StatModifierSpec(StatId.Attack, ModifierType.FlatAdd, 5)}));
            c.Add(new RealmSpec("foundation", null, new long[]{0, 50, 150, 300}, 300, new[]{new StatModifierSpec(StatId.Attack, ModifierType.FlatAdd, 10)}));
            var damage = new AbilityEffectSpec[]{new DamageEffectSpec(StatId.Attack, 1, 0, ElementType.Physical)};
            c.Add(new TechniqueSpec("strike", new DeliverySpec(DeliveryShape.LineRay, 4, .3f), damage, 2, 1));
            c.Add(new TechniqueSpec("projectile", new DeliverySpec(DeliveryShape.Projectile, 10, .1f, projectileSpeed: 20, maxTargets: 3), damage, 1, 1));
            c.Add(new TechniqueSpec("channel", new DeliverySpec(DeliveryShape.LineRay, 5, .2f), damage, 1, 1, .1f, .4f, .1f));
            c.Add(new TechniqueSpec("windup", new DeliverySpec(DeliveryShape.LineRay, 5, .2f), damage, 1, 1, .3f));
            foreach (DeliveryShape shape in Enum.GetValues(typeof(DeliveryShape)))
                c.Add(new TechniqueSpec("shape-" + shape, new DeliverySpec(shape, 4, 1, projectileSpeed: 20), shape == DeliveryShape.Self ? new AbilityEffectSpec[]{new DisplacementEffectSpec(1, DisplacementTarget.Caster)} : damage));
            c.Add(new StatusEffectSpec("burn", 3, 1, 2, StatusStackPolicy.AddStacksRefreshDuration, new[]{new StatModifierSpec(StatId.MoveSpeed, ModifierType.FlatAdd, -1)}, new AbilityEffectSpec[]{new DamageEffectSpec(StatId.Attack, 0, 2, ElementType.Physical)}));
            c.Add(new StatusEffectSpec("quick", .06f, .02f, 1, StatusStackPolicy.RefreshDuration, Array.Empty<StatModifierSpec>(), new AbilityEffectSpec[]{new DamageEffectSpec(StatId.Attack, 0, 1, ElementType.Physical)}));
            c.Add(new BeastSpeciesSpec("turtle", "turtle-actor", new BeastAiSpec(1, 8, 5, 2, new[]{"strike"}), new[]{StatId.Attack}));
            c.Add(new ItemSpec("herb", ItemCategory.Herb));
            c.Add(new ItemSpec("ore", ItemCategory.Ore));
            c.Add(new ItemSpec("seed", ItemCategory.Herb));
            c.Add(new ItemSpec("robe", ItemCategory.Artifact, 1, "body", new[]{new StatModifierSpec(StatId.Attack, ModifierType.FlatAdd, 3)}, outfitId: "jade"));
            c.Add(new ItemSpec("pill", ItemCategory.Pill, useEffects: new AbilityEffectSpec[]{new RestoreVitalEffectSpec(VitalType.Hp, 20)}));
            c.Add(new ItemSpec("scroll", ItemCategory.TechniqueScroll, techniqueId: "strike"));
            c.Add(new ItemSpec("egg", ItemCategory.BeastEgg, beastSpeciesId: "turtle"));
            c.Add(new WorldNodeSpec("herb-node", WorldNodeKind.HerbNode, new[]{new ItemStack("herb", 2)}, seedItemId: "seed"));
            c.Add(new WorldNodeSpec("ore-node", WorldNodeKind.OreNode, new[]{new ItemStack("ore", 1)}));
            c.Add(new WorldNodeSpec("field", WorldNodeKind.SpiritFieldPlot, Array.Empty<ItemStack>()));
            c.Add(new WorldNodeSpec("egg-node", WorldNodeKind.SpiritEggNode, new[]{new ItemStack("egg", 1)}, respawnSeconds: 0));
            c.Add(new WorldNodeSpec("portal", WorldNodeKind.TeleportPortal, Array.Empty<ItemStack>(), destinationMapId: "meadow", portalDestination: new WorldPoint(5, 0)));
            return c;
        }

        internal static WorldState World(WorldGeometry geometry = null, DomainContent content = null, int capacity = 16, IEnumerable<NodeSpawn> nodes = null, IEnumerable<BeastSeed> beasts = null, string realmId = "qi")
        {
            var rules = new CombatRules(new ElementInteractionTable(new[]{new ElementInteraction(ElementType.Fire, ElementType.Wood, 1.2f)}));
            var relations = new FactionRelations(new[]{new FactionPair(ActorFaction.Player, ActorFaction.HostileMonster), new FactionPair(ActorFaction.PlayerBeast, ActorFaction.HostileMonster), new FactionPair(ActorFaction.HostileMonster, ActorFaction.Player), new FactionPair(ActorFaction.HostileMonster, ActorFaction.PlayerBeast), new FactionPair(ActorFaction.Player, ActorFaction.WildBeast), new FactionPair(ActorFaction.PlayerBeast, ActorFaction.WildBeast), new FactionPair(ActorFaction.WildBeast, ActorFaction.Player), new FactionPair(ActorFaction.WildBeast, ActorFaction.PlayerBeast)});
            return WorldFactory.Create(content ?? Content(), geometry ?? new WorldGeometry(new WorldPoint(20, 20), Array.Empty<WorldObstacle>()), new SeededRandomSource(7), new DomainEventBus(), new WorldInitialState("player", realmId, "meadow", default, capacity, rules, relations, nodes ?? new[]{new NodeSpawn("herb-node", new WorldPoint(1, 0)), new NodeSpawn("ore-node", new WorldPoint(0, 1)), new NodeSpawn("field", new WorldPoint(-1, 0)), new NodeSpawn("portal", new WorldPoint(0, -1))}, beasts));
        }

        internal static BeastInstance Beast(WorldState w, BeastState state = BeastState.Resting)
        {
            var b = new BeastInstance(w.Ids.Allocate(), w.Player.ActorId, "turtle", "common", "ancient", .5f, state);
            var result = w.Player.Beasts.Add(b);
            if (!result.Succeeded)
                throw new Exception(result.ErrorCode.ToString());
            return b;
        }

        internal static void Tick(WorldState w, float total)
        {
            int ticks = (int)Math.Floor((double)total / .1 + .000001);
            for (int i = 0; i < ticks; i++)
                w.Tick(.1f);
            float rest = total - ticks * .1f;
            if (rest > .000001f)
                w.Tick(rest);
        }
    }
}

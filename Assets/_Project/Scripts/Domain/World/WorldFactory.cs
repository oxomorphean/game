using System;
using System.Collections.Generic;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Combat.Effects;
using LinhThu.Domain.Combat.Targeting;
using LinhThu.Domain.Beasts;

namespace LinhThu.Domain.World
{
    public readonly struct NodeSpawn
    {
        public string DefinitionId { get; }

        public WorldPoint Position { get; }

        public bool Ready { get; }

        public NodeSpawn(string id, WorldPoint position, bool ready = true)
        {
            DefinitionId = Guard.Id(id);
            Position = position;
            Ready = ready;
        }
    }

    public readonly struct BeastSeed
    {
        public string DefinitionId { get; }

        public string QualityId { get; }

        public string BloodlineId { get; }

        public float Bond { get; }

        public BeastState State { get; }

        public BeastSeed(string definitionId, string qualityId, string bloodlineId, float bond, BeastState state = BeastState.Resting)
        {
            DefinitionId = Guard.Id(definitionId);
            QualityId = Guard.Id(qualityId);
            BloodlineId = Guard.Id(bloodlineId);
            Bond = Guard.Number(bond, nameof(bond), 0, 1);
            if (state != BeastState.InEgg && state != BeastState.Resting)
                throw new ArgumentException("Initial beasts must be eggs or resting.");
            State = state;
        }
    }

    public sealed class WorldInitialState
    {
        public string PlayerDefinitionId { get; }

        public string RealmDefinitionId { get; }

        public string MapDefinitionId { get; }

        public WorldPoint PlayerSpawn { get; }

        public int InventoryCapacity { get; }

        public CombatRules Rules { get; }

        public FactionRelations Relations { get; }

        public IReadOnlyList<NodeSpawn> Nodes { get; }

        public IReadOnlyList<BeastSeed> Beasts { get; }

        public WorldInitialState(string playerDefinitionId, string realmDefinitionId, string mapDefinitionId, WorldPoint playerSpawn, int inventoryCapacity, CombatRules rules, FactionRelations relations, IEnumerable<NodeSpawn> nodes = null, IEnumerable<BeastSeed> beasts = null)
        {
            PlayerDefinitionId = Guard.Id(playerDefinitionId);
            RealmDefinitionId = Guard.Id(realmDefinitionId);
            MapDefinitionId = Guard.Id(mapDefinitionId);
            if (!playerSpawn.Finite || inventoryCapacity < 1 || inventoryCapacity > 10000)
                throw new ArgumentException("Invalid initial state.");
            PlayerSpawn = playerSpawn;
            InventoryCapacity = inventoryCapacity;
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Relations = relations ?? throw new ArgumentNullException(nameof(relations));
            Nodes = Guard.Copy(nodes ?? Array.Empty<NodeSpawn>());
            Beasts = Guard.Copy(beasts ?? Array.Empty<BeastSeed>());
        }
    }

    public static class WorldFactory
    {
        public static AbilityEffectRegistry CreateEffects()
        {
            var r = new AbilityEffectRegistry();
            r.Register(new DamageEffect());
            r.Register(new ApplyStatusEffect());
            r.Register(new DisplacementEffect());
            r.Register(new RestoreVitalEffect());
            return r;
        }

        public static TargetSelectorRegistry CreateTargeting()
        {
            var r = new TargetSelectorRegistry();
            r.Register(new CircleTargetSelector());
            r.Register(new ConeTargetSelector());
            r.Register(new LineTargetSelector());
            r.Register(new ProjectileTargetSelector());
            r.Register(new SelfTargetSelector());
            return r;
        }

        public static WorldState Create(IDomainContent content, WorldGeometry geometry, IRandomSource random, DomainEventBus events, WorldInitialState initialState) => new WorldState(content, geometry, random, events, initialState, CreateEffects(), CreateTargeting());
    }
}

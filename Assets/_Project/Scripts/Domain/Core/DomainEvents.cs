using System.Collections.Generic;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.Navigation;

namespace LinhThu.Domain.Core
{
    public readonly struct ActorDamagedEvent
    {
        public EntityId TargetId { get; }

        public EntityId SourceId { get; }

        public float HpLost { get; }

        public float ShieldLost { get; }

        public ActorDamagedEvent(EntityId targetId, EntityId sourceId, float hpLost, float shieldLost)
        {
            TargetId = targetId;
            SourceId = sourceId;
            HpLost = hpLost;
            ShieldLost = shieldLost;
        }
    }

    public readonly struct ActorDiedEvent
    {
        public EntityId ActorId { get; }

        public EntityId KillerId { get; }

        public ActorDiedEvent(EntityId actorId, EntityId killerId)
        {
            ActorId = actorId;
            KillerId = killerId;
        }
    }

    public readonly struct AbilityExecutedEvent
    {
        public EntityId CasterId { get; }

        public string TechniqueId { get; }

        public AbilityExecutedEvent(EntityId casterId, string techniqueId)
        {
            CasterId = casterId;
            TechniqueId = techniqueId;
        }
    }

    public readonly struct NodeHarvestedEvent
    {
        public EntityId ActorId { get; }

        public EntityId NodeId { get; }

        public IReadOnlyList<ItemStack> Rewards { get; }

        public NodeHarvestedEvent(EntityId actorId, EntityId nodeId, IReadOnlyList<ItemStack> rewards)
        {
            ActorId = actorId;
            NodeId = nodeId;
            Rewards = Guard.Copy(rewards);
        }
    }

    public readonly struct RealmBreakthroughEvent
    {
        public EntityId ActorId { get; }

        public string FromRealmId { get; }

        public string ToRealmId { get; }

        public RealmBreakthroughEvent(EntityId actorId, string from, string to)
        {
            ActorId = actorId;
            FromRealmId = from;
            ToRealmId = to;
        }
    }

    public readonly struct BeastTamedEvent
    {
        public EntityId OwnerId { get; }

        public EntityId BeastId { get; }

        public BeastTamedEvent(EntityId owner, EntityId beast)
        {
            OwnerId = owner;
            BeastId = beast;
        }
    }

    public readonly struct BeastSummonedEvent
    {
        public EntityId OwnerId { get; }

        public EntityId BeastId { get; }

        public EntityId ActorId { get; }

        public BeastSummonedEvent(EntityId owner, EntityId beast, EntityId actor)
        {
            OwnerId = owner;
            BeastId = beast;
            ActorId = actor;
        }
    }

    public readonly struct BeastRecalledEvent
    {
        public EntityId OwnerId { get; }

        public EntityId BeastId { get; }

        public BeastRecalledEvent(EntityId owner, EntityId beast)
        {
            OwnerId = owner;
            BeastId = beast;
        }
    }

    public readonly struct PortalTravelRequestedEvent
    {
        public EntityId ActorId { get; }

        public string MapId { get; }

        public WorldPoint Position { get; }

        public PortalTravelRequestedEvent(EntityId actorId, string mapId, WorldPoint position)
        {
            ActorId = actorId;
            MapId = mapId;
            Position = position;
        }
    }
}

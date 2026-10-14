using LinhThu.Domain.Core;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.Cultivation;
using LinhThu.Domain.Beasts;

namespace LinhThu.Domain.World
{
    public sealed class PlayerState
    {
        public EntityId ActorId { get; }

        public InventoryContainer Inventory { get; }

        public EquipmentLoadout Equipment { get; }

        public CultivationProgress Cultivation { get; }

        public BeastRoster Beasts { get; }

        public int RealmSourceId { get; }

        internal PlayerState(EntityId actorId, int realmSourceId, RealmSpec realm, int capacity, IDomainContent content, SourceIdAllocator sources, long experience = 0)
        {
            ActorId = actorId;
            RealmSourceId = realmSourceId;
            Inventory = new InventoryContainer(capacity, content);
            Equipment = new EquipmentLoadout(content, sources);
            Cultivation = new CultivationProgress(realm, experience);
            Beasts = new BeastRoster(actorId);
        }
    }
}

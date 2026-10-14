using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;

namespace LinhThu.Domain.Combat
{
    public sealed class ProjectileState
    {
        public EntityId Id { get; }

        public string TechniqueId { get; }

        public EffectSourceSnapshot Source { get; }

        public WorldPoint Position { get; internal set; }

        public WorldPoint Direction { get; }

        public float RemainingLifetime { get; internal set; }

        internal readonly HashSet<EntityId> hits = new HashSet<EntityId>();
        public IReadOnlyList<EntityId> HitTargets => hits.OrderBy(x => x).ToArray();
        internal ProjectileState(EntityId id, TechniqueSpec spec, EffectSourceSnapshot source, WorldPoint position, WorldPoint direction)
        {
            Id = id;
            TechniqueId = spec.DefinitionId;
            Source = source;
            Position = position;
            Direction = direction.Normalized;
            RemainingLifetime = spec.Delivery.Range / spec.Delivery.ProjectileSpeed;
        }
    }
}

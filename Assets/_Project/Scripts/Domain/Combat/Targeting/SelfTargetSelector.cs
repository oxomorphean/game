using System.Collections.Generic;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.World;

namespace LinhThu.Domain.Combat.Targeting
{
    public sealed class SelfTargetSelector : ITargetSelector
    {
        public DeliveryShape Shape => DeliveryShape.Self;
        public IReadOnlyList<EntityId> Select(WorldState world, ActorEntity caster, DeliverySpec d, AimRequest aim) => new[]{caster.Id};
    }
}

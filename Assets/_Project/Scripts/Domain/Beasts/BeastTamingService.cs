using System;
using System.Linq;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;

namespace LinhThu.Domain.Beasts
{
    public sealed class BeastTamingService
    {
        public static bool IsWeakened(ActorEntity beast) => beast?.WildBeast != null && beast.WildBeast.Spec.SpeciesId != null
            && beast.Vitals.IsAlive && beast.Vitals.CurrentHp <= beast.Vitals.Maximum(VitalType.Hp) * beast.WildBeast.Spec.TameHpRatio + .00001f;
        public DomainResult Check(WorldState world, EntityId playerId, EntityId targetId)
        {
            if (!world.Players.TryGetValue(playerId, out var player)) return DomainResult.Fail(DomainError.WrongOwner);
            var owner = world.Actors[playerId];
            if (!owner.CanAct || owner.Dash.Active || owner.Abilities.Cast != null) return DomainResult.Fail(DomainError.InvalidState);
            if (!world.TryGetActor(targetId, out var target)) return DomainResult.Fail(DomainError.NotFound);
            if (!IsWeakened(target)) return DomainResult.Fail(DomainError.InvalidState);
            if ((owner.Transform.Position - target.Transform.Position).Length > target.WildBeast.Spec.TameRange) return DomainResult.Fail(DomainError.OutOfRange);
            if (!world.Geometry.ClearLine(owner.Transform.Position, target.Transform.Position)) return DomainResult.Fail(DomainError.Blocked);
            if (player.Beasts.All.Count >= 1000 || world.Ids.Next == long.MaxValue) return DomainResult.Fail(DomainError.CapacityExceeded);
            return DomainResult.Ok();
        }
        public ActorEntity Select(WorldState world, EntityId playerId)
        {
            if (!world.Players.ContainsKey(playerId)) return null;
            var owner = world.Actors[playerId];
            return world.SpatialGrid.QueryActors(owner.Transform.Position, 10).Where(a => Check(world, playerId, a.Id).Succeeded)
                .OrderBy(a => (a.Transform.Position - owner.Transform.Position).LengthSquared).ThenBy(a => a.Id).FirstOrDefault();
        }
        public DomainResult TryTame(WorldState world, EntityId playerId, EntityId targetId)
        {
            var check = Check(world, playerId, targetId); if (!check.Succeeded) return check;
            var target = world.Actors[targetId]; var spawn = target.WildBeast;
            var beast = new BeastInstance(world.Ids.Allocate(), playerId, spawn.Spec.SpeciesId, "common", "wild", .5f, BeastState.Resting);
            var added = world.Players[playerId].Beasts.Add(beast);
            if (!added.Succeeded) throw new InvalidOperationException("Validated tame failed: " + added.ErrorCode);
            world.Actors[playerId].CancelLifeActivity();
            spawn.Deplete(world, null);
            world.Events.Publish(new BeastTamedEvent(playerId, beast.Id));
            return DomainResult.Ok();
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.World;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Combat.Effects;

namespace LinhThu.Domain.Inventory
{
    public sealed class ItemUseService
    {
        public DomainResult TryUse(WorldState world, EntityId ownerId, string itemId)
        {
            if (!world.Players.ContainsKey(ownerId))
                return DomainResult.Fail(DomainError.WrongOwner);
            if (!world.Actors[ownerId].CanAct)
                return DomainResult.Fail(DomainError.InvalidState);
            if (!world.Content.TryGetItem(itemId, out var item))
                return DomainResult.Fail(DomainError.MissingDefinition);
            if (world.Players[ownerId].Inventory.Count(itemId) < 1)
                return DomainResult.Fail(DomainError.InsufficientResource);
            var actor = world.Actors[ownerId];
            if (item.Category == ItemCategory.TechniqueScroll)
            {
                var learned = actor.Abilities.Learn(item.TechniqueId, world.Content);
                if (!learned.Succeeded)
                    return learned;
            }
            else if (item.Category == ItemCategory.BeastEgg)
            {
                if (!world.Content.TryGetBeast(item.BeastSpeciesId, out _))
                    return DomainResult.Fail(DomainError.MissingDefinition);
                if (world.Ids.Next == long.MaxValue)
                    return DomainResult.Fail(DomainError.CapacityExceeded);
                var beast = new BeastInstance(world.Ids.Allocate(), ownerId, item.BeastSpeciesId, "common", "default", 0);
                var added = world.Players[ownerId].Beasts.Add(beast);
                if (!added.Succeeded)
                    return added;
            }
            else if (item.Category == ItemCategory.Pill)
            {
                var ctx = new AbilityExecutionContext(world, actor, actor, actor.Transform.FacingDirection, EffectSourceSnapshot.Capture(actor));
                var valid = world.Combat.ValidateEffects(item.UseEffects, ctx, new HashSet<string>());
                if (!valid.Succeeded)
                    return valid;
                if (item.UseEffects.Count == 0 || item.UseEffects.All(e => e is RestoreVitalEffectSpec restore && (restore.Amount == 0 || actor.Vitals.Current(restore.Vital) >= actor.Vitals.Maximum(restore.Vital))))
                    return DomainResult.Fail(DomainError.NoEffect);
                world.Combat.ApplyEffects(item.UseEffects, ctx);
            }
            else
                return DomainResult.Fail(DomainError.InvalidState);
            world.Players[ownerId].Inventory.TryRemove(itemId, 1);
            return DomainResult.Ok();
        }
    }
}

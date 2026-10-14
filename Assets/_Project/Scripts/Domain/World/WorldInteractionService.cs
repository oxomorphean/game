using System;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Stats;

namespace LinhThu.Domain.World
{
    public sealed class WorldInteractionService
    {
        private static DomainResult Check(WorldState world, EntityId actorId, EntityId nodeId, out ActorEntity actor, out WorldNodeEntity node)
        {
            actor = null;
            node = null;
            if (actorId != world.Player.ActorId)
                return DomainResult.Fail(DomainError.WrongOwner);
            if (!world.TryGetActor(actorId, out actor) || !world.Nodes.TryGetValue(nodeId, out node))
                return DomainResult.Fail(DomainError.NotFound);
            if (!actor.CanAct)
                return DomainResult.Fail(DomainError.InvalidState);
            if ((actor.Transform.Position - node.Position).Length > node.Spec.InteractionRadius + Math.Max(0, actor.Stats.Get(StatId.PickupRadius)))
                return DomainResult.Fail(DomainError.OutOfRange);
            if (!world.Geometry.ClearLine(actor.Transform.Position, node.Position))
                return DomainResult.Fail(DomainError.Blocked);
            return DomainResult.Ok();
        }

        public DomainResult TryHarvest(WorldState world, EntityId actorId, EntityId nodeId)
        {
            var result = Check(world, actorId, nodeId, out _, out var node);
            if (!result.Succeeded)
                return result;
            if (!node.IsReady)
                return DomainResult.Fail(DomainError.InvalidState);
            var spec = node.Spec;
            if (node.CropDefinitionId != null && !world.Content.TryGetNode(node.CropDefinitionId, out spec))
                return DomainResult.Fail(DomainError.MissingDefinition);
            if (spec.Rewards.Count == 0)
                return DomainResult.Fail(DomainError.NoEffect);
            var add = world.Player.Inventory.TryAddAll(spec.Rewards);
            if (!add.Succeeded)
                return add;
            if (node.Spec.Kind == WorldNodeKind.SpiritFieldPlot)
            {
                node.CropDefinitionId = null;
                node.GrowthTimer = 0;
            }
            else
            {
                node.GrowthTimer = spec.RespawnSeconds;
                node.IsDepleted = spec.RespawnSeconds == 0;
            }

            world.Events.Publish(new NodeHarvestedEvent(actorId, node.Id, spec.Rewards));
            return DomainResult.Ok();
        }

        public DomainResult TryPlant(WorldState world, EntityId actorId, EntityId fieldId, string cropId)
        {
            var result = Check(world, actorId, fieldId, out _, out var field);
            if (!result.Succeeded)
                return result;
            if (field.Spec.Kind != WorldNodeKind.SpiritFieldPlot || field.CropDefinitionId != null)
                return DomainResult.Fail(DomainError.InvalidState);
            if (!world.Content.TryGetNode(cropId, out var crop) || crop.Kind != WorldNodeKind.HerbNode || crop.SeedItemId == null)
                return DomainResult.Fail(DomainError.MissingDefinition);
            foreach (var reward in crop.Rewards)
                if (!world.Content.TryGetItem(reward.DefinitionId, out _))
                    return DomainResult.Fail(DomainError.MissingDefinition);
            var remove = world.Player.Inventory.TryRemove(crop.SeedItemId, 1);
            if (!remove.Succeeded)
                return remove;
            field.CropDefinitionId = cropId;
            field.GrowthTimer = crop.GrowthSeconds;
            field.IsDepleted = false;
            return DomainResult.Ok();
        }

        public DomainResult TryUsePortal(WorldState world, EntityId actorId, EntityId portalId)
        {
            var result = Check(world, actorId, portalId, out var actor, out var portal);
            if (!result.Succeeded)
                return result;
            if (portal.Spec.Kind != WorldNodeKind.TeleportPortal)
                return DomainResult.Fail(DomainError.InvalidState);
            if (portal.Spec.DestinationMapId != world.MapDefinitionId)
            {
                world.Events.Publish(new PortalTravelRequestedEvent(actorId, portal.Spec.DestinationMapId, portal.Spec.PortalDestination));
                return DomainResult.Ok();
            }

            if (!world.Geometry.CanStand(portal.Spec.PortalDestination, actor.Transform.CollisionRadius))
                return DomainResult.Fail(DomainError.Blocked);
            actor.Transform.Position = portal.Spec.PortalDestination;
            return DomainResult.Ok();
        }
    }
}

using System;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Stats;

namespace LinhThu.Domain.World
{
    public sealed class WorldInteractionService
    {
        public bool CanFocus(WorldState world, EntityId actorId, EntityId nodeId)
        {
            if (!Check(world, actorId, nodeId, out _, out var node).Succeeded || node.IsDepleted)
                return false;
            // IsReady describes harvest readiness; empty plots and portals have other actions.
            return node.IsReady || node.Spec.Kind == WorldNodeKind.TeleportPortal ||
                (node.Spec.Kind == WorldNodeKind.SpiritFieldPlot && node.CropDefinitionId == null);
        }

        public DomainResult TryInteractFocused(WorldState world, EntityId actorId, string cropId)
        {
            if (!world.Players.ContainsKey(actorId))
                return DomainResult.Fail(DomainError.WrongOwner);
            if (!world.Actors[actorId].CanAct)
                return DomainResult.Fail(DomainError.InvalidState);
            var node = world.InteractionTargets.Select(world, actorId);
            if (node == null)
                return DomainResult.Fail(DomainError.NotFound);
            if (node.Spec.Kind == WorldNodeKind.FishingSpot)
                return world.LifeActivities.Fish(actorId, node.Id);
            if (node.Spec.Kind == WorldNodeKind.TeleportPortal)
                return TryUsePortal(world, actorId, node.Id);
            if (node.Spec.Kind == WorldNodeKind.SpiritFieldPlot && node.CropDefinitionId == null)
                return string.IsNullOrWhiteSpace(cropId) ? DomainResult.Fail(DomainError.MissingDefinition) : TryPlant(world, actorId, node.Id, cropId);
            return TryHarvest(world, actorId, node.Id);
        }

        internal DomainResult Check(WorldState world, EntityId actorId, EntityId nodeId, out ActorEntity actor, out WorldNodeEntity node)
        {
            actor = null;
            node = null;
            if (!world.Players.ContainsKey(actorId))
                return DomainResult.Fail(DomainError.WrongOwner);
            if (!world.TryGetActor(actorId, out actor) || !world.Nodes.TryGetValue(nodeId, out node))
                return DomainResult.Fail(DomainError.NotFound);
            if (!actor.CanAct)
                return DomainResult.Fail(DomainError.InvalidState);
            var interactionPosition = node.InteractionPosition(actor.Transform.Position);
            if ((actor.Transform.Position - interactionPosition).Length > node.Spec.InteractionRadius + Math.Max(0, actor.Stats.Get(StatId.PickupRadius)))
                return DomainResult.Fail(DomainError.OutOfRange);
            if ((node.Spec.Kind == WorldNodeKind.FishingSpot && !world.Geometry.CanStand(actor.Transform.Position, actor.Transform.CollisionRadius)) ||
                !world.Geometry.ClearLine(actor.Transform.Position, interactionPosition))
                return DomainResult.Fail(DomainError.Blocked);
            return DomainResult.Ok();
        }

        public DomainResult TryHarvest(WorldState world, EntityId actorId, EntityId nodeId)
        {
            var result = Check(world, actorId, nodeId, out _, out var node);
            if (!result.Succeeded)
                return result;
            if (!node.IsReady || node.Spec.Kind == WorldNodeKind.FishingSpot)
                return DomainResult.Fail(DomainError.InvalidState);
            var spec = node.Spec;
            if (node.CropDefinitionId != null && !world.Content.TryGetNode(node.CropDefinitionId, out spec))
                return DomainResult.Fail(DomainError.MissingDefinition);
            if (spec.Rewards.Count == 0)
                return DomainResult.Fail(DomainError.NoEffect);
            var add = world.Players[actorId].Inventory.TryAddAll(spec.Rewards);
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
                world.Geometry.SetNodeDepleted(node.Id, node.IsDepleted);
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
            var remove = world.Players[actorId].Inventory.TryRemove(crop.SeedItemId, 1);
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
            actor.StopMotion();
            if (actor.State == ActorActionState.Moving || actor.State == ActorActionState.Dashing)
                actor.State = ActorActionState.Idle;
            return DomainResult.Ok();
        }
    }
}

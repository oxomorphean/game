using System;
using System.Linq;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.World
{
    public sealed class LifeActivityService
    {
        private readonly WorldState world;
        internal LifeActivityService(WorldState world) => this.world = world;

        private static bool CanStart(ActorEntity actor) => actor.CanAct && actor.Spec.LifeActivities != null
            && actor.Abilities.Cast == null && !actor.Dash.Active;

        public DomainResult ToggleMeditate(EntityId actorId)
        {
            if (!world.Players.ContainsKey(actorId)) return DomainResult.Fail(DomainError.WrongOwner);
            var actor = world.Actors[actorId];
            if (!CanStart(actor)) return DomainResult.Fail(DomainError.InvalidState);
            if (actor.State == ActorActionState.Meditating) actor.CancelLifeActivity();
            else
            {
                actor.StopMotion();
                actor.State = ActorActionState.Meditating;
            }
            return DomainResult.Ok();
        }

        public DomainResult Fish(EntityId actorId, EntityId? requestedNode)
        {
            if (!world.Players.ContainsKey(actorId)) return DomainResult.Fail(DomainError.WrongOwner);
            var actor = world.Actors[actorId];
            if (!CanStart(actor)) return DomainResult.Fail(DomainError.InvalidState);
            var nodeId = requestedNode ?? actor.LifeActivity.FishingNodeId;
            if (!nodeId.HasValue)
                nodeId = world.SpatialGrid.QueryNodes(actor.Transform.Position, Math.Min(10000, Math.Max(0, actor.Stats.Get(LinhThu.Domain.Stats.StatId.PickupRadius)))).Where(n => n.Spec.Kind == WorldNodeKind.FishingSpot && world.Interactions.CanFocus(world, actorId, n.Id))
                    .OrderBy(n => (n.InteractionPosition(actor.Transform.Position) - actor.Transform.Position).LengthSquared).ThenBy(n => n.Id).FirstOrDefault()?.Id;
            if (!nodeId.HasValue) return DomainResult.Fail(DomainError.OutOfRange);
            var check = world.Interactions.Check(world, actorId, nodeId.Value, out _, out var node);
            if (!check.Succeeded) return check;
            if (node.Spec.Kind != WorldNodeKind.FishingSpot || !node.IsReady) return DomainResult.Fail(DomainError.InvalidState);
            if (actor.State == ActorActionState.Fishing && actor.LifeActivity.FishingNodeId == node.Id)
            {
                if (!actor.LifeActivity.FishBiting)
                {
                    actor.CancelLifeActivity();
                    return DomainResult.Ok();
                }
                // Capacity is checked before ending the cast or consuming the fishing spot.
                var award = world.Players[actorId].Inventory.TryAddAll(node.Spec.Rewards);
                if (!award.Succeeded) return award;
                node.GrowthTimer = node.Spec.RespawnSeconds;
                node.IsDepleted = node.Spec.RespawnSeconds == 0;
                world.Geometry.SetNodeDepleted(node.Id, node.IsDepleted);
                actor.CancelLifeActivity();
                world.Events.Publish(new NodeHarvestedEvent(actorId, node.Id, node.Spec.Rewards));
                return DomainResult.Ok();
            }
            var target = node.Position + node.Spec.FishingWaterOffset;
            var direction = target - actor.Transform.Position;
            if (direction.LengthSquared < .000001f) return DomainResult.Fail(DomainError.InvalidInput);
            actor.StopMotion();
            actor.LifeActivity.FishingNodeId = node.Id;
            actor.LifeActivity.FishingTarget = target;
            actor.LifeActivity.FishingRemaining = node.Spec.FishingBiteSeconds;
            actor.Transform.FacingDirection = direction.Normalized;
            actor.State = ActorActionState.Fishing;
            return DomainResult.Ok();
        }

        internal void Tick(float deltaTime)
        {
            foreach (var player in world.Players.Values) TickPlayer(player, deltaTime);
        }
        private void TickPlayer(PlayerState player, float deltaTime)
        {
            var actor = world.Actors[player.ActorId];
            var state = actor.LifeActivity;
            if (actor.State == ActorActionState.Meditating)
            {
                var spec = actor.Spec.LifeActivities;
                state.MeditationElapsed += deltaTime;
                while (state.MeditationElapsed + .000001f >= spec.MeditationTickSeconds)
                {
                    var add = player.Cultivation.AddExperience(spec.MeditationExperience);
                    if (!add.Succeeded) { actor.CancelLifeActivity(); break; }
                    state.MeditationElapsed = Math.Max(0, state.MeditationElapsed - spec.MeditationTickSeconds);
                }
            }
            else if (actor.State == ActorActionState.Fishing)
            {
                if (!state.FishingNodeId.HasValue || !world.Interactions.Check(world, actor.Id, state.FishingNodeId.Value, out _, out var node).Succeeded || !node.IsReady)
                    actor.CancelLifeActivity();
                else state.FishingRemaining = state.FishingRemaining <= deltaTime + .00001f ? 0 : state.FishingRemaining - deltaTime;
            }
        }
    }
}

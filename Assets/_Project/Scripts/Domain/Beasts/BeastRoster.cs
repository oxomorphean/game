using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;
using LinhThu.Domain.Navigation;

namespace LinhThu.Domain.Beasts
{
    public sealed class BeastRoster
    {
        private readonly EntityId ownerId;
        private readonly List<BeastInstance> all = new List<BeastInstance>();
        private readonly BondStatLink link = new BondStatLink();
        public IReadOnlyList<BeastInstance> All => all.AsReadOnly();
        public BeastRoster(EntityId ownerId)
        {
            this.ownerId = ownerId;
        }

        public DomainResult Add(BeastInstance beast)
        {
            if (beast == null)
                return DomainResult.Fail(DomainError.InvalidInput);
            if (beast.OwnerId != ownerId)
                return DomainResult.Fail(DomainError.WrongOwner);
            if (all.Any(b => b.Id == beast.Id) || beast.ActorId.HasValue || beast.AssignedFieldId.HasValue || (beast.State != BeastState.InEgg && beast.State != BeastState.Resting))
                return DomainResult.Fail(DomainError.InvalidState);
            all.Add(beast);
            return DomainResult.Ok();
        }

        internal void Restore(BeastInstance beast) => all.Add(beast);
        private DomainResult Find(EntityId id, WorldState world, out BeastInstance beast)
        {
            beast = all.Find(b => b.Id == id);
            if (beast == null)
                return DomainResult.Fail(DomainError.NotFound);
            if (beast.OwnerId != world.Player.ActorId)
                return DomainResult.Fail(DomainError.WrongOwner);
            return DomainResult.Ok();
        }

        public DomainResult TryHatch(EntityId beastId, WorldState world)
        {
            var r = Find(beastId, world, out var b);
            if (!r.Succeeded)
                return r;
            if (b.State != BeastState.InEgg || !world.PlayerActor.CanAct)
                return DomainResult.Fail(DomainError.InvalidState);
            if (!world.Content.TryGetBeast(b.DefinitionId, out _))
                return DomainResult.Fail(DomainError.MissingDefinition);
            b.State = BeastState.Resting;
            world.Events.Publish(new BeastTamedEvent(ownerId, b.Id));
            return DomainResult.Ok();
        }

        public DomainResult TrySummon(EntityId beastId, WorldState world)
        {
            var r = Find(beastId, world, out var b);
            if (!r.Succeeded)
                return r;
            var owner = world.PlayerActor;
            if (!owner.CanAct || b.State != BeastState.Resting)
                return DomainResult.Fail(DomainError.InvalidState);
            if (all.Count(x => x.State == BeastState.SummonedActive) >= Math.Floor(Math.Max(0, owner.Stats.Get(StatId.BeastCommandCap))))
                return DomainResult.Fail(DomainError.CapacityExceeded);
            if (!world.Content.TryGetBeast(b.DefinitionId, out var spec) || !world.Content.TryGetActor(spec.ActorDefinitionId, out var actorSpec))
                return DomainResult.Fail(DomainError.MissingDefinition);
            if (!spec.RestoreVitalsOnSummon && b.RestingHp.HasValue && b.RestingHp.Value <= 0)
                return DomainResult.Fail(DomainError.InvalidState, "Beast requires recovery.");
            var center = owner.Transform.Position + new WorldPoint(-spec.Ai.FollowDistance, 0);
            if (!world.Geometry.TryFindStandableNear(center, actorSpec.CollisionRadius, spec.Ai.FollowDistance, out var spawn))
                return DomainResult.Fail(DomainError.Blocked);
            if (world.Sources.Next == int.MaxValue || world.Ids.Next == long.MaxValue)
                return DomainResult.Fail(DomainError.CapacityExceeded);
            var preview = new ActorEntity(new EntityId(world.Ids.Next), actorSpec, ActorFaction.PlayerBeast, spawn, ownerId, world.Sources);
            var statProbe = owner.Stats.Copy();
            try
            {
                var mods = spec.BondStats.Select(s => new StatModifier(s, ModifierType.FlatAdd, preview.Stats.Get(s) * Math.Max(0, owner.Stats.Get(StatId.BeastStatLinkRatio)) * spec.BondScale, world.Sources.Next)).ToArray();
                statProbe.SetModifiersFromSource(world.Sources.Next, mods);
            }
            catch (ArgumentException error)
            {
                return DomainResult.Fail(DomainError.InvalidInput, error.Message);
            }
            catch (OverflowException error)
            {
                return DomainResult.Fail(DomainError.CapacityExceeded, error.Message);
            }

            var created = world.TrySpawnActor(spec.ActorDefinitionId, ActorFaction.PlayerBeast, spawn, ownerId);
            if (!created.Succeeded)
                return DomainResult.Fail(created.ErrorCode, created.Message);
            var actor = created.Value;
            if (!spec.RestoreVitalsOnSummon && b.RestingHp.HasValue)
                actor.Vitals = new VitalResourcePool(actor.Stats, b.RestingHp.Value, b.RestingQi ?? 0, 0);
            b.ActorId = actor.Id;
            b.BondSourceId = world.Sources.Allocate();
            b.State = BeastState.SummonedActive;
            link.Refresh(owner, actor, spec, b.BondSourceId);
            world.Events.Publish(new BeastSummonedEvent(ownerId, b.Id, actor.Id));
            return DomainResult.Ok();
        }

        public DomainResult TryRecall(EntityId beastId, WorldState world)
        {
            var r = Find(beastId, world, out var b);
            if (!r.Succeeded)
                return r;
            if (b.State != BeastState.SummonedActive)
                return DomainResult.Fail(DomainError.InvalidState);
            if (b.ActorId.HasValue && world.TryGetActor(b.ActorId.Value, out var actor))
            {
                b.RestingHp = actor.Vitals.CurrentHp;
                b.RestingQi = actor.Vitals.CurrentQi;
                world.RemoveActorInternal(actor.Id);
            }

            link.Remove(world.PlayerActor, b.BondSourceId);
            b.ActorId = null;
            b.BondSourceId = 0;
            b.State = BeastState.Resting;
            world.Events.Publish(new BeastRecalledEvent(ownerId, b.Id));
            return DomainResult.Ok();
        }

        public DomainResult TryAssignToField(EntityId beastId, EntityId fieldId, WorldState world)
        {
            var r = Find(beastId, world, out var b);
            if (!r.Succeeded)
                return r;
            if (!world.PlayerActor.CanAct || b.State != BeastState.Resting)
                return DomainResult.Fail(DomainError.InvalidState);
            if (!world.Nodes.TryGetValue(fieldId, out var field) || field.Spec.Kind != WorldNodeKind.SpiritFieldPlot)
                return DomainResult.Fail(DomainError.NotFound);
            if (field.AssignedBeastId.HasValue)
                return DomainResult.Fail(DomainError.InvalidState);
            if (!world.Content.TryGetBeast(b.DefinitionId, out _))
                return DomainResult.Fail(DomainError.MissingDefinition);
            b.State = BeastState.AssignedToField;
            b.AssignedFieldId = fieldId;
            field.AssignedBeastId = b.Id;
            return DomainResult.Ok();
        }

        public DomainResult TryUnassignFromField(EntityId beastId, WorldState world)
        {
            var r = Find(beastId, world, out var b);
            if (!r.Succeeded)
                return r;
            if (b.State != BeastState.AssignedToField)
                return DomainResult.Fail(DomainError.InvalidState);
            if (b.AssignedFieldId.HasValue && world.Nodes.TryGetValue(b.AssignedFieldId.Value, out var field))
                field.AssignedBeastId = null;
            b.AssignedFieldId = null;
            b.State = BeastState.Resting;
            return DomainResult.Ok();
        }

        internal void Reconcile(WorldState world)
        {
            foreach (var b in all.Where(b => b.State == BeastState.SummonedActive).ToArray())
            {
                if (!world.PlayerActor.Vitals.IsAlive || !b.ActorId.HasValue || !world.TryGetActor(b.ActorId.Value, out var actor) || !actor.Vitals.IsAlive)
                    TryRecall(b.Id, world);
            }

            int cap = (int)Math.Min(10000, Math.Floor(Math.Max(0, world.PlayerActor.Stats.Get(StatId.BeastCommandCap))));
            foreach (var b in all.Where(b => b.State == BeastState.SummonedActive).OrderByDescending(b => b.ActorId.Value).Take(Math.Max(0, all.Count(b => b.State == BeastState.SummonedActive) - cap)).ToArray())
                TryRecall(b.Id, world);
            foreach (var b in all.Where(b => b.State == BeastState.SummonedActive))
            {
                world.Content.TryGetBeast(b.DefinitionId, out var spec);
                link.Refresh(world.PlayerActor, world.Actors[b.ActorId.Value], spec, b.BondSourceId);
            }

            foreach (var b in all.Where(b => b.State == BeastState.AssignedToField).ToArray())
                if (!world.PlayerActor.Vitals.IsAlive || !b.AssignedFieldId.HasValue || !world.Nodes.ContainsKey(b.AssignedFieldId.Value))
                    TryUnassignFromField(b.Id, world);
        }
    }
}

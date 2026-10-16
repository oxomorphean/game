using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;

namespace LinhThu.Domain.Combat
{
    public sealed class StatusEffectContainer
    {
        private readonly ActorEntity owner;
        private readonly SourceIdAllocator sources;
        private readonly List<StatusInstance> active = new List<StatusInstance>();
        public IReadOnlyList<StatusInstance> Active => active.AsReadOnly();
        public StatusEffectContainer(ActorEntity owner, SourceIdAllocator sources)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            this.sources = sources ?? throw new ArgumentNullException(nameof(sources));
        }

        internal DomainResult ValidateApplications(IReadOnlyList<StatusEffectSpec> specs, EntityId casterId)
        {
            if (!owner.Vitals.IsAlive)
                return DomainResult.Fail(DomainError.InvalidState);
            var probe = owner.Stats.Copy();
            int next = sources.Next;
            var groups = active.Where(s => s.Spec.StackPolicy != StatusStackPolicy.IndependentInstances).ToDictionary(s => (s.DefinitionId, s.CasterId), s => (source: s.SourceId, count: s.StackCount));
            try
            {
                foreach (var spec in specs)
                {
                    bool grouped = spec.StackPolicy != StatusStackPolicy.IndependentInstances;
                    var key = (spec.DefinitionId, casterId);
                    bool exists = grouped && groups.TryGetValue(key, out _);
                    var previous = exists ? groups[key] : (source: 0, count: 0);
                    if (!exists && next == int.MaxValue)
                        return DomainResult.Fail(DomainError.CapacityExceeded);
                    int id = exists ? previous.source : next++;
                    int count = !exists ? 1 : spec.StackPolicy == StatusStackPolicy.AddStacksRefreshDuration ? Math.Min(spec.MaxStacks, previous.count + 1) : previous.count;
                    probe.SetModifiersFromSource(id, Materialize(spec, count, id));
                    if (grouped)
                        groups[key] = (id, count);
                }

                return DomainResult.Ok();
            }
            catch (ArgumentException error)
            {
                return DomainResult.Fail(DomainError.InvalidInput, error.Message);
            }
            catch (OverflowException error)
            {
                return DomainResult.Fail(DomainError.CapacityExceeded, error.Message);
            }
        }

        public DomainResult Apply(StatusEffectSpec spec, EntityId casterId, EffectSourceSnapshot source)
        {
            if (spec == null || source == null || casterId.Value <= 0 || source.ActorId != casterId)
                return DomainResult.Fail(DomainError.InvalidInput);
            var validation = ValidateApplications(new[]{spec}, casterId);
            if (!validation.Succeeded)
                return validation;
            var existing = spec.StackPolicy == StatusStackPolicy.IndependentInstances ? null : active.Find(s => s.DefinitionId == spec.DefinitionId && s.CasterId == casterId);
            int count = existing == null ? 1 : spec.StackPolicy == StatusStackPolicy.AddStacksRefreshDuration ? Math.Min(spec.MaxStacks, existing.StackCount + 1) : existing.StackCount;
            if (existing == null && sources.Next == int.MaxValue)
                return DomainResult.Fail(DomainError.CapacityExceeded);
            int id = existing?.SourceId ?? sources.Next;
            var mods = Materialize(spec, count, id);
            if (existing == null)
            {
                existing = new StatusInstance(sources.Allocate(), spec, casterId, source);
                active.Add(existing);
            }

            existing.StackCount = count;
            existing.RemainingDuration = spec.Duration;
            existing.Source = source;
            owner.Stats.SetModifiersFromSource(id, mods);
            owner.Vitals.ClampToLimits();
            return DomainResult.Ok();
        }

        internal static StatModifier[] Materialize(StatusEffectSpec spec, int count, int id)
        {
            var result = new List<StatModifier>();
            foreach (var m in spec.Modifiers)
            {
                if (m.Type == ModifierType.PercentMult)
                {
                    for (int i = 0; i < count; i++)
                        result.Add(m.FromSource(id));
                }
                else
                    result.Add(new StatModifier(m.StatId, m.Type, m.Value * count, id));
            }

            return result.ToArray();
        }

        internal void Restore(StatusInstance instance)
        {
            owner.Stats.SetModifiersFromSource(instance.SourceId, Materialize(instance.Spec, instance.StackCount, instance.SourceId));
            active.Add(instance);
        }

        public bool Remove(int sourceId)
        {
            int removed = active.RemoveAll(s => s.SourceId == sourceId);
            if (removed == 0)
                return false;
            owner.Stats.RemoveAllModifiersFromSource(sourceId);
            owner.Vitals.ClampToLimits();
            return true;
        }

        public void Clear()
        {
            foreach (var s in active.ToArray())
                Remove(s.SourceId);
        }

        internal void OnDeath()
        {
            foreach (var s in active.Where(s => s.Spec.ClearOnDeath).ToArray())
                Remove(s.SourceId);
        }

        internal float TimeToNextEvent(float limit)
        {
            foreach (var status in active)
            {
                limit = Math.Min(limit, status.RemainingDuration);
                if (status.Spec.TickInterval > 0)
                    limit = Math.Min(limit, status.UntilNextTick);
            }
            return limit;
        }

        public void Tick(float deltaTime, WorldState world)
        {
            Guard.Delta(deltaTime);
            if (deltaTime == 0)
                return;
            foreach (var status in active.ToArray())
            {
                if (!active.Contains(status))
                    continue;
                float elapsed = Math.Min(deltaTime, status.RemainingDuration);
                if (status.Spec.TickInterval > 0)
                {
                    status.UntilNextTick -= elapsed;
                    while (status.UntilNextTick <= .000001f && active.Contains(status) && owner.Vitals.IsAlive)
                    {
                        world.TryGetActor(status.CasterId, out var caster);
                        var context = new AbilityExecutionContext(world, caster, owner, owner.Transform.FacingDirection, status.Source, status.StackCount);
                        world.Combat.ApplyEffects(status.Spec.TickEffects, context);
                        status.UntilNextTick += status.Spec.TickInterval;
                    }
                }

                status.RemainingDuration = Math.Max(0, status.RemainingDuration - deltaTime);
                if (status.RemainingDuration <= .000001f)
                    Remove(status.SourceId);
            }
        }
    }
}

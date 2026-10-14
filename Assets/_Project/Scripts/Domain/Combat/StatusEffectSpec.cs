using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;

namespace LinhThu.Domain.Combat
{
    public enum StatusStackPolicy
    {
        RefreshDuration,
        AddStacksRefreshDuration,
        IndependentInstances
    }

    public sealed class StatusEffectSpec
    {
        public string DefinitionId { get; }

        public float Duration { get; }

        public float TickInterval { get; }

        public int MaxStacks { get; }

        public StatusStackPolicy StackPolicy { get; }

        public bool ClearOnDeath { get; }

        public IReadOnlyList<StatModifierSpec> Modifiers { get; }

        public IReadOnlyList<AbilityEffectSpec> TickEffects { get; }

        public StatusEffectSpec(string definitionId, float duration, float tickInterval, int maxStacks, StatusStackPolicy stackPolicy, IEnumerable<StatModifierSpec> modifiers, IEnumerable<AbilityEffectSpec> tickEffects, bool clearOnDeath = true)
        {
            DefinitionId = Guard.Id(definitionId);
            Duration = Guard.Number(duration, nameof(duration), .001f, 86400);
            TickInterval = Guard.Number(tickInterval, nameof(tickInterval), 0, 86400);
            if (maxStacks < 1 || maxStacks > 100)
                throw new ArgumentException(nameof(maxStacks));
            MaxStacks = maxStacks;
            StackPolicy = Guard.EnumValue(stackPolicy);
            ClearOnDeath = clearOnDeath;
            Modifiers = Guard.Copy(modifiers);
            TickEffects = Guard.Copy(tickEffects);
            foreach (var modifier in Modifiers)
                modifier.FromSource(1);
            if (TickEffects.Any(x => x == null) || (TickEffects.Count > 0 && tickInterval < .001f) || (tickInterval > 0 && duration / tickInterval > 1000000))
                throw new ArgumentException("Invalid periodic status.");
        }
    }

    public sealed class StatusInstance
    {
        public int SourceId { get; }

        public string DefinitionId => Spec.DefinitionId;
        public StatusEffectSpec Spec { get; }

        public EntityId CasterId { get; }

        public int StackCount { get; internal set; }

        public float RemainingDuration { get; internal set; }

        public float UntilNextTick { get; internal set; }

        public EffectSourceSnapshot Source { get; internal set; }

        internal StatusInstance(int id, StatusEffectSpec spec, EntityId caster, EffectSourceSnapshot source)
        {
            SourceId = id;
            Spec = spec;
            CasterId = caster;
            Source = source;
            StackCount = 1;
            RemainingDuration = spec.Duration;
            UntilNextTick = spec.TickInterval;
        }
    }
}

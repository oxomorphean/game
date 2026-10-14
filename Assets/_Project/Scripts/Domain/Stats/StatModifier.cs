using System;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Stats
{
    public enum ModifierType
    {
        FlatAdd,
        PercentAdd,
        PercentMult
    }

    public readonly struct StatModifierSpec
    {
        public StatId StatId { get; }

        public ModifierType Type { get; }

        public float Value { get; }

        public StatModifierSpec(StatId statId, ModifierType type, float value)
        {
            StatId = Guard.EnumValue(statId);
            Type = Guard.EnumValue(type);
            Value = Guard.Number(value, nameof(value), -1e9f);
        }

        public StatModifier FromSource(int source) => new StatModifier(StatId, Type, Value, source);
    }

    public readonly struct StatModifier
    {
        public StatId StatId { get; }

        public ModifierType Type { get; }

        public float Value { get; }

        public int SourceId { get; }

        public StatModifier(StatId statId, ModifierType type, float value, int sourceId)
        {
            if (sourceId <= 0)
                throw new ArgumentException(nameof(sourceId));
            StatId = Guard.EnumValue(statId);
            Type = Guard.EnumValue(type);
            Value = Guard.Number(value, nameof(value), -1e9f);
            SourceId = sourceId;
        }
    }
}

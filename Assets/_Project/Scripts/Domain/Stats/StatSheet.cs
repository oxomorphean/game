using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Stats
{
    public sealed class StatSheet
    {
        private readonly Dictionary<StatId, float> bases;
        private readonly Dictionary<StatId, float> cache = new Dictionary<StatId, float>();
        private List<StatModifier> modifiers = new List<StatModifier>();
        public IReadOnlyDictionary<StatId, float> BaseValues { get; }

        public int Revision { get; private set; }

        public StatSheet(IReadOnlyDictionary<StatId, float> baseValues)
        {
            bases = new Dictionary<StatId, float>();
            foreach (var kv in baseValues ?? throw new ArgumentNullException(nameof(baseValues)))
                bases.Add(Guard.EnumValue(kv.Key), Guard.Number(kv.Value, "base stat", -1e9f));
            BaseValues = new ReadOnlyDictionary<StatId, float>(bases);
        }

        public float Get(StatId stat)
        {
            Guard.EnumValue(stat);
            if (!cache.TryGetValue(stat, out float value))
            {
                value = Compute(stat, bases, modifiers);
                cache[stat] = value;
            }

            return value;
        }

        public void SetBase(StatId stat, float value)
        {
            Guard.EnumValue(stat);
            Guard.Number(value, nameof(value), -1e9f);
            var candidate = new Dictionary<StatId, float>(bases)
            {[stat] = value};
            Compute(stat, candidate, modifiers);
            int revision = checked(Revision + 1);
            bases[stat] = value;
            cache.Remove(stat);
            Revision = revision;
        }

        public void AddModifier(StatModifier modifier)
        {
            Validate(modifier);
            var candidate = new List<StatModifier>(modifiers)
            {modifier};
            Compute(modifier.StatId, bases, candidate);
            int revision = checked(Revision + 1);
            modifiers = candidate;
            cache.Remove(modifier.StatId);
            Revision = revision;
        }

        public void SetModifiersFromSource(int sourceId, IReadOnlyList<StatModifier> values)
        {
            if (sourceId <= 0 || values == null)
                throw new ArgumentException("Invalid modifier source.");
            foreach (var m in values)
            {
                Validate(m);
                if (m.SourceId != sourceId)
                    throw new ArgumentException("Modifier source mismatch.");
            }

            var candidate = modifiers.Where(m => m.SourceId != sourceId).Concat(values).ToList();
            foreach (var stat in candidate.Select(m => m.StatId).Distinct())
                Compute(stat, bases, candidate);
            int revision = checked(Revision + 1);
            modifiers = candidate;
            cache.Clear();
            Revision = revision;
        }

        public int RemoveAllModifiersFromSource(int sourceId)
        {
            var candidate = modifiers.Where(m => m.SourceId != sourceId).ToList();
            int removed = modifiers.Count - candidate.Count;
            if (removed == 0)
                return 0;
            foreach (var stat in modifiers.Where(m => m.SourceId == sourceId).Select(m => m.StatId).Distinct())
                Compute(stat, bases, candidate);
            int revision = checked(Revision + 1);
            modifiers = candidate;
            cache.Clear();
            Revision = revision;
            return removed;
        }

        public IReadOnlyList<StatModifier> GetModifiers() => Array.AsReadOnly(modifiers.ToArray());
        internal StatSheet Copy() => new StatSheet(bases)
        {modifiers = new List<StatModifier>(modifiers)};
        private static void Validate(StatModifier m)
        {
            if (m.SourceId <= 0)
                throw new ArgumentException("Invalid modifier.");
            Guard.EnumValue(m.StatId);
            Guard.EnumValue(m.Type);
            Guard.Number(m.Value, "modifier", -1e9f);
        }

        private static float Compute(StatId id, IReadOnlyDictionary<StatId, float> values, IEnumerable<StatModifier> mods)
        {
            double flat = values.TryGetValue(id, out float b) ? b : 0, add = 1, mult = 1;
            foreach (var m in mods)
            {
                if (m.StatId != id)
                    continue;
                switch (m.Type)
                {
                    case ModifierType.FlatAdd:
                        flat += m.Value;
                        break;
                    case ModifierType.PercentAdd:
                        add += m.Value;
                        break;
                    case ModifierType.PercentMult:
                        mult *= 1 + (double)m.Value;
                        break;
                }
            }

            double result = flat * add * mult;
            if (double.IsNaN(result) || double.IsInfinity(result) || Math.Abs(result) > float.MaxValue)
                throw new ArgumentException("Stat overflow.");
            return (float)result;
        }
    }
}

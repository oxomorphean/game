using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;

namespace LinhThu.Domain.Cultivation
{
    public sealed class RealmSpec
    {
        public string DefinitionId { get; }

        public string NextRealmId { get; }

        public IReadOnlyList<long> SubStageThresholds { get; }

        public long BreakthroughCost { get; }

        public IReadOnlyList<StatModifierSpec> Modifiers { get; }

        public RealmSpec(string definitionId, string nextRealmId, IEnumerable<long> subStageThresholds, long breakthroughCost, IEnumerable<StatModifierSpec> modifiers)
        {
            DefinitionId = Guard.Id(definitionId);
            NextRealmId = nextRealmId == null ? null : Guard.Id(nextRealmId);
            SubStageThresholds = Guard.Copy(subStageThresholds);
            if (SubStageThresholds.Count != 4 || SubStageThresholds[0] != 0 || SubStageThresholds.Zip(SubStageThresholds.Skip(1), (a, b) => b <= a).Any(x => x) || breakthroughCost < SubStageThresholds[3])
                throw new ArgumentException("Invalid realm thresholds/cost.");
            BreakthroughCost = breakthroughCost;
            Modifiers = Guard.Copy(modifiers);
            foreach (var modifier in Modifiers)
                modifier.FromSource(1);
        }
    }
}

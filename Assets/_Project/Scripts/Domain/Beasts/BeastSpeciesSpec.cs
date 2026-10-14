using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;

namespace LinhThu.Domain.Beasts
{
    public sealed class BeastAiSpec
    {
        public float FollowDistance { get; }

        public float LeashDistance { get; }

        public float SearchRadius { get; }

        public float CatchUpMultiplier { get; }

        public IReadOnlyList<string> AssistTechniqueIds { get; }

        public BeastAiSpec(float followDistance, float leashDistance, float searchRadius, float catchUpMultiplier, IEnumerable<string> assistTechniqueIds)
        {
            FollowDistance = Guard.Number(followDistance, nameof(followDistance), .1f, 10);
            LeashDistance = Guard.Number(leashDistance, nameof(leashDistance), followDistance, 100);
            SearchRadius = Guard.Number(searchRadius, nameof(searchRadius), .1f, 100);
            CatchUpMultiplier = Guard.Number(catchUpMultiplier, nameof(catchUpMultiplier), 1, 10);
            AssistTechniqueIds = Guard.Copy(assistTechniqueIds.Select(Guard.Id));
        }
    }

    public sealed class BeastSpeciesSpec
    {
        public string DefinitionId { get; }

        public string ActorDefinitionId { get; }

        public BeastAiSpec Ai { get; }

        public IReadOnlyList<StatId> BondStats { get; }

        public float BondScale { get; }

        public bool RestoreVitalsOnSummon { get; }

        public BeastSpeciesSpec(string definitionId, string actorDefinitionId, BeastAiSpec ai, IEnumerable<StatId> bondStats, float bondScale = 1, bool restoreVitalsOnSummon = true)
        {
            DefinitionId = Guard.Id(definitionId);
            ActorDefinitionId = Guard.Id(actorDefinitionId);
            Ai = ai ?? throw new ArgumentNullException(nameof(ai));
            BondStats = Guard.Copy(bondStats.Select(Guard.EnumValue).Distinct());
            if (BondStats.Any(s => s == StatId.BeastCommandCap || s == StatId.BeastStatLinkRatio))
                throw new ArgumentException("Cyclic bond output.");
            BondScale = Guard.Number(bondScale, nameof(bondScale), 0, 100);
            RestoreVitalsOnSummon = restoreVitalsOnSummon;
        }
    }
}

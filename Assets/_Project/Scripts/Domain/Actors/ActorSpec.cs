using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;
using LinhThu.Domain.Combat;

namespace LinhThu.Domain.Actors
{
    public sealed class ActorSpec
    {
        public string DefinitionId { get; }

        public ElementType PrimaryElement { get; }

        public IReadOnlyDictionary<StatId, float> BaseStats { get; }

        public IReadOnlyList<string> AbilityIds { get; }

        public float CollisionRadius { get; }

        public ActorSpec(string definitionId, ElementType primaryElement, IReadOnlyDictionary<StatId, float> baseStats, IEnumerable<string> abilityIds, float collisionRadius = .2f)
        {
            DefinitionId = Guard.Id(definitionId);
            PrimaryElement = Guard.EnumValue(primaryElement);
            BaseStats = new StatSheet(baseStats).BaseValues;
            AbilityIds = Guard.Copy(abilityIds.Select(Guard.Id).Distinct());
            CollisionRadius = Guard.Number(collisionRadius, nameof(collisionRadius), .01f, 10);
        }
    }
}

using System;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Cultivation
{
    public enum CultivationSubStage
    {
        Early,
        Middle,
        Late,
        Perfection
    }

    public sealed class CultivationProgress
    {
        internal RealmSpec Realm { get; private set; }

        public string CurrentRealmId => Realm.DefinitionId;
        public CultivationSubStage SubStage
        {
            get
            {
                for (int i = 3; i >= 0; i--)
                    if (CurrentCultivationExp >= Realm.SubStageThresholds[i])
                        return (CultivationSubStage)i;
                return CultivationSubStage.Early;
            }
        }

        public long CurrentCultivationExp { get; private set; }
        public long NextThreshold => SubStage < CultivationSubStage.Perfection
            ? Realm.SubStageThresholds[(int)SubStage + 1] : Realm.BreakthroughCost;
        public bool CanBreakthrough => Realm.NextRealmId != null && SubStage == CultivationSubStage.Perfection
            && CurrentCultivationExp >= Realm.BreakthroughCost;

        public CultivationProgress(RealmSpec realm, long experience)
        {
            Realm = realm ?? throw new ArgumentNullException(nameof(realm));
            if (experience < 0)
                throw new ArgumentException(nameof(experience));
            CurrentCultivationExp = experience;
        }

        public DomainResult AddExperience(long amount)
        {
            if (amount < 0 || long.MaxValue - CurrentCultivationExp < amount)
                return DomainResult.Fail(DomainError.InvalidInput);
            CurrentCultivationExp += amount;
            return DomainResult.Ok();
        }

        internal void Advance(RealmSpec next, long remaining)
        {
            Realm = next;
            CurrentCultivationExp = remaining;
        }
    }
}

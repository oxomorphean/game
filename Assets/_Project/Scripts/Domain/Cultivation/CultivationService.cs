using System;
using System.Linq;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Cultivation
{
    public sealed class CultivationService
    {
        private readonly IDomainContent content;
        private readonly DomainEventBus events;
        public CultivationService(IDomainContent content, DomainEventBus events)
        {
            this.content = content;
            this.events = events;
        }

        public DomainResult TryBreakthrough(ActorEntity actor, CultivationProgress progress, int realmSourceId)
        {
            if (actor == null || progress == null || realmSourceId <= 0)
                return DomainResult.Fail(DomainError.InvalidInput);
            if (!actor.CanAct || progress.Realm.NextRealmId == null)
                return DomainResult.Fail(DomainError.InvalidState);
            if (progress.SubStage != CultivationSubStage.Perfection || progress.CurrentCultivationExp < progress.Realm.BreakthroughCost)
                return DomainResult.Fail(DomainError.InsufficientResource);
            if (!content.TryGetRealm(progress.Realm.NextRealmId, out var next))
                return DomainResult.Fail(DomainError.MissingDefinition);
            string old = progress.CurrentRealmId;
            long remaining = progress.CurrentCultivationExp - progress.Realm.BreakthroughCost;
            try
            {
                actor.Stats.SetModifiersFromSource(realmSourceId, next.Modifiers.Select(m => m.FromSource(realmSourceId)).ToArray());
            }
            catch (ArgumentException error)
            {
                return DomainResult.Fail(DomainError.InvalidInput, error.Message);
            }
            catch (OverflowException error)
            {
                return DomainResult.Fail(DomainError.CapacityExceeded, error.Message);
            }

            progress.Advance(next, remaining);
            actor.Vitals.ClampToLimits();
            events.Publish(new RealmBreakthroughEvent(actor.Id, old, next.DefinitionId));
            return DomainResult.Ok();
        }
    }
}

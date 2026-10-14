using System;
using System.Linq;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Stats;

namespace LinhThu.Domain.Beasts
{
    public sealed class BondStatLink
    {
        public void Refresh(ActorEntity owner, ActorEntity beast, BeastSpeciesSpec spec, int sourceId)
        {
            float ratio = Math.Max(0, owner.Stats.Get(StatId.BeastStatLinkRatio)) * spec.BondScale;
            var values = spec.BondStats.Select(s => new StatModifier(s, ModifierType.FlatAdd, beast.Stats.Get(s) * ratio, sourceId)).ToArray();
            var old = owner.Stats.GetModifiers().Where(m => m.SourceId == sourceId).ToArray();
            if (!old.SequenceEqual(values))
                owner.Stats.SetModifiersFromSource(sourceId, values);
            owner.Vitals.ClampToLimits();
        }

        public void Remove(ActorEntity owner, int sourceId)
        {
            owner.Stats.RemoveAllModifiersFromSource(sourceId);
            owner.Vitals.ClampToLimits();
        }
    }
}

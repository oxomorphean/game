using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Combat
{
    public sealed class AbilityBook
    {
        private readonly Dictionary<string, float> cooldowns = new Dictionary<string, float>(StringComparer.Ordinal);
        public IReadOnlyDictionary<string, float> Cooldowns { get; }

        public CastState Cast { get; internal set; }

        public AbilityBook(IEnumerable<string> initial)
        {
            foreach (var id in initial)
                cooldowns.Add(Guard.Id(id), 0);
            Cooldowns = new ReadOnlyDictionary<string, float>(cooldowns);
        }

        public DomainResult Learn(string techniqueId, IDomainContent content)
        {
            if (!content.TryGetTechnique(techniqueId, out _))
                return DomainResult.Fail(DomainError.MissingDefinition);
            if (Knows(techniqueId))
                return DomainResult.Fail(DomainError.NoEffect);
            cooldowns.Add(techniqueId, 0);
            return DomainResult.Ok();
        }

        public bool Knows(string id) => id != null && cooldowns.ContainsKey(id);
        public float GetCooldownRemaining(string id) => id != null && cooldowns.TryGetValue(id, out var value) ? value : 0;
        internal void StartCooldown(string id, float duration)
        {
            if (!Knows(id))
                throw new ArgumentException(nameof(id));
            cooldowns[id] = Guard.Number(duration, nameof(duration), 0, 86400);
        }

        public void Tick(float deltaTime)
        {
            Guard.Delta(deltaTime);
            foreach (var key in cooldowns.Keys.ToArray())
            {
                float next = cooldowns[key] - deltaTime;
                cooldowns[key] = next <= .000001f ? 0 : next;
            }
        }
    }

    public sealed class CastState
    {
        public string TechniqueId { get; }

        public AimRequest Aim { get; }

        public float WindupRemaining { get; internal set; }

        public float ChannelRemaining { get; internal set; }

        public float UntilPulse { get; internal set; }

        public bool Started { get; internal set; }

        internal CastState(TechniqueSpec spec, AimRequest aim)
        {
            TechniqueId = spec.DefinitionId;
            Aim = aim;
            WindupRemaining = spec.CastDuration;
            ChannelRemaining = spec.ChannelDuration;
            UntilPulse = spec.ChannelInterval;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Combat
{
    public sealed class TechniqueSpec
    {
        public string DefinitionId { get; }

        public DeliverySpec Delivery { get; }

        public float QiCost { get; }

        public float Cooldown { get; }

        public float CastDuration { get; }

        public float ChannelDuration { get; }

        public float ChannelInterval { get; }

        public IReadOnlyList<AbilityEffectSpec> Effects { get; }

        public TechniqueSpec(string definitionId, DeliverySpec delivery, IEnumerable<AbilityEffectSpec> effects, float qiCost = 0, float cooldown = 0, float castDuration = 0, float channelDuration = 0, float channelInterval = 0)
        {
            DefinitionId = Guard.Id(definitionId);
            Delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
            Effects = Guard.Copy(effects);
            if (Effects.Count == 0 || Effects.Any(x => x == null))
                throw new ArgumentException(nameof(effects));
            QiCost = Guard.Number(qiCost, nameof(qiCost));
            Cooldown = Guard.Number(cooldown, nameof(cooldown), 0, 86400);
            CastDuration = Guard.Number(castDuration, nameof(castDuration), 0, 3600);
            ChannelDuration = Guard.Number(channelDuration, nameof(channelDuration), 0, 3600);
            ChannelInterval = Guard.Number(channelInterval, nameof(channelInterval), 0, 3600);
            if (channelDuration > 0 && (channelInterval < .001f || channelDuration / channelInterval > 10000))
                throw new ArgumentException("Invalid channel interval.");
        }
    }
}

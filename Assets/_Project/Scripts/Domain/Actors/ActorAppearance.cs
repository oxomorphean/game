using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Actors
{
    /// <summary>Cosmetic identity only. default represents an actor without a modular wardrobe.</summary>
    public readonly struct ActorAppearance : IEquatable<ActorAppearance>
    {
        public string OutfitId { get; }
        public string HairId { get; }
        public string EyeId { get; }
        public bool IsEmpty => OutfitId == null && HairId == null && EyeId == null;

        public ActorAppearance(string outfitId, string hairId, string eyeId)
        {
            OutfitId = Guard.Id(outfitId);
            HairId = Guard.Id(hairId);
            EyeId = Guard.Id(eyeId);
        }

        public ActorAppearance WithOutfit(string id) => new ActorAppearance(id, HairId, EyeId);
        public bool Equals(ActorAppearance other) => OutfitId == other.OutfitId && HairId == other.HairId && EyeId == other.EyeId;
        public override bool Equals(object obj) => obj is ActorAppearance other && Equals(other);
        public override int GetHashCode() => ((OutfitId?.GetHashCode() ?? 0) * 397 ^ (HairId?.GetHashCode() ?? 0)) * 397 ^ (EyeId?.GetHashCode() ?? 0);
        public static bool operator ==(ActorAppearance left, ActorAppearance right) => left.Equals(right);
        public static bool operator !=(ActorAppearance left, ActorAppearance right) => !left.Equals(right);
    }

    public sealed class AppearanceSpec
    {
        public ActorAppearance Default { get; }
        public IReadOnlyList<string> OutfitIds { get; }
        public IReadOnlyList<string> HairIds { get; }
        public IReadOnlyList<string> EyeIds { get; }

        public AppearanceSpec(ActorAppearance defaultAppearance, IEnumerable<string> outfits, IEnumerable<string> hair, IEnumerable<string> eyes)
        {
            OutfitIds = Guard.Copy(outfits.Select(Guard.Id).Distinct());
            HairIds = Guard.Copy(hair.Select(Guard.Id).Distinct());
            EyeIds = Guard.Copy(eyes.Select(Guard.Id).Distinct());
            Default = defaultAppearance;
            if (!Supports(Default))
                throw new ArgumentException("Default appearance is not in the actor wardrobe.");
        }

        public bool Supports(ActorAppearance value) => !value.IsEmpty && OutfitIds.Contains(value.OutfitId) && HairIds.Contains(value.HairId) && EyeIds.Contains(value.EyeId);
    }
}

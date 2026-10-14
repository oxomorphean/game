using System;
using System.Collections.Generic;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Actors
{
    public enum ActorFaction
    {
        Player,
        PlayerBeast,
        WildBeast,
        HostileMonster,
        Neutral
    }

    public readonly struct FactionPair
    {
        public ActorFaction From { get; }

        public ActorFaction To { get; }

        public FactionPair(ActorFaction from, ActorFaction to)
        {
            From = Guard.EnumValue(from);
            To = Guard.EnumValue(to);
        }
    }

    public sealed class FactionRelations
    {
        private readonly HashSet<FactionPair> hostile;
        public FactionRelations(IEnumerable<FactionPair> hostilePairs)
        {
            hostile = new HashSet<FactionPair>(hostilePairs ?? throw new ArgumentNullException(nameof(hostilePairs)));
        }

        internal IEnumerable<FactionPair> Entries => hostile;
        public bool IsHostile(ActorFaction from, ActorFaction to) => hostile.Contains(new FactionPair(from, to));
    }

    public enum ActorActionState
    {
        Idle,
        Moving,
        Casting,
        Channeling,
        Staggered,
        Dead,
        Dashing,
        Meditating,
        Fishing
    }
}

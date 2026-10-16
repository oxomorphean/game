using System;
using System.Collections.Generic;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Core;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.Navigation;

namespace LinhThu.Domain.Beasts
{
    public sealed class WildBeastSpec
    {
        public string DefinitionId { get; }
        public string ActorDefinitionId { get; }
        public string SpeciesId { get; }
        public string AttackId { get; }
        public ActorFaction Faction { get; }
        public float AggroRadius { get; }
        public float LeashRadius { get; }
        public float PatrolRadius { get; }
        public float RespawnSeconds { get; }
        public float TameHpRatio { get; }
        public float TameRange { get; }
        public long Experience { get; }
        public IReadOnlyList<ItemStack> Loot { get; }
        public WildBeastSpec(string id, string actorId, string speciesId, string attackId, IEnumerable<ItemStack> loot,
            long experience = 25, float aggroRadius = 5, float leashRadius = 8, float patrolRadius = 2,
            float respawnSeconds = 15, float tameHpRatio = .3f, float tameRange = 2, ActorFaction faction = ActorFaction.WildBeast)
        {
            DefinitionId = Guard.Id(id); ActorDefinitionId = Guard.Id(actorId);
            SpeciesId = speciesId == null ? null : Guard.Id(speciesId); AttackId = Guard.Id(attackId);
            if (faction != ActorFaction.WildBeast && faction != ActorFaction.HostileMonster) throw new ArgumentException(nameof(faction));
            Faction = faction; AggroRadius = Guard.Number(aggroRadius, nameof(aggroRadius), .1f, 100);
            LeashRadius = Guard.Number(leashRadius, nameof(leashRadius), aggroRadius, 100);
            PatrolRadius = Guard.Number(patrolRadius, nameof(patrolRadius), 0, leashRadius * .5f);
            RespawnSeconds = Guard.Number(respawnSeconds, nameof(respawnSeconds), .1f, 86400);
            TameHpRatio = Guard.Number(tameHpRatio, nameof(tameHpRatio), .01f, 1);
            TameRange = Guard.Number(tameRange, nameof(tameRange), .1f, 10);
            if (experience < 0 || experience > 1000000000) throw new ArgumentException(nameof(experience));
            Experience = experience; Loot = Guard.Copy(loot);
            if (Loot.Count > 100) throw new ArgumentException("Too many loot entries.");
        }
    }
    public readonly struct WildBeastSeed
    {
        public string DefinitionId { get; }
        public WorldPoint Home { get; }
        public WildBeastSeed(string id, WorldPoint home)
        {
            DefinitionId = Guard.Id(id);
            if (!home.Finite) throw new ArgumentException(nameof(home));
            Home = home;
        }
    }
}

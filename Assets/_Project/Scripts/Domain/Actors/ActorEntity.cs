using System;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Stats;
using LinhThu.Domain.Combat;

namespace LinhThu.Domain.Actors
{
    public sealed class ActorEntity
    {
        public EntityId Id { get; }

        public string DefinitionId => Spec.DefinitionId;
        public ActorSpec Spec { get; }

        public ActorFaction Faction { get; }

        public EntityId? OwnerId { get; }

        public Transform2D Transform { get; }

        public StatSheet Stats { get; }

        public VitalResourcePool Vitals { get; internal set; }

        public ActorActionState State { get; internal set; }
        public WorldPoint DesiredMovement { get; internal set; }
        public float MovementSpeedFactor { get; internal set; } = 1;
        public DashState Dash { get; } = new DashState();
        public ActorAppearance Appearance { get; internal set; }
        public LifeActivityState LifeActivity { get; } = new LifeActivityState();
        public bool HasLifeActivity => State == ActorActionState.Meditating || State == ActorActionState.Fishing;

        public StatusEffectContainer StatusEffects { get; }

        public AbilityBook Abilities { get; }

        public EntityId? LastDamagerId { get; internal set; }

        public bool CanAct => Vitals.IsAlive && State != ActorActionState.Dead && State != ActorActionState.Staggered;
        public ActorEntity(EntityId id, ActorSpec spec, ActorFaction faction, WorldPoint spawn, EntityId? ownerId, SourceIdAllocator sources)
        {
            if (id.Value <= 0 || (ownerId.HasValue && ownerId.Value.Value <= 0))
                throw new ArgumentException("Invalid actor ID.");
            Id = id;
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
            Appearance = spec.Appearance?.Default ?? default;
            Faction = Guard.EnumValue(faction);
            OwnerId = ownerId;
            Transform = new Transform2D(spawn, new WorldPoint(1, 0), spec.CollisionRadius);
            Stats = new StatSheet(spec.BaseStats);
            Vitals = new VitalResourcePool(Stats, Math.Max(1, Stats.Get(StatId.MaxHp)), Math.Max(0, Stats.Get(StatId.MaxQi)), 0);
            Abilities = new AbilityBook(spec.AbilityIds);
            StatusEffects = new StatusEffectContainer(this, sources);
        }

        public DomainResult SetStaggered(bool staggered)
        {
            if (!Vitals.IsAlive)
                return DomainResult.Fail(DomainError.InvalidState);
            if (staggered)
            {
                StopMotion();
                Abilities.Cast = null;
                State = ActorActionState.Staggered;
            }
            else if (State == ActorActionState.Staggered)
                State = ActorActionState.Idle;
            return DomainResult.Ok();
        }

        internal void StopMotion()
        {
            CancelLifeActivity();
            Transform.CurrentVelocity = default;
            DesiredMovement = default;
            Dash.Cancel();
        }

        internal void CancelLifeActivity()
        {
            LifeActivity.Reset();
            if (HasLifeActivity) State = ActorActionState.Idle;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Combat.Targeting;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Stats;
using LinhThu.Domain.Cultivation;
using LinhThu.Domain.Inventory;

namespace LinhThu.Domain.World
{
    public sealed class WorldState
    {
        internal readonly Dictionary<EntityId, ActorEntity> actors = new Dictionary<EntityId, ActorEntity>();
        internal readonly Dictionary<EntityId, WorldNodeEntity> nodes = new Dictionary<EntityId, WorldNodeEntity>();
        internal readonly Dictionary<EntityId, BeastAiController> ai = new Dictionary<EntityId, BeastAiController>();
        internal WorldPoint desiredMovement;
        private bool ticking;
        public bool IsTicking => ticking;
        public string MapDefinitionId { get; }

        public IDomainContent Content { get; }

        public WorldGeometry Geometry { get; }

        public IRandomSource Random { get; }

        public DomainEventBus Events { get; }

        public SimulationClock Clock { get; internal set; }

        public EntityIdAllocator Ids { get; internal set; }

        public SourceIdAllocator Sources { get; internal set; }

        public PlayerState Player { get; internal set; }

        public ActorEntity PlayerActor => actors[Player.ActorId];
        public CombatRules Rules { get; }

        public FactionRelations Relations { get; }

        public IReadOnlyDictionary<EntityId, ActorEntity> Actors { get; }

        public IReadOnlyDictionary<EntityId, WorldNodeEntity> Nodes { get; }

        public MovementMotor Motor { get; }

        public CombatResolver Combat { get; }

        public CultivationService Cultivation { get; }

        public WorldInteractionService Interactions { get; }

        public ItemUseService Items { get; }

        private readonly WorldCommandProcessor commands;
        public WorldState(IDomainContent content, WorldGeometry geometry, IRandomSource random, DomainEventBus events, WorldInitialState initialState, AbilityEffectRegistry effects, TargetSelectorRegistry targeting)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
            Random = random ?? throw new ArgumentNullException(nameof(random));
            Events = events ?? throw new ArgumentNullException(nameof(events));
            if (initialState == null || effects == null || targeting == null)
                throw new ArgumentNullException(nameof(initialState));
            MapDefinitionId = initialState.MapDefinitionId;
            Rules = initialState.Rules;
            Relations = initialState.Relations;
            Ids = new EntityIdAllocator();
            Sources = new SourceIdAllocator();
            Clock = new SimulationClock();
            Actors = new ReadOnlyDictionary<EntityId, ActorEntity>(actors);
            Nodes = new ReadOnlyDictionary<EntityId, WorldNodeEntity>(nodes);
            Motor = new MovementMotor(geometry);
            Combat = new CombatResolver(content, Rules, effects, targeting, random, events);
            Cultivation = new CultivationService(content, events);
            Interactions = new WorldInteractionService();
            Items = new ItemUseService();
            commands = new WorldCommandProcessor(this);
            if (!content.TryGetRealm(initialState.RealmDefinitionId, out var realm))
                throw new ArgumentException("Missing initial realm.");
            var spawn = TrySpawnActor(initialState.PlayerDefinitionId, ActorFaction.Player, initialState.PlayerSpawn);
            if (!spawn.Succeeded)
                throw new ArgumentException(spawn.Message + spawn.ErrorCode);
            int source = Sources.Allocate();
            Player = new PlayerState(spawn.Value.Id, source, realm, initialState.InventoryCapacity, content, Sources);
            PlayerActor.Stats.SetModifiersFromSource(source, realm.Modifiers.Select(m => m.FromSource(source)).ToArray());
            PlayerActor.Vitals.ClampToLimits();
            foreach (var node in initialState.Nodes)
            {
                if (!content.TryGetNode(node.DefinitionId, out var spec))
                    throw new ArgumentException("Missing node definition.");
                foreach (var reward in spec.Rewards)
                    if (!content.TryGetItem(reward.DefinitionId, out _))
                        throw new ArgumentException("Missing reward definition.");
                if (!node.Position.Finite || Math.Abs(node.Position.X) > geometry.HalfSize.X || Math.Abs(node.Position.Y) > geometry.HalfSize.Y)
                    throw new ArgumentException("Node outside map.");
                var id = Ids.Allocate();
                nodes.Add(id, new WorldNodeEntity(id, spec, node.Position, node.Ready));
            }

            foreach (var seed in initialState.Beasts)
            {
                if (!content.TryGetBeast(seed.DefinitionId, out var spec) || !content.TryGetActor(spec.ActorDefinitionId, out _))
                    throw new ArgumentException("Missing initial beast definition.");
                var result = Player.Beasts.Add(new BeastInstance(Ids.Allocate(), Player.ActorId, seed.DefinitionId, seed.QualityId, seed.BloodlineId, seed.Bond, seed.State));
                if (!result.Succeeded)
                    throw new ArgumentException("Invalid initial beast.");
            }
        }

        public bool TryGetActor(EntityId id, out ActorEntity actor) => actors.TryGetValue(id, out actor);
        public DomainResult<ActorEntity> TrySpawnActor(string definitionId, ActorFaction faction, WorldPoint position, EntityId? ownerId = null)
        {
            if (!Content.TryGetActor(definitionId, out var spec))
                return DomainResult<ActorEntity>.Fail(DomainError.MissingDefinition);
            if (!Enum.IsDefined(typeof(ActorFaction), faction))
                return DomainResult<ActorEntity>.Fail(DomainError.InvalidInput);
            if (ownerId.HasValue && !actors.ContainsKey(ownerId.Value))
                return DomainResult<ActorEntity>.Fail(DomainError.WrongOwner);
            if (!Geometry.CanStand(position, spec.CollisionRadius))
                return DomainResult<ActorEntity>.Fail(DomainError.Blocked);
            foreach (var ability in spec.AbilityIds)
                if (!Content.TryGetTechnique(ability, out _))
                    return DomainResult<ActorEntity>.Fail(DomainError.MissingDefinition);
            if (Ids.Next == long.MaxValue)
                return DomainResult<ActorEntity>.Fail(DomainError.CapacityExceeded);
            var actor = new ActorEntity(new EntityId(Ids.Next), spec, faction, position, ownerId, Sources);
            var context = new AbilityExecutionContext(this, actor, null, actor.Transform.FacingDirection, EffectSourceSnapshot.Capture(actor));
            foreach (var id in spec.AbilityIds)
            {
                Content.TryGetTechnique(id, out var technique);
                var validation = Combat.ValidateEffects(technique.Effects, context, new HashSet<string>(), false);
                if (!validation.Succeeded)
                    return DomainResult<ActorEntity>.Fail(validation.ErrorCode, validation.Message);
            }

            Ids.Allocate();
            actors.Add(actor.Id, actor);
            return DomainResult<ActorEntity>.Ok(actor);
        }

        public DomainResult RemoveActor(EntityId id)
        {
            if (Player != null && id == Player.ActorId)
                return DomainResult.Fail(DomainError.InvalidState);
            var beast = Player?.Beasts.All.FirstOrDefault(b => b.ActorId == id);
            if (beast != null)
                return Player.Beasts.TryRecall(beast.Id, this);
            if (!actors.ContainsKey(id))
                return DomainResult.Fail(DomainError.NotFound);
            RemoveActorInternal(id);
            return DomainResult.Ok();
        }

        internal void RemoveActorInternal(EntityId id)
        {
            actors.Remove(id);
            ai.Remove(id);
        }

        public void DealDamage(ActorEntity target, EntityId source, float amount)
        {
            if (target == null || !actors.TryGetValue(target.Id, out var actual) || !ReferenceEquals(target, actual))
                throw new ArgumentException(nameof(target));
            var applied = target.Vitals.ApplyDamage(amount);
            if (applied.HpLost + applied.ShieldLost == 0)
                return;
            target.LastDamagerId = source;
            Events.Publish(new ActorDamagedEvent(target.Id, source, applied.HpLost, applied.ShieldLost));
            if (applied.DiedNow)
            {
                target.State = ActorActionState.Dead;
                target.Abilities.Cast = null;
                target.StatusEffects.OnDeath();
                Events.Publish(new ActorDiedEvent(target.Id, source));
            }
        }

        public DomainResult Execute(WorldCommand command)
        {
            if (ticking)
                throw new InvalidOperationException("Commands execute at simulation boundaries.");
            var result = commands.Execute(command);
            Events.Flush();
            return result;
        }

        public void Tick(float deltaTime)
        {
            Guard.Delta(deltaTime);
            if (deltaTime == 0)
                return;
            if (ticking)
                throw new InvalidOperationException("Reentrant world tick.");
            ticking = true;
            try
            {
                Clock.Advance(deltaTime);
                foreach (var actor in actors.Values.OrderBy(a => a.Id).ToArray())
                {
                    actor.Vitals.Regenerate(deltaTime);
                    actor.Abilities.Tick(deltaTime);
                    actor.StatusEffects.Tick(deltaTime, this);
                    if (!actor.Vitals.IsAlive && actor.State != ActorActionState.Dead)
                    {
                        actor.State = ActorActionState.Dead;
                        actor.Abilities.Cast = null;
                        actor.StatusEffects.OnDeath();
                        Events.Publish(new ActorDiedEvent(actor.Id, actor.LastDamagerId ?? actor.Id));
                    }
                }

                Player.Beasts.Reconcile(this);
                Combat.Tick(this, deltaTime);
                Player.Beasts.Reconcile(this);
                foreach (var beast in Player.Beasts.All.Where(b => b.State == BeastState.SummonedActive).OrderBy(b => b.Id).ToArray())
                {
                    if (!ai.TryGetValue(beast.ActorId.Value, out var controller))
                    {
                        Content.TryGetBeast(beast.DefinitionId, out var spec);
                        controller = new BeastAiController(beast.ActorId.Value, beast.OwnerId, spec.Ai);
                        ai.Add(beast.ActorId.Value, controller);
                    }

                    controller.Tick(this, deltaTime);
                }

                if (PlayerActor.CanAct && PlayerActor.Abilities.Cast == null)
                {
                    Motor.Move(PlayerActor.Transform, desiredMovement, Math.Min(100, Math.Max(0, PlayerActor.Stats.Get(StatId.MoveSpeed))), deltaTime, MovementMode.Walk);
                    PlayerActor.State = desiredMovement.LengthSquared > 0 ? ActorActionState.Moving : ActorActionState.Idle;
                }

                foreach (var node in nodes.Values.OrderBy(n => n.Id))
                    node.Tick(deltaTime);
                Player.Beasts.Reconcile(this);
            }
            finally
            {
                ticking = false;
            }

            Events.Flush();
        }
    }
}

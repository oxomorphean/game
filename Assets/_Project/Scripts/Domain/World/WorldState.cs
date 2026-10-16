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
        internal readonly Dictionary<EntityId, PlayerState> players = new Dictionary<EntityId, PlayerState>();
        internal readonly Dictionary<EntityId, WildBeastAiController> wildBeasts = new Dictionary<EntityId, WildBeastAiController>();
        public IReadOnlyDictionary<EntityId, WildBeastAiController> WildBeasts { get; }
        public BeastTamingService Taming { get; } = new BeastTamingService();
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

        public EntityId LocalPlayerId { get; internal set; }
        public PlayerState LocalPlayer => players[LocalPlayerId];
        public PlayerState Player => LocalPlayer;
        public IReadOnlyDictionary<EntityId, PlayerState> Players { get; }
        public SpatialChunkGrid SpatialGrid => Geometry.SpatialGrid;

        public ActorEntity PlayerActor => actors[Player.ActorId];
        public CombatRules Rules { get; }

        public FactionRelations Relations { get; }

        public IReadOnlyDictionary<EntityId, ActorEntity> Actors { get; }

        public IReadOnlyDictionary<EntityId, WorldNodeEntity> Nodes { get; }

        public MovementMotor Motor { get; }

        public CombatResolver Combat { get; }

        public CultivationService Cultivation { get; }

        public WorldInteractionService Interactions { get; }
        public LifeActivityService LifeActivities { get; }

        public InteractionTargetSelector InteractionTargets { get; } = new InteractionTargetSelector();

        public WorldNodeEntity FocusedNode => InteractionTargets.Select(this);

        public EntityId? FocusedNodeId => FocusedNode?.Id;

        public ItemUseService Items { get; }

        private readonly WorldCommandProcessor commands;
        public WorldState(IDomainContent content, WorldGeometry geometry, IRandomSource random, DomainEventBus events, WorldInitialState initialState, AbilityEffectRegistry effects, TargetSelectorRegistry targeting)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Geometry = geometry?.Copy() ?? throw new ArgumentNullException(nameof(geometry));
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
            WildBeasts = new ReadOnlyDictionary<EntityId, WildBeastAiController>(wildBeasts);
            Players = new ReadOnlyDictionary<EntityId, PlayerState>(players);
            Actors = new ReadOnlyDictionary<EntityId, ActorEntity>(actors);
            Nodes = new ReadOnlyDictionary<EntityId, WorldNodeEntity>(nodes);
            Motor = new MovementMotor(Geometry);
            Combat = new CombatResolver(content, Rules, effects, targeting, random, events);
            Cultivation = new CultivationService(content, events);
            Interactions = new WorldInteractionService();
            LifeActivities = new LifeActivityService(this);
            Items = new ItemUseService();
            commands = new WorldCommandProcessor(this);
            if (!content.TryGetRealm(initialState.RealmDefinitionId, out var realm))
                throw new ArgumentException("Missing initial realm.");
            var spawn = TrySpawnActor(initialState.PlayerDefinitionId, ActorFaction.Player, initialState.PlayerSpawn);
            if (!spawn.Succeeded)
                throw new ArgumentException(spawn.Message + spawn.ErrorCode);
            int source = Sources.Allocate();
            LocalPlayerId = spawn.Value.Id;
            players.Add(LocalPlayerId, new PlayerState(LocalPlayerId, source, realm, initialState.InventoryCapacity, content, Sources));
            PlayerActor.Stats.SetModifiersFromSource(source, realm.Modifiers.Select(m => m.FromSource(source)).ToArray());
            PlayerActor.Vitals.ClampToLimits();
            foreach (var node in initialState.Nodes)
            {
                if (!content.TryGetNode(node.DefinitionId, out var spec))
                    throw new ArgumentException("Missing node definition.");
                foreach (var reward in spec.Rewards)
                    if (!content.TryGetItem(reward.DefinitionId, out _))
                        throw new ArgumentException("Missing reward definition.");
                if (!geometry.Bounds.Contains(node.Position, 0))
                    throw new ArgumentException("Node outside map.");
                var id = Ids.Allocate();
                var entity = new WorldNodeEntity(id, spec, node.Position, node.Ready);
                nodes.Add(id, entity);
                SpatialGrid.RegisterNode(entity);
            }

            foreach (var seed in initialState.Beasts)
            {
                if (!content.TryGetBeast(seed.DefinitionId, out var spec) || !content.TryGetActor(spec.ActorDefinitionId, out _))
                    throw new ArgumentException("Missing initial beast definition.");
                var result = Player.Beasts.Add(new BeastInstance(Ids.Allocate(), Player.ActorId, seed.DefinitionId, seed.QualityId, seed.BloodlineId, seed.Bond, seed.State));
                if (!result.Succeeded)
                    throw new ArgumentException("Invalid initial beast.");
            }
            foreach (var seed in initialState.WildBeasts)
            {
                var result = TryAddWildBeast(seed);
                if (!result.Succeeded) throw new ArgumentException("Invalid wild beast: " + result.ErrorCode);
            }
        }

        public DomainResult<WildBeastAiController> TryAddWildBeast(WildBeastSeed seed)
        {
            if (!Content.TryGetWildBeast(seed.DefinitionId, out var spec) || !Content.TryGetActor(spec.ActorDefinitionId, out var actorSpec)
                || !actorSpec.AbilityIds.Contains(spec.AttackId) || !Content.TryGetTechnique(spec.AttackId, out var attack)
                || attack.CastDuration <= 0 || (spec.SpeciesId != null && !Content.TryGetBeast(spec.SpeciesId, out _))
                || spec.Loot.Any(item => !Content.TryGetItem(item.DefinitionId, out _)))
                return DomainResult<WildBeastAiController>.Fail(DomainError.MissingDefinition);
            if (Ids.Next >= long.MaxValue - 1 || wildBeasts.Count >= 10000) return DomainResult<WildBeastAiController>.Fail(DomainError.CapacityExceeded);
            var spawn = TrySpawnActor(spec.ActorDefinitionId, spec.Faction, seed.Home);
            if (!spawn.Succeeded) return DomainResult<WildBeastAiController>.Fail(spawn.ErrorCode, spawn.Message);
            var controller = new WildBeastAiController(Ids.Allocate(), spec, seed.Home) { ActorId = spawn.Value.Id };
            spawn.Value.WildBeast = controller; wildBeasts.Add(controller.Id, controller);
            return DomainResult<WildBeastAiController>.Ok(controller);
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
            SpatialGrid.RegisterActor(actor);
            return DomainResult<ActorEntity>.Ok(actor);
        }

        public DomainResult RemoveActor(EntityId id)
        {
            if (players.ContainsKey(id))
                return DomainResult.Fail(DomainError.InvalidState);
            var beast = players.Values.SelectMany(p => p.Beasts.All).FirstOrDefault(b => b.ActorId == id);
            if (beast != null)
                return players[beast.OwnerId].Beasts.TryRecall(beast.Id, this);
            if (!actors.TryGetValue(id, out var actor))
                return DomainResult.Fail(DomainError.NotFound);
            if (actor.WildBeast != null) actor.WildBeast.Deplete(this, null);
            else RemoveActorInternal(id);
            return DomainResult.Ok();
        }

        internal void RemoveActorInternal(EntityId id)
        {
            SpatialGrid.RemoveActor(id);
            actors.Remove(id);
            ai.Remove(id);
        }

        public void DealDamage(ActorEntity target, EntityId source, float amount, EntityId? rewardOwnerId = null)
        {
            if (target == null || !actors.TryGetValue(target.Id, out var actual) || !ReferenceEquals(target, actual))
                throw new ArgumentException(nameof(target));
            var applied = target.Vitals.ApplyDamage(amount);
            if (applied.HpLost + applied.ShieldLost == 0)
                return;
            target.CancelLifeActivity();
            target.LastDamagerId = source;
            Events.Publish(new ActorDamagedEvent(target.Id, source, applied.HpLost, applied.ShieldLost));
            if (applied.DiedNow)
            {
                target.StopMotion();
                target.State = ActorActionState.Dead;
                target.Abilities.Cast = null;
                target.StatusEffects.OnDeath();
                Events.Publish(new ActorDiedEvent(target.Id, source));
                var owner = rewardOwnerId ?? (players.ContainsKey(source) ? source : actors.TryGetValue(source, out var attacker) ? attacker.OwnerId : null);
                target.WildBeast?.Deplete(this, owner.HasValue && players.ContainsKey(owner.Value) ? owner : null);
            }
        }

        /// <summary>Authenticated adapters supply the sender; a payload cannot choose another player.</summary>
        public DomainResult ExecuteFor(EntityId playerId, WorldCommand command)
        {
            if (!players.ContainsKey(playerId) || command == null || command.ActorId != playerId)
                return DomainResult.Fail(DomainError.WrongOwner);
            return Execute(command);
        }

        public DomainResult<PlayerState> TryAddPlayer(string actorDefinitionId, string realmDefinitionId, WorldPoint position, int capacity = 24)
        {
            if (ticking || capacity < 1 || capacity > 10000) return DomainResult<PlayerState>.Fail(DomainError.InvalidInput);
            if (!Content.TryGetRealm(realmDefinitionId, out var realm) || !Content.TryGetActor(actorDefinitionId, out var spec))
                return DomainResult<PlayerState>.Fail(DomainError.MissingDefinition);
            if (Sources.Next == int.MaxValue || players.Count >= 1000) return DomainResult<PlayerState>.Fail(DomainError.CapacityExceeded);
            try
            {
                var probe = new StatSheet(spec.BaseStats);
                probe.SetModifiersFromSource(Sources.Next, realm.Modifiers.Select(m => m.FromSource(Sources.Next)).ToArray());
            }
            catch (ArgumentException e) { return DomainResult<PlayerState>.Fail(DomainError.InvalidInput, e.Message); }
            catch (OverflowException e) { return DomainResult<PlayerState>.Fail(DomainError.CapacityExceeded, e.Message); }
            var spawn = TrySpawnActor(actorDefinitionId, ActorFaction.Player, position);
            if (!spawn.Succeeded) return DomainResult<PlayerState>.Fail(spawn.ErrorCode, spawn.Message);
            int source = Sources.Allocate();
            var player = new PlayerState(spawn.Value.Id, source, realm, capacity, Content, Sources);
            players.Add(player.ActorId, player);
            spawn.Value.Stats.SetModifiersFromSource(source, realm.Modifiers.Select(m => m.FromSource(source)).ToArray());
            spawn.Value.Vitals.ClampToLimits();
            return DomainResult<PlayerState>.Ok(player);
        }

        public DomainResult Execute(WorldCommand command)
        {
            if (ticking)
                throw new InvalidOperationException("Commands execute at simulation boundaries.");
            var result = commands.Execute(command);
            Events.Flush();
            return result;
        }

        internal DomainResult TryDash(EntityId id, WorldPoint direction)
        {
            if (!direction.Finite)
                return DomainResult.Fail(DomainError.InvalidInput);
            if (!TryGetActor(id, out var actor))
                return DomainResult.Fail(DomainError.NotFound);
            if (!actor.CanAct || actor.Abilities.Cast != null || actor.Dash.Active)
                return DomainResult.Fail(DomainError.InvalidState);
            if (actor.Dash.CooldownRemaining > .000001f)
                return DomainResult.Fail(DomainError.OnCooldown);
            float peak = Math.Min(100, Math.Max(0, actor.Stats.Get(StatId.MoveSpeed)) * actor.Spec.Movement.DashSpeedMultiplier);
            if (peak <= 0)
                return DomainResult.Fail(DomainError.InvalidState);
            if (direction.LengthSquared < .000001f)
                direction = actor.Transform.FacingDirection;
            actor.CancelLifeActivity();
            actor.Dash.Start(direction, peak, actor.Spec.Movement);
            actor.Transform.FacingDirection = actor.Dash.Direction;
            actor.State = ActorActionState.Dashing;
            return DomainResult.Ok();
        }

        private void ResolveMovement(ActorEntity actor, float deltaTime)
        {
            Motor.RecoverOverlap(actor.Transform);
            if (!actor.CanAct || actor.Abilities.Cast != null || actor.HasLifeActivity)
            {
                actor.Transform.CurrentVelocity = default;
                actor.Dash.Cancel();
                return;
            }
            float speed = Math.Min(100, Math.Max(0, actor.Stats.Get(StatId.MoveSpeed)) * actor.MovementSpeedFactor);
            if (actor.Dash.Active)
            {
                float continuationSpeed = speed * Math.Min(1, actor.DesiredMovement.Length);
                float distance = actor.Dash.Advance(deltaTime, continuationSpeed, out var duration, out var endSpeed);
                Motor.Dash(actor.Transform, actor.Dash.Direction, distance, endSpeed, duration);
                actor.Transform.FacingDirection = actor.Dash.Direction;
                if (actor.Dash.Active)
                    return;
                deltaTime -= duration;
                if (deltaTime < .000001f)
                {
                    actor.State = actor.Transform.CurrentVelocity.LengthSquared > .0001f ? ActorActionState.Moving : ActorActionState.Idle;
                    return;
                }
            }
            Motor.Move(actor.Transform, actor.DesiredMovement, speed, deltaTime, MovementMode.Walk, actor.Spec.Movement);
            actor.State = actor.Transform.CurrentVelocity.LengthSquared > .0001f ? ActorActionState.Moving : ActorActionState.Idle;
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
                    actor.Dash.TickCooldown(deltaTime);
                    actor.Vitals.Regenerate(deltaTime, actor.State == ActorActionState.Meditating ? actor.Spec.LifeActivities.MeditationQiMultiplier : 1);
                    actor.Abilities.Tick(deltaTime);
                    if (!actor.Vitals.IsAlive && actor.State != ActorActionState.Dead)
                    {
                        actor.StopMotion();
                        actor.State = ActorActionState.Dead;
                        actor.Abilities.Cast = null;
                        actor.StatusEffects.OnDeath();
                        Events.Publish(new ActorDiedEvent(actor.Id, actor.LastDamagerId ?? actor.Id));
                        actor.WildBeast?.Deplete(this, null);
                    }
                }

                foreach (var player in players.Values) player.Beasts.Reconcile(this);
                foreach (var controller in wildBeasts.Values.OrderBy(b => b.Id)) controller.Tick(this, deltaTime);
                Combat.Tick(this, deltaTime);
                LifeActivities.Tick(deltaTime);
                foreach (var player in players.Values) player.Beasts.Reconcile(this);
                foreach (var beast in players.Values.SelectMany(p => p.Beasts.All).Where(b => b.State == BeastState.SummonedActive).OrderBy(b => b.Id).ToArray())
                {
                    if (!ai.TryGetValue(beast.ActorId.Value, out var controller))
                    {
                        Content.TryGetBeast(beast.DefinitionId, out var spec);
                        controller = new BeastAiController(beast.ActorId.Value, beast.OwnerId, spec.Ai);
                        ai.Add(beast.ActorId.Value, controller);
                    }

                    controller.Tick(this, deltaTime);
                }

                foreach (var actor in actors.Values.OrderBy(a => a.Id))
                    ResolveMovement(actor, deltaTime);

                foreach (var node in nodes.Values.OrderBy(n => n.Id))
                    node.Tick(deltaTime);
                foreach (var player in players.Values) player.Beasts.Reconcile(this);
            }
            finally
            {
                ticking = false;
            }

            Events.Flush();
        }
    }
}

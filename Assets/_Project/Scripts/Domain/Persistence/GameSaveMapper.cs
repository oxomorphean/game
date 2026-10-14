using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.Stats;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Combat.Targeting;
using LinhThu.Domain.Cultivation;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.World;

namespace LinhThu.Domain.Persistence
{
    public sealed class GameSaveMapper
    {
        private readonly Func<AbilityEffectRegistry> effects;
        private readonly Func<TargetSelectorRegistry> targeting;
        public GameSaveMapper(Func<AbilityEffectRegistry> effects = null, Func<TargetSelectorRegistry> targeting = null)
        {
            this.effects = effects ?? WorldFactory.CreateEffects;
            this.targeting = targeting ?? WorldFactory.CreateTargeting;
        }

        public GameSaveDto Capture(WorldState world)
        {
            if (world == null || world.IsTicking || world.Events.HasPending)
                throw new InvalidOperationException("Capture requires a committed, flushed simulation boundary.");
            var player = world.Player;
            var rng = world.Random.Capture();
            var dto = new GameSaveDto{MapDefinitionId = world.MapDefinitionId, SimulationSeconds = world.Clock.ElapsedSeconds, TickIndex = world.Clock.TickIndex, NextEntityId = world.Ids.Next, NextSourceId = world.Sources.Next, Random = new RandomStateDto{AlgorithmId = rng.AlgorithmId, State = rng.State}, DesiredMovement = new PointDto(world.desiredMovement)};
            dto.Rules = new RulesSaveDto{DefenseScale = world.Rules.DefenseScale, OffenseScale = world.Rules.OffenseScale, ResistanceScale = world.Rules.ResistanceScale, CooldownReductionCap = world.Rules.CooldownReductionCap, Elements = world.Rules.Elements.Entries.OrderBy(e => e.Attack).ThenBy(e => e.Defense).Select(e => new ElementSaveDto{Attack = e.Attack, Defense = e.Defense, Factor = e.Factor}).ToList(), Hostilities = world.Relations.Entries.OrderBy(f => f.From).ThenBy(f => f.To).Select(f => new FactionSaveDto{From = f.From, To = f.To}).ToList()};
            dto.Player = new PlayerSaveDto{ActorId = player.ActorId.Value, RealmSourceId = player.RealmSourceId, RealmId = player.Cultivation.CurrentRealmId, SubStage = (int)player.Cultivation.SubStage, CultivationExp = player.Cultivation.CurrentCultivationExp, InventoryCapacity = player.Inventory.Capacity, Inventory = player.Inventory.Slots.Select(i => new ItemSaveDto{DefinitionId = i.DefinitionId, Count = i.Count}).ToList(), Equipment = player.Equipment.Slots.OrderBy(e => e.Key).Select(e => new EquipmentSaveDto{SlotId = e.Key, ItemId = e.Value.ItemId, SourceId = e.Value.SourceId}).ToList(), Beasts = player.Beasts.All.OrderBy(b => b.Id).Select(b => new BeastSaveDto{Id = b.Id.Value, OwnerId = b.OwnerId.Value, DefinitionId = b.DefinitionId, QualityId = b.QualityId, BloodlineId = b.BloodlineId, Bond = b.Bond, State = b.State, ActorId = b.ActorId?.Value, AssignedFieldId = b.AssignedFieldId?.Value, BondSourceId = b.BondSourceId, RestingHp = b.RestingHp, RestingQi = b.RestingQi}).ToList()};
            var playerSources = new HashSet<int>(player.Equipment.Slots.Values.Select(e => e.SourceId))
            {player.RealmSourceId};
            foreach (var b in player.Beasts.All)
                if (b.BondSourceId > 0)
                    playerSources.Add(b.BondSourceId);
            foreach (var actor in world.Actors.Values.OrderBy(a => a.Id))
            {
                var derived = new HashSet<int>(actor.StatusEffects.Active.Select(s => s.SourceId));
                if (actor.Id == player.ActorId)
                    derived.UnionWith(playerSources);
                var a = new ActorSaveDto{Id = actor.Id.Value, DefinitionId = actor.DefinitionId, Faction = actor.Faction, OwnerId = actor.OwnerId?.Value, LastDamagerId = actor.LastDamagerId?.Value, Position = new PointDto(actor.Transform.Position), Facing = new PointDto(actor.Transform.FacingDirection), Hp = actor.Vitals.CurrentHp, Qi = actor.Vitals.CurrentQi, Shield = actor.Vitals.CurrentShield, State = actor.State, BaseStats = actor.Stats.BaseValues.ToDictionary(x => x.Key, x => x.Value), Cooldowns = actor.Abilities.Cooldowns.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value)};
                a.ManualModifiers = actor.Stats.GetModifiers().Where(m => !derived.Contains(m.SourceId)).Select(m => new ModifierSaveDto{StatId = m.StatId, Type = m.Type, Value = m.Value, SourceId = m.SourceId}).ToList();
                a.Statuses = actor.StatusEffects.Active.Select(s => new StatusSaveDto{DefinitionId = s.DefinitionId, SourceId = s.SourceId, CasterId = s.CasterId.Value, StackCount = s.StackCount, RemainingDuration = s.RemainingDuration, UntilNextTick = s.UntilNextTick, Source = CaptureSource(s.Source)}).ToList();
                var cast = actor.Abilities.Cast;
                if (cast != null)
                    a.Cast = new CastSaveDto{TechniqueId = cast.TechniqueId, Direction = new PointDto(cast.Aim.Direction), TargetId = cast.Aim.TargetId?.Value, WindupRemaining = cast.WindupRemaining, ChannelRemaining = cast.ChannelRemaining, UntilPulse = cast.UntilPulse, Started = cast.Started};
                dto.Actors.Add(a);
            }

            dto.Nodes = world.Nodes.Values.OrderBy(n => n.Id).Select(n => new WorldNodeSaveDto{Id = n.Id.Value, DefinitionId = n.DefinitionId, Position = new PointDto(n.Position), GrowthTimer = n.GrowthTimer, IsDepleted = n.IsDepleted, CropDefinitionId = n.CropDefinitionId, AssignedBeastId = n.AssignedBeastId?.Value}).ToList();
            dto.Projectiles = world.Combat.Projectiles.OrderBy(p => p.Id).Select(p => new ProjectileSaveDto{Id = p.Id.Value, TechniqueId = p.TechniqueId, Source = CaptureSource(p.Source), Position = new PointDto(p.Position), Direction = new PointDto(p.Direction), RemainingLifetime = p.RemainingLifetime, HitTargets = p.HitTargets.Select(id => id.Value).ToList()}).ToList();
            return dto;
        }

        private static SourceSaveDto CaptureSource(EffectSourceSnapshot source) => new SourceSaveDto{ActorId = source.ActorId.Value, Faction = source.Faction, Stats = source.Stats.ToDictionary(s => s.Key, s => s.Value)};
        private static EffectSourceSnapshot RestoreSource(SourceSaveDto source, long nextEntity)
        {
            Require(source.ActorId > 0 && source.ActorId < nextEntity, "Invalid effect source ID.");
            return new EffectSourceSnapshot(new EntityId(source.ActorId), source.Faction, source.Stats);
        }

        private static void Require(bool condition, string message) => SaveValidation.Require(condition, message);
        /// <summary>Supply fresh RNG/event instances. Builds an isolated graph; the caller swaps worlds only on success.</summary>
        public DomainResult<WorldState> TryRestore(GameSaveDto dto, IDomainContent content, WorldGeometry geometry, IRandomSource random, DomainEventBus events)
        {
            var structure = SaveValidation.Structure(dto);
            if (!structure.Succeeded)
                return DomainResult<WorldState>.Fail(structure.ErrorCode, structure.Message);
            if (content == null || geometry == null || random == null || events == null || events.HasPending)
                return DomainResult<WorldState>.Fail(DomainError.InvalidInput, "Fresh dependencies required.");
            try
            {
                var ids = new HashSet<long>();
                var sources = new HashSet<int>();
                void Id(long id)
                {
                    Require(id > 0 && id < dto.NextEntityId && ids.Add(id), "Duplicate or invalid entity ID.");
                }

                void Source(int id)
                {
                    Require(id > 0 && id < dto.NextSourceId && sources.Add(id), "Duplicate or invalid modifier source ID.");
                }

                foreach (var a in dto.Actors)
                    Id(a.Id);
                foreach (var n in dto.Nodes)
                    Id(n.Id);
                foreach (var b in dto.Player.Beasts)
                    Id(b.Id);
                foreach (var p in dto.Projectiles)
                    Id(p.Id);
                var playerDto = dto.Actors.SingleOrDefault(a => a.Id == dto.Player.ActorId);
                Require(playerDto != null && playerDto.Faction == ActorFaction.Player && playerDto.OwnerId == null, "Missing player actor.");
                Require(content.TryGetRealm(dto.Player.RealmId, out var realm), "Missing realm definition.");
                var r = dto.Rules;
                var rules = new CombatRules(new ElementInteractionTable(r.Elements.Select(e => new ElementInteraction(e.Attack, e.Defense, e.Factor))), r.DefenseScale, r.OffenseScale, r.ResistanceScale, r.CooldownReductionCap);
                var relations = new FactionRelations(r.Hostilities.Select(f => new FactionPair(f.From, f.To)));
                Require(random.Capture().AlgorithmId == dto.Random.AlgorithmId && dto.Random.AlgorithmId == SeededRandomSource.Algorithm && dto.Random.State != 0, "Unsupported RNG state.");
                var world = new WorldState(content, geometry, random, events, new WorldInitialState(playerDto.DefinitionId, realm.DefinitionId, dto.MapDefinitionId, playerDto.Position.ToPoint(), dto.Player.InventoryCapacity, rules, relations), effects(), targeting());
                world.actors.Clear();
                world.Ids = new EntityIdAllocator(dto.NextEntityId);
                world.Sources = new SourceIdAllocator(dto.NextSourceId);
                world.Clock = new SimulationClock(dto.SimulationSeconds, dto.TickIndex);
                world.desiredMovement = dto.DesiredMovement.ToPoint();
                foreach (var a in dto.Actors)
                {
                    Require(content.TryGetActor(a.DefinitionId, out var spec), "Missing actor definition.");
                    Guard.EnumValue(a.State);
                    Require(!a.OwnerId.HasValue || dto.Actors.Any(o => o.Id == a.OwnerId.Value && o.Id != a.Id), "Actor owner missing.");
                    Require(!a.LastDamagerId.HasValue || (a.LastDamagerId.Value > 0 && a.LastDamagerId.Value < dto.NextEntityId), "Invalid last attacker.");
                    Require(geometry.CanStand(a.Position.ToPoint(), spec.CollisionRadius), "Blocked actor position.");
                    foreach (var cd in a.Cooldowns)
                    {
                        Require(content.TryGetTechnique(cd.Key, out var ability), "Missing learned technique.");
                        Guard.Number(cd.Value, "cooldown", 0, ability.Cooldown);
                    }

                    var runtimeSpec = new ActorSpec(spec.DefinitionId, spec.PrimaryElement, a.BaseStats, a.Cooldowns.Keys, spec.CollisionRadius);
                    var actor = new ActorEntity(new EntityId(a.Id), runtimeSpec, a.Faction, a.Position.ToPoint(), a.OwnerId.HasValue ? new EntityId(a.OwnerId.Value) : (EntityId? )null, world.Sources);
                    var transform = new Transform2D(a.Position.ToPoint(), a.Facing.ToPoint(), spec.CollisionRadius);
                    actor.Transform.FacingDirection = transform.FacingDirection;
                    actor.State = a.State;
                    actor.LastDamagerId = a.LastDamagerId.HasValue ? new EntityId(a.LastDamagerId.Value) : (EntityId? )null;
                    foreach (var group in a.ManualModifiers.GroupBy(m => m.SourceId))
                    {
                        Source(group.Key);
                        actor.Stats.SetModifiersFromSource(group.Key, group.Select(m => new StatModifier(m.StatId, m.Type, m.Value, m.SourceId)).ToArray());
                    }

                    foreach (var cd in a.Cooldowns)
                        actor.Abilities.StartCooldown(cd.Key, cd.Value);
                    foreach (var s in a.Statuses)
                    {
                        Source(s.SourceId);
                        Require(content.TryGetStatus(s.DefinitionId, out var status), "Missing status definition.");
                        Require(s.CasterId == s.Source.ActorId && s.StackCount >= 1 && s.StackCount <= status.MaxStacks && (status.StackPolicy != StatusStackPolicy.IndependentInstances || s.StackCount == 1), "Invalid status stack or caster.");
                        Guard.Number(s.RemainingDuration, "status duration", .0000001f, status.Duration);
                        Guard.Number(s.UntilNextTick, "status tick", 0, status.TickInterval);
                        Require(status.TickInterval == 0 || s.UntilNextTick > 0, "Invalid next status tick.");
                        Require(a.Hp > 0 || !status.ClearOnDeath, "Dead actor retains death-cleared status.");
                        Require(status.StackPolicy == StatusStackPolicy.IndependentInstances || !actor.StatusEffects.Active.Any(x => x.DefinitionId == s.DefinitionId && x.CasterId.Value == s.CasterId), "Duplicate grouped status.");
                        actor.StatusEffects.Restore(new StatusInstance(s.SourceId, status, new EntityId(s.CasterId), RestoreSource(s.Source, dto.NextEntityId))
                        {StackCount = s.StackCount, RemainingDuration = s.RemainingDuration, UntilNextTick = s.UntilNextTick});
                    }

                    world.actors.Add(actor.Id, actor);
                }

                Source(dto.Player.RealmSourceId);
                world.Player = new PlayerState(new EntityId(dto.Player.ActorId), dto.Player.RealmSourceId, realm, dto.Player.InventoryCapacity, content, world.Sources, dto.Player.CultivationExp);
                Require((int)world.Player.Cultivation.SubStage == dto.Player.SubStage, "Cultivation substage mismatch.");
                world.PlayerActor.Stats.SetModifiersFromSource(dto.Player.RealmSourceId, realm.Modifiers.Select(m => m.FromSource(dto.Player.RealmSourceId)).ToArray());
                Require(dto.Player.Inventory.Count <= dto.Player.InventoryCapacity, "Inventory over capacity.");
                foreach (var item in dto.Player.Inventory)
                {
                    Require(content.TryGetItem(item.DefinitionId, out var spec), "Missing inventory definition.");
                    Require(item.Count > 0 && item.Count <= spec.MaxStack, "Invalid inventory stack.");
                }

                var items = dto.Player.Inventory.Select(i => new ItemStack(i.DefinitionId, i.Count)).ToArray();
                foreach (var group in items.GroupBy(i => i.DefinitionId))
                    Require(group.Sum(i => (long)i.Count) <= int.MaxValue, "Inventory count overflow.");
                world.Player.Inventory.Replace(items);
                foreach (var equipment in dto.Player.Equipment)
                {
                    Source(equipment.SourceId);
                    Require(content.TryGetItem(equipment.ItemId, out var item) && item.Category == ItemCategory.Artifact && item.EquipmentSlot == equipment.SlotId, "Invalid equipment.");
                    world.Player.Equipment.Restore(equipment.SlotId, new EquippedItem(equipment.ItemId, equipment.SourceId), world.PlayerActor);
                }

                foreach (var n in dto.Nodes)
                {
                    Require(content.TryGetNode(n.DefinitionId, out var spec), "Missing node definition.");
                    Require(n.Position.ToPoint().Finite && Math.Abs(n.Position.X) <= geometry.HalfSize.X && Math.Abs(n.Position.Y) <= geometry.HalfSize.Y, "Node outside map.");
                    var node = new WorldNodeEntity(new EntityId(n.Id), spec, n.Position.ToPoint())
                    {GrowthTimer = Guard.Number(n.GrowthTimer, "node timer", 0, 86400), IsDepleted = n.IsDepleted, CropDefinitionId = n.CropDefinitionId, AssignedBeastId = n.AssignedBeastId.HasValue ? new EntityId(n.AssignedBeastId.Value) : (EntityId? )null};
                    if (n.CropDefinitionId != null)
                    {
                        Require(spec.Kind == WorldNodeKind.SpiritFieldPlot && content.TryGetNode(n.CropDefinitionId, out _), "Invalid crop definition.");
                        content.TryGetNode(n.CropDefinitionId, out var crop);
                        Require(crop.Kind == WorldNodeKind.HerbNode && crop.SeedItemId != null && content.TryGetItem(crop.SeedItemId, out _), "Invalid crop definition.");
                        foreach (var reward in crop.Rewards)
                            Require(content.TryGetItem(reward.DefinitionId, out _), "Missing crop reward.");
                    }

                    Require(!n.AssignedBeastId.HasValue || spec.Kind == WorldNodeKind.SpiritFieldPlot, "Only fields can have assigned beasts.");
                    foreach (var reward in spec.Rewards)
                        Require(content.TryGetItem(reward.DefinitionId, out _), "Missing node reward.");
                    world.nodes.Add(node.Id, node);
                }

                foreach (var b in dto.Player.Beasts)
                {
                    Require(b.OwnerId == dto.Player.ActorId, "Invalid beast owner.");
                    Require(content.TryGetBeast(b.DefinitionId, out var spec), "Missing beast species.");
                    Guard.EnumValue(b.State);
                    if (b.RestingHp.HasValue)
                        Guard.Number(b.RestingHp.Value, "resting hp");
                    if (b.RestingQi.HasValue)
                        Guard.Number(b.RestingQi.Value, "resting qi");
                    var beast = new BeastInstance(new EntityId(b.Id), new EntityId(b.OwnerId), b.DefinitionId, b.QualityId, b.BloodlineId, b.Bond, b.State)
                    {ActorId = b.ActorId.HasValue ? new EntityId(b.ActorId.Value) : (EntityId? )null, AssignedFieldId = b.AssignedFieldId.HasValue ? new EntityId(b.AssignedFieldId.Value) : (EntityId? )null, BondSourceId = b.BondSourceId, RestingHp = b.RestingHp, RestingQi = b.RestingQi};
                    if (b.State == BeastState.SummonedActive)
                    {
                        Require(b.ActorId.HasValue && !b.AssignedFieldId.HasValue, "Invalid active beast.");
                        Source(b.BondSourceId);
                        Require(world.actors.TryGetValue(beast.ActorId.Value, out var actor) && actor.OwnerId == beast.OwnerId && actor.Faction == ActorFaction.PlayerBeast && actor.DefinitionId == spec.ActorDefinitionId, "Missing beast actor.");
                        Require(!world.Player.Beasts.All.Any(x => x.ActorId == beast.ActorId), "Duplicate beast actor.");
                        new BondStatLink().Refresh(world.PlayerActor, actor, spec, b.BondSourceId);
                    }
                    else
                    {
                        Require(!b.ActorId.HasValue && b.BondSourceId == 0, "Inactive beast has combat state.");
                        if (b.State == BeastState.AssignedToField)
                            Require(b.AssignedFieldId.HasValue && world.nodes.TryGetValue(new EntityId(b.AssignedFieldId.Value), out var field) && field.Spec.Kind == WorldNodeKind.SpiritFieldPlot && field.AssignedBeastId == beast.Id, "Invalid field assignment.");
                        else
                            Require(!b.AssignedFieldId.HasValue, "Invalid inactive field assignment.");
                    }

                    world.Player.Beasts.Restore(beast);
                }

                foreach (var node in world.nodes.Values)
                    if (node.AssignedBeastId.HasValue)
                        Require(world.Player.Beasts.All.Any(b => b.Id == node.AssignedBeastId.Value && b.AssignedFieldId == node.Id && b.State == BeastState.AssignedToField), "Orphan field assignment.");
                Require(world.Player.Beasts.All.Count(b => b.State == BeastState.SummonedActive) <= Math.Floor(Math.Max(0, world.PlayerActor.Stats.Get(StatId.BeastCommandCap))), "Active beasts exceed cap.");
                Require(playerDto.Hp > 0 || world.Player.Beasts.All.All(b => b.State != BeastState.SummonedActive && b.State != BeastState.AssignedToField), "Dead owner retains active beasts.");
                foreach (var a in dto.Actors)
                {
                    var actor = world.actors[new EntityId(a.Id)];
                    var pool = new VitalResourcePool(actor.Stats, a.Hp, a.Qi, a.Shield);
                    Require(pool.CurrentHp == a.Hp && pool.CurrentQi == a.Qi && pool.CurrentShield == a.Shield, "Vitals exceed caps.");
                    Require((a.Hp <= 0) == (a.State == ActorActionState.Dead), "Death state mismatch.");
                    actor.Vitals = pool;
                    if (a.Faction == ActorFaction.PlayerBeast)
                        Require(world.Player.Beasts.All.Any(b => b.ActorId == actor.Id && b.State == BeastState.SummonedActive) && a.Hp > 0, "Orphan or dead active beast actor.");
                    foreach (var status in actor.StatusEffects.Active)
                    {
                        var ctx = new AbilityExecutionContext(world, actor, actor, actor.Transform.FacingDirection, status.Source);
                        Require(world.Combat.ValidateEffects(status.Spec.TickEffects, ctx, new HashSet<string>(), false).Succeeded, "Invalid periodic effect content.");
                    }

                    var contentContext = new AbilityExecutionContext(world, actor, null, actor.Transform.FacingDirection, EffectSourceSnapshot.Capture(actor));
                    foreach (var id in actor.Abilities.Cooldowns.Keys)
                    {
                        content.TryGetTechnique(id, out var definition);
                        Require(world.Combat.ValidateEffects(definition.Effects, contentContext, new HashSet<string>(), false).Succeeded, "Invalid learned effect content.");
                    }

                    if (a.Cast != null)
                    {
                        var c = a.Cast;
                        Require(actor.CanAct && actor.Abilities.Knows(c.TechniqueId), "Invalid active cast.");
                        Require(content.TryGetTechnique(c.TechniqueId, out var technique), "Missing cast technique.");
                        Require(c.Direction.ToPoint().Finite && c.Direction.ToPoint().LengthSquared > .000001f, "Invalid cast aim.");
                        Require(!c.TargetId.HasValue || (c.TargetId > 0 && c.TargetId < dto.NextEntityId), "Invalid cast target ID.");
                        Guard.Number(c.WindupRemaining, "windup", 0, technique.CastDuration);
                        Guard.Number(c.ChannelRemaining, "channel", 0, technique.ChannelDuration);
                        Guard.Number(c.UntilPulse, "pulse", 0, technique.ChannelInterval);
                        Require(c.Started ? (c.WindupRemaining <= .000001f && c.ChannelRemaining > 0 && c.UntilPulse > 0 && a.State == ActorActionState.Channeling) : (c.WindupRemaining > 0 && a.State == ActorActionState.Casting), "Inconsistent cast phase.");
                        actor.Abilities.Cast = new CastState(technique, new AimRequest(c.Direction.ToPoint(), c.TargetId.HasValue ? new EntityId(c.TargetId.Value) : (EntityId? )null))
                        {Started = c.Started, WindupRemaining = c.WindupRemaining, ChannelRemaining = c.ChannelRemaining, UntilPulse = c.UntilPulse};
                    }
                    else
                        Require(a.State != ActorActionState.Casting && a.State != ActorActionState.Channeling, "Missing cast state.");
                }

                foreach (var p in dto.Projectiles)
                {
                    Require(content.TryGetTechnique(p.TechniqueId, out var spec) && spec.Delivery.Shape == DeliveryShape.Projectile, "Invalid projectile technique.");
                    Guard.Number(p.RemainingLifetime, "projectile lifetime", .0000001f, spec.Delivery.Range / spec.Delivery.ProjectileSpeed);
                    Require(p.Position.ToPoint().Finite && p.Direction.ToPoint().Finite && Math.Abs(p.Direction.ToPoint().Length - 1) < .0001f && Math.Abs(p.Position.X) <= geometry.HalfSize.X && Math.Abs(p.Position.Y) <= geometry.HalfSize.Y, "Invalid projectile transform.");
                    Require(p.HitTargets.Count < spec.Delivery.MaxTargets && p.HitTargets.Distinct().Count() == p.HitTargets.Count && p.HitTargets.All(id => id > 0 && id < dto.NextEntityId), "Invalid projectile hit history.");
                    var projectile = new ProjectileState(new EntityId(p.Id), spec, RestoreSource(p.Source, dto.NextEntityId), p.Position.ToPoint(), p.Direction.ToPoint())
                    {RemainingLifetime = p.RemainingLifetime};
                    Require(world.Combat.ValidateEffects(spec.Effects, new AbilityExecutionContext(world, null, null, projectile.Direction, projectile.Source), new HashSet<string>(), false).Succeeded, "Invalid projectile effect content.");
                    foreach (var id in p.HitTargets)
                        projectile.hits.Add(new EntityId(id));
                    world.Combat.projectiles.Add(projectile);
                }

                // No injected state changes until every reference and invariant above has passed.
                random.Restore(new RandomState(dto.Random.AlgorithmId, dto.Random.State));
                return DomainResult<WorldState>.Ok(world);
            }
            catch (ArgumentException error)
            {
                return DomainResult<WorldState>.Fail(DomainError.InvalidSave, error.Message);
            }
            catch (InvalidOperationException error)
            {
                return DomainResult<WorldState>.Fail(DomainError.InvalidSave, error.Message);
            }
            catch (OverflowException error)
            {
                return DomainResult<WorldState>.Fail(DomainError.InvalidSave, error.Message);
            }
        }
    }
}

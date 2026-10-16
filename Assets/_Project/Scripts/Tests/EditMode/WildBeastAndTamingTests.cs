using System;
using System.Collections.Generic;
using System.Linq;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Combat.Effects;
using LinhThu.Domain.Core;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Persistence;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;
using NUnit.Framework;

namespace LinhThu.Tests.EditMode
{
    public sealed class WildBeastAndTamingTests
    {
        private static WorldState World(DeliveryShape shape = DeliveryShape.ConeSector, int capacity = 16, WorldGeometry geometry = null)
        {
            var content = Fixture.Content();
            content.Add(new TechniqueSpec("wild-hit", new DeliverySpec(shape, 2, .5f, 90, maxTargets: 4),
                new AbilityEffectSpec[] { new DamageEffectSpec(StatId.Attack, 1, 0, ElementType.Physical) }, castDuration: .65f, cooldown: 2));
            content.Add(new ActorSpec("wild", ElementType.Water, new Dictionary<StatId, float> {
                { StatId.MaxHp, 100 }, { StatId.Attack, 10 }, { StatId.MoveSpeed, 2 } }, new[] { "wild-hit" }));
            content.Add(new WildBeastSpec("site", "wild", "turtle", "wild-hit", new[] { new ItemStack("herb", 2) },
                experience: 25, aggroRadius: 5, leashRadius: 8, patrolRadius: 2, respawnSeconds: 3));
            return Fixture.World(geometry ?? new WorldGeometry(new WorldPoint(100, 100), Array.Empty<WorldObstacle>()), content, capacity, Array.Empty<NodeSpawn>());
        }
        private static WildBeastAiController Spawn(WorldState world, WorldPoint home)
        {
            var result = world.TryAddWildBeast(new WildBeastSeed("site", home));
            Assert.That(result.Succeeded, Is.True, result.Message); return result.Value;
        }
        private static void Place(WorldState world, ActorEntity actor, WorldPoint point)
            => world.Motor.Displace(actor.Transform, point - actor.Transform.Position);
        private static WorldState RoundTrip(WorldState world)
        {
            var mapper = new GameSaveMapper(); var serializer = new SaveSerializer();
            string json = serializer.Serialize(mapper.Capture(world));
            var data = serializer.TryDeserialize(json); Assert.That(data.Succeeded, Is.True, data.Message);
            var restored = mapper.TryRestore(data.Value, world.Content, world.Geometry, new SeededRandomSource(9), new DomainEventBus());
            Assert.That(restored.Succeeded, Is.True, restored.Message);
            Assert.That(serializer.Serialize(mapper.Capture(restored.Value)), Is.EqualTo(json)); return restored.Value;
        }

        [Test]
        public void PatrolChaseLockedWindupAttackAndLeashHeal()
        {
            var world = World(); var site = Spawn(world, new WorldPoint(18, 0)); var wild = world.Actors[site.ActorId.Value];
            Fixture.Tick(world, 1.1f); Assert.That(site.State, Is.EqualTo(WildBeastAiState.Patrol));
            Place(world, world.PlayerActor, wild.Transform.Position + new WorldPoint(4, 0));
            world.Tick(.1f); Assert.That(site.State, Is.EqualTo(WildBeastAiState.Chase));
            Place(world, world.PlayerActor, wild.Transform.Position + new WorldPoint(1, 0));
            world.Tick(.1f); Assert.That(site.State, Is.EqualTo(WildBeastAiState.WindupCast));
            var direction = wild.Abilities.Cast.Aim.Direction; float hp = world.PlayerActor.Vitals.CurrentHp;
            Fixture.Tick(world, .6f); world.Tick(.01f);
            Assert.That(site.State, Is.EqualTo(WildBeastAiState.ExecuteAttack));
            Assert.That(world.PlayerActor.Vitals.CurrentHp, Is.LessThan(hp));
            world.DealDamage(wild, world.LocalPlayerId, 40);
            Place(world, world.PlayerActor, new WorldPoint(40, 0));
            float wounded = wild.Vitals.CurrentHp; world.Tick(.1f);
            Assert.That(site.State, Is.EqualTo(WildBeastAiState.LeashReturn));
            Assert.That(wild.Abilities.Cast, Is.Null); Assert.That(wild.Vitals.CurrentHp, Is.GreaterThan(wounded));
            Fixture.Tick(world, 5); Assert.That(wild.Vitals.CurrentHp, Is.EqualTo(100));
            Assert.That((wild.Transform.Position - site.HomePosition).Length, Is.LessThanOrEqualTo(2.1f));
        }

        [TestCase(DeliveryShape.ConeSector)] [TestCase(DeliveryShape.LineRay)] [TestCase(DeliveryShape.CircleAoE)]
        public void DashEvadesLockedGroundAttack(DeliveryShape shape)
        {
            var world = World(shape); var site = Spawn(world, new WorldPoint(.75f, 0)); var wild = world.Actors[site.ActorId.Value];
            world.Tick(.01f); var aim = wild.Abilities.Cast.Aim; float hp = world.PlayerActor.Vitals.CurrentHp;
            Assert.That(world.Execute(new DashCommand(world.LocalPlayerId, new WorldPoint(0, 1))).Succeeded, Is.True);
            Fixture.Tick(world, .3f);
            Assert.That(wild.Abilities.Cast.Aim.Direction, Is.EqualTo(aim.Direction));
            Assert.That(wild.Abilities.Cast.Aim.LockedPoint, Is.EqualTo(aim.LockedPoint));
            world = RoundTrip(world); Fixture.Tick(world, .4f);
            Assert.That(world.PlayerActor.Vitals.CurrentHp, Is.EqualTo(hp), "Windup must not track the dodging player.");
            Assert.That(world.PlayerActor.Transform.Position.Y, Is.GreaterThan(1));
        }

        [Test]
        public void TamingThresholdOwnerRangeAndDuplicateAreAtomic()
        {
            var world = World(); var site = Spawn(world, new WorldPoint(1, 0)); var wild = world.Actors[site.ActorId.Value];
            var second = world.TryAddPlayer("player", "qi", new WorldPoint(1, 1)).Value;
            int events = 0; world.Events.Subscribe<BeastTamedEvent>(_ => events++);
            world.DealDamage(wild, world.LocalPlayerId, 69);
            var command = new TryTameBeastCommand(second.ActorId, wild.Id);
            Assert.That(world.ExecuteFor(world.LocalPlayerId, command).ErrorCode, Is.EqualTo(DomainError.WrongOwner));
            Assert.That(world.Execute(command).ErrorCode, Is.EqualTo(DomainError.InvalidState));
            world.DealDamage(wild, world.LocalPlayerId, 1);
            Assert.That(BeastTamingService.IsWeakened(wild), Is.True);
            Place(world, world.Actors[second.ActorId], new WorldPoint(20, 20));
            Assert.That(world.Execute(command).ErrorCode, Is.EqualTo(DomainError.OutOfRange));
            Place(world, world.Actors[second.ActorId], new WorldPoint(1, 1));
            Assert.That(world.ExecuteFor(second.ActorId, command).Succeeded, Is.True);
            Assert.That(world.Execute(command).Succeeded, Is.False);
            Assert.That(world.Execute(new TryTameBeastCommand(world.LocalPlayerId, wild.Id)).Succeeded, Is.False);
            world.Events.Flush(); Assert.That(events, Is.EqualTo(1));
            Assert.That(second.Beasts.All.Single().OwnerId, Is.EqualTo(second.ActorId));
            Assert.That(world.LocalPlayer.Beasts.All, Is.Empty); Assert.That(second.Cultivation.CurrentCultivationExp, Is.Zero);
            Assert.That(world.SpatialGrid.QueryActors(wild.Transform.Position, 2), Has.No.Member(wild));
            var loaded = RoundTrip(world); var beast = loaded.Players[second.ActorId].Beasts.All.Single();
            Assert.That(loaded.ExecuteFor(second.ActorId, new SummonCommand(second.ActorId, beast.Id)).Succeeded, Is.True);
            Assert.That(loaded.Execute(new RecallCommand(world.LocalPlayerId, beast.Id)).Succeeded, Is.False);
        }

        [Test]
        public void KillRewardsCorrectPlayerAndRespawnsOnlyOnceAfterTimer()
        {
            var world = World(); var second = world.TryAddPlayer("player", "qi", new WorldPoint(35, 0)).Value;
            var site = Spawn(world, new WorldPoint(35, 1)); var wild = world.Actors[site.ActorId.Value];
            world.DealDamage(wild, second.ActorId, 100);
            Assert.That(world.Execute(new TryTameBeastCommand(second.ActorId, wild.Id)).Succeeded, Is.False);
            Assert.That(second.Cultivation.CurrentCultivationExp, Is.EqualTo(25));
            Assert.That(second.Inventory.Count("herb"), Is.EqualTo(2)); Assert.That(world.Player.Cultivation.CurrentCultivationExp, Is.Zero);
            Fixture.Tick(world, 1); world = RoundTrip(world); site = world.WildBeasts[site.Id];
            Fixture.Tick(world, 1.9f); Assert.That(site.ActorId, Is.Null);
            Fixture.Tick(world, .2f); Assert.That(site.ActorId, Is.Not.Null); Assert.That(site.ActorId, Is.Not.EqualTo(wild.Id));
            Assert.That(world.Actors[site.ActorId.Value].Vitals.CurrentHp, Is.EqualTo(100));
            Assert.That(world.Players[second.ActorId].Inventory.Count("herb"), Is.EqualTo(2));
        }

        [Test]
        public void FullInventoryKeepsPendingLootThroughSaveAndDoesNotDuplicateExperience()
        {
            var world = World(capacity: 1); world.Player.Inventory.TryAddAll(new[] { new ItemStack("robe", 1) });
            var site = Spawn(world, new WorldPoint(17, 1)); var wild = world.Actors[site.ActorId.Value];
            world.DealDamage(wild, world.LocalPlayerId, 100); Fixture.Tick(world, 4);
            Assert.That(site.PendingLoot, Is.True); Assert.That(site.ActorId, Is.Null);
            world = RoundTrip(world); site = world.WildBeasts[site.Id];
            world.Player.Inventory.TryRemove("robe", 1); world.Tick(.1f);
            Assert.That(site.PendingLoot, Is.False); Assert.That(site.ActorId, Is.Not.Null);
            Assert.That(world.Player.Inventory.Count("herb"), Is.EqualTo(2));
            Fixture.Tick(world, 1); Assert.That(world.Player.Cultivation.CurrentCultivationExp, Is.EqualTo(25));
            Assert.That(world.Player.Inventory.Count("herb"), Is.EqualTo(2));
        }

        [Test]
        public void TamingCannotCrossObstacleAndInvalidSpawnSaveDoesNotChangeRandom()
        {
            var world = World(geometry: new WorldGeometry(new WorldPoint(100, 100), new[] {
                new WorldObstacle(new WorldPoint(.75f, 0), new WorldPoint(.1f, 1), true) }));
            var site = Spawn(world, new WorldPoint(1.5f, 0)); var wild = world.Actors[site.ActorId.Value];
            world.DealDamage(wild, world.LocalPlayerId, 70); world.Events.Flush();
            Assert.That(world.Execute(new TryTameBeastCommand(world.LocalPlayerId, wild.Id)).ErrorCode, Is.EqualTo(DomainError.Blocked));
            Assert.That(world.Player.Beasts.All, Is.Empty); Assert.That(wild.Vitals.CurrentHp, Is.EqualTo(30));
            var mapper = new GameSaveMapper(); var dto = mapper.Capture(world); dto.WildBeasts.Single().ActorId = world.LocalPlayerId.Value;
            var random = new SeededRandomSource(456); uint state = random.Capture().State;
            var restored = mapper.TryRestore(dto, world.Content, world.Geometry, random, new DomainEventBus());
            Assert.That(restored.Succeeded, Is.False); Assert.That(random.Capture().State, Is.EqualTo(state));
            Assert.That(world.Actors[wild.Id], Is.SameAs(wild));
        }

        [Test]
        public void PeriodicDamageCreditsCompanionOwnerAfterRecallAndSave()
        {
            var world = World(); var second = world.TryAddPlayer("player", "qi", new WorldPoint(35, 0)).Value;
            var beast = new BeastInstance(world.Ids.Allocate(), second.ActorId, "turtle", "common", "wild", .5f, BeastState.Resting);
            second.Beasts.Add(beast); world.Execute(new SummonCommand(second.ActorId, beast.Id));
            var companion = world.Actors[beast.ActorId.Value]; var site = Spawn(world, new WorldPoint(36, 0)); var wild = world.Actors[site.ActorId.Value];
            wild.Vitals.ApplyDamage(99); world.Content.TryGetStatus("burn", out var burn);
            Assert.That(wild.StatusEffects.Apply(burn, companion.Id, EffectSourceSnapshot.Capture(companion)).Succeeded, Is.True);
            Assert.That(world.Execute(new RecallCommand(second.ActorId, beast.Id)).Succeeded, Is.True);
            world = RoundTrip(world); Fixture.Tick(world, 1.1f);
            Assert.That(world.Players[second.ActorId].Cultivation.CurrentCultivationExp, Is.EqualTo(25));
            Assert.That(world.Players[second.ActorId].Inventory.Count("herb"), Is.EqualTo(2));
            Assert.That(world.Player.Cultivation.CurrentCultivationExp, Is.Zero);
        }
    }
}

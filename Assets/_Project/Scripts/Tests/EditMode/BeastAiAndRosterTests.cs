using System;
using System.Linq;
using NUnit.Framework;
using LinhThu.Domain.Core;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;

namespace LinhThu.Tests.EditMode
{
    public sealed class BeastAiAndRosterTests
    {
        [Test]
        public void SummonLinkCapRecallAndDeathAreConsistent()
        {
            var w = Fixture.World();
            var roster = w.Player.Beasts;
            var first = Fixture.Beast(w);
            var second = Fixture.Beast(w);
            float attack = w.PlayerActor.Stats.Get(StatId.Attack);
            Assert.That(roster.TrySummon(first.Id, w).Succeeded, Is.True);
            Assert.That(w.PlayerActor.Stats.Get(StatId.Attack), Is.EqualTo(attack + 2));
            Assert.That(roster.TrySummon(first.Id, w).Succeeded, Is.False);
            Assert.That(roster.TrySummon(second.Id, w).ErrorCode, Is.EqualTo(DomainError.CapacityExceeded));
            w.Tick(.1f);
            Assert.That(w.PlayerActor.Stats.Get(StatId.Attack), Is.EqualTo(attack + 2));
            var actor = first.ActorId.Value;
            w.DealDamage(w.Actors[actor], w.Player.ActorId, 999);
            w.Tick(.1f);
            Assert.That(first.State, Is.EqualTo(BeastState.Resting));
            Assert.That(w.Actors.ContainsKey(actor), Is.False);
            Assert.That(w.PlayerActor.Stats.Get(StatId.Attack), Is.EqualTo(attack));
            roster.TrySummon(first.Id, w);
            roster.TryRecall(first.Id, w);
            Assert.That(w.PlayerActor.Stats.Get(StatId.Attack), Is.EqualTo(attack));
        }

        [Test]
        public void FollowAndAssistUseMovementAndNormalCastCosts()
        {
            var w = Fixture.World();
            var b = Fixture.Beast(w);
            w.Player.Beasts.TrySummon(b.Id, w);
            var actor = w.Actors[b.ActorId.Value];
            w.Motor.Displace(w.PlayerActor.Transform, new WorldPoint(4, 0));
            float before = (actor.Transform.Position - w.PlayerActor.Transform.Position).Length;
            w.Tick(.1f);
            Assert.That((actor.Transform.Position - w.PlayerActor.Transform.Position).Length, Is.LessThan(before));
            var enemy = w.TrySpawnActor("target", ActorFaction.HostileMonster, w.PlayerActor.Transform.Position + new WorldPoint(1, 0)).Value;
            var neutral = w.TrySpawnActor("target", ActorFaction.Neutral, w.PlayerActor.Transform.Position).Value;
            float hp = enemy.Vitals.CurrentHp;
            Fixture.Tick(w, 1);
            Assert.That(enemy.Vitals.CurrentHp, Is.LessThan(hp));
            Assert.That(neutral.Vitals.CurrentHp, Is.EqualTo(200));
            Assert.That(actor.Vitals.CurrentQi, Is.LessThan(20));
        }

        [Test]
        public void OwnerEggFieldAndBlockedSpawnRejectWithoutOrphans()
        {
            var w = Fixture.World();
            var roster = w.Player.Beasts;
            var egg = Fixture.Beast(w, BeastState.InEgg);
            Assert.That(roster.Add(new BeastInstance(w.Ids.Allocate(), new EntityId(999), "turtle", "common", "default", 0)).ErrorCode, Is.EqualTo(DomainError.WrongOwner));
            Assert.That(roster.TrySummon(egg.Id, w).ErrorCode, Is.EqualTo(DomainError.InvalidState));
            Assert.That(roster.TryHatch(egg.Id, w).Succeeded, Is.True);
            var herb = w.Nodes.Values.Single(n => n.DefinitionId == "herb-node");
            var field = w.Nodes.Values.Single(n => n.DefinitionId == "field");
            Assert.That(roster.TryAssignToField(egg.Id, herb.Id, w).Succeeded, Is.False);
            Assert.That(roster.TryAssignToField(egg.Id, field.Id, w).Succeeded, Is.True);
            Assert.That(roster.TrySummon(egg.Id, w).Succeeded, Is.False);
            Assert.That(egg.ActorId, Is.Null);
            Assert.That(roster.TryUnassignFromField(egg.Id, w).Succeeded, Is.True);
            Assert.That(field.AssignedBeastId, Is.Null);
            var c = Fixture.Content();
            c.TryGetActor("turtle-actor", out var small);
            c.Add(new ActorSpec("large-turtle", small.PrimaryElement, small.BaseStats, small.AbilityIds, 2));
            c.Add(new BeastSpeciesSpec("large", "large-turtle", new BeastAiSpec(1, 8, 5, 2, new[]{"strike"}), new[]{StatId.Attack}));
            var tight = Fixture.World(new WorldGeometry(new WorldPoint(1, 1), Array.Empty<WorldObstacle>()), c);
            var beast = new BeastInstance(tight.Ids.Allocate(), tight.Player.ActorId, "large", "common", "default", 0, BeastState.Resting);
            tight.Player.Beasts.Add(beast);
            long next = tight.Ids.Next;
            int source = tight.Sources.Next;
            Assert.That(tight.Player.Beasts.TrySummon(beast.Id, tight).ErrorCode, Is.EqualTo(DomainError.Blocked));
            Assert.That(tight.Actors.Count, Is.EqualTo(1));
            Assert.That(tight.Ids.Next, Is.EqualTo(next));
            Assert.That(tight.Sources.Next, Is.EqualTo(source));
        }

        [Test]
        public void CapReductionRecallsNewestAndPreservesUnrelatedModifiers()
        {
            var w = Fixture.World();
            var actor = w.PlayerActor;
            actor.Stats.SetBase(StatId.BeastCommandCap, 2);
            int independent = w.Sources.Allocate();
            actor.Stats.AddModifier(new StatModifier(StatId.Attack, ModifierType.FlatAdd, 7, independent));
            var first = Fixture.Beast(w);
            var last = Fixture.Beast(w);
            w.Player.Beasts.TrySummon(first.Id, w);
            w.Player.Beasts.TrySummon(last.Id, w);
            Assert.That(actor.Stats.Get(StatId.Attack), Is.EqualTo(36));
            actor.Stats.SetBase(StatId.BeastCommandCap, 1);
            w.Tick(.1f);
            Assert.That(first.State, Is.EqualTo(BeastState.SummonedActive));
            Assert.That(last.State, Is.EqualTo(BeastState.Resting));
            Assert.That(actor.Stats.Get(StatId.Attack), Is.EqualTo(34));
            w.DealDamage(actor, actor.Id, 999);
            w.Tick(.1f);
            Assert.That(first.State, Is.EqualTo(BeastState.Resting));
            Assert.That(w.Actors.Count, Is.EqualTo(1));
            Assert.That(actor.Stats.GetModifiers().Any(m => m.SourceId == independent), Is.True);
        }

        [Test]
        public void LeashFindsSafePositionAndFollowStopsWithoutJitter()
        {
            var w = Fixture.World();
            var b = Fixture.Beast(w);
            w.Player.Beasts.TrySummon(b.Id, w);
            var actor = w.Actors[b.ActorId.Value];
            w.Motor.Displace(w.PlayerActor.Transform, new WorldPoint(12, 0));
            w.Tick(.1f);
            Assert.That(w.Geometry.CanStand(actor.Transform.Position, actor.Transform.CollisionRadius), Is.True);
            Assert.That((actor.Transform.Position - w.PlayerActor.Transform.Position).Length, Is.LessThanOrEqualTo(1.05f));
            var stopped = actor.Transform.Position;
            Fixture.Tick(w, 1);
            Assert.That(actor.Transform.Position, Is.EqualTo(stopped));
        }

        [Test]
        public void AssistPrioritizesOwnersAttackerOverNearestHostile()
        {
            var w = Fixture.World();
            var b = Fixture.Beast(w);
            w.Player.Beasts.TrySummon(b.Id, w);
            var nearest = w.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(0, 2)).Value;
            var attacker = w.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(3, 0)).Value;
            w.DealDamage(w.PlayerActor, attacker.Id, 1);
            w.Tick(.1f);
            Assert.That(attacker.Vitals.CurrentHp, Is.EqualTo(180));
            Assert.That(nearest.Vitals.CurrentHp, Is.EqualTo(200));
        }

        [Test]
        public void FactorySeedsRosterWithoutHardcodedSpecies()
        {
            var w = Fixture.World(beasts: new[]{new BeastSeed("turtle", "rare", "ancient", .75f)});
            var beast = w.Player.Beasts.All.Single();
            Assert.That(beast.Bond, Is.EqualTo(.75f));
            Assert.That(beast.State, Is.EqualTo(BeastState.Resting));
            Assert.That(w.Player.Beasts.TrySummon(beast.Id, w).Succeeded, Is.True);
        }

        [Test]
        public void FollowSlidesPastObstacleAndAssistDoesNotFireThroughWall()
        {
            var geometry = new WorldGeometry(new WorldPoint(20, 20), new[] { new WorldObstacle(new WorldPoint(.8f, 0), new WorldPoint(.2f, 1), true) });
            var world = Fixture.World(geometry);
            var beast = Fixture.Beast(world);
            world.Player.Beasts.TrySummon(beast.Id, world);
            var actor = world.Actors[beast.ActorId.Value];
            var enemy = world.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(2, 0)).Value;
            world.Tick(.1f);
            Assert.That(enemy.Vitals.CurrentHp, Is.EqualTo(200));
            Assert.That(actor.Vitals.CurrentQi, Is.EqualTo(20));
            world.RemoveActor(enemy.Id);
            world.Motor.Displace(world.PlayerActor.Transform, new WorldPoint(4, 3));
            for (int i = 0; i < 50; i++)
            {
                world.Tick(.1f);
                Assert.That(geometry.CanStand(actor.Transform.Position, actor.Transform.CollisionRadius), Is.True);
            }
            Assert.That((actor.Transform.Position - world.PlayerActor.Transform.Position).Length, Is.LessThanOrEqualTo(1.06f));
        }

        [Test]
        public void FailedLeashSearchFallsBackToBoundedMovement()
        {
            var content = Fixture.Content();
            content.TryGetActor("turtle-actor", out var normal);
            content.Add(new ActorSpec("large-turtle", normal.PrimaryElement, normal.BaseStats, normal.AbilityIds, 2));
            var geometry = new WorldGeometry(new WorldPoint(20, 20), new[]
            {
                new WorldObstacle(new WorldPoint(-1, 0), new WorldPoint(.25f, 10), true),
                new WorldObstacle(new WorldPoint(1, 0), new WorldPoint(.25f, 10), true)
            });
            var world = Fixture.World(geometry, content);
            var actor = world.TrySpawnActor("large-turtle", ActorFaction.PlayerBeast, new WorldPoint(-10, 0), world.Player.ActorId).Value;
            var ai = new BeastAiController(actor.Id, world.Player.ActorId, new BeastAiSpec(1, 8, 5, 2, new[] { "strike" }));
            Assert.That(geometry.TryFindStandableNear(new WorldPoint(-1, 0), 2, 1, out _), Is.False);
            ai.Tick(world, .1f);
            Assert.That(actor.Transform.Position.X, Is.EqualTo(-10)); // AI writes intent; WorldState owns motion.
            Assert.That(actor.DesiredMovement.X, Is.EqualTo(1));
            world.Tick(.1f);
            Assert.That(actor.Transform.Position.X, Is.GreaterThan(-10).And.LessThan(-9.2f));
            Assert.That(geometry.CanStand(actor.Transform.Position, 2), Is.True);
            Fixture.Tick(world, .2f);
            Assert.That(actor.Transform.CurrentVelocity.Length, Is.GreaterThan(actor.Stats.Get(StatId.MoveSpeed)), "Catch-up intent can exceed normal walking speed.");
        }
    }
}

using System;
using System.Linq;
using NUnit.Framework;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.World;
using LinhThu.Domain.Persistence;
using LinhThu.Domain.Inventory;
using LinhThu.Domain.Beasts;
using LinhThu.Domain.Stats;

namespace LinhThu.Tests.EditMode
{
    public sealed class SpatialChunkGridTests
    {
        [TestCase(-.01f, -1)] [TestCase(0, 0)] [TestCase(15.999f, 0)] [TestCase(16, 1)] [TestCase(-16, -1)]
        public void CoordinatesUseFloor(float x, int expected)
        {
            Assert.That(ChunkCoord.FromWorld(new WorldPoint(x, 0)).X, Is.EqualTo(expected));
            Assert.That(new ChunkCoord(0, 0).Neighbors(1).Distinct().Count(), Is.EqualTo(9));
        }

        [Test]
        public void QueriesMatchGlobalScanAcrossMovesRemovalAndWideFootprints()
        {
            var world = Fixture.World(new WorldGeometry(new WorldPoint(300, 300), Array.Empty<WorldObstacle>()));
            for (int i = 0; i < 120; i++)
                Assert.That(world.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(i % 12 * 21 - 115, i / 12 * 23 - 110)).Succeeded, Is.True);
            var random = new Random(19);
            for (int i = 0; i < 80; i++)
            {
                var center = new WorldPoint(random.Next(-150, 150), random.Next(-150, 150)); float radius = random.Next(1, 35);
                Func<ActorEntity, bool> exact = a => (a.Transform.Position - center).Length <= radius + a.Transform.CollisionRadius;
                Assert.That(world.SpatialGrid.QueryActors(center, radius).Where(exact).Select(a => a.Id),
                    Is.EquivalentTo(world.Actors.Values.Where(exact).Select(a => a.Id)));
            }
            world.Motor.Displace(world.PlayerActor.Transform, new WorldPoint(90, 70));
            Assert.That(world.SpatialGrid.QueryActors(new WorldPoint(90, 70), 1), Does.Contain(world.PlayerActor));
            Assert.That(world.SpatialGrid.QueryActors(default, 1), Has.No.Member(world.PlayerActor));
            var actor = world.TrySpawnActor("target", ActorFaction.HostileMonster, new WorldPoint(16, 0)).Value;
            Assert.That(world.SpatialGrid.QueryActors(new WorldPoint(15.9f, 0), 0), Does.Contain(actor));
            world.RemoveActor(actor.Id);
            Assert.That(world.SpatialGrid.QueryActors(new WorldPoint(16, 0), 1), Has.No.Member(actor));
        }

        [Test]
        public void ObstacleIndexMatchesBruteForceAtSeamsAndAfterNodeDepletion()
        {
            var obstacles = Enumerable.Range(0, 40).Select(i => new WorldObstacle(new WorldPoint(i * 16 - 300, i % 3 * 20), new WorldPoint(9, 2), true)).ToArray();
            var geometry = new WorldGeometry(new WorldPoint(400, 100), obstacles);
            var random = new Random(7);
            for (int i = 0; i < 200; i++)
            {
                var a = new WorldPoint(random.Next(-390, 390), random.Next(-90, 90));
                var b = a + new WorldPoint(random.Next(-30, 30), random.Next(-10, 10));
                Assert.That(geometry.CanStand(a, .5f), Is.EqualTo(!obstacles.Any(o => o.Overlaps(a, .5f))));
                Assert.That(geometry.ClearLine(a, b), Is.EqualTo(!obstacles.Any(o => o.Crosses(a, b))));
            }
            var point = new WorldPoint(16, -50);
            var world = Fixture.World(geometry, nodes: new[] { new NodeSpawn("egg-node", point) });
            geometry = world.Geometry;
            var id = world.Nodes.Keys.Single();
            geometry.RegisterNodeObstacles(id, new[] { new WorldObstacle(point, new WorldPoint(30, 1), true) });
            Assert.That(geometry.CanStand(new WorldPoint(-5, -50), .2f), Is.False);
            world.Motor.Displace(world.PlayerActor.Transform, new WorldPoint(16, -48.7f));
            // The node centre is behind its footprint, so test depletion via a save restored in isolation.
            var saved = new GameSaveMapper().Capture(world); saved.Nodes.Single().IsDepleted = true;
            var restored = new GameSaveMapper().TryRestore(saved, world.Content, geometry, new SeededRandomSource(1), new DomainEventBus());
            Assert.That(restored.Succeeded, Is.True, restored.Message); geometry = restored.Value.Geometry;
            Assert.That(geometry.CanStand(new WorldPoint(-5, -50), .2f), Is.True);
        }

        [Test]
        public void NodeQueriesCoverDistantShorelineAndMatchGlobalFocusableNodes()
        {
            var content = Fixture.Content();
            content.Add(new WorldNodeSpec("wide-pond", WorldNodeKind.FishingSpot, new[] { new ItemStack("herb", 1) },
                fishingShoreline: new[] { new WorldPoint(-20, -2), new WorldPoint(20, -2), new WorldPoint(20, 2), new WorldPoint(-20, 2) }));
            var nodes = Enumerable.Range(0, 40).Select(i => new NodeSpawn("herb-node", new WorldPoint(i % 8 * 17 - 60, i / 8 * 19 - 40)))
                .Concat(new[] { new NodeSpawn("wide-pond", new WorldPoint(32, 0)) });
            var world = Fixture.World(new WorldGeometry(new WorldPoint(150, 150), Array.Empty<WorldObstacle>()), content, nodes: nodes);
            var points = world.Nodes.Values.Select(n => n.Position + new WorldPoint(.5f, .5f)).Concat(new[] { new WorldPoint(11, 0), new WorldPoint(53, 0) });
            foreach (var position in points)
            {
                world.Motor.Displace(world.PlayerActor.Transform, position - world.PlayerActor.Transform.Position);
                Func<WorldNodeEntity, bool> focus = n => world.Interactions.CanFocus(world, world.LocalPlayerId, n.Id);
                Assert.That(world.SpatialGrid.QueryNodes(position, 0).Where(focus).Select(n => n.Id),
                    Is.EquivalentTo(world.Nodes.Values.Where(focus).Select(n => n.Id)), position.X + "," + position.Y);
            }
            Assert.That(world.SpatialGrid.QueryNodes(new WorldPoint(11, 0), 0).Any(n => n.DefinitionId == "wide-pond"), Is.True);
        }

        [Test]
        public void EquipmentCultivationLifeAndBeastBondBelongToEachCommandOwner()
        {
            var world = Fixture.World(); var a = world.PlayerActor;
            var second = world.TryAddPlayer("player", "qi", new WorldPoint(17, 2)).Value; var b = world.Actors[second.ActorId];
            float baseAttack = a.Stats.Get(StatId.Attack);
            second.Inventory.TryAddAll(new[] { new ItemStack("robe", 1) });
            Assert.That(world.ExecuteFor(second.ActorId, new EquipCommand(second.ActorId, "body", "robe")).Succeeded, Is.True);
            Assert.That(b.Stats.Get(StatId.Attack), Is.EqualTo(baseAttack + 3)); Assert.That(a.Stats.Get(StatId.Attack), Is.EqualTo(baseAttack));
            second.Cultivation.AddExperience(100);
            Assert.That(world.Execute(new BreakthroughCommand(second.ActorId)).Succeeded, Is.True);
            Assert.That(second.Cultivation.CurrentRealmId, Is.EqualTo("foundation")); Assert.That(world.Player.Cultivation.CurrentRealmId, Is.EqualTo("qi"));
            var beast = new BeastInstance(world.Ids.Allocate(), second.ActorId, "turtle", "common", "wild", .5f, BeastState.Resting);
            Assert.That(second.Beasts.Add(beast).Succeeded, Is.True);
            float beforeBond = b.Stats.Get(StatId.Attack);
            Assert.That(world.Execute(new SummonCommand(second.ActorId, beast.Id)).Succeeded, Is.True);
            Assert.That(b.Stats.Get(StatId.Attack), Is.GreaterThan(beforeBond)); Assert.That(a.Stats.Get(StatId.Attack), Is.EqualTo(baseAttack));
            Assert.That(world.Execute(new ToggleMeditateCommand(second.ActorId)).Succeeded, Is.True);
            Fixture.Tick(world, 1.1f); Assert.That(second.Cultivation.CurrentCultivationExp, Is.GreaterThan(0));
            Assert.That(world.Player.Cultivation.CurrentCultivationExp, Is.Zero);
            var mapper = new GameSaveMapper(); var restored = mapper.TryRestore(mapper.Capture(world), world.Content, world.Geometry, new SeededRandomSource(19), new DomainEventBus());
            Assert.That(restored.Succeeded, Is.True, restored.Message);
            Assert.That(restored.Value.Actors[second.ActorId].Stats.Get(StatId.Attack), Is.EqualTo(b.Stats.Get(StatId.Attack)));
            Assert.That(restored.Value.Execute(new RecallCommand(second.ActorId, beast.Id)).Succeeded, Is.True);
            Assert.That(restored.Value.Actors[second.ActorId].Stats.Get(StatId.Attack), Is.EqualTo(beforeBond));
        }

        [Test]
        public void PlayersRouteCommandsIndependentlyAndRoundTripAcrossChunks()
        {
            var world = Fixture.World(); var a = world.LocalPlayer;
            var b = world.TryAddPlayer("player", "qi", new WorldPoint(17, 2)).Value;
            Assert.That(world.ExecuteFor(a.ActorId, new MoveCommand(b.ActorId, new WorldPoint(1, 0))).ErrorCode, Is.EqualTo(DomainError.WrongOwner));
            Assert.That(world.ExecuteFor(b.ActorId, new MoveCommand(b.ActorId, new WorldPoint(1, 0))).Succeeded, Is.True);
            world.Tick(.1f);
            Assert.That(world.Actors[b.ActorId].Transform.Position.X, Is.GreaterThan(17));
            Assert.That(world.PlayerActor.Transform.Position.LengthSquared, Is.Zero);
            b.Inventory.TryAddAll(new[] { new ItemStack("herb", 3) }); b.Cultivation.AddExperience(30);
            var mapper = new GameSaveMapper(); var serializer = new SaveSerializer();
            var data = serializer.TryDeserialize(serializer.Serialize(mapper.Capture(world)));
            Assert.That(data.Succeeded, Is.True);
            var restored = mapper.TryRestore(data.Value, world.Content, world.Geometry, new SeededRandomSource(2), new DomainEventBus());
            Assert.That(restored.Succeeded, Is.True, restored.Message);
            Assert.That(restored.Value.Players.Count, Is.EqualTo(2));
            Assert.That(restored.Value.LocalPlayer.ActorId, Is.EqualTo(a.ActorId));
            Assert.That(restored.Value.Players[b.ActorId].Inventory.Count("herb"), Is.EqualTo(3));
            Assert.That(restored.Value.Players[b.ActorId].Cultivation.CurrentCultivationExp, Is.EqualTo(30));
            Assert.That(restored.Value.Player.Inventory.Count("herb"), Is.Zero);
            Assert.That(restored.Value.SpatialGrid.QueryActors(new WorldPoint(17, 2), 1).Select(x => x.Id), Does.Contain(b.ActorId));
            Assert.That(serializer.Serialize(mapper.Capture(restored.Value)), Is.EqualTo(serializer.Serialize(mapper.Capture(world))));
        }
    }
}

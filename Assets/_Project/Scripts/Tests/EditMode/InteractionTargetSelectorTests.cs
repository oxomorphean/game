using System;
using System.Linq;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Stats;
using LinhThu.Domain.World;
using NUnit.Framework;

namespace LinhThu.Tests.EditMode
{
    public sealed class InteractionTargetSelectorTests
    {
        [Test]
        public void ThreeNearbyNodesHaveExactlyOneNearestFocus()
        {
            var world = Fixture.World(nodes: new[] {
                new NodeSpawn("herb-node", new WorldPoint(1.1f, 0)),
                new NodeSpawn("egg-node", new WorldPoint(.45f, 0)),
                new NodeSpawn("ore-node", new WorldPoint(.8f, 0)) });
            Assert.That(world.FocusedNode.DefinitionId, Is.EqualTo("egg-node"));
            Assert.That(world.FocusedNodeId, Is.EqualTo(world.Nodes.Values.Single(n => n.DefinitionId == "egg-node").Id));
        }

        [Test]
        public void TinyMovementSwitchesNearestNodeEvenAgainstFacingPreference()
        {
            var world = Fixture.World(nodes: new[] {
                new NodeSpawn("herb-node", new WorldPoint(-.5f, 0)),
                new NodeSpawn("ore-node", new WorldPoint(.5f, 0)) });
            world.Motor.Move(world.PlayerActor.Transform, new WorldPoint(-1, 0), 0, .01f, MovementMode.Walk);
            Assert.That(world.FocusedNode.DefinitionId, Is.EqualTo("herb-node"), "Facing breaks equal distance ties.");
            world.Motor.Displace(world.PlayerActor.Transform, new WorldPoint(.002f, 0));
            Assert.That(world.FocusedNode.DefinitionId, Is.EqualTo("ore-node"));
            world.Motor.Displace(world.PlayerActor.Transform, new WorldPoint(-.004f, 0));
            Assert.That(world.FocusedNode.DefinitionId, Is.EqualTo("herb-node"));
        }

        [Test]
        public void HarvestImmediatelyAdvancesFocusWithoutMovingOrTicking()
        {
            var world = Fixture.World(nodes: new[] {
                new NodeSpawn("egg-node", new WorldPoint(.3f, 0)),
                new NodeSpawn("herb-node", new WorldPoint(.6f, 0)),
                new NodeSpawn("ore-node", new WorldPoint(.9f, 0)) });
            var position = world.PlayerActor.Transform.Position;
            foreach (var id in new[] { "egg-node", "herb-node", "ore-node" })
            {
                Assert.That(world.FocusedNode.DefinitionId, Is.EqualTo(id));
                Assert.That(world.Execute(new InteractCommand(world.Player.ActorId)).Succeeded, Is.True);
                Assert.That(world.PlayerActor.Transform.Position, Is.EqualTo(position));
            }
            Assert.That(world.FocusedNodeId, Is.Null);
            Assert.That(world.Execute(new InteractCommand(world.Player.ActorId)).ErrorCode, Is.EqualTo(DomainError.NotFound));
            Assert.That(world.Player.Inventory.Count("egg"), Is.EqualTo(1));
            Assert.That(world.Player.Inventory.Count("herb"), Is.EqualTo(2));
            Assert.That(world.Player.Inventory.Count("ore"), Is.EqualTo(1));
            Fixture.Tick(world, 1);
            Assert.That(world.FocusedNode.DefinitionId, Is.EqualTo("herb-node"), "Regrown nodes become eligible without movement.");
        }

        [Test]
        public void OutOfRangeGrowingAndOccludedNodesAreExcluded()
        {
            var geometry = new WorldGeometry(new WorldPoint(20, 20), new[] {
                new WorldObstacle(new WorldPoint(.4f, 0), new WorldPoint(.02f, .3f), true) });
            var world = Fixture.World(geometry, nodes: new[] {
                new NodeSpawn("herb-node", new WorldPoint(.8f, 0)),
                new NodeSpawn("egg-node", new WorldPoint(0, .2f), false),
                new NodeSpawn("ore-node", new WorldPoint(0, 3)) });
            Assert.That(world.FocusedNodeId, Is.Null);
            world.PlayerActor.Stats.SetBase(StatId.PickupRadius, 1.1f);
            Assert.That(world.FocusedNode.DefinitionId, Is.EqualTo("ore-node"));
            world.DealDamage(world.PlayerActor, world.PlayerActor.Id, 1000);
            Assert.That(world.FocusedNodeId, Is.Null);
        }

        [Test]
        public void PortalAndEmptyFieldKeepTheirOwnInteractionActions()
        {
            var world = Fixture.World(nodes: new[] { new NodeSpawn("portal", default) });
            Assert.That(world.FocusedNode.DefinitionId, Is.EqualTo("portal"));
            Assert.That(world.Execute(new InteractCommand(world.Player.ActorId)).Succeeded, Is.True);
            Assert.That(world.PlayerActor.Transform.Position, Is.EqualTo(new WorldPoint(5, 0)));
            Assert.That(world.FocusedNodeId, Is.Null);

            world = Fixture.World(nodes: new[] { new NodeSpawn("field", default) });
            world.Player.Inventory.TryAdd(new LinhThu.Domain.Inventory.ItemStack("seed", 1));
            Assert.That(world.FocusedNode.DefinitionId, Is.EqualTo("field"));
            Assert.That(world.Execute(new InteractCommand(world.Player.ActorId, "herb-node")).Succeeded, Is.True);
            Assert.That(world.FocusedNodeId, Is.Null, "Growing plots have no action.");
            Fixture.Tick(world, 1);
            Assert.That(world.FocusedNode.IsReady, Is.True);
        }

        [Test]
        public void FailedHarvestPreservesFocusAndWrongOwnerCannotInteract()
        {
            var world = Fixture.World(capacity: 1, nodes: new[] { new NodeSpawn("herb-node", default) });
            world.Player.Inventory.TryAdd(new LinhThu.Domain.Inventory.ItemStack("ore", 1));
            var id = world.FocusedNodeId;
            Assert.That(world.Execute(new InteractCommand(new EntityId(999))).ErrorCode, Is.EqualTo(DomainError.WrongOwner));
            Assert.That(world.Execute(new InteractCommand(world.Player.ActorId)).ErrorCode, Is.EqualTo(DomainError.CapacityExceeded));
            Assert.That(world.FocusedNodeId, Is.EqualTo(id));
            Assert.That(world.FocusedNode.IsReady, Is.True);
            Assert.That(world.Player.Inventory.Count("herb"), Is.Zero);
        }

        [Test]
        public void EqualDistanceAndFacingUseStableNodeIdentity()
        {
            var world = Fixture.World(nodes: new[] {
                new NodeSpawn("herb-node", default), new NodeSpawn("ore-node", default) });
            Assert.That(world.FocusedNodeId, Is.EqualTo(world.Nodes.Keys.Min()));
            Assert.That(Fixture.World(nodes: Array.Empty<NodeSpawn>()).FocusedNodeId, Is.Null);
        }
    }
}

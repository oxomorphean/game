using System;
using NUnit.Framework;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Combat;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Persistence;
using LinhThu.Domain.World;

namespace LinhThu.Tests.EditMode
{
    public sealed class GameFeelTests
    {
        static WorldPoint P(float x, float y = 0) => new WorldPoint(x, y);

        [Test]
        public void StartingMovementAcceleratesAndReleaseHasBoundedCoast()
        {
            var world = Fixture.World();
            var actor = world.PlayerActor;
            world.Execute(new MoveCommand(actor.Id, P(1)));
            world.Tick(.02f);
            Assert.That(actor.Transform.CurrentVelocity.Length, Is.InRange(.01f, 3.99f));
            float firstSpeed = actor.Transform.CurrentVelocity.Length;
            world.Tick(.02f);
            Assert.That(actor.Transform.CurrentVelocity.Length, Is.GreaterThan(firstSpeed));
            Fixture.Tick(world, .3f);
            Assert.That(actor.Transform.CurrentVelocity.Length, Is.EqualTo(4).Within(.001f));
            world.Execute(new MoveCommand(actor.Id, default));
            var releasedAt = actor.Transform.Position;
            world.Tick(.02f);
            Assert.That(actor.State, Is.EqualTo(ActorActionState.Moving));
            Assert.That(actor.Transform.CurrentVelocity.Length, Is.InRange(.01f, 3.99f));
            Fixture.Tick(world, .5f);
            Assert.That(actor.Transform.CurrentVelocity.Length, Is.Zero);
            Assert.That(actor.State, Is.EqualTo(ActorActionState.Idle));
            Assert.That((actor.Transform.Position - releasedAt).Length, Is.InRange(.01f, .8f));
        }

        [Test]
        public void TurnResponsivenessControlsReversalWithoutExceedingSpeed()
        {
            var geometry = new WorldGeometry(P(20, 20), Array.Empty<WorldObstacle>());
            var motor = new MovementMotor(geometry);
            var slow = new Transform2D(default, P(1), .2f);
            var fast = new Transform2D(default, P(1), .2f);
            for (int i = 0; i < 10; i++)
            {
                motor.Move(slow, P(1), 4, .05f, MovementMode.Walk);
                motor.Move(fast, P(1), 4, .05f, MovementMode.Walk);
            }
            motor.Move(slow, P(-1), 4, .05f, MovementMode.Walk, new MovementSpec(turnResponsiveness: 0));
            motor.Move(fast, P(-1), 4, .05f, MovementMode.Walk, new MovementSpec(turnResponsiveness: 30));
            Assert.That(fast.CurrentVelocity.X, Is.LessThan(slow.CurrentVelocity.X));
            Assert.That(fast.CurrentVelocity.Length, Is.LessThanOrEqualTo(4.0001f));
        }

        [Test]
        public void DashLocksDirectionEasesOutAndHasIndependentCooldown()
        {
            var world = Fixture.World();
            var actor = world.PlayerActor;
            Assert.That(world.Execute(new DashCommand(actor.Id, P(1))).Succeeded, Is.True);
            Assert.That(actor.State, Is.EqualTo(ActorActionState.Dashing));
            Assert.That(actor.Abilities.GetCooldownRemaining("strike"), Is.Zero);
            world.Tick(.02f);
            float peak = actor.Transform.CurrentVelocity.Length;
            Assert.That(peak, Is.GreaterThan(4));
            world.Execute(new MoveCommand(actor.Id, P(0, 1)));
            world.Tick(.08f);
            Assert.That(actor.Transform.Position.Y, Is.Zero);
            Assert.That(actor.Transform.CurrentVelocity.Length, Is.LessThan(peak));
            Assert.That(world.Execute(new CastCommand(actor.Id, "strike", new AimRequest(P(1)))).ErrorCode, Is.EqualTo(DomainError.InvalidState));
            world.Tick(.1f);
            Assert.That(actor.Dash.Active, Is.False);
            Assert.That(actor.State, Is.Not.EqualTo(ActorActionState.Dashing));
            Assert.That(world.Execute(new DashCommand(actor.Id, P(1))).ErrorCode, Is.EqualTo(DomainError.OnCooldown));
            Assert.That(world.Execute(new CastCommand(actor.Id, "strike", new AimRequest(P(1)))).Succeeded, Is.True);
            Fixture.Tick(world, 1);
            Assert.That(world.Execute(new DashCommand(actor.Id, P(1))).Succeeded, Is.True);
        }

        [Test]
        public void DashCannotTunnelThroughThinWallAndUsesResolvedVelocity()
        {
            var geometry = new WorldGeometry(P(20, 20), new[] { new WorldObstacle(P(.7f), P(.01f, 10), true) });
            var world = Fixture.World(geometry);
            var actor = world.PlayerActor;
            world.Execute(new DashCommand(actor.Id, P(1)));
            for (int i = 0; i < 10; i++)
            {
                world.Tick(.02f);
                Assert.That(geometry.CanStand(actor.Transform.Position, actor.Transform.CollisionRadius), Is.True);
                Assert.That(actor.Transform.Position.X, Is.LessThan(.5f));
            }
            Assert.That(actor.Transform.CurrentVelocity.Length, Is.LessThan(.1f));
        }

        [Test]
        public void InvalidDashAndStaggerDoNotLeaveMomentumOrResetCooldown()
        {
            var world = Fixture.World();
            var actor = world.PlayerActor;
            Assert.That(world.Execute(new DashCommand(actor.Id, P(float.NaN))).ErrorCode, Is.EqualTo(DomainError.InvalidInput));
            Assert.That(actor.Dash.CooldownRemaining, Is.Zero);
            world.Execute(new DashCommand(actor.Id, P(1)));
            world.Tick(.02f);
            actor.SetStaggered(true);
            var stoppedAt = actor.Transform.Position;
            world.Tick(.1f);
            Assert.That(actor.Dash.Active, Is.False);
            Assert.That(actor.Transform.CurrentVelocity.Length, Is.Zero);
            Assert.That(actor.Transform.Position, Is.EqualTo(stoppedAt));
            Assert.That(actor.Dash.CooldownRemaining, Is.GreaterThan(0));
        }

        [Test]
        public void SaveMidDashRestoresVelocityDirectionTimeAndCooldown()
        {
            var world = Fixture.World();
            world.Execute(new DashCommand(world.Player.ActorId, P(1, 1)));
            world.Tick(.04f);
            var mapper = new GameSaveMapper();
            var serializer = new SaveSerializer();
            var dto = serializer.TryDeserialize(serializer.Serialize(mapper.Capture(world)));
            Assert.That(dto.Succeeded, Is.True, dto.Message);
            var load = mapper.TryRestore(dto.Value, world.Content, world.Geometry, new SeededRandomSource(2), new DomainEventBus());
            Assert.That(load.Succeeded, Is.True, load.Message);
            Assert.That(load.Value.PlayerActor.Transform.CurrentVelocity, Is.EqualTo(world.PlayerActor.Transform.CurrentVelocity));
            Fixture.Tick(world, .4f);
            Fixture.Tick(load.Value, .4f);
            Assert.That(load.Value.PlayerActor.Transform.Position, Is.EqualTo(world.PlayerActor.Transform.Position));
            Assert.That(load.Value.PlayerActor.Dash.CooldownRemaining, Is.EqualTo(world.PlayerActor.Dash.CooldownRemaining));
        }
    }
}

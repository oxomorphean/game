using System;
using NUnit.Framework;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Navigation;
using LinhThu.Domain.Core;

namespace LinhThu.Tests.EditMode
{
    public sealed class MovementAndCollisionTests
    {
        static WorldPoint P(float x, float y = 0) => new WorldPoint(x, y);
        [TestCase(false)]
        [TestCase(true)]
        public void HoldingIntoAPropKeepsSlideSpeedAndClearsItAtDifferentTickRates(bool box)
        {
            float fastest = float.MaxValue, slowest = 0;
            foreach (float delta in new[] { 1f / 30, 1f / 60, 1f / 120 })
            {
                var obstacle = new WorldObstacle(P(0), P(.6f, .6f), box);
                var geometry = new WorldGeometry(P(20, 20), new[] { obstacle });
                var actor = new Transform2D(P(-1, .1f), P(1), .1375f);
                var motor = new MovementMotor(geometry);
                float elapsed = 0, slowestSlide = float.MaxValue;
                while (elapsed < 2 && actor.Position.X < .8f)
                {
                    var from = actor.Position;
                    motor.Move(actor, P(1), 2.8f, delta, MovementMode.Walk);
                    elapsed += delta;
                    Assert.That(geometry.CanStand(actor.Position, actor.CollisionRadius), Is.True);
                    Assert.That(obstacle.Crosses(from, actor.Position), Is.False);
                    Assert.That((actor.Position - from).Length, Is.LessThanOrEqualTo(2.8f * delta + .0001f));
                    if (elapsed > .3f) slowestSlide = Math.Min(slowestSlide, actor.CurrentVelocity.Length);
                }
                Assert.That(actor.Position.X, Is.GreaterThanOrEqualTo(.8f), "Holding one direction must get around the prop.");
                Assert.That(slowestSlide, Is.GreaterThan(1.8f), "Sliding must not collapse to a crawl.");
                fastest = Math.Min(fastest, elapsed); slowest = Math.Max(slowest, elapsed);
            }
            Assert.That(slowest - fastest, Is.LessThan(.15f), "Steering must not get slower as the simulation tick gets smaller.");
        }

        [Test]
        public void ExpandedOffsetWorldClampsMovementAndRecoversAgainstAllFourBounds()
        {
            var bounds = new WorldBounds(P(20, -3), P(30, 6));
            var geometry = new WorldGeometry(bounds, Array.Empty<WorldObstacle>());
            Assert.That(geometry.CanStand(P(45, -3), .2f), Is.True);
            Assert.That(geometry.CanStand(P(-11, -3), .2f), Is.False);
            foreach (var point in new[] { P(-20, -3), P(70, -3), P(20, -20), P(20, 15) })
            {
                var resolved = geometry.ResolvePenetration(point, .2f);
                Assert.That(bounds.Contains(resolved, .2f), Is.True);
                Assert.That(geometry.ResolvePenetration(resolved, .2f), Is.EqualTo(resolved));
            }
            Assert.That(geometry.Move(P(20, -3), P(100), .2f).X, Is.EqualTo(49.8f).Within(.1f));
            Assert.Throws<ArgumentException>(() => new WorldBounds(P(float.NaN), P(30, 6)));
            Assert.Throws<ArgumentException>(() => new WorldBounds(P(0), P(0, 6)));
        }
        [Test]
        public void DiagonalInputIsNormalizedAndZeroKeepsFacing()
        {
            var motor = new MovementMotor(new WorldGeometry(P(10, 10), Array.Empty<WorldObstacle>()));
            var a = new Transform2D(P(0), P(0, 1), .2f);
            var b = new Transform2D(P(0), P(0, 1), .2f);
            motor.Move(a, P(1), 3, .1f, MovementMode.Walk);
            motor.Move(b, P(1, 1), 3, .1f, MovementMode.Walk);
            Assert.That(a.Position.Length, Is.EqualTo(b.Position.Length).Within(.0001));
            var facing = b.FacingDirection;
            motor.Move(b, P(0), 3, .1f, MovementMode.Walk);
            Assert.That(b.FacingDirection, Is.EqualTo(facing));
            Assert.Throws<ArgumentException>(() => motor.Move(b, P(float.NaN), 3, .1f, MovementMode.Walk));
            Assert.Throws<ArgumentException>(() => motor.Move(b, P(1), 3, .2f, MovementMode.Walk));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void WalkSlidesAlongCircleBoxAndConvex(int shape)
        {
            var obstacle = shape == 2 ? new WorldObstacle(new[]{P(-.5f, -1), P(.5f, -1), P(.5f, 1), P(-.5f, 1)}) : new WorldObstacle(P(0), P(.5f, 1), shape == 1);
            var geo = new WorldGeometry(P(5, 5), new[]{obstacle});
            var actor = new Transform2D(P(-2, .1f), P(1), .2f);
            var motor = new MovementMotor(geo);
            for (int i = 0; i < 50; i++)
            {
                motor.Move(actor, P(1, .4f), 2, .1f, MovementMode.Walk);
                Assert.That(geo.CanStand(actor.Position, .2f), Is.True);
            }

            Assert.That(actor.Position.Y, Is.GreaterThan(.5f));
            Assert.That(actor.Position.X, Is.GreaterThan(.5f));
        }

        [Test]
        public void DisplacementCannotTunnelAndSafeSpawnMayFail()
        {
            var geo = new WorldGeometry(P(5, 5), new[]{new WorldObstacle(P(0), P(.01f, 4), true)});
            var t = new Transform2D(P(-2), P(1), .2f);
            new MovementMotor(geo).Displace(t, P(4));
            Assert.That(t.Position.X, Is.LessThan(-.2f));
            Assert.That(geo.ClearLine(P(-2), P(2)), Is.False);
            Assert.That(geo.TryFindStandableNear(P(0), .2f, .1f, out _), Is.False);
            Assert.Throws<ArgumentException>(() => geo.Move(P(-2), P(1), 0));
            Assert.Throws<ArgumentException>(() => new Transform2D(P(0), P(1), 0));
            Assert.That(geo.CanStand(geo.Move(P(0), P(1), .2f), .2f), Is.True);
        }

        [Test]
        public void MapEdgeNarrowGapAndHeadOnSlideRemainDeterministic()
        {
            var obstacles = new[]{new WorldObstacle(P(1, 1), P(.5f, .85f), true), new WorldObstacle(P(1, -1), P(.5f, .85f), true)};
            var geo = new WorldGeometry(P(3, 3), obstacles);
            var motor = new MovementMotor(geo);
            var a = new Transform2D(P(0), P(1), .2f);
            var b = new Transform2D(P(0), P(1), .2f);
            for (int i = 0; i < 20; i++)
            {
                motor.Move(a, P(1), 2, .1f, MovementMode.Walk);
                motor.Move(b, P(1), 2, .1f, MovementMode.Walk);
                Assert.That(geo.CanStand(a.Position, .2f), Is.True);
                Assert.That(a.Position, Is.EqualTo(b.Position));
            }

            // Displacement must not slip through the 0.3-wide gap with a 0.4 diameter.
            var dash = new Transform2D(P(0), P(1), .2f);
            motor.Displace(dash, P(2));
            Assert.That(dash.Position.X, Is.LessThan(.5f));
            motor.Displace(a, P(0, 20));
            Assert.That(a.Position.Y, Is.LessThanOrEqualTo(2.8001f));
            var position = a.Position;
            motor.Move(a, P(1), 2, 0, MovementMode.Walk);
            Assert.That(a.Position, Is.EqualTo(position));
            Assert.That(Fixture.World(geo).TrySpawnActor("target", ActorFaction.Neutral, P(1, 1)).ErrorCode, Is.EqualTo(DomainError.Blocked));
            Assert.Throws<ArgumentException>(() => motor.Displace(a, P(float.PositiveInfinity)));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EmbeddedOriginsRecoverAtCenterEdgesAndCornersWithoutVelocitySpikes(int shape)
        {
            var obstacle = shape == 2 ? new WorldObstacle(new[] { P(-.5f, -.5f), P(.5f, -.5f), P(.5f, .5f), P(-.5f, .5f) })
                : new WorldObstacle(P(0), P(.5f, .5f), shape == 1);
            var geometry = new WorldGeometry(P(5, 5), new[] { obstacle });
            var motor = new MovementMotor(geometry);
            foreach (var start in new[] { P(0), P(.5f, 0), P(.49f, .49f), P(-.59f, 0), P(0, -.5f) })
            {
                var actor = new Transform2D(start, P(1), .2f);
                motor.Move(actor, default, 3, .016f, MovementMode.Walk);
                Assert.That(geometry.CanStand(actor.Position, .2f), Is.True, $"shape {shape} origin {start.X},{start.Y}");
                Assert.That(actor.CurrentVelocity.Length, Is.LessThan(.00001f), "Push-out is not locomotion velocity.");
                Assert.That(actor.Position, Is.EqualTo(geometry.ResolvePenetration(start, .2f)), "Deterministic recovery.");
                var from = actor.Position;
                motor.Move(actor, (from - obstacle.Center).Normalized, 3, .1f, MovementMode.Walk);
                Assert.That((actor.Position - obstacle.Center).Length, Is.GreaterThan((from - obstacle.Center).Length));
            }
        }

        [Test]
        public void OverlappingFootprintsAndArenaEdgeCannotTrapOrOscillateAnActor()
        {
            var geometry = new WorldGeometry(P(3, 3), new[] {
                new WorldObstacle(P(-.3f), P(.5f, .6f), true),
                new WorldObstacle(P(.3f), P(.5f, .6f), true),
                new WorldObstacle(P(2.7f, 2.7f), P(.5f, .5f), false) });
            foreach (var start in new[] { P(0), P(2.8f, 2.8f), P(3.1f, -2.9f) })
            {
                var free = geometry.ResolvePenetration(start, .22f);
                Assert.That(geometry.CanStand(free, .22f), Is.True);
                for (int i = 0; i < 60; i++) Assert.That(geometry.ResolvePenetration(free, .22f), Is.EqualTo(free));
            }
        }

        [Test]
        public void IdleAndMeditatingWorldActorsArePushedOutBeforeMovementIsLocked()
        {
            var geometry = new WorldGeometry(P(5, 5), new[] { new WorldObstacle(P(2), P(.5f, .5f), false) });
            var world = Fixture.World(geometry);
            foreach (bool meditate in new[] { false, true })
            {
                if (meditate) Assert.That(world.Execute(new LinhThu.Domain.World.ToggleMeditateCommand(world.Player.ActorId)).Succeeded, Is.True);
                typeof(Transform2D).GetProperty(nameof(Transform2D.Position)).SetValue(world.PlayerActor.Transform, P(2));
                world.Tick(.016f);
                Assert.That(geometry.CanStand(world.PlayerActor.Transform.Position, world.PlayerActor.Transform.CollisionRadius), Is.True);
                Assert.That(world.PlayerActor.Transform.CurrentVelocity.Length, Is.Zero);
            }
        }
    }
}

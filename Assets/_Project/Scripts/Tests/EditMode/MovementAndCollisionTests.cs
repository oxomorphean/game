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
            Assert.Throws<ArgumentException>(() => geo.Move(P(0), P(1), .2f));
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
    }
}

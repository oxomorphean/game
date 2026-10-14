using System;
using LinhThu.Domain.Actors;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Navigation
{
    public enum MovementMode
    {
        Walk,
        Displacement
    }

    public readonly struct MovementResult
    {
        public WorldPoint Position { get; }

        public WorldPoint Displacement { get; }

        internal MovementResult(WorldPoint position, WorldPoint displacement)
        {
            Position = position;
            Displacement = displacement;
        }
    }

    public sealed class MovementMotor
    {
        private readonly WorldGeometry geometry;
        public MovementMotor(WorldGeometry geometry)
        {
            this.geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        }

        public MovementResult Move(Transform2D transform, WorldPoint direction, float speed, float deltaTime, MovementMode mode, MovementSpec tuning = null)
        {
            if (transform == null || !direction.Finite)
                throw new ArgumentException("Invalid movement input.");
            Guard.Number(speed, nameof(speed), 0, 100);
            Guard.Delta(deltaTime);
            Guard.EnumValue(mode);
            if (deltaTime == 0)
                return new MovementResult(transform.Position, default);
            if (direction.LengthSquared > 1)
                direction = direction.Normalized;
            tuning = tuning ?? MovementSpec.Default;
            var target = direction * speed;
            var velocity = transform.CurrentVelocity;
            WorldPoint next, displacement;
            if (mode == MovementMode.Displacement)
            {
                next = target;
                displacement = next * deltaTime;
            }
            else
            {
                float rate = tuning.Deceleration;
                if (target.LengthSquared > .000001f)
                {
                    float turn = velocity.LengthSquared > .000001f ? (1 - WorldPoint.Dot(velocity.Normalized, target.Normalized)) * .5f : 0;
                    rate = tuning.Acceleration + tuning.TurnResponsiveness * velocity.Length * turn;
                }
                var difference = target - velocity;
                float rampTime = Math.Min(deltaTime, difference.Length / rate);
                next = difference.Length <= rate * deltaTime ? target : velocity + difference.Normalized * (rate * deltaTime);
                displacement = (velocity + next) * (.5f * rampTime) + next * (deltaTime - rampTime);
            }
            var result = ApplyVelocity(transform, displacement, next, deltaTime, mode == MovementMode.Walk);
            if (direction.LengthSquared > .000001f)
                transform.FacingDirection = direction.Normalized;
            return result;
        }

        public MovementResult Displace(Transform2D transform, WorldPoint delta)
        {
            var result = Apply(transform, delta, false);
            transform.CurrentVelocity = default;
            return result;
        }

        internal MovementResult Dash(Transform2D transform, WorldPoint direction, float distance, float endSpeed, float delta)
            => ApplyVelocity(transform, direction * distance, direction * endSpeed, delta, true);

        public void RecoverOverlap(Transform2D transform)
        {
            if (transform == null) throw new ArgumentNullException(nameof(transform));
            transform.Position = geometry.ResolvePenetration(transform.Position, transform.CollisionRadius);
        }

        private MovementResult ApplyVelocity(Transform2D transform, WorldPoint delta, WorldPoint velocity, float seconds, bool slide)
        {
            var result = Apply(transform, delta, slide);
            // Collision feedback must describe the actual direction/speed, never the blocked intent.
            transform.CurrentVelocity = (result.Displacement - delta).LengthSquared > .00000001f
                ? result.Displacement / seconds : velocity;
            return result;
        }
        private MovementResult Apply(Transform2D transform, WorldPoint delta, bool slide)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));
            RecoverOverlap(transform);
            var from = transform.Position;
            var next = geometry.Move(from, delta, transform.CollisionRadius, slide);
            transform.Position = next;
            return new MovementResult(next, next - from);
        }
    }
}

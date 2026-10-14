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

        public MovementResult Move(Transform2D transform, WorldPoint direction, float speed, float deltaTime, MovementMode mode)
        {
            if (transform == null || !direction.Finite)
                throw new ArgumentException("Invalid movement input.");
            Guard.Number(speed, nameof(speed), 0, 100);
            Guard.Delta(deltaTime);
            Guard.EnumValue(mode);
            if (direction.LengthSquared > 1)
                direction = direction.Normalized;
            var result = Apply(transform, direction * (speed * deltaTime), mode == MovementMode.Walk);
            if (direction.LengthSquared > .000001f && deltaTime > 0)
                transform.FacingDirection = direction.Normalized;
            return result;
        }

        public MovementResult Displace(Transform2D transform, WorldPoint delta) => Apply(transform, delta, false);
        private MovementResult Apply(Transform2D transform, WorldPoint delta, bool slide)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));
            var from = transform.Position;
            var next = geometry.Move(from, delta, transform.CollisionRadius, slide);
            transform.Position = next;
            return new MovementResult(next, next - from);
        }
    }
}

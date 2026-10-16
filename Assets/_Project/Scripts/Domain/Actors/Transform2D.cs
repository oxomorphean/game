using System;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;

namespace LinhThu.Domain.Actors
{
    public sealed class Transform2D
    {
        private WorldPoint position;
        internal event Action PositionChanged;
        public WorldPoint Position
        {
            get => position;
            internal set
            {
                if (!value.Finite) throw new ArgumentException("Invalid position.");
                position = value;
                PositionChanged?.Invoke();
            }
        }

        public WorldPoint FacingDirection { get; internal set; }

        public WorldPoint CurrentVelocity { get; internal set; }

        public float CollisionRadius { get; }

        public Transform2D(WorldPoint position, WorldPoint facingDirection, float collisionRadius)
        {
            if (!position.Finite || !facingDirection.Finite || facingDirection.LengthSquared < .000001f)
                throw new ArgumentException("Invalid transform.");
            CollisionRadius = Guard.Number(collisionRadius, nameof(collisionRadius), .01f, 10);
            Position = position;
            FacingDirection = facingDirection.Normalized;
        }
    }
}

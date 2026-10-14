using System;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;

namespace LinhThu.Domain.Actors
{
    public sealed class Transform2D
    {
        public WorldPoint Position { get; internal set; }

        public WorldPoint FacingDirection { get; internal set; }

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

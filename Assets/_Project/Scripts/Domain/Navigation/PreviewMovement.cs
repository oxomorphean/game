using System;

namespace LinhThu.Domain.Navigation
{
    /// <summary>Resolved position and displacement for a local movement preview, independent of rendering.</summary>
    public sealed class PreviewMovement
    {
        public MovementArena Arena { get; }
        public WorldPoint Position { get; private set; }
        public WorldPoint Displacement { get; private set; }
        public float Speed { get; }
        public float Radius { get; }
        private readonly WorldPoint spawn;

        public PreviewMovement(MovementArena arena, WorldPoint spawn, float speed, float radius)
        {
            if (arena == null || !GeometryRules.Range(speed, .1f, 100) || !GeometryRules.Range(radius, .01f, 10) || !arena.CanStand(spawn, radius))
                throw new ArgumentException("Invalid movement preview configuration or blocked spawn.");
            Arena = arena; this.spawn = spawn; Speed = speed; Radius = radius; Position = spawn;
        }

        public void Step(WorldPoint direction, float seconds)
        {
            if (!direction.Finite || !GeometryRules.Range(seconds, 0, .1f))
                throw new ArgumentException("Invalid movement input or time step.");
            if (direction.LengthSquared > 1) direction = direction.Normalized;
            var next = Arena.Move(Position, direction * (Speed * seconds), Radius, true);
            Displacement = next - Position;
            Position = next;
        }

        public void Reset() { Position = spawn; Displacement = default; }
    }
}

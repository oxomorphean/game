using LinhThu.Domain.Core;

namespace LinhThu.Domain.Navigation
{
    /// <summary>Shared simulation tuning; no engine curves or rendering state.</summary>
    public sealed class MovementSpec
    {
        public static MovementSpec Default { get; } = new MovementSpec();
        public float Acceleration { get; }
        public float Deceleration { get; }
        public float TurnResponsiveness { get; }
        public float DashDuration { get; }
        public float DashSpeedMultiplier { get; }
        public float DashCooldown { get; }

        public MovementSpec(float acceleration = 28, float deceleration = 22, float turnResponsiveness = 16,
            float dashDuration = .2f, float dashSpeedMultiplier = 4, float dashCooldown = 1)
        {
            Acceleration = Guard.Number(acceleration, nameof(acceleration), .1f, 1000);
            Deceleration = Guard.Number(deceleration, nameof(deceleration), .1f, 1000);
            TurnResponsiveness = Guard.Number(turnResponsiveness, nameof(turnResponsiveness), 0, 100);
            DashDuration = Guard.Number(dashDuration, nameof(dashDuration), .05f, 1);
            DashSpeedMultiplier = Guard.Number(dashSpeedMultiplier, nameof(dashSpeedMultiplier), 1, 10);
            DashCooldown = Guard.Number(dashCooldown, nameof(dashCooldown), dashDuration, 60);
        }
    }
}

using System;
using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;

namespace LinhThu.Domain.World
{
    public sealed class WorldNodeEntity
    {
        public EntityId Id { get; }

        public WorldNodeSpec Spec { get; }

        public string DefinitionId => Spec.DefinitionId;
        public WorldPoint Position { get; }

        public float GrowthTimer { get; internal set; }

        public bool IsDepleted { get; internal set; }

        public string CropDefinitionId { get; internal set; }

        public EntityId? AssignedBeastId { get; internal set; }

        public bool IsReady => !IsDepleted && GrowthTimer <= .000001f && Spec.Kind != WorldNodeKind.TeleportPortal && (Spec.Kind != WorldNodeKind.SpiritFieldPlot || CropDefinitionId != null);
        public WorldNodeEntity(EntityId id, WorldNodeSpec spec, WorldPoint position, bool ready = true)
        {
            if (id.Value <= 0 || !position.Finite)
                throw new ArgumentException("Invalid node.");
            Id = id;
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
            Position = position;
            GrowthTimer = ready ? 0 : spec.GrowthSeconds;
        }

        public void Tick(float deltaTime)
        {
            Guard.Delta(deltaTime);
            if (!IsDepleted)
                GrowthTimer = Math.Max(0, GrowthTimer - deltaTime);
        }
    }
}

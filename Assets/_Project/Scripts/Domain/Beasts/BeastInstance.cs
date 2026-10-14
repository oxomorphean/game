using System;
using LinhThu.Domain.Core;

namespace LinhThu.Domain.Beasts
{
    public enum BeastState
    {
        InEgg,
        Resting,
        SummonedActive,
        AssignedToField
    }

    public sealed class BeastInstance
    {
        public EntityId Id { get; }

        public EntityId OwnerId { get; }

        public string DefinitionId { get; }

        public string QualityId { get; }

        public string BloodlineId { get; }

        public float Bond { get; }

        public BeastState State { get; internal set; }

        public EntityId? ActorId { get; internal set; }

        public EntityId? AssignedFieldId { get; internal set; }

        public int BondSourceId { get; internal set; }

        public float? RestingHp { get; internal set; }

        public float? RestingQi { get; internal set; }

        public BeastInstance(EntityId id, EntityId ownerId, string definitionId, string qualityId, string bloodlineId, float bond, BeastState state = BeastState.InEgg)
        {
            if (id.Value <= 0 || ownerId.Value <= 0)
                throw new ArgumentException("Invalid beast identity.");
            Id = id;
            OwnerId = ownerId;
            DefinitionId = Guard.Id(definitionId);
            QualityId = Guard.Id(qualityId);
            BloodlineId = Guard.Id(bloodlineId);
            Bond = Guard.Number(bond, nameof(bond), 0, 1);
            State = Guard.EnumValue(state);
        }
    }
}

using LinhThu.Domain.Core;
using LinhThu.Domain.Navigation;

namespace LinhThu.Domain.Actors
{
    public sealed class LifeActivitySpec
    {
        public float MeditationQiMultiplier { get; }
        public float MeditationTickSeconds { get; }
        public int MeditationExperience { get; }

        public LifeActivitySpec(float qiMultiplier = 3, float tickSeconds = 1, int experience = 1)
        {
            MeditationQiMultiplier = Guard.Number(qiMultiplier, nameof(qiMultiplier), 1, 100);
            MeditationTickSeconds = Guard.Number(tickSeconds, nameof(tickSeconds), .1f, 60);
            if (experience < 1 || experience > 10000) throw new System.ArgumentOutOfRangeException(nameof(experience));
            MeditationExperience = experience;
        }
    }

    /// <summary>Committed activity state, shared by local and remote presentation.</summary>
    public sealed class LifeActivityState
    {
        public float MeditationElapsed { get; internal set; }
        public EntityId? FishingNodeId { get; internal set; }
        public WorldPoint FishingTarget { get; internal set; }
        public float FishingRemaining { get; internal set; }
        public bool FishBiting => FishingNodeId.HasValue && FishingRemaining <= .000001f;

        internal void Reset()
        {
            MeditationElapsed = FishingRemaining = 0;
            FishingNodeId = null;
            FishingTarget = default;
        }
    }
}

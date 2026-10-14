namespace LinhThu.Domain.Core
{
    public readonly struct RandomState
    {
        public string AlgorithmId { get; }

        public uint State { get; }

        public RandomState(string algorithmId, uint state)
        {
            AlgorithmId = algorithmId;
            State = state;
        }
    }

    public interface IRandomSource
    {
        float Next01();
        RandomState Capture();
        void Restore(RandomState state);
    }
}

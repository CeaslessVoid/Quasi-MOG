namespace Entities
{
    public interface ITurnActor
    {
        bool CanAct { get; }
        void TakeTurn();
    }
}
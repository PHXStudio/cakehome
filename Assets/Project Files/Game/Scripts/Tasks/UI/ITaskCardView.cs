using System;

namespace Watermelon
{
    // Optional hook for task cards that want a non-instant appear/disappear. UITaskPanel checks
    // for this interface when adding/removing cards; cards that don't implement it keep the
    // previous instant instantiate/destroy behavior.
    public interface ITaskCardView
    {
        void PlayEnter();
        void PlayExit(Action onComplete);
    }
}

using System;
using System.Collections;

namespace Watermelon
{
    public interface ILoadingStep
    {
        string LoadingMessage { get; }
        string ErrorMessage { get; }

        IEnumerator Execute(Initializer initializer, Action<bool> onCompleted);
    }
}

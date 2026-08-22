using UnityEngine;

namespace Watermelon
{
    public abstract class UIDialogStepView : MonoBehaviour
    {
        public virtual bool IsAnimating => false;
        public virtual void CompleteInstant() { }
    }
}

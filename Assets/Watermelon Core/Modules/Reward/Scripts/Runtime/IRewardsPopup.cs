using System.Collections.Generic;

namespace Watermelon
{
    public interface IRewardsPopup
    {
        bool Display(List<IRewardPreview> previews, SimpleCallback closeCallback);
    }
}

using System;

namespace Watermelon
{
    /// <summary>
    /// Marks a concrete <see cref="Reward"/> subclass with its matching <see cref="RewardView"/> type.
    /// The code generator (<c>RewardsMapGenerator</c>) reads this attribute to produce
    /// <c>MD_GeneratedRewardsMap.cs</c>, which populates <see cref="RewardsMap"/> at runtime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class RegisterRewardAttribute : Attribute
    {
        public Type ViewType { get; }

        public RegisterRewardAttribute(Type viewType)
        {
            ViewType = viewType;
        }
    }
}

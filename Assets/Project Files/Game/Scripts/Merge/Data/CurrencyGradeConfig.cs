using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class CurrencyGradeConfig : MergeGradeConfig
    {
        [SerializeField] CurrencyAmount currencyAmount;

        public CurrencyAmount Amount => currencyAmount;
    }
}

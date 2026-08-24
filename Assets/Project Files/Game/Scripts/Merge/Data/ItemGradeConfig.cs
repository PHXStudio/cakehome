using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class ItemGradeConfig : MergeGradeConfig
    {
        [SerializeField] int sellPrice; // 0 = Delete, >0 = Sell

        public int SellPrice => sellPrice;
    }
}

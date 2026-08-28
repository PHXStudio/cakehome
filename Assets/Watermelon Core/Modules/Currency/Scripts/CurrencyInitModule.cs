using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Currencies", false)]
    public class CurrencyInitModule : InitModule
    {
        public override string ModuleName => "Currencies";

        [SerializeField] CurrencyDatabase currenciesDatabase;
        public CurrencyDatabase Database => currenciesDatabase;

        private CurrencyController currencyController;

        public override IEnumerator InitAsync(GameObject owner)
        {
            currencyController = new CurrencyController(currenciesDatabase);
            yield break;
        }

        public override void Unload()
        {
            // 防: Play 中途停止时模块可能尚未初始化（currencyController 为 null）
            if (currencyController != null)
            {
                currencyController.Unload();
                currencyController = null;
            }
        }
    }
}

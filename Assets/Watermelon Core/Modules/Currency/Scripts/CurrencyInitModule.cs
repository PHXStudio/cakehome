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
            currencyController.Unload();
            currencyController = null;
        }
    }
}

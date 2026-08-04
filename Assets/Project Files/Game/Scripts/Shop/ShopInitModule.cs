using UnityEngine;

namespace Watermelon
{
    [RegisterModule("Shop System", core: false)]
    public class ShopInitModule : InitModule
    {
        public override string ModuleName => "Shop System";

        [SerializeField] CakeCatalog cakeCatalog;
        [SerializeField] ShopConfig shopConfig;

        public override void CreateComponent()
        {
            if (cakeCatalog == null)
                cakeCatalog = Resources.Load<CakeCatalog>("Shop/Cake Catalog");

            if (shopConfig == null)
                shopConfig = Resources.Load<ShopConfig>("Shop/Shop Config");

            ShopController.Init(cakeCatalog, shopConfig);
        }
    }
}

using UnityEngine;

namespace Watermelon
{
    public class ShopShelfSlot : MonoBehaviour
    {
        [SerializeField] Transform cakeAnchor;
        [SerializeField] GameObject lockedVisual;
        [SerializeField] GameObject emptyVisual;
        [SerializeField] GameObject recommendBadge;
        [SerializeField] GameObject hotBadge;

        private GameObject currentCakeVisual;
        private int shelfIndex = -1;

        public int ShelfIndex => shelfIndex;
        public Transform CakeAnchor => cakeAnchor != null ? cakeAnchor : transform;

        public void Setup(int index, Transform anchor, GameObject locked, GameObject empty, GameObject recommend, GameObject hot)
        {
            shelfIndex = index;
            cakeAnchor = anchor;
            lockedVisual = locked;
            emptyVisual = empty;
            recommendBadge = recommend;
            hotBadge = hot;
        }

        public void Setup(int index)
        {
            shelfIndex = index;
        }

        public void Refresh()
        {
            if (!ShopController.IsInitialized)
                return;

            bool unlocked = ShopController.IsShelfUnlocked(shelfIndex);
            if (lockedVisual != null)
                lockedVisual.SetActive(!unlocked);

            if (!unlocked)
            {
                ClearCakeVisual();
                if (emptyVisual != null)
                    emptyVisual.SetActive(false);
                if (recommendBadge != null)
                    recommendBadge.SetActive(false);
                if (hotBadge != null)
                    hotBadge.SetActive(false);
                return;
            }

            OwnedCake cake = ShopController.GetCakeOnShelf(shelfIndex);
            if (emptyVisual != null)
                emptyVisual.SetActive(cake == null);

            if (cake == null)
            {
                ClearCakeVisual();
                if (recommendBadge != null)
                    recommendBadge.SetActive(false);
                if (hotBadge != null)
                    hotBadge.SetActive(false);
                return;
            }

            EnsureCakeVisual(cake);

            System.DateTime now = System.DateTime.Now;
            if (recommendBadge != null)
                recommendBadge.SetActive(ShopController.IsManagerRecommended(cake, now));

            if (hotBadge != null)
                hotBadge.SetActive(ShopController.IsHotThemeMatch(cake));
        }

        private void EnsureCakeVisual(OwnedCake cake)
        {
            CakeDefinition definition = ShopController.GetDefinition(cake);
            string key = cake.InstanceId;

            if (currentCakeVisual != null && currentCakeVisual.name == key)
                return;

            ClearCakeVisual();

            Transform anchor = CakeAnchor;
            if (definition != null && definition.DisplayPrefab != null)
            {
                currentCakeVisual = Instantiate(definition.DisplayPrefab, anchor);
            }
            else
            {
                currentCakeVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                currentCakeVisual.transform.SetParent(anchor, false);
                currentCakeVisual.transform.localScale = new Vector3(0.55f, 0.28f, 0.55f);
                currentCakeVisual.transform.localPosition = Vector3.up * 0.28f;

                Collider col = currentCakeVisual.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);

                Color color = definition != null ? definition.DisplayColor : Color.white;
                MeshRenderer renderer = currentCakeVisual.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                    if (shader != null)
                    {
                        renderer.material = new Material(shader) { color = color };
                    }
                }
            }

            currentCakeVisual.name = key;
            currentCakeVisual.transform.localRotation = Quaternion.identity;
        }

        private void ClearCakeVisual()
        {
            if (currentCakeVisual == null)
                return;

            Destroy(currentCakeVisual);
            currentCakeVisual = null;
        }

        private void OnMouseDown()
        {
            if (!ShopController.IsInitialized)
                return;

            if (!ShopController.IsShelfUnlocked(shelfIndex))
                return;

            OwnedCake cake = ShopController.GetCakeOnShelf(shelfIndex);
            if (cake != null)
                ShopController.UnshelveToFreezer(cake.InstanceId);
        }
    }
}

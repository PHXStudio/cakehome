using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Manages a set of page prefabs that are instantiated on demand rather than existing in the scene at startup.
    /// Pages can optionally be destroyed automatically when they close (<see cref="PageData.AutoDestroy"/>).
    /// </summary>
    [System.Serializable]
    internal class CachedPages
    {
        [SerializeField] PageData[] pages;
        /// <summary>All configured cached page entries (may include unassigned prefabs).</summary>
        public PageData[] Pages => pages;

        private UIController controller;
        private Dictionary<Type, PageData> pagesMap;

        public void Init(UIController controller)
        {
            this.controller = controller;

            pagesMap = new Dictionary<Type, PageData>(pages.Length);
            foreach (PageData page in pages)
            {
                if (page.PagePrefab == null) continue;

                Type pageType = page.PagePrefab.GetType();
                if (!pagesMap.ContainsKey(pageType))
                    pagesMap.Add(pageType, page);
            }
        }

        /// <summary>Returns <c>true</c> if a cached page of the given type is configured.</summary>
        public bool HasPage(Type pageType)
        {
            return pagesMap.ContainsKey(pageType);
        }

        /// <summary>Returns the <see cref="PageData"/> for the given type, or <c>null</c> if not found.</summary>
        public PageData GetPageData(Type pageType)
        {
            pagesMap.TryGetValue(pageType, out PageData data);
            return data;
        }

        /// <summary>
        /// Instantiates the cached prefab for the given type, initializes it, and registers it
        /// with the <see cref="UIController"/>. Returns the created <see cref="UIPage"/> instance.
        /// </summary>
        public UIPage InstantiateCachedPage(Type pageType)
        {
            PageData pageData = GetPageData(pageType);
            if (pageData == null)
            {
                Debug.LogError($"[UIController]: Cached page {pageType} was not found. Make sure the UIController is initialized and the page prefab is properly linked!");

                return null;
            }

            if(pageData.PagePrefab == null)
            {
                Debug.LogError($"[UIController]: Cached page {pageType} has been registered, but the linked prefab reference is missing!");

                return null;
            }

            UIPage page = GameObject.Instantiate(pageData.PagePrefab, controller.transform);
            page.CacheComponents();

            Canvas prefabCanvas = pageData.PagePrefab.GetComponent<Canvas>();
            if(prefabCanvas != null)
            {
                int sortingOrder = prefabCanvas.sortingOrder;
                if (sortingOrder != 0)
                    controller.StartCoroutine(ApplySortingOrderNextFrame(page.Canvas, sortingOrder));
            }

            page.PreparePage();
            page.Init();
            page.MarkAsCached();

            controller.RegisterCachedPage(pageType, page);

            return page;
        }

        private IEnumerator ApplySortingOrderNextFrame(Canvas canvas, int sortingOrder)
        {
            yield return null;
            
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        /// <summary>Configuration entry for a single cached page prefab.</summary>
        [System.Serializable]
        public class PageData
        {
            [SerializeField] UIPage pagePrefab;
            /// <summary>The page prefab to instantiate on demand.</summary>
            public UIPage PagePrefab => pagePrefab;

            [SerializeField] bool autoDestroy = true;
            /// <summary>When <c>true</c>, the instantiated page is destroyed after it closes.</summary>
            public bool AutoDestroy => autoDestroy;
        }
    }
}
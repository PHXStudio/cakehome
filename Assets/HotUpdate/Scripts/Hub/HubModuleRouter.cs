using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Routes bottom-nav tabs to independent gameplay modules.
    /// UIBottomNavBar and GameController talk only to this router, not to Shop/Map details.
    /// </summary>
    [StaticUnload]
    public static class HubModuleRouter
    {
        private static readonly Dictionary<MainHubTab, IHubModule> modules = new Dictionary<MainHubTab, IHubModule>();
        private static IHubModule activeModule;
        private static bool initialized;

        public static MainHubTab? ActiveTab => activeModule != null ? activeModule.Tab : (MainHubTab?)null;
        public static bool HasActiveModule => activeModule != null && activeModule.IsActive;

        public static void EnsureInitialized()
        {
            if (initialized)
                return;

            Register(new ShopHubModule());
            Register(new CamperHubModule());
            Register(new ProfileHubModule());
            initialized = true;
        }

        public static void Register(IHubModule module)
        {
            if (module == null)
                return;

            modules[module.Tab] = module;
        }

        public static bool IsModuleActive(MainHubTab tab)
        {
            EnsureInitialized();

            if (activeModule == null || activeModule.Tab != tab)
                return false;

            return activeModule.IsActive;
        }

        public static void SwitchTo(MainHubTab tab, bool force = false)
        {
            EnsureInitialized();

            if (!force && activeModule != null && activeModule.Tab == tab && activeModule.IsActive)
                return;

            if (UIController.IsDisplayed<Watermelon.IAPStore.UIStore>())
                UIController.HidePage<Watermelon.IAPStore.UIStore>();

            if (activeModule != null)
            {
                activeModule.Exit();
                activeModule = null;
            }

            if (!modules.TryGetValue(tab, out IHubModule next) || next == null)
            {
                Debug.LogWarning($"[Hub] No module registered for tab {tab}");
                return;
            }

            activeModule = next;
            activeModule.Enter();
        }

        public static void ExitAll()
        {
            EnsureInitialized();

            if (UIController.IsDisplayed<Watermelon.IAPStore.UIStore>())
                UIController.HidePage<Watermelon.IAPStore.UIStore>();

            if (activeModule != null)
            {
                activeModule.Exit();
                activeModule = null;
            }

            // Safety: ensure every registered module is inactive even if tracking drifted.
            foreach (KeyValuePair<MainHubTab, IHubModule> pair in modules)
            {
                if (pair.Value != null && pair.Value.IsActive)
                    pair.Value.Exit();
            }
        }

        private static void UnloadStatic()
        {
            activeModule = null;
            modules.Clear();
            initialized = false;
        }
    }
}

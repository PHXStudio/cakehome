using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    public static class LogCategoryToggles
    {
        private const string MENU_ALL_ON    = "Actions/Log Categories/All On";
        private const string MENU_ALL_OFF   = "Actions/Log Categories/All Off";
        private const string MENU_GAME      = "Actions/Log Categories/Game";
        private const string MENU_SYSTEMS   = "Actions/Log Categories/Systems";
        private const string MENU_SERVICES  = "Actions/Log Categories/Services";
        private const string MENU_CHECKPOINT = "Actions/Log Categories/Checkpoint";

        private const string DEFINE_GAME       = "DEBUG_LOGS_GAME";
        private const string DEFINE_SYSTEMS    = "DEBUG_LOGS_SYSTEMS";
        private const string DEFINE_SERVICES   = "DEBUG_LOGS_SERVICES";
        private const string DEFINE_CHECKPOINT = "DEBUG_LOGS_CHECKPOINT";

        static LogCategoryToggles()
        {
            EditorApplication.delayCall += () =>
            {
                Menu.SetChecked(MENU_GAME,       DefineManager.HasDefine(DEFINE_GAME));
                Menu.SetChecked(MENU_SYSTEMS,    DefineManager.HasDefine(DEFINE_SYSTEMS));
                Menu.SetChecked(MENU_SERVICES,   DefineManager.HasDefine(DEFINE_SERVICES));
                Menu.SetChecked(MENU_CHECKPOINT, DefineManager.HasDefine(DEFINE_CHECKPOINT));
            };
        }

        [MenuItem(MENU_ALL_ON, priority = 1)]
        private static void AllOn()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Log Categories]: Can't change log categories while game is running!");
                return;
            }

            DefineManager.EnableDefine(DEFINE_GAME);
            DefineManager.EnableDefine(DEFINE_SYSTEMS);
            DefineManager.EnableDefine(DEFINE_SERVICES);
            DefineManager.EnableDefine(DEFINE_CHECKPOINT);
            Menu.SetChecked(MENU_GAME, true);
            Menu.SetChecked(MENU_SYSTEMS, true);
            Menu.SetChecked(MENU_SERVICES, true);
            Menu.SetChecked(MENU_CHECKPOINT, true);
        }

        [MenuItem(MENU_ALL_OFF, priority = 2)]
        private static void AllOff()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Log Categories]: Can't change log categories while game is running!");
                return;
            }

            DefineManager.DisableDefine(DEFINE_GAME);
            DefineManager.DisableDefine(DEFINE_SYSTEMS);
            DefineManager.DisableDefine(DEFINE_SERVICES);
            DefineManager.DisableDefine(DEFINE_CHECKPOINT);
            Menu.SetChecked(MENU_GAME, false);
            Menu.SetChecked(MENU_SYSTEMS, false);
            Menu.SetChecked(MENU_SERVICES, false);
            Menu.SetChecked(MENU_CHECKPOINT, false);
        }

        [MenuItem(MENU_GAME, priority = 100)]
        private static void ToggleGame()       => Toggle(DEFINE_GAME, MENU_GAME);

        [MenuItem(MENU_SYSTEMS, priority = 101)]
        private static void ToggleSystems()    => Toggle(DEFINE_SYSTEMS, MENU_SYSTEMS);

        [MenuItem(MENU_SERVICES, priority = 102)]
        private static void ToggleServices()   => Toggle(DEFINE_SERVICES, MENU_SERVICES);

        [MenuItem(MENU_CHECKPOINT, priority = 103)]
        private static void ToggleCheckpoint() => Toggle(DEFINE_CHECKPOINT, MENU_CHECKPOINT);

        [MenuItem(MENU_GAME, validate = true)]
        private static bool ValidateGame()       => Validate(DEFINE_GAME, MENU_GAME);

        [MenuItem(MENU_SYSTEMS, validate = true)]
        private static bool ValidateSystems()    => Validate(DEFINE_SYSTEMS, MENU_SYSTEMS);

        [MenuItem(MENU_SERVICES, validate = true)]
        private static bool ValidateServices()   => Validate(DEFINE_SERVICES, MENU_SERVICES);

        [MenuItem(MENU_CHECKPOINT, validate = true)]
        private static bool ValidateCheckpoint() => Validate(DEFINE_CHECKPOINT, MENU_CHECKPOINT);

        private static void Toggle(string define, string menuName)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Log Categories]: Can't change log categories while game is running!");
                return;
            }

            bool active = DefineManager.HasDefine(define);
            if (active) DefineManager.DisableDefine(define);
            else        DefineManager.EnableDefine(define);
            Menu.SetChecked(menuName, !active);
        }

        private static bool Validate(string define, string menuName)
        {
            Menu.SetChecked(menuName, DefineManager.HasDefine(define));
            return true;
        }
    }
}

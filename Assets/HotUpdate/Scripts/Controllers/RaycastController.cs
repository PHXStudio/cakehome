using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Watermelon
{
    public class RaycastController : MonoBehaviour
    {
        private static bool isActive;

        public static event SimpleCallback OnInputActivated;

        public void Init()
        {
            isActive = true;
        }

        private void Update()
        {
            if (!isActive || !LevelController.IsRaycastEnabled || UIController.IsPopupOpened) return;

            if (InputController.ClickAction.WasPressedThisFrame())
            {
                Ray ray = Camera.main.ScreenPointToRay(InputController.MousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit))
                {
                    IClickableObject clickableObject = hit.transform.GetComponent<IClickableObject>();
                    if (clickableObject != null)
                    {
                        if (LevelController.IsLevelLoaded)
                        {
                            clickableObject.OnObjectClicked();
                        }
                    }
                }
            }
        }

        public static void Disable()
        {
            isActive = false;
        }

        public static void Enable()
        {
            isActive = true;

            OnInputActivated?.Invoke();
        }

        public void ResetControl()
        {

        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Watermelon
{
    public static class UIExtensions
    {
        public static void ClickButton(this Button button)
        {
            if (button == null) return;

            button.StartCoroutine(ClickButtonCoroutine(button));
        }

        private static readonly WaitForSecondsRealtime ClickDelay = new(0.2f);

        private static IEnumerator ClickButtonCoroutine(Button button)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);

            yield return null;

            button.OnPointerClick(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });

            yield return ClickDelay;

            EventSystem.current.SetSelectedGameObject(null);
        }

        public static void AddEvent(this Component behaviour, EventTriggerType triggerType, Action<PointerEventData> call)
        {
            AddEvent(behaviour.gameObject, triggerType, call);
        }

        public static void AddEvent(this GameObject behaviour, EventTriggerType triggerType, Action<PointerEventData> call)
        {
            EventTrigger trigger = behaviour.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = behaviour.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = triggerType;
            entry.callback.AddListener((data) => { call((PointerEventData)data); });

            trigger.triggers.Add(entry);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    // Shared scale-bounce for any transform (select, spawn, etc.) — default curves and duration
    // are configured once in Game Data; custom settings can be passed per call.
    public class BounceAnimation
    {
        public static BounceAnimation Instance { get; private set; }

        private readonly BounceSettings defaultSettings;
        private readonly Dictionary<Transform, TweenCase> activeCases = new Dictionary<Transform, TweenCase>();

        private BounceAnimation(BounceSettings settings)
        {
            defaultSettings = settings;
        }

        public static void Init(BounceSettings bounceSettings)
        {
            Instance = new BounceAnimation(bounceSettings);
        }

        public static void Bounce(Transform target, BounceSettings settings = null, Action onComplete = null)
        {
            Instance?.BounceInternal(target, settings, onComplete);
        }

        private void BounceInternal(Transform target, BounceSettings settings, Action onComplete)
        {
            if (settings == null)
                settings = defaultSettings;

            if (activeCases.TryGetValue(target, out TweenCase activeCase))
                activeCase.KillActive();

            activeCases[target] = Tween.DoFloat(0f, 1f, settings.Duration, t =>
            {
                if (target != null)
                    target.localScale = settings.Evaluate(t);
            }).OnComplete(() =>
            {
                activeCases.Remove(target);
                onComplete?.Invoke();
            });
        }
    }
}

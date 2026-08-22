#pragma warning disable 0649

using System.Collections;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// <see cref="InitModule"/> that bootstraps the Tween system during game initialisation.
    /// Registered as a core module (order 800) — it runs early so that every subsequent module
    /// can safely create tweens in its own init coroutine.
    ///
    /// <para>Configuration (set in the Inspector via the Project Init Settings asset):
    /// <list type="bullet">
    ///   <item><c>tweensUpdateCount</c> — initial capacity of the <see cref="UpdateMethod.Update"/> tween array (default 300).</item>
    ///   <item><c>tweensFixedUpdateCount</c> — initial capacity of the <see cref="UpdateMethod.FixedUpdate"/> array (default 30).</item>
    ///   <item><c>tweensLateUpdateCount</c> — initial capacity of the <see cref="UpdateMethod.LateUpdate"/> array (default 0).</item>
    ///   <item><c>customEasingFunctions</c> — project-specific <see cref="AnimationCurve"/>-based easing presets registered with <see cref="Ease.Init"/>.</item>
    /// </list>
    /// Tune capacities to the expected peak tween count to avoid mid-game array reallocations.
    /// </para>
    /// </summary>
    [RegisterModule("Tween", core: true, order: 800)]
    public class TweenInitModule : InitModule
    {
        public override string ModuleName => "Tween";

        [SerializeField] CustomEasingFunction[] customEasingFunctions;

        [Space]
        [SerializeField] int tweensUpdateCount = 300;
        [SerializeField] int tweensFixedUpdateCount = 30;
        [SerializeField] int tweensLateUpdateCount = 0;

        /// <summary>
        /// Adds the <see cref="Tween"/> MonoBehaviour to the initialiser's <paramref name="owner"/> GameObject,
        /// calls <see cref="Tween.Init"/> with the configured capacities, and registers any custom easing functions.
        /// </summary>
        public override IEnumerator InitAsync(GameObject owner)
        {
            Tween tween = owner.AddComponent<Tween>();
            tween.Init(tweensUpdateCount, tweensFixedUpdateCount, tweensLateUpdateCount);

            Ease.Init(customEasingFunctions);

            yield break;
        }
    }
}

namespace Watermelon
{
    /// <summary>
    /// Specifies which Unity update loop drives a <see cref="TweenCase"/>.
    /// The value is used as an array index into <see cref="TweenManager.Holders"/>.
    /// </summary>
    public enum UpdateMethod
    {
        /// <summary>Tween is updated in <c>MonoBehaviour.Update</c> — the most common choice. Affected by <c>Time.timeScale</c> unless <see cref="TweenCase.IsUnscaled"/> is set.</summary>
        Update = 0,

        /// <summary>Tween is updated in <c>MonoBehaviour.FixedUpdate</c> — use for physics-driven animations to stay in sync with the physics step.</summary>
        FixedUpdate = 1,

        /// <summary>Tween is updated in <c>MonoBehaviour.LateUpdate</c> — use when the animated value must be applied after all regular updates (e.g. camera follow).</summary>
        LateUpdate = 2
    }
}

namespace Watermelon
{
    /// <summary>
    /// Intermediate generic base for tweens that animate a single typed value on a target object.
    /// Concrete subclasses (e.g. <c>TransformTweenCases.Position</c>, <c>GraphicTweenCases.Fade</c>) inherit from this
    /// to get strongly-typed <see cref="tweenObject"/>, <see cref="startValue"/>, and <see cref="resultValue"/> fields
    /// without repeating the constructor boilerplate.
    ///
    /// <para><b>Implementing a concrete case:</b>
    /// <list type="number">
    ///   <item>Inherit from <c>TweenCaseFunction&lt;TBaseObject, TValue&gt;</c>.</item>
    ///   <item>In the constructor, store the initial value in <see cref="startValue"/> (read it from the target).</item>
    ///   <item>Implement <see cref="TweenCase.Invoke"/> to interpolate from <see cref="startValue"/> to <see cref="resultValue"/> using <c>Interpolate(state)</c>.</item>
    ///   <item>Implement <see cref="TweenCase.DefaultComplete"/> to snap directly to <see cref="resultValue"/>.</item>
    ///   <item>Implement <see cref="TweenCase.Validate"/> to return <c>false</c> when <see cref="tweenObject"/> is destroyed.</item>
    /// </list>
    /// </para>
    /// </summary>
    /// <typeparam name="TBaseObject">Type of the Unity object being animated (e.g. <c>Transform</c>, <c>SpriteRenderer</c>).</typeparam>
    /// <typeparam name="TValue">Type of the value being interpolated (e.g. <c>Vector3</c>, <c>Color</c>, <c>float</c>).</typeparam>
    public abstract class TweenCaseFunction<TBaseObject, TValue> : TweenCase
    {
        /// <summary>The target Unity object being animated.</summary>
        public TBaseObject tweenObject;

        /// <summary>The value at which the animation begins. Typically captured from <see cref="tweenObject"/> in the constructor.</summary>
        public TValue startValue;

        /// <summary>The target value the animation moves towards.</summary>
        public TValue resultValue;

        /// <summary>
        /// Stores the target object and result value. Concrete subclasses should read the current
        /// value from <paramref name="tweenObject"/> and assign it to <see cref="startValue"/> in their own constructor.
        /// </summary>
        public TweenCaseFunction(TBaseObject tweenObject, TValue resultValue)
        {
            this.tweenObject = tweenObject;
            this.resultValue = resultValue;
        }
    }
}

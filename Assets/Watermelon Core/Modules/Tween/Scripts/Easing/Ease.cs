using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Static easing library. Provides 31 built-in mathematical easing functions plus support
    /// for custom <see cref="AnimationCurve"/>-based easings registered via <see cref="Init"/>.
    ///
    /// <para><b>Quick start:</b>
    /// <code>
    /// // Use a built-in type directly:
    /// float value = Ease.Interpolate(t, Ease.Type.CubicOut);
    ///
    /// // Use an IEasingFunction object (cached, allocation-free):
    /// var fn = Ease.GetFunction(Ease.Type.BounceOut);
    /// float value = fn.Interpolate(t);
    /// </code>
    /// </para>
    ///
    /// <para>All built-in functions satisfy <c>f(0) ≈ 0</c> and <c>f(1) ≈ 1</c>.
    /// Elastic, Back, and Bounce variants may temporarily overshoot these bounds.
    /// See <see href="http://easings.net">easings.net</see> for visual examples.</para>
    /// </summary>
    public class Ease
    {
        /// <summary>
        /// All built-in easing curve types. The integer value is used as a direct index into the
        /// internal function table so the enum order must not be changed.
        /// See visual examples at <see href="http://easings.net">easings.net</see>.
        /// </summary>
        public enum Type
        {
            Linear,
            QuadIn,
            QuadOut,
            QuadOutIn,
            CubicIn,
            CubicOut,
            CubicInOut,
            QuartIn,
            QuartOut,
            QuartInOut,
            QuintIn,
            QuintOut,
            QuintInOut,
            SineIn,
            SineOut,
            SineInOut,
            CircIn,
            CircOut,
            CircInOut,
            ExpoIn,
            ExpoOut,
            ExpoInOut,
            ElasticIn,
            ElasticOut,
            ElastinInOut,
            BackIn,
            BackOut,
            BackInOut,
            BounceIn,
            BounceOut,
            BounceInOut
        }

        private static readonly IEasingFunction[] easingFunctions = new IEasingFunction[31]
        {
            new LinearEasingFunction(),       // Linear
            new QuadInEasingFunction(),       // QuadIn
            new QuadOutEasingFunction(),      // QuadOut
            new QuadOutInEasingFunction(),    // QuadOutIn
            new CubicInEasingFunction(),      // CubicIn
            new CubicOutEasingFunction(),     // CubicOut
            new CubicInOutEasingFunction(),   // CubicInOut
            new QuartInEasingFunction(),      // QuartIn
            new QuartOutEasingFunction(),     // QuartOut
            new QuartInOutEasingFunction(),   // QuartInOut
            new QuintInEasingFunction(),      // QuintIn
            new QuintOutEasingFunction(),     // QuintOut
            new QuintInOutEasingFunction(),   // QuintInOut
            new SineInEasingFunction(),       // SineIn
            new SineOutEasingFunction(),      // SineOut
            new SineInOutEasingFunction(),    // SineInOut
            new CircInEasingFunction(),       // CircIn
            new CircOutEasingFunction(),      // CircOut
            new CircInOutEasingFunction(),    // CircInOut
            new ExpoInEasingFunction(),       // ExpoIn
            new ExpoOutEasingFunction(),      // ExpoOut
            new ExpoInOutEasingFunction(),    // ExpoInOut
            new ElasticInEasingFunction(),    // ElasticIn
            new ElasticOutEasingFunction(),   // ElasticOut
            new ElastinInOutEasingFunction(), // ElastinInOut
            new BackInEasingFunction(),       // BackIn
            new BackOutEasingFunction(),      // BackOut
            new BackInOutEasingFunction(),    // BackInOut
            new BounceInEasingFunction(),     // BounceIn
            new BounceOutEasingFunction(),    // BounceOut
            new BounceInOutEasingFunction(),  // BounceInOut
        };

        private const float PI = Mathf.PI;
        private const float HALF_PI = Mathf.PI / 2.0f;

        private static CustomEasingFunction defaultEasingFunction;
        private static CustomEasingFunction[] customEasingFunctions;
        private static Dictionary<int, int> customEasingFunctionsLink;

        /// <summary>
        /// Registers project-specific <see cref="CustomEasingFunction"/> presets.
        /// Must be called once at startup (done automatically by <see cref="TweenInitModule"/>).
        /// Functions are indexed by name hash for O(1) lookup via <see cref="GetCustomEasingFunction(string)"/>.
        /// </summary>
        /// <param name="easingFunctions">Array of named custom easing functions (may be empty).</param>
        public static void Init(CustomEasingFunction[] easingFunctions)
        {
            customEasingFunctionsLink = new Dictionary<int, int>();

            customEasingFunctions = easingFunctions;
            if(!easingFunctions.IsNullOrEmpty())
            {
                for (int i = 0; i < customEasingFunctions.Length; i++)
                {
                    customEasingFunctions[i].Init();

                    int functionNameHash = customEasingFunctions[i].Name.GetHashCode();
                    if (!customEasingFunctionsLink.ContainsKey(functionNameHash))
                    {
                        customEasingFunctionsLink.Add(functionNameHash, i);
                    }
                }
            }

            defaultEasingFunction = new CustomEasingFunction("default", new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1)));
        }

        /// <summary>
        /// Returns a registered <see cref="CustomEasingFunction"/> by name.
        /// Falls back to a linear default and logs an error if the name is not found.
        /// </summary>
        public static CustomEasingFunction GetCustomEasingFunction(string name)
        {
            int hash = name.GetHashCode();
            if (customEasingFunctionsLink.ContainsKey(hash))
            {
                return customEasingFunctions[customEasingFunctionsLink[hash]];
            }

            Debug.LogError(string.Format("[Tween]: Custom easing {0} is missing!", name));

            return defaultEasingFunction;
        }

        /// <summary>
        /// Returns a registered <see cref="CustomEasingFunction"/> by pre-computed name hash.
        /// Falls back to a linear default and logs an error if the hash is not found.
        /// </summary>
        public static CustomEasingFunction GetCustomEasingFunction(int hash)
        {
            if (customEasingFunctionsLink.ContainsKey(hash))
            {
                return customEasingFunctions[customEasingFunctionsLink[hash]];
            }

            Debug.LogError("[Tween]: Custom easing is missing!");

            return defaultEasingFunction;
        }

        /// <summary>
        /// Evaluates the built-in easing function for <paramref name="ease"/> at progress <paramref name="p"/>.
        /// Equivalent to calling <c>GetFunction(ease).Interpolate(p)</c> but avoids a virtual dispatch
        /// by going through the same cached instance.
        /// </summary>
        /// <param name="p">Normalised progress in [0, 1].</param>
        /// <param name="ease">The easing curve to apply.</param>
        /// <returns>Eased output value (may exceed [0, 1] for elastic/back/bounce variants).</returns>
        public static float Interpolate(float p, Type ease)
        {
            return easingFunctions[(int)ease].Interpolate(p);
        }

        /// <summary>
        /// Returns the cached <see cref="IEasingFunction"/> instance for <paramref name="ease"/>.
        /// Store the result if you need to call it repeatedly to avoid the array lookup overhead.
        /// </summary>
        public static IEasingFunction GetFunction(Type ease)
        {
            return easingFunctions[(int)ease];
        }

        #region Bounce
        private static float BounceEaseIn(float p)
        {
            return 1 - BounceEaseOut(1 - p);
        }

        private static float BounceEaseOut(float p)
        {
            if (p < 4 / 11.0f)
            {
                return (121 * p * p) / 16.0f;
            }
            else if (p < 8 / 11.0f)
            {
                return (363 / 40.0f * p * p) - (99 / 10.0f * p) + 17 / 5.0f;
            }
            else if (p < 9 / 10.0f)
            {
                return (4356 / 361.0f * p * p) - (35442 / 1805.0f * p) + 16061 / 1805.0f;
            }
            else
            {
                return (54 / 5.0f * p * p) - (513 / 25.0f * p) + 268 / 25.0f;
            }
        }

        private static float BounceEaseInOut(float p)
        {
            if (p < 0.5f)
            {
                return 0.5f * BounceEaseIn(p * 2);
            }
            else
            {
                return 0.5f * BounceEaseOut(p * 2 - 1) + 0.5f;
            }
        }
        #endregion

        public class LinearEasingFunction : IEasingFunction { public float Interpolate(float p) { return p; } }

        public class QuadInEasingFunction : IEasingFunction { public float Interpolate(float p) { return p * p; } }
        public class QuadOutEasingFunction : IEasingFunction { public float Interpolate(float p) { return -(p * (p - 2)); } }
        public class QuadOutInEasingFunction : IEasingFunction { public float Interpolate(float p) { if (p < 0.5f) { return 2 * p * p; } else { return (-2 * p * p) + (4 * p) - 1; }; } }

        public class CubicInEasingFunction : IEasingFunction { public float Interpolate(float p) { return p * p * p; } }
        public class CubicOutEasingFunction : IEasingFunction { public float Interpolate(float p) { float f1 = (p - 1); return f1 * f1 * f1 + 1; } }
        public class CubicInOutEasingFunction : IEasingFunction { public float Interpolate(float p) { if (p < 0.5f) { return 4 * p * p * p; } else { float f2 = ((2 * p) - 2); return 0.5f * f2 * f2 * f2 + 1; } } }

        public class QuartInEasingFunction : IEasingFunction { public float Interpolate(float p) { return p * p * p * p; } }
        public class QuartOutEasingFunction : IEasingFunction { public float Interpolate(float p) { float f3 = (p - 1); return f3 * f3 * f3 * (1 - p) + 1; } }
        public class QuartInOutEasingFunction : IEasingFunction { public float Interpolate(float p) { if (p < 0.5f) { return 8 * p * p * p * p; } else { float f4 = (p - 1); return -8 * f4 * f4 * f4 * f4 + 1; } } }

        public class QuintInEasingFunction : IEasingFunction { public float Interpolate(float p) { return p * p * p * p * p; } }
        public class QuintOutEasingFunction : IEasingFunction { public float Interpolate(float p) { float f = (p - 1); return f * f * f * f * f + 1; } }
        public class QuintInOutEasingFunction : IEasingFunction { public float Interpolate(float p) { if (p < 0.5f) { return 16 * p * p * p * p * p; } else { float f = ((2 * p) - 2); return 0.5f * f * f * f * f * f + 1; } } }

        public class SineInEasingFunction : IEasingFunction { public float Interpolate(float p) { return Mathf.Sin((p - 1) * HALF_PI) + 1; } }
        public class SineOutEasingFunction : IEasingFunction { public float Interpolate(float p) { return Mathf.Sin(p * HALF_PI); } }
        public class SineInOutEasingFunction : IEasingFunction { public float Interpolate(float p) { return 0.5f * (1 - Mathf.Cos(p * PI)); } }

        public class CircInEasingFunction : IEasingFunction { public float Interpolate(float p) { return 1 - Mathf.Sqrt(1 - (p * p)); } }
        public class CircOutEasingFunction : IEasingFunction { public float Interpolate(float p) { return Mathf.Sqrt((2 - p) * p); } }
        public class CircInOutEasingFunction : IEasingFunction { public float Interpolate(float p) { if (p < 0.5f) { return 0.5f * (1 - Mathf.Sqrt(1 - 4 * (p * p))); } else { return 0.5f * (Mathf.Sqrt(-((2 * p) - 3) * ((2 * p) - 1)) + 1); } } }

        public class ExpoInEasingFunction : IEasingFunction { public float Interpolate(float p) { return (p == 0.0f) ? p : Mathf.Pow(2, 10 * (p - 1)); } }
        public class ExpoOutEasingFunction : IEasingFunction { public float Interpolate(float p) { return (p == 1.0f) ? p : 1 - Mathf.Pow(2, -10 * p); } }
        public class ExpoInOutEasingFunction : IEasingFunction { public float Interpolate(float p) { if (p == 0.0 || p == 1.0) return p; if (p < 0.5f) { return 0.5f * Mathf.Pow(2, (20 * p) - 10); } else { return -0.5f * Mathf.Pow(2, (-20 * p) + 10) + 1; } } }

        public class ElasticInEasingFunction : IEasingFunction { public float Interpolate(float p) { return Mathf.Sin(13 * HALF_PI * p) * Mathf.Pow(2, 10 * (p - 1)); } }
        public class ElasticOutEasingFunction : IEasingFunction { public float Interpolate(float p) { return Mathf.Sin(-13 * HALF_PI * (p + 1)) * Mathf.Pow(2, -10 * p) + 1; } }
        public class ElastinInOutEasingFunction : IEasingFunction { public float Interpolate(float p) { if (p < 0.5f) { return 0.5f * Mathf.Sin(13 * HALF_PI * (2 * p)) * Mathf.Pow(2, 10 * ((2 * p) - 1)); } else { return 0.5f * (Mathf.Sin(-13 * HALF_PI * ((2 * p - 1) + 1)) * Mathf.Pow(2, -10 * (2 * p - 1)) + 2); } } }

        public class BackInEasingFunction : IEasingFunction { public float Interpolate(float p) { return p * p * p - p * Mathf.Sin(p * PI); } }
        public class BackOutEasingFunction : IEasingFunction { public float Interpolate(float p) { float f = (1 - p); return 1 - (f * f * f - f * Mathf.Sin(f * PI)); } }
        public class BackInOutEasingFunction : IEasingFunction { public float Interpolate(float p) { if (p < 0.5f) { float f = 2 * p; return 0.5f * (f * f * f - f * Mathf.Sin(f * PI)); } else { float f = (1 - (2 * p - 1)); return 0.5f * (1 - (f * f * f - f * Mathf.Sin(f * PI))) + 0.5f; } } }

        public class BounceInEasingFunction : IEasingFunction { public float Interpolate(float p) { return BounceEaseIn(p); } }
        public class BounceOutEasingFunction : IEasingFunction { public float Interpolate(float p) { return BounceEaseOut(p); } }
        public class BounceInOutEasingFunction : IEasingFunction { public float Interpolate(float p) { return BounceEaseInOut(p); } }

        /// <summary>
        /// Contract for any easing function — built-in or custom.
        /// Implement this interface to create a fully custom easing curve usable with <see cref="TweenCase.SetCustomEasing"/>.
        /// </summary>
        public interface IEasingFunction
        {
            /// <summary>
            /// Maps a normalised input progress <paramref name="p"/> to an eased output value.
            /// </summary>
            /// <param name="p">Normalised input in [0, 1].</param>
            /// <returns>Eased output — typically in [0, 1], may overshoot for elastic/back variants.</returns>
            public float Interpolate(float p);
        }
    }
}

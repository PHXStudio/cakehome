using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Tween extensions for <see cref="Transform"/> world/local space animations.
    /// This is the most comprehensive cases file, covering:
    /// <list type="bullet">
    ///   <item><b>Rotation</b> — Euler angles, Quaternion, local, constant, look-at (3D and 2D)</item>
    ///   <item><b>Position</b> — world/local, per-axis, X+Z combined, Bezier curves, follow</item>
    ///   <item><b>Scale</b> — uniform, per-axis, two-phase push, ping-pong oscillation</item>
    ///   <item><b>Shake</b> — random 3D position offset</item>
    /// </list>
    /// All extension methods target <see cref="Component"/> so they work on any MonoBehaviour or Transform directly.
    /// </summary>
    public static class TransformTweenCases
    {
        #region Extensions
        /// <summary>
        /// Animates <see cref="Transform.eulerAngles"/> to <paramref name="resultValue"/>.
        /// Automatically clamps the rotation delta into the ±180° range to take the shortest arc.
        /// </summary>
        public static TweenCase DORotate(this Component tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new RotateAngle(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Animates <see cref="Transform.rotation"/> (world-space <see cref="Quaternion"/>) to <paramref name="resultValue"/> using <see cref="Quaternion.LerpUnclamped"/>.
        /// </summary>
        public static TweenCase DORotate(this Component tweenObject, Quaternion resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new RotateQuaternion(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Animates <see cref="Transform.localRotation"/> to <paramref name="resultValue"/> using <see cref="Quaternion.LerpUnclamped"/>.
        /// </summary>
        public static TweenCase DOLocalRotate(this Component tweenObject, Quaternion resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new LocalRotate(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Animates <see cref="Transform.localEulerAngles"/> to <paramref name="resultValue"/> (delegates to <see cref="RotateAngle"/> in local space).
        /// </summary>
        public static TweenCase DOLocalRotate(this Component tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new RotateAngle(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Rotates the object continuously at <paramref name="rotationVector"/> degrees per second for the specified <paramref name="time"/>.
        /// Uses <see cref="Time.deltaTime"/> directly, so it is not affected by the tween's easing curve.
        /// </summary>
        public static TweenCase DORotateConstant(this Component tweenObject, Vector3 rotationVector, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new RotateConstant(tweenObject.transform, rotationVector).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Animates <see cref="Transform.position"/> (world space) to <paramref name="resultValue"/>.
        /// </summary>
        public static TweenCase DOMove(this Component tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new Position(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Animates <see cref="Transform.position"/> (world space) to the position of <paramref name="resultValue"/> at the moment the tween starts.
        /// </summary>
        public static TweenCase DOMove(this Component tweenObject, Transform resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new Position(tweenObject.transform, resultValue.position).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Moves the object along a <b>quadratic Bezier curve</b> to <paramref name="resultValue"/>,
        /// with the control point auto-calculated from <paramref name="upOffset"/>, <paramref name="rightOffset"/>, and <paramref name="forwardOffset"/> relative to the path midpoint.
        /// </summary>
        public static TweenCase DOBezierMove(this Component tweenObject, Vector3 resultValue, float upOffset, float rightOffset, float forwardOffset, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new BezierPosition(tweenObject.transform, resultValue, upOffset, rightOffset, forwardOffset).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Moves the object along a <b>quadratic Bezier curve</b> to <paramref name="resultValue"/> using an explicit <paramref name="controlPoint1"/>.
        /// </summary>
        public static TweenCase DOBezierMove(this Component tweenObject, Vector3 resultValue, Vector3 controlPoint1, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new BezierPosition(tweenObject.transform, resultValue, controlPoint1).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Moves the object along a <b>cubic Bezier curve</b> to <paramref name="resultValue"/> using two explicit control points.
        /// </summary>
        public static TweenCase DOBezierMove(this Component tweenObject, Vector3 resultValue, Vector3 controlPoint1, Vector3 controlPoint2, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new BezierPosition(tweenObject.transform, resultValue, controlPoint1, controlPoint2).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Moves the object toward <paramref name="target"/> at <paramref name="speed"/> units per second using <see cref="Vector3.MoveTowards"/>.
        /// Completes automatically when within <paramref name="minimumDistance"/> of the target.
        /// The tween duration is set to <see cref="float.MaxValue"/>; the tween self-completes when close enough.
        /// </summary>
        public static TweenCase DoFollow(this Component tweenObject, Transform target, float speed, float minimumDistance, float delay, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new Follow(tweenObject.transform, target, speed, minimumDistance).SetDelay(delay).SetDuration(float.MaxValue).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Moves the object along a quadratic Bezier curve that tracks the live position of <paramref name="resultValue"/> each frame.
        /// The control point is recalculated each update based on the current offset parameters.
        /// </summary>
        public static TweenCase DOBezierFollow(this Component tweenObject, Transform resultValue, float upOffset, float rightOffset, float forwardOffset, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new BezierFollow(tweenObject.transform, resultValue, upOffset, rightOffset, forwardOffset).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>Animates only the X component of <see cref="Transform.position"/>, leaving Y and Z unchanged.</summary>
        public static TweenCase DOMoveX(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new PositionX(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>Animates only the Y component of <see cref="Transform.position"/>, leaving X and Z unchanged.</summary>
        public static TweenCase DOMoveY(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new PositionY(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>Animates only the Z component of <see cref="Transform.position"/>, leaving X and Y unchanged.</summary>
        public static TweenCase DOMoveZ(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new PositionZ(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>Animates both X and Z components of <see cref="Transform.position"/> simultaneously, leaving Y unchanged.</summary>
        public static TweenCase DOMoveXZ(this Component tweenObject, float resultValueX, float resultValueZ, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new PositionXZ(tweenObject.transform, resultValueX, resultValueZ).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>Animates <see cref="Transform.localScale"/> to the Vector3 <paramref name="resultValue"/>.</summary>
        public static TweenCase DOScale(this Component tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new Scale(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>Animates <see cref="Transform.localScale"/> to a uniform scale of <paramref name="resultValue"/> on all axes.</summary>
        public static TweenCase DOScale(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new Scale(tweenObject.transform, new Vector3(resultValue, resultValue, resultValue)).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Animates <see cref="Transform.localScale"/> in two sequential phases: first to <paramref name="firstScale"/>, then to <paramref name="secondScale"/>.
        /// Each phase has its own duration and easing curve, allowing a punch-and-settle feel.
        /// </summary>
        public static TweenCase DOPushScale(this Component tweenObject, Vector3 firstScale, Vector3 secondScale, float firstScaleTime, float secondScaleTime, Ease.Type firstScaleEasing = Ease.Type.Linear, Ease.Type secondScaleEasing = Ease.Type.Linear, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new PushScale(tweenObject.transform, firstScale, secondScale, firstScaleTime, secondScaleTime, firstScaleEasing, secondScaleEasing).SetDelay(delay).SetDuration(firstScaleTime + secondScaleTime).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Animates <see cref="Transform.localScale"/> in two sequential phases using uniform float values.
        /// Converts floats to <c>Vector3</c> internally.
        /// </summary>
        public static TweenCase DOPushScale(this Component tweenObject, float firstScale, float secondScale, float firstScaleTime, float secondScaleTime, Ease.Type firstScaleEasing = Ease.Type.Linear, Ease.Type secondScaleEasing = Ease.Type.Linear, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new PushScale(tweenObject.transform, firstScale.ToVector3(), secondScale.ToVector3(), firstScaleTime, secondScaleTime, firstScaleEasing, secondScaleEasing).SetDelay(delay).SetDuration(firstScaleTime + secondScaleTime).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>Animates only the X component of <see cref="Transform.localScale"/>, leaving Y and Z unchanged.</summary>
        public static TweenCase DOScaleX(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new ScaleX(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>Animates only the Y component of <see cref="Transform.localScale"/>, leaving X and Z unchanged.</summary>
        public static TweenCase DOScaleY(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new ScaleY(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>Animates only the Z component of <see cref="Transform.localScale"/>, leaving X and Y unchanged.</summary>
        public static TweenCase DOScaleZ(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new ScaleZ(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Continuously oscillates <see cref="Transform.localScale"/> between <paramref name="minValue"/> and <paramref name="maxValue"/>
        /// for the given <paramref name="time"/> using separate easing curves for the scale-up and scale-down phases.
        /// Useful for idle pulse or heartbeat effects.
        /// </summary>
        public static TweenCase DOPingPongScale(this Component tweenObject, float minValue, float maxValue, float time, Ease.Type positiveScaleEasing, Ease.Type negativeScaleEasing, float delay = 0, bool unscaledTime = false)
        {
            return new PingPongScale(tweenObject.transform, minValue, maxValue, time, positiveScaleEasing, negativeScaleEasing).SetDelay(delay).SetUnscaledMode(unscaledTime).StartTween();
        }

        /// <summary>Animates <see cref="Transform.localPosition"/> to <paramref name="resultValue"/>.</summary>
        public static TweenCase DOLocalMove(this Component tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new LocalMove(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>Animates only the X component of <see cref="Transform.localPosition"/>, leaving Y and Z unchanged.</summary>
        public static TweenCase DOLocalMoveX(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new LocalPositionX(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>Animates only the Y component of <see cref="Transform.localPosition"/>, leaving X and Z unchanged.</summary>
        public static TweenCase DOLocalMoveY(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new LocalPositionY(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>Animates only the Z component of <see cref="Transform.localPosition"/>, leaving X and Y unchanged.</summary>
        public static TweenCase DOLocalMoveZ(this Component tweenObject, float resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new LocalPositionZ(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Smoothly rotates the object to face <paramref name="resultValue"/> (world position) using <see cref="Quaternion.Slerp"/>.
        /// </summary>
        public static TweenCase DOLookAt(this Component tweenObject, Vector3 resultValue, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new LookAt(tweenObject.transform, resultValue).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Smoothly rotates a 2D object so that its <paramref name="type"/> axis points toward <paramref name="resultValue"/> (world position).
        /// Uses Z-axis rotation only (suitable for 2D/top-down games).
        /// </summary>
        public static TweenCase DOLookAt2D(this Component tweenObject, Vector3 resultValue, TransformTweenCases.LookAt2D.LookAtType type, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new LookAt2D(tweenObject.transform, resultValue, type).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }

        /// <summary>
        /// Shakes <see cref="Transform.position"/> each frame by a random point on the unit sphere scaled by <c>magnitude × easedProgress</c>.
        /// Restores the original position on completion.
        /// </summary>
        public static TweenCase DOShake(this Component tweenObject, float magnitude, float time, float delay = 0, bool unscaledTime = false, UpdateMethod tweenType = UpdateMethod.Update)
        {
            return new Shake(tweenObject.transform, magnitude).SetDelay(delay).SetDuration(time).SetUnscaledMode(unscaledTime).SetUpdateMethod(tweenType).StartTween();
        }
        #endregion

        /// <summary>
        /// Interpolates <see cref="Transform.eulerAngles"/> from its starting value to <c>resultValue</c>.
        /// The constructor normalises the delta into ±180° on each axis to guarantee the shortest-arc rotation.
        /// </summary>
        public class RotateAngle : TweenCaseFunction<Transform, Vector3>
        {
            public RotateAngle(Transform tweenObject, Vector3 resultValue) : base(tweenObject, resultValue)
            {
                var startRotation = tweenObject.eulerAngles;

                if (resultValue.x - startRotation.x > 180)
                    resultValue.x -= 360;
                if (resultValue.y - startRotation.y > 180)
                    resultValue.y -= 360;
                if (resultValue.z - startRotation.z > 180)
                    resultValue.z -= 360;

                if (resultValue.x - startRotation.x < -180)
                    resultValue.x += 360;
                if (resultValue.y - startRotation.y < -180)
                    resultValue.y += 360;
                if (resultValue.z - startRotation.z < -180)
                    resultValue.z += 360;

                parentObject = tweenObject.gameObject;

                startValue = tweenObject.eulerAngles;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.eulerAngles = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.eulerAngles = Vector3.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>Interpolates <see cref="Transform.rotation"/> (world-space Quaternion) using <see cref="Quaternion.LerpUnclamped"/>.</summary>
        public class RotateQuaternion : TweenCaseFunction<Transform, Quaternion>
        {
            public RotateQuaternion(Transform tweenObject, Quaternion resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.rotation;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.rotation = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.rotation = Quaternion.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>Interpolates <see cref="Transform.localRotation"/> using <see cref="Quaternion.LerpUnclamped"/>.</summary>
        public class LocalRotate : TweenCaseFunction<Transform, Quaternion>
        {
            public LocalRotate(Transform tweenObject, Quaternion resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localRotation;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localRotation = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localRotation = Quaternion.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>
        /// Interpolates <see cref="Transform.localEulerAngles"/> from its starting value to <c>resultValue</c>.
        /// Validates that the parent GameObject is active (<c>activeSelf</c>) in addition to being non-null.
        /// </summary>
        public class LocalRotateAngle : TweenCaseFunction<Transform, Vector3>
        {
            public LocalRotateAngle(Transform tweenObject, Vector3 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localEulerAngles;
            }

            public override bool Validate()
            {
                return (parentObject != null && parentObject.activeSelf);
            }

            public override void DefaultComplete()
            {
                tweenObject.localEulerAngles = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localEulerAngles = Vector3.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>
        /// Rotates the object at a constant angular velocity (<paramref name="rotationVector"/> degrees/second) for the tween's duration.
        /// Unlike other rotation cases, this does <b>not</b> lerp between two values — it accumulates rotation each frame via <see cref="Transform.Rotate"/>.
        /// </summary>
        public class RotateConstant : TweenCase
        {
            private Transform objectTransform;
            private Vector3 rotationVector;

            public RotateConstant(Transform tweenObject, Vector3 rotationVector)
            {
                objectTransform = tweenObject;
                this.rotationVector = rotationVector;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {

            }

            public override void Invoke(float deltaTime)
            {
                objectTransform.Rotate(rotationVector * Time.deltaTime);
            }
        }

        /// <summary>Interpolates <see cref="Transform.position"/> (world space) from its starting value to <c>resultValue</c>.</summary>
        public class Position : TweenCaseFunction<Transform, Vector3>
        {
            public Position(Transform tweenObject, Vector3 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.position;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.position = Vector3.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>
        /// Interpolates <see cref="Transform.position"/> toward the live position of a target <see cref="Transform"/>.
        /// The target position is sampled at tween creation time; use <see cref="Follow"/> if you need continuous target tracking.
        /// </summary>
        public class PositionTransform : TweenCaseFunction<Transform, Transform>
        {
            private Vector3 startPosition;

            public PositionTransform(Transform tweenObject, Transform resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startPosition = tweenObject.position;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = resultValue.position;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.position = Vector3.LerpUnclamped(startPosition, resultValue.position, Interpolate(state));
            }
        }

        /// <summary>
        /// Moves along a Bezier curve (quadratic or cubic) from the starting world position to <c>resultValue</c>.
        /// <list type="bullet">
        ///   <item>Quadratic: one control point — uses <see cref="Bezier.EvaluateQuadratic"/>.</item>
        ///   <item>Cubic: two control points — uses <see cref="Bezier.EvaluateCubic"/>.</item>
        /// </list>
        /// The single-offset constructor auto-calculates the control point relative to the path midpoint.
        /// </summary>
        public class BezierPosition : TweenCaseFunction<Transform, Vector3>
        {
            private Vector3 controlPoint1;
            private Vector3 controlPoint2;

            private bool isQuadratic;

            public BezierPosition(Transform tweenObject, Vector3 resultValue, float upOffset, float rightOffset, float forwardOffset) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.position;

                var direction = resultValue - startValue;

                var rotation = Quaternion.FromToRotation(Vector3.forward, direction);

                var right = rotation * Vector3.right;

                isQuadratic = true;

                controlPoint1 = startValue + direction * 0.5f + Vector3.up * upOffset + right * rightOffset + direction.normalized * forwardOffset;
            }

            public BezierPosition(Transform tweenObject, Vector3 resultValue, Vector3 controlPoint) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;
                startValue = tweenObject.position;

                isQuadratic = true;

                controlPoint1 = controlPoint;
            }

            public BezierPosition(Transform tweenObject, Vector3 resultValue, Vector3 controlPoint1, Vector3 controlPoint2) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;
                startValue = tweenObject.position;

                isQuadratic = false;

                this.controlPoint1 = controlPoint1;
                this.controlPoint2 = controlPoint2;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                if (isQuadratic)
                {
                    tweenObject.position = BezierUtils.EvaluateQuadratic(startValue, controlPoint1, resultValue, Interpolate(state));
                }
                else
                {
                    tweenObject.position = BezierUtils.EvaluateCubic(startValue, controlPoint1, controlPoint2, resultValue, Interpolate(state));
                }
            }
        }

        /// <summary>
        /// Tracks a moving <see cref="Transform"/> target along a quadratic Bezier arc, recalculating the control point each frame.
        /// Unlike <see cref="BezierPosition"/>, the destination updates every frame as the target moves.
        /// </summary>
        public class BezierFollow : TweenCase
        {
            private Vector3 startPosition;

            private Transform fromTransform;
            private Transform toTransform;

            private Vector3 keyPoint1;
            private Vector3 keyPoint2;

            private float upOffset;
            private float rightOffset;
            private float forwardOffset;

            private bool isQuadratic;

            public BezierFollow(Transform tweenObject, Transform followTarget, float upOffset, float rightOffset, float forwardOffset)
            {
                parentObject = tweenObject.gameObject;

                startPosition = tweenObject.position;

                fromTransform = tweenObject;
                toTransform = followTarget;

                this.upOffset = upOffset;
                this.rightOffset = rightOffset;
                this.forwardOffset = forwardOffset;

                isQuadratic = true;

                RecalculateKeyPoints();
            }

            private void RecalculateKeyPoints()
            {
                var direction = toTransform.position - fromTransform.position;

                var rotation = Quaternion.FromToRotation(Vector3.forward, direction);

                var right = rotation * Vector3.right;

                keyPoint1 = fromTransform.position + direction * 0.5f + Vector3.up * upOffset + right * rightOffset + direction.normalized * forwardOffset;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                fromTransform.position = toTransform.position;
            }

            public override void Invoke(float deltaTime)
            {
                if (isQuadratic)
                {
                    fromTransform.position = BezierUtils.EvaluateQuadratic(startPosition, keyPoint1, toTransform.position, Interpolate(state));
                }
                else
                {
                    fromTransform.position = BezierUtils.EvaluateCubic(startPosition, keyPoint1, keyPoint2, toTransform.position, Interpolate(state));
                }
            }
        }

        /// <summary>
        /// Moves the object toward a <see cref="Transform"/> target at a fixed speed using <see cref="Vector3.MoveTowards"/>.
        /// Self-completes when squared distance falls below <c>minimumDistance²</c>. The tween is not eased — speed is constant.
        /// </summary>
        public class Follow : TweenCase
        {
            private Transform tweenObject;
            private Transform target;

            private float minimumDistanceSqr;
            private float speed;

            public Follow(Transform tweenObject, Transform target, float speed, float minimumDistance)
            {
                parentObject = tweenObject.gameObject;

                this.tweenObject = tweenObject;
                this.target = target;
                this.speed = speed;

                minimumDistanceSqr = Mathf.Pow(minimumDistance, 2);
            }

            public override bool Validate()
            {
                return parentObject != null && target != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = target.position;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.position = Vector3.MoveTowards(tweenObject.position, target.position, deltaTime * speed);

                if (Vector3.SqrMagnitude(tweenObject.position - target.position) <= minimumDistanceSqr)
                    Complete();
            }
        }

        /// <summary>Interpolates only the X component of <see cref="Transform.position"/>, preserving Y and Z.</summary>
        public class PositionX : TweenCaseFunction<Transform, float>
        {
            public PositionX(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.position.x;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = new Vector3(resultValue, tweenObject.position.y, tweenObject.position.z);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.position = new Vector3(Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)), tweenObject.position.y, tweenObject.position.z);
            }
        }

        /// <summary>Interpolates only the Y component of <see cref="Transform.position"/>, preserving X and Z.</summary>
        public class PositionY : TweenCaseFunction<Transform, float>
        {
            public PositionY(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.position.y;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = new Vector3(tweenObject.position.x, resultValue, tweenObject.position.z);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.position = new Vector3(tweenObject.position.x, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)), tweenObject.position.z);
            }
        }

        /// <summary>
        /// Interpolates both X and Z components of <see cref="Transform.position"/> simultaneously, preserving Y.
        /// Both axes share the same eased progress value, so they arrive at their targets at the same time.
        /// </summary>
        public class PositionXZ : TweenCase
        {
            private Transform tweenObject;

            private float resultValueX;
            private float resultValueZ;

            private float startValueX;
            private float startValueZ;

            private float intepolatedState;

            public PositionXZ(Transform tweenObject, float resultValueX, float resultValueZ)
            {
                this.tweenObject = tweenObject;

                this.resultValueX = resultValueX;
                this.resultValueZ = resultValueZ;

                parentObject = tweenObject.gameObject;

                startValueX = tweenObject.position.x;
                startValueZ = tweenObject.position.z;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = new Vector3(resultValueX, tweenObject.position.y, resultValueZ);
            }

            public override void Invoke(float deltaTime)
            {
                intepolatedState = Interpolate(state);

                tweenObject.position = new Vector3(Mathf.LerpUnclamped(startValueX, resultValueX, intepolatedState), tweenObject.position.y, Mathf.LerpUnclamped(startValueZ, resultValueZ, intepolatedState));
            }
        }

        /// <summary>Interpolates only the Z component of <see cref="Transform.position"/>, preserving X and Y.</summary>
        public class PositionZ : TweenCaseFunction<Transform, float>
        {
            public PositionZ(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.position.z;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = new Vector3(tweenObject.position.x, tweenObject.position.y, resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.position = new Vector3(tweenObject.position.x, tweenObject.position.y, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)));
            }
        }

        /// <summary>Interpolates <see cref="Transform.localScale"/> (all three axes) from its starting value to <c>resultValue</c>.</summary>
        public class Scale : TweenCaseFunction<Transform, Vector3>
        {
            public Scale(Transform tweenObject, Vector3 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localScale;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localScale = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localScale = Vector3.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>
        /// Animates <see cref="Transform.localScale"/> in two sequential phases with independent easing:
        /// <list type="number">
        ///   <item>Phase 1 (0 → <c>firstTime</c>): scales from start to <c>firstScaleValue</c> using <c>firstScaleEasing</c>.</item>
        ///   <item>Phase 2 (<c>firstTime</c> → total duration): scales from <c>firstScaleValue</c> to <c>secondScaleValue</c> using <c>secondScaleEasing</c>.</item>
        /// </list>
        /// </summary>
        public class PushScale : TweenCase
        {
            public Transform tweenObject;

            public Vector3 startValue;
            public Vector3 firstScaleValue;
            public Vector3 secondScaleValue;

            public float firstTime;
            public float secondTime;

            private Ease.Type firstScaleEasing;
            private Ease.Type secondScaleEasing;

            private float relativeState;

            public PushScale(Transform tweenObject, Vector3 firstScaleValue, Vector3 secondScaleValue, float firstTime, float secondTime, Ease.Type firstScaleEasing, Ease.Type secondScaleEasing)
            {
                this.tweenObject = tweenObject;

                this.startValue = tweenObject.localScale;

                this.firstScaleValue = firstScaleValue;
                this.secondScaleValue = secondScaleValue;

                this.firstTime = firstTime;
                this.secondTime = secondTime;

                this.firstScaleEasing = firstScaleEasing;
                this.secondScaleEasing = secondScaleEasing;

                parentObject = tweenObject.gameObject;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localScale = secondScaleValue;
            }

            public override void Invoke(float deltaTime)
            {
                relativeState = Duration * state;

                if (relativeState <= firstTime)
                {
                    tweenObject.localScale = Vector3.LerpUnclamped(startValue, firstScaleValue, Ease.Interpolate(Mathf.InverseLerp(0, firstTime, relativeState), firstScaleEasing));
                }
                else
                {
                    tweenObject.localScale = Vector3.LerpUnclamped(firstScaleValue, secondScaleValue, Ease.Interpolate(Mathf.InverseLerp(firstTime, Duration, relativeState), secondScaleEasing));
                }
            }
        }

        /// <summary>Interpolates only the X component of <see cref="Transform.localScale"/>, preserving Y and Z.</summary>
        public class ScaleX : TweenCaseFunction<Transform, float>
        {
            public ScaleX(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localScale.x;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localScale = new Vector3(resultValue, tweenObject.localScale.y, tweenObject.localScale.z);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localScale = new Vector3(Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)), tweenObject.localScale.y, tweenObject.localScale.z);
            }
        }

        /// <summary>Interpolates only the Y component of <see cref="Transform.localScale"/>, preserving X and Z.</summary>
        public class ScaleY : TweenCaseFunction<Transform, float>
        {
            public ScaleY(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localScale.y;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localScale = new Vector3(tweenObject.localScale.x, resultValue, tweenObject.localScale.z);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localScale = new Vector3(tweenObject.localScale.x, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)), tweenObject.localScale.z);
            }
        }

        /// <summary>Interpolates only the Z component of <see cref="Transform.localScale"/>, preserving X and Y.</summary>
        public class ScaleZ : TweenCaseFunction<Transform, float>
        {
            public ScaleZ(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localScale.z;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localScale = new Vector3(tweenObject.localScale.x, tweenObject.localScale.y, resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localScale = new Vector3(tweenObject.localScale.x, tweenObject.localScale.y, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)));
            }
        }

        /// <summary>
        /// Oscillates <see cref="Transform.localScale"/> uniformly between <c>minValue</c> and <c>maxValue</c> over the tween's duration.
        /// The duration is split into two equal half-periods: scale-up and scale-down, each with its own easing function.
        /// Accumulates wall-clock time internally rather than using the base <c>state</c> so that the oscillation
        /// runs independently of the outer tween progress.
        /// </summary>
        public class PingPongScale : TweenCase
        {
            private Transform tweenObject;

            private float minValue;
            private float maxValue;

            private float totalTime;
            private float halfTime;

            private float tempScaleValue;

            private bool direction;

            private Ease.IEasingFunction negativeEaseFunction;

            public PingPongScale(Transform tweenObject, float minValue, float maxValue, float duration, Ease.Type positiveScaleEasing, Ease.Type negativeScaleEasing)
            {
                this.tweenObject = tweenObject;

                this.minValue = minValue;
                this.maxValue = maxValue;

                SetDuration(duration);

                parentObject = tweenObject.gameObject;

                SetEasing(positiveScaleEasing);
                negativeEaseFunction = Ease.GetFunction(negativeScaleEasing);
                totalTime = 0;
                halfTime = Duration / 2f;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localScale = new Vector3(minValue, minValue, minValue);
            }

            public override void Invoke(float deltaTime)
            {
                totalTime += deltaTime;

                direction = (totalTime <= halfTime);

                if (direction)
                {
                    tempScaleValue = Mathf.LerpUnclamped(minValue, maxValue, Interpolate(totalTime / halfTime));
                }
                else
                {
                    tempScaleValue = Mathf.LerpUnclamped(maxValue, minValue, negativeEaseFunction.Interpolate((totalTime - halfTime) / halfTime));
                }

                tweenObject.localScale = new Vector3(tempScaleValue, tempScaleValue, tempScaleValue);
            }
        }

        /// <summary>Interpolates <see cref="Transform.localPosition"/> from its starting value to <c>resultValue</c>.</summary>
        public class LocalMove : TweenCaseFunction<Transform, Vector3>
        {
            public LocalMove(Transform tweenObject, Vector3 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localPosition;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localPosition = resultValue;
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localPosition = Vector3.LerpUnclamped(startValue, resultValue, Interpolate(state));
            }
        }

        /// <summary>Interpolates only the X component of <see cref="Transform.localPosition"/>, preserving Y and Z.</summary>
        public class LocalPositionX : TweenCaseFunction<Transform, float>
        {
            public LocalPositionX(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localPosition.x;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localPosition = new Vector3(resultValue, tweenObject.localPosition.y, tweenObject.localPosition.z);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localPosition = new Vector3(Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)), tweenObject.localPosition.y, tweenObject.localPosition.z);
            }
        }

        /// <summary>Interpolates only the Y component of <see cref="Transform.localPosition"/>, preserving X and Z.</summary>
        public class LocalPositionY : TweenCaseFunction<Transform, float>
        {
            public LocalPositionY(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localPosition.y;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localPosition = new Vector3(tweenObject.localPosition.x, resultValue, tweenObject.localPosition.z);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localPosition = new Vector3(tweenObject.localPosition.x, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)), tweenObject.localPosition.z);
            }
        }

        /// <summary>Interpolates only the Z component of <see cref="Transform.localPosition"/>, preserving X and Y.</summary>
        public class LocalPositionZ : TweenCaseFunction<Transform, float>
        {
            public LocalPositionZ(Transform tweenObject, float resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.localPosition.z;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.localPosition = new Vector3(tweenObject.localPosition.x, tweenObject.localPosition.y, resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.localPosition = new Vector3(tweenObject.localPosition.x, tweenObject.localPosition.y, Mathf.LerpUnclamped(startValue, resultValue, Interpolate(state)));
            }
        }

        /// <summary>
        /// Smoothly rotates the object to face a world-space <c>resultValue</c> position using <see cref="Quaternion.Slerp"/>.
        /// The target direction is computed from the object's position at tween creation time.
        /// </summary>
        public class LookAt : TweenCaseFunction<Transform, Vector3>
        {
            private Quaternion startRotation;

            public LookAt(Transform tweenObject, Vector3 resultValue) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                startValue = tweenObject.position;
                startRotation = tweenObject.rotation;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.LookAt(resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                var targetRotation = Quaternion.LookRotation(resultValue - startValue);

                // Smoothly rotate towards the target point.
                tweenObject.rotation = Quaternion.Slerp(startRotation, targetRotation, Interpolate(state));
            }
        }

        /// <summary>
        /// Smoothly rotates a 2D object toward a world-space target using Z-axis rotation only.
        /// The facing axis is selected by <see cref="LookAtType"/>: <c>Up</c> rotates the sprite's up-axis, <c>Right</c> rotates the right-axis, <c>Forward</c> the forward-axis.
        /// </summary>
        public class LookAt2D : TweenCaseFunction<Transform, Vector3>
        {
            /// <summary>Which local axis of the object should point toward the target.</summary>
            public LookAtType type;
            float rotationZ;

            public LookAt2D(Transform tweenObject, Vector3 resultValue, LookAtType type) : base(tweenObject, resultValue)
            {
                parentObject = tweenObject.gameObject;

                this.type = type;

                startValue = tweenObject.eulerAngles;

                Vector3 different = (resultValue - tweenObject.position);
                different.Normalize();

                rotationZ = (Mathf.Atan2(different.y, different.x) * Mathf.Rad2Deg);

                if (type == LookAtType.Up)
                    rotationZ -= 90;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.LookAt(resultValue);
            }

            public override void Invoke(float deltaTime)
            {
                tweenObject.rotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(startValue.z, rotationZ, Interpolate(state)));
            }

            /// <summary>Specifies which local axis of the 2D object should point toward the look-at target.</summary>
            public enum LookAtType
            {
                /// <summary>The sprite's local up axis (+Y) faces the target (e.g. character heads).</summary>
                Up,
                /// <summary>The sprite's local right axis (+X) faces the target.</summary>
                Right,
                /// <summary>The sprite's local forward axis (+Z) faces the target.</summary>
                Forward
            }
        }

        /// <summary>
        /// Shakes <see cref="Transform.position"/> each frame by a random point on the unit sphere scaled by <c>magnitude × easedProgress</c>.
        /// The object's original position is restored when the tween completes.
        /// </summary>
        public class Shake : TweenCase
        {
            private Transform tweenObject;
            private Vector3 startPosition;
            private float magnitude;

            public Shake(Transform tweenObject, float magnitude)
            {
                this.tweenObject = tweenObject;
                this.magnitude = magnitude;

                parentObject = tweenObject.gameObject;

                startPosition = tweenObject.position;
            }

            public override bool Validate()
            {
                return parentObject != null;
            }

            public override void DefaultComplete()
            {
                tweenObject.position = startPosition;
            }

            public override void Invoke(float timeDelta)
            {
                tweenObject.position = startPosition + Random.onUnitSphere * magnitude * Interpolate(state);
            }
        }
    }
}

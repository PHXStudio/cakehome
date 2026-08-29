using UnityEngine;
using UnityEngine.InputSystem;

namespace Watermelon
{
    /// <summary>
    /// 45° 等距相机拖拽平移（鼠标左键/单指触摸），带边界钳制。
    /// 挂在正交相机上，配合 CakeShop 场景使用。
    /// 注意：项目 Active Input Handling = Input System 包，UnityEngine.Input 不可用。
    /// </summary>
    public class IsoCameraPan : MonoBehaviour
    {
        [SerializeField] float panSpeed = 1f;
        [SerializeField] Vector2 minBounds = new Vector2(-2f, -2f);
        [SerializeField] Vector2 maxBounds = new Vector2(32f, 32f);
        [SerializeField] float inertiaDamping = 6f;

        Camera cam;
        Vector2 lastPointerPos;
        Vector3 velocity;
        bool dragging;

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        void Update()
        {
            Vector2 pointer;
            bool pressed;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                pointer = Touchscreen.current.primaryTouch.position.ReadValue();
                pressed = true;
            }
            else if (Mouse.current != null)
            {
                pointer = Mouse.current.position.ReadValue();
                pressed = Mouse.current.leftButton.isPressed;
            }
            else
            {
                return;
            }

            if (pressed)
            {
                if (!dragging)
                {
                    dragging = true;
                    lastPointerPos = pointer;
                    velocity = Vector3.zero;
                }
                else
                {
                    Vector2 delta = pointer - lastPointerPos;
                    lastPointerPos = pointer;

                    // 屏幕像素 → 世界单位（正交相机垂直方向：2*orthoSize 覆盖 Screen.height 像素）
                    float worldPerPixel = (cam.orthographicSize * 2f) / Screen.height;
                    Vector3 move = ScreenDeltaToWorld(delta) * (worldPerPixel * panSpeed);
                    transform.position -= move;
                    velocity = -move / Mathf.Max(Time.deltaTime, 0.0001f);
                }
            }
            else
            {
                dragging = false;
                // 惯性滑行
                if (velocity.sqrMagnitude > 0.01f)
                {
                    transform.position += velocity * Time.deltaTime;
                    velocity = Vector3.Lerp(velocity, Vector3.zero, inertiaDamping * Time.deltaTime);
                }
            }

            // 边界钳制（X/Z 平面）
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
            pos.z = Mathf.Clamp(pos.z, minBounds.y, maxBounds.y);
            transform.position = pos;
        }

        // 屏幕 delta 映射到地面 XZ 平面（相机 right + 相机 forward 在 XZ 的投影）
        Vector3 ScreenDeltaToWorld(Vector2 screenDelta)
        {
            Vector3 right = transform.right;
            Vector3 fwd = transform.forward;
            fwd.y = 0;
            fwd.Normalize();
            return right * screenDelta.x + fwd * screenDelta.y;
        }
    }
}

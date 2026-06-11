using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    // 这里的相机只跟车的位置和水平朝向，不直接继承车体翻滚，避免高速时画面抖得太厉害。
    public Transform target; // 相机需要跟随的车辆根对象。
    public float distance = 8f; // 相机在车辆后方保持的水平距离。
    public float height = 4f; // 相机高于车辆根对象的高度。
    public float lookHeight = 1.2f; // 相机注视点相对车辆根对象向上的偏移。
    public float followSmoothTime = 0.18f; // 位置平滑时间，越大越柔和，但响应会更慢。
    public float rotateSpeed = 8f; // 相机朝向目标时的旋转平滑速度。
    public bool lockBehindTarget = true; // 是否始终把相机锁在车辆屁股后方。
    public float yawFollowSpeed = 12f; // 车辆转向时相机追随车辆水平朝向的速度。
    public bool allowMouseOrbit = false; // 是否允许鼠标绕车旋转相机。
    public bool requireLeftMouseHold = true; // 是否必须按住鼠标左键才允许旋转相机。
    public bool requireRightMouseHold = false; // 是否必须按住鼠标右键才允许旋转相机。
    public float mouseSensitivity = 0.15f; // 鼠标控制相机旋转的灵敏度。
    public float pitch = 12f; // 当前相机俯仰角初值。
    public float minPitch = -10f; // 鼠标控制时允许的最小俯仰角。
    public float maxPitch = 45f; // 鼠标控制时允许的最大俯仰角。

    private Vector3 followVelocity;
    private float orbitYaw;
    private bool orbitInitialized;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        UpdateCamera(false);
    }

    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        followVelocity = Vector3.zero;
        orbitInitialized = false;
        UpdateCamera(true);
    }

    private void UpdateCamera(bool snapInstantly)
    {
        if (!orbitInitialized)
        {
            orbitYaw = GetTargetPlanarYaw();
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            orbitInitialized = true;
        }

        UpdateOrbitAngles(snapInstantly);

        Vector3 lookPoint = target.position + Vector3.up * lookHeight;
        Quaternion orbitRotation = Quaternion.Euler(pitch, orbitYaw, 0f);
        Vector3 desiredOffset = orbitRotation * new Vector3(0f, height - lookHeight, -distance);
        Vector3 desiredPosition = lookPoint + desiredOffset;
        transform.position = snapInstantly
            ? desiredPosition
            : Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, followSmoothTime);

        // 看向车体略高一点的位置，比直接盯着根节点更稳定，也更符合第三人称视角。
        Quaternion desiredRotation = Quaternion.LookRotation((lookPoint - transform.position).normalized, Vector3.up);
        transform.rotation = snapInstantly
            ? desiredRotation
            : Quaternion.Slerp(transform.rotation, desiredRotation, rotateSpeed * Time.deltaTime);
    }

    private void UpdateOrbitAngles(bool snapInstantly)
    {
        if (lockBehindTarget)
        {
            float targetYaw = GetTargetPlanarYaw();
            orbitYaw = snapInstantly
                ? targetYaw
                : Mathf.LerpAngle(orbitYaw, targetYaw, Mathf.Clamp01(yawFollowSpeed * Time.deltaTime));
            return;
        }

        UpdateMouseOrbit();
    }

    private float GetTargetPlanarYaw()
    {
        Vector3 planarForward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
        if (planarForward.sqrMagnitude < 0.001f)
        {
            planarForward = Vector3.forward;
        }

        return Quaternion.LookRotation(planarForward.normalized, Vector3.up).eulerAngles.y;
    }

    private void UpdateMouseOrbit()
    {
        if (!allowMouseOrbit)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        bool leftHeld = mouse.leftButton.isPressed;
        bool rightHeld = mouse.rightButton.isPressed;
        bool needsHold = requireLeftMouseHold || requireRightMouseHold;
        bool holdSatisfied =
            (!requireLeftMouseHold || leftHeld) &&
            (!requireRightMouseHold || rightHeld);

        if (needsHold && !holdSatisfied)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        if (delta.sqrMagnitude < 0.0001f)
        {
            return;
        }

        orbitYaw += delta.x * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, minPitch, maxPitch);
    }
}

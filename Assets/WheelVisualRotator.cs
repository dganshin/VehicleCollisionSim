using System.Collections.Generic;
using UnityEngine;

public class WheelVisualRotator : MonoBehaviour
{
    // 这里只做视觉轮子旋转，不参与实际物理求解。
    public float wheelRadius = 0.37f; // 近似轮胎半径，用来把前进速度换算成视觉滚动速度。
    public float spinMultiplier = 1f; // 视觉轮子滚动速度的倍率。
    public float steerAngle = 25f; // 前轮视觉偏转时使用的最大转角。
    public WheelVisual[] wheels; // 可选的轮子列表；为空时脚本会按名字自动查找轮子网格。

    private Rigidbody rb;
    private SimpleCarController controller;

    [System.Serializable]
    public class WheelVisual
    {
        public Transform wheelTransform; // 需要做视觉旋转的轮子网格 Transform。
        public bool steerWithInput; // 这个轮子是否跟随转向输入做视觉偏转。
        public Vector3 rotationAxis = Vector3.right; // 轮子滚动时使用的局部旋转轴。
        public Vector3 steerAxis = Vector3.up; // 轮子转向时使用的局部偏转轴。

        [HideInInspector] public Quaternion baseLocalRotation;
        [HideInInspector] public float spinAngle;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<SimpleCarController>();

        if (wheels == null || wheels.Length == 0)
        {
            // 默认按名字自动找轮子，减少后续手工拖引用的工作量。
            AutoFindWheels();
        }

        CacheWheelRotations();
    }

    private void LateUpdate()
    {
        if (rb == null || wheels == null || wheels.Length == 0)
        {
            return;
        }

        float forwardSpeed = controller != null
            ? controller.CurrentForwardSpeed
            : Vector3.Dot(GetLinearVelocity(), transform.forward);

        float deltaSpin = wheelRadius > 0.001f
            ? (forwardSpeed / (2f * Mathf.PI * wheelRadius)) * 360f * Time.deltaTime * spinMultiplier
            : 0f;

        float steerInput = controller != null ? controller.CurrentSteerInput : 0f;

        for (int i = 0; i < wheels.Length; i++)
        {
            WheelVisual wheel = wheels[i];
            if (wheel == null || wheel.wheelTransform == null)
            {
                continue;
            }

            wheel.spinAngle += deltaSpin;

            Quaternion steerRotation = wheel.steerWithInput
                ? Quaternion.AngleAxis(steerInput * steerAngle, wheel.steerAxis.normalized)
                : Quaternion.identity;

            // 视觉朝向 = 初始本地朝向 * 转向偏角 * 滚动角。
            Quaternion spinRotation = Quaternion.AngleAxis(wheel.spinAngle, wheel.rotationAxis.normalized);
            wheel.wheelTransform.localRotation = wheel.baseLocalRotation * steerRotation * spinRotation;
        }
    }

    private void AutoFindWheels()
    {
        List<WheelVisual> foundWheels = new List<WheelVisual>();
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>(true);

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            Transform meshTransform = meshRenderers[i].transform;
            string lowerName = meshTransform.name.ToLowerInvariant();

            if (!lowerName.Contains("wheel"))
            {
                continue;
            }

            // 名字里带 front 的轮子默认允许跟随转向输入偏转。
            bool isFrontWheel = lowerName.Contains("front");
            foundWheels.Add(new WheelVisual
            {
                wheelTransform = meshTransform,
                steerWithInput = isFrontWheel,
                rotationAxis = Vector3.right,
                steerAxis = Vector3.up
            });
        }

        wheels = foundWheels.ToArray();
    }

    private void CacheWheelRotations()
    {
        if (wheels == null)
        {
            return;
        }

        for (int i = 0; i < wheels.Length; i++)
        {
            if (wheels[i] == null || wheels[i].wheelTransform == null)
            {
                continue;
            }

            wheels[i].baseLocalRotation = wheels[i].wheelTransform.localRotation;
            wheels[i].spinAngle = 0f;
        }
    }

    private Vector3 GetLinearVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }
}

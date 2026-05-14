using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleCarController : MonoBehaviour
{
    public bool isControlled = false; // 当前这辆车是否接收玩家输入。
    public float motorAcceleration = 2600f; // 车辆已经滚起来之后的正常驱动力。
    public float startBoostAcceleration = 3400f; // 低速起步时的额外驱动力，用来帮助车辆脱离静止状态。
    public float turnAcceleration = 2.5f; // 基础转向强度；在轮驱动模式下会换算成前轮转角。
    public float highSpeedTurnFactor = 0.45f; // 车速升高后用于削弱转向能力的系数。
    public float brakeDamping = 4.5f; // 按下 Space 或需要快速收住车身时使用的附加阻尼/制动力度。
    public float maxSpeed = 72f; // 软速度上限，超过后不再继续施加额外驱动力或驱动扭矩。
    public float lateralGrip = 8f; // 备用刚体驱动模式下用于抑制横向侧滑的力度。
    public float minTurnSpeed = 2.5f; // 备用刚体驱动模式下，达到该前后速度后才允许明显转向。
    public float steerAngularDamping = 6f; // 备用刚体驱动模式下用于压制低速原地乱扭的偏航阻尼。
    public float flippedDotThreshold = 0.55f; // 车身朝上程度低于该阈值时，判定为翻车并停止驱动输入。
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.4f, 0f); // 局部重心偏移，用来降低重心、减少翻车倾向。
    public bool disableChildWheelColliders = false; // 旧调试开关；当前方案保持 WheelCollider 启用。
    public bool applyRuntimeRigidbodySettings = true; // 是否在运行时覆盖部分 Rigidbody 阻尼和插值设置。
    public float drivingLinearDamping = 0.015f; // 持续给油时使用的线性阻尼，应尽量小，避免前进动力被阻尼吃掉。
    public float runtimeLinearDamping = 0.05f; // 松开油门后的基础空气阻力；保持较小，但要足够避免车辆长时间自己继续跑。
    public float runtimeAngularDamping = 5f; // 车辆旋转时的基础角阻尼，用于减少晃动和过度旋转。
    public bool useInactiveCollisionTuning = false; // 是否对非当前控制车辆启用更低阻力碰撞展示参数；当前默认关闭，避免切车后手感不一致。
    public float inactiveLinearDamping = 0.02f; // 非激活车辆使用的较低线性阻尼，兼顾被撞动和不过分打滑。
    public float wheelDampingRateScale = 0.35f; // 当前主线车辆的轮子滚动阻力缩放，降低后可减少“松油马上被拖死”和碰撞后过快耗能。
    public float inactiveWheelDampingRate = 0.01f; // 非激活车辆轮子的滚动阻力，适度降低，避免像手刹锁死。
    public float inactiveForwardFrictionStiffness = 0.08f; // 非激活车辆前后向轮胎抓地力倍率，低于默认值但不至于完全打滑。
    public float inactiveSidewaysFrictionStiffness = 0.35f; // 非激活车辆侧向轮胎抓地力倍率，保留明显侧向阻力，避免侧门轻易被推走。
    public float wheelTorqueScale = 1.25f; // 轮驱动模式下的扭矩倍率，避免把 Inspector 参数再额外放大很多倍。
    public float reverseAccelerationScale = 0.4f; // 倒车驱动力倍率，通常应明显低于前进驱动力。
    public float brakeTorqueScale = 160f; // 刹车扭矩倍率，用来控制按下 Space 后的实际制动强度。
    public float coastBrakeTorque = 35f; // 松开油门后的温和轮上阻力，主要用于中高速滑行，不用于低速急停。
    public float coastBrakeStartSpeed = 2.5f; // 低于该速度时不再施加空挡轮上阻力，避免 1 m/s 左右突然像重刹。
    public float coastBrakeFullSpeed = 10f; // 达到该速度后，空挡轮上阻力增长到设定上限。
    public float coastingWheelSyncBrakeTorque = 320f; // 松开刹车且无油门时，用来把轮速拉回车速的同步刹车扭矩。
    public float coastingWheelSyncRpmTolerance = 25f; // 轮速与车速对应转速相差超过该阈值时，认为存在明显“轮速滞后/超前”。
    public float coastingWheelSyncMinSpeed = 2f; // 只有速度高于该阈值时才启用轮速同步，避免低速阶段像突然补刹。
    public float lowSpeedCoastThreshold = 2.2f; // 低于该速度后开始补一点尾速收口，避免低速滑太久停不下来。
    public float lowSpeedCoastBrakeTorque = 0f; // 低速空挡尾速收口使用的小刹车扭矩；默认关闭，避免再次出现“低速突然一脚刹车”。
    public float directionChangeSpeedThreshold = 0.35f; // 当前进/后退方向与输入相反且速度高于该阈值时，先刹停再反向驱动。
    public float directionChangeBrakeTorque = 2500f; // 换向阶段附加的刹车扭矩，用来避免先顺着旧方向多滑一截。
    public float stationaryReverseRpmThreshold = 120f; // 车辆几乎没动但轮子仍在空转时，超过该转速就先刹轮再允许反向。
    public float stationaryReverseBrakeTorque = 3200f; // 顶住障碍或另一辆车时，切换前后方向所用的额外刹车扭矩。
    public float stationaryReverseLockTime = 0.18f; // 静止受阻时前后换向的强制刹轮时间，避免像开船一样慢悠悠反向。
    public bool holdBrakeAtIdle = false; // 是否在几乎静止且无输入时自动补一点刹车，当前默认关闭以保留自然滑停。
    public bool applyLowFrictionMaterial = true; // 是否在运行时给车身碰撞体附加低摩擦材质。
    public float bodyStaticFriction = 0.08f; // 运行时车身碰撞体材质的静摩擦系数。
    public float bodyDynamicFriction = 0.06f; // 运行时车身碰撞体材质的动摩擦系数。
    public bool liftVehicleAboveGroundOnStart = true; // 是否在启动时把车辆轻微抬起，避免出生时和地面穿插。
    public float startGroundClearance = 0.03f; // 启动离地校正后，车辆最低点与地面保留的额外间隙。
    public float inactivePushAssistSpeed = 1.8f; // 非激活静止车辆在被撞时附加的启动速度，帮助把碰撞力转成可见位移。
    public float inactivePushAssistMaxTargetSpeed = 4f; // 非激活车辆低于该速度时才使用启动辅助，避免高速碰撞被过度放大。
    public float inactivePushAssistImpulseThreshold = 75f; // 碰撞水平冲量低于该阈值时，不额外放大，避免轻微接触就乱动。
    public float inactivePushAssistTuningDuration = 0.35f; // 旧参数保留给 UI；当前主线不再依赖“低阻力窗口”来让车被撞动。
    public float contactImpulseTransferScale = 1.35f; // 前后向碰撞时，将纵向碰撞冲量换算成目标车纵向速度变化的倍率。
    public float contactPushVelocityStep = 0.28f; // 当前控制车贴住静止目标持续给油时，每个物理步补给目标车的纵向速度步进。
    public float contactStaticPushSpeedThreshold = 1.25f; // 两车都接近静止时，才启用贴住推车步进，避免中高速碰撞被额外放大。
    public float contactPushSpeedThreshold = 6f; // 只有在较低碰撞速度下才启用接触推车辅助。
    public float contactPushAlignmentThreshold = 0.35f; // 接触点与车辆前后方向的对齐阈值，避免侧擦时误判为推车。
    public float bumperZoneThreshold = 1.35f; // 接触点在车体前后向上的占比阈值，只有明显处于前后保险杠区域才触发推车辅助。

    private Rigidbody rb;
    private WheelCollider[] cachedWheelColliders;
    private WheelCollider[] steeringWheels;
    private WheelCollider[] driveWheels;
    private float[] defaultWheelDampingRates;
    private WheelFrictionCurve[] defaultForwardFrictions;
    private WheelFrictionCurve[] defaultSidewaysFrictions;
    private float normalDamping;
    private bool flipLogShown;
    private PhysicsMaterial runtimeBodyMaterial;
    private bool startPoseAdjusted;
    private Vector3 pendingInactiveVelocityChange;
    private float reverseLockTimer;
    private int lastThrottleDirection;

    public float CurrentSteerInput { get; private set; }
    public float CurrentThrottleInput { get; private set; }
    public float CurrentForwardSpeed { get; private set; }
    public bool IsFlipped => Vector3.Dot(transform.up, Vector3.up) < flippedDotThreshold;
    public Rigidbody CachedRigidbody => rb;
    public float SpeedMetersPerSecond => rb == null ? 0f : Vector3.ProjectOnPlane(GetLinearVelocity(), Vector3.up).magnitude;
    public float SpeedKilometersPerHour => SpeedMetersPerSecond * 3.6f;

    private void Awake()
    {
        if (!EnsureInitialized())
        {
            enabled = false;
        }
    }

    private bool EnsureInitialized()
    {
        if (rb != null)
        {
            return true;
        }

        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("SimpleCarController must be attached to the same GameObject as a Rigidbody. Put both on the bigyellowbee root object.");
            return false;
        }

        cachedWheelColliders = GetComponentsInChildren<WheelCollider>(true);
        CacheWheelDampingRates();
        CacheWheelFrictionCurves();
        // 当前项目优先复用资源自带轮子碰撞器，避免继续走“车身硬推”的漂浮手感。
        CacheWheelGroups();
        ApplyRuntimeRigidbodySettings();
        normalDamping = GetLinearDamping();
        rb.centerOfMass = centerOfMassOffset;
        ApplyRuntimeColliderMaterial();

        for (int i = 0; i < cachedWheelColliders.Length; i++)
        {
            cachedWheelColliders[i].enabled = true;
        }

        return true;
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        UpdateMotionState();
        bool useWheelDrive = HasWheelDrive();
        if (!useWheelDrive)
        {
            // 只有在没有可用 WheelCollider 时，才退回到简化刚体横向阻尼方案。
            ApplyLateralGrip();
        }

        bool applyInactiveTuning = !isControlled && useInactiveCollisionTuning;

        if (!isControlled)
        {
            CurrentThrottleInput = 0f;
            CurrentSteerInput = 0f;
            SetLinearDamping(applyInactiveTuning ? inactiveLinearDamping : normalDamping);
            ApplyWheelRollingResistance(applyInactiveTuning);
            ApplyWheelSurfaceGrip(applyInactiveTuning);
            ResetWheelDrive();
            ApplyInactivePushAssist();
            return;
        }

        ApplyWheelRollingResistance(false);
        ApplyWheelSurfaceGrip(false);

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            CurrentThrottleInput = 0f;
            CurrentSteerInput = 0f;
            SetLinearDamping(normalDamping);
            ResetWheelDrive();
            return;
        }

        CurrentThrottleInput = 0f;
        CurrentSteerInput = 0f;

        if (keyboard.wKey.isPressed)
        {
            CurrentThrottleInput += 1f;
        }

        if (keyboard.sKey.isPressed)
        {
            CurrentThrottleInput -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            CurrentSteerInput += 1f;
        }

        if (keyboard.aKey.isPressed)
        {
            CurrentSteerInput -= 1f;
        }

        bool brakePressed = keyboard.spaceKey.isPressed;
        UpdateReverseLockState();
        float targetDamping = brakePressed
            ? brakeDamping
            : Mathf.Abs(CurrentThrottleInput) > 0.01f
                ? drivingLinearDamping
                : normalDamping;
        SetLinearDamping(targetDamping);

        if (IsFlipped)
        {
            if (!flipLogShown)
            {
                Debug.Log("Vehicle flipped, press R to reset.");
                flipLogShown = true;
            }

            CurrentThrottleInput = 0f;
            CurrentSteerInput = 0f;
            ResetWheelDrive();
            return;
        }

        flipLogShown = false;

        float horizontalSpeed = Vector3.ProjectOnPlane(GetLinearVelocity(), Vector3.up).magnitude;
        if (useWheelDrive)
        {
            // 有现成轮子时，直接驱动轮子，比给整车硬推更接近车辆行为。
            ApplyWheelDrive(horizontalSpeed, brakePressed);
        }
        else
        {
            Vector3 driveForward = GetPlanarForward();

            if (Mathf.Abs(CurrentThrottleInput) > 0.01f && horizontalSpeed < maxSpeed)
            {
                float appliedAcceleration = horizontalSpeed < 1.5f ? startBoostAcceleration : motorAcceleration;
                rb.AddForce(driveForward * CurrentThrottleInput * appliedAcceleration, ForceMode.Acceleration);
            }

            float absForwardSpeed = Mathf.Abs(CurrentForwardSpeed);
            if (Mathf.Abs(CurrentSteerInput) > 0.01f && absForwardSpeed >= minTurnSpeed)
            {
                float speedFactor = Mathf.InverseLerp(minTurnSpeed, maxSpeed, absForwardSpeed);
                float steeringStrength = turnAcceleration * Mathf.Lerp(1f, highSpeedTurnFactor, speedFactor);
                float steeringDirection = CurrentForwardSpeed < -0.1f ? -1f : 1f;
                rb.AddTorque(Vector3.up * CurrentSteerInput * steeringDirection * steeringStrength, ForceMode.Acceleration);
            }

            ApplySteerDamping(absForwardSpeed);
        }
    }

    private void Start()
    {
        // 启动时按轮子/碰撞体最低点做一次离地校正，避免初始姿态直接穿进地面。
        TryLiftVehicleAboveGround();
    }

    public void SetControlled(bool controlled)
    {
        if (!EnsureInitialized())
        {
            return;
        }

        isControlled = controlled;

        if (!controlled)
        {
            CurrentThrottleInput = 0f;
            CurrentSteerInput = 0f;
            bool applyInactiveTuning = useInactiveCollisionTuning;
            SetLinearDamping(applyInactiveTuning ? inactiveLinearDamping : normalDamping);
            ApplyWheelRollingResistance(applyInactiveTuning);
            ApplyWheelSurfaceGrip(applyInactiveTuning);
            ResetWheelDrive();
            return;
        }

        SetLinearDamping(normalDamping);
        ApplyWheelRollingResistance(false);
        ApplyWheelSurfaceGrip(false);
    }

    public void RefreshCenterOfMass()
    {
        if (!EnsureInitialized())
        {
            return;
        }

        if (rb != null)
        {
            rb.centerOfMass = centerOfMassOffset;
        }
    }

    public void RefreshRuntimeConfiguration()
    {
        if (!EnsureInitialized())
        {
            return;
        }

        normalDamping = runtimeLinearDamping;
        rb.centerOfMass = centerOfMassOffset;

        if (applyRuntimeRigidbodySettings)
        {
            SetAngularDamping(runtimeAngularDamping);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        ApplyRuntimeColliderMaterial();
        bool applyInactiveTuning = !isControlled && useInactiveCollisionTuning;
        ApplyWheelRollingResistance(applyInactiveTuning);
        ApplyWheelSurfaceGrip(applyInactiveTuning);

        if (!isControlled)
        {
            SetLinearDamping(applyInactiveTuning ? inactiveLinearDamping : normalDamping);
            return;
        }

        float targetDamping = Mathf.Abs(CurrentThrottleInput) > 0.01f ? drivingLinearDamping : normalDamping;
        SetLinearDamping(targetDamping);
    }

    public void QueueInactivePush(Vector3 pushDirection, float pushSpeed)
    {
        if (isControlled || rb == null || pushDirection.sqrMagnitude < 0.0001f || pushSpeed <= 0f)
        {
            return;
        }

        pendingInactiveVelocityChange += pushDirection.normalized * pushSpeed;
    }

    public void CopyTuningFrom(SimpleCarController source)
    {
        if (source == null || source == this)
        {
            return;
        }

        if (!EnsureInitialized() || !source.EnsureInitialized())
        {
            return;
        }

        motorAcceleration = source.motorAcceleration;
        startBoostAcceleration = source.startBoostAcceleration;
        turnAcceleration = source.turnAcceleration;
        highSpeedTurnFactor = source.highSpeedTurnFactor;
        brakeDamping = source.brakeDamping;
        maxSpeed = source.maxSpeed;
        lateralGrip = source.lateralGrip;
        minTurnSpeed = source.minTurnSpeed;
        steerAngularDamping = source.steerAngularDamping;
        flippedDotThreshold = source.flippedDotThreshold;
        centerOfMassOffset = source.centerOfMassOffset;
        disableChildWheelColliders = source.disableChildWheelColliders;
        applyRuntimeRigidbodySettings = source.applyRuntimeRigidbodySettings;
        drivingLinearDamping = source.drivingLinearDamping;
        runtimeLinearDamping = source.runtimeLinearDamping;
        runtimeAngularDamping = source.runtimeAngularDamping;
        useInactiveCollisionTuning = source.useInactiveCollisionTuning;
        wheelDampingRateScale = source.wheelDampingRateScale;
        inactiveLinearDamping = source.inactiveLinearDamping;
        inactiveWheelDampingRate = source.inactiveWheelDampingRate;
        inactiveForwardFrictionStiffness = source.inactiveForwardFrictionStiffness;
        inactiveSidewaysFrictionStiffness = source.inactiveSidewaysFrictionStiffness;
        wheelTorqueScale = source.wheelTorqueScale;
        reverseAccelerationScale = source.reverseAccelerationScale;
        brakeTorqueScale = source.brakeTorqueScale;
        coastBrakeTorque = source.coastBrakeTorque;
        coastBrakeStartSpeed = source.coastBrakeStartSpeed;
        coastBrakeFullSpeed = source.coastBrakeFullSpeed;
        coastingWheelSyncBrakeTorque = source.coastingWheelSyncBrakeTorque;
        coastingWheelSyncRpmTolerance = source.coastingWheelSyncRpmTolerance;
        coastingWheelSyncMinSpeed = source.coastingWheelSyncMinSpeed;
        lowSpeedCoastThreshold = source.lowSpeedCoastThreshold;
        lowSpeedCoastBrakeTorque = source.lowSpeedCoastBrakeTorque;
        directionChangeSpeedThreshold = source.directionChangeSpeedThreshold;
        directionChangeBrakeTorque = source.directionChangeBrakeTorque;
        stationaryReverseRpmThreshold = source.stationaryReverseRpmThreshold;
        stationaryReverseBrakeTorque = source.stationaryReverseBrakeTorque;
        holdBrakeAtIdle = source.holdBrakeAtIdle;
        applyLowFrictionMaterial = source.applyLowFrictionMaterial;
        bodyStaticFriction = source.bodyStaticFriction;
        bodyDynamicFriction = source.bodyDynamicFriction;
        liftVehicleAboveGroundOnStart = source.liftVehicleAboveGroundOnStart;
        startGroundClearance = source.startGroundClearance;
        inactivePushAssistSpeed = source.inactivePushAssistSpeed;
        inactivePushAssistMaxTargetSpeed = source.inactivePushAssistMaxTargetSpeed;
        inactivePushAssistImpulseThreshold = source.inactivePushAssistImpulseThreshold;
        inactivePushAssistTuningDuration = source.inactivePushAssistTuningDuration;
        contactImpulseTransferScale = source.contactImpulseTransferScale;
        contactPushVelocityStep = source.contactPushVelocityStep;
        contactStaticPushSpeedThreshold = source.contactStaticPushSpeedThreshold;
        contactPushSpeedThreshold = source.contactPushSpeedThreshold;
        contactPushAlignmentThreshold = source.contactPushAlignmentThreshold;
        bumperZoneThreshold = source.bumperZoneThreshold;

        Rigidbody sourceRb = source.CachedRigidbody != null ? source.CachedRigidbody : source.GetComponent<Rigidbody>();
        Rigidbody targetRb = CachedRigidbody != null ? CachedRigidbody : GetComponent<Rigidbody>();
        if (sourceRb != null && targetRb != null)
        {
            targetRb.mass = sourceRb.mass;
            targetRb.interpolation = sourceRb.interpolation;
            targetRb.collisionDetectionMode = sourceRb.collisionDetectionMode;
            SetLinearDamping(source.GetLinearDamping());
            SetAngularDamping(sourceRb.angularDamping);
        }

        RefreshRuntimeConfiguration();
    }

    private void UpdateMotionState()
    {
        CurrentForwardSpeed = Vector3.Dot(GetLinearVelocity(), GetPlanarForward());
    }

    private bool HasWheelDrive()
    {
        return cachedWheelColliders != null && cachedWheelColliders.Length >= 4 && driveWheels != null && driveWheels.Length > 0 && steeringWheels != null && steeringWheels.Length > 0;
    }

    private void CacheWheelDampingRates()
    {
        if (cachedWheelColliders == null)
        {
            defaultWheelDampingRates = System.Array.Empty<float>();
            return;
        }

        defaultWheelDampingRates = new float[cachedWheelColliders.Length];
        for (int i = 0; i < cachedWheelColliders.Length; i++)
        {
            defaultWheelDampingRates[i] = cachedWheelColliders[i] != null ? cachedWheelColliders[i].wheelDampingRate : 0f;
        }
    }

    private void CacheWheelFrictionCurves()
    {
        if (cachedWheelColliders == null)
        {
            defaultForwardFrictions = System.Array.Empty<WheelFrictionCurve>();
            defaultSidewaysFrictions = System.Array.Empty<WheelFrictionCurve>();
            return;
        }

        defaultForwardFrictions = new WheelFrictionCurve[cachedWheelColliders.Length];
        defaultSidewaysFrictions = new WheelFrictionCurve[cachedWheelColliders.Length];

        for (int i = 0; i < cachedWheelColliders.Length; i++)
        {
            if (cachedWheelColliders[i] == null)
            {
                continue;
            }

            defaultForwardFrictions[i] = cachedWheelColliders[i].forwardFriction;
            defaultSidewaysFrictions[i] = cachedWheelColliders[i].sidewaysFriction;
        }
    }

    private void CacheWheelGroups()
    {
        if (cachedWheelColliders == null || cachedWheelColliders.Length == 0)
        {
            steeringWheels = System.Array.Empty<WheelCollider>();
            driveWheels = System.Array.Empty<WheelCollider>();
            return;
        }

        System.Collections.Generic.List<WheelCollider> front = new System.Collections.Generic.List<WheelCollider>();
        System.Collections.Generic.List<WheelCollider> rear = new System.Collections.Generic.List<WheelCollider>();

        for (int i = 0; i < cachedWheelColliders.Length; i++)
        {
            WheelCollider wheel = cachedWheelColliders[i];
            if (wheel == null)
            {
                continue;
            }

            if (wheel.transform.localPosition.z >= 0f)
            {
                // 简化约定：局部 z >= 0 视为前轮，负责转向。
                front.Add(wheel);
            }
            else
            {
                // 局部 z < 0 视为后轮，优先作为驱动轮。
                rear.Add(wheel);
            }
        }

        steeringWheels = front.Count > 0 ? front.ToArray() : cachedWheelColliders;
        driveWheels = rear.Count > 0 ? rear.ToArray() : cachedWheelColliders;
    }

    private void ApplyWheelDrive(float horizontalSpeed, bool brakePressed)
    {
        float absForwardSpeed = Mathf.Abs(CurrentForwardSpeed);
        float speedFactor = Mathf.InverseLerp(minTurnSpeed, maxSpeed, absForwardSpeed);
        // 这里不是做真实转向几何，而是课程级的简化前轮转向：低速角度更大，高速自动收一点。
        float steerAngle = turnAcceleration * Mathf.Lerp(8f, 4f * highSpeedTurnFactor, speedFactor) * CurrentSteerInput;
        float motorScale = horizontalSpeed < 2.5f ? startBoostAcceleration : motorAcceleration;
        float directionScale = CurrentThrottleInput < 0f ? reverseAccelerationScale : 1f;
        float motorTorque = CurrentThrottleInput * motorScale * wheelTorqueScale * directionScale;
        float brakeTorque = brakePressed ? brakeDamping * brakeTorqueScale : 0f;
        float averageDriveWheelRpm = GetAverageDriveWheelRpm();
        bool isChangingDirection =
            Mathf.Abs(CurrentThrottleInput) > 0.01f &&
            absForwardSpeed > directionChangeSpeedThreshold &&
            Mathf.Sign(CurrentThrottleInput) != Mathf.Sign(CurrentForwardSpeed);
        bool isReversingWheelSpinWhileBlocked =
            Mathf.Abs(CurrentThrottleInput) > 0.01f &&
            absForwardSpeed < 0.35f &&
            Mathf.Abs(averageDriveWheelRpm) > stationaryReverseRpmThreshold &&
            Mathf.Sign(CurrentThrottleInput) != Mathf.Sign(averageDriveWheelRpm);

        if (isChangingDirection)
        {
            motorTorque = 0f;
            brakeTorque = Mathf.Max(brakeTorque, directionChangeBrakeTorque);
        }
        else if (reverseLockTimer > 0f)
        {
            motorTorque = 0f;
            brakeTorque = Mathf.Max(brakeTorque, stationaryReverseBrakeTorque);
        }
        else if (isReversingWheelSpinWhileBlocked)
        {
            // 车身几乎没动，但轮子还在按旧方向空转时，先刹掉轮速，再允许反向给扭矩。
            motorTorque = 0f;
            brakeTorque = Mathf.Max(brakeTorque, stationaryReverseBrakeTorque);
        }

        if (!brakePressed && Mathf.Abs(CurrentThrottleInput) < 0.01f && absForwardSpeed > coastBrakeStartSpeed)
        {
            // 空挡阻力随速度逐步增强：低速不生硬，高速也不会像完全无阻力一样继续蹿。
            float coastFactor = Mathf.InverseLerp(coastBrakeStartSpeed, Mathf.Max(coastBrakeFullSpeed, coastBrakeStartSpeed + 0.01f), absForwardSpeed);
            brakeTorque = Mathf.Max(brakeTorque, coastBrakeTorque * coastFactor);
        }

        if (holdBrakeAtIdle && Mathf.Abs(CurrentThrottleInput) < 0.01f && absForwardSpeed < minTurnSpeed)
        {
            // 如需停车后更快收住，可打开该选项；当前默认关闭，避免出现“突然被踩一脚刹车”的感觉。
            brakeTorque = Mathf.Max(brakeTorque, brakeDamping * 150f);
        }

        float syncBrakeTorque = GetCoastingWheelSyncBrakeTorque(brakePressed, absForwardSpeed, averageDriveWheelRpm);
        if (syncBrakeTorque > 0f)
        {
            // 松开油门后如果轮子仍明显快于车速，对同步刹车做比例控制，避免过晚介入或低速阶段突然补重刹。
            brakeTorque = Mathf.Max(brakeTorque, syncBrakeTorque);
        }

        if (!brakePressed && Mathf.Abs(CurrentThrottleInput) < 0.01f && absForwardSpeed < lowSpeedCoastThreshold)
        {
            float tailBrakeFactor = 1f - Mathf.Clamp01(absForwardSpeed / Mathf.Max(lowSpeedCoastThreshold, 0.01f));
            brakeTorque = Mathf.Max(brakeTorque, lowSpeedCoastBrakeTorque * tailBrakeFactor);
        }

        for (int i = 0; i < steeringWheels.Length; i++)
        {
            if (steeringWheels[i] == null)
            {
                continue;
            }

            steeringWheels[i].steerAngle = steerAngle;
            steeringWheels[i].brakeTorque = brakeTorque;
        }

        for (int i = 0; i < driveWheels.Length; i++)
        {
            if (driveWheels[i] == null)
            {
                continue;
            }

            driveWheels[i].motorTorque = horizontalSpeed < maxSpeed ? motorTorque : 0f;
            driveWheels[i].brakeTorque = brakeTorque;
        }
    }

    private float GetAverageDriveWheelRpm()
    {
        if (driveWheels == null || driveWheels.Length == 0)
        {
            return 0f;
        }

        float rpmSum = 0f;
        int validCount = 0;
        for (int i = 0; i < driveWheels.Length; i++)
        {
            if (driveWheels[i] == null)
            {
                continue;
            }

            rpmSum += driveWheels[i].rpm;
            validCount++;
        }

        return validCount > 0 ? rpmSum / validCount : 0f;
    }

    private float GetCoastingWheelSyncBrakeTorque(bool brakePressed, float absForwardSpeed, float averageDriveWheelRpm)
    {
        if (brakePressed || Mathf.Abs(CurrentThrottleInput) > 0.01f || absForwardSpeed < coastingWheelSyncMinSpeed)
        {
            return 0f;
        }

        float expectedRollingRpm = GetExpectedRollingRpm(absForwardSpeed);
        float rpmOverspeed = Mathf.Abs(averageDriveWheelRpm) - expectedRollingRpm - coastingWheelSyncRpmTolerance;
        if (rpmOverspeed <= 0f)
        {
            return 0f;
        }

        float syncFactor = Mathf.Clamp01(rpmOverspeed / Mathf.Max(coastingWheelSyncRpmTolerance * 3f, 1f));
        return coastingWheelSyncBrakeTorque * syncFactor;
    }

    private float GetExpectedRollingRpm(float absForwardSpeed)
    {
        if (driveWheels == null || driveWheels.Length == 0)
        {
            return 0f;
        }

        float radiusSum = 0f;
        int validCount = 0;
        for (int i = 0; i < driveWheels.Length; i++)
        {
            if (driveWheels[i] == null || driveWheels[i].radius <= 0.001f)
            {
                continue;
            }

            radiusSum += driveWheels[i].radius;
            validCount++;
        }

        if (validCount == 0)
        {
            return 0f;
        }

        float averageRadius = radiusSum / validCount;
        float wheelCircumference = 2f * Mathf.PI * averageRadius;
        return wheelCircumference > 0.0001f ? (absForwardSpeed / wheelCircumference) * 60f : 0f;
    }

    private void ApplyInactivePushAssist()
    {
        if (rb == null || pendingInactiveVelocityChange.sqrMagnitude <= 0.000001f)
        {
            pendingInactiveVelocityChange = Vector3.zero;
            return;
        }

        float horizontalSpeed = Vector3.ProjectOnPlane(GetLinearVelocity(), Vector3.up).magnitude;
        if (horizontalSpeed < inactivePushAssistMaxTargetSpeed)
        {
            rb.AddForce(pendingInactiveVelocityChange, ForceMode.VelocityChange);
            rb.WakeUp();
        }

        pendingInactiveVelocityChange = Vector3.zero;
    }

    private void UpdateReverseLockState()
    {
        if (reverseLockTimer > 0f)
        {
            reverseLockTimer = Mathf.Max(0f, reverseLockTimer - Time.fixedDeltaTime);
        }

        int currentDirection = 0;
        if (CurrentThrottleInput > 0.01f)
        {
            currentDirection = 1;
        }
        else if (CurrentThrottleInput < -0.01f)
        {
            currentDirection = -1;
        }

        float absForwardSpeed = Mathf.Abs(CurrentForwardSpeed);
        if (currentDirection != 0 &&
            lastThrottleDirection != 0 &&
            currentDirection != lastThrottleDirection &&
            absForwardSpeed < 0.35f)
        {
            reverseLockTimer = stationaryReverseLockTime;
        }

        if (currentDirection != 0)
        {
            lastThrottleDirection = currentDirection;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (rb == null || !isControlled)
        {
            return;
        }

        ApplyControlledContactPush(collision);
    }

    private void ApplyControlledContactPush(Collision collision)
    {
        if (Mathf.Abs(CurrentThrottleInput) < 0.01f)
        {
            return;
        }

        float ownSpeed = Vector3.ProjectOnPlane(GetLinearVelocity(), Vector3.up).magnitude;
        if (ownSpeed > contactPushSpeedThreshold)
        {
            return;
        }

        Rigidbody otherRb = collision.rigidbody;
        if (otherRb == null || otherRb == rb || otherRb.isKinematic)
        {
            return;
        }

        Vector3 ownPushDirection = GetPlanarForward() * Mathf.Sign(CurrentThrottleInput);
        if (ownPushDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        if (!TryGetLongitudinalPushDirection(collision, otherRb, out Vector3 targetPushDirection))
        {
            return;
        }

        if (Vector3.Dot(ownPushDirection.normalized, targetPushDirection.normalized) < contactPushAlignmentThreshold)
        {
            return;
        }

        float otherSpeed = GetHorizontalSpeed(otherRb);
        if (otherSpeed > inactivePushAssistMaxTargetSpeed)
        {
            return;
        }

        Vector3 planarImpulse = Vector3.ProjectOnPlane(collision.impulse, Vector3.up);
        float longitudinalImpulse = Mathf.Abs(Vector3.Dot(planarImpulse, targetPushDirection));
        float impulsePushSpeed = 0f;
        if (longitudinalImpulse >= inactivePushAssistImpulseThreshold)
        {
            impulsePushSpeed = Mathf.Min(
                inactivePushAssistSpeed,
                (longitudinalImpulse / Mathf.Max(otherRb.mass, 0.01f)) * contactImpulseTransferScale);
        }

        float sustainedPushSpeed = 0f;
        if (ownSpeed < contactStaticPushSpeedThreshold && otherSpeed < contactStaticPushSpeedThreshold)
        {
            sustainedPushSpeed = Mathf.Abs(CurrentThrottleInput) * contactPushVelocityStep;
        }

        float pushSpeed = Mathf.Max(impulsePushSpeed, sustainedPushSpeed);
        if (pushSpeed <= 0.0001f)
        {
            return;
        }

        SimpleCarController otherController = otherRb.GetComponent<SimpleCarController>();
        if (otherController != null)
        {
            otherController.QueueInactivePush(targetPushDirection, pushSpeed);
        }
        else
        {
            otherRb.AddForce(targetPushDirection * pushSpeed, ForceMode.VelocityChange);
            otherRb.WakeUp();
        }
    }

    private bool TryGetLongitudinalPushDirection(Collision collision, Rigidbody targetRigidbody, out Vector3 pushDirection)
    {
        pushDirection = Vector3.zero;
        if (collision == null || targetRigidbody == null)
        {
            return false;
        }

        Vector3 targetForward = Vector3.ProjectOnPlane(targetRigidbody.transform.forward, Vector3.up);
        if (targetForward.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        targetForward.Normalize();

        ContactPoint[] contacts = collision.contacts;
        if (contacts == null || contacts.Length == 0)
        {
            return false;
        }

        float averageLocalZ = 0f;
        float averageAbsZ = 0f;
        float averageAbsX = 0f;
        int validCount = 0;

        for (int i = 0; i < contacts.Length; i++)
        {
            Vector3 localContact = targetRigidbody.transform.InverseTransformPoint(contacts[i].point);
            averageLocalZ += localContact.z;
            averageAbsZ += Mathf.Abs(localContact.z);
            averageAbsX += Mathf.Abs(localContact.x);
            validCount++;
        }

        if (validCount == 0)
        {
            return false;
        }

        averageLocalZ /= validCount;
        averageAbsZ /= validCount;
        averageAbsX /= validCount;

        if (averageAbsZ <= averageAbsX * bumperZoneThreshold)
        {
            // 接触主要发生在侧门区域，不触发前后向推车辅助。
            return false;
        }

        pushDirection = averageLocalZ < 0f ? targetForward : -targetForward;
        return pushDirection.sqrMagnitude > 0.0001f;
    }

    private void ResetWheelDrive()
    {
        if (cachedWheelColliders == null)
        {
            return;
        }

        for (int i = 0; i < cachedWheelColliders.Length; i++)
        {
            if (cachedWheelColliders[i] == null)
            {
                continue;
            }

            cachedWheelColliders[i].motorTorque = 0f;
            cachedWheelColliders[i].steerAngle = 0f;
            cachedWheelColliders[i].brakeTorque = 0f;
        }
    }

    private void ApplyWheelRollingResistance(bool inactive)
    {
        if (cachedWheelColliders == null || defaultWheelDampingRates == null)
        {
            return;
        }

        for (int i = 0; i < cachedWheelColliders.Length; i++)
        {
            if (cachedWheelColliders[i] == null)
            {
                continue;
            }

            cachedWheelColliders[i].wheelDampingRate = inactive
                ? inactiveWheelDampingRate
                : defaultWheelDampingRates[i] * wheelDampingRateScale;
        }
    }

    private void ApplyWheelSurfaceGrip(bool inactive)
    {
        if (cachedWheelColliders == null || defaultForwardFrictions == null || defaultSidewaysFrictions == null)
        {
            return;
        }

        for (int i = 0; i < cachedWheelColliders.Length; i++)
        {
            WheelCollider wheel = cachedWheelColliders[i];
            if (wheel == null)
            {
                continue;
            }

            WheelFrictionCurve forward = defaultForwardFrictions[i];
            WheelFrictionCurve sideways = defaultSidewaysFrictions[i];

            forward.stiffness = inactive ? defaultForwardFrictions[i].stiffness * inactiveForwardFrictionStiffness : defaultForwardFrictions[i].stiffness;
            sideways.stiffness = inactive ? defaultSidewaysFrictions[i].stiffness * inactiveSidewaysFrictionStiffness : defaultSidewaysFrictions[i].stiffness;

            wheel.forwardFriction = forward;
            wheel.sidewaysFriction = sideways;
        }
    }

    private void ApplyLateralGrip()
    {
        if (rb == null || IsFlipped)
        {
            return;
        }

        Vector3 localVelocity = transform.InverseTransformDirection(GetLinearVelocity());
        Vector3 lateralVelocity = transform.right * localVelocity.x;
        rb.AddForce(-lateralVelocity * lateralGrip, ForceMode.Acceleration);
    }

    private void ApplySteerDamping(float absForwardSpeed)
    {
        Vector3 angularVelocity = rb.angularVelocity;
        float targetYaw = angularVelocity.y;

        if (absForwardSpeed < minTurnSpeed)
        {
            targetYaw = Mathf.Lerp(angularVelocity.y, 0f, steerAngularDamping * Time.fixedDeltaTime);
        }

        rb.angularVelocity = new Vector3(angularVelocity.x, targetYaw, angularVelocity.z);
    }

    private Vector3 GetPlanarForward()
    {
        Vector3 planarForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (planarForward.sqrMagnitude < 0.001f)
        {
            return Vector3.forward;
        }

        return planarForward.normalized;
    }

    private void ApplyRuntimeRigidbodySettings()
    {
        if (!applyRuntimeRigidbodySettings || rb == null)
        {
            return;
        }

        SetLinearDamping(runtimeLinearDamping);
        SetAngularDamping(runtimeAngularDamping);
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void ApplyRuntimeColliderMaterial()
    {
        if (!applyLowFrictionMaterial)
        {
            return;
        }

        // 只降低车身碰撞体与地面的摩擦，WheelCollider 本身的轮胎摩擦仍由 Unity 轮子模型处理。
        runtimeBodyMaterial = new PhysicsMaterial("RuntimeVehicleBodyLowFriction")
        {
            staticFriction = bodyStaticFriction,
            dynamicFriction = bodyDynamicFriction,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].isTrigger || colliders[i] is WheelCollider)
            {
                continue;
            }

            colliders[i].material = runtimeBodyMaterial;
        }
    }

    private void TryLiftVehicleAboveGround()
    {
        if (!liftVehicleAboveGroundOnStart || startPoseAdjusted)
        {
            return;
        }

        startPoseAdjusted = true;

        float localLowestPoint = float.PositiveInfinity;

        for (int i = 0; i < cachedWheelColliders.Length; i++)
        {
            WheelCollider wheel = cachedWheelColliders[i];
            if (wheel == null)
            {
                continue;
            }

            float wheelBottom = wheel.transform.localPosition.y + wheel.center.y - wheel.radius;
            if (wheelBottom < localLowestPoint)
            {
                localLowestPoint = wheelBottom;
            }
        }

        if (float.IsPositiveInfinity(localLowestPoint))
        {
            // 如果当前车没有可用轮子，就退回到普通碰撞体最低点作为参考。
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null || colliders[i].isTrigger || colliders[i] is WheelCollider)
                {
                    continue;
                }

                Bounds bounds = colliders[i].bounds;
                float localBottom = transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z)).y;
                if (localBottom < localLowestPoint)
                {
                    localLowestPoint = localBottom;
                }
            }
        }

        if (float.IsPositiveInfinity(localLowestPoint))
        {
            return;
        }

        Vector3 rayOrigin = transform.position + Vector3.up * 5f;
        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        float desiredY = hit.point.y - localLowestPoint + startGroundClearance;
        if (transform.position.y < desiredY)
        {
            Vector3 liftedPosition = transform.position;
            liftedPosition.y = desiredY;
            transform.position = liftedPosition;
            rb.position = liftedPosition;
            rb.Sleep();
            rb.WakeUp();
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

    private static Vector3 GetLinearVelocity(Rigidbody targetRigidbody)
    {
#if UNITY_6000_0_OR_NEWER
        return targetRigidbody.linearVelocity;
#else
        return targetRigidbody.velocity;
#endif
    }

    private static float GetHorizontalSpeed(Rigidbody targetRigidbody)
    {
        return Vector3.ProjectOnPlane(GetLinearVelocity(targetRigidbody), Vector3.up).magnitude;
    }

    private float GetLinearDamping()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearDamping;
#else
        return rb.drag;
#endif
    }

    private void SetLinearDamping(float value)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = value;
#else
        rb.drag = value;
#endif
    }

    private void SetAngularDamping(float value)
    {
#if UNITY_6000_0_OR_NEWER
        rb.angularDamping = value;
#else
        rb.angularDrag = value;
#endif
    }
}

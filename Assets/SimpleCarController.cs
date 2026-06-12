using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Text;
using System.IO;

public class SimpleCarController : MonoBehaviour
{
    private const string CurrentBuildTagValue = "responsive_drive_2026_06_12_v15";
    private const float CurrentMotorAccelerationValue = 6400f;
    private const float CurrentStartBoostAccelerationValue = 8400f;
    private const float CurrentWheelTorqueScaleValue = 1.4f;
    private const float CurrentCoastingDecelerationLowSpeedValue = 0.24f;
    private const float CurrentCoastingDecelerationHighSpeedValue = 0.72f;
    private const float CurrentCoastingDecelerationBlendSpeedValue = 30f;
    private const float CurrentCenterOfMassYValue = -0.05f;
    private const float CurrentRuntimeAngularDampingValue = 1.6f;
    private const bool CurrentUseInactiveCollisionTuningValue = true;
    private const float CurrentInactivePushAssistSpeedValue = 8f;
    private const float CurrentInactivePushAssistMaxTargetSpeedValue = 22f;
    private const float CurrentInactivePushAssistImpulseThresholdValue = 4f;
    private const float CurrentInactivePushAssistTuningDurationValue = 1f;
    private const float CurrentContactImpulseTransferScaleValue = 7f;
    private const float CurrentContactPushVelocityStepValue = 2.8f;
    private const float CurrentContactStaticPushSpeedThresholdValue = 3.5f;
    private const float CurrentContactMinimumPushSpeedValue = 3.5f;
    private const float CurrentContactImmediateVelocitySeedValue = 4f;
    private const float CurrentContactPushSpeedThresholdValue = 30f;
    private const float CurrentContactPushAlignmentThresholdValue = 0.2f;
    private const float CurrentBumperZoneThresholdValue = 0.8f;
    private const float CurrentDirectionChangeSpeedThresholdValue = 4f;
    private const float CurrentStationaryReverseRpmThresholdValue = 260f;
    private const float CurrentStationaryReverseBrakeTorqueValue = 0f;
    private const float CurrentStationaryReverseLockTimeValue = 0f;
    private const float CurrentCollisionReverseAssistBrakeTorqueValue = 2800f;
    private const float CurrentCollisionReverseAssistLockTimeValue = 0.03f;
    private const float CurrentCollisionReverseBodyAssistAccelerationValue = 16f;
    private const float CurrentCollisionReverseBodyAssistSpeedThresholdValue = 3f;
    private const float CurrentCollisionReverseBodyVelocitySeedValue = 1.2f;
    private const float CurrentBlockedReverseWheelRpmCancelThresholdValue = 80f;
    private const float CurrentResponsiveBodyDriveAccelerationValue = 22f;
    private const float CurrentResponsiveBodyDriveSpeedThresholdValue = 4f;
    private const float CurrentResponsiveBodyDriveVelocitySeedValue = 1.2f;
    private const float CurrentDirectionChangeDriveBoostValue = 1.6f;
    private const float CurrentContactPushAccelerationValue = 18f;
    private const float CurrentContactSelfFollowSpeedRatioValue = 0.8f;

    public bool isControlled = false; // 当前这辆车是否接收玩家输入。
    public string debugBuildTag = CurrentBuildTagValue; // 仅用于在 Inspector 中确认当前场景实例是否真的吃到了这轮修改。
    public float motorAcceleration = 6400f; // 车辆已经滚起来之后的正常驱动力；本轮按用户要求直接翻倍。
    public float startBoostAcceleration = 8400f; // 低速起步时的额外驱动力；本轮按用户要求直接翻倍。
    public float turnAcceleration = 2.5f; // 基础转向强度；在轮驱动模式下会换算成前轮转角。
    public float highSpeedTurnFactor = 0.45f; // 车速升高后用于削弱转向能力的系数。
    public float brakeDamping = 4.5f; // 按下 Space 或需要快速收住车身时使用的附加阻尼/制动力度。
    public float maxSpeed = 72f; // 软速度上限，超过后不再继续施加额外驱动力或驱动扭矩。
    public float lateralGrip = 8f; // 备用刚体驱动模式下用于抑制横向侧滑的力度。
    public float minTurnSpeed = 2.5f; // 备用刚体驱动模式下，达到该前后速度后才允许明显转向。
    public float steerAngularDamping = 6f; // 备用刚体驱动模式下用于压制低速原地乱扭的偏航阻尼。
    public float flippedDotThreshold = 0.55f; // 车身朝上程度低于该阈值时，判定为翻车并停止驱动输入。
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.05f, 0f); // 局部重心偏移；继续抬高重心，减少高速侧撞后像浮漂一样被硬拉回正。
    public bool disableChildWheelColliders = false; // 旧调试开关；当前方案保持 WheelCollider 启用。
    public bool applyRuntimeRigidbodySettings = true; // 是否在运行时覆盖部分 Rigidbody 阻尼和插值设置。
    public float drivingLinearDamping = 0.015f; // 持续给油时使用的线性阻尼，应尽量小，避免前进动力被阻尼吃掉。
    public float runtimeLinearDamping = 0.005f; // 松开油门后的基础空气阻力；进一步压低，避免车身阻尼本身就做出“像踩刹车”的效果。
    public float coastingDampingStartSpeed = 4f; // 松油后达到该速度开始追加更明显的线性阻尼，避免中高速阶段假匀速滑太久。
    public float coastingDampingFullSpeed = 9f; // 松油后达到该速度时，额外线性阻尼增益达到满值。
    public float coastingDampingBoost = 0f; // 旧参数保留；当前不再依赖速度分层线性阻尼解决滑行问题，避免 20 km/h 平台和脚刹感。
    public float coastingDecelerationLowSpeed = 0.24f; // 空挡滑行时的低速基础减速度；在不引回“突然一脚刹车”的前提下，再稍微加大一点尾速收口。
    public float coastingDecelerationHighSpeed = 0.72f; // 空挡滑行时的高速基础减速度；小幅提高，让中高速松油更有自然减速感，但峰值仍低于 1m/s²。
    public float coastingDecelerationBlendSpeed = 30f; // 空挡滑行减速度的过渡速度；速度越高减速越明显，速度越低减速越弱。
    public bool debugCoastingAudit = false; // 是否开启空挡滑行来源审计日志。
    public float debugCoastingAuditInterval = 0.2f; // 空挡滑行审计日志的输出间隔，避免每帧刷屏。
    public bool debugCoastingAuditWriteToFile = true; // 是否把空挡滑行审计日志同步写入文本文件，方便完整保存。
    public float runtimeAngularDamping = 1.6f; // 车辆旋转时的基础角阻尼；继续降低，让侧撞后的翻滚趋势更容易保留下来。
    public bool useInactiveCollisionTuning = true; // 是否对非当前控制车辆启用更低阻力碰撞展示参数；演示档开启，避免目标车像焊死在地上。
    public float inactiveLinearDamping = 0.02f; // 非激活车辆使用的较低线性阻尼，兼顾被撞动和不过分打滑。
    public float wheelColliderMass = 8f; // 轮子等效质量；适度降低，减轻“轮子像巨大飞轮一样难起转、难反转、松油后还继续推车”的感觉。
    public float wheelDampingRateScale = 0.35f; // 当前主线车辆的轮子滚动阻力缩放，降低后可减少“松油马上被拖死”和碰撞后过快耗能。
    public float coastingWheelDampingScale = 1f; // 松油时驱动轮额外滚动阻力倍率；当前恢复为 1，避免轮子滚阻把滑行手感拖得像踩刹车。
    public float inactiveWheelDampingRate = 0.01f; // 非激活车辆轮子的滚动阻力，适度降低，避免像手刹锁死。
    public float inactiveForwardFrictionStiffness = 0.08f; // 非激活车辆前后向轮胎抓地力倍率，低于默认值但不至于完全打滑。
    public float inactiveSidewaysFrictionStiffness = 0.35f; // 非激活车辆侧向轮胎抓地力倍率，保留明显侧向阻力，避免侧门轻易被推走。
    public float wheelTorqueScale = 1.4f; // 轮驱动模式下的扭矩倍率；小幅上调，让当前动力参数体感更直接。
    public float reverseAccelerationScale = 0.4f; // 倒车驱动力倍率，通常应明显低于前进驱动力。
    public float brakeTorqueScale = 160f; // 刹车扭矩倍率，用来控制按下 Space 后的实际制动强度。
    public float coastBrakeTorque = 0f; // 旧参数保留；当前主线不再依赖轮上刹车模拟滑行，避免低速阶段突兀重刹。
    public float coastBrakeStartSpeed = 2.5f; // 低于该速度时不再施加空挡轮上阻力，避免 1 m/s 左右突然像重刹。
    public float coastBrakeFullSpeed = 10f; // 达到该速度后，空挡轮上阻力增长到设定上限。
    public float coastingMotorDragTorque = 0f; // 松开油门后的基础发动机拖拽扭矩；当前设为 0，避免一松油就被固定扭矩猛拉一截。
    public float coastingMotorDragRpmFactor = 0f; // 当前关闭基于轮速超差的额外反向拖拽，避免继续引入“松油像踩刹车”或平台速度。
    public float coastingWheelSyncBrakeTorque = 0f; // 当前关闭空挡轮速同步刹车，避免松油阶段被 brakeTorque 主导。
    public float coastingWheelSyncRpmTolerance = 30f; // 旧参数保留；当前同步刹车关闭后只作为容差占位。
    public float coastingWheelSyncMinSpeed = 2f; // 只在中高速阶段才允许同步刹车介入，避免临近低速时突然被砍一截。
    public float coastingOppositeSpinBrakeTorque = 220f; // 松油时如果驱动轮转速方向与车身前进方向相反，则用小刹车把异常反号轮速压掉，避免再次出现“松油继续加速”。
    public float coastingOppositeSpinRpmThreshold = 120f; // 只有反号轮速达到该阈值后才介入，避免正常细小抖动被误判。
    public float lowSpeedCoastThreshold = 2.2f; // 低于该速度后开始补一点尾速收口，避免低速滑太久停不下来。
    public float lowSpeedCoastBrakeTorque = 0f; // 低速空挡尾速收口使用的小刹车扭矩；默认关闭，避免再次出现“低速突然一脚刹车”。
    public float directionChangeSpeedThreshold = 4f; // 低于该速度时，W/S 换向不再先刹停，而是立刻给新方向驱动。
    public float directionChangeBrakeTorque = 2500f; // 旧调试参数保留；当前 W/S 换向不再靠清空扭矩再刹停。
    public float directionChangeDriveBoost = 1.6f; // 前进中立刻按 S 或倒车中立刻按 W 时，对新方向扭矩做短促增强。
    public float stationaryReverseRpmThreshold = 260f; // 旧调试参数保留；当前不再用驱动轮 rpm 阻塞 W/S 换向。
    public float stationaryReverseBrakeTorque = 0f; // 静止受阻换向不再补刹车，避免抵墙/顶车后 S/W 需要先等旧轮速卸完。
    public float stationaryReverseLockTime = 0f; // 静止受阻换向不再加锁，玩家输入应立即进入新方向。
    public bool holdBrakeAtIdle = false; // 是否在几乎静止且无输入时自动补一点刹车，当前默认关闭以保留自然滑停。
    public bool applyLowFrictionMaterial = true; // 是否在运行时给车身碰撞体附加低摩擦材质。
    public float bodyStaticFriction = 0.08f; // 运行时车身碰撞体材质的静摩擦系数。
    public float bodyDynamicFriction = 0.06f; // 运行时车身碰撞体材质的动摩擦系数。
    public bool liftVehicleAboveGroundOnStart = true; // 是否在启动时把车辆轻微抬起，避免出生时和地面穿插。
    public float startGroundClearance = 0.03f; // 启动离地校正后，车辆最低点与地面保留的额外间隙。
    public float inactivePushAssistSpeed = 8f; // 非激活静止车辆在被撞时附加的启动速度；演示档优先保证车车碰撞有可见位移。
    public float inactivePushAssistMaxTargetSpeed = 22f; // 非激活车辆低于该速度时使用启动辅助；放宽到中速碰撞，避免看起来像焊死。
    public float inactivePushAssistImpulseThreshold = 4f; // 纵向冲量触发门槛；降低后中低速追尾更容易推动目标车。
    public float inactivePushAssistTuningDuration = 1f; // 被顶车辆短时间切到更易滚动配置，避免后轮抬起但车身不走。
    public float contactImpulseTransferScale = 7f; // 前后向碰撞冲量到目标车速度变化的转换倍率；演示档提高碰撞反馈。
    public float contactPushVelocityStep = 2.8f; // 当前控制车贴住静止目标持续给油时，每个物理步补给目标车的纵向速度步进。
    public float contactStaticPushSpeedThreshold = 3.5f; // 两车接近静止时启用贴住推车步进的速度阈值。
    public float contactMinimumPushSpeed = 3.5f; // 低速正面追尾时目标车至少建立的前后向启动速度。
    public float contactImmediateVelocitySeed = 4f; // 正面撞击当下给目标车建立的最低纵向速度种子，避免只抬轮不走。
    public float contactPushSpeedThreshold = 30f; // 接触推车辅助允许的主车速度上限；覆盖中速碰撞展示。
    public float contactPushAlignmentThreshold = 0.2f; // 接触方向与车辆前后方向的对齐阈值；降低后斜向追尾也能触发。
    public float bumperZoneThreshold = 0.8f; // 接触点前后向占比阈值；降低后保险杠附近碰撞不易漏判。
    public float contactPushAcceleration = 18f; // 持续顶住另一辆车时施加的连续推送加速度，避免一走一停。
    public float contactSelfFollowSpeedRatio = 0.8f; // 推车时主控车跟随目标速度的比例，避免自己顶住后掉成 0。
    public float collisionReverseAssistBrakeTorque = 2800f; // 旧调试参数保留；当前碰撞换向不再靠刹旧轮速，而是直接施加反向驱动。
    public float collisionReverseAssistLockTime = 0.03f; // 碰撞状态下的反向辅助刹轮时间，比普通换向更短，只用于尽快把控制权交给新方向。
    public float collisionReverseBodyAssistAcceleration = 16f; // 碰撞态反向时，直接给车身一个小的反向加速度，避免必须等轮子先完全消掉旧扭矩才开始后退。
    public float collisionReverseBodyAssistSpeedThreshold = 3f; // 只有在较低前后速度下才启用碰撞态反向车身辅助，避免正常高速驾驶时被误触发。
    public float collisionReverseBodyVelocitySeed = 1.2f; // 碰撞态反向时，车身沿新方向至少建立起的最低速度种子，避免要等几秒才终于开始后退。
    public float blockedReverseWheelRpmCancelThreshold = 80f; // 撞住静态/动态障碍后换向时，驱动轮残余转速超过该值就直接进入碰撞态反向辅助。
    public float responsiveBodyDriveAcceleration = 22f; // 低速或受阻时的车身辅助驱动力，让 W/S 输入不再完全受 WheelCollider 残余轮速支配。
    public float responsiveBodyDriveSpeedThreshold = 4f; // 低于该前后速度时启动车身辅助；覆盖抵墙、顶车和低速换向。
    public float responsiveBodyDriveVelocitySeed = 1.2f; // 低速/受阻时沿输入方向建立的最低速度，避免按反方向后等很久才动。

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
    private float inactivePushAssistTimer;
    private float reverseLockTimer;
    private int lastThrottleDirection;
    private float coastingAuditTimer;
    private float coastingAuditLastSpeed;
    private bool auditAppliedWheelRollingResistance;
    private bool auditAppliedWheelDrive;
    private bool auditAppliedCoastingBodyDeceleration;
    private bool auditUsedCoastingLinearDamping;
    private bool auditUsedCoastingWheelSyncBrake;
    private bool auditUsedCoastingMotorDrag;
    private float auditCurrentLinearDamping;
    private float auditCurrentBodyDeceleration;
    private float auditCurrentWheelSyncBrakeTorque;
    private float auditCurrentMotorDragTorque;
    private float auditCurrentDriveMotorTorque;
    private float auditCurrentDriveBrakeTorque;
    private float auditCurrentOppositeSpinBrakeTorque;
    private string coastingAuditFilePath;
    private bool coastingAuditFileInitialized;
    private float lastCoastingPlanarSpeed = -1f;

    public float CurrentSteerInput { get; private set; }
    public float CurrentThrottleInput { get; private set; }
    public float CurrentForwardSpeed { get; private set; }
    public bool IsFlipped => Vector3.Dot(transform.up, Vector3.up) < flippedDotThreshold;
    public Rigidbody CachedRigidbody => rb;
    public float SpeedMetersPerSecond => rb == null ? 0f : Vector3.ProjectOnPlane(GetLinearVelocity(), Vector3.up).magnitude;
    public float SpeedKilometersPerHour => SpeedMetersPerSecond * 3.6f;

    public Vector3 GetSafeGroundedPosition(Vector3 candidatePosition, float extraGroundClearance)
    {
        EnsureInitialized();

        if (!TryGetLocalLowestPoint(out float localLowestPoint))
        {
            return candidatePosition + Vector3.up * Mathf.Max(extraGroundClearance, startGroundClearance);
        }

        Vector3 rayOrigin = candidatePosition + Vector3.up * 30f;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return candidatePosition + Vector3.up * Mathf.Max(extraGroundClearance, startGroundClearance);
        }

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            candidatePosition.y = hits[i].point.y - localLowestPoint + Mathf.Max(extraGroundClearance, startGroundClearance);
            return candidatePosition;
        }

        return candidatePosition + Vector3.up * Mathf.Max(extraGroundClearance, startGroundClearance);
    }

    private void Awake()
    {
        if (!EnsureInitialized())
        {
            enabled = false;
        }
    }

    private bool EnsureInitialized()
    {
        ApplySerializedValueMigrationIfNeeded();

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
        ApplyWheelMasses();

        for (int i = 0; i < cachedWheelColliders.Length; i++)
        {
            cachedWheelColliders[i].enabled = true;
        }

        return true;
    }

    private void ApplySerializedValueMigrationIfNeeded()
    {
        if (debugBuildTag == CurrentBuildTagValue)
        {
            return;
        }

        // 旧场景实例如果还停留在之前的序列化值，进入 Play 时直接迁移到当前这轮确认过的动力参数，
        // 避免“文件已更新但 Unity 内存里的对象还是旧值”继续浪费调试时间。
        debugBuildTag = CurrentBuildTagValue;
        motorAcceleration = CurrentMotorAccelerationValue;
        startBoostAcceleration = CurrentStartBoostAccelerationValue;
        wheelTorqueScale = CurrentWheelTorqueScaleValue;
        coastingDecelerationLowSpeed = CurrentCoastingDecelerationLowSpeedValue;
        coastingDecelerationHighSpeed = CurrentCoastingDecelerationHighSpeedValue;
        coastingDecelerationBlendSpeed = CurrentCoastingDecelerationBlendSpeedValue;
        centerOfMassOffset.y = CurrentCenterOfMassYValue;
        runtimeAngularDamping = CurrentRuntimeAngularDampingValue;
        useInactiveCollisionTuning = CurrentUseInactiveCollisionTuningValue;
        inactivePushAssistSpeed = CurrentInactivePushAssistSpeedValue;
        inactivePushAssistMaxTargetSpeed = CurrentInactivePushAssistMaxTargetSpeedValue;
        inactivePushAssistImpulseThreshold = CurrentInactivePushAssistImpulseThresholdValue;
        inactivePushAssistTuningDuration = CurrentInactivePushAssistTuningDurationValue;
        contactImpulseTransferScale = CurrentContactImpulseTransferScaleValue;
        contactPushVelocityStep = CurrentContactPushVelocityStepValue;
        contactStaticPushSpeedThreshold = CurrentContactStaticPushSpeedThresholdValue;
        contactMinimumPushSpeed = CurrentContactMinimumPushSpeedValue;
        contactImmediateVelocitySeed = CurrentContactImmediateVelocitySeedValue;
        contactPushSpeedThreshold = CurrentContactPushSpeedThresholdValue;
        contactPushAlignmentThreshold = CurrentContactPushAlignmentThresholdValue;
        bumperZoneThreshold = CurrentBumperZoneThresholdValue;
        contactPushAcceleration = CurrentContactPushAccelerationValue;
        contactSelfFollowSpeedRatio = CurrentContactSelfFollowSpeedRatioValue;
        directionChangeSpeedThreshold = CurrentDirectionChangeSpeedThresholdValue;
        directionChangeDriveBoost = CurrentDirectionChangeDriveBoostValue;
        stationaryReverseRpmThreshold = CurrentStationaryReverseRpmThresholdValue;
        stationaryReverseBrakeTorque = CurrentStationaryReverseBrakeTorqueValue;
        stationaryReverseLockTime = CurrentStationaryReverseLockTimeValue;
        collisionReverseAssistBrakeTorque = CurrentCollisionReverseAssistBrakeTorqueValue;
        collisionReverseAssistLockTime = CurrentCollisionReverseAssistLockTimeValue;
        collisionReverseBodyAssistAcceleration = CurrentCollisionReverseBodyAssistAccelerationValue;
        collisionReverseBodyAssistSpeedThreshold = CurrentCollisionReverseBodyAssistSpeedThresholdValue;
        collisionReverseBodyVelocitySeed = CurrentCollisionReverseBodyVelocitySeedValue;
        blockedReverseWheelRpmCancelThreshold = CurrentBlockedReverseWheelRpmCancelThresholdValue;
        responsiveBodyDriveAcceleration = CurrentResponsiveBodyDriveAccelerationValue;
        responsiveBodyDriveSpeedThreshold = CurrentResponsiveBodyDriveSpeedThresholdValue;
        responsiveBodyDriveVelocitySeed = CurrentResponsiveBodyDriveVelocitySeedValue;
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        ResetCoastingAuditFrame();

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
            if (inactivePushAssistTimer > 0f)
            {
                inactivePushAssistTimer = Mathf.Max(0f, inactivePushAssistTimer - Time.fixedDeltaTime);
                applyInactiveTuning = true;
            }

            ApplyNeutralCoastState(applyInactiveTuning);
            ApplyInactivePushAssist();
            ApplyCoastingBodyDeceleration(false);
            return;
        }

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
        bool isCoastingInput = !brakePressed && Mathf.Abs(CurrentThrottleInput) < 0.01f;
        ApplyWheelRollingResistance(false, isCoastingInput);
        ApplyWheelSurfaceGrip(false);
        UpdateReverseLockState();
        float targetDamping = brakePressed
            ? brakeDamping
            : Mathf.Abs(CurrentThrottleInput) > 0.01f
                ? drivingLinearDamping
                : GetCoastingLinearDamping();
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
            ApplyResponsiveBodyDriveAssist(horizontalSpeed, brakePressed);
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

        ApplyCoastingBodyDeceleration(brakePressed);
        EmitCoastingAudit(brakePressed, isCoastingInput);
    }

    private void Start()
    {
        // 启动时按轮子/碰撞体最低点做一次离地校正，避免初始姿态直接穿进地面。
        TryLiftVehicleAboveGround();
        PrepareCoastingAuditFile();
    }

    public void SetControlled(bool controlled)
    {
        if (!EnsureInitialized())
        {
            return;
        }

        if (isControlled == controlled)
        {
            return;
        }

        isControlled = controlled;

        if (!controlled)
        {
            ClearControlState();
            return;
        }

        SetLinearDamping(normalDamping);
        ApplyWheelRollingResistance(false, false);
        ApplyWheelSurfaceGrip(false);
    }

    private void ClearControlState()
    {
        CurrentThrottleInput = 0f;
        CurrentSteerInput = 0f;
        reverseLockTimer = 0f;
        lastThrottleDirection = 0;
        pendingInactiveVelocityChange = Vector3.zero;
        inactivePushAssistTimer = 0f;
        lastCoastingPlanarSpeed = -1f;

        ApplyNeutralCoastState(useInactiveCollisionTuning);
        ResetCoastingAuditFrame();
    }

    private void ApplyNeutralCoastState(bool applyInactiveTuning)
    {
        CurrentThrottleInput = 0f;
        CurrentSteerInput = 0f;
        reverseLockTimer = 0f;
        lastThrottleDirection = 0;

        SetLinearDamping(applyInactiveTuning ? inactiveLinearDamping : GetCoastingLinearDamping());
        ApplyWheelRollingResistance(applyInactiveTuning, true);
        ApplyWheelSurfaceGrip(applyInactiveTuning);
        ResetWheelDrive();
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
        ApplyWheelMasses();
        bool applyInactiveTuning = !isControlled && useInactiveCollisionTuning;
        ApplyWheelRollingResistance(applyInactiveTuning, false);
        ApplyWheelSurfaceGrip(applyInactiveTuning);

        if (!isControlled)
        {
            SetLinearDamping(applyInactiveTuning ? inactiveLinearDamping : normalDamping);
            return;
        }

        float targetDamping = Mathf.Abs(CurrentThrottleInput) > 0.01f ? drivingLinearDamping : GetCoastingLinearDamping();
        SetLinearDamping(targetDamping);
    }

    public void QueueInactivePush(Vector3 pushDirection, float pushSpeed)
    {
        if (isControlled || rb == null || pushDirection.sqrMagnitude < 0.0001f || pushSpeed <= 0f)
        {
            return;
        }

        inactivePushAssistTimer = Mathf.Max(inactivePushAssistTimer, inactivePushAssistTuningDuration);
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
        coastingMotorDragTorque = source.coastingMotorDragTorque;
        coastingMotorDragRpmFactor = source.coastingMotorDragRpmFactor;
        coastingWheelSyncBrakeTorque = source.coastingWheelSyncBrakeTorque;
        coastingWheelSyncRpmTolerance = source.coastingWheelSyncRpmTolerance;
        coastingWheelSyncMinSpeed = source.coastingWheelSyncMinSpeed;
        lowSpeedCoastThreshold = source.lowSpeedCoastThreshold;
        lowSpeedCoastBrakeTorque = source.lowSpeedCoastBrakeTorque;
        directionChangeSpeedThreshold = source.directionChangeSpeedThreshold;
        directionChangeBrakeTorque = source.directionChangeBrakeTorque;
        directionChangeDriveBoost = source.directionChangeDriveBoost;
        stationaryReverseRpmThreshold = source.stationaryReverseRpmThreshold;
        stationaryReverseBrakeTorque = source.stationaryReverseBrakeTorque;
        stationaryReverseLockTime = source.stationaryReverseLockTime;
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
        contactMinimumPushSpeed = source.contactMinimumPushSpeed;
        contactImmediateVelocitySeed = source.contactImmediateVelocitySeed;
        contactPushSpeedThreshold = source.contactPushSpeedThreshold;
        contactPushAlignmentThreshold = source.contactPushAlignmentThreshold;
        bumperZoneThreshold = source.bumperZoneThreshold;
        contactPushAcceleration = source.contactPushAcceleration;
        contactSelfFollowSpeedRatio = source.contactSelfFollowSpeedRatio;
        collisionReverseAssistBrakeTorque = source.collisionReverseAssistBrakeTorque;
        collisionReverseAssistLockTime = source.collisionReverseAssistLockTime;
        collisionReverseBodyAssistAcceleration = source.collisionReverseBodyAssistAcceleration;
        collisionReverseBodyAssistSpeedThreshold = source.collisionReverseBodyAssistSpeedThreshold;
        collisionReverseBodyVelocitySeed = source.collisionReverseBodyVelocitySeed;
        blockedReverseWheelRpmCancelThreshold = source.blockedReverseWheelRpmCancelThreshold;
        responsiveBodyDriveAcceleration = source.responsiveBodyDriveAcceleration;
        responsiveBodyDriveSpeedThreshold = source.responsiveBodyDriveSpeedThreshold;
        responsiveBodyDriveVelocitySeed = source.responsiveBodyDriveVelocitySeed;

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
        auditAppliedWheelDrive = true;
        float absForwardSpeed = Mathf.Abs(CurrentForwardSpeed);
        float speedFactor = Mathf.InverseLerp(minTurnSpeed, maxSpeed, absForwardSpeed);
        // 这里不是做真实转向几何，而是课程级的简化前轮转向：低速角度更大，高速自动收一点。
        float steerAngle = turnAcceleration * Mathf.Lerp(8f, 4f * highSpeedTurnFactor, speedFactor) * CurrentSteerInput;
        float motorScale = horizontalSpeed < 2.5f ? startBoostAcceleration : motorAcceleration;
        float directionScale = CurrentThrottleInput < 0f ? reverseAccelerationScale : 1f;
        float motorTorque = CurrentThrottleInput * motorScale * wheelTorqueScale * directionScale;
        float brakeTorque = brakePressed ? brakeDamping * brakeTorqueScale : 0f;
        float averageDriveWheelRpm = GetAverageDriveWheelRpm();
        bool isCoasting = !brakePressed && Mathf.Abs(CurrentThrottleInput) < 0.01f;
        bool isChangingDirection =
            Mathf.Abs(CurrentThrottleInput) > 0.01f &&
            absForwardSpeed > directionChangeSpeedThreshold &&
            Mathf.Sign(CurrentThrottleInput) != Mathf.Sign(CurrentForwardSpeed);
        if (isChangingDirection)
        {
            // 换向时反向油门必须立即作用；WheelCollider 的反向扭矩本身就会承担减速和反转。
            // 不能再把 motorTorque 清零，否则玩家按着 S 会先经历几秒“只滑不加速”。
            motorTorque *= Mathf.Max(1f, directionChangeDriveBoost);
        }
        else if (reverseLockTimer > 0f)
        {
            brakeTorque = Mathf.Max(brakeTorque, stationaryReverseBrakeTorque);
        }
        if (isCoasting)
        {
            // 滑行状态下不再使用任何轮上制动/反拖主线，避免脚刹感和平台速度。
            motorTorque = 0f;
            brakeTorque = 0f;
        }

        if (holdBrakeAtIdle && Mathf.Abs(CurrentThrottleInput) < 0.01f && absForwardSpeed < minTurnSpeed)
        {
            // 如需停车后更快收住，可打开该选项；当前默认关闭，避免出现“突然被踩一脚刹车”的感觉。
            brakeTorque = Mathf.Max(brakeTorque, brakeDamping * 150f);
        }

        float oppositeSpinBrakeTorque = GetCoastingOppositeSpinBrakeTorque(brakePressed, absForwardSpeed, averageDriveWheelRpm);
        if (oppositeSpinBrakeTorque > 0f)
        {
            brakeTorque = Mathf.Max(brakeTorque, oppositeSpinBrakeTorque);
        }

        float syncBrakeTorque = isCoasting ? 0f : GetCoastingWheelSyncBrakeTorque(brakePressed, absForwardSpeed, averageDriveWheelRpm);
        if (!isCoasting && syncBrakeTorque > 0f)
        {
            // 松开油门后如果轮子仍明显快于车速，对同步刹车做比例控制，避免过晚介入或低速阶段突然补重刹。
            brakeTorque = Mathf.Max(brakeTorque, syncBrakeTorque);
        }

        if (!isCoasting && absForwardSpeed < lowSpeedCoastThreshold)
        {
            float tailBrakeFactor = 1f - Mathf.Clamp01(absForwardSpeed / Mathf.Max(lowSpeedCoastThreshold, 0.01f));
            brakeTorque = Mathf.Max(brakeTorque, lowSpeedCoastBrakeTorque * tailBrakeFactor);
        }

        auditCurrentWheelSyncBrakeTorque = syncBrakeTorque;
        auditCurrentOppositeSpinBrakeTorque = oppositeSpinBrakeTorque;
        auditCurrentMotorDragTorque = 0f;
        auditCurrentDriveMotorTorque = motorTorque;
        auditCurrentDriveBrakeTorque = brakeTorque;

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

    private void ApplyResponsiveBodyDriveAssist(float horizontalSpeed, bool brakePressed)
    {
        if (rb == null || brakePressed || Mathf.Abs(CurrentThrottleInput) < 0.01f)
        {
            return;
        }

        Vector3 desiredDirection = GetPlanarForward() * Mathf.Sign(CurrentThrottleInput);
        if (desiredDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        desiredDirection.Normalize();
        Vector3 planarVelocity = Vector3.ProjectOnPlane(GetLinearVelocity(), Vector3.up);
        float alongSpeed = Vector3.Dot(planarVelocity, desiredDirection);
        bool lowSpeedOrBlocked = horizontalSpeed < responsiveBodyDriveSpeedThreshold;
        bool movingAgainstInput = alongSpeed < -0.05f;
        if (!lowSpeedOrBlocked && !movingAgainstInput)
        {
            return;
        }

        float speedFactor = 1f - Mathf.Clamp01(Mathf.Max(0f, alongSpeed) / Mathf.Max(responsiveBodyDriveSpeedThreshold, 0.01f));
        float acceleration = responsiveBodyDriveAcceleration * Mathf.Max(0.25f, speedFactor);
        rb.AddForce(desiredDirection * acceleration, ForceMode.Acceleration);

        if (lowSpeedOrBlocked && alongSpeed < responsiveBodyDriveVelocitySeed)
        {
            EnsurePlanarSpeedAlong(rb, desiredDirection, responsiveBodyDriveVelocitySeed);
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
        auditUsedCoastingWheelSyncBrake = true;
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

        float syncFactor = Mathf.Clamp01(rpmOverspeed / Mathf.Max(coastingWheelSyncRpmTolerance, 1f));
        float speedFactor = Mathf.InverseLerp(
            coastingWheelSyncMinSpeed,
            Mathf.Max(coastBrakeFullSpeed, coastingWheelSyncMinSpeed + 0.01f),
            absForwardSpeed);
        return coastingWheelSyncBrakeTorque * syncFactor * speedFactor;
    }

    private float GetCoastingOppositeSpinBrakeTorque(bool brakePressed, float absForwardSpeed, float averageDriveWheelRpm)
    {
        if (brakePressed || Mathf.Abs(CurrentThrottleInput) > 0.01f || absForwardSpeed < coastingWheelSyncMinSpeed)
        {
            return 0f;
        }

        if (Mathf.Abs(CurrentForwardSpeed) < 0.01f || Mathf.Abs(averageDriveWheelRpm) < coastingOppositeSpinRpmThreshold)
        {
            return 0f;
        }

        // 车身仍在前进，但驱动轮 rpm 已经整体反号暴冲时，说明轮胎系统正在用错误轮速把车继续拖着跑。
        if (Mathf.Sign(CurrentForwardSpeed) == Mathf.Sign(averageDriveWheelRpm))
        {
            return 0f;
        }

        float rpmFactor = Mathf.Clamp01(Mathf.Abs(averageDriveWheelRpm) / (coastingOppositeSpinRpmThreshold * 4f));
        float speedFactor = Mathf.InverseLerp(
            coastingWheelSyncMinSpeed,
            Mathf.Max(coastingDecelerationBlendSpeed, coastingWheelSyncMinSpeed + 0.01f),
            absForwardSpeed);
        return coastingOppositeSpinBrakeTorque * rpmFactor * speedFactor;
    }

    private float GetCoastingMotorDragTorque(bool brakePressed, float absForwardSpeed, float averageDriveWheelRpm)
    {
        auditUsedCoastingMotorDrag = true;
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

        float speedFactor = Mathf.InverseLerp(
            coastingWheelSyncMinSpeed,
            Mathf.Max(coastBrakeFullSpeed, coastingWheelSyncMinSpeed + 0.01f),
            absForwardSpeed);
        return (coastingMotorDragTorque + rpmOverspeed * coastingMotorDragRpmFactor) * speedFactor * speedFactor;
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

    private void ApplyCoastingBodyDeceleration(bool brakePressed)
    {
        if (rb == null || brakePressed || Mathf.Abs(CurrentThrottleInput) > 0.01f)
        {
            auditCurrentBodyDeceleration = 0f;
            lastCoastingPlanarSpeed = -1f;
            return;
        }

        Vector3 planarVelocity = Vector3.ProjectOnPlane(GetLinearVelocity(), Vector3.up);
        float speed = planarVelocity.magnitude;
        if (speed < 0.05f)
        {
            auditCurrentBodyDeceleration = 0f;
            lastCoastingPlanarSpeed = -1f;
            return;
        }

        float speedFactor = Mathf.Clamp01(speed / Mathf.Max(coastingDecelerationBlendSpeed, 0.01f));
        float deceleration = Mathf.Lerp(coastingDecelerationLowSpeed, coastingDecelerationHighSpeed, speedFactor);

        float currentSpeed = speed;
        if (lastCoastingPlanarSpeed >= 0f)
        {
            float maxAllowedSpeed = Mathf.Max(0f, lastCoastingPlanarSpeed - deceleration * Time.fixedDeltaTime);
            if (currentSpeed > maxAllowedSpeed)
            {
                Vector3 correctedPlanarVelocity = planarVelocity.normalized * maxAllowedSpeed;
                Vector3 verticalVelocity = Vector3.Project(GetLinearVelocity(), Vector3.up);
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = correctedPlanarVelocity + verticalVelocity;
#else
                rb.velocity = correctedPlanarVelocity + verticalVelocity;
#endif
                currentSpeed = maxAllowedSpeed;
            }
        }

        lastCoastingPlanarSpeed = currentSpeed;
        auditAppliedCoastingBodyDeceleration = true;
        auditCurrentBodyDeceleration = deceleration;
    }

    private float GetCoastingLinearDamping()
    {
        auditUsedCoastingLinearDamping = true;
        // 当前滑行主线已经改成“单一车身减速度”，这里不再额外叠速度分层阻尼，避免再次做出 20 km/h 平台和脚刹感。
        return runtimeLinearDamping;
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
            Vector3 pushDirection = Vector3.ProjectOnPlane(pendingInactiveVelocityChange, Vector3.up);
            float pushMagnitude = pushDirection.magnitude;
            if (pushMagnitude > 0.0001f)
            {
                pushDirection /= pushMagnitude;
                Vector3 planarVelocity = Vector3.ProjectOnPlane(GetLinearVelocity(), Vector3.up);
                float currentAlong = Vector3.Dot(planarVelocity, pushDirection);
                float targetAlong = Mathf.Min(inactivePushAssistMaxTargetSpeed, Mathf.Max(pushMagnitude, currentAlong + pushMagnitude));
                float requiredDelta = targetAlong - currentAlong;
                if (requiredDelta > 0.0001f)
                {
                    rb.AddForce(pushDirection * requiredDelta, ForceMode.VelocityChange);
                }
            }

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
        ApplyCollisionReverseAssist(collision);
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

        float minimumPushSpeed = 0f;
        if (otherSpeed < contactPushSpeedThreshold)
        {
            minimumPushSpeed = Mathf.Min(
                inactivePushAssistMaxTargetSpeed,
                Mathf.Max(contactMinimumPushSpeed, ownSpeed * 0.35f));
        }

        float pushSpeed = Mathf.Max(impulsePushSpeed, sustainedPushSpeed, minimumPushSpeed);
        if (pushSpeed <= 0.0001f)
        {
            return;
        }

        float seededPushSpeed = Mathf.Min(
            inactivePushAssistMaxTargetSpeed,
            Mathf.Max(pushSpeed, contactImmediateVelocitySeed));

        ApplyContinuousContactPush(otherRb, ownPushDirection.normalized, targetPushDirection.normalized, seededPushSpeed);

        SimpleCarController otherController = otherRb.GetComponent<SimpleCarController>();
        if (otherController != null)
        {
            otherController.PrepareForPassivePush();
            EnsurePlanarSpeedAlong(otherRb, targetPushDirection, seededPushSpeed);
            otherController.QueueInactivePush(targetPushDirection, pushSpeed);
        }
        else
        {
            EnsurePlanarSpeedAlong(otherRb, targetPushDirection, seededPushSpeed);
        }
    }

    private void ApplyContinuousContactPush(Rigidbody otherRb, Vector3 ownPushDirection, Vector3 targetPushDirection, float seededPushSpeed)
    {
        if (otherRb == null || rb == null || contactPushAcceleration <= 0f)
        {
            return;
        }

        float throttle = Mathf.Abs(CurrentThrottleInput);
        if (throttle < 0.01f)
        {
            return;
        }

        float acceleration = contactPushAcceleration * throttle;
        otherRb.AddForce(targetPushDirection * acceleration, ForceMode.Acceleration);

        float selfFollowSpeed = Mathf.Min(
            contactPushSpeedThreshold,
            Mathf.Max(contactMinimumPushSpeed, seededPushSpeed * Mathf.Clamp01(contactSelfFollowSpeedRatio)));
        EnsurePlanarSpeedAlong(rb, ownPushDirection, selfFollowSpeed);
    }

    private void ApplyCollisionReverseAssist(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        if (Mathf.Abs(CurrentThrottleInput) < 0.01f)
        {
            return;
        }

        float absForwardSpeed = Mathf.Abs(CurrentForwardSpeed);
        float averageDriveWheelRpm = GetAverageDriveWheelRpm();
        bool wantsOppositeOfVelocity =
            absForwardSpeed > 0.05f &&
            Mathf.Sign(CurrentThrottleInput) != Mathf.Sign(CurrentForwardSpeed);
        bool wantsOppositeOfWheelSpin =
            Mathf.Abs(averageDriveWheelRpm) > blockedReverseWheelRpmCancelThreshold &&
            Mathf.Sign(CurrentThrottleInput) != Mathf.Sign(averageDriveWheelRpm);

        if (!wantsOppositeOfVelocity && !wantsOppositeOfWheelSpin)
        {
            return;
        }

        // 这里必须覆盖静态墙体/柱子等 collision.rigidbody == null 的情况。
        // 车辆撞墙后车身速度被约束到接近 0，但 WheelCollider.rpm 仍可能保存旧方向趋势；
        // 如果只等轮速自然衰减，玩家按反方向会感觉几秒钟都没有响应。
        reverseLockTimer = 0f;
        lastThrottleDirection = CurrentThrottleInput > 0f ? 1 : -1;

        for (int i = 0; driveWheels != null && i < driveWheels.Length; i++)
        {
            if (driveWheels[i] == null)
            {
                continue;
            }

            float directionScale = CurrentThrottleInput < 0f ? reverseAccelerationScale : 1f;
            driveWheels[i].brakeTorque = 0f;
            driveWheels[i].motorTorque = CurrentThrottleInput * startBoostAcceleration * wheelTorqueScale * directionScale;
        }

        if (Mathf.Abs(CurrentForwardSpeed) < collisionReverseBodyAssistSpeedThreshold)
        {
            Vector3 reverseAssistDirection = GetPlanarForward() * Mathf.Sign(CurrentThrottleInput);
            if (reverseAssistDirection.sqrMagnitude > 0.0001f)
            {
                EnsurePlanarSpeedAlong(rb, reverseAssistDirection.normalized, collisionReverseBodyVelocitySeed);
                rb.AddForce(reverseAssistDirection.normalized * collisionReverseBodyAssistAcceleration, ForceMode.Acceleration);
            }
        }
    }

    private void PrepareForPassivePush()
    {
        reverseLockTimer = 0f;
        lastThrottleDirection = 0;

        if (driveWheels == null)
        {
            return;
        }

        for (int i = 0; i < driveWheels.Length; i++)
        {
            if (driveWheels[i] == null)
            {
                continue;
            }

            driveWheels[i].motorTorque = 0f;
            driveWheels[i].brakeTorque = 0f;
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

    private void ApplyWheelRollingResistance(bool inactive, bool isCoasting)
    {
        auditAppliedWheelRollingResistance = true;
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

            if (inactive)
            {
                cachedWheelColliders[i].wheelDampingRate = inactiveWheelDampingRate;
                continue;
            }

            float dampingRate = defaultWheelDampingRates[i] * wheelDampingRateScale;
            if (isCoasting)
            {
                dampingRate *= coastingWheelDampingScale;
            }

            cachedWheelColliders[i].wheelDampingRate = dampingRate;
        }
    }

    private void ApplyWheelMasses()
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

            cachedWheelColliders[i].mass = wheelColliderMass;
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

        // 这里不能在运行时同步参数的每一帧都 new/重挂一次材质，否则会不断触发 PhysX 的形状材质重建，
        // 当前项目里已经实测会把 Unity 6000.3 在 Play 中直接顶崩。
        if (runtimeBodyMaterial == null)
        {
            runtimeBodyMaterial = new PhysicsMaterial("RuntimeVehicleBodyLowFriction")
            {
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }

        runtimeBodyMaterial.staticFriction = bodyStaticFriction;
        runtimeBodyMaterial.dynamicFriction = bodyDynamicFriction;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].isTrigger || colliders[i] is WheelCollider)
            {
                continue;
            }

            if (colliders[i].sharedMaterial != runtimeBodyMaterial)
            {
                colliders[i].sharedMaterial = runtimeBodyMaterial;
            }
        }
    }

    private void TryLiftVehicleAboveGround()
    {
        if (!liftVehicleAboveGroundOnStart || startPoseAdjusted)
        {
            return;
        }

        startPoseAdjusted = true;

        if (!TryGetLocalLowestPoint(out _))
        {
            return;
        }

        Vector3 safePosition = GetSafeGroundedPosition(transform.position, startGroundClearance);
        if (safePosition.y > transform.position.y)
        {
            transform.position = safePosition;
            rb.position = safePosition;
            rb.Sleep();
            rb.WakeUp();
        }
    }

    private bool TryGetLocalLowestPoint(out float localLowestPoint)
    {
        localLowestPoint = float.PositiveInfinity;

        if (cachedWheelColliders != null)
        {
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
            return false;
        }

        return true;
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

    private static void EnsurePlanarSpeedAlong(Rigidbody targetRigidbody, Vector3 direction, float desiredAlongSpeed)
    {
        if (targetRigidbody == null || desiredAlongSpeed <= 0.0001f)
        {
            return;
        }

        Vector3 planarDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (planarDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        planarDirection.Normalize();

        Vector3 currentVelocity = GetLinearVelocity(targetRigidbody);
        Vector3 planarVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
        Vector3 verticalVelocity = Vector3.Project(currentVelocity, Vector3.up);
        float currentAlongSpeed = Vector3.Dot(planarVelocity, planarDirection);
        if (currentAlongSpeed >= desiredAlongSpeed)
        {
            targetRigidbody.WakeUp();
            return;
        }

        Vector3 lateralVelocity = planarVelocity - planarDirection * currentAlongSpeed;
        Vector3 updatedPlanarVelocity = lateralVelocity + planarDirection * desiredAlongSpeed;
#if UNITY_6000_0_OR_NEWER
        targetRigidbody.linearVelocity = updatedPlanarVelocity + verticalVelocity;
#else
        targetRigidbody.velocity = updatedPlanarVelocity + verticalVelocity;
#endif
        targetRigidbody.WakeUp();
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
        auditCurrentLinearDamping = value;
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = value;
#else
        rb.drag = value;
#endif
    }

    private void ResetCoastingAuditFrame()
    {
        auditAppliedWheelRollingResistance = false;
        auditAppliedWheelDrive = false;
        auditAppliedCoastingBodyDeceleration = false;
        auditUsedCoastingLinearDamping = false;
        auditUsedCoastingWheelSyncBrake = false;
        auditUsedCoastingMotorDrag = false;
        auditCurrentLinearDamping = 0f;
        auditCurrentBodyDeceleration = 0f;
        auditCurrentWheelSyncBrakeTorque = 0f;
        auditCurrentOppositeSpinBrakeTorque = 0f;
        auditCurrentMotorDragTorque = 0f;
        auditCurrentDriveMotorTorque = 0f;
        auditCurrentDriveBrakeTorque = 0f;
    }

    private void EmitCoastingAudit(bool brakePressed, bool isCoastingInput)
    {
        if (!debugCoastingAudit || !isControlled || !isCoastingInput || brakePressed || rb == null)
        {
            coastingAuditTimer = 0f;
            coastingAuditLastSpeed = SpeedMetersPerSecond;
            return;
        }

        coastingAuditTimer += Time.fixedDeltaTime;
        if (coastingAuditTimer < debugCoastingAuditInterval)
        {
            return;
        }

        float interval = Mathf.Max(coastingAuditTimer, 0.0001f);
        float speedNow = SpeedMetersPerSecond;
        float deltaSpeed = speedNow - coastingAuditLastSpeed;
        coastingAuditTimer = 0f;
        coastingAuditLastSpeed = speedNow;

        StringBuilder sb = new StringBuilder();
        sb.Append("[CoastAudit] ")
          .Append(name)
          .Append(" speed=")
          .Append(speedNow.ToString("F2"))
          .Append("m/s(")
          .Append((speedNow * 3.6f).ToString("F1"))
          .Append("km/h)")
          .Append(" throttle=")
          .Append(CurrentThrottleInput.ToString("F2"))
          .Append(" brake=")
          .Append(brakePressed)
          .Append(" coasting=")
          .Append(isCoastingInput)
          .Append(" damping=")
          .Append(auditCurrentLinearDamping.ToString("F3"))
          .Append(" bodyDecel=")
          .Append(auditCurrentBodyDeceleration.ToString("F3"))
          .Append(" deltaV=")
          .Append((deltaSpeed / interval).ToString("F3"))
          .Append("m/s²")
          .Append(" wheelRes=")
          .Append(auditAppliedWheelRollingResistance)
          .Append(" wheelDrive=")
          .Append(auditAppliedWheelDrive)
          .Append(" coastBody=")
          .Append(auditAppliedCoastingBodyDeceleration)
          .Append(" coastDamping=")
          .Append(auditUsedCoastingLinearDamping)
          .Append(" syncBrake=")
          .Append(auditUsedCoastingWheelSyncBrake)
          .Append(":")
          .Append(auditCurrentWheelSyncBrakeTorque.ToString("F1"))
          .Append(" oppositeSpinBrake=")
          .Append(auditCurrentOppositeSpinBrakeTorque.ToString("F1"))
          .Append(" motorDrag=")
          .Append(auditUsedCoastingMotorDrag)
          .Append(":")
          .Append(auditCurrentMotorDragTorque.ToString("F1"))
          .Append(" driveMotor=")
          .Append(auditCurrentDriveMotorTorque.ToString("F1"))
          .Append(" driveBrake=")
          .Append(auditCurrentDriveBrakeTorque.ToString("F1"));

        if (cachedWheelColliders != null)
        {
            for (int i = 0; i < cachedWheelColliders.Length; i++)
            {
                WheelCollider wheel = cachedWheelColliders[i];
                if (wheel == null)
                {
                    continue;
                }

                sb.Append(" | W")
                  .Append(i)
                  .Append(" rpm=")
                  .Append(wheel.rpm.ToString("F1"))
                  .Append(" motor=")
                  .Append(wheel.motorTorque.ToString("F1"))
                  .Append(" brake=")
                  .Append(wheel.brakeTorque.ToString("F1"))
                  .Append(" damp=")
                  .Append(wheel.wheelDampingRate.ToString("F3"))
                  .Append(" grounded=")
                  .Append(wheel.isGrounded);
            }
        }

        string line = sb.ToString();
        Debug.Log(line);
        AppendCoastingAuditLine(line);
    }

    private void PrepareCoastingAuditFile()
    {
        if (!debugCoastingAuditWriteToFile || coastingAuditFileInitialized)
        {
            return;
        }

        string docsDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs"));
        if (!Directory.Exists(docsDirectory))
        {
            Directory.CreateDirectory(docsDirectory);
        }

        coastingAuditFilePath = Path.Combine(docsDirectory, "coasting_audit_log.txt");
        string header =
            "[CoastAudit] session start " +
            System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
            System.Environment.NewLine;
        File.WriteAllText(coastingAuditFilePath, header, Encoding.UTF8);
        coastingAuditFileInitialized = true;
        Debug.Log("[CoastAudit] writing audit log to " + coastingAuditFilePath);
    }

    private void AppendCoastingAuditLine(string line)
    {
        if (!debugCoastingAuditWriteToFile)
        {
            return;
        }

        if (!coastingAuditFileInitialized)
        {
            PrepareCoastingAuditFile();
        }

        if (string.IsNullOrEmpty(coastingAuditFilePath))
        {
            return;
        }

        File.AppendAllText(
            coastingAuditFilePath,
            line + System.Environment.NewLine,
            Encoding.UTF8);
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

using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleRuntimeUI : MonoBehaviour
{
    public VehicleManager vehicleManager; // 车辆管理器，用来获取当前车辆和切车状态。
    public bool showPanel = true; // 是否显示运行时调参面板。
    public float panelWidth = 380f; // 面板宽度。
    public float panelHeight = 760f; // 面板高度。
    public float panelMargin = 16f; // 面板与屏幕边缘的间距。
    public float telemetrySampleInterval = 0.12f; // 状态读数刷新间隔，越大越稳，越小越灵敏。
    public float telemetrySmoothing = 0.35f; // 状态读数平滑系数，越大越跟手，越小越稳定。
    public float telemetryDeadZone = 0.08f; // 小于该阈值的细小抖动直接视为 0，避免数字来回跳。

    private Vector2 scrollPosition;
    private float lastForwardSpeed;
    private float telemetryTimer;
    private float displayedForwardAcceleration;
    private float displayedDriveForce;
    private SimpleCarController lastTelemetryVehicle;
    private GUIStyle headerStyle;
    private GUIStyle sectionStyle;
    private GUIStyle noteStyle;

    private void Awake()
    {
        if (vehicleManager == null)
        {
            vehicleManager = GetComponent<VehicleManager>();
            if (vehicleManager == null)
            {
                vehicleManager = FindFirstObjectByType<VehicleManager>();
            }
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f2Key.wasPressedThisFrame)
        {
            showPanel = !showPanel;
        }

        SimpleCarController currentVehicle = vehicleManager != null ? vehicleManager.CurrentVehicle : null;
        if (currentVehicle == null)
        {
            telemetryTimer = 0f;
            displayedForwardAcceleration = 0f;
            displayedDriveForce = 0f;
            return;
        }

        UpdateTelemetry(currentVehicle);
    }

    private void OnGUI()
    {
        if (!showPanel)
        {
            DrawCollapsedHint();
            return;
        }

        EnsureStyles();

        float height = Mathf.Min(panelHeight, Screen.height - panelMargin * 2f);
        Rect panelRect = new Rect(panelMargin, panelMargin, panelWidth, height);
        GUILayout.BeginArea(panelRect, GUI.skin.box);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

        GUILayout.Label("车辆物理参数面板", headerStyle);
        GUILayout.Label("F2 显示/隐藏 | 约定：1 Unity 单位≈1 米，重力按 m/s² 显示", noteStyle);

        SimpleCarController currentVehicle = vehicleManager != null ? vehicleManager.CurrentVehicle : null;
        if (currentVehicle == null)
        {
            GUILayout.Label("当前没有可控制车辆。", noteStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            return;
        }

        DrawVehicleSummary(currentVehicle);
        DrawActionSection();
        DrawDriveSection(currentVehicle);
        DrawPhysicsSection(currentVehicle);
        DrawCollisionSection(currentVehicle);
        DrawCameraSection();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawCollapsedHint()
    {
        GUI.Box(new Rect(panelMargin, panelMargin, 110f, 32f), "F2 打开参数面板");
    }

    private void DrawVehicleSummary(SimpleCarController vehicle)
    {
        Rigidbody rb = vehicle.CachedRigidbody != null ? vehicle.CachedRigidbody : vehicle.GetComponent<Rigidbody>();
        float mass = rb != null ? rb.mass : 0f;
        float speedMps = vehicle.SpeedMetersPerSecond;
        float speedKph = vehicle.SpeedKilometersPerHour;
        float estimatedLength = EstimateVehicleLengthMeters(vehicle.transform);

        GUILayout.Space(6f);
        GUILayout.Label("当前车辆状态", sectionStyle);
        GUILayout.Label($"名称：{vehicle.name}");
        GUILayout.Label($"速度：{speedMps:F2} m/s | {speedKph:F1} km/h");
        GUILayout.Label($"前向速度：{vehicle.CurrentForwardSpeed:F2} m/s");
        GUILayout.Label($"估算前向加速度：{displayedForwardAcceleration:F2} m/s²");
        GUILayout.Label($"估算纵向合力：{displayedDriveForce:F0} N");
        GUILayout.Label($"质量：{mass:F0} kg");
        GUILayout.Label($"重力加速度：{Physics.gravity.magnitude:F2} m/s²");
        GUILayout.Label($"估算车长：{estimatedLength:F2} m");
        GUILayout.Label($"翻车状态：{(vehicle.IsFlipped ? "是" : "否")}");
    }

    private void DrawActionSection()
    {
        if (vehicleManager == null)
        {
            return;
        }

        GUILayout.Space(10f);
        GUILayout.Label("快捷操作", sectionStyle);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("重置当前车辆", GUILayout.Height(28f)))
        {
            vehicleManager.ResetCurrentVehicle();
        }

        if (GUILayout.Button("重置全部车辆", GUILayout.Height(28f)))
        {
            vehicleManager.ResetAllVehicles();
        }
        GUILayout.EndHorizontal();

        int vehicleCount = vehicleManager.VehicleCount;
        if (vehicleCount <= 0)
        {
            return;
        }

        GUILayout.Label("车辆切换：");
        GUILayout.BeginHorizontal();
        for (int i = 0; i < vehicleCount && i < 5; i++)
        {
            string label = vehicleManager.vehicles[i] != null ? $"{i + 1}:{vehicleManager.vehicles[i].name}" : $"{i + 1}:null";
            if (GUILayout.Button(label, GUILayout.Height(26f)))
            {
                vehicleManager.SetCurrentVehicle(i);
            }
        }
        GUILayout.EndHorizontal();
    }

    private void DrawDriveSection(SimpleCarController vehicle)
    {
        GUILayout.Space(10f);
        GUILayout.Label("驱动与操控", sectionStyle);

        bool changed = false;
        changed |= DrawSlider(ref vehicle.motorAcceleration, "正常驱动力", 100f, 4000f, "N/torque");
        changed |= DrawSlider(ref vehicle.startBoostAcceleration, "起步辅助驱动力", 100f, 5000f, "N/torque");
        changed |= DrawSlider(ref vehicle.turnAcceleration, "基础转向强度", 0.5f, 8f, "");
        changed |= DrawSlider(ref vehicle.highSpeedTurnFactor, "高速转向系数", 0.1f, 1f, "");
        changed |= DrawSlider(ref vehicle.brakeDamping, "刹车阻尼/制动力", 0.5f, 12f, "");
        changed |= DrawSlider(ref vehicle.maxSpeed, "软速度上限", 5f, 120f, "m/s");
        changed |= DrawSlider(ref vehicle.wheelTorqueScale, "轮驱动扭矩倍率", 0.2f, 4f, "x");
        changed |= DrawSlider(ref vehicle.reverseAccelerationScale, "倒车驱动力倍率", 0.2f, 1f, "x");
        changed |= DrawSlider(ref vehicle.brakeTorqueScale, "刹车扭矩倍率", 50f, 500f, "x");
        changed |= DrawSlider(ref vehicle.coastingWheelSyncBrakeTorque, "空挡轮速同步刹车", 0f, 3000f, "Nm");
        changed |= DrawSlider(ref vehicle.coastingWheelSyncRpmTolerance, "轮速同步容差", 0f, 120f, "rpm");
        changed |= DrawSlider(ref vehicle.directionChangeSpeedThreshold, "换向速度阈值", 0.05f, 5f, "m/s");
        changed |= DrawSlider(ref vehicle.directionChangeBrakeTorque, "换向刹车扭矩", 200f, 6000f, "Nm");
        changed |= DrawSlider(ref vehicle.stationaryReverseRpmThreshold, "静止换向轮速阈值", 20f, 400f, "rpm");
        changed |= DrawSlider(ref vehicle.stationaryReverseBrakeTorque, "静止换向刹车扭矩", 200f, 6000f, "Nm");
        changed |= DrawSlider(ref vehicle.stationaryReverseLockTime, "静止换向刹轮时间", 0.02f, 0.6f, "s");
        changed |= DrawSlider(ref vehicle.drivingLinearDamping, "有油门阻尼", 0f, 0.5f, "");
        changed |= DrawSlider(ref vehicle.runtimeLinearDamping, "空挡滑行阻尼", 0f, 1f, "");
        bool inactiveCollisionTuning = GUILayout.Toggle(vehicle.useInactiveCollisionTuning, "非当前车使用低阻力碰撞展示参数");
        if (inactiveCollisionTuning != vehicle.useInactiveCollisionTuning)
        {
            vehicle.useInactiveCollisionTuning = inactiveCollisionTuning;
            changed = true;
        }
        changed |= DrawSlider(ref vehicle.lowSpeedCoastThreshold, "低速尾速收口阈值", 0.5f, 5f, "m/s");
        changed |= DrawSlider(ref vehicle.lowSpeedCoastBrakeTorque, "低速尾速收口扭矩", 0f, 800f, "Nm");
        changed |= DrawSlider(ref vehicle.runtimeAngularDamping, "角阻尼", 0f, 8f, "");

        if (changed)
        {
            ApplyVehicleChanges(vehicle);
        }
    }

    private void DrawPhysicsSection(SimpleCarController vehicle)
    {
        GUILayout.Space(10f);
        GUILayout.Label("常用物理参数", sectionStyle);

        Rigidbody rb = vehicle.CachedRigidbody != null ? vehicle.CachedRigidbody : vehicle.GetComponent<Rigidbody>();
        bool changed = false;

        if (rb != null)
        {
            float mass = rb.mass;
            changed |= DrawSlider(ref mass, "车辆质量", 200f, 3000f, "kg");
            if (changed)
            {
                rb.mass = mass;
            }
        }

        Vector3 centerOfMass = vehicle.centerOfMassOffset;
        float centerY = centerOfMass.y;
        bool comChanged = DrawSlider(ref centerY, "重心 Y 偏移", -1.2f, 0.2f, "m");
        if (comChanged)
        {
            centerOfMass.y = centerY;
            vehicle.centerOfMassOffset = centerOfMass;
            changed = true;
        }

        changed |= DrawSlider(ref vehicle.bodyStaticFriction, "车身静摩擦系数", 0f, 1f, "");
        changed |= DrawSlider(ref vehicle.bodyDynamicFriction, "车身动摩擦系数", 0f, 1f, "");

        if (changed)
        {
            ApplyVehicleChanges(vehicle);
        }
    }

    private void DrawCollisionSection(SimpleCarController vehicle)
    {
        GUILayout.Space(10f);
        GUILayout.Label("低速碰撞/推车参数", sectionStyle);

        bool changed = false;
        changed |= DrawSlider(ref vehicle.inactiveLinearDamping, "非激活车线性阻尼", 0f, 0.5f, "");
        changed |= DrawSlider(ref vehicle.inactiveWheelDampingRate, "非激活车轮滚动阻力", 0f, 0.5f, "");
        changed |= DrawSlider(ref vehicle.inactiveForwardFrictionStiffness, "非激活车前后抓地力", 0.01f, 1f, "x");
        changed |= DrawSlider(ref vehicle.inactiveSidewaysFrictionStiffness, "非激活车侧向抓地力", 0.01f, 1f, "x");
        changed |= DrawSlider(ref vehicle.inactivePushAssistSpeed, "纵向推车单步上限", 0f, 3f, "m/s");
        changed |= DrawSlider(ref vehicle.inactivePushAssistMaxTargetSpeed, "推车目标速度上限", 0.5f, 8f, "m/s");
        changed |= DrawSlider(ref vehicle.inactivePushAssistImpulseThreshold, "纵向冲量触发阈值", 0f, 800f, "Ns");
        changed |= DrawSlider(ref vehicle.contactImpulseTransferScale, "冲量转速度倍率", 0.2f, 3f, "x");
        changed |= DrawSlider(ref vehicle.contactPushVelocityStep, "静止贴车推送步进", 0f, 0.6f, "m/s");
        changed |= DrawSlider(ref vehicle.contactStaticPushSpeedThreshold, "静止贴车判定阈值", 0.2f, 3f, "m/s");
        changed |= DrawSlider(ref vehicle.contactPushSpeedThreshold, "低速顶车速度阈值", 0.5f, 10f, "m/s");
        changed |= DrawSlider(ref vehicle.contactPushAlignmentThreshold, "顶车方向对齐阈值", 0.1f, 0.95f, "");
        changed |= DrawSlider(ref vehicle.bumperZoneThreshold, "保险杠区域阈值", 1f, 3f, "x");

        if (changed)
        {
            ApplyVehicleChanges(vehicle);
        }
    }

    private void DrawCameraSection()
    {
        if (vehicleManager == null || vehicleManager.CameraFollowComponent == null)
        {
            return;
        }

        CameraFollow follow = vehicleManager.CameraFollowComponent;
        GUILayout.Space(10f);
        GUILayout.Label("相机参数", sectionStyle);

        DrawSlider(ref follow.distance, "跟车距离", 3f, 20f, "m");
        DrawSlider(ref follow.height, "相机高度", 1f, 10f, "m");
        DrawSlider(ref follow.lookHeight, "注视点高度", 0f, 4f, "m");
        DrawSlider(ref follow.followSmoothTime, "位置平滑时间", 0.01f, 0.6f, "s");
        DrawSlider(ref follow.rotateSpeed, "旋转平滑速度", 1f, 15f, "");
        DrawSlider(ref follow.mouseSensitivity, "鼠标灵敏度", 0.01f, 0.5f, "");

        bool allowOrbit = GUILayout.Toggle(follow.allowMouseOrbit, "允许鼠标绕车旋转");
        if (allowOrbit != follow.allowMouseOrbit)
        {
            follow.allowMouseOrbit = allowOrbit;
        }

        bool leftHold = GUILayout.Toggle(follow.requireLeftMouseHold, "按住鼠标左键才允许转视角");
        if (leftHold != follow.requireLeftMouseHold)
        {
            follow.requireLeftMouseHold = leftHold;
        }
    }

    private void ApplyVehicleChanges(SimpleCarController vehicle)
    {
        vehicle.RefreshRuntimeConfiguration();
    }

    private bool DrawSlider(ref float value, string label, float min, float max, string unit)
    {
        float original = value;

        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value:F2} {unit}", GUILayout.Width(220f));
        value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(120f));
        GUILayout.EndHorizontal();

        return !Mathf.Approximately(original, value);
    }

    private void UpdateTelemetry(SimpleCarController currentVehicle)
    {
        if (lastTelemetryVehicle != currentVehicle)
        {
            lastTelemetryVehicle = currentVehicle;
            lastForwardSpeed = currentVehicle.CurrentForwardSpeed;
            telemetryTimer = 0f;
            displayedForwardAcceleration = 0f;
            displayedDriveForce = 0f;
            return;
        }

        telemetryTimer += Time.unscaledDeltaTime;
        if (telemetryTimer < telemetrySampleInterval)
        {
            return;
        }

        float sampleDuration = Mathf.Max(telemetryTimer, 0.0001f);
        telemetryTimer = 0f;

        float currentForwardSpeed = currentVehicle.CurrentForwardSpeed;
        float rawAcceleration = (currentForwardSpeed - lastForwardSpeed) / sampleDuration;
        if (Mathf.Abs(rawAcceleration) < telemetryDeadZone)
        {
            rawAcceleration = 0f;
        }

        displayedForwardAcceleration = Mathf.Lerp(displayedForwardAcceleration, rawAcceleration, telemetrySmoothing);

        Rigidbody rb = currentVehicle.CachedRigidbody != null ? currentVehicle.CachedRigidbody : currentVehicle.GetComponent<Rigidbody>();
        float rawDriveForce = rb != null ? rb.mass * displayedForwardAcceleration : 0f;
        if (Mathf.Abs(rawDriveForce) < telemetryDeadZone * 100f)
        {
            rawDriveForce = 0f;
        }

        displayedDriveForce = Mathf.Lerp(displayedDriveForce, rawDriveForce, telemetrySmoothing);
        lastForwardSpeed = currentForwardSpeed;
    }

    private float EstimateVehicleLengthMeters(Transform vehicleRoot)
    {
        Collider[] colliders = vehicleRoot.GetComponentsInChildren<Collider>(true);
        bool hasBounds = false;
        Bounds combinedBounds = default;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null || colliders[i].isTrigger || colliders[i] is WheelCollider)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = colliders[i].bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(colliders[i].bounds);
            }
        }

        if (!hasBounds)
        {
            Renderer[] renderers = vehicleRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!hasBounds)
                {
                    combinedBounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
            }
        }

        return hasBounds ? combinedBounds.size.z : 0f;
    }

    private void EnsureStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
        }

        if (sectionStyle == null)
        {
            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
        }

        if (noteStyle == null)
        {
            noteStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true
            };
        }
    }
}

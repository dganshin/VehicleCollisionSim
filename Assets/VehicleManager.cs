using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleManager : MonoBehaviour
{
    // vehicles 列表允许人工在 Inspector 指定顺序；运行时只补齐漏掉的车，不强行重排。
    public SimpleCarController[] vehicles; // 车辆列表，顺序以 Inspector 人工设置为准，运行时只补齐漏掉的车。
    public int currentVehicleIndex; // 当前由玩家控制的是列表中的第几辆车。
    public CameraFollow cameraFollow; // 用来跟随当前车辆的相机脚本。
    public bool autoDiscoverVehicles = true; // 是否在运行时扫描场景并自动注册车辆根对象。
    public bool keepVehicleTuningsSynchronized = true; // 是否在运行时把当前车辆的参数同步到其它车辆，避免每辆车单独调参。
    public bool logSwitches = true; // 是否输出车辆清单和切车日志，便于排查输入和注册问题。
    public float resetLift = 0.35f; // 复位车辆时额外抬高的高度，避免直接和地面重叠。

    private VehicleState[] initialStates;
    public SimpleCarController CurrentVehicle => vehicles != null && currentVehicleIndex >= 0 && currentVehicleIndex < vehicles.Length ? vehicles[currentVehicleIndex] : null;
    public int VehicleCount => vehicles != null ? vehicles.Length : 0;
    public CameraFollow CameraFollowComponent => cameraFollow;

    [Serializable]
    private struct VehicleState
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    private void Awake()
    {
        // 先补齐场景里漏挂的车辆组件，再做车辆列表合并。
        EnsureVehicleSetup();

        if (autoDiscoverVehicles)
        {
            MergeDiscoveredVehicles();
        }

        NormalizeVehicleTunings();

        if (GetComponent<VehicleRuntimeUI>() == null)
        {
            gameObject.AddComponent<VehicleRuntimeUI>();
        }

        if (cameraFollow == null)
        {
            cameraFollow = GetComponent<CameraFollow>();
            if (cameraFollow == null)
            {
                cameraFollow = FindFirstObjectByType<CameraFollow>();
            }
        }

        CacheInitialStates();

        if (logSwitches && vehicles != null && vehicles.Length > 0)
        {
            // 这条日志主要用于排查“为什么按 2 还是切不到第二辆车”。
            Debug.Log($"Vehicle roster: {string.Join(", ", Array.ConvertAll(vehicles, vehicle => vehicle != null ? vehicle.name : "null"))}");
        }

        if (vehicles != null && vehicles.Length > 0)
        {
            SetCurrentVehicle(Mathf.Clamp(currentVehicleIndex, 0, vehicles.Length - 1), true);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || vehicles == null || vehicles.Length == 0)
        {
            return;
        }

        EnforceSingleControlledVehicle();
        SyncVehicleTuningsFromCurrent();
        HandleVehicleSwitchInput(keyboard);
        HandleResetInput(keyboard);
    }

    private void HandleVehicleSwitchInput(Keyboard keyboard)
    {
        if (keyboard.digit1Key.wasPressedThisFrame) TrySwitchVehicle(0, "digit1");
        if (keyboard.digit2Key.wasPressedThisFrame) TrySwitchVehicle(1, "digit2");
        if (keyboard.digit3Key.wasPressedThisFrame) TrySwitchVehicle(2, "digit3");
        if (keyboard.digit4Key.wasPressedThisFrame) TrySwitchVehicle(3, "digit4");
        if (keyboard.digit5Key.wasPressedThisFrame) TrySwitchVehicle(4, "digit5");
        if (keyboard.numpad1Key.wasPressedThisFrame) TrySwitchVehicle(0, "numpad1");
        if (keyboard.numpad2Key.wasPressedThisFrame) TrySwitchVehicle(1, "numpad2");
        if (keyboard.numpad3Key.wasPressedThisFrame) TrySwitchVehicle(2, "numpad3");
        if (keyboard.numpad4Key.wasPressedThisFrame) TrySwitchVehicle(3, "numpad4");
        if (keyboard.numpad5Key.wasPressedThisFrame) TrySwitchVehicle(4, "numpad5");
    }

    private void HandleResetInput(Keyboard keyboard)
    {
        if (!keyboard.rKey.wasPressedThisFrame)
        {
            return;
        }

        bool resetAll = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        if (resetAll)
        {
            ResetAllVehicles();
            return;
        }

        ResetVehicle(currentVehicleIndex);
    }

    public void SetCurrentVehicle(int index)
    {
        SetCurrentVehicle(index, false);
    }

    private void TrySwitchVehicle(int index, string source)
    {
        int vehicleCount = vehicles != null ? vehicles.Length : 0;
        string targetName = index >= 0 && index < vehicleCount && vehicles[index] != null ? vehicles[index].name : "null";
        // 保留切车请求日志，方便直接从 Console 判断是输入问题还是列表里根本没有那辆车。
        Debug.Log($"Switch request from {source}: index={index}, count={vehicleCount}, target={targetName}");
        SetCurrentVehicle(index, false);
    }

    public void ResetCurrentVehicle()
    {
        ResetVehicle(currentVehicleIndex);
    }

    public void ResetAllVehicles()
    {
        if (vehicles == null)
        {
            return;
        }

        for (int i = 0; i < vehicles.Length; i++)
        {
            ResetVehicle(i, false);
        }

        if (cameraFollow != null)
        {
            cameraFollow.SnapToTarget();
        }
    }

    private void SetCurrentVehicle(int index, bool silent)
    {
        if (vehicles == null || vehicles.Length == 0 || index < 0 || index >= vehicles.Length)
        {
            return;
        }

        currentVehicleIndex = index;
        EnforceSingleControlledVehicle();

        SimpleCarController currentVehicle = vehicles[currentVehicleIndex];
        if (currentVehicle != null && cameraFollow != null)
        {
            cameraFollow.target = currentVehicle.transform;
            cameraFollow.SnapToTarget();
        }

        if (!silent && logSwitches && currentVehicle != null)
        {
            Debug.Log($"Current vehicle: {currentVehicle.name}");
        }
    }

    private void EnforceSingleControlledVehicle()
    {
        if (vehicles == null || vehicles.Length == 0)
        {
            return;
        }

        for (int i = 0; i < vehicles.Length; i++)
        {
            if (vehicles[i] == null)
            {
                continue;
            }

            // 每帧收口一次控制权，防止运行时动态补挂的车辆保留默认可控状态。
            vehicles[i].SetControlled(i == currentVehicleIndex);
        }
    }

    private void CacheInitialStates()
    {
        if (vehicles == null)
        {
            initialStates = Array.Empty<VehicleState>();
            return;
        }

        initialStates = new VehicleState[vehicles.Length];
        for (int i = 0; i < vehicles.Length; i++)
        {
            if (vehicles[i] == null)
            {
                continue;
            }

            initialStates[i] = new VehicleState
            {
                position = vehicles[i].transform.position,
                rotation = vehicles[i].transform.rotation
            };
        }
    }

    private void ResetVehicle(int index, bool snapCamera = true)
    {
        if (vehicles == null || initialStates == null || index < 0 || index >= vehicles.Length || index >= initialStates.Length)
        {
            return;
        }

        SimpleCarController vehicle = vehicles[index];
        if (vehicle == null)
        {
            return;
        }

        Rigidbody rb = vehicle.CachedRigidbody != null ? vehicle.CachedRigidbody : vehicle.GetComponent<Rigidbody>();
        VehicleState state = initialStates[index];

        vehicle.transform.SetPositionAndRotation(state.position + Vector3.up * resetLift, state.rotation);
        vehicle.RefreshCenterOfMass();

        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
            rb.WakeUp();
        }

        if (snapCamera && cameraFollow != null && index == currentVehicleIndex)
        {
            cameraFollow.SnapToTarget();
        }
    }

    private void MergeDiscoveredVehicles()
    {
        EnsureVehicleSetup();

        SimpleCarController[] discoveredVehicles = FindVehicleControllers();

        if (discoveredVehicles == null || discoveredVehicles.Length == 0)
        {
            vehicles = Array.Empty<SimpleCarController>();
            return;
        }

        SimpleCarController[] existingVehicles = vehicles ?? Array.Empty<SimpleCarController>();
        SimpleCarController[] mergedVehicles = new SimpleCarController[existingVehicles.Length + discoveredVehicles.Length];
        int count = 0;

        for (int i = 0; i < existingVehicles.Length; i++)
        {
            if (existingVehicles[i] == null)
            {
                continue;
            }

            mergedVehicles[count++] = existingVehicles[i];
        }

        for (int i = 0; i < discoveredVehicles.Length; i++)
        {
            if (discoveredVehicles[i] == null || Array.IndexOf(mergedVehicles, discoveredVehicles[i], 0, count) >= 0)
            {
                continue;
            }

            mergedVehicles[count++] = discoveredVehicles[i];
        }

        Array.Resize(ref mergedVehicles, count);
        Array.Sort(mergedVehicles, CompareVehiclesByName);
        vehicles = mergedVehicles;
    }

    private static int CompareVehiclesByName(SimpleCarController left, SimpleCarController right)
    {
        string leftName = left != null ? left.name : string.Empty;
        string rightName = right != null ? right.name : string.Empty;
        return string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase);
    }

    private SimpleCarController[] FindVehicleControllers()
    {
        // 不直接按 SimpleCarController 全场景查找，而是先按 Rigidbody 根对象识别“像车的对象”。
        Rigidbody[] rigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        SimpleCarController[] foundControllers = new SimpleCarController[rigidbodies.Length];
        int count = 0;

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rb = rigidbodies[i];
            if (rb == null || !LooksLikeVehicleRoot(rb.gameObject))
            {
                continue;
            }

            SimpleCarController controller = rb.GetComponent<SimpleCarController>();
            if (controller == null)
            {
                continue;
            }

            foundControllers[count++] = controller;
        }

        Array.Resize(ref foundControllers, count);
        return foundControllers;
    }

    private void EnsureVehicleSetup()
    {
        Rigidbody[] rigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rb = rigidbodies[i];
            if (rb == null || !LooksLikeVehicleRoot(rb.gameObject))
            {
                continue;
            }

            if (rb.GetComponent<SimpleCarController>() == null)
            {
                SimpleCarController addedController = rb.gameObject.AddComponent<SimpleCarController>();
                SimpleCarController templateController = FindTemplateController(addedController);
                if (templateController != null)
                {
                    // 新接入的车辆尽量复制当前主车的调参结果，避免两辆车一边吃场景覆盖值、一边吃脚本默认值。
                    addedController.CopyTuningFrom(templateController);
                }
            }

            if (rb.GetComponent<WheelVisualRotator>() == null)
            {
                rb.gameObject.AddComponent<WheelVisualRotator>();
            }
        }
    }

    private bool LooksLikeVehicleRoot(GameObject candidate)
    {
        if (candidate == null || !candidate.activeInHierarchy)
        {
            return false;
        }

        if (candidate.GetComponentInChildren<WheelCollider>(true) != null)
        {
            return true;
        }

        if (candidate.transform.Find("Wheels") != null)
        {
            return true;
        }

        // 名字匹配只作为最后兜底，不作为主判断依据。
        string lowerName = candidate.name.ToLowerInvariant();
        return lowerName.Contains("car") || lowerName.Contains("bee");
    }

    private SimpleCarController FindTemplateController(SimpleCarController exclude)
    {
        if (vehicles != null)
        {
            for (int i = 0; i < vehicles.Length; i++)
            {
                if (vehicles[i] != null && vehicles[i] != exclude)
                {
                    return vehicles[i];
                }
            }
        }

        SimpleCarController[] controllers = FindObjectsByType<SimpleCarController>(FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null && controllers[i] != exclude)
            {
                return controllers[i];
            }
        }

        return null;
    }

    private void NormalizeVehicleTunings()
    {
        if (vehicles == null || vehicles.Length <= 1)
        {
            return;
        }

        SimpleCarController template = null;
        for (int i = 0; i < vehicles.Length; i++)
        {
            if (vehicles[i] != null)
            {
                template = vehicles[i];
                break;
            }
        }

        if (template == null)
        {
            return;
        }

        for (int i = 0; i < vehicles.Length; i++)
        {
            if (vehicles[i] == null || vehicles[i] == template)
            {
                continue;
            }

            // 运行时统一把其他车辆的动力和刚体基线对齐到主车，避免不同车辆手感明显不一致。
            vehicles[i].CopyTuningFrom(template);
        }
    }

    private void SyncVehicleTuningsFromCurrent()
    {
        if (!keepVehicleTuningsSynchronized || vehicles == null || vehicles.Length <= 1)
        {
            return;
        }

        SimpleCarController template = CurrentVehicle;
        if (template == null)
        {
            return;
        }

        for (int i = 0; i < vehicles.Length; i++)
        {
            if (vehicles[i] == null || vehicles[i] == template)
            {
                continue;
            }

            // 当前受控车辆就是调参模板：无论是 Inspector 改值还是运行时 UI 改值，其它车辆都会在下一帧跟上。
            vehicles[i].CopyTuningFrom(template);
        }
    }
}

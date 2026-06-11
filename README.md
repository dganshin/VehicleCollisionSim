# 基于 Unity 的车辆碰撞虚拟仿真与物理参数可视化系统

本项目是车辆碰撞虚拟仿真课程设计项目，目标是在 Unity 中搭建一个可运行、可演示、可验证的车辆碰撞仿真系统。



## 1. 项目功能概述

当前项目已经实现的核心功能包括：

- 车辆基础驱动：前进、后退、转向、刹车。
- 多车辆管理：支持运行时切换当前控制车辆。
- 车辆与车辆碰撞：可演示车辆之间的基础物理碰撞。
- 车辆与环境对象碰撞：可与城市道路、马路牙子、人行道柱、桥梁、建筑、树木等对象发生物理碰撞。
- 植物碰撞演示：场景中额外放置 3 棵 `CrashTestTree` 测试树，车辆撞击后可触发倒伏或位移效果。
- 运行时参数面板：可查看并调节驱动力、刹车、阻尼、摩擦、质量、重心等常用物理参数。
- 物理量可视化：显示速度、前向速度、估算加速度、估算纵向合力、质量、重力加速度、估算车长等读数。



## 2. 环境要求

建议使用与本仓库一致的环境打开项目，避免 Unity 版本差异导致资源导入、脚本编译或渲染管线异常。

| 项目 | 要求 |
| --- | --- |
| 操作系统 | Windows 11 |
| Unity Hub | 建议安装 |
| Unity Editor | `6000.3.10f1` |
| 脚本 IDE | Visual Studio 2022 或 JetBrains Rider，非必须 |
| 输入系统 | Unity Input System |
| 渲染管线 | Universal Render Pipeline |

Unity 精确版本记录在：

```text
ProjectSettings/ProjectVersion.txt
```

当前项目版本为：

```text
6000.3.10f1
```

## 3. 获取代码

如果通过 Git 获取项目：

```powershell
git clone <仓库地址>
cd VehicleCollisionSim
```

如果通过压缩包获取项目，请解压后进入项目根目录。项目根目录应包含以下关键目录：

```text
Assets/
Packages/
ProjectSettings/
README.md
VehicleCollisionSim.sln
```


## 4. 首次打开项目

1. 打开 Unity Hub。
2. 点击 `Add` 或 `添加项目`。
3. 选择本仓库根目录，例如：

```text
D:\BaiduSyncdisk\repo\VehicleCollisionSim
```

4. 使用 Unity Editor `6000.3.10f1` 打开项目。
5. 等待 Unity 完成首次导入、脚本编译和包恢复。
6. 如果 Unity 弹出是否启用 New Input System 的提示，请选择启用，并按提示重启 Unity Editor。
7. 等待 `Console` 中没有红色编译错误后，再运行场景。

首次打开项目可能需要几分钟，这是 Unity 导入资源和恢复依赖的正常过程。

## 5. 依赖包说明

项目依赖由 Unity 的包管理文件维护：

```text
Packages/manifest.json
```

首次打开项目时，Unity 会自动根据该文件恢复依赖。主要依赖包括：

- `com.unity.inputsystem`：新输入系统，用于键盘输入。
- `com.unity.ugui`：运行时 UI 面板。
- `com.unity.render-pipelines.universal`：URP 渲染管线。
- `com.unity.test-framework`：Unity 测试框架。
- `com.unity.modules.physics`：Unity 3D 物理系统。
- `com.unity.modules.vehicles`：车辆相关 Unity 模块。

正常情况下不需要手动逐个安装这些包。如果包恢复失败，请先检查网络连接和 `Packages/manifest.json` 是否存在。

## 6. 运行项目

按照以下步骤运行验收场景：

1. 在 Unity Project 面板中打开场景：

```text
Assets/Versatile Studio Assets/Demo City By Versatile Studio/Scenes/demo_city_night.unity
```

2. 确认场景层级中存在以下关键对象：

```text
Main Camera
Car1
Car2
Car3
Car4
VehicleRuntimeCameraAndManager
CollisionTestObjects
```

3. 点击 Unity 顶部的 `Play` 按钮进入运行模式。
4. 点击 `Game` 窗口，使键盘输入焦点进入游戏窗口。
5. 按下操作键进行车辆控制和碰撞测试。

如果按键没有反应，通常是因为 `Game` 窗口没有获得焦点，请先点击 `Game` 窗口再操作。

## 7. 操作说明

| 操作 | 按键 |
| --- | --- |
| 前进 | `W` |
| 后退 | `S` |
| 左转 | `A` |
| 右转 | `D` |
| 刹车 | `Space` |
| 切换车辆 | `1`、`2`、`3`、`4` |
| 重置当前车辆 | `R` |
| 重置全部车辆 | `Shift + R` |
| 显示或隐藏参数面板 | `F2` |
| 鼠标绕车观察 | 移动鼠标 |

当前场景中已配置 `Car1`、`Car2`、`Car3`、`Car4` 四辆车，对应使用 `1`、`2`、`3`、`4` 进行车辆切换。

## 8. 验收建议流程

建议按以下顺序验证项目功能：

1. 启动 `Assets/Versatile Studio Assets/Demo City By Versatile Studio/Scenes/demo_city_night.unity`。
2. 点击 `Play`，进入运行模式。
3. 点击 `Game` 窗口获取输入焦点。
4. 使用 `W/S/A/D` 驾驶当前车辆，确认车辆可以前进、后退和转向。
5. 使用 `Space` 刹车，确认车辆可减速或停止。
6. 使用 `1`、`2`、`3`、`4` 切换车辆，确认多车管理功能可用。
7. 驾驶车辆撞向另一辆车，验证车车物理碰撞。
8. 驾驶车辆撞向马路牙子、人行道柱、桥梁、建筑等静态环境对象，验证车辆与环境的物理碰撞。
9. 驾驶车辆撞向 `CollisionTestObjects` 下的 `CrashTestTree_01`、`CrashTestTree_02`、`CrashTestTree_03`，验证车辆与植物对象的碰撞和倒伏效果。
10. 按 `F2` 打开参数面板，调整驱动力、刹车、阻尼、摩擦、质量等参数，观察车辆运动变化。
11. 观察参数面板中的速度、前向速度、估算加速度、估算纵向合力、质量、重力加速度和估算车长等读数。

更详细的测试用例见：

```text
docs/test_cases.md
```

## 9. 课程要求对应关系

| 课程要求 | 当前实现状态 |
| --- | --- |
| 系统至少包含 3-5 辆汽车 | 当前场景已配置 4 辆车：`Car1`、`Car2`、`Car3`、`Car4` |
| 车辆均可以实现驱动 | 已实现基础驱动，并支持当前车辆切换 |
| 实现车辆与车辆的物理碰撞 | 已具备基础车车碰撞效果，可用于课程演示 |
| 实现车辆与其它对象的物理碰撞 | 已实现车辆与城市道路、马路牙子、人行道柱、桥梁、建筑和 3 棵可撞倒测试树的物理碰撞 |
| UI 界面包含运动驱动、力和常用物理参数设置及可视化 | 已实现运行时参数面板和基础物理量可视化 |

当前车辆数量已满足“3-5 辆汽车”的课程要求。若后续继续增删车辆，需要同步更新场景和本 README。

## 10. 项目结构

```text
VehicleCollisionSim/
├─ Assets/
│  ├─ Scenes/
│  │  └─ CollisionTestScene.unity
│  ├─ Versatile Studio Assets/
│  │  └─ Demo City By Versatile Studio/
│  │     └─ Scenes/
│  │        └─ demo_city_night.unity
│  ├─ SimpleCarController.cs
│  ├─ VehicleManager.cs
│  ├─ CameraFollow.cs
│  ├─ VehicleRuntimeUI.cs
│  ├─ WheelVisualRotator.cs
│  ├─ TestFieldGenerator.cs
│  ├─ DestructibleObstacle.cs
│  ├─ CollisionObjectLabel.cs
│  ├─ Editor/
│  │  ├─ InstallTunedVehiclesIntoDemoCity.cs
│  │  └─ InstallCrashTestTree.cs
│  └─ Scripts/
│     └─ Physics/
│        └─ CrashableTree.cs
├─ Packages/
│  └─ manifest.json
├─ ProjectSettings/
│  └─ ProjectVersion.txt
├─ docs/
│  ├─ ai_usage_log.md
│  ├─ dev_timeline.md
│  ├─ test_cases.md
│  ├─ module_assignment.md
│  └─ final_delivery_checklist.md
└─ README.md
```

## 11. 关键脚本说明

| 文件 | 作用 |
| --- | --- |
| `Assets/SimpleCarController.cs` | 单车控制、车辆驱动、刹车、物理参数处理 |
| `Assets/VehicleManager.cs` | 多车切换、车辆复位、运行时参数同步 |
| `Assets/CameraFollow.cs` | 第三人称跟车相机与鼠标视角控制 |
| `Assets/VehicleRuntimeUI.cs` | 运行时参数面板和物理量显示 |
| `Assets/WheelVisualRotator.cs` | 轮子视觉旋转 |
| `Assets/TestFieldGenerator.cs` | 自动生成环境碰撞测试场 |
| `Assets/DestructibleObstacle.cs` | 动态障碍轻量碰撞反馈 |
| `Assets/CollisionObjectLabel.cs` | 环境对象命名与碰撞日志辅助 |
| `Assets/Scripts/Physics/CrashableTree.cs` | 可撞倒测试树逻辑，用于演示车辆与植物碰撞 |
| `Assets/Editor/InstallTunedVehiclesIntoDemoCity.cs` | 将调好参数的车辆安装到城市场景的辅助 Editor 脚本 |
| `Assets/Editor/InstallCrashTestTree.cs` | 在城市场景中放置可碰撞测试树的辅助 Editor 脚本 |

## 12. 资源来源

- 车辆资源：`ARCADE - FREE Racing Car`
- 城市与高速环境：`Demo City By Versatile Studio`
- 备用基础测试场：`Assets/Scenes/CollisionTestScene.unity`

当前推荐验收场景是完整城市/高速道路场景，课程验收时建议重点展示车辆控制、车车碰撞、车辆与树木/桥梁/路沿等环境对象碰撞，以及运行时物理参数可视化。

## 13. 常见问题

### 13.1 项目打不开或打开后报错

优先检查 Unity Editor 版本是否为 `6000.3.10f1`。如果使用其它 Unity 6 小版本，可能可以打开，但存在导入差异风险。

### 13.2 首次打开项目很慢

首次打开时 Unity 会导入资源、生成 `Library` 缓存并恢复依赖包，耗时较长是正常现象。等待导入完成后再运行场景。

### 13.3 按键没有反应

先点击 `Game` 窗口，使游戏窗口获得输入焦点。然后再使用 `W/S/A/D`、`Space`、`1/2/3/4`、`R`、`F2` 等按键。

### 13.4 参数面板没有显示

运行场景后按 `F2` 显示或隐藏参数面板。也可以检查场景中是否存在挂载 `VehicleManager` 和 `VehicleRuntimeUI` 的对象。

### 13.5 依赖包缺失

确认以下文件存在：

```text
Packages/manifest.json
```

然后重新打开 Unity 项目，等待 Unity Package Manager 自动恢复依赖。

### 13.6 场景打开后没有测试环境

确认打开的是：

```text
Assets/Versatile Studio Assets/Demo City By Versatile Studio/Scenes/demo_city_night.unity
```

并确认场景中存在：

```text
Car1
Car2
Car3
Car4
VehicleRuntimeCameraAndManager
CollisionTestObjects
```

如果只想运行备用基础测试场，也可以打开 `Assets/Scenes/CollisionTestScene.unity`。

## 14. 已知限制

- 当前版本以课程演示和功能闭环为目标，车辆动力学为简化实现。
- 当前场景已配置 4 辆车，满足课程要求中的 3-5 辆汽车数量。
- 碰撞表现以 Unity 物理系统和演示稳定性为主，不包含工业级真实车损形变。
- 下载的城市环境中，马路牙子、人行道柱、路灯、建筑、桥梁等主要作为静态碰撞体使用，不做整体可破坏或可撞飞效果。
- 当前仅额外配置 3 棵可撞倒测试树用于“车辆与植物对象碰撞”演示。
- 项目没有专门制作复杂材质、光线追踪或真实车损形变，展示重点是车辆动力学、碰撞和物理参数可视化。

## 15. AI / Code Agent 使用说明

本项目允许并鼓励使用 AI / Code Agent 辅助开发。按照课程要求，AI 使用过程需要在 PPT 和结题报告中说明，避免被视为未说明的代写或作弊。

当前 AI / Code Agent 主要用于：

- 辅助排查 Unity 脚本和物理参数问题。
- 生成和整理脚本初稿。
- 辅助整理测试清单。
- 辅助整理 README、PPT 和报告底稿。

详细记录见：

```text
docs/ai_usage_log.md
```

## 16. 最终提交前检查清单

提交前建议至少检查以下内容：

- `README.md` 中的 Unity 版本、场景路径、按键说明与当前项目一致。
- `Assets/Versatile Studio Assets/Demo City By Versatile Studio/Scenes/demo_city_night.unity` 可以正常打开并运行。
- `Console` 中没有红色编译错误。
- 车辆可以驱动、切换、重置。
- 车车碰撞和车辆与环境碰撞可以演示。
- 3 棵 `CrashTestTree` 可以被车辆撞动或撞倒，且不会穿模或飞出地图。
- `F2` 参数面板可以打开，参数调节和物理量显示可用。
- 如果后续继续增删车辆，需要更新本 README 的车辆数量和切换按键说明。
- PPT 和结题报告中写清楚 AI / Code Agent 的具体使用过程。

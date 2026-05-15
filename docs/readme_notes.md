# README 编写要点

说明：本文件用于整理最终 README 应包含的内容，后续可据此汇总成项目主页文档。

## README 建议结构

1. 项目名称
   基于 Unity 的车辆碰撞虚拟仿真与物理参数可视化系统

2. 项目简介
   简要说明项目目标、课程背景、主要功能和展示价值。

3. 技术路线与方案选择
   说明早期调研过 PyBullet、UE5、Unity。
   说明最终选择 Unity 的原因：
   - 资源生态成熟
   - 适合课程周期
   - 场景、交互、碰撞、UI 实现成本较平衡

4. 前期调研与方案取舍
   建议简要写明：
   - PyBullet 原型验证快，但观感和驾驶感不足
   - UE5 表现强，但工具链复杂、短期课程实现成本高
   - Unity 在开发效率和展示效果之间更均衡

5. 开发环境
   - Unity 版本
   - 目标平台
   - 主要包依赖
   - 是否启用新 Input System

6. 资源来源
   - 车辆资源：`ARCADE - FREE Racing Car`
   - 其他模型、贴图、音效、UI 资源来源
   - 若有第三方资源，标明授权或来源链接

7. 当前已实现功能
   - 基础车辆控制
   - 基础相机跟随
   - 场景碰撞测试
   - 参数调节或可视化功能（完成后补充）

8. 项目结构说明
   - `Assets/` 主要脚本与场景位置
   - `docs/` 文档记录用途
   - 关键场景、预制体、脚本说明

9. 运行方法
   - 打开工程
   - 打开指定场景，如 `CollisionTestScene`
   - 确认 `bigyellowbee` 根对象挂有 `Rigidbody + SimpleCarController`
   - 确认 `Main Camera` 挂有 `CameraFollow` 且 `target` 指向车辆
   - 点击 Play
   - 点击 Game 窗口
   - 使用 `W/S/A/D/Space` 测试

10. 已解决的典型问题
   - 新 Input System 与 `Input.GetAxis` 冲突
   - 材质粉色问题的临时兼容处理
   - 大质量车体下默认力模式运动不明显，改为 `ForceMode.Acceleration`

11. 测试说明
   - 车辆移动测试
   - 碰撞测试
   - UI 参数调节测试
   - Reset 测试
   - README 复现测试

12. AI/Code Agent 使用说明
   建议明确写明：
   - AI 用于辅助排查问题、生成脚本初稿、提供调试建议、整理开发文档
   - 项目技术路线、资源选择、集成、调参、测试与最终定稿由人工完成
   - 详细过程见 `docs/ai_usage_log.md`

13. 开发记录与答辩准备
   README 或附录中建议明确说明开发记录的组织方式，便于老师追问时快速定位：
   - `docs/ai_usage_log.md`
     记录“问题现象、怀疑原因、检查文件、修改文件、修改前后、人工工作、AI辅助、测试结果、遗留问题”
   - `docs/dev_timeline.md`
     记录“项目关键节点、路线取舍、为什么放弃某些方案”
   - `docs/test_cases.md`
     记录“每轮修改后如何验证”
   建议在答辩中强调：
   - 人工先提出目标和怀疑点
   - AI 负责辅助查代码、生成初稿、整理思路
   - 人工负责在 Unity 中集成、观察现象、调参、确认最终是否采用

14. 典型问题案例建议
   README 或结题报告中可单独挑 2~4 个典型问题做“修改前/修改后”展示，例如：
   - `Input.GetAxis` 与新 Input System 冲突
   - `VehicleManager` 中 `Car2` 未进入车辆列表，导致 `count=1 target=null`
   - 黄车场景实例埋地，涉及 `WheelCollider`、`BoxCollider` 和启动离地校正
   - 由“车身硬推”回调到“最简轮驱动”的控制逻辑变化

15. 小组分工
   可引用 `docs/module_assignment.md` 中的最终版本。

16. 后续优化方向
   - 增强碰撞强度分级
   - 增加动态障碍物
   - 完善 UI 可视化
   - 补充更多车辆和场景

## 当前版本建议写成的“已实现功能”

- 车辆控制：支持 `W/S/A/D/Space`
- 多车切换：支持 `1-5` 切换当前车辆
- 车辆复位：支持当前车辆复位和全部车辆复位
- 跟车视角：支持第三人称跟车和鼠标绕车
- 运行时参数面板：支持驱动力、阻尼、摩擦等参数调节与读数显示
- 车车碰撞：已具备基础物理碰撞效果
- 车与环境碰撞：已具备墙体、建筑、桥墩、树木、动态箱子、路障、交通锥等碰撞测试场

## 当前版本建议写成的“仍在完善内容”

- 场景车辆数量补齐到 `3-5` 辆
- 车车碰撞在中低速区间的展示效果继续收尾
- 环境 primitive 场景可继续替换为更完整的材质包或模型包
- 碰撞强度分级和碰撞结果展示可进一步增强

## README 必须补充的运行说明

- 打开场景：`Assets/Scenes/CollisionTestScene.unity`
- 运行前确认：
  - `Main Camera` 上有 `CameraFollow`、`VehicleManager`、`VehicleRuntimeUI`
  - 场景中至少有 `bigyellowbee`、`Car2`
  - `TestFieldManager` 存在，Play 后会生成 `TestFieldRoot`
- 键位说明：
  - `W/S` 前进后退
  - `A/D` 转向
  - `Space` 刹车
  - `1-5` 切车
  - `R` 重置当前车辆
  - `Shift+R` 重置全部车辆
  - `F2` 显示/隐藏参数面板

## README 中建议加入的答辩口径

- 本项目采用 Unity 6 的课程级简化车辆动力学模型，以 `WheelCollider + Rigidbody + 参数补偿` 为主线。
- 项目重点在于完成多车辆驱动、车辆与车辆碰撞、车辆与环境对象碰撞，以及物理参数的运行时可视化。
- 对于极端真实的轮胎模型、车损形变和高保真碰撞细节，本项目不作为主要目标，优先保证课程展示稳定性和可解释性。

## README 中建议单独写一节“当前还差什么”

- 补齐 `3-5` 辆车
- 完成最终 README 截图和运行截图
- 录制或整理中低速/高速碰撞演示截图
- 将 `docs/` 中的 AI 使用记录转写到 PPT 和结题报告
- 如有时间，替换环境材质或补充更完整场景资源

## README 中建议附上的关键文件说明

- `Assets/SimpleCarController.cs`
  单车控制主线
- `Assets/VehicleManager.cs`
  多车切换、复位、运行时参数同步
- `Assets/CameraFollow.cs`
  跟车相机
- `Assets/VehicleRuntimeUI.cs`
  运行时参数面板
- `Assets/WheelVisualRotator.cs`
  轮子视觉旋转
- `Assets/TestFieldGenerator.cs`
  环境碰撞测试场生成
- `Assets/DestructibleObstacle.cs`
  轻量破坏反馈
- `Assets/CollisionObjectLabel.cs`
  环境对象碰撞命名与日志
- `docs/ai_usage_log.md`
  AI 辅助开发过程记录
- `docs/test_cases.md`
  测试用例与验证路径
- `docs/module_assignment.md`
  当前分工和剩余任务

## 当前项目剩余任务建议

按优先级：
1. 补齐车辆数量到 `3-5`
2. 收尾车车碰撞展示效果
3. 完成 README 正式版
4. 完成 PPT 和结题报告
5. 如有时间，再做环境材质和视觉增强

## 当前补充提示

- README 不要写成“AI 独立完成项目”。
- 建议在 README 中加入运行截图、碰撞截图和参数面板截图。
- 建议把“已实现功能”和“待完成功能”分开写，避免夸大当前进度。

# CakeHome (tilematch) 项目知识库

> Unity 2022.3.62f3 | HybridCLR 热更新 | Watermelon Core 框架  
> 蛋糕主题三消休闲手游

---

## 一、项目概况

- **产品名**: tilematch (蛋糕主题休闲消除手游)
- **引擎版本**: Unity 2022.3.62f3
- **热更方案**: HybridCLR (`Assets/Scripts/LoadDll.cs` → `Assets/HotUpdate/Entry.cs`)
- **核心框架**: Watermelon Core (自研框架，含 Audio/Currency/Monetization/Pool/Tween/UI/Save 等模块)
- **开发状态**: 早期开发中，核心玩法已完成，商店/Hub/关卡地图系统已接入；2026-08-22 完成 Watermelon Core 大版本升级 + Merge Adventure 模板资源整合（见第十六章）
- **设计文档索引**: `KNOWLEDGE.md` 的最后一章收录了 4 份 GDD 设计文档摘要
- **产品愿景**: 3D DIY 蛋糕 + 露营车消消乐 + 门店经营换装 Hybrid-Casual 手游
- **项目路径**: `D:\claudeWorkbase\cakehome\`
- **场景**: Game.unity (主场景)

---

## 二、目录结构

> 📍 **全量资源定位速查（场景/UI/美术/音频/数据/字体/代码/工具）见 [§21 全量资源地图](#二十一全量资源地图2026-09-10-整理定位用速查)** —— 用户提到某类资源时直接跳该节。

```
Assets/
├── Editor/                          # Editor 工具（AOT 编译期）
│   └── HotUpdateBuildProcessor.cs   # HybridCLR 构建流程
├── HotUpdate/                       # 热更新程序集（核心逻辑）
│   ├── Entry.cs                     # 热更入口
│   ├── GameData.cs                  # 游戏全局配置 ScriptableObject
│   ├── CustomAnalytics.cs           # 分析（占位）
│   ├── GuestRegistration.cs         # 游客注册 + 奖励
│   ├── Scripts/
│   │   ├── Controllers/             # 核心控制器
│   │   ├── Editor/                  # Editor 工具（热更侧）
│   │   ├── Hub/                     # 底部导航栏模块系统
│   │   ├── IAP Store/               # 内购商店
│   │   ├── Level/                   # 关卡核心（玩法/数据/场/特效/动画/编辑器）
│   │   ├── Level Map/               # 关卡大地图
│   │   ├── Lives System/            # 能量门票适配层 + 无限模式（旧命数体系已删，见第八章）
│   │   ├── Other/                   # 背景、开发者面板
│   │   ├── Power Ups/               # 道具具体实现
│   │   ├── Power Ups System/        # 道具框架（控制器/UI/数据）
│   │   ├── Settings/                # 设置界面
│   │   ├── Merge/                   # 合成玩法桥接（碎片掉落钩子/兑换面板/入口按钮）
│   │   ├── Tutorial/                # 新手引导框架
│   │   └── UI/                      # 游戏 UI 页面
├── Project Files/
│   ├── Data/                        # ScriptableObject 数据资产
│   └── Game/                        # 游戏资产（动画/音频/字体/图片/材质/模型/预制体/场景）
│       └── Art/                     # ★ 美术资源归集区（2026-09-10 建立，换皮唯一入口）
│           ├── _MANIFEST.md         # 资源索引（分组/尺寸/原始路径）
│           ├── Gameplay/            # Pieces 棋子64 / Tiles 瓦片 / Chests / Characters / PowerUps / Dock / Misc
│           ├── UI/                  # Common 通用 / Icons 图标 / Store 商店 / Tutorial 引导
│           ├── Scene/               # Backgrounds / Zone1 / Zone2 / Zone3
│           ├── Dialog/  Map/  Fx/  Fonts/
│           ├── _Pending/            # 已生成待接入（Zone1Shop 烘焙店素材）
│           └── _Unused/             # 未被任何 prefab/scene 引用的孤儿资源
├── Scripts/
│   └── LoadDll.cs                   # HybridCLR dll 加载器
├── StreamingAssets/HotUpdate/       # 热更新 dll 存放目录
└── Watermelon Core/                 # 核心框架（以下为模块）
    └── Modules/
        ├── Audio/                   # 音频管理
        ├── Currency/                # 货币系统
        ├── Defines/                 # 全局定义
        ├── Haptic/                  # 触觉反馈
        ├── Initializer/             # 游戏启动初始化流程
        ├── Inspector/               # 自定义 Inspector
        ├── Monetization/            # 广告 + IAP
        ├── Pool/                    # 对象池
        ├── Reward/                  # 奖励系统
        ├── Save/                    # 存档系统（ISaveObject）
        ├── Skins/                   # 皮肤系统
        ├── Tween/                   # 补间动画引擎
        └── UI/                      # UI 页面管理框架
```

---

## 三、核心架构

### 3.1 启动流程

```
LoadDll.Start()
  → 加载 AOT 补充元数据（mscorlib/System/System.Core）
  → 加载 HotUpdate.dll（Assembly.Load）
  → 反射调用 HotUpdate.Entry.Start()
  → Watermelon Core Initializer 初始化所有 InitModule
```

### 3.2 模块生命周期

项目使用 Watermelon Core 的 `InitModule` 系统顺序初始化：

1. **SaveController** — 存档系统
2. **CurrencyController** — 货币（金币）
3. **PoolManager** — 对象池
4. **AudioController** — 音频
5. **UIController** — UI 页面管理
6. **GameController** — 游戏主控制器
7. **LevelController** — 关卡控制
8. **ParticlesController** — 粒子系统
9. **PUController** — 道具系统（⚠️ 整个 `Init()` 被 `#if MODULE_POWERUPS` 包裹，宏缺失时静默跳过 → 启动 NRE，见注意事项 12）
10. **LivesSystem** — 能量门票适配层（Lock/Unlock = 预扣/返还能量）+ 无限模式容器
11. **MergeController / SpawnerController / TaskController** — 门店合成玩法（棋盘/生成器/订单，Game.Scripts 程序集）
12. **TutorialController** — 新手引导
13. **MapBehavior** — 关卡地图
14. **AdsManager** — 广告
15. **IAPManager** — 内购

### 3.3 游戏循环

```
主菜单 (UIMainMenu) 
  → 点击 Play / 选择地图关卡
  → GameController.LoadLevel()
  → LevelController.LoadLevel()
    → 从 LevelDatabase 加载 LevelData
    → LevelRepresentation 生成瓦片布局（多层交错网格）
    → LevelSpawnAnimation 播放出场动画
    → DockBehavior 滑入
    → GameplayTimer 开始计时
  → 玩家点击瓦片提交到 Dock
  → DockBehavior 三连匹配消去 → LevelController.OnMatchCompleted()
  → Dock 满槽未匹配 → LevelController.OnSlotsFilled() → 关卡失败
  → 所有瓦片消完 + Dock 空 → 关卡完成
```

---

## 四、玩法系统详解

### 4.1 关卡数据模型

```
LevelData (ScriptableObject)
├── layers[]: Layer              # 多层瓦片布局
├── bottomLayerWidth/Height      # 底层尺寸
├── elementsPerLevel             # 每关使用的瓦片种类数
├── coinsReward                  # 金币奖励
├── timer: IntToggle             # 限时（开关+秒数）
├── useInRandomizer              # 是否用于随机池
└── Difficulty = SetsAmount / elementsPerLevel

Layer
└── rows[]: LayerRow
    └── cells[]: CellData
        ├── IsFilled: bool       # 该位置是否有瓦片
        └── Effect: TileEffectType  # 特殊效果类型

TileEffectType: None | Unknown | Crate | Ice | Link
```

### 4.2 多层交错网格

- 奇偶层尺寸交替（底层最大，逐层缩小）
- 层间偏移半格，上层瓦片部分覆盖下层
- `LevelRepresentation.IsTileUnconcealed()` 判断瓦片是否可点击（上无遮挡）
- 坐标映射: `ElementPosition(x, y, layerId)` → 世界坐标 `LevelScaler.GetPosition()`

### 4.3 Dock 系统（核心交互）

- 底部 7 个槽位，瓦片从下往上排列
- 同类型相邻 3 个自动消去（连锁匹配）
- 匹配后可扩展额外槽位（`addedDepth`，最多 2 层即 +3 槽）
- 实现: `DockBehavior` + `SlotBehavior` + `SlotCase`

### 4.4 特殊瓦片效果（TileEffect）

| 类型 | 行为 | 文件 |
|------|------|------|
| Unknown | 图标遮盖，提交后解除 | `UnknownTileEffect.cs` |
| Crate | 木箱遮盖，不可点击，周围提交任意瓦片后破坏 | `CrateTileEffect.cs` |
| Ice | 多层冰层（Sprite 切换），每消一次碎一层 | `IceTileEffect.cs` |
| Link | 两个瓦片绑定，提交一个连带提交另一个 | `LinkedTileEffect.cs` |

### 4.5 道具系统

4 种道具：`Undo`（撤回）/ `Shuffle`（重排）/ `Hint`（提示）/ `ExtraSlot`（扩展槽）

架构：
- `PUSettings`（ScriptableObject 配置基类）→ `PUCustomSettings`（含标题）
- `PUDatabase` 管理所有道具配置
- `PUController` 单例管理运行时行为
- `PUBehavior` 行为基类 → 具体实现 `PUUndoBehavior` / `PUShuffleBehavior` / `PUHintBehavior` / `PUExtraSlotBehavior`
- `PUUIBehavior` / `PUUIController` UI 展示
- `PUUIPurchasePanel` 购买弹窗

---

## 五、门店合成玩法（Merge）

> 2026-08-23 起，门店 Tab 由「挂机货架经济」整体替换为 mergedev 模板的「合成 + 订单 + 装修」玩法。
> 旧 ShopController/货架/配方/原料系统已彻底删除（详见第十七章迁移记录）。

### 5.1 玩法闭环

```
消消乐关卡 ──掉金币──┐         （蛋糕碎片掉落已禁用，见 18.3）
                     ▼
合成棋盘（7×9 uGUI）  拖拽同级同类物品 → 合成升 1 级
  ├─ 锁定格（LockedCell，藏隐藏物品，被相邻合成波及后解锁）
  ├─ 生成器（茶壶/搅拌机等）：点按耗能量产出物品
  │   （g1-g3 无产出池是原版设计，合成到 g4+ 才能产出）
  └─ 能量：上限 50，120 秒恢复 1 点，离线结算
        │   ※ 与消消乐门票共用同一能量池（见第八章）
        ▼
顾客订单（ClientOrderTask）  需求物品绑定棋盘活体 → Give 交单
  ├─ 得金币（碎片奖励已禁用）
  └─ 订单批次按建筑升级数解锁，随机订单池防金币过剩
        │
        ▼
店铺装修（Zone/Building）  金币升级建筑 → 换外观
  └─ 升级数解锁：新订单批次/对话/经验等级/新区域（Zone 1-3）
```

### 5.2 代码架构

| 层 | 类 | 程序集 | 职责 |
|---|---|---|---|
| 棋盘 | `MergeController` / `MergeGrid` / `MergeCell(Background)` / `MergeFieldObject`(+6 子类) | Game.Scripts | 拖拽磁吸合成、格子状态、初始布局（`MergeLevelData`，60 锁定 + 3 茶壶） |
| 生成器 | `SpawnerController` / `SpawnerObject` / `ChestObject` | Game.Scripts | 加权产出池、能量门控、宝箱次数 |
| 订单 | `TaskController` / `ClientOrderTask` / `SpawnerRewardQueue` | Game.Scripts | 顺序+随机订单队列、交单发奖 |
| 装修 | `ZoneController` / `BuildingController` / `ZoneData` | Game.Scripts | 区域解锁（经验等级）、建筑升级驱动进度 |
| 能量 | `EnergyController` / `EnergyRegenRunner` | Game.Scripts | 50 上限 / 120s 恢复 / 离线结算 |
| 数据 | `MergeDatabase`（13 条物品链，`[SerializeReference]` 多态 config） | Game.Scripts | 物品链/产出池/售价配置 |
| 桥接 | `MergeViewController` | Game.Scripts | 页面门面（EnterHub/ExitHub/双视图互切/教程激活）——**HotUpdate 不可直接引用模板页面类型，必须走这里** |
| 桥接 | `CakeUIBridge` | Game.Scripts | 反向桥：OpenStore / OrderCompleted（HotUpdate 启动时赋值） |
| Hub | `MergeHubModule` | HotUpdate | Tab 生命周期（已替代 ShopHubModule） |
| 碎片 | `FragmentController` / `FragmentSave` | Game.Scripts | 蛋糕碎片库存（⚠️ 2026-08-24 起功能禁用，代码/存档保留，见 18.3） |
| 碎片 | `FragmentDropHook` / `UIFragmentExchangePanel` / `OpenFragmentPanelButton` | HotUpdate | 关卡掉落（0.15）/ 运行时兑换面板（⚠️ 已禁用：钩子短路、入口隐藏） |
| 统计 | `MergeStatsController` / `MergeStatsSave` | Game.Scripts | 订单总数（称号/个人页） |

### 5.3 场景装配（Game.unity）

- `UI Main Canvas` 下：`UI Merge Game`（棋盘页，含嵌套 Canvas `Items`/`Flying Objects`）、`UI Merge Menu`（装修页）、`UI Header`（金币/能量/经验）、`UI Building`、`UI Zones`、`UI Info Window`、`UI Recover Energy`、`UI Level Up`、`UI Dialog`、`UI Rewards Confirmation Popup`、`Tutorial Overlay`
- `Scripts Holder`：`MergeController`、`SpawnerController`、`TaskController`、`ClientOrderHighlightController`、`CurrencyCloud`、`TutorialController(Watermelon.Tutorial)` + 3 个教程对象
- InitModule（`Project Init Settings`，共 18 个）：新增 Game/Character/UIQueue/Building/Dialog/Energy/Experience/Zone 8 个

### 5.4 显隐规则（重要）

棋盘页内含**嵌套 Canvas**（`Items`/`Flying Objects`，独立渲染根），禁用页面根 Canvas **不能**隐藏棋盘——必须整体激活/禁用 `UI Merge Game` 根节点（由 `MergeViewController.EnterHub/ExitHub` 负责；`GameController.Start` 在 `InitGrid` 后立即 `ExitHub` 隐藏）。`InitGrid` 需要激活的 Canvas 量格子尺寸，故场景内保持激活、靠启动流程隐藏。

### 5.5 称号/日常/推送改接

- 称号：订单 50 单「订单达人」/ 建筑 10 级「装修大师」/ 区域全解锁「区域开拓者」（⚠️ 「我的」Tab 已禁用，称号入口不可见，见 18.2）
- 每日任务：`MergeOrders`（完成 2 订单，经 `CakeUIBridge.OrderCompleted`；⚠️ 每日任务面板已屏蔽，进度照跑但无 UI 出口）
- 游客奖励：能量 30 + Kettle 生成器 + 50 金币
- 推送（2026-08-28 重写）：`PushNotificationManager`（HotUpdate，`Other/PushNotificationManager.cs`）接 **Unity Notifications 2.3.2** API
  - 能量回满提醒（`ScheduleEnergyFullReminder`）+ 每日 18:00 刷新提醒（`ScheduleDailyResetReminder`）
  - 真机生效（`#if UNITY_ANDROID || UNITY_IOS`），编辑器/PC 静默
  - 包 `com.unity.mobile.notifications` 为 **本地副本**（`Packages/com.unity.mobile.notifications/`，`manifest.json` `file:` 引用 + lock `embedded`）——因 `packages.unity.com` 被墙（ECONNRESET）无法在线注册，见 19.3

---

## 六、Hub 模块系统（底部导航栏）

### 6.1 架构

```
UIBottomNavBar — 底部导航栏 UI
HubModuleRouter — 模块路由（单例）
├── MergeHubModule   — 门店合成玩法（Tab 枚举仍为 MainHubTab.Shop=0）
├── CamperHubModule — 关卡地图 + 主菜单
└── ProfileHubModule — 个人中心（⚠️ 2026-08-24 起禁用：路由不注册、底栏按钮隐藏，代码保留可恢复，见 18.2）
```

### 6.2 接口 `IHubModule`

```csharp
interface IHubModule {
    MainHubTab Tab { get; }
    bool IsActive { get; }
    void Enter();
    void Exit();
}
```

### 6.3 底部导航栏布局

- 两栏: Shop（门店合成，锚点 0~0.5） | Camper（关卡，0.5~1）；Profile 按钮已隐藏（m_IsActive: 0）
- 纯图标样式，使用特定资源
- 相机渲染方式提升层级
- `HubModuleRouter.SwitchTo` 先解析目标模块再退出当前模块——未注册的 Tab 会被拦截且不污染当前页
- **两个 Tab 共用同一顶栏**（UIHeader：经验徽章 + 能量/金币/钻石 + 商店按钮），由各自 Hub 模块在 Enter/Exit 时经 `MergeViewController.SetHeaderVisible` 管理；进关卡自动隐藏，回主菜单自动恢复（见 18.7）

---

## 七、关卡地图系统

- `MapBehavior` — 垂直滚动大地图管理器
- `MapData` — 分块预制体列表 + 滚动参数
- `MapChunkBehavior` — 每块关卡组
- `MapLevelBehavior` — 单个关卡节点（三态: 未解锁/已解锁/当前）
- 支持 `rubber-band` 滚动回弹、宽屏适配

---

## 八、能量系统（原生命系统，2026-08-23 已合并）

> 消消乐 Lives（5 命/20 分钟恢复）与合成 Energy（50 点/120 秒）已**合并为单一能量池**，
> 沿用合成的数值与图标/布局（EnergyUIPanel）。旧 Lives 命数体系已删除。

### 8.1 规则

- `EnergyController`（Game.Scripts）— 上限 50，120 秒回 1 点，离线结算（`RecoverOffline`）
- **进关门票 = 10 能量**：`LivesSystem.LockLife()` 预扣；通关/返回主页 `UnlockLife(false)` **全额返还**（即赢不耗能量）；失败/中途退出不返还
- **半价重试 = 5 能量**（赢也返还）：`CanStartHalfPrice()`
- **无限模式保留**：激活期间进关不耗能量（Starter Pack 2h + 签到第 5 天 30 分钟权益不变）；合成生成器照常耗能
- 能量不足时 Play → 弹 `UIRecoverEnergy`（看广告/钻石购买补足）

### 8.2 适配层

`LivesSystem`（HotUpdate）保留类名/存档，改造为**门票适配层 + 无限模式容器**：
- `LEVEL_ENERGY_COST = 10` / `HALF_PRICE_ENERGY_COST = 5`
- 连续 NextLevel 链路天然正确（每关预扣 + 每赢返还 = 净 0，同原"赢免费"）
- `LivesSave` 保留无限模式字段；命数恢复循环/半价累积器已删除
- UI：主菜单/失败页/商店页的生命指示器已全部换成 Energy Panel（`EnergyPanelInstaller`，Actions/Merge 步骤 6-7）；`Lives Indicator.prefab`、`Add Lives Panel.prefab` 等已删除

### 关卡失败流程

```
OnSlotsFilled / TimerFinished
  → GameController.OnLevelFailed()
  → LivesSystem.LockLife()（= 能量已预扣 10，不返还）
  → UIComplete / UIGameOver 弹出
  → 复活（激励视频广告） / 半价重试（5 能量） / 返回主菜单
```

---

## 九、UI 页面系统

基于 Watermelon Core 的 `UIPage` 框架（Show/Hide + 动画回调）：

| 页面 | 脚本 | 用途 |
|------|------|------|
| UIMainMenu | `UI/UIMainMenu.cs` | 主菜单（关卡号/金币/生命/去广告/IAP 入口） |
| UIGame | `UI/UIGame.cs` | 游戏 HUD（金币/计时器/退出/道具/教程） |
| UIComplete | `UI/UIComplete.cs` | 通关结算（金币奖励动画/加倍/下一关） |
| UIGameOver | `UI/UIGameOver.cs` | 失败结算（复活/重玩/返回） |
| UIStore | `IAP Store/UIStore.cs` | IAP 商店 |
| UISettings | `Settings/UISettings.cs` | 设置面板 |
| UIAddLivesPanel | `Lives System/UIAddLivesPanel.cs` | 添加生命弹窗 |
| UILevelQuitPopUp | `Lives System/UILevelQuitPopUp.cs` | 退出确认弹窗 |

---

## 十、编辑器工具

| 工具 | 文件 | 用途 |
|------|------|------|
| Level Editor Window | `Level/Editor/LevelEditorWindow.cs` | 关卡可视化编辑窗口（Window → Level Editor） |
| Tile Texture Generator | `Editor/TileTextureGenerator.cs` | 程序化生成糖果瓦片纹理（64x64 PNG） |
| Bottom Nav Scene Setup | `Editor/BottomNavSceneSetup.cs` | 代码构建底部导航栏场景结构 |
| Merge Scene Importer | `Editor/MergeSceneImporter.cs` | **合成玩法场景搬运**（mergedev → Game.unity，Dry Run/Execute/Verify + 旧门店清理 + 碎片入口） |
| Custom Actions Menu | `Editor/CustomActionsMenu.cs` | 自定义操作菜单（占位） |
| HotUpdate Build Processor | `Editor/HotUpdateBuildProcessor.cs` | HybridCLR 构建流程 |
| Unity MCP 自动启动 | `Editor/UnityMcpAutoStart.cs` | 开启 MCP HTTP 桥接自动启动（uvx mcpforunityserver, 127.0.0.1:8080） |
| MCP 失焦自动刷新 | `Editor/UnityBackgroundUpdate.cs` | **自动化前置**：runInBackground + 失焦强制重绘 GameView/SceneView |

### 10.1 MCP 自动化运行保障（UnityBackgroundUpdate.cs）

Unity 编辑器**失焦时** Play Mode 默认冻结（逻辑暂停 + 画面不刷新），会破坏 MCP 自动化（截图/状态读取看到死画面）。`UnityBackgroundUpdate.cs` 三管齐下解决：

1. **逻辑不冻结** — `PlayerSettings.runInBackground = true`（已写入 `ProjectSettings.asset`），失焦时 Update/物理/协程继续执行
2. **运行时兜底** — `EnteredPlayMode` 时 `Application.runInBackground = true` + `QualitySettings.vSyncCount = 0` + `targetFrameRate = 60`
3. **画面持续刷新** — `EditorApplication.update` 检测 `InternalEditorUtility.isApplicationActive == false`（失焦）时，以 ~10Hz 强制 `GameView`/`SceneView` `Repaint()`

- 开关菜单：`Tools/MCP/失焦自动刷新`（EditorPrefs `UnityMCP.BackgroundAutoRefresh`，默认开启）
- 验证结论：失焦状态下 `isAppActive=False`，Play Mode 照常运行（`runInBackground=True, targetFPS=60, vsync=0`）

---

## 十一、Watermelon Core 框架

自研 Unity 手游框架，提供以下模块：

| 模块 | 用途 |
|------|------|
| Pool | 对象池系统 |
| Save | 存档系统（`ISaveObject` + JSON 序列化） |
| Tween | 补间动画引擎（链式调用、Ease 缓动函数） |
| UI | 页面管理（`UIPage` 基类，Show/Hide 动画） |
| Currency | 货币系统（`CurrencyType` + `CurrencyController`） |
| Audio | 音频管理（`AudioController` + `AudioClips`） |
| Haptic | 触觉反馈 |
| Monetization | 广告 + IAP 集成 |
| Initializer | 多阶段初始化流程 |
| Defines | 全局编译常量 |
| Inspector | 自定义 PropertyDrawer |

---

## 十二、相关文件索引

| 类别 | 文件 | 行数 |
|------|------|------|
| 热更入口 | `Assets/HotUpdate/Entry.cs` | 19 |
| DLL 加载器 | `Assets/Scripts/LoadDll.cs` | 154 |
| 游戏配置 | `Assets/HotUpdate/Scripts/GameData.cs` | 27 |
| 主控制器 | `Assets/HotUpdate/Scripts/Controllers/GameController.cs` | ~200 |
| 输入 | `Assets/HotUpdate/Scripts/Controllers/InputController.cs` | ~50 |
| 射线检测 | `Assets/HotUpdate/Scripts/Controllers/RaycastController.cs` | ~100 |
| 关卡控制器 | `Assets/HotUpdate/Scripts/Level/LevelController.cs` | 625 |
| 等级数据 | `Assets/HotUpdate/Scripts/Level/Level Data/LevelData.cs` | 56 |
| 瓦片行为 | `Assets/HotUpdate/Scripts/Level/TileBehavior.cs` | 318 |
| 瓦片数据 | `Assets/HotUpdate/Scripts/Level/TileData.cs` | 47 |
| 关卡场 | `Assets/HotUpdate/Scripts/Level/Level Field/LayersMatrix.cs` | 48 |
| 层网格 | `Assets/HotUpdate/Scripts/Level/Level Field/LayerGrid.cs` | 39 |
| 关卡表示 | `Assets/HotUpdate/Scripts/Level/LevelRepresentation.cs` | 496 |
| 关卡缩放 | `Assets/HotUpdate/Scripts/Level/LevelScaler.cs` | 113 |
| Dock | `Assets/HotUpdate/Scripts/Level/Dock/DockBehavior.cs` | 616 |
| 生命系统 | `Assets/HotUpdate/Scripts/Lives System/LivesSystem.cs` | 352 |
| 合成控制器 | `Assets/Project Files/Game/Scripts/Merge/MergeController.cs` | ~800 |
| 合成门面 | `Assets/Project Files/Game/Scripts/Merge/MergeViewController.cs` | ~100 |
| Hub路由器 | `Assets/HotUpdate/Scripts/Hub/HubModuleRouter.cs` | ~100 |
| 道具控制器 | `Assets/HotUpdate/Scripts/Power Ups System/PUController.cs` | ~200 |
| 关卡地图 | `Assets/HotUpdate/Scripts/Level Map/MapBehavior.cs` | ~250 |
| 教程控制器 | `Assets/HotUpdate/Scripts/Tutorial/TutorialController.cs` | 70 |
| LevelData 存档 | `Assets/HotUpdate/Scripts/Level/LevelSave.cs` | 20 |
| UI 主菜单 | `Assets/HotUpdate/Scripts/UI/UIMainMenu.cs` | 279 |
| UI 游戏 | `Assets/HotUpdate/Scripts/UI/UIGame.cs` | ~150 |
| UI 完成 | `Assets/HotUpdate/Scripts/UI/UIComplete.cs` | ~200 |
| UI 失败 | `Assets/HotUpdate/Scripts/UI/UIGameOver.cs` | ~150 |
| 游客注册 | `Assets/HotUpdate/Scripts/GuestRegistration.cs` | 51 |
| 推送管理器 | `Assets/HotUpdate/Scripts/Other/PushNotificationManager.cs` | ~90 |
| 启动冒烟测试 | `Assets/Tests/PlayMode/CakeHomeBootTest.cs` | ~50 |

---

## 十三、注意事项

1. **所有逻辑在 `Watermelon` 命名空间下**（`HotUpdate.asmdef`）
2. **预热 dll 在 `Assets/StreamingAssets/HotUpdate/`**，线上从 CDN 下载
3. **`LevelEditorBase` 编辑器基类** 需子类实现抽象方法，适配不同的关卡数据格式
4. **店铺 3D 场景** 使用固定 45° 等距视角 + 专属天空盒，代码场景构建
5. **HybridCLR 构建** 通过 `HotUpdateBuildProcessor.cs` 控制
6. **存档模型** 统一实现 `ISaveObject` + `Flush()` 接口，通过 `SaveController` 管理
7. **🎮 积分体系统一为金币 Coins（2026-08）** — 关卡奖励、游客注册、店铺挂机收获全部发放 `CurrencyType.Coins`；烘焙积分 `BakingCredits` 已从 `MD_CurrencyType.cs` 枚举和 `Currencies Database.asset` 彻底移除（GDD 14.2 的"烘焙积分中台"规划暂未采纳，实际以金币统一）
8. **⚠️ Enter Play Mode Options 已禁用（`EditorSettings.asset` = 0/0）** — 此前开启 `DisableDomainReload + DisableSceneReload`（=3）导致场景不重载时 `Initializer.Awake` 不执行、Watermelon 核心模块（Save/Audio/Currency）不初始化，产生大量 NullReferenceException。恢复标准重载后模块每次 Play 稳定初始化。**不要重新启用该选项**
9. **⚡ Unity 失焦自动刷新** — `Assets/Editor/UnityBackgroundUpdate.cs` 解决编辑器失焦时 Play Mode 逻辑/渲染冻结（runInBackground + 强制重绘），便于 MCP 自动化
10. **🧩 模板代码隔离在 `Game.Scripts` 程序集** — Merge 模板代码在 `Assets/Project Files/Game/Scripts/`（程序集 `Game.Scripts` + 7 个 `Game.Scripts.*.Editor`），HotUpdate 单向引用它；模板代码**不允许**反向引用 HotUpdate 类型（会成环），跨层调用点均已打 `TODO(模板迁移)` 存根
11. **🧱 同名类型双存在是刻意的** — `GameData`/`LevelDatabase`/`TutorialController` 等在 HotUpdate（蛋糕版）与 Game.Scripts/Watermelon.Tutorial（模板版）各有一份；同程序集内优先解析本程序集类型（CS0436 警告属预期）。**不要再把第三个同名类型引进 Assembly-CSharp**（CS0433 硬错误）
12. **⚙️ 平台宏必须齐全** — `MODULE_POWERUPS` / `MODULE_MONETIZATION` 需在 Android/iOS/tvOS/Standalone **同时**定义（2026-08-28 修复：原只在 Standalone，移动端 `PUController.Init()` 被 `#if` 编译掉 → 启动刷 2 个 NullReference）。改动位置：`ProjectSettings.asset` → `scriptingDefineSymbols`（或 Player Settings → Scripting Define Symbols）
13. **🤖 batchmode/CI 验证启动用 PlayMode 测试** — `WaitForEndOfFrame` 在 batchmode（无渲染帧）**不触发**，GameLoading 已加 `Application.isBatchMode` 兼容（GUI 行为不变）。验证启动全链路：`Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter CakeHomeBootTest -logFile Library/PlayModeTest.log`（见 19.2）
14. **🔔 推送包是本地副本** — `com.unity.mobile.notifications` 位于 `Packages/com.unity.mobile.notifications/`（`file:` 引用 + lock `embedded`），因 `packages.unity.com` 被墙无法在线拉取。**勿从包管理器删除/替换**，否则推送编译报错

---

## 十六、2026-08-22 框架升级与 Merge 模板整合

导入「Merge Adventure Template」时同时完成了一次 Watermelon Core 大版本覆盖升级。以下为变更全貌与后续工作清单。

### 16.1 新框架 API 变更（蛋糕代码已全部适配）

| 旧 API | 新 API | 影响面 |
|--------|--------|--------|
| `InitModule.CreateComponent()` | `InitAsync(GameObject owner)`（协程，`yield break` 结尾） | 5 个 InitModule 已迁移 |
| `ISaveObject.Flush()` | `ISaveObject.OnBeforeSave()` | 14 个存档类已迁移 |
| `UIPage.PlayShowAnimation/PlayHideAnimation()` | `OnShow()/OnHide()` + `NotifyOpened()/NotifyClosed()` | 8 个页面已迁移 |
| `UIController.OnPopupWindowOpened/Closed` | 移除；弹窗改用 `UIPage.IsPopup => true` 自动跟踪 | 6 个弹窗已清理调用 |
| `Reward : MonoBehaviour` + `Init()` | `Reward` 纯 `[Serializable]` 类 + `ApplyReward()/CheckDisableState()` | 见 16.3 遗留 |
| `GameLoading` 静态类 | `GameLoading : MonoBehaviour`（驱动模块初始化 + 场景加载） | Init.unity 已接线 |
| `AudioController.Init(AudioClips,...)` | `new AudioController(poolSize, AudioRegistry,...)` | 已加 `SetLegacyAudioClips` 兼容垫片 |
| `AdsManager.EnableBanner()` 不看设置 | 现在尊重 `AdsSettings.BannerEnabled`（当前=关，TEST BANNER 已隐藏） | — |

### 16.2 启动链路（已修复并验证）

- `AutoInitializerLoader`（新框架 editor 脚本）：从非 Init 场景进 Play 时自动把 `playModeStartScene` 重写为 Init.unity，并记录原场景 buildIndex
- **Init.unity 的 "Loading Graphics" 对象上挂了 `GameLoading` 组件**（本次新增接线），链接到 Initializer prefab 实例 → 驱动 `ProjectInitSettings.InitAsync` 逐个初始化模块 → 加载 Game.unity
- Project Init Settings 模块列表（10 个）：Initializer → Save → **Pool（本次补入）** → Tween → Audio → Currencies → Haptic → Monetization → Screen → Lives
- `AdsSettings.providerContainers` 已注入 `AdDummyContainer`（Dummy  provider 占位）

### 16.3 ✅ 已解决：奖励系统重接线（2026-08-22，commit `cb9790e`）

~~新版 `Reward` 不再是 MonoBehaviour，蛋糕预制体里挂的旧奖励组件变成 missing script。~~
已完成：8 个商品包 prefab 迁移到 `RewardsSet` 资产 + `RewardView` 接线（`Data/Rewards/{IAP,Ads,Timer}/` 8 个新资产）；`PUReward`/`LivesInfiniteModeReward` 重构为纯数据类并新建对应 View；33 个资产 321 处过期程序集名（`asm:`）已修正。详见第十七章。

### 16.4 模板资源整合（Game.Scripts 程序集）

- 模板游戏代码（Building/Zone/Energy/Merge/Tasks/Dialog/Characters 等）在 `Assets/Project Files/Game/Scripts/`，编译为 `Game.Scripts`
- 用户迁移中的模板 UI（UIBuilding/UIHeader/UIZoneCard/UILevelUpPopup/OpenStoreButton/RewardFlyElement 等）也已归入该程序集（`Game/Scripts/UI/`）
- ~~模板对蛋糕页面的 8 处引用已存根（`TODO(模板迁移)`）~~ **已全部解决（2026-08-23）**：7 处恢复 mergedev 原版/桥接（`CakeUIBridge.OpenStore`、`MergeViewController.SetBuildingActive` 等），`FirstStartTutorial` 872 行完整版已恢复并接入
- 模板存档键映射 `MD_GeneratedSaveKeyRegistration.cs` 移入 `Assets/HotUpdate/Scripts/Other/Save/`（避开 Assembly-CSharp 的 CS0433）；**重新生成（Tools/Save/Regenerate Key Map）后需手动挪回**，蛋糕存档类型已登记；新增 `FragmentSave`/`MergeStatsSave` 两个键；废弃 `ShopSave`/`RecipeSave`/`IngredientSave` 三个键已移除
- **场景整合已完成（2026-08-23）**：11 个模板页面 + 3 个教程对象 + 6 个控制器组件已迁入 Game.unity，8 个 InitModule 已注册，详见第十七章

### 16.5 被模板覆盖后已恢复的文件（git checkout 自 HEAD）

场景（Game/Init/2 个示例场景）、ProjectSettings 全套 8 个（**含 Enter Play Mode Options=0/0**）、Game Data/Project Init Settings/Level Database/Input System/icon.png，以及 GameController/UIMainMenu/UIGame/UIStore/GameData/MD_ProductKeyType 6 个代码文件。**再导入模板包时这些还会被覆盖，需再次恢复。**

### 16.6 编辑器已知杂音（不影响运行）

- 重复菜单项警告（Help/Open Editor Folder、Actions/Game Scene 等，模板 Editor Tools 与项目原有菜单重名）
- 模板关卡编辑器改挂 `Window/Merge Level Editor` 菜单（避开与蛋糕 Level Editor 冲突）
- CS0436 同名类型遮蔽警告（见注意事项 11）

---

## 十七、2026-08-23 门店玩法替换：合成玩法落地全记录

门店 Tab 由「挂机货架」整体替换为 mergedev 合成玩法（决策：旧系统**彻底删除**、纳入装修层、碎片改接合成）。全部场景操作经 MCP-FOR-UNITY（127.0.0.1:8080）自动化执行 + Play 模式端到端验证。

### 17.1 删除清单

- 代码：`HotUpdate/Scripts/Shop/`（ShopController/ShopWorld/ShopIdleProducer/ShopShelfSlot/CakeCatalog/ShopConfig/ShopDailyTheme/ShopSave/ShopInitModule + 冰柜/原料 UI）、`Other/Recipe/`、`Other/Ingredient/`、`UI/BottomNav/UIShopPage.cs`、`Hub/Modules/ShopHubModule.cs`、`Editor/ShopUIBuilder.cs`、`ShopSceneSetup.cs`、`ShopDebugMenu.cs`
- 资产：`Prefabs/Shop/`、`Prefabs/UI/Canvas/UIShopPage.prefab`、`Resources/Shop/`、`Resources/Recipe/`
- 场景：`ShopWorld` 根节点（含 Environment_Shop1/DisplayStage）、画布下 UIShopPage 实例
- 存档：shop/recipe/ingredient 三键废弃（旧存档条目无害遗留，不迁移）

### 17.2 场景搬运（MergeSceneImporter，Actions/Merge 菜单）

mergedev Game.unity → cakehome Game.unity，**Editor 脚本 additive 搬运**（两场景同源自模板、大量相同 fileID，纯 YAML 合并不可行）：
- 11 个页面 → `UI Main Canvas`（UI Game→UI Merge Game、UI Main Menu→UI Merge Menu 等）
- 3 个教程对象 → `Scripts Holder` 子级；6 个控制器组件 → `Scripts Holder`（按**脚本 GUID** 匹配，避开同名类歧义）
- `MoveGameObjectToScene` 要求根节点：须先 `SetParent(null)` 再移动
- 组件复制用 SerializedObject 逐字段拷贝（跳过 `m_*` 对象头），引用指向已搬入的活体对象
- 步骤菜单：1 Dry Run → 2 Execute → 3 Verify（可重入，含重开场景复验）→ 4 清理旧门店 → 5 碎片兑换入口

### 17.3 数据/初始化

- `Data/Level System/Merge/Merge Level Database.asset`（保 GUID f2484a03，避开消除关卡库同名文件）
- `Merge Game Data.asset`（Game.Scripts GameData，注意：**Game.Scripts 版 GameData.cs 的 GUID 是 d0dd1368，不是模板的 bdfe1560**——后者被 HotUpdate GameData 占用）
- `Project Init Settings` 注册 8 个模块（Game/Character/UIQueue/Building/Dialog/Energy/Experience/Zone，共 18 个）
- `AudioInitModule.audioRegistry` 接线（merge 音效全走 GetClip）
- `Currencies Database` 补 Gems（模板 Header/能量恢复/UIStore 依赖）

### 17.4 踩过的坑（后续维护必读）

1. **同名脚本 GUID 冲突**：模板 `UIGame.cs` 与 HotUpdate `UIGame.cs` 原生同 GUID（3c785c42），导入时模板版被重分配新 GUID（539b905e），场景组件因此绑到 HotUpdate 版 → 页面不注册 + 字段丢失（绑错类期间保存场景，序列化字段被按错误类重写）。**教训：导入同名类后必须核对场景组件的脚本 GUID 指向；绑错类期间不要保存场景。**
2. **嵌套 Canvas 穿透**：棋盘 `Items`/`Flying Objects` 是独立渲染根，页面 Canvas 禁用管不住 → Tab 显隐必须切换根 GameObject（见 5.4）。
3. **`FloatingCloud`（UI 模块）是死代码**：其 `Init()` 全工程无调用方，注册表恒空 → `SpawnCurrency` 静默失败，**通关金币从未到账**。活系统是 `CurrencyCloud`（Currency 模块 MonoBehaviour）。UIComplete/UIStore 已切换。
4. **粒子需场景注册**：mergedev ParticlesController 的 6 个注册项（Merge/Delete/Appear/Energy/Currency Item Usage/Building Upgrade）需手动补入 Game.unity 的 ParticlesController。
5. **生成器 g1-g3 产出池为空是原版设计**（须合成到 g4+ 才产出），不是数据丢失。
6. **程序集拓扑**：Game.Scripts ← HotUpdate 单向引用。模板代码要调 HotUpdate 的东西走 `CakeUIBridge`；HotUpdate 要操作模板页面走 `MergeViewController`；`FirstStartTutorial` 基类在 `Watermelon.Tutorial` 程序集（HotUpdate 未引用），相关调用也只能放 Game.Scripts 侧。
7. **`FindObjectByType` 在当前编译环境不可用**（报 CS0117），统一用 `FindObjectOfType`。
8. **Game.Scripts 侧 `TutorialController` 无初始化方**（2026-08-27 修复）：HotUpdate `GameController` 只 Init 同名的 HotUpdate 版，模板版 `instance` 恒为 null → `ActivateTutorial` 静默返回 → FirstStartTutorial 永不启动 → 订单/进度全冻结（无报错、无警告，极难察觉）。修复：`MergeViewController.ActivateTutorials()` 开头补 `FindObjectOfType<TutorialController>(true).Init()`。
9. **FirstStartTutorial 场景引用悬空**（2026-08-27 修复）：`introDialog`/`mergeGrid`/`heroineCharacter`/`taskCharacter` 四个 `[SerializeField]` 均未接线（教程从未运行所以从未暴露；Intro 步 `DialogController.Play(null)` 直接 NRE）。已接线：`Intro Dialog.asset`、`Game Area`(MergeGrid)、`C1 Maya`×2。`cachedGiveCard` 等 cached/pending 字段是运行时赋值，NULL 属正常。
10. **新手引导总开关**（2026-08-27）：`GameData.showTutorial=false` 时 `MergeViewController.TutorialsEnabled=false` → `ActivateTutorials()` 跳过、`IsOnboardingCompleted()` 恒 true（订单/进度直接解锁）、插屏守卫走 `IsOnboardingCompleted` 包装（教程关闭不会永久屏蔽广告）。**注意**：Zone 1 进度从 atUpgrade:1 起步，0 金币时门店侧无订单属正常——赢第一关拿游客奖励 50 金币 → 首次建筑升级后订单链启动。

### 17.5 MCP 自动化工作流（可复用）

- 桥接：Unity 内 MCP-FOR-UNITY（uvx mcpforunityserver, `127.0.0.1:8080/mcp`，streamable HTTP）
- 客户端：`/tmp/mcp_unity.py`（initialize/tools/call）+ `/tmp/mcp_exec.py`（execute_code 执行 C# 并返回结果）
- 模式：`execute_code` 里反射调用私有 Editor 方法 + `Application.logMessageReceived` 捕获日志；`read_console` 查编译错误；`AssetDatabase.Refresh(ForceUpdate)` 触发重编译
- 注意：Play 模式中不能 `EditorSceneManager.OpenScene`（须先停 Play）；动态代码里引用双存在类型（UIGame 等）会歧义，用 `GameObject.Find` 或按程序集反射
- **改完脚本必须显式 `AssetDatabase.Refresh()` 再进 Play 验证**：编辑器后台不自动检测外部文件改动，否则会拿着旧程序集白测一轮

---

## 十八、2026-08-24 功能裁剪与 UI 修复

### 18.1 UI 页面层级修复（商店/设置"假失效"）

- **根因**：所有 UI 页同在 `UI Main Canvas` 下按**子节点顺序**渲染。合成页面嫁接时排在末尾 → `UI IAP Store`/`UI Settings` 被压在合成页面之下，门店 Tab 打开它们时 `IsPageDisplayed=true` 但不可见。
- **修复**：两页移到子节点最末尾（modal 层级，覆盖底栏与所有页面）。**以后新加覆盖页必须放最后。**
- 同批修复：棋盘页 `Fragment Button` 与 `Map Button` 同叠右下角导致 Map 按钮点不到 → 碎片按钮移至左下（后随碎片功能一并隐藏）。
- **验证方法论**：`onClick.Invoke()` 只能验证逻辑不能验证可见性——要 `EventSystem.RaycastAll` 查遮挡 + `ScreenCapture` 截屏眼见为实。

### 18.2 「我的」Tab 禁用

- 场景隐藏 `Bottom Nav Bar/Tabs/Profile`（锚点重排：Shop 0~0.5 / Camper 0.5~1）
- `HubModuleRouter` 不再注册 `ProfileHubModule`（代码/Avatar/称号/统计系统保留）
- 顺手修复：`SwitchTo` 原为先退出当前模块再校验目标 → 改为**先解析目标再切换**，无效 Tab 不再把当前页搞空
- 恢复：场景激活 Profile 按钮 + 取消路由注册注释

### 18.3 配方碎片功能禁用

- 三处口子全关：`FragmentDropHook`（消除 15% 掉落）短路 return、`UIClientOrderCard` 订单碎片奖励注释、场景 `Fragment Button` 停用（兑换面板无入口）
- `FragmentController`/兑换面板/碎片存档全部保留；`MergeSceneImporter` 重导入时创建未激活按钮保持一致
- 恢复：激活场景按钮 + 取消两处注释（各屏蔽点有注释指引）

### 18.4 NO ADS 按钮移除

- `UIMainMenu.ShowAdButton` 短路为始终隐藏（按钮滑屏机制不变，可逆）
- 保留：商店内 No Ads 商品、设置页恢复购买、强制广告开关

### 18.5 能量体系统一（2026-08-23，补记）

- Lives + Energy 合并为单一能量池（详见第八章）：门票 10 能量赢返还、半价重试 5、无限模式改为免门票 buff
- `EnergyPanelInstaller`（Actions/Merge 步骤 6-7）：Energy Panel 提取为 prefab 并替换三处宿主页的生命指示器
- `EnergyUIPanel` 多实例注册冲突修复：`CurrencyCloud.IsRegistered` 全局守卫 + Start 自初始化
- 每日任务面板屏蔽：`UIMainMenu.CheckDailyTaskPanel` 短路（每日签到保留）

### 18.6 商店内容对齐 mergedev（2026-08-24）

- **背景**：大版本升级时 IAP Settings 被 mergedev 版整体覆盖（BoostPack=5…NoAdsPack=13），但商店 UI 与 `ProductKeyType` 枚举仍是旧的（NoAds=0…PUPack=5）——旧 8 个商品全部失效/错配（PUPack=5 撞上 BoostPack 注册）。
- **最终阵容**（UI IAP Store.prefab）：Starter Pack（保留，新注册 com.example.starter.pack / $4.99 / NonConsumable）→ Boost Pack($3.99) → Pro Pack($7.99) → No Ads Pack($4.99) → 能量×3（25/40/65 **钻石**软货币购买）→ 钻石×6（$1.99~$119.99）。旧 Power Pack/金币×3/广告金币/计时金币下线（prefab 资产保留未删）。
- **实现方式**：跨项目 YAML 移植——mergedev Game.unity 的 5 个 offer 子树（655 文档）重映射 fileID/rid 后并入商店 prefab；`IAPRewardsHolder` GUID 替换为 Core 版（ad80efb8→c01010db）；`currencyCloudTargetPoint` 场景引用置空（spawnCurrencyCloud 全为 0 无影响）。
- **枚举**：`ProductKeyType` 重写为 StarterPack=1 + mergedev 阵容（5-13）；`UINoAdsPopUp`/`UIMainMenu` 的 `NoAds` 引用改为 `NoAdsPack`。
- **顺带修复**：`UI Rewards Confirmation Popup` 的 `rewardUIPrefab` 引用悬空（mergedev 预制体未导入）→ 重指到 `UI Level Up Reward Tile.prefab`；Pro Pack 标题在 mergedev 原数据就误写为 Boost Pack，已修正。
- 编辑器内 IAP 价格显示 USD 0.00 + 转圈属正常（无真实商品数据），软货币购买已实测通过（25 钻 → +100 能量）。

### 18.7 双 Tab 顶栏统一（2026-08-24）

- **UIHeader 共用**：`MergeViewController.SetHeaderVisible(bool)` 新门面；`CamperHubModule.Enter/Exit` 与门店侧对称管理顶栏（经验徽章 + 能量/金币/钻石）。进关经 `LoadLevel→ExitAll` 自动隐藏，回主菜单自动恢复。
- **露营车主菜单清理**：停用旧金币/能量面板（顶栏替代）；商店按钮换门店同款（`ui_icon_store`，静态无滑入动画、无红点徽章）；每日签到屏蔽（`CheckDailyPanels` 短路，数据层保留）。
- **位置对齐教训**：跨页面拷贝 anchoredPosition 必须核对 **pivot 与父容器**——露营车商店按钮 pivot(1,1) 套门店 pivot(0.5,0.5) 的坐标导致偏移 50px；正确做法是在 Play 模式实测世界坐标反推（门店中心 right-100/top-70 ↔ 露营车 pivot(1,1) 的 (-50,-20)）。
- **框架修复**：`IAPRewardsHolder.OnIAPManagerLoaded/OnPurchaseComplete` 对 `product==null`（编辑器无商品数据）加守卫——此前开机刷 8 条 NRE。

---

## 十九、2026-08-28 运行稳定化 + 推送通知 + 自动化冒烟

### 19.1 运行报错根因与修复（commit `d117a79`）

启动报 2 个 NullReference（`PUController.GetPowerUpBehavior` / `PUUIController.Update`），根因是**平台宏缺失**：

- `MODULE_POWERUPS` / `MODULE_MONETIZATION` 原**只在 Standalone** 定义 → Android/iOS 目标下 `PUController.Init()`（整个方法体被 `#if MODULE_POWERUPS` 包裹）被编译掉 → `powerUpsLink`/`uiBehaviors` 为 null
- 修复：`ProjectSettings.asset` 的 Android/iPhone/tvOS `scriptingDefineSymbols` 补上两个宏（与 Standalone 对齐）

顺带修复：
- `AudioInitModule` / `CurrencyInitModule` 的 `Unload()` 无空保护 → 退出 Play 时（模块未初始化，`audioController`/`currencyController` 为 null）NRE
- `GameLoading.BootstrapCoroutine` 首段 `yield return new WaitForEndOfFrame()` 在 batchmode（无渲染帧）**永不恢复** → 启动协程卡死（仅 batchmode/CI，GUI 正常）；已加 `Application.isBatchMode` 分支改用普通帧等待

### 19.2 PlayMode 冒烟测试（新增，自动化验证启动）

`Assets/Tests/PlayMode/CakeHomeBootTest.cs`（asmdef `CakeHome.PlayModeTests`，`includePlatforms` 必须为空 = 全平台才是 PlayMode 测试）：

- 流程：加载 Init(0) → 等 `GameLoading` 自动切到 Game(1) → Game 内跑 120 帧 → 断言全程无 Error/Exception（`Application.logMessageReceived` 捕获）
- 运行：`Unity.exe -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter CakeHomeBootTest -logFile Library/PlayModeTest.log`（结果 XML 在项目根 `TestResults-*.xml`）
- 已实测**通过**：修复后 Init→Game 秒切、120 帧零错误（修复前两个 PU NRE 刷屏）
- ⚠️ `LogAssert.NoUnexpectedReceived()` 会拦所有信息日志（如 LoadDll 的 editor-mode 提示）→ 用显式错误捕获代替

### 19.3 推送通知接入（commit `8da5b95`）

- `PushNotificationManager` 重写为 Unity Notifications **2.3.2 正确 API**：`NotificationCenter.RequestPermission()` + `Notification` + `NotificationDateTimeSchedule.FireTime` + `ScheduleNotification(notification, schedule)`
- 触发点：能量回满（按 `EnergyController` 剩余秒数）+ 每日 18:00 刷新提醒；`#if UNITY_ANDROID || UNITY_IOS` 真机生效
- `com.unity.mobile.notifications` 为**本地副本包**（`Packages/com.unity.mobile.notifications/`），`manifest.json` 用 `file:com.unity.mobile.notifications` 引用 + `packages-lock.json` source=`embedded`——`packages.unity.com` 被墙（ECONNRESET）无法在线注册，这是唯一可用方案
- `HotUpdate.asmdef` 引用程序集名 **`Unity.Notifications.Unified`**（不是 `Unity.Notifications`，旧名解析不到 `NotificationCenter`）

### 19.4 系统备注

- 本机 Windows 是**内置 Administrator 账户**（SID 以 `-500` 结尾），默认不受 UAC 过滤 → 所有进程全权限 → Unity 每次启动提示 "running with Administrator privileges"。已设 `HKLM\...\Policies\System\FilterAdministratorToken=1`（2026-08-28，**重启后生效**）：之后普通启动的进程走受限令牌，警告消失；需要管理员权限的操作会弹 UAC 确认

---

## 二十、2026-08-29 QA 全逻辑测试循环

### 20.1 测试方法（可复用）

- **驱动**：Unity MCP（`127.0.0.1:8080/mcp`，streamable HTTP）+ Python 客户端 `D:\claudeWorkbase\mcp_unity.py`（`exec`/`play`/`console`/`shot`）
- **逻辑测试**：`D:\claudeWorkbase\qa_runner.py`（`level|fail|merge|economy|all`），execute_code 驱动**真实 UI 按钮**（反射调 private，如 `UIGameOver.ReplayButton`）+ 相对断言（before/after 差值，不依赖绝对初值）
- **PlayMode 冒烟**：`Unity.exe -batchmode -runTests -testPlatform PlayMode -testFilter CakeHomeBootTest`
- **屏幕识别限制**：编辑器失焦时 Game view 不重绘，`ScreenCapture.CaptureScreenshot` 截到旧帧 → 用 `execute_code` 读 `UIPage.IsPageDisplayed` + `Canvas.enabled` 做精确 UI 断言（比像素截图更强）；截图仅用于首次视觉基线
- **测试前置**：`CurrencyController.Set(Coins,500)` + `EnergyController.Set(50)` + 清棋盘多余物品，让各测试相互独立

### 20.2 覆盖（31/31 通过）

- **消消乐**：进关预扣10/三消匹配/通关/Home返还10/失败不返还/积分续局(-100金币)/半价重试(-5)/返回无页面残留
- **门店**：生成器产出（`SetForcedSpawn`+`TryActivate`）/建筑升级(step+1 扣金币)/订单状态/合成物品放置(`grid.SpawnObject`)
- **经济**：货币增减/能量 TrySpend 边界/每日签到/每日任务进度/积分棋子(5/个)

### 20.3 发现的 bug（已修复，commit `6b0a86e`）

1. **能量超上限**：`GuestRegistration.ClaimRegistrationReward` 用 `Add(30, ignoreCap:true)` → 43+30=73>50。去掉 `ignoreCap`（钳制到 Max）
2. **Kettle typeId 错误**：`SpawnerQueue.Push("Kettle",…)` → MergeDatabase 里是 `"Kettle Spawner"`，游客奖励生成器从未生效。改 id
3. **UI 残留遮挡**：UIGameOver 按钮用异步 `HidePage` 被页面切换打断 → 残留遮挡主菜单。改 `DisablePage` 同步隐藏 + `ReturnToMenu` 兜底隐藏 UIGame/UIGameOver/UIComplete
4. **ReturnToMenu 未解锁 LifeLocked** → 下次进关 `LockLife` 守卫不扣能量。补 `LivesSystem.UnlockLife(true)`（通关路径 Home 已返还，幂等）
5. **SetForcedSpawn 被 IsPoolEmpty 挡**：g1 生成器（池空设计）下强制产出永远失败。forced 分支绕过池空检查

### 20.4 端到端验证（干净存档首通）

重置存档+PlayerPrefs → 重进 Play → 首关进关/三消/通关 → 断言：`coins=70`（首通+游客50）、`energy=50/50 clamped=True`（钳制生效）、`spawnerQueue=1`（Kettle 入队）、console 0 错误。

---

## 附录 A、玩家存档总览（2026-08-24 盘点）

### A.1 存档机制

- 单一文件：`~/Library/Application Support/CakeHome/CakeHome/save.save`（⚠️ 2026-08-27 起 Company/Product 改为 CakeHome/CakeHome，旧路径 `DefaultCompany/tilematch` 已作废）
- 结构：`{"containers":[{key, json}]}`——所有系统共用一个容器列表，按 key 区分；`SaveController.GetSaveObject<T>(key)` 读写
- 另有 `SavePresets/`（开发用存档预设，非玩家数据）

### A.2 全部 53 个 key 盘点

**活跃系统（27 个）**

| key | 所属 | 内容 |
|---|---|---|
| audio / haptic | 设置 | 音量、震动开关 |
| currency:0 / currency:1 | 货币 | 金币、钻石余额 |
| level | 关卡 | 关卡进度（MaxReached 等 5 字段） |
| powerUp_Hint/Shuffle/Undo/ExtraSlot | 道具 | 4 道具数量 |
| advertisement_forced_ad | 广告 | 强制广告关闭截止时间 |
| iapGlobalSave | IAP | 首购标记 |
| iap_StarterPack / iap_BoostPack / iap_ProPack / iap_Gems1~6 / iap_NoAdsPack | IAP | 现阵容 10 商品的购买次数 |
| Lives | 能量适配层 | 无限模式状态（旧命数字段已废弃） |
| Resources | 能量 | Energy 值 + 恢复时间戳 |
| Experience | 经验 | 等级 + 进度 |
| Zone | 区域 | 当前区域 ID |
| Building | 装修 | 各建筑升级步数 |
| Tasks_{zoneId} | 订单 | 进行中订单 + 队列状态（每区域一份） |
| MergeGrid | 棋盘 | 7×9 格子全量状态（最大的一份） |
| SpawnerQueue | 生成器奖励队列 | 待领取生成器（Kettle g5/g6、Energy Chest…） |
| Fragments | 碎片（已禁用） | 碎片数（功能停用，数据保留） |
| MergeStats | 统计 | 订单完成总数 |
| TUTORIAL:FirstLevel / TUTORIAL:FirstStartTutorial | 教程 | 完成标记 |
| CurrencyProduct_{guid} ×3 | 软货币商品 | 能量商品购买标记（1 已购 2 未购） |
| TimerProduct_uniqueTimerSaveID | 计时商品 | 旧计时金币残留（功能已下线） |

**已下线系统的遗留（5 个）**：`shop`（旧挂机门店）、`recipe`（旧配方）、`ingredient`（旧原料）、`iap_NoAds / iap_GoldSmall / iap_GoldMedium / iap_GoldBig / iap_PUPack`（旧商品枚举键）、`iap_0 / iap_2 / iap_3 / iap_4`（更早期的数字键 IAP 存档）——**三代 IAP key 同存**（数字键→旧名→新名），均只读无害，新玩家不会再产生。

**功能禁用但保留（4 个）**：`avatar`（我的 Tab）、`daily_reward`（签到）、`daily_task`（每日任务）、`TimerProduct_*`。

### A.3 结论

- 存档**物理上已是单文件**；分散的是代码里的 key 注册点（19 处 `GetSaveObject` 调用，跨 HotUpdate/Game.Scripts 两程序集），上表即统一索引
- 测试污染的存档建议直接删除重置：`rm ~/Library/Application\ Support/CakeHome/CakeHome/save.save`（彻底重置还需清 PlayerPrefs：`GuestReg`/`RegRewardClaimed`/`FirstLaunch` 等键）
- 若要对线上玩家做死 key 清理：shop/recipe/ingredient/旧 iap 键可在加载后一键移除（目前未做，无害）

---

## 附录 B、合成配方数据分析（Merge Database，13 条链）

### B.1 链拓扑

```
Kettle Spawner g4~g9 (茶壶)                Mixer Spawner g4~g9 (搅拌机)
  ├─ Coffee g1~g12 (主链, 权重 95→65%)       ├─ Cake g1~g12 (主链, 权重 95→65%)
  └─ Soda g1~g5 (副链, 5→10%)                └─ Candy g1~g5 (副链, 5→10%)

Coins/Gems/Energy Chest g1~g5 (宝箱, 次数 3→7)
  └─ Coins/Gems/Energy Item g1~g5 (货币棋子, 合成升级后收取)

Placeholders g1~g7 (占位链)
```

### B.2 生成器产出池（Kettle/Mixer 完全对称）

| 等级 | 主链 g1 | 主链 g2 | 主链 g3 | 主链 g4 | 副链 g1 | 副链 g2 | 副链 g3 |
|---|---|---|---|---|---|---|---|
| g4 | 95 | 5 | – | – | – | – | – |
| g5 | 84 | 8 | 3 | – | 5 | – | – |
| g6 | 80 | 10 | 4 | – | 6 | – | – |
| g7 | 76 | 11 | 5 | – | 7 | 1 | – |
| g8 | 71 | 13 | 5 | 1 | 8 | 2 | – |
| g9 | 65 | 15 | 5 | 1 | 10 | 3 | 1 |

- **g1~g3 产出池为空**（原版设计，低级生成器是合成材料不是产源）
- 能耗：每次点按 1 能量（`SpawnerObject.ENERGY_COST=1` 常量，非按等级配置）
- 高等级生成器提升高阶产出权重，g1 产出从 95% 递减到 65%——升级生成器显著降低合成工作量

### B.3 宝箱（Coins/Gems/Energy Chest）

- 次数：g1=3 / g2=4 / g3=5 / g4=6 / g5=7 次点击
- 产出池随等级上移：g1 箱(88% g1 + 12% g2) → g5 箱(55% g4 + 45% g5)
- 货币棋子 g1~g5 可继续合成升级（5 级满），是金币/钻石/能量的补充来源
- 获取渠道：Booster/Pro 包（Coins Chest g2/g4）、商店能量区（Energy Chest）、生成器奖励队列

### B.4 数据观察

1. **经济对称性**：两条产线（咖啡/蛋糕）权重表完全一致，换皮不换数值——后续调平衡改一处要同步另一处
2. **主链 12 级是订单需求天花板**：订单索要高阶物品时，按 g9 生成器 65% g1 产出率估算，合一个 g12 物品约需 2^11/0.65 ≈ 3150 次点按（3150 能量）——高阶订单必须靠宝箱/生成器升级/等待恢复来摊薄
3. **副链只有 5 级**：定位是快速可得的订单填充物
4. **能量消耗是硬编码常量**：想按生成器等级差异化能耗（如高级生成器 2 能量/次），需要把 `ENERGY_COST` 改为读 `SpawnerGradeConfig` 数据

---

## 附录 C、玩法流程现状（2026-08-24）

### C.1 首次启动流程

```
Init 场景 → Game 场景 → GameController 初始化
  → 合成棋盘预建（InitGrid 后立即 ExitHub 隐藏）
  → 教程激活 → 加载屏淡出（0.7s 延迟）
  → 默认落地「露营车」Tab（SelectTab(Camper, force)）
        ├─ 第一关教程 FirstLevelTutorial（未完成时优先启动）
        └─ 首次进「门店」Tab 触发 FirstStartTutorial：
           锁拖拽/锁生成/强制产出的全剧本 onboarding
           完成后才解锁 atUpgrade:0 区域初始进度（TriggerInitialProgressionIfOnboarded）
```

### C.2 露营车线：关卡循环

```
Play（能量≥10 或无限模式）→ 预扣 10 能量 → UIGame（多层三消 + Dock）
  ├─ 赢 → UIComplete：金币到账 + 门票全额返还
  │   ├─ Next Level：再扣 10（连胜净消耗 = 0，"赢就免费"）
  │   └─ Home → 回地图
  └─ 输 → UIGameOver（门票不退）
      ├─ 复活：激励广告续局
      ├─ 半价重试：5 能量（赢也返还）
      └─ Home（净亏 10 能量）
```

### C.3 门店线：合成经营循环

```
点生成器（1 能量/次，g4+ 才产出）→ 物品合成升级
  → 顾客订单（绑定棋盘活物，Give 交单）→ 金币
  → 店面视图花金币升级建筑 → 进度步骤：
    经验（升级弹窗发奖）→ 剧情对话 → 新订单批次/随机池
    → 区域升满 → 解锁下一区域（Zone 1→2→3）
```

### C.4 经济大循环（双 Tab 咬合）

| 资源 | 露营车线 | 门店线 | 补口 |
|---|---|---|---|
| ⚡能量 | 门票 10（赢返还） | 生成器 1/次（净消耗） | 120s 自回、升级奖励、广告/钻石、能量宝箱 |
| ⭐金币 | 通关奖励 | 订单产出 → 装修消耗 | 金币宝箱、IAP |
| 💎钻石 | — | 买能量/宝箱 | 仅 IAP（6 档） |

**核心张力**：能量统一后两线抢同一池——消消乐"赢就免费"使它实质成为免消耗区，门店是能量黑洞，玩家被迫在两线间做资源分配。

### C.5 已裁剪功能（无 UI 入口，数据保留）

每日签到、每日任务、配方碎片、NO ADS 按钮、「我的」Tab（详见 18.2~18.5）。

### C.6 流程观察（设计层面待决策）

1. **新手期两线割裂**：~~没有任何引导指向门店 Tab~~ **已修（2026-08-29）**：首胜后 Shop Tab 出现呼吸红点 + 中文气泡"咖啡店有顾客在等!"，玩家点过 Shop 后永久消失（PlayerPrefs 标记）。实现：`UIBottomNavBar` 的 BuildShopNudge/RefreshNudge/DismissNudge + `GameController.OnLevelCompleted` 钩子。长线召回（订单/可升级红点）留给 A/B 实验后定
2. **连胜链条隐患**：高手可无限连玩消消乐（净 0 消耗），门店点按全是净消耗——长线能量必然流向门店，需确认这是否是预期节奏
3. **金币单向流**：消消乐通关金币也汇入装修线，消消乐实质是经营线的辅助供血
4. **钻石出口单一**：仅买能量/宝箱 + 两个礼包，缺长期消耗钩（加速/专属生成器等）→ 方案已落附录 D.7 TODO
5. **风景照片收集品无闭环（2026-08-31 盘点，待设计）**：合成棋盘的 `Placeholders` 链（7 张风景照片，Cozy Cafe/Road Trip/Mountain Valley/Wildflower Meadow/Forest Path/Tropical Beach/Sunset Lake，图源 `game_placeholder_1~7.png`）预埋在 Level.asset `lockState:2` 格子里。现状：旁边合成 → 开箱 → 半锁定展示（不可拖/不可合/不可卖），点亮图鉴（`UIInfoWindow` via `MergeGrid.TryGetInstanceState`），**之后无任何系统消费**（无订单/任务/相册），半锁定在玩法内无解除路径。候选方案：① 旁边再合成一次解除半锁定变自由物品（`UnlockNeighbors` 加分支，改动最小）② 点按收进相册 UI，格子释放 ③ 集齐 7 张发奖励（钻石/门票）。待产品拍板后实现

---

## 附录 D、经济模型（2026-08-29 落地）

> 三线审计（升级/货币/合成经济）后的统一数值模型。**改数值前先读这里**。

### D.1 货币三层的定位

| 货币 | 性质 | 产出 | 消耗 | 设计意图 |
|---|---|---|---|---|
| ⚡能量 | 时间货币 | 自回 120s/点（上限 50）·广告·钻石·宝箱·升级奖励 | 三消门票 10（赢返还）·生成器 1/次 | 卡在线时长与回流节奏；1 能量 = 120 秒 = 1 次产出 |
| ⭐金币 | 玩法货币 | 订单（主）·卖物（中性）·三消通关（辅）·金币宝箱 | 建筑升级（唯一出口） | 合成→订单→装修的循环传动带 |
| 💎钻石 | 付费货币 | IAP（主）·宝石宝箱（微量留存钩） | 买能量（20 钻=100）·宝箱·礼包 | 付费省时间；免费微量产出是留存手段不是付费替代 |

### D.2 核心公式

**生产代价**：合成到 g_k 需要 2^(k-1) 个 g1 ≈ 2^(k-1) 能量 ≈ 2^(k-1)×120 秒自回。

**售价曲线**（消除倒挂，合成中性）：`sell(g) = 2^(g-3)`（g≥3）

| g3 | g4 | g5 | g6 | g7 | g8 | g9 | g10 | g11 | g12 |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 2 | 4 | 8 | 16 | 32 | 64 | 128 | 256 | 512 |

- 2×sell(g) = sell(g+1) → 为卖而合不赚不亏，卖只是回收渠道
- 订单奖励 ≈ 组件卖价 ×2（如 Coffee g7 订单 32 = 卖价 16×2）→ 订单永远是更优出口

**订单奖励桶**（Zone 2/3）：16/32/64/128/256/512/1024 七档，均值 ≈207 金币/单。

### D.3 升级费用曲线（建筑 = 金币唯一出口）

| 区域 | 模式 | 总额 | 节奏目标 |
|---|---|---|---|
| Zone 1 | 5–100（原配） | 565 | ~1 天（新手期） |
| Zone 2 | Zone 1 ×7，取整 5 | 3,955 | ~19 单 ≈ 1.5–2 天 |
| Zone 3 | Zone 1 ×17，取整 5 | 9,605 | ~46 单 ≈ 3–4 天 |

单建筑 3 级、每区 5 建筑 15 步，费用沿用 Zone 1 的楼间分布形态（25/30/100/15/40/70/20/10/50/5/30/80/10/20/60 按比例放大）。

### D.4 等级与区域门槛

- 经验是绝对进度快照（targetLevel），Zone 1→Lv8 → Zone 2→Lv24 → Zone 3→Lv40
- 区域解锁门槛 = 上一区域满级：Zone 2 需 Lv8，Zone 3 需 Lv24
- 升级奖励（Experience Database）：Lv2–5 = 15/15/25/25 能量，Lv6–40 = 每级 25 能量

### D.5 已固化的机制决策

1. **宝箱合成继承 min 剩余次数**——堵死"开到剩 1 次再合成回满"的屯箱策略
2. **钻石宝箱保留**——1/3/7/15/32 的微量产出是留存钩，不冲击 20 钻=100 能量档（那是省时间的付费捷径）
3. **胜利门票即时返还**——通关即返（杀进程不丢），Next Level 重新预扣下一张票，净额不变
4. **能量时间锚定**——满格扣费时重置回复时间戳，满格期间不再"存时间"
5. **Energy Item 面值** 2/5/10/20/100（g5=100 是有意的头奖档）
6. **Kettle/Mixer g4 对称**：95% g1 + 5% g2

### D.6 数值修改入口速查

| 要改什么 | 文件 |
|---|---|
| 售价/产出池/宝箱次数 | `Data/Level System/Merge Database.asset` |
| 升级费用 | `Game/Prefabs/Building/Zone N.prefab`（每建筑 upgrades 的 cost） |
| 订单奖励 | `Data/Level System/Zone N/Zone N Data.asset`（coinsReward） |
| 区域门槛/经验目标/步骤奖励 | 同上（minLevelToUnlock / targetLevel / reward） |
| 升级等级奖励 | `Data/Experience Database.asset` |
| 能量上限/回速/门票 | `Data/Energy Data.asset` + `LivesSystem` 常量（10/5） |
| IAP 档位 | `Data/IAP Settings.asset` + `Game/Prefabs/UI Store/UI IAP Store.prefab` |

### D.7 TODO — 钻石长期消耗钩（gem sink）

**问题**：钻石目前只有"买能量/宝箱/一次性礼包"三个出口，首充后无复购理由；且宝箱棋子免费产钻石，进一步贬值唯一出口。需要**重复可买、价值不贬值、免费渠道不可替代**的消耗口。

**候选方案**（按优先级）：

| # | 方案 | 机制 | 消耗频率 | 改动面 |
|---|---|---|---|---|
| 1 | **生成器加速** ⭐首选 | 钻石跳过生成器冷却/立即补满产出次数 | 每次游戏高频 | `SpawnerController` + `UISelectionPanel` ActionButton 框架（已有） |
| 2 | 订单刷新/插队 | 钻石刷新不想做的订单、立即叫下一位顾客 | 日常高频小额 | `TaskController` 订单轮换 + 订单卡按钮 |
| 3 | 棋盘扩容 | 钻石买临时背包栏位/额外格子 | 中频刚需 | `MergeGrid` 动态行列 + 存档结构 |
| 4 | 专属生成器 | 钻石购永久限定生成器（直接产 g3 物品） | 一次性+身份 | Merge Database 新链 + 商店页 |
| 5 | 限时皮肤 | 店面/露营车外观（纯装饰） | 一次性收藏 | BuildingBehavior 皮肤槽 |

**方案 1 实施清单（TODO）**：

- [ ] 生成器加冷却参数：Merge Database `SpawnerGradeConfig` 增 `cooldownSeconds`（0=无冷却，保持现行为）
- [ ] `SpawnerController.TryActivate` 冷却拦截 + 冷却剩余时间存档（SpawnerObject CustomData）
- [ ] `UISelectionPanel` 生成器选中态加"⚡x钻 立即完成"按钮（复用 ActionButtonConfig 新类型 `ActionButtonType.SpeedUp`）
- [ ] 定价建议：按生成器等级 1/2/3/5/8 钻（g1–g5），约为"等值能量购买价"的 60%——让玩家算得过账
- [ ] 数值校验：加速带来的能量等效收益必须 < 直接买能量（20钻=100能量），否则自我蚕食
- [ ] 埋点：`CustomAnalytics` 记录加速消费（对照 rewarded video 的 `multiply_reward`）

**红线**：不做任何影响合成物品产出概率的付费项（公平性）；装饰类商品不进能量/金币公式。

---

## 十四、设计文档摘要（GDD 概念设计）

> 以下 4 份设计文档（外部 .md 文件）规划了游戏的完整愿景。  
> 标注 ✅ = **代码已实现**，🟡 = **部分实现**，🔴 = **待实现**

---

### 14.1 角色画像 — 用户定位

**文件**: `角色.md`  
**核心人群**: 18-35 岁女性为主，追求 ASMR 解压 + 审美表达 + 碎片化放松

| 画像类型 | 占比人群 | 核心动机 | 商业化匹配 |
|----------|----------|----------|------------|
| 🍰 治愈系创作者 | 女大/职场新人 | ASMR 解压，捏蛋糕成就感 | 限定食材/模具微氪 |
| 🧩 碎片化消除玩家 | 28-45 上班族/宝妈 | 打发碎片时间，消除爽感 | **70% IAA 广告收入** |
| 👗 换装收集控店长 | 暖暖/经营类爱好者 | 身份认同、全图鉴、装扮 | **高额 IAP 核心付费** |

**四大心理驱动**: ASMR 解压 ↔ 低门槛自我表达 ↔ 秩序感微挫败掌控 ↔ 温暖陪伴经营养成

**🎯 设计影响**: 三个 Tab 分别对应三类用户——露营车（消除玩家）、门店（经营控）、我的（换装收集控）

---

### 14.2 烘焙积分中台 — 单一通货经济架构

**文件**: `蛋糕店.md`  
**核心**: 全游戏仅一种通货 **「烘焙积分（Baking Credits）」**，三 Tab 闭环流通

```
                           烘焙积分（唯一底层货币）
                                │
            ┌───────────────────┼────────────────────┐
            ▼                   ▼                    ▼
      【门店&创意工坊】    【露营车消消乐】      【我的 Tab】
      🟢产出:              🟢产出:               🔴纯消耗:
       · 售卖DIY蛋糕         · 通关固定奖励         · 店长换装(Avatar)
       · 客流好评            · 隐藏积分块          · 名片框/称号
      🔴消耗:               · 解封【隐藏配方】     · 社交主页特效
       · 采购原材料          🔴消耗:
       · 扩展货架/装修       · 买额外步数
       · 开分店              · 买辅助道具
```

**核心亮点 — 隐藏配方闭环**: 消消乐掉落碎片 → 消耗积分解封配方 → 门店售卖 2x 回报

**战略优势**:
- ✅ **无废币** — 单一通货，每分都有价值
- ✅ **玩家自由选择** — 高手刷消除暴富 / 经营大师靠门店裂变 / 装扮党看广告补积分
- ✅ **极简数值** — 只需监控 `PlayerCredits` 单变量通胀/紧缩

**代码实现状态**（2026-08-23 更新）:
- ✅ `CurrencyController` + `CurrencyType.Coins`（统一金币收口，关卡/注册/消除加分/订单奖励全走 Coins）
- ✅ 门店合成玩法：棋盘合成 + 顾客订单得金币 + 店铺装修升级（替代原挂机产出，见五/十七章）
- ✅ 蛋糕碎片闭环：消除掉落（0.15）+ 订单奖励 → 兑换能量/生成器（`FragmentController`，替代原配方解封）
- ✅ 换装 Avatar（3 部位）+ 称号（订单/装修/区域条件）+ 个人页统计（`AvatarController`/UIProfilePage）
- 🟡 保鲜度：随门店挂机系统一并移除，如需在合成体系重做可参考 M3 设计
- 🔴 隐藏配方的"解封前隐藏"机制（已随配方系统删除，后续可在合成物品链上实现"图鉴未解锁"形态）
- 🔴 门店扩建/开分店
- 🔴 烘焙积分命名（当前是 Coins，需全量重构为 BakingCredits）

---

### 14.3 门店库存策略 — 策略经营子系统

**文件**: `门店.md`  
**核心理念**: 将门店从挂机动画提升为「库存管理 + 策略销售」子系统

| 系统 | 设计目标 | 代码状态 |
|------|---------|---------|
| 货架网格 | 有限格子（初始6→18），蛋糕大小不同（1-4格） | 🔴 未实现（当前是无格子上架） |
| 保鲜度 | 6小时/3周期倒计时，3档状态（🟢全价/🟡临期打折/🔴过期废弃） | 🟡 简化版已实现（随时间衰减 + 折扣入结算公式） |
| 动态市场 | 天气/时段影响客流偏好（雨天→热可可、周末→水果蛋糕） | 🔴 未实现 |
| 顾客类型 | 学生（低价快周转）/ 商务（高颜值高毛利）/ 派对采购（大单） | 🔴 未实现 |
| 定价策略 | 高低搭配（20%引流/50%主力/30%形象）+ 打折清仓 + 套餐打包 | 🔴 未实现 |
| 供应链联动 | 门店显示缺料→倒逼消消乐定向刷原料 | 🟡 `CakeElement` 已定义6种元素 |

**已实现可复用部分**:
- `ShopShelfSlot` 货架展示位（需扩展为网格系统）
- `CakeCatalog` / `CakeDefinition` / `CakeElement` / `OwnedCake` 数据结构
- `ShopConfig` 经济参数配置（`initialShelfCount`/`maxShelfCount`/`expandCosts`）

**开发建议**: 本质是 `CakeInventoryItem` 数据表（CakeID/QualityScore/CreateTime/ShelfLife/Discount），不需要额外 3D 美术。

---

### 14.4 SNS 社交系统 — 留存曲线优化

**文件**: `SNS分裂设想.md`  
**目标**: 将 D7-D30 留存从"崖式斜率"掰成"平缓高原"

**三大心理引擎**:

| 引擎 | 心理 | 对应功能 | 解决流失阶段 |
|------|------|---------|-------------|
| 认同感 | UGC 自我表达与炫耀 | 甜品写真馆 / 全服点赞大赛 | D3-D7 |
| 互惠 | 社交羁绊与愧疚感 | 烘焙车队(Guild) / 卡关求助送槽位 | D7-D14 |
| 攀比 | Social FOMO | 排行榜 / 店长评分 / 俱乐部积分 | D14-D30 |

**三 Tab 渗透方案**:

```
【门店】                  【露营车】              【我的】
好友串门/偷吃金币        车队/公会消除           甜品写真馆/点赞大赛
借用好友限定食材        卡关求助送槽位          店长穿搭/海报分享
```

**节奏控制（避坑）**:
- Day 1（第5关前）: ❌ 不弹社交绑定的任何窗口
- Day 1（第5关后）: ✅ 一键带框截图分享
- Day 2（第15关后）: ✅ 解锁甜品写真馆/全服点赞
- Day 3（开第二家店时）: ✅ 解锁烘焙车队/公会

**代码状态**: 🔴 全部待实现

---

### 14.5 设计-实现对照总表

| 模块 | 当前代码实现（2026-08-24） | GDD 远期规划 |
|------|--------------------|--------------|
| 门店 | **合成玩法**：棋盘合成 + 顾客订单 + 店铺装修升级（mergedev 模板，见五/十七章） | 库存网格/动态市场/定价策略 |
| 经济 | 单货币（Coins）+ Gems + **统一能量池**（消消乐门票与合成燃料同池）；蛋糕碎片已禁用 | 单一烘焙积分 + 隐藏配方发现机制 |
| 消消乐 | 多层网格3消，Dock提交，消除实时加分；进关耗 10 能量赢返还 | 定向刷原料 |
| 我的 | ⚠️ **Tab 已禁用**（Avatar/称号/统计代码保留可恢复） | 店长换装扩展/写真馆/个人主页 |
| 留存 | 失败挽留（积分续局/半价重试 5 能量）+ 每日签到（任务面板已屏蔽） | Day1-3 分阶段解锁社交 |
| 社交 | 无 | 好友串门/车队公会/点赞大赛 |
| 新手引导 | FirstLevelTutorial + **FirstStartTutorial 完整版已接入**（合成玩法 onboarding） | — |
| 关卡地图 | 垂直滚动 Chunk 池化 | 露营车旅行主题联动 |
| 框架 | Watermelon Core 新版（2026-08-22 升级，见十六章）+ Merge 模板资源整合于 Game.Scripts | — |

---

## 十五、开发路线图（从代码现状到 GDD 愿景）

### Phase 1 — 经济统一（当前优先）
- 货币从 Coins 重构为 Baking Credits（单一通货）
- 消消乐通关产出积分 + 隐藏配方碎片
- 门店购入原材料消耗积分

### Phase 2 — 门店策略化（已转向：合成玩法已落地，以下为在其上的深化）
- 合成棋盘主题化（蛋糕店皮肤/棋盘装饰解锁）
- 订单策略化：限时订单/偏好顾客/小费机制
- 装修更多层级与区域（Zone 4+）
- 钻石消耗出口扩充（买生成器/加速等；当前仅补能量一个出口）
- 风景照片收集品闭环（C.6 观察 5：解锁后仅展示，待定为可收取/集齐奖励）

### Phase 3 — 社交系统
- 甜品写真馆 + 全服点赞大赛
- 烘焙车队(Guild) + 车队里程
- 好友访问 + 食材借用

### Phase 4 — 商业化
- **钻石长期消耗钩**：设计方案与实施 TODO 见附录 D.7（首选：生成器加速）
- 店长 Avatar 换装系统
- 赛季通行证（Bakery Pass）
- 限定食材/模具 IAP 礼包
---

## 美术风格规范（2026-09-05 确立，全项目基准）

> 这是**当前确定的唯一美术风格**，此后所有 UI/场景/物品/图标资源一律以此为基准生成或绘制。
> 参考图：`C:\Users\Administrator\Pictures\Screenshots\Gemini_Generated_Image_*.jpg`（4 张风格/店面/UI/棋盘示意）
> 生成通道：火山 Seedream 5.0 Pro（key ark-7d0a5146-7919-44cf-b458-be91bbcf0c08-f0727），img2img 保持原构图。
> 看图片质检验证：`python D:\claudeWorkbase\volc_vision.py <图路径> "<问题>"`（doubao-seed-2-1-pro）。

### 风格关键词
**暖调治愈烘焙风**（casual bakery / Yummy Tales / 烘焙合并休闲类），不是冷调白金皇室。

### 配色（低饱和、无尖锐高饱和）
- **基底**：暖木棕 + 奶油米白 / 浅卡其，整体温暖柔和
- **主体**：马卡龙柔色（柔粉 powder pink / 奶油黄 butter yellow / 浅橙 light orange / 奶棕 / 可可巧克力棕）——均取甜点本身自然色
- **背景/辅助**：低饱和浅雾霾蓝或暖浅底，干净不抢主体
- **功能区分**：低饱和草绿 / 暖橙 / 柔蓝；货币用暖调柔金，钻石用淡紫柔色
- 提示红点用小面积
- **禁用**：高饱和刺眼色、冷调白金、厚重粗金边、宝石抢镜

### 造型（圆角体系）
- 所有 UI 容器（面板/按钮/棋盘格/道具框）＝**超大圆角矩形（squircle 方圆形）**，无尖锐棱角
- 面板：奶米/浅卡其底 + **香槟暖金细描边** + 柔和凸起包边（不要厚重粗金边）
- 按钮：同色系深一度外描边 + 顶部细白高光
- 成品蛋糕/高价值物：配带金边雕花的白瓷高脚盘承载

### 三消棋盘瓦片（关键约束）
- 棋盘格子（`tile_background`）＝**正俯视伪 3D 圆角方形**：干净低饱和奶白+暖杏底、中央平整空白放 icon、边缘柔和，**正俯视下不得有上翻花边/波浪带**（花边若需立体感，只做顶面外缘"垂直向下"的简单衬边，厚度要够，避免纸片感）
- 棋子（icon）＝甜点/糖果主题 3D 立体、图案为主、避免圆形徽章化
- 具体 3D 物体（蛋糕/咖啡/工具）可轻微立体斜视，但棋盘格/容器类坚持正俯视

### Seedream 提示词公共段（img2img 换肤时附加）
```
Warm cozy bakery merge-game art style: cream/butter-ivory base with soft warm wood-brown accents,
soft pastel macaron colors (powder pink, butter yellow, light orange, cream, cocoa),
LOW saturation muted cozy feel, super rounded squircle shapes, thin champagne-gold border line,
soft matte finish with gentle top highlight. NO harsh saturated colors, NO cold white-gold,
NO heavy thick gold frame, NO gemstones. Keep the original shape, icon composition and colors.
Transparent background.
```

### 美术资源目录规范（2026-09-10 建立）

**归集根目录：`Assets/Project Files/Game/Art/`** —— 换皮的唯一入口，不再去别处找图。

| 分组 | 内容 |
|---|---|
| `Gameplay/Pieces` (64) | 三消棋子（蛋糕/糖果） |
| `Gameplay/Tiles` (17) + `Tiles/Special` (6) | 棋盘瓦片与特殊瓦片 |
| `Gameplay/Chests` (15) / `Characters` (6) / `PowerUps` (4) / `Dock` (1) / `Misc` (9) | 宝箱开箱序列、角色立绘、道具、槽位、杂项 |
| `UI/Common` (47) | 通用面板、按钮、进度条 |
| `UI/Icons` (33) | 全量功能图标 |
| `UI/Store` (16) / `UI/Tutorial` (2) | 商店界面、引导指针 |
| `Scene/Zone1` (22) / `Zone2` (2) / `Zone3` (1) / `Backgrounds` (4) | 关卡背景与布景 |
| `Dialog` (13) / `Map` (7) / `Fx` (9) / `Fonts` (6) | 剧情弹窗、地图、特效、字体描边贴图 |
| `_Pending/Zone1Shop` (24) | 已生成但尚未接入的烘焙店素材 |
| `_Unused` (61) | 未被任何 prefab/scene 引用的孤儿资源 |

**关键性质**：284 个使用中资源**全部是 GUID 序列化引用**（prefab/scene/asset），**零代码路径加载**。因此移动/改名/重组目录**不会断任何场景与预制体引用**；换皮时直接覆盖同名文件即可。

**索引**：`Art/_MANIFEST.md`（21 分组 × 文件 × 尺寸 × 原始路径，575 行）

**迁移工具**（`Tools/`）：
- `scan_art_refs.py` — 扫描资源引用，输出 `Tools/art_refs.json`（区分使用中/孤儿）
- `migrate_art.py` — 归集迁移（`--apply` 执行，`--rollback` 按映射表回滚）
- `fix_art_paths.py` — 按映射表批量修复代码里硬编码的旧资源路径
- `gen_art_manifest.py` — 生成 `Art/_MANIFEST.md`
- 映射表：`Tools/art_migration_map.json`（369 条 src→dst，回滚依据）

**遗留**：`Assets/Project Files/Game/Images/` 与 `Assets/Textures/` 现仅剩空文件夹及其 `.meta`，可在 Unity 中直接删除。

**已同步修改的硬编码路径**：`BottomNavSceneSetup.cs` / `DailyRewardUIBuilder.cs` / `DailyTaskUIBuilder.cs` / `ProfileUIBuilder.cs`（共 4 文件 10 处）、`run_generate_zone1.bat`（输出目录）

### 资源替换状态（2026-09-05）
- 换肤脚本：`D:\claudeWorkbase\reskin_apply_batch.py`（Gameplay/Merge 物品，旧冷调，用户已保留不再重换）、`D:\claudeWorkbase\reskin_warm.py`（暖烘焙 UI 图标/宝箱/角色）、`D:\claudeWorkbase\reskin_bg.py`（Backgrounds 关卡背景暖烘焙化）
- done 清单：`reskin_royal_done.txt`（冷调 82）、`reskin_warm_done.txt`（暖烘焙 64）
- **Backgrounds 4 张已换暖烘焙**（commit 7dace77，关卡背景 back_1~4：奶油草地+姜饼屋/暖杏村/糖霜山/暖金暮），关卡实测暖奶黄糖果郊野
- 商店场景布景（Zone 1 的 house/table/bush/fence/fountain ×4 档）按参考图**另设计布局**（2026-09-06 定方案，进行中）
  - **方向**：Zone 1 布景从"蓝房庭院外景"整容成"烘焙店内"，保留 5 物件 × 每物 4 档升级机制(buildingId/存档/成本全不变，只改美术+布局+台词)。Zone 2/3 本次不动，其 prefab 共享 z1 图，故新图全走新目录避免连累。
  - **5 槽映射**：building_1(house)→后墙左高柜 / building_4(fence)→右高柜 / building_2(fountain)→前景玻璃柜 / building_3(table)→圆桌餐椅 / building_5(bush)→三层点心塔(绿植吊灯入背景固定)
  - **资源产出**(commit 后)：`Images/Zone 1 Shop/`（**2026-09-10 已迁至 `Art/_Pending/Zone1Shop/`**）= 空壳背景 `shop1_bg`(1882×3346 竖幅) + 5 物族×4 档透明物件图 `shop1_counter/display/dining/rightcab/tower_1~4`(RMBG 抠透明) + 豪华整店样板 `shop_sample_full`
  - **技术教训**：逐件生成"正立面物件"与"纵深透视空壳"透视不匹配拼装违和；用户已定**改平视整店 + 同源切层**(先生成平视无纵深整店图,再切背景+5物,保证同源对齐)。`shop_flat.png` 平视样板已生成待视觉确认切层
  - **脚本**：`D:\claudeWorkbase\gen_shop_objects.py / gen_shop_t2i.py`(物件生成)、`rmbg_cut.py / cut_all.py`(RMBG 抠图)、`bg2alpha.py`(色键备用)。RMBG 模型 `D:\claudeWorkbase\models_rmbg\rmbg14.onnx`
  - **待续**：视觉确认平视样板→同源切层→Prefab 重排坐标/接 sprite→15 段 completionThought 改店内台词→Play 验证。prefab 已还原 HEAD(避免 SavePrefabAsset 引号格式噪音)，接入时统一用 Unity 改

### ⚠️ img2img 适用边界（2026-09-05 教训）
- **内容型/立体感图片**（瓦片、甜点、宝箱、角色、店招）→ seedream img2img 效果 OK
- **几何符号/基础 UI 图标**（叉、勾、圆钮、加号、心形、开关、纯色按钮底）→ **禁止 seedream**：会把干净线条画乱、画丢内部符号（如 btn_close 白叉丢失）、加白色描边、高饱和刷成米白失去语义区分
- 符号类正确做法：**恢复原版(HEAD)干净几何 + HSV 降饱和(约×0.6)+ 微提亮** 转暖烘焙马卡龙，保留 alpha/渐变/白符号
- 脚本：`D:\claudeWorkbase\reskin_soften.py`（通用: 恢复 HEAD + 低饱和烘焙调色）
- 已应用：`General UI` 18 张基础图标（2026-09-05 commit bc37d69）；大地图关卡按钮三态配色（bd617cc）

---

## 二十一、全量资源地图（2026-09-10 整理，定位用速查）

> 用户提到某类资源时，直接按下表定位。**唯一美术入口 = `Assets/Project Files/Game/Art/`**（见"美术资源目录规范"），其余为数据结构/预制体/代码。

### 21.1 场景 Scenes（3 个）
| 路径 | 说明 |
|---|---|
| `Assets/Project Files/Game/Scenes/Init.unity` | 启动场景（build 0）：加载 DLL + 模块 → 自动切 Game |
| `Assets/Project Files/Game/Scenes/Game.unity` | 主场景（build 1）：三消棋盘 + Dock + 全部 UI Canvas |
| `Assets/Project Files/Game/Scenes/CakeShop.unity` | 门店/Merge 场景，**未加入 build settings** |
| （仅 build 设置含前 2 个，见 `ProjectSettings/EditorBuildSettings.asset`） | |

### 21.2 UI 预制体（`Assets/Project Files/Game/Prefabs/`，共 109 个 prefab）
| 子目录 | 数量 | 内容 |
|---|---|---|
| `UI/Canvas/` | 8 | UI Main Menu / UI Game / UI Game Over / UI Complete / UI Settings / UIProfilePage / DailyRewardPanel / DailyTaskPanel |
| `UI/` | 7 | Currency Panel Simple、Energy Panel、Level Cell、Pointer、Timer Visualiser、Flying Experience/Spawner Reward |
| `UI/Tasks/` | 6 | UI Client Order Card、UI Hammer Card、UI Placeholder Card、UI Spawner Reward Card、UI Order Item Icon、Cell Task Highlight |
| `UI/Info/` | 2 | UI Info Cross Ref Cell、UI Info Grade Cell（图鉴） |
| `UI/Building/` | 2 | Building UI、Star |
| `UI/Level Up/`、`UI/Zones/` | 1+1 | UI Level Up Reward Tile、UI Zone Card |
| `UI Store/` | 2 | UI IAP Store、Store Spacer |
| `UI Store/Offers/` | 8 | 8 档内购卡：Starter / Power / Big Money / Medium Money / Small Money / Free Money With Timer / Money For Ads / No Ads |
| `Settings/` | 2 | Settings Button、UI Settings |
| `Dialog/` | 4 | Message (Character) / (Image) / (User) / Message Idea (User) |
| `Lives System/` | 1 | Quit Pop Up |

### 21.3 玩法预制体
| 子目录 | 数量 | 内容 |
|---|---|---|
| `Prefabs/Tiles/` | 16 | Tile Base + 15 个具体瓦片（C1~C3 系 × T1~T5 型） |
| `Prefabs/Tiles/Effects/` | 4 | Crate / Ice / Link / Unknown Effect（障碍效果） |
| `Prefabs/Items/` | 6 | Merge 物品：Spawner / Merge Item / Chest / Currency / Energy / Locked |
| `Prefabs/Dock/` | 2 | Dock.prefab、Slot.prefab |
| `Prefabs/Power Ups/` | 5 | PU Hint / Shuffle / Undo / Extra Slot + PU Behavior UI |
| `Prefabs/Power Ups System/` | 3 | Power Up Panel / Power Up Purchase Panel / PU Behavior UI |
| `Prefabs/Particles/` | 12 | Block Trail、Crate/Ice/Link Shatter、Item Appear/Delete/Merge、Slot Highlight、Snow Sparks、Poof、UI Level Up、Building Upgrade |
| `Prefabs/Backgrounds/` | 5 | Background 1~4 + Background Tutorial |
| `Prefabs/Building/` | 3 | Zone 1 / Zone 2 / Zone 3 |
| `Prefabs/Map/` | 5 | Chunk_1~4、Map Level |
| `Prefabs/Level Map/` | 2 | Example Chunk、Example Map Level（模板样例） |
| `Prefabs/` 根 | 2 | Initializer.prefab（全局初始化）、SDKs.prefab |

### 21.4 美术位图（`Assets/Project Files/Game/Art/`，369 PNG，索引 `Art/_MANIFEST.md`）
分组明细见"美术资源目录规范"表。补充要点：
- 使用中 284 张全部 GUID 引用，**零代码路径加载** → 换皮直接覆盖同名文件。
- 特殊子目录：`_Pending/Zone1Shop/`（24 张待接入烘焙店素材）、`_Unused/`（61 张孤儿）。

### 21.5 字体 Fonts
| 路径 | 说明 |
|---|---|
| `Assets/Project Files/Game/Fonts/FredokaOne/FredokaOne.ttf` | 英文主字体源文件 |
| `Assets/Project Files/Game/Fonts/Resources/ChineseFont.ttf` | 中文字体源文件（⚠ 无 TMP fallback，中文显示为空格，见"已知问题"） |
| `Assets/TextMesh Pro/Fonts/`、`Assets/TextMesh Pro/Resources/Fonts & Materials/` | TMP 默认 SDF 资产（LiberationSans 系列） |
| `Assets/Project Files/Game/Art/Fonts/FredokaOne 120` / `FredokaOne 50` | 美术字贴图（Glow / Outline / Shadow / Texture） |

### 21.6 音频 Audio（35 个）
| 路径 | 说明 |
|---|---|
| `Assets/Project Files/Game/Audio/Music/music_main.wav` | 主背景音乐 |
| `Assets/Project Files/Game/Audio/Sounds/`（34） | 音效：tile click / tile click blocked / item_merge / item_place / item_pop_01~04 / merge sound / level complete / level failed / level_up / coin_pickup / coins reward / coin_appear / crate crack / ice crack / power up / task_complete / spawner_activate / building_hammer / building_available / button_sound / ui_button_1 / energy_pick_up / energy_restore / life received / item_flying / item_pickup / item_swoosh / level_sparks / message_appear / rubber quick / ui 等 |
| `Assets/Project Files/Data/Audio Registry.asset`、`Audio Settings.asset` | 音频注册表与音量配置 |

### 21.7 数据 / 配置 ScriptableObject
**根目录 `Assets/Project Files/Data/`（79 个 .asset，不含关卡）**
| 路径 | 说明 |
|---|---|
| `Project Init Settings.asset` | 模块初始化顺序总入口 |
| `Game Data.asset` / `Merge Game Data.asset` / `Map Data.asset` | 三消 / Merge / 地图 全局参数 |
| `Currencies Database.asset`、`Experience Database.asset`、`Energy Data.asset` | 货币 / 经验 / 体力 |
| `Character Database.asset` + `Characters/C1~C6 *.asset` | 角色库与 6 个角色 |
| `Dialog/Dialog 1~15.asset` + `Intro Dialog.asset` | 16 个对话脚本 |
| `IAP Settings.asset`、`Ads Settings.asset`、`Monetization Settings.asset` | 内购 / 广告 / 变现 |
| `Power Ups/Power Ups Database.asset` + `PU Hint/Shuffle/Undo/Extra Slot Settings.asset` | 道具库与 4 个道具参数 |
| `Rewards/IAP/`（14）、`Rewards/Ads/`、`Rewards/Hard Currency/`、`Rewards/Timer/`、`Rewards/Settings/` | 奖励定义（礼包 / 看广告 / 体力 / 定时金币 / 金币预览） |
| `Pipeline/URP Asset.asset`、`URP Asset Renderer.asset`、`Default Volume Profile.asset`、`URP Global Settings.asset` | URP 渲染管线 |

**关卡 Merge 数据 `Assets/Project Files/Data/Level System/`**
| 路径 | 说明 |
|---|---|
| `Levels/Level 001~200.asset`（**200 个**） | 三消关卡数据 |
| `Special Levels/Tutorial Level 1~2.asset` | 教程关 |
| `Level Database.asset`、`Level.asset` | 关卡索引与模板 |
| `Merge Database.asset`、`Merge/Merge Level Database.asset` | Merge 合成链 / 关卡库 |
| `Zone 1~3/Zone N Data.asset` | 三 Zone 解锁与建筑升级数据 |
| `Animations/Simple·Instant·Layer·Shuffle Spawn Animation.asset` | 4 种棋子入场动画配置 |

### 21.8 材质 / Shader / 模型 / 动画
| 路径 | 说明 |
|---|---|
| `Assets/Project Files/Game/Materials/Particles/`（11） | 粒子材质（Spark / Mist / Smoke / Ice Sheet / Cartoon White / Board / Pop 等） |
| `Assets/Project Files/Game/Materials/Level Map/Map Wall Universal.mat` | 大地图墙面 |
| `Assets/Project Files/Game/Materials/Shop/Shop0 Skybox.mat` | 商店天空盒 |
| `Assets/Project Files/Game/Models/Particles/smoke_mesh.fbx` | 唯一 3D 模型（烟雾网格） |
| `Assets/HotUpdate/Scripts/Tutorial/Shaders/Tutorial Spotlight Mask.shader` | 教程聚光灯遮罩 |
| `Assets/Project Files/Game/Animations/Pointer/` | Click.anim、Idle.anim、PointerHolder.controller |
| `Assets/Project Files/Game/Animations/UI/Tasks Hammer/` | Icon.controller、Tasks Hammer Idle.anim |
| `Assets/TextMesh Pro/Shaders/`（15） | TMP 官方 shader 集 |

### 21.9 功能模块（`Assets/Project Files/Modules/`）
| 路径 | 说明 |
|---|---|
| `Loading/` | 加载页 prefab ×2 + Loading.anim + LoadingGraphicsBehavior |
| `Level Resize/` | LevelSizeController + Editor（关卡自适应屏幕） |
| `Reward/` | UIRewardsPopup（Watermelon.Reward.asmdef） |
| `Save/` | CustomBackendSaveHandler / CustomBackendCloudHandler（云存档） |
| `Scene Overlay/` | SceneOverlay 编辑器与数据库 |
| `UI/` | SystemMessageBehavior、UIControllerHelper |
| `Inspector Examples/` | 属性示例（模板自带，未用于业务） |

### 21.10 代码
| 路径 | 说明 |
|---|---|
| `Assets/HotUpdate/`（171 个 .cs） | 热更主程序集：Entry.cs + Level/Dock、Merge、Power Ups、Tutorial、UI、Hub、Level Map、Lives System、DailyTask/Reward、IAP Store、Avatar、Combo 等 |
| `Assets/HotUpdate/Scripts/Editor/` | 工具：BottomNavSceneSetup、DailyReward/Profile/DailyTask UIBuilder、MergeSceneImporter、TileTextureGenerator、ChineseFontSetup、BuildAndroid/iOS、CustomActionsMenu |
| `Assets/Scripts/LoadDll.cs` | HybridCLR 热更 DLL 加载器 |
| `Assets/Editor/` | HotUpdateBuildProcessor、UnityBackgroundUpdate（后台刷新保 MCP）、UnityMcpAutoStart |
| `Assets/Project Files/Game/Scripts/`（163 个 .cs） | 非热更业务：Building/Zone、Characters、Dialog、Merge 等（模板载体） |
| `Assets/Watermelon Core/`（499 个 .cs） | 框架源码：Modules（Audio/Currency/Haptic/Initializer/Inspector/Monetization/Pool/Reward/Save/Skins/Tween/UI）、Editor Tools、Examples、Utils |
| `Assets/Tests/PlayMode/CakeHomeBootTest.cs` | 唯一自动化测试（启动冒烟） |

### 21.11 热更与构建
| 路径 | 说明 |
|---|---|
| `Assets/StreamingAssets/HotUpdate/` | 热更 DLL：HotUpdate.dll（237 KB）+ mscorlib / System / System.Core |
| `Assets/HybridCLRGenerate/` | AOTGenericReferences.cs、link.xml |
| `Assets/link.xml` | IL2CPP 防裁剪白名单（保留 Assembly-CSharp / HotUpdate / HybridCLR.Runtime） |
| `ProjectSettings/HybridCLRSettings.asset` | HybridCLR 全局配置 |
| `Packages/manifest.json` | 依赖：hybridclr、unity-mcp、inputsystem 1.14、burst、URP 14.0.12、TMP 3.0.9、ugui 2.0、Newtonsoft 3.2.1、mobile.notifications（本地包） |

### 21.12 空目录残留（无资源，可直接删）
`Assets/Project Files/Game/Images/`、`Assets/Project Files/Game/Resources/`、`Assets/Project Files/Packages/`、`Assets/Textures/CandyTiles/`、`Assets/HotUpdate/Scripts/Tutorial/Images/` —— 仅剩空文件夹 + `.meta`。

### 21.13 工具脚本（`Tools/`，仓库内）
| 脚本 | 用途 |
|---|---|
| `mcp_call.py` | CLI 调 Unity MCP（HTTP 8080），用法 `python Tools/mcp_call.py <tool> '<json>'` |
| `dismiss_unity_popup.ps1` | 关闭 Watermelon 推广模态窗（否则卡死 MCP run_tests） |
| `scan_art_refs.py` / `migrate_art.py` / `fix_art_paths.py` / `gen_art_manifest.py` | 美术归集四件套（扫描 / 迁移 / 修路径 / 出索引） |
| `art_migration_map.json` / `art_refs.json` | 迁移映射表与引用扫描结果 |

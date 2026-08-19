# CakeHome (tilematch) 项目知识库

> Unity 2022.3.62f3 | HybridCLR 热更新 | Watermelon Core 框架  
> 蛋糕主题三消休闲手游

---

## 一、项目概况

- **产品名**: tilematch (蛋糕主题休闲消除手游)
- **引擎版本**: Unity 2022.3.62f3
- **热更方案**: HybridCLR (`Assets/Scripts/LoadDll.cs` → `Assets/HotUpdate/Entry.cs`)
- **核心框架**: Watermelon Core (自研框架，含 Audio/Currency/Monetization/Pool/Tween/UI/Save 等模块)
- **开发状态**: 早期开发中，核心玩法已完成，商店/Hub/关卡地图系统已接入
- **设计文档索引**: `KNOWLEDGE.md` 的最后一章收录了 4 份 GDD 设计文档摘要
- **产品愿景**: 3D DIY 蛋糕 + 露营车消消乐 + 门店经营换装 Hybrid-Casual 手游
- **项目路径**: `D:\claudeWorkbase\cakehome\`
- **场景**: Game.unity (主场景)

---

## 二、目录结构

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
│   │   ├── Lives System/            # 生命值系统
│   │   ├── Other/                   # 背景、开发者面板
│   │   ├── Power Ups/               # 道具具体实现
│   │   ├── Power Ups System/        # 道具框架（控制器/UI/数据）
│   │   ├── Settings/                # 设置界面
│   │   ├── Shop/                    # 店铺挂机经济系统
│   │   ├── Tutorial/                # 新手引导框架
│   │   └── UI/                      # 游戏 UI 页面
├── Project Files/
│   ├── Data/                        # ScriptableObject 数据资产
│   └── Game/                        # 游戏资产（动画/音频/字体/图片/材质/模型/预制体/场景）
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
9. **PUController** — 道具系统
10. **LivesSystem** — 生命系统
11. **ShopController** — 店铺挂机
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

## 五、店铺挂机经济系统

### 5.1 架构

```
ShopController (静态单例)
├── CakeCatalog   — 蛋糕种类目录
├── ShopConfig    — 经济参数配置
├── ShopSave      — 存档
├── ShopDailyTheme — 每日热门主题（随机元素，2x 收益）
└── ShopIdleProducer — 挂机收入 Tick（每秒）

ShopWorld (3D 场景)
├── 等距摄像机（固定 45° 视角 + 拖拽平移）
├── 玩具屋环境（Shop1 预制体）
├── 专用天空盒
└── ShopShelfSlot[] — 货架展示位

CakeDefinition: id / displayName / elements[] / creditsPerHour / displayColor
OwnedCake: DefinitionId / PlacedAt（二进制时间戳） / InstanceId
```

### 5.2 挂机经济模型

- 在线: `ShopIdleProducer` 每秒调用 `ShopController.TickOnline()` 累计积分
- 离线: 进入商店时 `SettleOffline()` 最多结算 8 小时
- 收益计算公式: `Cake.creditsPerHour × multiplier`（managerRecommend: 2x / hotTheme: 2x）
- 货架: 初始 4 个，可扩展至 8 个（`expandCosts[]` 配置）
- 冰柜: 未上架蛋糕暂存区（`GetFreezerCakes()`）
- 系统入口: `ShopHubModule` 切换时初始化 `ShopWorld`

### 5.3 Cake 元素（CakeElement）

枚举：`Strawberry` | `Chocolate` | `Matcha` | `Blueberry` | `Lemon` | `Cream`

每日随机选一个作为热门主题，匹配蛋糕收益 x2。

---

## 六、Hub 模块系统（底部导航栏）

### 6.1 架构

```
UIBottomNavBar — 底部导航栏 UI
HubModuleRouter — 模块路由（单例）
├── CamperHubModule — 关卡地图 + 主菜单
├── ShopHubModule   — 店铺挂机 3D 世界
└── ProfileHubModule — 个人中心
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

- 三栏: Camper（关卡） | Shop（商店） | Profile（个人）
- 纯图标样式，使用特定资源
- 相机渲染方式提升层级

---

## 七、关卡地图系统

- `MapBehavior` — 垂直滚动大地图管理器
- `MapData` — 分块预制体列表 + 滚动参数
- `MapChunkBehavior` — 每块关卡组
- `MapLevelBehavior` — 单个关卡节点（三态: 未解锁/已解锁/当前）
- 支持 `rubber-band` 滚动回弹、宽屏适配

---

## 八、生命系统

- `LivesSystem` — 静态单例，管理生命恢复/消耗/无限模式
- `LivesData` — ScriptableObject 配置（maxLives=5，恢复间隔=1200s/20min）
- `LivesSave` — 存档（含离线恢复计算）
- 离线期间自动计算已恢复的生命数
- 无限生命模式（`EnableInfiniteMode(seconds)`）由 IAP 或奖励触发

### 关卡失败流程

```
OnSlotsFilled / TimerFinished
  → GameController.OnLevelFailed()
  → LivesSystem.LockLife()
  → UIComplete / UIGameOver 弹出
  → 复活（激励视频广告） / 重玩 / 返回主菜单
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
| Shop Scene Setup | `Editor/ShopSceneSetup.cs` | 代码构建店铺 3D 场景结构 |
| Shop Debug Menu | `Editor/ShopDebugMenu.cs` | 运行时调试（送蛋糕/加积分/强制结算） |
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
| 商店控制器 | `Assets/HotUpdate/Scripts/Shop/ShopController.cs` | ~300 |
| 商店世界 | `Assets/HotUpdate/Scripts/Shop/ShopWorld.cs` | ~400 |
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

**代码实现状态**:
- ✅ `CurrencyController` + `CurrencyType.Coins`（Watermelon Core 已有货币系统，但当前是"金币"而非"烘焙积分"）
- 🟡 `ShopController` 已有 `GetTotalCreditsPerHour()` / `TickOnline()` 挂机产出框架
- 🔴 隐藏配方系统未实现
- 🔴 门店扩建/开分店未实现
- 🔴 换装 Avatar 系统未实现
- 🔴 烘焙积分命名（当前是 Coins，需全量重构为 BakingCredits）

---

### 14.3 门店库存策略 — 策略经营子系统

**文件**: `门店.md`  
**核心理念**: 将门店从挂机动画提升为「库存管理 + 策略销售」子系统

| 系统 | 设计目标 | 代码状态 |
|------|---------|---------|
| 货架网格 | 有限格子（初始6→18），蛋糕大小不同（1-4格） | 🔴 未实现（当前是无格子上架） |
| 保鲜度 | 6小时/3周期倒计时，3档状态（🟢全价/🟡临期打折/🔴过期废弃） | 🔴 未实现 |
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

| 模块 | 当前代码实现（v0.1） | GDD 远期规划 |
|------|--------------------|--------------|
| 门店 | 挂机积分产出，货架+冰柜 | 库存网格/保鲜度/动态市场/定价策略 |
| 经济 | 单货币（Coins） | 单一烘焙积分 + 隐藏配方闭环 |
| 消消乐 | 多层网格3消，Dock提交 | 掉落配方碎片、定向刷原料 |
| 我的 | ProfileHubModule（占位） | 店长换装/甜品写真馆/个人主页 |
| 社交 | 无 | 好友串门/车队公会/点赞大赛 |
| 新手引导 | FirstLevelTutorial 基本框架 | Day1-3 分阶段解锁社交 |
| 关卡地图 | 垂直滚动 Chunk 池化 | 露营车旅行主题联动 |

---

## 十五、开发路线图（从代码现状到 GDD 愿景）

### Phase 1 — 经济统一（当前优先）
- 货币从 Coins 重构为 Baking Credits（单一通货）
- 消消乐通关产出积分 + 隐藏配方碎片
- 门店购入原材料消耗积分

### Phase 2 — 门店策略化
- 货架网格系统（替换当前 Slot 列表）
- 保鲜度/过期机制
- 动态市场/顾客偏好
- 定价与清仓策略

### Phase 3 — 社交系统
- 甜品写真馆 + 全服点赞大赛
- 烘焙车队(Guild) + 车队里程
- 好友访问 + 食材借用

### Phase 4 — 商业化
- 店长 Avatar 换装系统
- 赛季通行证（Bakery Pass）
- 限定食材/模具 IAP 礼包
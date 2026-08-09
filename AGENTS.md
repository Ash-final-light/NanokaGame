# NanokaGame AI 协作规范

本文件适用于在 `NanokaGame` Unity 项目中工作的所有 AI 编码助手。执行任何任务前，应先阅读本文件，并以这里的约束作为默认行为。

## 1. 项目概况

- 项目类型：小型 2D Unity 游戏项目。
- Unity 版本：`2022.3.62f1c1`。
- 渲染管线：Universal Render Pipeline 2D。
- 主要 UI：Unity uGUI 与 TextMeshPro。
- 已安装动画插件：DOTween / DOTween Pro。
- Unity MCP：`com.ivanmurzak.unity.mcp 0.87.0`。
- 当前主要场景：
  - `Assets/Scenes/Title.unity`
  - `Assets/Scenes/Klotski.unity`
- 项目未来的功能范围仅包括：
  - 通用 UI 功能；
  - 音频与音量设置；
  - 华容道游戏；
  - 连连看游戏。

除非用户明确要求，不要主动增加联网、账号、商城、广告、复杂存档、热更新、多人游戏、依赖注入框架或其他超出上述范围的系统。

## 2. 小型项目原则

- 优先选择简单、直接、容易维护的实现。
- 不为假设中的未来需求提前设计复杂架构。
- 不引入不必要的 Manager、Service Locator、Event Bus、依赖注入框架或多层抽象。
- 只有在确实存在多个实现、明确测试需求或能够显著消除重复时才创建接口或抽象基类。
- 一个小功能优先由少量职责明确的组件和普通 C# 类完成。
- 游戏规则与 Unity 表现层分离，但不要为了分层而分层。
- 不增加新的第三方包，除非用户明确授权。
- 项目已经安装 DOTween；需要简单 UI 动画时可以复用它，不要再引入其他 Tween 插件。

## 3. C# 命名空间硬性规则

### 3.1 所有第一方 C# 代码必须有命名空间

所有由项目团队或 AI 创建、修改的第一方 `.cs` 文件都必须显式声明命名空间，不允许把类型放在全局命名空间中。

此规则适用于：

- `Assets/Scripts/**`
- `Assets/Editor/**`
- `Assets/Tests/**`
- 未来新增的其他第一方代码目录

第三方插件代码不受此规则约束，也不得为了统一风格而修改。例如：

- `Assets/Plugins/Demigiant/**`
- `Packages/**`

### 3.2 根命名空间

统一使用：

```csharp
NanokaGame
```

推荐命名空间映射：

| 代码范围 | 命名空间 |
| --- | --- |
| 通用基础代码 | `NanokaGame.Core` |
| UI | `NanokaGame.UI` |
| 音频 | `NanokaGame.Audio` |
| 华容道 | `NanokaGame.Games.Klotski` |
| 华容道 UI | `NanokaGame.Games.Klotski.UI` |
| 连连看 | `NanokaGame.Games.LinkMatch` |
| 连连看 UI | `NanokaGame.Games.LinkMatch.UI` |
| Editor 工具 | `NanokaGame.Editor` 或 `NanokaGame.Editor.<Feature>` |
| EditMode 测试 | `NanokaGame.Tests.EditMode.<Feature>` |
| PlayMode 测试 | `NanokaGame.Tests.PlayMode.<Feature>` |

命名空间应表达功能归属，不要机械地把每一层文件夹都转化为命名空间。

### 3.3 Unity 2022.3 命名空间写法

使用传统大括号形式，不使用 C# 10 的 file-scoped namespace：

```csharp
using UnityEngine;

namespace NanokaGame.UI
{
    public sealed class TimeCounter : MonoBehaviour
    {
    }
}
```

禁止：

```csharp
namespace NanokaGame.UI;
```

### 3.4 文件与类型

- 一个 `.cs` 文件通常只放一个顶层公共类型。
- 文件名必须与主要类型名一致。
- 类型移动到命名空间后，必须检查引用它的其他脚本、测试、自定义 Editor 和序列化类型名。
- 修改已有 `MonoBehaviour` 或 `ScriptableObject` 的命名空间后，必须让 Unity 完成编译，并确认场景与 Prefab 中没有出现 `Missing Script`。

## 4. 推荐目录结构

新增第一方代码时优先使用以下结构：

```text
Assets/
  Scripts/
    Core/
    UI/
    Audio/
    Games/
      Klotski/
        UI/
      LinkMatch/
        UI/
  Prefabs/
    UI/
    Klotski/
    LinkMatch/
  Scenes/
  Tests/
    EditMode/
    PlayMode/
```

不要仅为了满足目录示例而创建空文件夹。只有实际添加相应功能时才创建。

## 5. C# 编码规范

- 类型、方法、属性和事件使用 `PascalCase`。
- 局部变量和参数使用 `camelCase`。
- 私有字段使用 `_camelCase`。
- Inspector 字段优先使用 `[SerializeField] private`，不要为了序列化而公开字段。
- 能标记为 `sealed` 且不需要继承的 `MonoBehaviour` 可以标记为 `sealed`。
- 常量使用 `PascalCase`，不要使用全大写下划线风格。
- `bool` 名称应表达状态或条件，例如 `IsPaused`、`CanMove`、`HasWon`。
- 方法名应表达动作，例如 `StartGame`、`TryMoveBlock`、`ApplyVolume`。
- 避免不明确的缩写和单字母字段名；循环索引除外。
- 清理未使用的 `using`、字段、方法和注释掉的代码。
- 不使用 `#region` 隐藏过大的类；类过大时按实际职责拆分。
- 注释解释原因、约束和非显然规则，不重复描述代码表面行为。
- 不在正常业务代码中吞掉异常；需要捕获时应记录有用上下文。
- 不在 `Update`、`FixedUpdate` 或高频循环中反复分配集合、执行 LINQ、调用 `Find` 或拼接日志字符串。
- 不在 `Update` 中使用 `GameObject.Find`、`FindObjectOfType` 或 `GetComponent` 获取固定依赖。
- 缓存频繁访问的组件引用。
- 注册事件后必须在合适的生命周期中取消注册。
- 协程、Tween、异步任务在对象禁用或销毁时必须正确停止或释放。

## 6. Unity 序列化与组件规则

- 优先通过 Inspector 注入场景和 Prefab 引用。
- 必填引用应在 `Awake`、`OnEnable` 或开发期校验中明确检查。
- 不要静默查找并掩盖缺失的关键引用。
- 不随意重命名序列化字段；必须重命名时考虑使用 `FormerlySerializedAs`。
- 不随意改变已有组件的命名空间、类型名或程序集归属。
- 修改 Prefab 前先读取其当前结构和 overrides。
- 修改 Scene 前先确认当前打开的场景、脏状态和目标对象路径。
- 只修改任务涉及的序列化字段，避免无关的 Scene 或 Prefab 重序列化。
- 不直接编辑 Unity 生成的 `.meta` GUID。

## 7. UI 开发规则

- UI 主要使用 uGUI 和 TextMeshPro。
- 用户可见文本优先使用 `TMP_Text`，输入优先使用 TMP 输入组件。
- 通过 Inspector 绑定按钮、文本、图像和面板引用。
- 按钮监听应避免重复注册，并在需要时取消注册。
- UI 控制器只负责显示、输入转发和界面状态，不应包含完整游戏规则算法。
- 相同界面状态不要分散到多个无关组件中维护。
- 使用 Canvas Scaler 时考虑不同分辨率和宽高比。
- 修改 UI 后应检查：
  - 文字是否溢出；
  - 按钮是否可点击；
  - 锚点和边距是否正确；
  - 面板显示与隐藏状态是否一致；
  - 不同分辨率下是否重叠；
  - EventSystem 是否存在且没有重复。
- 简单动画可以使用现有 DOTween，但必须在对象销毁时正确处理 Tween 生命周期。

## 8. 音频设置规则

- 音频功能保持简单，默认只区分：
  - Master；
  - Music；
  - SFX。
- 优先使用 Unity `AudioMixer` 管理音量。
- UI Slider 的线性值转换为分贝时，应避免 `log10(0)`，必须设置安全下限。
- 音量设置可以使用简单、明确的本地持久化方式；不要引入复杂存档系统。
- 修改 Slider 时避免初始化阶段触发多次无意义保存或播放声音。
- 音频设置应在启动时正确恢复，并立即应用到 Mixer。
- 不创建多个彼此竞争的全局音频管理器。
- 只有在跨场景播放确有需要时才使用 `DontDestroyOnLoad`。

## 9. 华容道规则

- 命名空间使用 `NanokaGame.Games.Klotski`。
- 棋盘状态、合法移动和胜利判断尽量放在不依赖 `MonoBehaviour` 的普通 C# 逻辑中。
- Unity 组件负责输入、动画、音效、UI 和场景对象同步。
- 移动操作应具有明确结果，例如成功、被阻挡或输入无效。
- 动画期间必须防止重复输入造成棋块重叠或状态不同步。
- 逻辑状态应先验证，再驱动显示；不要仅依赖 Transform 位置判断游戏状态。
- 重点测试：
  - 合法移动；
  - 边界阻挡；
  - 方块碰撞；
  - 连续移动；
  - 重置；
  - 胜利条件；
  - 动画期间的输入锁定。

## 10. 连连看规则

- 命名空间使用 `NanokaGame.Games.LinkMatch`。
- 棋盘数据、配对规则和寻路判断应与 GameObject 表现分离。
- 连线算法应明确规定允许的转折次数、边界外路径和障碍物规则。
- 不通过场景中对象的层级顺序代替棋盘数据。
- 消除流程应避免重复点击、动画未结束时重复结算以及同一格被多次处理。
- 重点测试：
  - 相同图案判断；
  - 直线路径；
  - 一次转折；
  - 两次转折；
  - 被障碍阻挡；
  - 边界外连线；
  - 无可消除组合；
  - 洗牌后棋盘有效性；
  - 完成条件。

## 11. 第三方代码和包

- 不修改 `Assets/Plugins/Demigiant/**` 中的 DOTween 源码。
- 不修改 `Packages/**` 中的包源码。
- 不因为格式、命名空间或警告而批量重写第三方代码。
- 需要扩展第三方功能时，在第一方目录中创建适配层或扩展代码。
- 未经用户明确授权，不安装、升级或移除 Unity Package。
- 未经用户明确授权，不更换渲染管线、输入系统或 UI 框架。

## 12. MCP 操作规范

- 修改前先使用只读工具检查目标 Asset、Scene、GameObject 和 Component。
- 只操作用户指定的项目、场景、对象和资源。
- 调用写入工具前确认目标路径和对象引用准确。
- 默认不执行删除操作。
- 删除 Asset、GameObject、Component、Scene 或脚本前必须获得用户明确授权。
- 打开或切换场景前确认当前场景是否有未保存修改。
- 不覆盖用户已有的未提交修改。
- 脚本变化后刷新 AssetDatabase，并等待 Unity 编译结束。
- 如果某个 MCP 工具被禁用，使用安全的本地文件或 Unity 工作流替代；不要擅自扩大权限。

## 13. 标准 AI 工作流程

每个实现任务默认按以下顺序执行：

1. 阅读本文件和任务相关代码。
2. 检查 `git status`，识别用户已有修改。
3. 读取目标 Scene、Prefab、GameObject、Component 或 Asset 的当前状态。
4. 说明计划修改的最小范围。
5. 实施代码或 Unity 对象修改。
6. 刷新 AssetDatabase。
7. 等待脚本编译完成。
8. 检查 Unity Console，确认没有新增错误。
9. 运行相关 EditMode 或 PlayMode 测试。
10. 视觉功能应通过 Game View、Scene View 或隔离截图检查。
11. 保存明确需要保存的 Scene 或 Prefab。
12. 检查最终 Git 差异，确认没有无关序列化变化。
13. 向用户报告修改文件、修改对象、验证结果和剩余风险。

如果用户仅要求分析、诊断或评审，则保持只读，不实施修复。

## 14. 测试要求

- 新增纯游戏规则时优先添加 EditMode 测试。
- 依赖 MonoBehaviour、场景生命周期、协程或 UI 交互时使用 PlayMode 测试。
- Bug 修复应尽可能添加能够复现问题的回归测试。
- 测试命名应表达条件和预期结果，例如：

```csharp
TryMoveBlock_WhenDestinationIsOccupied_ReturnsFalse
FindPath_WhenTwoTurnsAreRequired_ReturnsValidPath
SetMasterVolume_WhenValueIsMinimum_DoesNotProduceInvalidDecibels
```

- 测试代码同样必须使用 `NanokaGame.Tests...` 命名空间。
- 不为了通过测试而弱化实际业务规则。

## 15. 完成标准

只有满足以下条件，任务才算完成：

- 所有新增或修改的第一方 C# 类型都位于正确的 `NanokaGame...` 命名空间中。
- 文件名、类型名和命名空间符合本文件约定。
- Unity 编译成功。
- 没有引入新的 Console Error 或编译警告。
- 相关自动化测试通过；如果没有测试，应说明原因和手动验证方法。
- UI 或动画变化经过视觉验证。
- Scene 与 Prefab 不包含意外的 `Missing Script` 或无关 overrides。
- 没有修改第三方插件和包源码。
- 没有覆盖用户原有的未提交修改。
- 最终报告清楚列出修改内容和验证结果。

## 16. 禁止事项

除非用户明确要求并理解影响，否则禁止：

- 创建无命名空间的第一方 C# 类型；
- 修改 DOTween 或其他第三方插件源码；
- 修改 `Library/`、`Temp/`、`Logs/` 或 `obj/` 中的生成内容；
- 手工修改 `.meta` GUID；
- 删除 Asset、Scene、Prefab、GameObject、Component 或脚本；
- 执行项目级批量重命名或批量序列化；
- 引入复杂架构或超出 UI、音频、华容道、连连看范围的系统；
- 在不知道用户已有修改内容时覆盖文件；
- 未验证 Unity 编译和 Console 状态就宣称任务完成。

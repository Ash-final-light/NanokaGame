# Klotski 华容道设计与 AI 开发规则

本文件适用于 `Assets/Scripts/Klotski/**` 下的全部第一方代码，并补充项目根目录 `AGENTS.md`。发生冲突时，在本目录范围内优先遵守本文件；项目级安全规则、第三方代码保护规则和命名空间规则仍然有效。

## 1. 功能范围

本目录只负责华容道玩法及其直接相关的表现逻辑：

- 棋盘数据；
- 棋子数据；
- 合法移动判断；
- 胜利判断；
- Sprite 棋子显示；
- 棋子拖动、点击或滑动输入；
- 棋子位置自动计算；
- 棋子移动动画；
- 华容道关卡初始化、重置和完成状态；
- 与华容道直接相关的 UI 状态通知。

通用 UI、通用音频设置和其他游戏逻辑应放在各自目录，不要堆积到华容道代码中。

## 2. 命名空间

本目录中的所有 C# 类型必须声明命名空间，禁止使用全局命名空间。

默认使用：

```csharp
namespace NanokaGame.Games.Klotski
{
}
```

仅华容道专用 UI 可以使用：

```csharp
namespace NanokaGame.Games.Klotski.UI
{
}
```

测试代码使用：

```csharp
namespace NanokaGame.Tests.EditMode.Klotski
{
}
```

或：

```csharp
namespace NanokaGame.Tests.PlayMode.Klotski
{
}
```

Unity 2022.3 下使用传统大括号命名空间，不使用 file-scoped namespace。

## 3. 核心设计原则

### 3.1 逻辑棋盘坐标是唯一真相

棋子的真实游戏位置只能由整数棋盘坐标表示，例如：

```text
Cell = (Column, Row)
Size = (WidthInCells, HeightInCells)
```

禁止使用以下数据作为游戏规则的唯一依据：

- `Transform.position`；
- Sprite 像素尺寸；
- Collider 边界；
- Hierarchy 顺序；
- 屏幕坐标；
- DOTween 当前插值位置。

世界坐标只负责显示，必须由棋盘布局器根据逻辑坐标自动计算。

### 3.2 Model 先更新，View 后同步

合法移动的标准流程：

1. 输入层提出移动请求；
2. 棋盘 Model 判断移动是否合法；
3. 合法时更新棋盘逻辑状态；
4. View 根据新的逻辑坐标计算目标世界坐标；
5. View 播放移动动画；
6. 动画结束后恢复输入。

禁止先移动 Transform，再尝试根据 Transform 推断 Model 状态。

### 3.3 自动布局必须集中处理

所有坐标换算必须集中在一个布局类型中，例如：

```text
KlotskiBoardLayout
```

其他脚本不能各自复制坐标公式或保存棋子的手工世界坐标。

### 3.4 小型项目不过度设计

- 不使用 ECS。
- 不引入依赖注入框架。
- 不为棋盘创建复杂消息总线。
- 不使用物理碰撞系统决定棋子是否可以移动。
- 不为简单棋盘逻辑创建过多接口。
- 普通 C# Model、少量 MonoBehaviour View 和一个协调控制器即可。

## 4. 默认棋盘约定

经典华容道默认采用：

```text
Columns = 4
Rows = 5
```

但布局代码必须允许通过序列化字段或配置数据调整列数和行数，不能把世界坐标写死为只适用于某一个场景。

逻辑坐标约定：

- 原点 `(0, 0)` 位于棋盘左下角；
- `Column` 向右递增；
- `Row` 向上递增；
- 棋子位置表示棋子占用区域的左下格；
- 棋子尺寸用格子数量表示。

示例：

```text
1×1 棋子：Size = (1, 1)
横向棋子：Size = (2, 1)
纵向棋子：Size = (1, 2)
目标棋子：Size = (2, 2)
```

如未来关卡需要不同规则，应通过数据配置表达，不要散落条件判断。

### 4.1 当前 Klotski 场景基准

当前 `Assets/Scenes/Klotski.unity` 中的初始关卡采用以下参数：

```text
Columns = 4
Rows = 5
CellSize = 1.68
BoardCenter = (0.06, 0.12)
BoardSize = (6.72, 8.40)
BoardTopLeft = (-3.30, 4.32)
BoardBottomLeft = (-3.30, -4.08)
BoardPlaneZ = 0
```

当前棋子映射与初始逻辑格：

| Scene GameObject | 角色含义 | SizeInCells | InitialCell |
| --- | --- | --- | --- |
| `nanoka_head` | 曹操/目标棋子 | `(2, 2)` | `(1, 3)` |
| `nanoka_body` | 关羽/横向棋子 | `(2, 1)` | `(1, 2)` |
| `nanoka_left_arm` | 左上竖将 | `(1, 2)` | `(0, 3)` |
| `nanoka_right_arm` | 右上竖将 | `(1, 2)` | `(3, 3)` |
| `nanoka_left_leg` | 左下竖将 | `(1, 2)` | `(0, 1)` |
| `nanoka_right_leg` | 右下竖将 | `(1, 2)` | `(3, 1)` |
| `nanoka_body_left` | 中间左卒 | `(1, 1)` | `(1, 1)` |
| `nanoka_body_right` | 中间右卒 | `(1, 1)` | `(2, 1)` |
| `nanoka_skirt_left` | 底部左卒 | `(1, 1)` | `(0, 0)` |
| `nanoka_skirt_right` | 底部右卒 | `(1, 1)` | `(3, 0)` |

底部中央 `(1, 0)` 和 `(2, 0)` 是初始空格。

当前 `Pieces` 下每个棋子只有 `Transform` 和 `SpriteRenderer`。实现交互时需要增加或配置 `BoxCollider2D` 与第一方棋子 View 脚本，但不能改变现有 Sprite、初始 Scale 和角色映射。

## 5. Sprite 棋子规则

### 5.1 棋子表现

每个棋子 View 使用 `SpriteRenderer` 显示，不以 UI `Image` 作为棋盘内棋子的默认实现。

推荐组件组成：

```text
KlotskiPieceView
SpriteRenderer
Collider2D（仅用于输入命中，可选）
```

Collider2D 只负责接收点击或拖动，不负责移动合法性和棋子碰撞判断。

### 5.2 Sprite 导入要求

- 棋子 Sprite 应使用一致的 Pixels Per Unit。
- Pivot 默认使用 Center。
- Sprite 文件名应能表达棋子身份或用途。
- 不通过修改 Sprite 原图尺寸适配棋盘。
- 不要求不同尺寸棋子使用完全相同的 Transform Scale。
- 若需要无失真的矩形拉伸，应准备适合 Sliced/Tiled 的 Sprite，或为不同棋子尺寸提供对应美术资源。

### 5.3 Sprite 尺寸自动适配

棋子的目标显示尺寸由占格大小计算：

```text
TargetWidth  = WidthInCells  × CellSize - PieceGap
TargetHeight = HeightInCells × CellSize - PieceGap
```

其中：

- `CellSize` 是一个逻辑格在世界空间中的边长；
- `PieceGap` 是相邻棋子之间希望保留的总视觉间距；
- `PieceGap` 必须满足 `0 <= PieceGap < CellSize`。

如果使用 `SpriteRenderer.drawMode = Sliced` 或 `Tiled`，优先设置 `SpriteRenderer.size`。

如果使用普通 Simple Sprite，需要根据 `sprite.bounds.size` 计算缩放：

```text
ScaleX = TargetWidth  / SpriteBoundsWidth
ScaleY = TargetHeight / SpriteBoundsHeight
```

缩放计算只能由 View 或布局辅助类型处理，不能影响逻辑棋盘尺寸。

## 6. 位置自动计算规则

### 6.1 布局参数

布局器至少应具有以下参数：

```text
BoardCenter       棋盘世界空间中心
BoardTopLeft      棋盘世界空间左上角锚点
Columns           棋盘列数
Rows              棋盘行数
CellSize          单格世界空间尺寸
PieceGap          棋子视觉间距
BoardPlaneZ       棋盘所在 Z 坐标
```

这些参数应由一个明确对象统一保存，例如棋盘根 Transform 和 `KlotskiBoardLayout` 组件。

### 6.2 `nanoka_left_arm` 左上角锚点

当前场景的硬性锚点规则：

> `nanoka_left_arm` 的 Sprite 左上角就是棋盘左上角。

`nanoka_left_arm` 占用逻辑格 `(0, 3)`，尺寸为 `(1, 2)`。它的左边缘与棋盘左边界重合，顶部边缘与棋盘上边界重合。

不得把 `nanoka_left_arm` 的中心点误认为棋盘左上角。必须使用 SpriteRenderer 世界 Bounds 的左上角：

```text
BoardTopLeftX = LeftArmRenderer.bounds.min.x
BoardTopLeftY = LeftArmRenderer.bounds.max.y
```

当前场景数据：

```text
LeftArmCenter = (-2.46, 2.64)
LeftArmWorldSize = (1.68, 3.36)

BoardTopLeftX = -2.46 - 1.68 / 2 = -3.30
BoardTopLeftY =  2.64 + 3.36 / 2 =  4.32

BoardTopLeft = (-3.30, 4.32)
```

棋盘宽高、中心和左下角由此自动反算：

```text
BoardWidth  = Columns × CellSize
BoardHeight = Rows × CellSize

BoardCenterX = BoardTopLeftX + BoardWidth  / 2
BoardCenterY = BoardTopLeftY - BoardHeight / 2

BottomLeftX = BoardTopLeftX
BottomLeftY = BoardTopLeftY - BoardHeight
```

代入当前 `4×5` 棋盘：

```text
BoardWidth  = 4 × 1.68 = 6.72
BoardHeight = 5 × 1.68 = 8.40

BoardCenter = (0.06, 0.12)
BottomLeft = (-3.30, -4.08)
```

这个左上角锚点不改变逻辑坐标约定。Model 的 `(0,0)` 仍位于棋盘左下角；`nanoka_left_arm` 的逻辑左下格仍是 `(0,3)`。

布局初始化建议流程：

1. 读取 `nanoka_left_arm` 的 `SpriteRenderer.bounds`；
2. 得到 `BoardTopLeft`；
3. 根据行列数和 `CellSize` 计算 `BoardCenter` 与 `BottomLeft`；
4. 用逻辑格坐标重新对齐全部棋子；
5. 校验全部棋子的整体 Bounds 是否为 `6.72×8.40`。

如果以后在 Editor 中整体移动棋盘，只需移动 `nanoka_left_arm` 与棋盘根的设计锚点，布局器必须重新计算其他棋子，不允许逐个更新硬编码世界坐标。

### 6.3 逻辑坐标转世界坐标

棋子左下格为 `(Column, Row)`，占格尺寸为 `(WidthInCells, HeightInCells)` 时，棋子中心世界坐标为：

```text
WorldX = BottomLeftX + (Column + WidthInCells  / 2) × CellSize
WorldY = BottomLeftY + (Row    + HeightInCells / 2) × CellSize
WorldZ = BoardPlaneZ
```

注意：`WidthInCells / 2` 和 `HeightInCells / 2` 必须使用浮点数计算。

示例，`CellSize = 1`：

```text
Cell (0, 0), Size (1, 1) -> Center Offset (0.5, 0.5)
Cell (0, 0), Size (2, 1) -> Center Offset (1.0, 0.5)
Cell (1, 2), Size (2, 2) -> Center Offset (2.0, 3.0)
```

### 6.4 世界坐标转候选棋盘坐标

拖动棋子时，不直接保存任意世界坐标。应将拖动位置量化为候选逻辑坐标：

```text
CandidateColumn = RoundToInt(
    (PieceCenterX - BottomLeftX) / CellSize - WidthInCells / 2
)

CandidateRow = RoundToInt(
    (PieceCenterY - BottomLeftY) / CellSize - HeightInCells / 2
)
```

候选坐标必须再交给 Board Model 验证。验证失败时，棋子回到 Model 当前坐标计算出的世界位置。

### 6.5 推荐 API

布局类型建议提供集中 API：

```csharp
Vector3 GetPieceWorldPosition(Vector2Int cell, Vector2Int sizeInCells);
Vector2 GetPieceWorldSize(Vector2Int sizeInCells);
Vector2Int GetNearestCell(Vector3 worldPosition, Vector2Int sizeInCells);
Bounds GetBoardWorldBounds();
```

所有棋子生成、重置、移动、动画完成和场景预览都调用这些 API。

### 6.6 Transform 层级与缩放

- 棋盘根节点默认保持均匀缩放 `(1, 1, 1)`。
- 优先通过 `CellSize` 改变棋盘整体大小，而不是同时叠加多层非均匀 Transform Scale。
- 如果棋盘根节点需要缩放，坐标换算必须明确使用本地坐标或 `TransformPoint` / `InverseTransformPoint`，不能混用本地与世界坐标。
- 推荐布局器在棋盘根节点的本地空间中计算，再通过根 Transform 转换到世界空间。
- 棋子 View 应统一挂在棋盘根节点下。

## 7. 推荐类型职责

以下是推荐职责，不要求为了文件数量机械拆分；同一类型不能同时承担互相冲突的职责。

### `KlotskiPieceState`

普通 C# 数据：

- 唯一 ID；
- 棋子类型；
- 当前逻辑坐标；
- 占格尺寸；
- 是否为目标棋子。

不得保存 Transform、SpriteRenderer 或世界坐标。

### `KlotskiBoardModel`

普通 C# 逻辑：

- 棋盘大小；
- 棋子集合；
- 占用格计算；
- 合法移动判断；
- 应用移动；
- 重置；
- 胜利判断；
- 关卡数据校验。

不得依赖 MonoBehaviour、Sprite、Collider 或 DOTween。

### `KlotskiBoardLayout`

负责：

- 棋盘边界计算；
- 格坐标与世界/本地坐标转换；
- 棋子目标显示尺寸；
- 可选的 Camera 自动取景辅助。

不得负责合法移动和胜利判断。

### `KlotskiPieceView`

负责：

- SpriteRenderer；
- 输入命中；
- 选中状态；
- 移动动画；
- 根据布局器同步位置和尺寸。

不得直接修改其他棋子的逻辑状态。

### `KlotskiBoardView`

负责：

- 创建或绑定棋子 View；
- 根据 Model 完整刷新显示；
- 管理棋子 ID 到 View 的映射；
- 统一设置 sorting layer 和显示层级。

### `KlotskiGameController`

负责：

- 创建 Model；
- 连接输入、Model、View、UI 和音效；
- 管理移动中、可输入、完成等流程状态；
- 重置关卡；
- 处理胜利事件。

不要把坐标公式复制到 Controller。

## 8. 棋盘占用与合法移动

### 8.1 占用数据

Board Model 应能生成或维护整数格占用表：

```text
Occupancy[Column, Row]
```

格子可以记录空、棋子 ID 或棋子索引。

### 8.2 放置验证

判断棋子能否放置到新坐标时必须检查：

1. 棋子所有占格都在棋盘边界内；
2. 目标格没有被其他棋子占用；
3. 检查时忽略棋子自身当前占用；
4. 移动方向符合当前玩法约束；
5. 若一次跨越多个格子，需要验证中间经过的每一步。

不要使用 Physics2D 碰撞结果替代占用表验证。

### 8.3 移动提交

- 非法移动不能改变 Model。
- 合法移动只提交一次。
- 动画播放失败或被打断时，View 必须重新对齐 Model，而不是反向修改 Model。
- 移动历史如用于撤销，应保存逻辑状态或逻辑移动记录，不保存 Transform 快照。

## 9. 输入与拖动表现层

拖动由表现层负责接收指针、展示跟手效果和播放吸附动画；移动是否合法仍然完全由 Board Model 决定。

### 9.1 输入组件

推荐使用 Unity EventSystem 的 Pointer 事件同时支持鼠标和触摸：

```text
Main Camera
  Physics2DRaycaster

Pieces/<Piece>
  SpriteRenderer
  BoxCollider2D
  KlotskiPieceView
```

`KlotskiPieceView` 可以实现：

```csharp
IPointerDownHandler
IDragHandler
IPointerUpHandler
IPointerCancelHandler
```

规则：

- `BoxCollider2D` 的尺寸应匹配棋子当前 Sprite 世界范围；
- Collider 只用于命中，不用于合法移动判断；
- 同一时刻只允许一个 Pointer 控制一个棋子；
- 必须记录并检查 Pointer ID，防止多指输入串扰；
- 游戏状态不是 `Ready` 时忽略新的 Pointer Down。

### 9.2 按下表现

Pointer Down 时：

1. 确认游戏状态为 `Ready`；
2. 确认目标棋子当前没有移动 Tween；
3. 记录起始逻辑格、起始世界位置和起始 Pointer 世界位置；
4. 记录 Pointer 与棋子中心的世界偏移，防止棋子瞬间跳到手指中心；
5. 向 Board Model 查询该棋子四个方向的连续可移动格范围；
6. 将游戏状态切换为拖动预览状态；
7. 提高棋子的 Sorting Order；
8. 可轻微提高 Sprite 亮度或显示简单选中框。

默认不要放大棋子。当前棋子按 `1.68` 网格紧密排列，缩放会造成视觉重叠和边界误导。

按下阶段不能修改 Model、步数和计时。

### 9.3 拖动轴锁定

拖动开始时先处于未确定方向状态。Pointer 位移超过阈值后锁定为水平或垂直：

```text
DragThreshold = CellSize × 0.12
```

当前场景约为：

```text
1.68 × 0.12 ≈ 0.20 世界单位
```

方向选择：

```text
abs(DeltaX) > abs(DeltaY) -> Horizontal
abs(DeltaY) > abs(DeltaX) -> Vertical
```

如果 Model 明确表示某个轴完全不可移动，而另一个轴存在合法移动，可以优先锁定可移动轴；但不能因此允许斜向移动。

方向一旦锁定，本次拖动不再中途切换轴，直到 Pointer Up 或 Cancel。

### 9.4 跟手预览

拖动过程中：

- 将屏幕坐标转换到棋盘平面 `BoardPlaneZ`；
- 保留 Pointer Down 时的抓取偏移；
- 只更新已锁定轴的坐标；
- 另一轴保持起始格计算出的精确中心坐标；
- 根据 Model 返回的连续可移动范围夹取预览坐标；
- 棋子不能在视觉上穿过其他棋子或离开棋盘；
- Model 仍保持起始逻辑格，不在每帧拖动中提交状态；
- 不在拖动过程中增加步数；
- 不启动多个 DOTween 跟随 Pointer，直接更新临时预览 Transform 即可。

推荐的夹取逻辑：

```text
PreviewPositionOnAxis = Clamp(
    PointerWorldPositionOnAxis - GrabOffsetOnAxis,
    MinAllowedWorldPosition,
    MaxAllowedWorldPosition
)
```

`MinAllowedWorldPosition` 和 `MaxAllowedWorldPosition` 必须由 Model 的连续空格范围与 `KlotskiBoardLayout` 共同计算，不能由 Collider 碰撞结果决定。

被完全阻挡的棋子可以保持原位，并通过轻微颜色变化、短震动或一次无效音效提示；不要让它临时穿入相邻棋子。

### 9.5 松手与目标格选择

Pointer Up 时：

1. 根据当前预览中心调用 `GetNearestCell`；
2. 把候选格限制到本次轴锁定方向；
3. 验证从起始格到候选格之间的每一步；
4. 候选格等于起始格时视为未移动；
5. 合法移动时先提交 Model；
6. Model 提交成功后步数加 `1`；
7. View 再 Tween 到新格的精确世界中心；
8. 非法或未达到移动阈值时 Tween 回起始格；
9. 动画完成后恢复 Sorting Order、颜色和输入状态。

推荐释放判定阈值：

```text
CommitThreshold = CellSize × 0.35
```

当前场景约为：

```text
1.68 × 0.35 ≈ 0.59 世界单位
```

一次拖动可以移动多个连续空格，但中间路径必须全部有效，并且整次 Pointer Down 到 Pointer Up 只计算为一步。

### 9.6 吸附与回弹表现

合法吸附：

```text
Duration = 0.12 ~ 0.20 秒
Ease = OutCubic
```

非法回弹：

```text
Duration = 0.10 ~ 0.15 秒
Ease = OutQuad 或轻量 OutBack
```

动画要求：

- Tween 目标只能来自 `KlotskiBoardLayout`；
- 动画期间游戏状态为 `Moving`；
- 动画期间不接受新的移动提交；
- Tween 完成后再次强制设置精确目标位置；
- 不通过 Tween 中间位置反写 Model；
- Tween 被 Kill、对象禁用或场景退出时，棋子必须立即对齐 Model。

### 9.7 Pointer Cancel 与异常恢复

以下情况统一视为取消拖动：

- Pointer 离开有效输入范围；
- 应用失去焦点；
- 触摸被系统取消；
- 场景切换；
- 棋子或棋盘被禁用；
- 游戏进入 Completed；
- 重置按钮被点击。

取消时：

1. 不修改 Model；
2. 不增加步数；
3. 清理拖动状态与 Pointer ID；
4. 棋子回到 Model 当前格的精确世界位置；
5. 恢复 Sorting Order 和颜色；
6. 如果游戏未完成，恢复为 `Ready`。

## 10. 动画与 DOTween

- 可以使用项目已有 DOTween 播放棋子移动和回弹。
- Tween 的目标位置必须来自 `KlotskiBoardLayout`。
- 开始新 Tween 前处理同一棋子的旧 Tween，避免多个 Tween 同时修改 Transform。
- 对象禁用、销毁、重置或重新加载关卡时清理 Tween。
- 动画时长是表现参数，不能参与逻辑合法性判断。
- 低帧率或跳过动画不能改变最终棋盘状态。
- 动画结束后可再次强制同步一次最终位置，避免浮点误差。

## 11. Sorting 与 Z 坐标

- 使用明确的 Sorting Layer 和 Order 管理显示顺序。
- 不通过不断修改 Z 坐标解决选中棋子遮挡问题。
- 推荐顺序：

```text
Board Background < Normal Pieces < Selected/Moving Piece < Hint/Effect
```

- 棋盘中所有棋子基础 Z 坐标由布局统一确定。
- 选中表现结束后恢复原 Sorting Order。

## 12. 关卡数据校验

加载关卡时至少验证：

- 棋盘列数、行数大于零；
- `CellSize > 0`；
- `0 <= PieceGap < CellSize`；
- 棋子 ID 唯一；
- 棋子尺寸为正整数；
- 棋子没有越界；
- 棋子初始位置没有重叠；
- 存在且仅存在一个目标棋子；
- 出口和胜利目标坐标有效；
- 经典 `4×5` 布局的空格数量符合关卡设计。

无效数据应在开发阶段给出清晰错误，指出棋子 ID、坐标和失败原因。

## 13. 胜利判断

胜利条件必须由逻辑坐标判断，不读取 Transform。

经典关卡可以配置为：目标 `2×2` 棋子的左下格到达指定出口坐标。例如出口位于棋盘底部中央时，目标坐标通常可表示为：

```text
TargetCell = (1, 0)
```

具体目标坐标应来自关卡配置，不要散落硬编码。

胜利只触发一次。完成后应锁定普通输入，再通知 UI 和音频系统。

## 14. Camera 自动取景建议

如果棋盘需要适应不同屏幕比例，可以根据布局器返回的世界 Bounds 计算正交相机尺寸：

```text
RequiredHalfHeightByHeight = BoardWorldHeight / 2
RequiredHalfHeightByWidth  = BoardWorldWidth / (2 × CameraAspect)

OrthographicSize = Max(
    RequiredHalfHeightByHeight,
    RequiredHalfHeightByWidth
) + CameraPadding
```

不要通过逐个修改棋子坐标适配屏幕。屏幕适配只影响棋盘根位置、整体 CellSize 或 Camera，不影响逻辑坐标。

## 15. 必须覆盖的自动化测试

### 15.1 布局 EditMode 测试

- `1×1` 棋子位于 `(0, 0)` 时中心坐标正确；
- `2×1`、`1×2`、`2×2` 棋子的中心偏移正确；
- 右上角棋子不会超过棋盘 Bounds；
- 改变 `BoardCenter` 后所有棋子保持相对布局；
- 改变 `CellSize` 后位置和尺寸按比例变化；
- `PieceGap` 只改变视觉尺寸，不改变逻辑位置；
- `Cell -> World -> Cell` 在合法格上能够往返；
- 使用奇数或偶数棋子尺寸时都没有半格偏移错误。

### 15.2 Model EditMode 测试

- 初始关卡无重叠且不越界；
- 合法移动成功；
- 越界移动失败；
- 被占用格阻挡时失败；
- 多格棋子检查全部占格；
- 移动后旧格释放、新格占用；
- 重置后恢复初始状态；
- 目标棋子到达出口时胜利；
- 非目标棋子到达出口不触发胜利。

### 15.3 PlayMode 测试

- 创建棋盘后每个 Sprite 棋子位置与 Model 一致；
- 非法拖动后棋子回到原格；
- 合法拖动后棋子吸附到目标格；
- 动画期间不能重复提交移动；
- 重置时正在播放的 Tween 被清理；
- 胜利后输入被锁定且完成 UI 只显示一次。

测试命名必须描述条件和结果，例如：

```text
GetPieceWorldPosition_WhenPieceIsTwoByTwo_ReturnsCellAreaCenter
TryMove_WhenDestinationIsOccupied_ReturnsFalse
ReleasePiece_WhenCandidateCellIsInvalid_SnapsBackToModelPosition
```

## 16. 场景与 Prefab 规则

- 棋盘根节点建议命名为 `KlotskiBoard`。
- 棋子 View 建议由 Prefab 创建或由场景中统一模板实例化。
- 棋子 GameObject 名称应包含稳定 ID 或可读名称。
- 不把关卡规则编码在 Hierarchy 顺序中。
- 不在场景里手工摆放最终世界坐标作为唯一数据来源。
- 如果为了编辑预览手工移动了棋子，运行或刷新时必须由布局器重新对齐。
- 修改 `Assets/Scenes/Klotski.unity` 前先检查用户已有未提交修改。
- 保存场景前检查是否产生无关对象或大范围序列化差异。

## 17. 推荐开发顺序

实现华容道时默认按以下顺序：

1. 定义棋盘坐标、棋子尺寸和关卡数据。
2. 实现不依赖 Unity 的 `KlotskiBoardModel`。
3. 为占用、移动和胜利规则编写 EditMode 测试。
4. 实现 `KlotskiBoardLayout` 及坐标换算测试。
5. 实现 Sprite 棋子 View 和自动尺寸适配。
6. 实现 Board View，将 Model 状态完整同步到棋子 Sprite。
7. 实现点击或拖动输入。
8. 加入 DOTween 移动与回弹动画。
9. 加入华容道 UI 与音效通知。
10. 运行 PlayMode 测试并用 Game View 截图验证。

不要在 Model 和布局公式尚未验证时先制作复杂拖动与动画。

## 18. AI 与 MCP 操作流程

AI 修改本目录或华容道场景时必须：

1. 先读取项目根 `AGENTS.md` 和本文件。
2. 检查当前 Git 状态，保护用户对 `Klotski.unity` 的已有修改。
3. 读取相关脚本、场景对象、Prefab 和组件。
4. 确认棋盘坐标约定、CellSize、BoardCenter 和棋子尺寸。
5. 先修改 Model 或布局规则，再修改 View。
6. 不手工逐个写死棋子世界坐标。
7. 修改脚本后刷新 AssetDatabase 并等待编译。
8. 检查 Console 是否出现编译错误、Missing Script 或空引用。
9. 运行布局和规则 EditMode 测试。
10. 运行相关 PlayMode 测试。
11. 使用 Game View 或 Scene View 截图检查棋子间距、尺寸、吸附和遮挡。
12. 检查 Git 差异，确认没有无关场景序列化。

## 19. 完成标准

华容道相关任务只有在以下条件满足后才算完成：

- 所有 C# 代码位于正确的 `NanokaGame.Games.Klotski...` 命名空间；
- Model 不依赖 Transform、SpriteRenderer、Collider 或 DOTween；
- 棋子逻辑位置使用整数格坐标；
- 世界坐标全部由统一布局器计算；
- 棋子 Sprite 尺寸根据占格大小自动计算；
- 改变棋盘中心或 CellSize 后无需手工重新摆放棋子；
- 合法移动、阻挡、越界和胜利规则有测试；
- Unity 编译成功且没有新增 Console Error；
- 场景和 Prefab 中没有 Missing Script；
- 拖动后棋子始终吸附到合法格或返回原格；
- 动画不会导致 Model 与 View 状态不一致；
- 没有覆盖用户已有的场景修改；
- 最终报告列出坐标约定、公式、修改文件和验证结果。

## 20. 明确禁止

- 禁止在 Inspector 中为每个棋子手工保存最终世界坐标。
- 禁止使用 Transform 位置作为胜利判断依据。
- 禁止使用 Physics2D 碰撞作为合法移动的唯一依据。
- 禁止把坐标公式复制到多个 Controller 或 View 中。
- 禁止在拖动过程中直接永久修改 Model。
- 禁止让棋子停留在半格或任意浮点位置。
- 禁止通过屏幕分辨率直接改变逻辑棋盘坐标。
- 禁止通过 Sprite 原始像素尺寸决定棋子占格数量。
- 禁止修改 DOTween 源码实现棋子动画。
- 禁止在未检查场景脏状态和 Git 差异时覆盖 `Klotski.unity`。

## 21. 当前版本完整功能需求

本节是当前单关卡华容道的功能规格。实现时应以本节作为验收依据，不擅自扩大范围。

### 21.1 游戏主流程

```text
进入 Klotski 场景
-> 根据关卡数据初始化 4×5 Board Model
-> 以 nanoka_left_arm 左上角建立棋盘世界空间锚点
-> 将 10 个 Scene Sprite 棋子绑定到逻辑棋子
-> 根据 InitialCell 自动对齐全部棋子
-> 步数显示 0，时间显示 00:00
-> 游戏进入 Ready
-> 玩家拖动棋子
-> 计算轴向与候选逻辑格
-> 验证边界、占用和中间路径
-> 合法：提交 Model、步数加一、View 吸附
-> 非法：Model 不变、View 回弹
-> 每次合法移动后检查胜利
-> 曹操到达出口时进入 Completed
-> 停止计时和输入
-> 播放完成表现和音效
-> 显示完成 UI
-> 玩家选择重新开始或返回标题
```

### 21.2 游戏状态

使用简单枚举，不引入复杂状态机框架：

```csharp
namespace NanokaGame.Games.Klotski
{
    public enum KlotskiGameState
    {
        Initializing,
        Ready,
        Dragging,
        Moving,
        Completed
    }
}
```

状态规则：

- `Initializing`：建立 Model、布局和 View，禁止输入；
- `Ready`：允许新的 Pointer Down；
- `Dragging`：一个棋子处于跟手预览，其他棋子不能被选中；
- `Moving`：正在吸附或回弹，禁止新的移动提交；
- `Completed`：胜利后锁定普通棋盘输入。

任何异常流程都必须能够回到与 Model 一致的稳定状态。

### 21.3 初始关卡

- 第一版只实现当前一个固定关卡；
- 运行时使用逻辑格数据初始化，不把 Scene Transform 当作长期真相；
- 当前 Scene Transform 是关卡设计基准与视觉锚点；
- 初始化后应检查 10 个棋子无重叠、无越界；
- 初始空格必须为 `(1,0)` 和 `(2,0)`；
- 目标棋子必须为 `nanoka_head`；
- 当前 Sprite、Scale、Rotation 和角色映射保持不变；
- `Root_Image` 当前禁用，第一版不启用第二套 UI Image 棋盘。

### 21.4 移动规则

- 棋子只能沿水平或垂直方向移动；
- 棋子不能旋转；
- 棋子不能斜向移动；
- 棋子不能越出 `4×5` 棋盘；
- 棋子不能与其他棋子重叠；
- 棋子不能穿过其他棋子；
- 一次拖动可以跨越多个连续空格；
- 跨越多个格时必须逐格验证中间路径；
- 一次 Pointer Down 到 Pointer Up 的合法移动统一计为一步；
- 非法拖动、点击未移动和回弹不计步；
- 松手后棋子只能吸附到合法格或返回原格。

### 21.5 步数

现有 UI：

```text
Klotski_Game/Root_Step/TMP_StepCount
```

需求：

- 初始显示 `0`；
- 每次成功提交一次合法移动后加 `1`；
- 一次拖动跨越多个连续格仍加 `1`；
- 非法拖动不增加；
- 未超过提交阈值不增加；
- 回弹不增加；
- 重置后恢复为 `0`；
- 胜利后不再增加；
- UI 显示由 Controller 或 HUD View 统一刷新，棋子 View 不直接修改 TMP 文本。

### 21.6 计时

现有 UI：

```text
Klotski_Game/Root_Time/TMP_TimeCount
```

需求：

- 初始显示 `00:00`；
- 第一次合法移动提交后开始计时；
- 阅读规则、观察棋盘和非法拖动不启动计时；
- 游戏完成后停止计时；
- 重置后时间归零并等待下一次合法移动；
- 退出场景时停止更新；
- 默认显示 `mm:ss`；
- 超过 59 分钟时允许显示 `hh:mm:ss`；
- 不在多个 MonoBehaviour 中同时维护时间；
- 计时使用未受不必要 TimeScale 影响的明确时间来源。

### 21.7 胜利条件

目标棋子为：

```text
nanoka_head
```

其逻辑尺寸为 `(2,2)`。当其左下逻辑格到达：

```text
TargetCell = (1, 0)
```

即判定胜利。

基于当前棋盘锚点，目标棋子的中心世界坐标为：

```text
TargetWorldPosition = (0.06, -2.40, 0)
```

世界坐标仅用于验证 View，实际胜利判断必须使用 Model 的逻辑格。

胜利后：

- 状态切换为 `Completed`；
- 胜利事件只触发一次；
- 停止计时；
- 保留最终步数和时间；
- 取消正在进行的无效拖动；
- 禁止新的棋子操作；
- 播放完成音效；
- 播放完成表现；
- 显示完成 UI。

### 21.8 重置

当前场景没有明确的重置按钮。第一版应增加或指定一个：

```text
Btn_Restart
```

点击后：

1. 取消当前 Pointer 与拖动状态；
2. Kill 所有棋子移动 Tween；
3. Model 恢复 InitialCell；
4. View 通过 Layout 重新计算并对齐；
5. 步数归零；
6. 时间归零并停止；
7. 隐藏完成 UI；
8. 恢复 Sorting Order 和颜色；
9. 游戏回到 `Ready`。

小型项目默认直接重置，不增加二次确认弹窗。

### 21.9 退出

现有按钮：

```text
Klotski_BG/Btn_Exit
```

点击后：

- 取消当前拖动；
- Kill 所有棋子 Tween；
- 停止计时；
- 清理当前游戏状态；
- 返回 `Title` 场景；
- 第一版不保存未完成棋盘状态；
- 如果以后保存最佳成绩，只保存已完成的最好步数或最快时间。

### 21.10 规则文本

现有文本：

```text
Klotski_BG/TMP_Rule
```

建议内容保持简短：

```text
拖动棋子移动。
棋子不能重叠或离开棋盘。
将曹操移动到底部中央出口即可完成。
```

不在规则文本中加入复杂教程系统。

### 21.11 完成 UI

场景中存在禁用对象：

```text
Klotski_BG/Root_Kill
```

`Root_Kill` 已确认没有作用，第一版及后续华容道功能都不得把它用作完成面板、遮罩、按钮容器或流程状态对象。当前阶段不删除、不重命名该对象，以免覆盖用户已有场景修改；实现完成 UI 时应单独创建语义明确的：

```text
Klotski_BG/Root_Complete
```

所有完成数据与重新开始、返回标题按钮都绑定到 `Root_Complete`，不得依赖 `Root_Kill`。

完成面板至少显示：

```text
完成
用时
步数
重新开始按钮
返回标题按钮
```

面板显示期间普通棋盘输入保持锁定。

### 21.12 音频

第一版音效资源映射固定为：

| 触发场景 | 音效资源 |
| --- | --- |
| 所有普通按钮点击 | `Sfx_System_Choice_001` |
| 取消、返回类按钮点击 | `Sfx_System_Cancel_001` |
| 合法移动正式提交或吸附完成 | `Sfx_System_Move_001` |
| 非法移动、无效松手或回弹 | `Sfx_System_Alert_001` |
| 胜利 | `Sfx_System_StartGame_001` |

`Btn_Exit`、完成面板中的返回标题按钮以及其他语义为取消/返回的按钮使用 `Sfx_System_Cancel_001`；重新开始、确认等普通按钮使用 `Sfx_System_Choice_001`。同一次点击只播放一种按钮音效，不得同时播放普通按钮和取消按钮音效。

要求：

- 所有音效归入 SFX 通道；
- 音量遵循 `NanokaGame.Audio` 的全局设置；
- 棋子 View 只发出移动结果通知，不创建各自的全局 Audio Manager；
- 连续拖动预览不应每帧播放声音；
- 移动音效在移动正式提交或吸附完成时播放一次；
- 非法移动音效在确认本次移动不能提交时播放一次，不随回弹动画逐帧播放；
- 胜利音效只在首次进入完成状态时播放一次；
- 重置和退出时正确停止需要停止的临时音效。

### 21.13 第一版不实现

除非用户后续明确要求，第一版不实现：

- 撤销；
- 提示；
- 自动求解；
- 多关卡选择；
- 随机关卡；
- 棋子旋转；
- 多指同时拖动；
- 联网排行榜；
- 游戏中途存档；
- 回放；
- 成就；
- 广告；
- 复杂粒子效果；
- 自定义棋盘大小 UI。

## 22. 当前版本验收清单

### 22.1 布局

- [x] `nanoka_left_arm` 的 Sprite 左上角与棋盘左上角完全一致；
- [x] 棋盘由该锚点计算为 `4×5`、`CellSize=1.68`；
- [x] Board Center 计算结果为 `(0.06,0.12)`；
- [x] 10 个棋子的逻辑格与当前场景映射一致；
- [x] 初始空格为 `(1,0)` 和 `(2,0)`；
- [x] 修改锚点或整体棋盘位置后无需逐个改棋子 Transform；
- [x] 整体 Sprite Bounds 为 `6.72×8.40`。

### 22.2 输入与拖动

- [x] 鼠标可以拖动棋子；
- [x] 单指触摸可以拖动棋子；
- [x] Pointer Down 不会导致棋子跳到指针中心；
- [x] 超过阈值后正确锁定水平或垂直轴；
- [x] 锁轴后不会中途切换为另一个轴；
- [x] 跟手预览不会穿过其他棋子；
- [x] 跟手预览不会超出棋盘；
- [x] 拖动时不提前修改 Model；
- [x] 松手合法时吸附到格中心；
- [x] 松手非法时回到原格；
- [x] Pointer Cancel 后恢复 Model 对应位置；
- [x] 多指不会同时控制多个棋子；
- [x] 动画期间不能重复提交移动。

### 22.3 规则

- [x] 只能水平或垂直移动；
- [x] 不能旋转；
- [x] 不能斜向移动；
- [x] 不能越界；
- [x] 不能重叠；
- [x] 不能穿过其他棋子；
- [x] 多格移动会逐格检查路径；
- [x] Model 与 View 在低帧率和 Tween 中断后仍一致。

### 22.4 UI 与流程

- [x] 初始步数为 0；
- [x] 只有合法移动增加步数；
- [x] 第一次合法移动开始计时；
- [x] 重置恢复初始位置、步数和时间；
- [x] `Btn_Exit` 返回 Title；
- [x] 曹操到达 `(1,0)` 时胜利；
- [x] 胜利事件只触发一次；
- [x] 胜利后停止时间和输入；
- [x] 完成 UI 显示最终步数与时间；
- [ ] 移动、无效和胜利音效正确响应全局 SFX 音量。

### 22.5 工程质量

- [x] 所有代码位于 `NanokaGame.Games.Klotski...` 命名空间；
- [x] Model 不依赖 Unity 表现组件；
- [x] 坐标公式只存在于 `KlotskiBoardLayout`；
- [x] 所有棋子没有 Missing Script；
- [x] Unity 编译无新增 Error；
- [x] EditMode 布局和移动测试通过；
- [x] PlayMode 拖动和胜利流程测试通过；
- [x] Scene 与 Prefab 没有无关序列化变化。

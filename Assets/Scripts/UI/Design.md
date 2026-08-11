# 游戏 UI 与音频设置设计

## 1. 当前范围

Title 第一版只负责：

- 进入华容道；
- 保留第二个游戏入口作为后续功能；
- 打开和关闭游戏设置；
- 调节背景音乐和音效音量；
- 播放通用按钮音效。

不在 Title 中加入账号、联网、商城、复杂存档或其他全局系统。

## 2. Title 场景

主要对象：

```text
Title
└── Btns
    ├── Btn_Jigsaw
    ├── Btn_Klotski
    └── Btn_Settings

GameSettings
├── Btn_Close
└── Btns
    ├── Btn_BGM
    │   └── Slider
    └── Btn_SFX
        └── Slider
```

初始状态：

- `Title` 显示；
- `GameSettings` 隐藏；
- BGM 和 SFX Slider 默认值为 `1`；
- `Btn_Jigsaw` 当前仅保留入口和按钮音效，尚未配置目标场景。

## 3. 设置面板开关

`Title` 根对象上的 `SettingsPanelController` 负责设置面板显隐：

- `Btn_Settings` 调用打开逻辑；
- `Btn_Close` 调用关闭逻辑；
- 组件启用时注册监听，禁用时移除监听；
- `GameSettings` 本身保持为独立 Canvas；
- 面板关闭后使用 `SetActive(false)`，防止隐藏 UI 继续接收输入。

设置面板控制器只维护界面状态，不处理 AudioMixer 数值。

## 4. 通用按钮音效

Title 场景根节点包含一个 `ButtonSfxPlayer`，输出到 `NanokaGameMixer/Master/SFX`。

| 按钮 | 类型 | 音效 |
| --- | --- | --- |
| `Btn_Jigsaw` | 普通 | `Sfx_System_Choice_001` |
| `Btn_Klotski` | 普通 | `Sfx_System_Choice_001` |
| `Btn_Settings` | 普通 | `Sfx_System_Choice_001` |
| `Btn_BGM` | 普通 | `Sfx_System_Choice_001` |
| `Btn_SFX` | 普通 | `Sfx_System_Choice_001` |
| `Btn_Close` | 取消 | `Sfx_System_Cancel_001` |

每次点击创建一个自动销毁的临时 2D AudioSource。临时对象进入 `DontDestroyOnLoad`，因此点击 `Btn_Klotski` 后，普通按钮音效不会因为 Title 场景卸载而被截断。

## 5. AudioMixer

资源：

```text
Assets/Audios/Mixers/NanokaGameMixer.mixer
```

Group：

```text
Master
├── Music
└── SFX
```

暴露参数：

```text
MasterVolume
MusicVolume
SfxVolume
```

当前设置面板只显示 Music 和 SFX。`MasterVolume` 保留为 Mixer 总控参数，第一版不增加 Master Slider。

## 6. 音量 Slider

`Title` 根对象上的 `AudioVolumeSettings` 负责：

- 读取 BGM Slider；
- 读取 SFX Slider；
- 将线性值转换为分贝；
- 写入 AudioMixer；
- 保存 PlayerPrefs；
- 下次启动时恢复设置；
- 组件禁用时移除 Slider 监听。

Slider 范围：

```text
Min = 0
Max = 1
Default = 1
```

分贝转换：

```text
linear <= 0.0001 → -80 dB
其他值           → log10(linear) × 20
```

禁止直接计算 `log10(0)`。

PlayerPrefs Key：

```text
NanokaGame.Audio.MusicVolume
NanokaGame.Audio.SfxVolume
```

初始化使用 `Slider.SetValueWithoutNotify`，避免启动时触发无意义的保存或 Slider 事件。Mixer Snapshot 可能在 `Awake` 后恢复默认值，因此组件会在 `Start` 再应用一次已保存值，保证持久化设置最终生效。

## 7. 场景背景音乐

| 场景 | 背景音乐 | Mixer Group |
| --- | --- | --- |
| Title | `根本真澄 - gDie Divil JIO` | Music |
| Klotski | `Bgm_007_001_Loop` | Music |

`MusicPlayer` 跨场景保留。新场景中的重复 MusicPlayer 只负责请求切换 Clip，然后销毁自己，运行时应始终只保留一个 MusicPlayer。

## 8. 当前验收状态

- [x] 设置按钮打开 GameSettings；
- [x] 关闭按钮隐藏 GameSettings；
- [x] 普通按钮播放 Choice；
- [x] 关闭按钮播放 Cancel；
- [x] 点击华容道时按钮音效跨场景播放；
- [x] BGM Slider 调节 `MusicVolume`；
- [x] SFX Slider 调节 `SfxVolume`；
- [x] Slider 设置保存到 PlayerPrefs；
- [x] 重新进入 Play Mode 后恢复 Slider 和 Mixer 分贝；
- [x] Title 和 Klotski 背景音乐按场景切换；
- [x] Music 和 SFX 使用不同 Mixer Group；
- [ ] `Btn_Jigsaw` 配置实际游戏场景；
- [ ] 字体显示问题后续单独处理。

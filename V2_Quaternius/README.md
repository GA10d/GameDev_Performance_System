# ASTRA · THE RELAY / 演出系统 V2.1

Git 克隆版请先运行 `Rebuild.ps1` 生成播放器。下述完整画面复查依赖本地 `QA/visual-review` 数据，该批量产物不纳入 Git；关键截图与验收文档仍保留，详见 [Git 文件管理](../Docs/06_Git文件管理.md)。

基于 Quaternius CC0 人物与真实骨骼动画制作的独立新版。双击 **[Launch.cmd](Launch.cmd)** 开始播放；完整播放器位于 [Build/ASTRA Relay V2.exe](Build/ASTRA%20Relay%20V2.exe)，请保留整个 Build 目录。

**V2.1 画面修订**：修复手指骨骼变形、坐姿头部错误剔除、切镜站位重置、镜头随头骨晃动及试演遮挡。逐镜问题记录见 [Docs/04_逐镜复查.md](Docs/04_逐镜复查.md)，原始帧和对比见 [画面复查.html](画面复查.html)。

## 本次变化

- **4 名人物、3 类种族**：林和沃斯为人类；回声为外星人；鲁克为半兽人。角色来自 Quaternius 模块化人物，在 Blender 中改造头部、体型、服装和装备，保留真实蒙皮。
- **真实素材动作**：导入 Universal Animation Library Standard 的 43 个条目（含 T Pose），试验台开放 17 种动作。故事使用说话、倾听、维修、设备操作、坐下、坐姿交流、起身和行走，并叠加有限幅度的头部反应。
- **9 种镜头、16 个演出节拍**：全景、中景、近景、双人、过肩、侧面、低机位、设备插入和跟拍，支持硬切、平滑过渡和缓慢推进。
- **可操作试验台**：随时选择人物、动作、镜头和播放速度；暂停、逐镜跳转、清晰／复古画面比较、减弱动态和截图。

这段约 90 秒的旧录像发生在中继站。玩家仍是独处探测舱的观看者；多人交流属于历史影像。外星人、半兽人及中继站故事是本次用户要求的演出探索设定，尚不代表修改了 Astra 正式世界观。

## 操作

| 操作 | 按键 |
|---|---|
| 暂停 / 继续 | Space、P 或 Esc |
| 故事 / 试验台 | Tab |
| 选择四位人物 | 1–4，会进入试验台 |
| 上一个 / 下一个动作 | Q / E，仅试验台 |
| 下一个镜头 | C，仅试验台 |
| 上一段 / 下一段故事 | ← / → |
| 重播故事 | R |
| 清晰 / 复古画面 | V |
| 隐藏 / 显示界面 | H |
| 保存截图 | F12，写入游戏 persistentDataPath/Screenshots |

## 修改入口

- [UnityProject](UnityProject)：Unity **2022.3.62f1 / Built-in**，场景 `Assets/PerformanceV2/Scenes/Relay.unity`。
- `Assets/PerformanceV2/Data/RelayArchive.asset`：直接编辑节拍、台词、动作、镜头、时长；除一次 V2.1 迁移外，重建场景会保留已有数据资产。
- [Source](Source)：四个可编辑的 `Astra_*.blend`、人物清单及下载原件。
- [Tools/build_cast.py](Tools/build_cast.py)：完整可重复的模块组合、异种头部修改、绑定和导出脚本。
- [Docs/01_新版演出设计.md](Docs/01_新版演出设计.md)：人物、动作、镜头与世界观边界。
- [Docs/02_实现与扩展.md](Docs/02_实现与扩展.md)：架构和素材管线。
- [Docs/03_验收与限制.md](Docs/03_验收与限制.md)：运行验证、截图和当前边界。
- [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)：Quaternius 原件出处、许可证和修改记录。

```powershell
.\Rebuild.ps1
.\Rebuild.ps1 -RegenerateCast
.\Verify.ps1
```

试验台的单人镜头将角色移至无遮挡站位；双人与过肩只保留当前配对，全景保留四人。返回故事会重建剧情站位。

上一版保留在父目录。Project-Astra 正式项目未因本次演出样板而修改。

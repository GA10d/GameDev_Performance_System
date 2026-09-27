# ASTRA Performance Tool V2

基于 Demo/V2 的 Quaternius CC0 角色和真实 UAL Humanoid 动作，提供模块化捏人、五轨演出时间轴和分支演出树。独立目录、独立命名空间 `Astra.PerformanceToolV2`，不覆盖 Tool/V1。

## 快速使用

1. 双击 `RunToolV2.cmd` 或 `Build/ASTRA Performance Tool V2.exe`：无需 Unity，先选择“新建人类 / 新建外星人”，按左侧类别编辑，右侧试演动作。外观可存为 JSON，再载入最近保存的外观。
2. 双击 `OpenUnityV2.cmd`，或在 Unity Hub 中打开 `UnityProject`，使用 Unity **2022.3.62f1**。启动脚本使用本机已安装的 Unity 路径。
3. 菜单 `Astra Performance Tool V2 → Open timeline` 打开策划工具。
4. 素材库“＋ 新建角色 → 人类 / 外星人”，或者选择捏人预设，再点“打开捏人编辑器”。修改草稿后点“保存角色与 Prefab”。
5. 保存的角色出现在人物素材库的“自建角色”下，可拖入人物轨，再添加动作、台词、镜头与场景。
6. `Open graph` 编辑演出树，连接选项及沉默出口。时间轴中点“验证”，再“运行预览”。

独立程序的 JSON 用于外观演示；Unity 工程内以 Character ScriptableObject 与 Prefab 为正式交付资产。独立程序不代替 Unity 演出编辑器。

## 本版内容

- 4 个 Demo/V2 原角色：林、回声、砾、沃斯，保持原模型。
- 42 个可编辑预设：16 男性、10 女性、16 外星人。
- 人类 10 脸型；外星人 10 个独立解剖头型，包含独眼、四目、裂颚、鸟喙、菌伞、晶面、花萼等。新增独立的原型／圆下巴／方下巴，保留眼睛、眉骨、嗅觉器官、口器及宽度微调；无独立眉骨的原型会显示说明。
- 男性 33 个发型 / 头饰选项（包含“无发”及 3 个帽盔）；外星人 12 个颅冠选项（包含“无冠”）。人类头部分为“短发 / 分缝刘海 / 长发束发 / 帽饰”；外星人分为“鳍膜叶瓣 / 角环骨板 / 触须感官”。
- 女性新增 14 个头部选项（含无发、10 个原包发型／头饰组合和 3 个适配帽盔），按短发束发 / 长发 / 帽饰分组；10 套原包服装，按日常正装 / 科幻勤务 / 旅行长装分组。
- 8 体型，整体身高 0.90–1.10 倍；22 上装、22 下装、22 鞋靴可独立组合，按原有服装 / 舰内勤务 / 舱外探索 / 工业安保分组，支持整套应用与推荐配色。
- 原人类 / 灰裔保留 10 面饰、10 装备、6 面部纹样（含“无”）。九种新外星人头型暂支持无面饰 / 护颈，使用自身鳃纹、甲板或花瓣；菜单隐藏未适配面饰。
- 人类 / 外星人各 16 肤色，16 发色、16 服装色、12 眼色、12 强调色。Unity 编辑器另支持自定义颜色。
- 默认呈现 17 个演出动作；资源库包含免费 Standard 包的 42 个实际动画，另有 T Pose 仅用于导入，不列为动作。
- 9 个预设镜头及自定义镜头；3 个场景；保留五轨与条件分支结构。

半兽人使用 Demo/V2 原成品角色，本版“新建角色”只开放用户要求的人类与外星人。脸部是低模风格的有限形变，不是写实扫描头或任意拓扑编辑器。

## 目录

| 目录 | 内容 |
| --- | --- |
| `UnityProject/Assets/AstraToolV2` | 独立工具源码、模型、动作、角色、示例与场景 |
| `Source/Downloads` | Quaternius 原始 Blend、Universal Base Characters 免费包与 CC0 许可证 |
| `Source/Quaternius_Astra_Modular.blend` | 可继续编辑的模块化模型，共享骨架 |
| `Source/modular_manifest.json` | 每个模块的顶点、三角面和形态键清单 |
| `Tools/build_modular.py` | 从 CC0 原始文件重建 FBX / Blend |
| `Build` | Windows 可运行工作台 |
| `Export` | 可导入其他 Unity 工程的 unitypackage |
| `QA` | 编译、结构验证、运行日志与截图 |
| `Docs` | 策划使用与技术说明 |

## 可复现构建

Git 克隆不含 `Build` 和 `Export`。在本目录运行 `.\Rebuild.ps1` 即可验证、构建；需要重建 Blender 模型时用 `.\Rebuild.ps1 -Models`。工具路径不同可传 `-EditorPath` / `-BlenderPath`。

完整命令参考：

```powershell
& 'F:\Blender\blender.exe' -b --python '.\Tools\build_modular.py'
& 'F:\Unity\Installs\2022.3.62f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath "$PWD\UnityProject" -executeMethod ToolV2Build.Initialize -logFile "$PWD\QA\initialize.log"
& 'F:\Unity\Installs\2022.3.62f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath "$PWD\UnityProject" -executeMethod ToolV2Build.BuildPlayer -logFile "$PWD\QA\build.log"
```

`Initialize` 是初次导入 / 重建示例用，会重设示例演出树和示例单元。日常修改角色无需执行。它保留已保存的自建角色。构建验证、生成 Prefab、导出包后才生成 Player。

出处与边界见 `THIRD_PARTY_NOTICES.md`。检查范围和已知限制见 `Docs/验收记录.md`。

## 发型更新（2026-09-25）

首轮新增 18 款：圆寸、平头、短碎发、前刺短发、后梳短发、左侧分、右侧分、中分短发、齐刘海、斜刘海、短波波头、齐颈直发、低马尾、高马尾、盘发、双髻、短卷发、宽莫西干。首轮选项增加到 30，模型模块总数为 109；最新重设计后为 33 个选项、121 个模块。

短寸改为从原头皮表面派生的发帽，修复旧版向下压缩头发造成的头顶露皮。发型仍使用相同材质、Head 骨骼和脸型形态键。原有 0–11 编号不变，旧 JSON / 角色资产无需迁移；首轮新发型使用 12–29。首轮截图及验证见 `QA/HairSheets` 和 `Docs/发型更新验收.md`。


## 外星人与原包长发重设计（2026-09-25）

将外星人 1–9 号轻微脸型变形替换为九个不同解剖结构的头部，保留 0 号灰裔。重做 11 款实体颅冠，每个头型有对应适配。人类 22、23 长发与 27 双髻改用 Quaternius Universal Base Characters 的真实发束；追加原包分缝、圆寸、贴头短发（30–32）。免费原包的五款头发形成六个适配选项，没有使用付费资源。

已修正大型颅冠在背面镜头中被墙面遮挡的问题，捏人预览使用可环绕布景与考虑深度的自动取景；原演出场景保持原布置。共享 Humanoid / UAL / Playables 动作管线不变。

完整设计、官方调研来源、资源映射和兼容边界见 [外星人重设计与原包发型](Docs/外星人重设计与原包发型.md)。新截图位于 `QA/AlienSheets`；原始实拍图位于 `QA/AlienFinal`。

后续增加衣服、发型、颅冠、配件和动作，请先阅读 [资产扩充与目录指南](Docs/资产扩充与目录指南.md)，其中包含目录位置、编号注册、两个完整示例和构建检查步骤。

## 太空服装扩充（2026-09-25）

新增 12 套服装 / 36 个蒙皮模块：驾驶、舱外加压、测绘、采矿、拆解、隔离、低温、热区、安保、医疗、舰桥、行商。总模型 157 模块、51 骨骼。见[设计与实现](Docs/太空服装扩充与调研.md)、[验收](Docs/太空服装验收.md)、[人类总览](QA/OutfitSheets/Human_000.jpg)、[外星人总览](QA/OutfitSheets/StandingAlien_000.jpg)。

## 面部贴图与时间轴修复（2026-09-27）

脸颊纹样改为真正的皮肤贴图，重做贴耳通讯器与面罩，修正莫西干头皮重叠。帽饰增加“无”，摘帽恢复原发型；时间轴增加黑色片段边框、常驻时长编辑和长演出全览／定位。该次修复后为 147 模块、51 骨骼。见 [修复说明与验收](Docs/面部贴图与时间轴修复.md)。


## 女性角色与灰裔修复（2026-09-27）

“新建角色 → 人类 → 女性”接入 Ultimate Modular Women。独立程序点击“＋新建人类 ▾ → 女性”；身份中也能切换人类底模。女性原包的服装、头发、脸型形变、肤色、配饰、外观 JSON、角色资产与 Prefab 均走现有共享 Humanoid / UAL 管线。当前共 200 模块、51 骨骼。

灰裔皮肤在 Blender 内融合为一张连续网格，嘴部使用与皮肤共享边的几何口缝；女性底模补全原包省略的后脑。详细范围、使用方式与实机截图见 [女性角色与灰裔修复](Docs/女性角色与灰裔修复.md)。

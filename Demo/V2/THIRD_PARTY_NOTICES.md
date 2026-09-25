# V2 资产出处与许可

本次确实下载并使用了 Quaternius 的人物网格与骨骼动画；不是仅参考风格。所有使用的 Quaternius 文件来自作者公开的免费包，没有购买或使用 Pro / Source 付费动作包。

## Ultimate Modular Men / Males

- 作者：Quaternius。
- 官方页面：https://quaternius.com/packs/ultimatemodularcharacters.html
- 作者链接的公共目录：https://drive.google.com/drive/folders/1USAAquX2JJWuA2m6zol0KUkFe3UkZ8zX
- 实际下载：Humanoid Rig → All Together → `Humans_Master.blend`，本地保存为 `Source/Downloads/Humans_Master_Humanoid.blend`。
- 文件 ID：`1lDSKTJX1CR15qo9JAFHFjRFGR7rp8K8D`。
- SHA-256：`1c4bd98437d0611be72498e8caf402adbd6f11002b244b35e8d9cc22e6a541bd`。
- 随附许可证：`Source/Downloads/Modular_License.txt`，CC0 1.0 Universal。
- 使用模块与每角色修改详见 `Source/cast_manifest.json` 与 `Tools/build_cast.py`。衍生的外星人及半兽人造型是本任务改造，不能说它们是 Quaternius 原包提供的成品角色。

## Universal Animation Library Standard

- 官方页面：https://quaternius.com/packs/universalanimationlibrary.html
- 作者下载页：https://quaternius.itch.io/universal-animation-library
- 实际免费文件：`Universal Animation Library[Standard].zip`；本地名 `Source/Downloads/UniversalAnimationLibrary_Standard.zip`。
- SHA-256：`cc73fc4e495b82958207316596317a3f40b9fa38065bde1027937452da537724`。
- Unity 使用：`Unity/UAL1_Standard.fbx`（原地动画），复制至 `UnityProject/Assets/PerformanceV2/Animations`。
- 随附 CC0 License.txt 与 README.txt 保存在解压目录。
- 本次检验实际为 43 个条目，包含 1 个 T Pose；官方完整系列的 120+ 数量不能用来描述免费包中实际交付的动作数量。
- 没有改写源动画曲线；Unity 中执行 Avatar 重定向、循环设置、根动作导入设置和运行时混合。头部微反应与走位由本任务代码添加。

CC0 原文：https://creativecommons.org/publicdomain/zero/1.0/

## 用户项目及本次内容

- 旧漆贴图 `NavalPaint_Albedo.png`、WornMetal.shader、Character.shader 与 Grade.shader 来自上一版演出项目及用户已有 Project-Astra 美术代码，按本任务要求复用。原项目的专有内容不因与 CC0 模型放在同一文件夹而变成 CC0。
- 站点环境几何、场景组装、衍生角色修改脚本、台词、UI 与演出控制代码为本任务新增。
- Windows 字体通过本机 Microsoft YaHei / Consolas 动态加载，没有打包分发系统字体。
- Unity Player 及 Mono 等构建依赖由已安装的 Unity 编辑器正常生成，许可属于相应提供者。
- 不包含 Mouthwashing 或 Arctic Eggs 的人物、动作、声音、贴图和台词。

## 技术参考

- [Unity Humanoid 动画重定向](https://docs.unity3d.com/2022.3/Documentation/Manual/Retargeting.html)
- [PlayableGraph.SetTimeUpdateMode](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Playables.PlayableGraph.SetTimeUpdateMode.html)
- Quaternius 免费包随附的 Unity_Setup.png：Humanoid、Bake Axis Conversion、root 节点与循环导入设置。

下载与验证日期：2026-09-23。

# ASTRA · 人物站桩演出系统

Git 克隆版不包含播放器、缓存与批量复查产物；请先运行对应版本的 `Rebuild.ps1` 再启动。保留范围与复查数据说明见 [Git 文件管理](Docs/06_Git文件管理.md)。

围绕 Mouthwashing、Arctic Eggs 的人物表现方式，结合 Project Astra 的工业囚舱、灰绿旧漆与延迟通信世界观，交付一套调研、素材方案、技术设计及可运行 Unity 样板。

## 先看结果

- **试玩**：双击 [Launch.cmd](Launch.cmd)，或打开 [ASTRA Performance.exe](Build/ASTRA%20Performance.exe)。请保留整个 Build 文件夹。
- **阅读**：[离线调研手册](调研手册.html)，不依赖网络。
- **修改**：用 Unity 2022.3.62f1 打开 [UnityProject](UnityProject)，场景为 `Assets/Performance/Scenes/Performance.unity`。
- **建模源**：[Astra_Performers.blend](Source/Astra_Performers.blend)。

![延迟通信演示](QA/runtime/01_lin_signal.png)

## 文档

| 文件 | 重点 |
|---|---|
| [00 方案总览](Docs/00_方案总览.md) | 路线选择、交付边界、制作规模 |
| [01 调研策划](Docs/01_调研策划.md) | 两款游戏的可借鉴语法、站姿/视线/手势/留白/镜头、完整 demo 脚本 |
| [02 素材与美术管线](Docs/02_素材与美术管线.md) | 开放素材库与授权、批量角色生产、Shader 能力边界、入库验收 |
| [03 技术架构](Docs/03_技术架构.md) | 数据、状态机、表演适配器、输入与渲染分层、回接现有项目 |
| [04 Demo 与验收](Docs/04_Demo与验收.md) | 操作、编辑内容、CLI 重建、验证证据与限制 |
| [05 来源与证据](Docs/05_来源与证据.md) | 官方资料、开发者访谈、本地依据、事实与推断界线 |

## 演示内容

三位原创低模人物，12 个可编辑演出节拍，6 种手势语义、4 种姿态、4 个视线方向、眨眼/文字节奏口型、两条回信分支、自动/手动播放、暂停、中断与重播、画风比较，以及 F1 检查面板。

Space 继续，A 自动，1–3 通道，P/Esc 暂停，R 重播，V 画风，M 静音，F1 检查，F12 截图。角色是在各自探针中录制的影像，玩家依然独自留在舱室内。回信只写入本次运行的本地剧情状态。

## 构建与工具

```powershell
.\Rebuild.ps1
.\Verify.ps1
# 可选：重建原创人物
.\Rebuild.ps1 -RegenerateCharacters
```

内含 [本地素材预处理脚本](Tools/batch_prepare.py)、清单范例、绑定角色保护、FBX/Blend 导出与审计报告。没有自动下载/购买第三方人物包。

本次是**可扩展的演出系统样板**。人物尚属原型美术，采用刚性蒙皮与程序化手势；没有真人配音、精确音素同步、接触 IK、正式存档或完整本地化系统。文档中的生产扩展均与已实现内容分开说明。

源 `Project-Astra` 未被修改。复用与第三方素材记录见 [THIRD_PARTY_NOTICES](THIRD_PARTY_NOTICES.md)。

最终验证：Unity Windows 构建成功；28 项核心检查、23 项运行时检查通过，另有 10 张实机截图。结果、设备及已知边界见 [验收记录](Docs/04_Demo与验收.md)。

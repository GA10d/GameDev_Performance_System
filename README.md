# ASTRA · 人物演出 Demo

三个可独立运行和编辑的版本都在 [Demo](Demo) 目录下。每个版本都有自己的 Unity 工程、构建脚本、播放器、源模型和验收资料。Git 克隆版不包含播放器，需要先在对应版本目录运行 `Rebuild.ps1`。

| 版本 | 内容 | 启动 | 文档 |
|---|---|---|---|
| V1 | 三名人类角色；原始站桩演出和延迟通信界面 | [Launch.cmd](Demo/V1/Launch.cmd) | [方案与调研](Demo/V1/README.md) |
| V2 | Quaternius 人物与骨骼动画；四人、十七种动作、九种镜头 | [Launch.cmd](Demo/V2/Launch.cmd) | [V2.1 说明](Demo/V2/README.md) |
| V3 | 基于 V1 扩展六人；新增站立外星人、半兽人、四肢爬行外星人 | [Launch.cmd](Demo/V3/Launch.cmd) | [V3 说明](Demo/V3/README.md) |

V1 的 [离线调研手册](Demo/V1/调研手册.html)、V2 的 [逐镜复查](Demo/V2/画面复查.html) 和 V3 的 [异种扩展说明与截图](Demo/V3/异种扩展说明.html) 均保留在各自目录。版本间没有运行时依赖；仓库外的 `Project-Astra` 项目未改动。

从仓库根目录重建示例：

```powershell
cd .\Demo\V3
.\Rebuild.ps1
.\Launch.cmd
```

播放器和 Unity 缓存仅保存在本地。Git 文件范围见 [文件管理](Demo/V1/Docs/06_Git文件管理.md)。

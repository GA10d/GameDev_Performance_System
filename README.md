# ASTRA · 人物演出 Demo 与工具

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


## 策划工具

| 目录 | 内容 | 入口 |
|---|---|---|
| `Tool/V1` | V3 风格角色的第一版演出 / 捏人工具 | [说明](Tool/V1/README.md)、[启动](Tool/V1/Launch.cmd) |
| `Tool/V2` | Quaternius 模块化捏人、UAL 动作、时间轴与分支树；22 套服装 | [说明](Tool/V2/README.md)、[启动](Tool/V2/RunToolV2.cmd) |

Tool/V2 的新增服装支持人类和外星人，按用途分组。见[太空服装调研与实现](Tool/V2/Docs/太空服装扩充与调研.md)和[资产扩充目录指南](Tool/V2/Docs/资产扩充与目录指南.md)。

Git 克隆后在 `Tool/V2` 执行 `./Rebuild.ps1` 生成播放器和 Unity 包，`./Rebuild.ps1 -Models` 同时重建 Blender 模型。各版本的 Assets、Packages、ProjectSettings、源模型和许可证纳入追踪；Build、Export 包、Unity 缓存、下载压缩包与连续截图留在本地。具体范围见 [Git 追踪说明](Git追踪说明.md)。

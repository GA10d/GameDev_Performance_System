# Git 文件管理

本仓库包含根目录和 `V2_Quaternius` 两套 Unity 样板。Git 保存可编辑源文件、构建所需资产与文档；播放器、缓存和批量检查产物留在本地，不纳入版本控制。忽略规则不会删除本地文件。

## 保留的内容

- 两套 `UnityProject/Assets` 及全部 `.meta`，包括场景、脚本、材质、贴图、模型和已导入的动画。
- `UnityProject/Packages`（包括 `packages-lock.json`）和 `UnityProject/ProjectSettings`。
- `Source` 中可编辑的 `.blend`、资产清单，以及 `V2_Quaternius/Source/Downloads/Humans_Master_Humanoid.blend`。后者是角色重建脚本的输入。
- 工具脚本、启动与构建脚本、文档、离线说明 HTML、第三方许可和下载包中的许可／设置说明。
- `QA/runtime` 的关键截图与验收结果、构建摘要、素材审计 JSON 等文档证据。

不要全局忽略 `.meta`、`.asset`、`.fbx`、`.blend`、图片或 DLL；这些格式可能是项目必需的源资产或插件。

## 仅在本地保留的内容

- 两套 `Build`／`Builds` 播放器输出，以及 Unity 的 `Library`、`Temp`、`obj`、`Logs`、`UserSettings` 等生成目录。
- IDE 用户状态、自动生成的解决方案、Python 缓存、Blender 自动备份和日志。
- `QA/backups`、机器相关的 `QA/delivery-manifest.json`、素材预处理验证导出的重复 Blend／FBX；审计 JSON 仍保留。
- `V2_Quaternius/QA/visual-review` 下的大批检查帧、CSV 索引和拼图。
- `V2_Quaternius/Source/Downloads` 的下载 ZIP 与重复 FBX／GLB；Unity 实际使用的动画仍保存在 `Assets` 中。

## 从 Git 克隆后的使用

在要运行的版本目录执行 `Rebuild.ps1`，生成 `Build` 后再运行 `Launch.cmd`。Unity 首次打开项目会重新生成缓存。角色再生成所需的原始人物 Blend 已保留，可按原文档使用 `-RegenerateCharacters` 或 `-RegenerateCast`。

`V2_Quaternius/画面复查.html` 是完整本地交付的复查入口，依赖被忽略的 `QA/visual-review` 检查帧。仅克隆 Git 后，这个页面没有完整图像数据；关键演示截图和文字验收记录仍可阅读。构建后可运行 `ReviewFrames.ps1` 和 `ReviewFrames.ps1 -Matrix` 生成当前版本的检查帧。历史 `before-visible` 对照帧需从原交付或本地备份保留，当前版本不能重建修复前画面。恢复完整三组数据后可用 `Tools/build_review_page.py` 更新页面。

需要原下载压缩包或其他引擎导出时，可参考第三方许可文档及 `Tools/fetch_animations.py` 重新获取；常规 Unity 构建不依赖这些副本。播放器和完整检查帧可作为单独交付包保存。

## 日常检查

在仓库根目录执行：

```powershell
# 查看待提交文件（忽略项不会出现在列表中）
git status --short

# 列出已经被追踪、但匹配忽略规则的文件；正常应为空
git ls-files -ci --exclude-standard

# 解释具体路径命中了哪条规则
git check-ignore -v -- "Build/ASTRA Performance.exe"
```

以后如果误将生成文件加入索引，确认具体路径后使用 `git rm --cached -- <文件路径>`，或对目录使用 `git rm -r --cached -- <目录路径>`；这只取消追踪，不删除本地副本。

# Git 文件管理

仓库在 `Demo/V1`、`Demo/V2`、`Demo/V3` 保留三个独立 Unity 样板。Git 保存可编辑的 `UnityProject/Assets`、`Packages`、`ProjectSettings`，以及源模型、脚本、文档和关键验收截图。Unity 播放器、缓存和批量检查产物留在本地，不纳入版本控制。

每个版本各自运行 `Rebuild.ps1` 生成 `Build`，然后运行 `Launch.cmd`。Unity 首次打开工程会重新生成 `Library`。V1/V3 可用 `-RegenerateCharacters` 重建角色，V2 可用 `-RegenerateCast` 重建角色；各版本源文件保留在自己的 `Source` 中。

`Demo/V2/画面复查.html` 的完整批量对比需要本地 `Demo/V2/QA/visual-review` 数据；Git 克隆后仍可查看保留的关键截图和文字验收记录。构建后可运行 V2 的 `ReviewFrames.ps1` 和 `ReviewFrames.ps1 -Matrix` 生成当前版本检查帧。历史修复前的 `before-visible` 帧需要从原交付或备份保留。

下载压缩包和重复引擎导出不纳入 Git；Unity 使用的动画、模型及必要许可说明保存在 V2 工程中。不要全局忽略 `.meta`、`.asset`、`.fbx`、`.blend`、图片或 DLL，这些格式可能是项目必需资产。

在仓库根目录运行 `git status --short` 查看待提交文件，运行 `git ls-files -ci --exclude-standard` 检查是否误追踪生成内容。举例：`git check-ignore -v -- "Demo/V3/Build/ASTRA Performance.exe"`。

# Git 追踪说明

整理日期：2026-09-25。仓库：`F:\Documents\GitHub\GameDev_Performance_System`。

## 本次整理

此前旧 `Tool/` 下的 352 个文件显示为删除，整理后的 `Tool/V1`、`Tool/V2` 没有纳入追踪。本次将现有目录迁移及有效源文件加入暂存区；Git 会按内容相似度显示部分重命名、部分新增 / 删除。文件系统中保留 V1、V2 和原有 Demo/V1、V2、V3。

此次只整理工作区规则和 Git 索引，不自动 commit 或 push，也不改写历史。旧提交中的导出包仍保留在历史中。

## 追踪范围

| 纳入 Git | 原因 |
|---|---|
| `UnityProject/Assets`，包括 `.meta` | 代码、着色器、场景、材质、动画、模型、角色配方与 Prefab；GUID 随工程保存 |
| `UnityProject/Packages`、`ProjectSettings` | 包版本与 Unity 设置，支持另一台电脑重建 |
| Blender 原始模型、生成 Blend、实际使用的原始 FBX | 继续制作和直接运行 Unity 都需要；生成 Blend 明确可被构建覆盖 |
| `Tools`、启动 / 重建脚本 | 可复现的资产管线与构建入口 |
| `Docs`、README、许可证和出处清单 | 使用、扩充、授权追溯 |
| QA 的文字 / JSON 报告、联系表、原生 UI 证据 | 精简的可审查验收资料 |

| 仅本地保留 | 原因 |
|---|---|
| Unity Library / Temp / Obj / Logs / UserSettings | 缓存和机器状态 |
| Demo、Tool 的 Build / Builds，Tool 的 Export/*.unitypackage | 从源码构建的发布产物 |
| Tool 的 Source/Downloads/*.zip / *.7z / *.rar | 下载包的重复封装；解压后用于重建的源件与许可证仍追踪 |
| Tool/V2 的 Runtime、HairFinal、AlienFinal、OutfitFinal 下 PNG | 自动生成的连续帧；汇总图与报告保留在 Git，原始帧仍在本机 |
| 日志、Blender 备份、Python 缓存、临时导入工程 | 可再生成 |

规则见根目录 `.gitignore` 及各版本 `.gitignore`。`.gitattributes` 明确 Blend / FBX / 图片为二进制，避免换行转换；Unity YAML 与代码仍能做文本差异。当前不引入 Git LFS 依赖。后续资产增长导致仓库明显膨胀时，可单独评估 LFS，并与团队同步安装；不要只添加指针规则而不迁移文件。

## 日常操作

从本仓库根目录执行：

```powershell
git status --short
git diff --stat
git check-ignore -v Tool/V2/Build/UnityPlayer.dll
git add -- Tool/V2/UnityProject/Assets Tool/V2/UnityProject/Packages Tool/V2/UnityProject/ProjectSettings Tool/V2/Tools Tool/V2/Source Tool/V2/Docs
git diff --cached --stat
```

新增模型后同时追踪其 `.meta`，不得把 `Source`、`Models` 或 `Assets` 整体忽略。新增压缩包不用强行 `git add -f`。运行包可另行压缩分享，仓库克隆按版本 README 重建。

本次索引审计结果保存在 `Tool/V2/QA/git-tracking-audit.txt`。暂存不是备份到远端；只有自行 commit、push 后才会形成新提交并上传。

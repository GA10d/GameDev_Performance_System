# Astra 演出 CLI：给 GPT / Codex 的调用协议

入口是本目录 `astra.py`，Python 3.9+，仅标准库。实际项目默认是相邻的 `../UnityProject`。请先读取本文件，再操作演出；不要直接拼写 Unity YAML 或伪造 GUID。

## 约定

- 所有命令（除 `--help`）stdout 输出一个 UTF-8 JSON 对象；成功退出码 0，失败 2。先检查 `ok` 和 `issues`，不要从日志里的“成功”字样判断。
- 通用参数放在子命令前：`--project`、`--unity`、`--transport auto|editor|batch`、`--timeout 240`。
- `--out` 保存 `data` 为独立 JSON，要求目标文件不存在。stdout 仍返回完整响应；用 UTF-8 读取。机器消费优先使用 `--out`，避免旧 PowerShell 的管道编码问题。
- 资产路径使用正斜杠的 `Assets/...`；操作系统文件路径可以使用绝对 Windows 路径。运行目录不影响默认 Unity 工程定位。
- `schemaVersion: 1`，枚举用 `Unit`、`Choice`、`Medium`、`GreaterOrEqual` 等字符串。未知字段、缺少字段、重复 JSON 键、NaN/Infinity、非法枚举会拒绝。先从模板/inspect 得到完整 JSON，再修改；不要凭记忆手写完整结构。
- `doctor` 是环境诊断，`ok` 表示诊断执行成功，不代表 Unity 就绪；检查 `bridgeInstalled`、`editorReady`、`unityError`。

## 必须遵循的工作流

1. `doctor` 检查环境；`catalog` 查询真实角色路径、动作、场景、镜头、特效。不要捏造素材 ID。
2. 用 `template` 创建新规格，或 `inspect --package Assets/.../Performance.asset` 读取现有演出。
3. 编辑独立 JSON。保留已有角色 ID；换角色只改 `actors[].character`。如果重命名 ID，要同步动作、台词、镜头主体和陪体。节点 ID 改名需要同步入口与所有连线。
4. `validate --input ...`。修复全部 error，向用户说明有意义的 warning。不要忽略角色引用错误。
5. `simulate --input ... --route ...` 测试需要的每个选择及 `silence`；记录结局与资源结果。条件分支要用不同 `initialStats` 的临时规格分别测试。
6. `apply --input ... --dest Assets/AstraToolV2/Performances/Name_v001`。只生成全新目录；已有目录拒绝，不覆盖已有资产。后续修改使用 `inspect → 编辑 JSON → validate → simulate → apply 到 v002`，从返回值取得新的 package 路径。这是版本副本，GUID 与 v001 不同。
7. 用户需要游戏交付时，`export --package <返回的路径> --id <稳定游戏ID> --dest <新的外部目录>`。同一个演出的不同版本可使用同一个游戏 ID，导出目录必须新建或为空。
8. 给用户报告演出资产路径、演出 ID、已测试的选择/结局、警告和导出位置。逻辑测试没有证明画面好看，需要时让用户用 Unity 的“运行预览”检查构图。

## 命令

| 命令 | 参数 | 返回 data |
| --- | --- | --- |
| doctor | 无 | Python、工程、Unity 路径、编辑器连接状态 |
| templates | 无 | 五种模板和支持的参数字段 |
| schema | 无 | 完整 JSON Schema |
| catalog | `--library Assets/.../Library.asset` 可省略 | 角色 ID/名称/路径、动作、场景、镜头、特效 |
| template | `--kind Question --name 名称 [--speaker 角色路径] [--partner 角色路径] [--params 参数.json]` | 可编辑完整演出 JSON，不生成 Unity 资产 |
| inspect | `--package Assets/.../Performance.asset` | 演出 JSON，保留时间轴与所有节点配置 |
| validate | `--input 演出.json` 或 `--package ...` | 校验通过的演出 JSON；错误在 issues |
| simulate | 与 validate 相同，另加 `--route accept,go --wait 0` | 经过的节点、选择、结局、最终局部资源 |
| apply | `--input 演出.json --dest Assets/.../NewVersion` | 实际 package 路径和目录 |
| export | `--package ... --id outpost_help --dest 外部目录` | package 路径、导出目录、游戏演出 ID |

## 模板参数

模板：`SingleCall` 单人来电、`Conversation` 双人对话、`Question` 交互问答、`Broadcast` 纯播报、`Blank` 空白。

`--params` 读取一个 JSON 对象，可覆盖：`sceneId`、`opening`、`reply`、`question`、`replySeconds`、`acceptLabel`、`declineLabel`、`acceptReply`、`declineReply`、`silenceReply`。角色通过 `--speaker`/`--partner` 单独选择；支持 catalog 返回的完整资产路径或无歧义的角色 ID。推荐完整路径。省略参数使用 Unity 模板默认值。

`Question` 自动生成：`opening` 的 `accept/decline/silence` 出口 → 各自回应 → `accepted/declined/unanswered`。问答是一个角色；两人场面使用 Conversation 后编辑 JSON。模板暂不支持四足主讲人；低层 JSON 仍可引用现有四足角色并选择适用动作。

文本改变时：`Text` 时长 = C# UTF-16 字符数 / `charactersPerSecond` + `hold`。台词不能重叠；Choice 必须最后；人物/镜头/动作/场景需要覆盖相应时间，所有片段必须在单元时长内。优先把台词写进 template 的 params，让模板计算完整覆盖。用 inspect 编辑长台词时自行重排时间并重新校验；CLI 不偷偷移动你的镜头。

## 分支与模拟边界

- Unit 没有 Choice：`next`；末尾有 Choice：每个选项 ID 和 `silence`。
- Condition：`yes`/`no`；GlobalChoice：每个选项 ID 和 `silence`；End 不连出口。
- `simulate` 按遇到选择的顺序消费 `--route`，不是按节点排列顺序。缺少后续选择时自动走 silence；多余选择、不可用选项、超时后选择、超过 128 次节点跳转都会报错。
- `--wait` 是每个显式选项前统一等待秒数，默认 0。silence 总是等到超时。复用真实 `ToolLogic` 条件与资源效果；保留当前运行时区别：Unit 内选项按自身 timeout，GlobalChoice 的 timeout 受全局 patience 限制，两者选择后均扣等待耐心。
- 这是确定性逻辑模拟，无渲染、无配音、无帧推进；不是 ToolPlayer 实际画面测试，也不自动穷举所有资源状态。
- `initialStats` / effects 是演出局部资源，不会自动接受游戏任务、扣船舶燃料或改变星图奖励。游戏接入仍需设置 performance 事件和同名 ID。

## Unity 连接方式

Unity 打开、完成编译且退出 Play 模式后，Editor 桥接会处理 `Library/AstraCli` 中的本机请求。`auto` 优先连接现有编辑器；没有可用编辑器且工程未锁定时启动 Unity batch。无需安装 Python 包、MCP 服务或开启网络端口。桥接不切换当前演出，也不会替你打开窗口。

Play 模式/编译/资源导入期间桥接暂停。`PROJECT_BUSY`：退出 Play 并等待编译完成后重试；不要强行启动第二个 Unity。`EDITOR_UNAVAILABLE`：指定 editor 但桥接不在线。关闭编辑器后，auto 可以用本机 Unity 许可证启动 batch。

`REQUEST_CANCELLED` 表示编辑器尚未领取请求且已撤回，可重试。`REQUEST_PENDING` 表示结果不确定，操作可能仍在执行；先读取返回的 `responseFile`，检查生成目录，绝不能直接重复 apply/export。请求、结果、batch 日志保存在 `Library/AstraCli`，可追溯 requestId；删除 Library 会删除这些记录。

导出要求当前包及其依赖已在 Unity 保存；拒绝导出含未保存依赖的版本。CLI 不会全局保存其他编辑中的资产。已存在的输出目录不会被覆盖。客户端与 Editor 均只处理本机受信任文件，不是多用户远程服务。

## 快速实例（在 Tool/V2 目录执行）

```powershell
python .\CLI\astra.py doctor
python .\CLI\astra.py catalog --out .\catalog.json
python .\CLI\astra.py template --kind Question --name "前哨站求援" --speaker "Assets/AstraToolV2/Characters/Original_Echo.asset" --params .\CLI\examples\question.params.json --out .\outpost.performance.json
python .\CLI\astra.py validate --input .\outpost.performance.json
python .\CLI\astra.py simulate --input .\outpost.performance.json --route accept
python .\CLI\astra.py simulate --input .\outpost.performance.json --route decline
python .\CLI\astra.py simulate --input .\outpost.performance.json --route silence
python .\CLI\astra.py apply --input .\outpost.performance.json --dest Assets/AstraToolV2/Performances/Outpost_v001
python .\CLI\astra.py export --package Assets/AstraToolV2/Performances/Outpost_v001/Performance.asset --id outpost_help --dest .\Export\Outpost_v001
```

先确认 catalog 中确有示例角色路径。导出目录整个 Assets（包括 .meta）合并到游戏 UnityProject，再绑定星图和构建。CLI 不自动部署游戏、替换 campaign.json 或修改游戏奖励。

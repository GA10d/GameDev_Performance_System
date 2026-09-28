# 演出系统代码调研与 AI CLI

日期：2026-09-28。范围：Tool V2 的数据、编辑器创作、模板、校验、播放逻辑、会议导出；本次不改变游戏导航事件系统。

## 代码结构与落点

| 层 | 主要代码 | 行为与 CLI 接入点 |
| --- | --- | --- |
| 数据 | Runtime/ToolPackage.cs、ToolGraph.cs、ToolUnit.cs、ToolData.cs | Package 引用素材库和 Graph；Graph 的 Unit 节点引用时间轴，Condition/GlobalChoice/End 控制流程。时间轴包含人物、动作、台词、场景、镜头五组片段。 |
| 素材 | Runtime/ToolLibrary.cs、ToolCharacter.cs、ToolMotion.cs | library 通过 ID 找动作/场景/角色；角色是 Unity 对象引用。CLI catalog 返回真实路径，用路径解析对象引用，不生成 GUID。 |
| 模板 | Editor/ToolV2Templates.cs | 已有五类模板负责布置人物、动作、镜头、时间和分支。CLI 直接调用同一生成器，避免两套模板行为分叉。 |
| 编辑状态 | Editor/ToolV2Authoring.cs、ToolV2TimelineWindow.cs、ToolV2GraphWindow.cs | 单个活动草稿与选中单元共享；自动恢复使用 Library 快照。CLI 不 Activate/AdoptSaved，不切换策划正在编辑的草稿。 |
| 校验 | Runtime/ToolData.cs 的 ToolValidation | 检查素材、时长、出口、选项；原始报错是字符串。CLI 保留已有校验，并补充带路径/错误码的跨轨引用、可达性和结束路径检查。 |
| 播放 | Runtime/ToolPlayer.cs、ToolPlayback.cs | ToolPlayer 用 Enter/Tick/Choose 执行；ToolLogic 处理条件、效果和连线。CLI simulate 复用 ToolLogic，模拟路径和状态，不创建舞台或渲染模型。 |
| 导出 | Editor/ToolV2MeetingExport.cs | 收集 Unity 依赖及运行时资源，保留 meta，创建 Resources/Performances/<ID>.asset。CLI 调用同一导出器，新增可选参数以跳过全局 SaveAssets；现有 UI 默认行为不变。 |

## 实际发现

1. **角色绑定使用字符串，改名不会自动同步。** 模板是 speaker，拖入的新人物常是 actor0；动作、台词、镜头仍可指向旧 ID。原动作校验把找不到角色与时长不足合并成一个“未完整覆盖”提示。CLI 分开报告 ACTOR_NOT_FOUND 和 ACTOR_COVERAGE，并列出具体字段与可用 ID。本次没有修改编辑器原提示或自动改名行为。
2. **图校验不等于能完成。** 原校验会查出口目标存在，但没有保证入口能走到 End。CLI 增加无结束路径错误、不可达节点警告和 End 多余出口错误；条件状态仍需要 simulate 测试，结构可达不保证任意资源组合都可达。
3. **两个选择系统对耐心的处理不同。** Unit 内 Choice 直接按自己的 patienceSeconds 计时；GlobalChoice 的等待时间取节点超时与剩余全局 patience 的较小值。两者选择后均扣等待耗时。CLI 保持这一已有行为，文档明确，未擅自统一游戏规则。
4. **保存、运行、导出不是一个动作。** 草稿是内存对象；正式资产需要 AssetDatabase；游戏收到的是依赖集合及固定 Resources 别名；导出不会修改星图，也不会自动构建游戏。CLI 分成 template/validate/apply/export，返回每一步产物。
5. **全局 SaveAssets 对并行编辑有副作用。** 原 SaveDraft 和导出会保存所有脏资产。CLI 使用复制后的新资产逐个 SaveAssetIfDirty；导出拒绝未保存依赖，并跳过全局保存。独立测试检查不相关脏资产仍为脏状态。
6. **原生 Unity JSON 对外不够稳定。** ScriptableObject 引用/枚举数字不适合 GPT 直接维护。新增版本化中间格式，角色用真实资产路径、单元用 ID、枚举用字符串；Python 校验合同，Unity 负责解析和生成资产。

## 已交付的 CLI

入口 `CLI/astra.py`，详细调用规范 `CLI/agent-guide.md`，结构合同 `CLI/performance.schema.json`，参数示例及完整演出示例在 `CLI/examples`。

支持环境诊断、查询素材和模板、创建模板 JSON、回读已保存包、完整 JSON 编排、校验、确定性分支模拟、生成 Unity 资产、游戏导出。CLI 无第三方 Python 依赖；支持 Unity 已打开时的本地文件桥接和未打开时的 batch 调用。桥接脚本在 Editor 目录，不进入游戏构建。

修改已有内容采用版本副本：inspect → 编辑 → validate → simulate → apply 到新目录。不会覆盖现有包、改变已有 GUID 或删除旧版本。新版本发布可以继续使用同一游戏演出 ID。

## 当前边界与后续建议

- CLI 不创建新的捏人模型、动作动画或配音；使用现有素材库。没有真实画面截图/渲染命令，逻辑模拟不能替代构图检查。
- JSON 编辑可以编排多个 Unit、条件、全局选项、多角色、镜头和局部资源；调整文本后需要重新校验并显式调整时间。未来可增加“延长台词并同步尾部覆盖”操作，但要避免偷偷改变策划的镜头节奏。
- 后续编辑器值得优先做角色下拉绑定和改名联动、区分缺失引用与时长错误、增加分支覆盖面板。
- 游戏任务状态/奖励依然独立。后续应设计明确的 End 结果协议，再接导航事件结算；不让台词里的“接受任务”被误认为已经写入任务系统。
- 为避免多用户修改冲突，本版不提供原地覆写；如果后续需要稳定 GUID 原地更新，应增加资产版本哈希、独占锁和失败回滚后再开放。

验证记录见 QA/CLI。结构和逻辑模拟测试与实际 UI/渲染测试分别说明，不把无画面模拟称为完整游戏验收。

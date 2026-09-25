# ASTRA 演出工具

这是一个独立的 Unity 2022.3.62f1 工程，位于仓库 `Tool/UnityProject`。它以 Demo/V3 的六名原创低模人物和材质为输入，借用 Demo/V2 的十七个动作语义与九类镜头构图，实现角色材料库、演出时间轴、交互演出树和可运行播放器。参考需求来自用户提供的《演出工具.docx》。

## 打开与使用

1. 在 Unity Hub 中添加并打开 `Tool/UnityProject`。使用 Unity 2022.3.62f1，内置渲染管线。
2. 打开菜单 **Astra Performance Tool → Open timeline**。示例包为 `Assets/PerformanceTool/Samples/AstraSample.asset`。若为空，运行 **Create sample project**。
3. 从左侧素材库拖人物、动作、台词、镜头、场景到下方五条轨道。悬停动作可在预览窗试演；拖动片段或右缘调整时长。点击角色可改肤色、服装、发型头饰、配饰、体型，并导出 Prefab。
4. 在演出树中选出口，再点目标节点建立连接；设置选项条件、资源增减、沉默出口及全局选择。点击**验证**修复提示问题。
5. 点击**运行预览**进入 Unity Game 视图，或直接打开 `Assets/PerformanceTool/Scenes/ToolPreview.unity` 后按 Play。选项用鼠标点击，空格暂停。示例走向：弦提出请求 → 支付进入砾的维修段，或检查/沉默进入伏的爬行段 → 全局保存选择。
6. **导出演出单元**会复制当前单元为 `.asset`；**导出 Unity 包**生成 `Tool/Export/ASTRA_Performance_Tool.unitypackage`，可导入 `Project-Astra/3D Demo/UnityProject`。导入后打开包内预览场景即可复现。

`Source/Astra_Six_Species.blend` 是人物可编辑源文件。工具不依赖 Demo/V2 的 Quaternius 动画文件：V3 使用自有骨骼，动作以程序化姿态复现 V2 动作目录与演出节奏，不能将 V2 动画片段直接重定向到 V3 骨架。

## 目录

| 路径 | 内容 |
|---|---|
| `UnityProject/Assets/Performance` | V3 人物、材质、纹理与源演出资源 |
| `UnityProject/Assets/PerformanceTool/Runtime` | 数据模型、校验、角色外观、动作、镜头、演出播放器 |
| `UnityProject/Assets/PerformanceTool/Editor` | 素材库、时间轴、演出树、导出与构建 |
| `UnityProject/Assets/PerformanceTool/Samples` | 六角色、十七动作、十镜头、三场景和三段演出实例 |
| `Docs` | 策划与技术说明、批量素材规范、验收记录 |
| `Source` | Blender 源模型 |
| `Export` | 导入 Project-Astra 使用的 Unity 包 |
| `Build` | 本地构建的 Windows 预览播放器；Git 不应提交 |
| `QA` | Unity 批量验证和实机画面检查产物；日志与截图不必提交 |

详细设计见 [需求与演出策划](Docs/01_演出策划.md)、[技术架构](Docs/02_技术架构.md)、[批量素材规范](Docs/03_批量素材规范.md)、[验收记录](Docs/04_验收记录.md)。资产来源见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。

## 当前边界

动作是 V2 风格的实时骨骼姿态，并非 V2 的原始 Quaternius Clip。复杂地形爬行、精确手指/道具接触、口型音素、语音录制与可视化波形不在此版实现范围。Unity 编辑器内预览使用独立简化舞台，最终构图以 Game 视图和截图验收为准。

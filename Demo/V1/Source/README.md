# 人物源资产

- `Astra_Performers.blend`：三位原创低模角色，横向排列供编辑。每人有独立 Armature 与 SkinnedMesh，局部几何由对应骨骼控制。
- `character_manifest.json`：三角面、骨骼数量、身高与资产来源。
- `asset_manifest.example.json`：本地批处理输入范例，路径以清单所在目录为基准。

生成：在根目录运行 `Rebuild.ps1 -RegenerateCharacters`。脚本会覆盖本脚本生成的三个 FBX 和 Blender 源文件；如果已经手工改模，请先另存手工版本并直接使用 Unity 构建，不再勾选重新生成。

人物的 Blender 前方为 -Y，FBX 进入 Unity 后为 +Z，场景生成器以 Y=180° 朝向摄影机。全局单位为米。模型本身包含眼皮和下颌骨；程序化适配器根据导入后的局部坐标系驱动，不假定 Blender 与 Unity 骨骼轴一致。

几何没有使用 Mouthwashing、Arctic Eggs 或任何下载的人体模型。背景舱室来自用户已有 Astra 项目，来源记录见根目录 THIRD_PARTY_NOTICES。

`Tools/batch_prepare.py` 的样例刻意要求减面到 65%，但默认规则会保护此绑定人物并在报告中记录跳过。这个测试验证安全的预处理边界，不等于已把人物拓扑优化到 65%。

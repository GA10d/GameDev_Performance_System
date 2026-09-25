# 资产来源与授权记录

## 用户已有 Project Astra 资产

以下文件来自用户指定的 `F:/Documents/GitHub/Project-Astra/3D Demo`，按用户要求在独立演出工程中复用，未修改源项目。这不意味着这些项目资产被重新许可为公共素材。

| 本工程路径 | 原始来源 |
|---|---|
| UnityProject/Assets/Performance/Models/Astra_Cabin.fbx | 用户已有 Blender 舱室模型导出 |
| UnityProject/Assets/Performance/Shaders/WornMetal.shader | 用户已有 Astra Shader |
| UnityProject/Assets/Performance/Textures/NavalPaint_Albedo.png | 既有项目生成的旧漆色纹理；其生成记录在源项目 Source/material_generation.md |

## ambientCG

`PaintedMetal012_2K-PNG_NormalGL.png` 和 `PaintedMetal012_2K-PNG_Roughness.png` 来自现有项目已下载的 ambientCG PaintedMetal012 素材包。

- 作者/提供方：ambientCG。
- [素材主页](https://ambientcg.com/view?id=PaintedMetal012)
- [官方许可证说明](https://docs.ambientcg.com/license/)
- 许可：CC0 1.0 Universal。
- 此处只复制用于微表面法线与粗糙度的两张贴图。

## 本次原创资产

原三名人物及本次新增的站立外星人、半兽人、四肢爬行外星人，以及程序化动作、场景补充几何、台词与生成音效为任务内编写。`Tools/build_characters.py` 与 `Tools/build_species.py` 不含第三方人物网格。原始 Blender、FBX 和审计文件一并交付。

文档推荐的 Quaternius、Kenney、MakeHuman 和 Mixamo 资源没有被静默下载、购买或混入演示。示例批处理清单使用本次原创人物作验证输入。

## 字体与运行时

中文 UI 调用本机安装的 Microsoft YaHei / CJK fallback；没有再分发 Windows 字体文件。Windows 构建包含 Unity 构建系统正常生成的播放器和运行时文件，其许可属于各自提供方。发布到其他操作系统前，应补齐字体与平台的独立验收。

## 参考游戏

Mouthwashing、Arctic Eggs 的名称和来源链接仅用于研究。Demo 不包含其人物、动画、声音、台词、贴图或商标图案。研究文件没有声称获得两款游戏原始实现的授权或源码。

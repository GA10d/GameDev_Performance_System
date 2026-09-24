from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=root/'Docs/02_实现与扩展.md'
s=p.read_text(encoding='utf-8')
s=s.replace('场景生成不会覆盖已经存在的剧情资产。','场景生成保留已有剧情资产；V2.1 通过 reviewRevision 执行一次已列明的镜头、移动起止点迁移，之后不重复覆盖。')
s=s.replace('重播和跳镜会重置角色站位与基础动作，避免把上段状态带到新镜头。','自动切镜保留已经发生的站位移动及动画衔接；手动跳镜会重建该段之前的移动结果，避免从行走切入近景时人物瞬移回原位。')
s=s.replace('种族缩放同时写进网格顶点与 edit bones。','种族缩放同时写进网格顶点与 edit bones。V2.1 先快照所有骨骼的 head/tail/roll 和连接关系，再断开 use_connect，一次性从快照计算新位置，最后恢复几何上仍相接的连接。不能边遍历边修改连接中的关节，否则 Blender 会反向更新父骨尾端，造成重复缩放和反向手指。导出前断言手指长度及方向。')
s=s.replace('镜头切换记录上一个姿态，以 smoothstep 插值过渡；','跨场景机位采用剪辑硬切；仅同一主体、同一镜头类型且移动小于 0.7 米的重构图允许 smoothstep 插值。取景锚点使用休止身高与该镜头开始时的站位，不直接读取每帧的动态头骨高度；')
s=s.replace('视口与 UI 使用同一 1600×900 等比参考坐标，适配窗口大小。','视口与 UI 使用同一 1600×900 等比参考坐标，适配窗口大小。试演模式将三维视口缩至左侧，控制台独占右栏；单人试演使用统一的无遮挡站位，双人和过肩使用独立配对站位，退出后恢复剧情。暂停状态只显示顶栏标记，不覆盖表演画面。')
s=s.replace('## 扩展接口','所有角色 SkinnedMeshRenderer 开启 updateWhenOffscreen，让包围盒随实际蒙皮姿态更新。原 FBX 的站立包围盒会在坐姿近镜中错误地剔除整个头部或面部附件；只设置 Animator AlwaysAnimate 不能修复这个渲染问题。\n\nVisualAudit 用固定 1/30 秒表演步长跑完整故事及源 clip 周期，定期保存真实播放器帧和 CSV，另遍历四名角色的九种镜头。截图黑屏会中止审查，不将未渲染的图片当作有效证据。该模式不代替正常速度下的窗口操作复查。\n\n## 扩展接口')
p.write_text(s,encoding='utf-8')
p=root/'README.md';s=p.read_text(encoding='utf-8');s=s.replace('# ASTRA · THE RELAY / 演出系统 V2','# ASTRA · THE RELAY / 演出系统 V2.1')
s=s.replace('## 本次变化','**V2.1 画面修订**：修复手指骨骼变形、坐姿头部错误剔除、切镜站位重置、镜头随头骨晃动及试演遮挡。逐镜问题记录见 [Docs/04_逐镜复查.md](Docs/04_逐镜复查.md)，原始帧和对比见 [画面复查.html](画面复查.html)。\n\n## 本次变化')
s=s.replace('重建场景会保留已有数据资产。','除一次 V2.1 迁移外，重建场景会保留已有数据资产。')
s=s.replace('试验台的单人镜头会隔离当前角色，便于检查动作；全景、双人与过肩镜头保留其他角色。','试验台的单人镜头将角色移至无遮挡站位；双人与过肩只保留当前配对，全景保留四人。返回故事会重建剧情站位。')
p.write_text(s,encoding='utf-8')
print('Technical documentation updated')

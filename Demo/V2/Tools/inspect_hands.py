import bpy, json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
report={}
for name,path in [('original', root/'Source/Downloads/Humans_Master_Humanoid.blend'),('Lin',root/'Source/Astra_Lin.blend')]:
    bpy.ops.wm.open_mainfile(filepath=str(path))
    rig=bpy.data.objects['CharacterArmature']
    report[name]={'rig_matrix': [list(row) for row in rig.matrix_world], 'bones':{b.name:{'head':list(b.head_local),'tail':list(b.tail_local),'connected':b.use_connect} for b in rig.data.bones if b.name.endswith('.L') and any(s in b.name for s in ['Hand','Index','Thumb','LowerArm'])},'meshes':{}}
    for obj in bpy.data.objects:
        if obj.type!='MESH' or obj.name!='SpaceSuit_Body':continue
        hand=[]
        for v in obj.data.vertices:
            groups=[(obj.vertex_groups[g.group].name,g.weight) for g in v.groups]
            if any(n=='Index4.L' and w>.5 for n,w in groups):hand.append(list(v.co))
        report[name]['meshes'][obj.name]={'matrix':[list(row) for row in obj.matrix_world], 'index4_verts':hand,'groups':[g.name for g in obj.vertex_groups]}
(root/'QA/visual-review/hands.json').write_text(json.dumps(report,indent=2))

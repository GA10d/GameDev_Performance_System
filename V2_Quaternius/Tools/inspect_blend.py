import bpy,json,pathlib
root=pathlib.Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'Source/Downloads/Humans_Master_Humanoid.blend'))
info={'objects':[],'actions':[],'materials':[]}
for o in bpy.data.objects:
    item={'name':o.name,'type':o.type,'location':list(o.location),'dimensions':list(o.dimensions)}
    if o.type=='ARMATURE':item['bones']=[b.name for b in o.data.bones]
    if o.type=='MESH':item.update(vertices=len(o.data.vertices),materials=[m.name for m in o.data.materials],groups=[g.name for g in o.vertex_groups])
    info['objects'].append(item)
for a in bpy.data.actions:info['actions'].append({'name':a.name,'range':list(a.frame_range)})
for m in bpy.data.materials:info['materials'].append({'name':m.name,'color':list(m.diffuse_color)})
(root/'QA/original-blend.json').write_text(json.dumps(info,indent=2),encoding='utf-8')
print('INSPECT_OK',len(info['objects']),len(info['actions']))

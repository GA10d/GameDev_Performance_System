"""Quaternius CC0 modular meshes -> four ASTRA variants, retained skinning + editable Blend."""
import bpy, bmesh, math, json, pathlib
from mathutils import Vector, Matrix
ROOT=pathlib.Path(__file__).resolve().parents[1]
SOURCE=ROOT/'Source/Downloads/Humans_Master_Humanoid.blend'
OUT=ROOT/'UnityProject/Assets/PerformanceV2/Models';OUT.mkdir(parents=True,exist_ok=True)
def material(name,color):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.82
    return m
def bind(o,rig,bone,mat):
    o.data.materials.clear();o.data.materials.append(mat)
    for f in o.data.polygons:f.use_smooth=False
    group=o.vertex_groups.new(name=bone);group.add(list(range(len(o.data.vertices))),1,'REPLACE')
    mod=o.modifiers.new('Quaternius skin','ARMATURE');mod.object=rig;o.parent=rig
    return o
def ellipsoid(name,loc,scale,mat,rig,bone='Head',segments=12,rings=8):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=loc)
    o=bpy.context.object;o.name=name;o.scale=scale
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    return bind(o,rig,bone,mat)
def cone(name,start,end,radius,mat,rig,bone='Head'):
    a,b=Vector(start),Vector(end);d=b-a
    bpy.ops.mesh.primitive_cone_add(vertices=7,radius1=radius,radius2=.004,depth=d.length,location=(a+b)/2)
    o=bpy.context.object;o.name=name;o.rotation_euler=d.to_track_quat('Z','Y').to_euler()
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    return bind(o,rig,bone,mat)
def box(name,loc,size,mat,rig,bone='Chest'):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.scale=size
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    return bind(o,rig,bone,mat)

cast=[
 {'id':'Lin','species':'human','parts':['Casual_Head','SpaceSuit_Body','SpaceSuit_Legs','SpaceSuit_Feet'],'scale':(1,1,1),'coat':(.48,.46,.35),'skin':(.52,.34,.22)},
 {'id':'Echo','species':'alien','parts':['Casual2_Head','SpaceSuit_Body','SpaceSuit_Legs','SpaceSuit_Feet'],'scale':(.87,.9,1.06),'coat':(.22,.32,.30),'skin':(.32,.47,.43)},
 {'id':'Ruk','species':'half-orc','parts':['Casual2_Head','Worker_Body','Swat_Legs','SpaceSuit_Feet'],'scale':(1.23,1.14,1.08),'coat':(.35,.23,.17),'skin':(.34,.38,.20)},
 {'id':'Voss','species':'human','parts':['Worker_Head','Farmer_Body','Worker_Legs','Worker_Feet'],'scale':(1.04,1.04,.98),'coat':(.37,.41,.30),'skin':(.43,.27,.17)}
]
manifest=[]
for c in cast:
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    rig=bpy.data.objects['CharacterArmature'];keep=set(c['parts']+[rig.name])
    for o in list(bpy.data.objects):
        if o.name not in keep:bpy.data.objects.remove(o,do_unlink=True)
    for o in bpy.data.objects:o.hide_set(False);o.hide_viewport=False;o.hide_render=False
    mats={
      'Skin':material('Skin_'+c['id'],c['skin']),
      'Coat':material('Coat_'+c['id'],c['coat']),
      'Dark':material('CanvasDark',(.075,.10,.09)),
      'Trim':material('OldBrass',(.42,.30,.12)),
      'Bone':material('OldIvory',(.68,.63,.44)),
      'Eye':material('EyeDark',(.012,.021,.019)),
      'Hair':material('HairCharcoal',(.047,.038,.032)),
      'Rust':material('OxideRed',(.32,.105,.058))}
    for o in list(bpy.data.objects):
        if o.type!='MESH':continue
        for slot in o.material_slots:
            n=slot.material.name
            key='Skin' if n.startswith('Skin') else 'Eye' if n in ['Eye','Visor'] else 'Hair' if n in ['Hair','Eyebrows','Moustache'] else 'Trim' if n in ['Worker_Yellow','Earrings','Gold','SciFi_Light_Accent'] else 'Dark' if n in ['Black','Grey','SciFi_MainDark','Brown2','Swat_Black'] else 'Coat'
            slot.material=mats[key]
        # Source topology and deform weights are preserved. Species head edits happen in mesh space.
        if o.name.endswith('_Head') and c['species']!='human':
            bm=bmesh.new();bm.from_mesh(o.data)
            bad=[f for f in bm.faces if o.data.materials[f.material_index] in [mats['Hair'],mats['Eye']]]
            bmesh.ops.delete(bm,geom=bad,context='FACES');bm.to_mesh(o.data);bm.free()
            for v in o.data.vertices:
                z=v.co.z
                if c['species']=='alien':
                    t=max(0,min(1,(z-1.60)/.24));v.co.x*=1+.65*t;v.co.y*=1+.18*t;v.co.z=1.51+(z-1.51)*1.28
                else:
                    v.co.x*=1.28;v.co.y= v.co.y*1.08-(.035 if z<1.69 else 0);v.co.z=1.51+(z-1.51)*1.03
    if c['species']=='alien':
        for s in [-1,1]:
            ellipsoid('Large_oblique_eye',(.080*s,-.190,1.765),(.052,.033,.069),mats['Eye'],rig)
            ellipsoid('Eye_reflection',(.070*s,-.221,1.791),(.008,.006,.012),mats['Bone'],rig,segments=8,rings=4)
            cone('Sensory_fin',(.13*s,.005,1.78),(.215*s,.03,1.94),.046,mats['Skin'],rig)
        ellipsoid('Cranial_dome',(0,-.015,1.827),(.141,.129,.13),mats['Skin'],rig)
        ellipsoid('Forehead_ridge',(0,-.136,1.863),(.017,.012,.06),mats['Coat'],rig,segments=8,rings=4)
        ellipsoid('Mouth_seam',(0,-.193,1.638),(.036,.008,.006),mats['Eye'],rig,segments=8,rings=4)
    if c['species']=='half-orc':
        ellipsoid('Heavy_lower_jaw',(0,-.113,1.598),(.114,.12,.071),mats['Skin'],rig)
        ellipsoid('Heavy_nose',(0,-.186,1.689),(.055,.043,.036),mats['Skin'],rig)
        ellipsoid('Mouth_seam',(0,-.240,1.629),(.075,.013,.009),mats['Eye'],rig)
        for s in [-1,1]:
            cone('Pointed_ear',(.106*s,.005,1.713),(.233*s,.015,1.825),.062,mats['Skin'],rig)
            cone('Lower_tusk',(.066*s,-.236,1.600),(.078*s,-.264,1.720),.025,mats['Bone'],rig)
            ellipsoid('Orc_eye',(.064*s,-.173,1.704),(.025,.029,.015),mats['Bone'],rig,segments=8,rings=4)
            ellipsoid('Orc_pupil',(.064*s,-.199,1.704),(.008,.009,.01),mats['Eye'],rig,segments=8,rings=4)
            ellipsoid('Brow_ridge',(.065*s,-.173,1.735),(.061,.037,.025),mats['Skin'],rig,segments=8,rings=4)
        ellipsoid('Short_crest',(0,.002,1.813),(.04,.12,.058),mats['Hair'],rig,segments=8,rings=4)
    # Common utilitarian insignia, harness and a receiver emphasize one coherent setting.
    box('Chest_identification',(-.10,-.144,1.39),(.10,.012,.055),mats['Bone'],rig)
    for i in range(3):box('Serial_bar',(-.125+i*.021,-.152,1.389),(.009,.007,.035),mats['Dark'],rig)
    box('Receiver',(.15,-.133,1.32),(.075,.035,.14),mats['Dark'],rig)
    box('Receiver_indicator',(.15,-.154,1.36),(.047,.009,.012),mats['Trim'],rig)
    # The source pack uses deliberately oversized hands; reduce the complete
    # hand region and matching rest bones together for closer dialogue shots.
    hand_anchors={s:rig.data.bones['Hand'+s].head_local.copy() for s in ['.L','.R']}
    for o in bpy.data.objects:
        if o.type!='MESH':continue
        for v in o.data.vertices:
            for side,anchor in hand_anchors.items():
                names={'Hand'+side,'Fist'+side,'Wrist'+side}|{f+str(n)+side for f in ['Thumb','Index','Middle','Ring','Pinky'] for n in [1,2,3,4]}
                weight=min(1,sum(g.weight for g in v.groups if o.vertex_groups[g.group].name in names))
                if weight:v.co=anchor+(v.co-anchor)*(1-.22*weight)
    # Bake anisotropic species proportions in both rest skeleton and mesh to avoid runtime scaled-bone errors.
    scale=Matrix.Diagonal((*c['scale'],1))
    for o in bpy.data.objects:
        if o.type=='MESH':
            for v in o.data.vertices:v.co=scale@v.co
            for p in o.data.polygons:p.use_smooth=False
    bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
    # Editing connected heads propagates back into their parent's tail. Snapshot
    # and disconnect the complete rest skeleton before applying any transforms.
    rest={b.name:(b.head.copy(),b.tail.copy(),b.roll,b.use_connect) for b in rig.data.edit_bones}
    for b in rig.data.edit_bones:b.use_connect=False
    for b in rig.data.edit_bones:
        start,end,roll,connected=rest[b.name]
        for side,anchor in hand_anchors.items():
            if b.name.endswith(side) and (b.name.startswith('Hand') or b.name.startswith(('Thumb','Index','Middle','Ring','Pinky'))):
                start=anchor+(start-anchor)*.78;end=anchor+(end-anchor)*.78
        b.head=scale@start;b.tail=scale@end;b.roll=roll
    # The original thumb has two segments. Split its final segment to provide
    # the three standard humanoid finger mappings used by the animation library.
    for side in ['.L','.R']:
        old=rig.data.edit_bones['Thumb3'+side];end=old.tail.copy();old.tail=old.head.lerp(end,.6)
        tip=rig.data.edit_bones.new('Thumb4'+side);tip.head=old.tail;tip.tail=end;tip.parent=old;tip.use_connect=True
    for b in rig.data.edit_bones:
        if b.name in rest and rest[b.name][3] and b.parent and (b.head-b.parent.tail).length<.00001:b.use_connect=True
    # Reject collapsed/reversed finger segments before FBX export.
    for b in rig.data.edit_bones:
        if b.name.startswith(('Index','Middle','Ring','Pinky')):
            assert b.length>.012, (c['id'],b.name,b.length)
            side=1 if b.name.endswith('.L') else -1
            assert (b.tail.x-b.head.x)*side>0, (c['id'],b.name,'reversed')
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=rig
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Source'/('Astra_'+c['id']+'.blend')))
    bpy.ops.export_scene.fbx(filepath=str(OUT/(c['id']+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',use_armature_deform_only=False)
    meshes=[o for o in bpy.data.objects if o.type=='MESH'];tris=sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes)
    manifest.append({**c,'triangles':tris,'bones':len(rig.data.bones),'source':'Quaternius Ultimate Modular Men CC0','edits':'palette, modular outfit, species mesh edits, weighted accessories, proportional skeleton'})
(ROOT/'Source/cast_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('CAST_EXPORT_OK',json.dumps(manifest))

"""Deterministic original low-poly cast. Blender background -> editable .blend + FBX.
No downloaded character geometry. Weighted rigid pieces intentionally expose planar forms.
"""
import bpy, bmesh, math, json, random
from mathutils import Vector
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'UnityProject/Assets/Performance/Models'
SOURCE=ROOT/'Source'
OUT.mkdir(parents=True,exist_ok=True); SOURCE.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
random.seed(47)

def mat(name,col):
    m=bpy.data.materials.new(name); m.diffuse_color=(*col,1); m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=(*col,1); bs.inputs['Roughness'].default_value=.91
    return m

M={k:mat(k,c) for k,c in {'Skin':(.54,.40,.29),'SkinLight':(.64,.50,.36),'SkinShadow':(.35,.25,.19),'Hair':(.045,.054,.046),'Suit':(.48,.46,.35),'SuitDark':(.23,.26,.21),'Boot':(.075,.085,.071),'Seam':(.16,.18,.15),'Patch':(.71,.65,.48),'Red':(.38,.14,.105),'WhiteEye':(.70,.67,.49),'Iris':(.07,.09,.075),'Lip':(.26,.145,.115),'Metal':(.32,.30,.22),'Glass':(.12,.23,.21)}.items()}
parts=[]; current_arm=None
def bind(ob, material, bone):
    ob.data.materials.append(M[material]); vg=ob.vertex_groups.new(name=bone); vg.add(list(range(len(ob.data.vertices))),1,'REPLACE'); parts.append(ob)
    return ob
def cube(name,loc,scale,material,bone='Chest',bevel=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc); o=bpy.context.object; o.name=name; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('single plane chamfer','BEVEL');mod.width=bevel;mod.segments=1
        bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
    return bind(o,material,bone)
def ellipsoid(name,loc,scale,material,bone='Head',segments=10,rings=5):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return bind(o,material,bone)
def tube(name,a,b,r1,r2,material,bone,vertices=8):
    a,b=Vector(a),Vector(b);mid=(a+b)/2
    bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r1,radius2=r2,depth=(b-a).length,location=mid)
    o=bpy.context.object;o.name=name;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return bind(o,material,bone)
def mesh(name,verts,faces,material,bone):
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();me.update()
    o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o)
    return bind(o,material,bone)
def body_ring(name,rings,material,bone,n=8):
    v=[]
    for z,rx,ry,cy in rings:
        for i in range(n):
            a=2*math.pi*i/n;v.append((rx*math.sin(a),cy+ry*math.cos(a),z))
    f=[tuple(range(n-1,-1,-1))]
    for j in range(len(rings)-1):
        for i in range(n):a=j*n+i;b=j*n+(i+1)%n;f.append((a,b,b+n,a+n))
    f.append(tuple(range((len(rings)-1)*n,len(rings)*n)))
    return mesh(name,v,f,material,bone)

def make_character(index):
    global parts
    parts=[]; names=['Lin047','He112','Yu203']; name=names[index]
    # Blender front is -Y; Unity imports this FBX facing +Z. Scene applies Y=180.
    width=[1,1.14,.9][index]
    headx=[.121,.134,.112][index]; facez=[1.565,1.565,1.585][index]
    body_ring('Jacket',[(.86,.18*width,.105,0),(1.04,.195*width,.13,0),(1.30,.24*width,.135,.008),(1.40,.22*width,.113,.006),(1.44,.105,.085,0)],'Suit','Chest')
    body_ring('Trouser waist',[(.78,.175*width,.103,0),(.96,.18*width,.113,0)],'SuitDark','Hips')
    tube('Neck',(0,0,1.39),(0,0,1.55),.069,.063,'Skin','Head')
    body_ring('Thick standing collar',[(1.39,.10,.10,0),(1.49,.088,.085,0)],'SuitDark','Chest',10)
    body_ring('Collar lining',[(1.46,.091,.088,0),(1.485,.092,.089,0)],'Patch','Chest',10)
    # A shaped head instead of a cube: chin / jaw / cheekbone / brow / skull.
    z=facez
    head=body_ring('Faceted head',[(z-.16,headx*.45,.065,-.035),(z-.125,headx*.78,.090,-.020),(z-.05,headx,.110,0),(z+.035,headx,.105,.004),(z+.12,headx*.87,.095,.007),(z+.16,headx*.48,.066,.01)],'Skin','Head',12)
    # Slightly asymmetrical planar nose, nostrils, cheek shadows and sockets.
    mesh('Nose bridge',[(-.024,-.106,z+.045),(.024,-.106,z+.045),(-.027,-.115,z-.056),(.027,-.115,z-.056),(0,-.159,z-.044)],[(0,1,4),(1,3,4),(3,2,4),(2,0,4),(0,2,3,1)],'SkinLight','Head')
    for sign in [-1,1]:
        x=sign*.054
        ellipsoid('Eye socket',(x,-.101,z+.006),(.042,.012,.023),'SkinShadow')
        cube('Eye white',(x,-.115,z+.008),(.050,.009,.013),'WhiteEye','Head',.002)
        cube('Pupil',(x-sign*.003,-.121,z+.007),(.012,.004,.013),'Iris','Head')
        cube('Eyelid',(x,-.126,z+.008),(.055,.004,.016),'Skin','Lid')
        brow=cube('Heavy brow',(x,-.117,z+.036),(.065,.015,.012),'Hair','Head',.002);brow.rotation_euler.y=sign*.10
        ellipsoid('Ear',(sign*(headx+.008),0,z-.01),(.024,.038,.044),'Skin','Head',8,4)
        cube('Under eye plane',(x,-.109,z-.024),(.052,.004,.011),'SkinShadow','Head')
        ellipsoid('Nostril',(sign*.016,-.137,z-.060),(.009,.012,.005),'SkinShadow','Head',6,3)
    cube('Mouth cavity',(0,-.106,z-.096),(.062,.012,.024),'Lip','Head',.004)
    cube('Lower lip',(0,-.116,z-.105),(.063,.008,.008),'SkinShadow','Jaw',.002)
    cube('Mouth seal',(0,-.119,z-.091),(.063,.003,.015),'Skin','Jaw')
    # Hair silhouette varies for each character, with large readable locks.
    ellipsoid('Hair cap',(0,.012,z+.11),(headx*1.02,.113,.10),'Hair','Head',10,4)
    if index==0:
        for k in range(7):
            x=-.10+k*.030; o=cube('Rough fringe',(x,-.087,z+.078+(k%3)*.008),(.044,.046,.064),'Hair','Head',.008);o.rotation_euler.y=.2
        cube('Temple hair',(-.108,-.040,z-.008),(.031,.055,.11),'Hair','Head',.008)
    elif index==1:
        for sign in [-1,1]:
            tube('Goggle cup',(sign*.059,-.073,z+.108),(sign*.059,-.131,z+.108),.046,.046,'Metal','Head',10)
            tube('Goggle lens',(sign*.059,-.131,z+.108),(sign*.059,-.136,z+.108),.035,.035,'Glass','Head',10)
        cube('Goggle strap',(0,.091,z+.102),(.20,.035,.027),'Boot','Head')
        ellipsoid('Beard',(0,-.030,z-.122),(.098,.089,.052),'SkinShadow','Head',10,3)
    else:
        for k in range(6):
            x=-.108+k*.037;o=cube('Long fringe',(x,-.063,z+.021+(k%3)*.022),(.049,.065,.15),'Hair','Head',.012);o.rotation_euler.y=-.22
        cube('Rear hair',(0,.087,z-.026),(.20,.052,.24),'Hair','Head',.022)
    # Coveralls: pockets, zip, seam strips, reinforced elbows, patched knees.
    cube('Zipper',(0,-.137,1.193),(.011,.008,.39),'Seam','Chest')
    for sign in [-1,1]:
        cube('Chest pocket',(sign*.118,-.133,1.276),(.132,.021,.126),'SuitDark','Chest',.009)
        cube('Pocket flap',(sign*.118,-.151,1.331),(.132,.016,.026),'Patch','Chest',.003)
        cube('Belt segment',(sign*.097,-.106,.954),(.175,.018,.05),'Boot','Hips',.005)
    cube('Buckle',(0,-.128,.954),(.050,.025,.042),'Metal','Hips',.005)
    cube('Identification cloth',(-.12,-.157,1.28),(.082,.007,.046),'Patch','Chest',.002)
    for k in range(6):cube('ID barcode',(-.151+k*.012,-.162,1.28),(.005,.003,.027 if k%2 else .033),'Seam','Chest')
    cube('Safety tab',(.183,-.126,1.18),(.025,.008,.105),'Red','Chest')
    for sign in [-1,1]:
        side='L' if sign<0 else 'R';sx=sign*.244*width
        shoulder=(sx,0,1.345); elbow=(sx+sign*.046,-.004,1.04); wrist=(sx+sign*.05,-.037,.80)
        tube('Sleeve upper',shoulder,elbow,.085,.065,'Suit','UpperArm'+side)
        tube('Sleeve lower',elbow,wrist,.068,.044,'Suit','Forearm'+side)
        cube('Elbow reinforcement',(elbow[0],.039,1.055),(.101,.045,.102),'SuitDark','Forearm'+side,.017)
        tube('Cuff',(wrist[0],wrist[1],.85),wrist,.049,.047,'SuitDark','Forearm'+side)
        ellipsoid('Hand',(wrist[0],-.040,.752),(.044,.041,.067),'Skin','Forearm'+side,8,4)
        ellipsoid('Thumb',(wrist[0]-sign*.037,-.051,.768),(.021,.025,.039),'Skin','Forearm'+side,6,3)
        hip=(sign*.098,0,.84);knee=(sign*.113,.0,.48);ankle=(sign*.126,-.004,.13)
        tube('Trouser upper',hip,knee,.104,.078,'SuitDark','Thigh'+side)
        tube('Trouser lower',knee,ankle,.078,.059,'SuitDark','Shin'+side)
        cube('Knee patch',(knee[0],-.065,.486),(.123,.025,.139),'Suit','Shin'+side,.015)
        cube('Boot',(ankle[0],-.045,.075),(.135,.25,.15),'Boot','Shin'+side,.025)
        cube('Boot sole',(ankle[0],-.052,.028),(.142,.263,.032),'Seam','Shin'+side,.008)
    # Optional details carry identity without high-frequency noise.
    if index==0:
        cube('Shoulder repair',(-.22,0,1.38),(.1,.16,.05),'Red','Chest',.008)
    if index==2:
        for k in range(3):cube('Repair tape',(.13,-.16,1.22-k*.032),(.11,.008,.018),'Patch','Chest')
    # Actual armature; procedural runtime poses use these stable bone names.
    armdata=bpy.data.armatures.new(name+'Rig');arm=bpy.data.objects.new(name+'Rig',armdata);bpy.context.collection.objects.link(arm)
    bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
    defs=[('Hips',(0,0,.9),None),('Chest',(0,0,1.04),'Hips'),('Head',(0,0,1.46),'Chest'),('Jaw',(0,-.08,z-.1),'Head'),('Lid',(0,0,z+.008),'Head')]
    for sign in [-1,1]:
        side='L' if sign<0 else 'R';sx=sign*.244*width
        defs.extend([('UpperArm'+side,(sx,0,1.345),'Chest'),('Forearm'+side,(sx+sign*.046,-.004,1.04),'UpperArm'+side),('Thigh'+side,(sign*.098,0,.84),'Hips'),('Shin'+side,(sign*.113,0,.48),'Thigh'+side)])
    for bn,pos,parent in defs:
        b=armdata.edit_bones.new(bn);b.head=pos;b.tail=Vector(pos)+Vector((0,0,.09))
        if parent:b.parent=armdata.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT');bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();skin=parts[0];skin.name=name+'_Mesh'
    mod=skin.modifiers.new('Armature','ARMATURE');mod.object=arm;skin.parent=arm
    bpy.context.view_layer.objects.active=skin
    tri=skin.modifiers.new('Triangulate','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
    arm.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
    tris=len(skin.data.polygons)
    bpy.context.view_layer.update()
    world_z=[(skin.matrix_world @ Vector(v)).z for v in skin.bound_box]
    height=round(max(world_z)-min(world_z),4)
    arm.location.x=index*1.1
    return {'id':name,'triangles':tris,'bones':len(armdata.bones),'height_m':height,'origin':'generated original geometry','rig':'rigid single-bone weights; no production smooth deformation'}

if __name__=='__main__':
    report=[make_character(i) for i in range(3)]
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Astra_Performers.blend'))
    (SOURCE/'character_manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps(report))

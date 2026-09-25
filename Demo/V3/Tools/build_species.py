"""Extend the V1 cast with original geometry, retaining its rigid planar construction."""
from pathlib import Path
import sys,json,math
sys.path.insert(0,str(Path(__file__).resolve().parent))
import build_characters as b
import bpy
from mathutils import Vector
cube,tube,ellipsoid,mesh,ring=b.cube,b.tube,b.ellipsoid,b.mesh,b.body_ring
for key,color in {'Alien':(.38,.47,.42),'AlienLight':(.56,.61,.49),'Orc':(.43,.43,.27),'OrcDark':(.25,.29,.19),'Crawler':(.40,.43,.33),'Plate':(.25,.30,.25),'Ivory':(.67,.64,.47),'BlackEye':(.035,.07,.055)}.items():
    b.M[key]=b.mat(key,color)

def finish(name,defs,index):
    ad=bpy.data.armatures.new(name+'Rig');arm=bpy.data.objects.new(name+'Rig',ad);bpy.context.collection.objects.link(arm)
    bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='EDIT')
    for bn,pos,parent in defs:
        bone=ad.edit_bones.new(bn);bone.head=pos;bone.tail=Vector(pos)+Vector((0,0,.09))
        if parent:bone.parent=ad.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT');bpy.ops.object.select_all(action='DESELECT')
    for part in b.parts:part.select_set(True)
    bpy.context.view_layer.objects.active=b.parts[0];bpy.ops.object.join();skin=b.parts[0];skin.name=name+'_Mesh'
    mod=skin.modifiers.new('Armature','ARMATURE');mod.object=arm;skin.parent=arm
    bpy.context.view_layer.objects.active=skin;tri=skin.modifiers.new('Triangulate','TRIANGULATE');bpy.ops.object.modifier_apply(modifier=tri.name)
    arm.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(b.OUT/(name+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
    zs=[(skin.matrix_world@Vector(v)).z for v in skin.bound_box]
    arm.location.x=index*1.45
    return {'id':name,'triangles':len(skin.data.polygons),'bones':len(ad.bones),'height_m':round(max(zs)-min(zs),3),'origin':'original V1-style geometry','rig':'named rigid bones; crawler uses four contact IK chains' if index==5 else 'V1 semantic pose rig'}

def badge(center,bone='Chest',width=.1):
    x,y,z=center;cube('Issued identity plate',center,(width,.012,.059),'Patch',bone,.002)
    for i in range(6):cube('Stamped barcode',(x-width*.39+i*width*.15,y-.009,z),(.006,.004,.034),'Seam',bone)

def standing(orc=False):
    b.parts=[];name='Ruk509' if orc else 'Xian318';index=4 if orc else 3
    skin='Orc' if orc else 'Alien';w=1.43 if orc else .88;top=1.40 if orc else 1.43;hz=1.61 if orc else 1.76
    ring('Work jacket',[(.86,.18*w,.13,0),(1.13,.20*w,.15,0),(top-.08,.235*w,.145,0),(top,.17*w,.11,0)],'Suit','Chest')
    ring('Trouser waist',[(.79,.17*w,.115,0),(.97,.18*w,.13,0)],'SuitDark','Hips')
    tube('Neck',(0,0,top-.01),(0,0,hz-.12),.085 if orc else .058,.082 if orc else .05,skin,'Head')
    ring('Pressure collar',[(top-.02,.115,.105,0),(top+.04,.111,.101,0)],'SuitDark','Chest',10)
    cube('Jacket seam',(0,-.153,1.17),(.017,.015,.42),'Seam','Chest')
    for s in [-1,1]:
        cube('Utility pocket',(s*.125*w,-.147,1.23),(.13,.035,.15),'SuitDark','Chest',.006)
        cube('Pocket binding',(s*.125*w,-.17,1.29),(.136,.016,.027),'Patch','Chest')
    badge((-.124*w,-.181,1.22));cube('Safety seal',(.13*w,-.173,1.16),(.032,.014,.10),'Red','Chest')
    if orc:
        ring('Wide faceted skull',[(hz-.19,.11,.10,-.03),(hz-.11,.168,.135,-.025),(hz+.04,.15,.13,0),(hz+.12,.13,.115,.02),(hz+.155,.085,.073,.02)],skin,'Head',10)
        cube('Heavy lower jaw',(0,-.08,hz-.15),(.29,.21,.115),skin,'Jaw',.025)
        cube('Muzzle',(0,-.138,hz-.044),(.152,.074,.075),'OrcDark','Head',.013)
        cube('Mouth line',(0,-.189,hz-.10),(.19,.013,.015),'Seam','Head')
        for s in [-1,1]:
            tube('Lower tusk',(s*.102,-.191,hz-.148),(s*.103,-.216,hz-.046),.023,.006,'Ivory','Jaw',5)
            mesh('Angular ear',[(s*.136,.014,hz+.02),(s*.265,.02,hz+.104),(s*.208,-.02,hz-.049),(s*.16,-.03,hz-.035)],[(0,1,2),(0,2,3),(3,2,1),(3,1,0)],skin,'Head')
            cube('Deep eye socket',(s*.073,-.128,hz+.014),(.102,.03,.041),'OrcDark','Head',.006)
            cube('Eye',(s*.073,-.147,hz+.012),(.063,.015,.017),'Ivory','Head')
            cube('Pupil',(s*.073,-.157,hz+.012),(.013,.006,.019),'BlackEye','Head')
            cube('Eyelid',(s*.073,-.162,hz+.012),(.068,.008,.022),skin,'Lid')
            brow=cube('Bony eyebrow',(s*.073,-.145,hz+.055),(.125,.049,.034),'OrcDark','Head',.007);brow.rotation_euler.y=s*.15
        for z in [1.30,1.35]:cube('Shoulder seam',(-.31,0,z),(.19,.23,.024),'Patch','Chest')
        cube('Shoulder repair',(.32,0,1.35),(.19,.26,.06),'Red','Chest',.01)
    else:
        ring('Elongated cranium',[(hz-.20,.071,.076,-.012),(hz-.10,.105,.097,-.008),(hz+.06,.133,.112,.007),(hz+.18,.12,.108,.028),(hz+.235,.065,.075,.03)],skin,'Head',10)
        for s in [-1,1]:
            eye=ellipsoid('Oblique black eye',(s*.066,-.093,hz+.018),(.047,.022,.073),'BlackEye');eye.rotation_euler.y=s*.28
            lid=ellipsoid('Blink membrane',(s*.066,-.111,hz+.018),(.05,.01,.075),skin,'Lid');lid.rotation_euler.y=s*.28
            tube('Temporal ridge',(s*.108,.01,hz+.09),(s*.135,.034,hz+.25),.027,.005,'AlienLight','Head',5)
            tube('Neck gill',(s*.067,-.023,1.51),(s*.073,-.028,1.61),.012,.01,'Plate','Head',5)
        cube('Breathing slit',(0,-.083,hz-.115),(.047,.015,.015),'BlackEye','Head')
        cube('Lower mouth seal',(0,-.094,hz-.12),(.05,.009,.014),skin,'Jaw')
        cube('Throat translator',(0,-.104,1.48),(.063,.035,.038),'Metal','Chest',.004)
    defs=[('Hips',(0,0,.9),None),('Chest',(0,0,1.04),'Hips'),('Head',(0,0,top+.01),'Chest'),('Jaw',(0,-.08,hz-.12),'Head'),('Lid',(0,0,hz+.012),'Head')]
    for s in [-1,1]:
        side='L' if s<0 else 'R';sx=s*.245*w
        shoulder=(sx,0,top-.045);elbow=(sx+s*.04,-.004,1.04);wrist=(sx+s*.047,-.035,.78)
        tube('Upper sleeve',shoulder,elbow,.093 if orc else .07,.07 if orc else .05,'Suit','UpperArm'+side)
        tube('Lower sleeve',elbow,wrist,.073 if orc else .052,.047 if orc else .036,'Suit','Forearm'+side)
        tube('Cuff',(wrist[0],wrist[1],.85),wrist,.052 if orc else .041,.05 if orc else .04,'SuitDark','Forearm'+side)
        ellipsoid('Mitten hand',(wrist[0],-.04,.735),(.054 if orc else .036,.048,.065),skin,'Forearm'+side,8,4)
        for i in range(3):tube('Blunt fingers',(wrist[0]-.026+i*.025,-.065,.71),(wrist[0]-.026+i*.025,-.071,.665),.012,.009,skin,'Forearm'+side,5)
        hip=(s*.108*w,0,.86);knee=(s*.116*w,0,.47);ankle=(s*.123*w,-.005,.12)
        tube('Trouser thigh',hip,knee,.115 if orc else .086,.08 if orc else .063,'SuitDark','Thigh'+side)
        tube('Trouser shin',knee,ankle,.083 if orc else .065,.06 if orc else .047,'SuitDark','Shin'+side)
        cube('Knee patch',(knee[0],-.069,.47),(.13,.035,.16),'Suit','Shin'+side,.01)
        cube('Boot',(ankle[0],-.045,.071),(.158 if orc else .125,.27,.14),'Boot','Shin'+side,.019)
        defs.extend([('UpperArm'+side,shoulder,'Chest'),('Forearm'+side,elbow,'UpperArm'+side),('Thigh'+side,hip,'Hips'),('Shin'+side,knee,'Thigh'+side)])
    return finish(name,defs,index)

def crawler():
    b.parts=[]
    ellipsoid('Horizontal thorax',(0,-.06,.64),(.29,.52,.18),'Crawler','Chest',10,5)
    ellipsoid('Hind thorax',(0,.36,.57),(.25,.31,.18),'Crawler','Hips',10,5)
    for y in [-.26,-.07,.12,.31]:cube('Dorsal armor',(0,y,.78 if y<.2 else .71),(.43,.155,.105),'Plate','Chest' if y<.2 else 'Hips',.033)
    cube('Salvaged torso harness',(0,-.05,.802),(.085,.70,.04),'Patch','Chest')
    cube('Receiver box',(.21,.05,.70),(.14,.21,.13),'SuitDark','Chest',.015)
    badge((.218,-.064,.70),'Chest',.096)
    tube('Neck',(0,-.43,.67),(0,-.63,.71),.14,.13,'Crawler','Head')
    ellipsoid('Forward skull',(0,-.74,.74),(.20,.27,.15),'Crawler','Head',10,5)
    cube('Forehead crest',(0,-.70,.88),(.13,.30,.04),'Plate','Head',.013)
    for s in [-1,1]:
        for y,z,rx in [(-.91,.78,.047),(-.79,.825,.026)]:
            x=s*(.09 if y<-.85 else .16)
            ellipsoid('Four dark eyes',(x,y,z),(rx,.026,rx*.63),'BlackEye','Head',8,4)
            ellipsoid('Eye membrane',(x,y-.022,z),(rx*1.07,.007,rx*.67),'Crawler','Lid',8,4)
    cube('Mouth opening',(0,-.93,.673),(.18,.035,.022),'BlackEye','Head',.005)
    cube('Lower mandible',(0,-.90,.64),(.195,.135,.047),'Plate','Jaw',.008)
    for s in [-1,1]:tube('Mouth feeler',(s*.14,-.91,.66),(s*.18,-1.05,.62),.014,.004,'Ivory','Head',5)
    defs=[('Hips',(0,.31,.60),None),('Chest',(0,-.05,.64),'Hips'),('Head',(0,-.49,.69),'Chest'),('Jaw',(0,-.88,.65),'Head'),('Lid',(0,-.85,.78),'Head')]
    for front in [True,False]:
        for s in [-1,1]:
            side='L' if s<0 else 'R';upper=('UpperArm' if front else 'Thigh')+side;lower=('Forearm' if front else 'Shin')+side;end=('Hand' if front else 'Foot')+side
            a=(s*.23,-.36 if front else .34,.65 if front else .59)
            knee=(s*.45,-.20 if front else .63,.33)
            foot=(s*.43,-.65 if front else .48,.055)
            tube('Upper walking limb',a,knee,.082,.06,'Crawler',upper,7)
            ellipsoid('Armored joint',knee,(.071,.078,.078),'Plate',lower,8,4)
            tube('Lower walking limb',knee,foot,.059,.035,'Crawler',lower,7)
            ellipsoid('Weight-bearing palm',(foot[0],foot[1]-.045,.044),(.072,.116,.042),'Plate',end,8,4)
            for i in range(3):tube('Walking claw',(foot[0]-.041+i*.041,foot[1]-.10,.038),(foot[0]-.044+i*.044,foot[1]-.19,.018),.015,.005,'Ivory',end,5)
            defs.extend([(upper,a,'Chest' if front else 'Hips'),(lower,knee,upper),(end,foot,lower)])
    tube('Tail root',(0,.55,.57),(0,.88,.40),.135,.073,'Crawler','Tail1',8)
    tube('Tail tip',(0,.88,.40),(0,1.23,.22),.073,.014,'Plate','Tail2',7)
    defs.extend([('Tail1',(0,.55,.57),'Hips'),('Tail2',(0,.88,.40),'Tail1')])
    return finish('Fu640',defs,5)

report=[b.make_character(i) for i in range(3)]+[standing(),standing(True),crawler()]
bpy.ops.wm.save_as_mainfile(filepath=str(b.SOURCE/'Astra_Six_Species.blend'))
(b.SOURCE/'character_manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report))

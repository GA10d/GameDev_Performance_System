"""Quaternius CC0 source -> a single shared-skeleton modular character + bounded shape keys.

No runtime bone scaling and no runtime face primitives. Rebuild only this generated asset.
"""
import bpy,bmesh,json,math,pathlib
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'UnityProject/Assets/AstraToolV2/Models/ModularCast.fbx'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Source/Downloads/Humans_Master_Humanoid.blend'))
rig=bpy.data.objects['CharacterArmature'];rig.animation_data_clear()
source={o.name:o for o in bpy.data.objects if o.type=='MESH'}
made=[]

def mat(name,color):
 m=bpy.data.materials.new('Q_'+name);m.diffuse_color=(*color,1);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.9
 return m
M={n:mat(n,c) for n,c in {'Skin':(.57,.40,.29),'SkinShade':(.44,.30,.22),'Hair':(.065,.052,.043),'Brow':(.065,.052,.043),'Eye':(.015,.025,.022),'Suit':(.34,.39,.32),'Dark':(.075,.095,.084),'Trim':(.48,.39,.23),'Ivory':(.68,.65,.52),'Accent':(.43,.53,.38)}.items()}

def duplicate(name,out,keep=None):
 o=source[name].copy();o.data=source[name].data.copy();bpy.context.collection.objects.link(o);o.name=out
 o.hide_set(False);o.hide_viewport=False;o.hide_render=False;o.animation_data_clear()
 # FBX cannot evaluate Mirror after shape keys. Bake source symmetry BEFORE sculpt keys.
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 for mod in list(o.modifiers):
  if mod.type=='MIRROR':bpy.ops.object.modifier_apply(modifier=mod.name)
  elif mod.type=='NODES':o.modifiers.remove(mod)
 if keep is not None:
  bm=bmesh.new();bm.from_mesh(o.data)
  bad=[f for f in bm.faces if o.data.materials[f.material_index].name not in keep]
  bmesh.ops.delete(bm,geom=bad,context='FACES')
  bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
  bm.to_mesh(o.data);bm.free()
 for slot in o.material_slots:
  n=slot.material.name
  key='SkinShade' if n=='Skin_Darker' else 'Skin' if n.startswith('Skin') else 'Brow' if n in ['Eyebrows','Moustache'] else 'Eye' if n in ['Eye','Visor'] else 'Hair' if n.startswith('Hair') else 'Trim' if n in ['Worker_Yellow','Gold','Earrings','Metal','Metal_Dark','SciFi_Light_Accent'] else 'Dark' if n in ['Black','Grey','Brown2','SciFi_MainDark','Swat_Black','DarkBrown'] else 'Ivory' if n in ['White','Beige','SciFi_Light'] else 'Suit'
  slot.material=M[key]
 for mod in o.modifiers:
  if mod.type=='ARMATURE':mod.object=rig
 o.parent=rig;made.append(o);return o

def bind(o,name,material,bone='Head'):
 o.name=name;bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 o.data.materials.clear();o.data.materials.append(M[material]);o.parent=rig
 g=o.vertex_groups.new(name=bone);g.add(list(range(len(o.data.vertices))),1,'REPLACE')
 o.modifiers.new('Quaternius skin','ARMATURE').object=rig
 made.append(o);return o
def orb(name,loc,scale,material='Skin',bone='Head',segments=12,rings=8):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=loc)
 o=bpy.context.object;o.scale=scale;return bind(o,name,material,bone)
def box(name,loc,scale,material='Dark',bone='Head'):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.scale=scale;return bind(o,name,material,bone)
def cone(name,start,end,radius,material='Skin',bone='Head'):
 a,b=Vector(start),Vector(end);d=b-a
 bpy.ops.mesh.primitive_cone_add(vertices=8,radius1=radius,radius2=.002,depth=d.length,location=(a+b)/2)
 o=bpy.context.object;o.rotation_euler=d.to_track_quat('Z','Y').to_euler();return bind(o,name,material,bone)
def join(objects,name):
 if len(objects)==1:objects[0].name=name;return objects[0]
 survivors=[m for m in made if m not in objects[1:]]
 bpy.ops.object.select_all(action='DESELECT')
 for o in objects:o.select_set(True)
 bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join()
 o=bpy.context.object;o.name=name;made[:]=survivors
 return o

exec(compile((ROOT/'Tools/face_geometry.py').read_text(encoding='utf-8'),'face_geometry.py','exec'))

human=duplicate('Casual_Head','Head_Human',{'Skin','Eye','Eyebrows'})
human=join([human,orb('Human_mouth',(0,-.161,1.637),(.028,.003,.0035),'Eye',segments=8,rings=4)],'Head_Human')
alien=duplicate('Casual2_Head','Head_Alien',{'Skin','Skin_Darker'})
for v in alien.data.vertices:
 z=v.co.z;t=max(0,min(1,(z-1.60)/.24));v.co.x*=1+.65*t;v.co.y*=1+.18*t;v.co.z=1.51+(z-1.51)*1.28
exec(compile((ROOT/'Tools/fuse_gray_head.py').read_text(encoding='utf-8'),'fuse_gray_head.py','exec'))
geometry_mouth(alien,.033,1.642)
parts=[alien]
gray_skin=BVHTree.FromPolygons([v.co for v in alien.data.vertices],[list(p.vertices) for p in alien.data.polygons])
for s in [-1,1]:
 parts.append(orb('Oblique_eye',(.080*s,-.190,1.765),(.052,.033,.069),'Eye'))
 parts.append(orb('Eye_glint',(.070*s,-.221,1.791),(.007,.005,.010),'Ivory',segments=8,rings=4))
 brow_hit,_,_,_=gray_skin.ray_cast(Vector((.082*s,-.5,1.835)),Vector((0,1,0)),1)
 assert brow_hit is not None,'Gray brow lost skin contact'
 parts.append(orb('Brow_ridge',(.082*s,brow_hit.y-.008,1.835),(.045,.014,.012),'Brow',segments=8,rings=4))
alien=join(parts,'Head_Alien')

# Source hair/headgear remains a real skinned module from the author, separate from face skin.
hair_sources=[('Casual_Head',{'Hair'}),('Casual2_Head',{'Hair'}),('Adventurer_Head',{'Hair'}),('Beach_Head',{'Hair'}),('Suit_Head',{'Hair'}),('King_Head',{'Hair_White'}),('Punk_Head',{'Red','Red_Dark'}),('Farmer_Head',{'Beige','Red'}),('Worker_Head',{'Worker_Yellow'}),('Casual_Head',{'Hair'}),('SpaceSuit_Head',None)]
for i,(name,keep) in enumerate(hair_sources,1):
 if i==10:continue # Rebuilt below from the scalp; compressing the old hair penetrated the skull.
 o=duplicate(name,f'Hair_Human_{i:02d}',keep)
 if i in [7,10]:
  for slot in o.material_slots:slot.material=M['Hair']

# Scalp-derived hair has exactly the same surface topology and bounded deformation field
# as the head. Keep a positive shell clearance instead of shrinking a coiffure into skin.
def scalp(name,thickness=.006,relief=None):
 o=duplicate('Casual_Head',name,{'Skin'})
 bm=bmesh.new();bm.from_mesh(o.data)
 bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
  dist=1e-6,plane_co=(0,0,1.707),plane_no=(0,.28,1),clear_inner=True)
 bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
 bm.to_mesh(o.data);bm.free()
 center=Vector((0,-.046,1.701))
 for v in o.data.vertices:
  p=v.co.copy();direction=(p-center).normalized();v.co+=direction*thickness
  if relief:v.co+=Vector(relief(p))
 # Thin folded rim removes the open edge when looking up at the hairline.
 bm=bmesh.new();bm.from_mesh(o.data);edges=[e for e in bm.edges if e.is_boundary]
 rim=bmesh.ops.extrude_edge_only(bm,edges=edges)
 for v in [v for v in rim['geom'] if isinstance(v,bmesh.types.BMVert)]:v.co-=(v.co-center).normalized()*(thickness+.002)
 bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
 o.data.materials.clear();o.data.materials.append(M['Hair'])
 for p in o.data.polygons:p.material_index=0;p.use_smooth=False
 o.vertex_groups.clear();g=o.vertex_groups.new(name='Head');g.add(list(range(len(o.data.vertices))),1,'REPLACE')
 return o

def lock(name,points,radii,flatten=1):
 # A tapered, faceted lock rather than intersecting unweighted runtime primitives.
 vs=[];fs=[];sides=6
 for i,point in enumerate(points):
  p=Vector(point);direction=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])
  tangent=direction.normalized();u=tangent.cross(Vector((0,1,0)))
  if u.length<.01:u=tangent.cross(Vector((1,0,0)))
  u.normalize();v=tangent.cross(u).normalized()
  for k in range(sides):
   angle=math.tau*k/sides;vs.append(p+radii[i]*(u*math.cos(angle)+v*math.sin(angle)*flatten))
 for i in range(len(points)-1):
  for k in range(sides):a=i*sides+k;b=i*sides+(k+1)%sides;fs.append((a,b,b+sides,a+sides))
 fs.extend([tuple(reversed(range(sides))),tuple(range((len(points)-1)*sides,len(points)*sides))])
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(vs,[],fs);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 return bind(o,name,'Hair')

scalp('Hair_Human_10')
head_tree=BVHTree.FromPolygons([v.co for v in human.data.vertices],
 [list(p.vertices) for p in human.data.polygons if human.data.materials[p.material_index]==M['Skin']])
# The authored punk scalp was coplanar with the shared human head. Keep its actual
# hair surface outside the scalp before both receive the same bounded face shapes.
punk=bpy.data.objects['Hair_Human_07'];hair_center=Vector((0,-.046,1.701))
for vertex in punk.data.vertices:
 ray=vertex.co-hair_center
 if ray.length<1e-6:continue
 hit,normal,index,distance=head_tree.ray_cast(hair_center,ray.normalized(),1)
 if hit is not None and ray.length<distance+.006:vertex.co=hair_center+ray.normalized()*(distance+.006)
# Keep the author's tall crest, and replace its entire old scalp/sideburn region.
# Per-face clearance filtering left fragments on the forehead because some large
# source triangles crossed the skin. A single seam lies inside the new foundation.
bm=bmesh.new();bm.from_mesh(punk.data)
bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
 dist=1e-6,plane_co=(0,0,1.816),plane_no=(0,0,1),clear_inner=True)
bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
bm.to_mesh(punk.data);bm.free()
join([punk,scalp('Mohawk short foundation',.010)],'Hair_Human_07')
def authored_hair(style):
 name='Beach_Head' if style==15 else 'Suit_Head' if style in [16,17,18,23,24,25,26,27] else 'Casual_Head'
 o=duplicate(name,'Authored hair locks',{'Hair'})
 center=Vector((0,-.046,1.701))
 for v in o.data.vertices:
  p=v.co.copy();x,y,z=p;top=max(0,min(1,(z-1.74)/.09));front=max(0,min(1,(-y-.075)/.08))
  if style==14:p.z-=.025*top;p.x+=.006*math.sin(y*90)*top
  if style==15:p.z-=.013*top;p.y-=.008*front*top
  if style==16:p.y+=.032*top;p.z+=.012*top
  if style in [17,18]:
   p.x+=.014*top;p.z+=.008*top
   if style==18:p.x=-p.x
  if style==19:p.x+=math.copysign(.018*top,x);p.z+=.006*top
  if style in [20,22]:p.z-=.026*front;p.y-=.006*front
  if style==21:p.z-=.040*front*max(0,min(1,(x+.10)/.20));p.y-=.009*front
  if style in [24,25,26,27]:p.y+=.020*top;p.z-=.006*top
  # Keep reshaped authored strands on the outside of the head. The continuous
  # foundation underneath remains the guarantee of coverage between strands.
  if p.z>1.721:
   ray=p-center;hit,normal,index,dist=head_tree.ray_cast(center,ray.normalized(),.5)
   if hit is not None and ray.length<dist+.005:p=center+ray.normalized()*(dist+.005)
  v.co=p
 if style==18:
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
 return o

for i in range(12,30):
 def relief(p):
  x,y,z=p;top=max(0,min(1,(z-1.75)/.06));dx=dy=dz=0
  if i==12:dz=.009*top
  if i==13:dz=max(0,1.855-z)*top
  if i in [14,15]:dz=.015*top
  if i==16:dy=.008*top;dz=.010*top
  if i in [17,18]:dz=.007*top
  if i==19:dz=.008*top
  if i in [20,21,22,23]:dz=.012*top
  return dx,dy,dz
 parts=[scalp('Hair foundation',.009 if i!=12 else .008,relief)]
 if i in range(14,28):parts.append(authored_hair(i))

 if i in [24,25]:
  high=i==25;z=1.824 if high else 1.72
  parts.append(orb('Hair tie',(0,.079,z),(.027,.023,.026),'Dark'))
  parts.append(lock('Ponytail',[(0,.085,z),(0,.137,z+.012),(0,.168,z-.052),(0,.156,z-.129)],[.024,.030,.023,.011],.85))
 if i==26:parts.append(orb('Coiled bun',(0,.045,1.831),(.060,.055,.047),'Hair',segments=10,rings=6))
 if i==27:
  for s in [-1,1]:parts.append(orb('Side bun',(s*.083,.006,1.811),(.045,.048,.043),'Hair',segments=10,rings=6))
 if i==28:
  for row in range(3):
   count=[10,8,5][row]
   for k in range(count):
    a=math.tau*k/count;radius=[.086,.064,.027][row]
    parts.append(orb('Curl',(math.sin(a)*radius,-.047+math.cos(a)*radius*1.24,[1.778,1.822,1.85][row]),(.027,.03,.03),'Hair',segments=8,rings=4))
 if i==29:
  for k,y in enumerate([-.122,-.071,-.015,.037]):parts.append(lock('Wide crest',[(0,y,1.79),(0,y-.004,1.89),(0,y+.002,1.905)],[.042,.032,.022],.65))
 join(parts,f'Hair_Human_{i:02d}')

exec((ROOT/'Tools/import_ubc_hair.py').read_text(encoding='utf-8'))

exec((ROOT/'Tools/alien_design.py').read_text(encoding='utf-8'))

outfits=[('SpaceSuit','SpaceSuit','SpaceSuit'),('Worker','Worker','Worker'),('Farmer','Farmer_Pants','Farmer'),('Casual','Casual','Casual'),('Casual2','Casual2','Casual2'),('Suit','Suit','Suit'),('Swat','Swat','Swat'),('Adventurer','Adventurer','Adventurer'),('Punk','Punk','Punk'),('King','King','King')]
for i,(body,legs,feet) in enumerate(outfits):
 duplicate(body+'_Body',f'Body_{i:02d}')
 duplicate(legs if legs.endswith('Pants') else legs+'_Legs',f'Legs_{i:02d}')
 duplicate(feet+'_Feet',f'Feet_{i:02d}')

# Append new wardrobe IDs without touching the saved original IDs 00..09.
exec(compile((ROOT/'Tools/space_outfits.py').read_text(encoding='utf-8'),'space_outfits.py','exec'))
exec(compile((ROOT/'Tools/import_women.py').read_text(encoding='utf-8'),'import_women.py','exec'))

# Species-specific accessories share the face deformation fields, so a wide head cannot detach them.
for species in ['Human','Alien','Female']:
 ay=.067 if species=='Alien' else .0;eyeZ=1.697+ay;front=-.231 if species=='Alien' else -.181;space=.080 if species=='Alien' else .043
 for i in range(1,10):
  if i in [4,5,7,8]:continue # Fitted masks and compact headsets are built below.
  p=[]
  if i in [1,2,6]:
   sides=[-1] if i==6 else [-1,1]
   for s in sides:
    for edge in [-1,1]:
     p.append(box('Lens_rim',(s*space,front,eyeZ+edge*.03),(.070,.009,.006),'Trim'))
     p.append(box('Lens_side',(s*space+edge*.035,front,eyeZ),(.006,.009,.06),'Trim'))
   if i!=6:p.append(box('Bridge',(0,front,eyeZ),(.03,.009,.006),'Trim'))
  if i==2:p.append(box('Visor_bar',(0,front+.005,eyeZ+.042),(.18,.02,.016),'Accent'))
  if i==3:
   p.extend([box('Lamp_band',(0,-.137,1.787+ay),(.19,.022,.018),'Dark'),box('Lamp',(0,-.160,1.788+ay),(.039,.020,.030),'Ivory')])
  if i==9:p.append(orb('Collar',(0,-.025,1.532),(.071,.073,.025),'Dark'))
  join(p,f'FaceAcc_{species}_{i:02d}')

for i in range(1,10):
 p=[]
 if i in [1,2,7]:
  p.append(box('ID',(-.09,-.162,1.39),(.09,.014,.06),'Ivory','Chest'))
  for j in range(3):p.append(box('Bar',(-.112+j*.018,-.171,1.39),(.006,.006,.04),'Dark','Chest'))
 if i in [2,3,8]:
  p.append(box('Radio',(.125,-.151,1.33),(.067,.046,.12),'Dark','Chest'))
  p.append(box('Indicator',(.125,-.178,1.36),(.036,.008,.015),'Accent','Chest'))
 if i in [4,7]:
  for s in [-1,1]:p.append(box('Harness',(.10*s,-.146,1.30),(.027,.025,.28),'Trim','Chest'))
 if i in [5,8]:p.append(box('Pack',(0,.154,1.28),(.24,.10,.30),'Dark','Chest'))
 if i==6:p.append(box('Tool_pouch',(.19,-.105,1.12),(.08,.08,.13),'Trim','Hips'))
 if i==9:
  for s in [-1,1]:p.append(orb('Shoulder_pad',(.23*s,.015,1.45),(.065,.085,.04),'Dark','Chest'))
 join(p,f'Gear_{i:02d}')

def clamp(x):return max(0,min(1,x))
def head_warp(v,kind,jaw_offset=0):
 local=v.copy();local.z-=jaw_offset
 p=chin_warp(local,kind);p.z+=jaw_offset
 x,y,z=p;w=clamp((z-1.54)/.13);low=clamp((1.74-z)/.16)
 if kind.startswith('Face'):
  i=int(kind[4:]);wx=[0,.07,.08,-.07,.04,.06,-.06,-.025,.04,-.03][i];hz=[0,-.025,0,.025,.018,-.02,.015,.06,-.012,.03][i]
  p.x*=1+wx*w*(.5+low*.5);p.z+=hz*(z-1.54)*w
  if i==2:p.x+=math.copysign(.004*low*w,x)
  if i==4:p.y-=.007*w*(1-low)
 elif kind in ['WidthWide','WidthNarrow']:p.x*=1+(.08 if kind=='WidthWide' else -.08)*w
 elif kind in ['JawWide','JawNarrow']:p.x*=1+(.10 if kind=='JawWide' else -.10)*low*w
 return p

def body_warp(v,i):
 p=v.copy();x,y,z=p;torso=clamp((z-.88)/.15)*clamp((1.53-z)/.12)*clamp((.48-abs(x))/.20);leg=clamp((.98-z)/.18)*clamp((z-.13)/.20)
 pairs=[(0,0),(-.06,-.03),(.055,.025),(.075,.06),(-.05,0),(.07,.015),(.015,.06),(0,0)]
 a,b=pairs[i];p.x*=1+a*torso;p.y*=1+b*torso
 if i==7:p.x*=1+.045*leg;p.y*=1+.08*leg
 return p

def shape(o,name,fn):
 k=o.shape_key_add(name=name,from_mix=False)
 for i,v in enumerate(o.data.vertices):k.data[i].co=fn(v.co,i)

for o in made:
 if o.name not in bpy.data.objects:continue
 # Original pack hands are large. Apply the tested V2.1 hand/rest-bone correction once.
 anchors={s:rig.data.bones['Hand'+s].head_local.copy() for s in ['.L','.R']}
 for v in o.data.vertices:
  for side,anchor in anchors.items():
   names={'Hand'+side,'Fist'+side,'Wrist'+side}|{f+str(n)+side for f in ['Thumb','Index','Middle','Ring','Pinky'] for n in [1,2,3,4]}
   weight=min(1,sum(g.weight for g in v.groups if o.vertex_groups[g.group].name in names))
   if weight:v.co=anchor+(v.co-anchor)*(1-.22*weight)
 # Keep authored surface smoothing on skin; hard surfaces retain the source normals.
 if o.name.startswith('Head_'):
  for p in o.data.polygons:p.use_smooth=True
 o.shape_key_add(name='Basis',from_mix=False)
 ishead=o.name.startswith(('Head_','Hair_','FaceAcc_','Mark_'))
 if ishead:
  jaw_offset=.040 if o.name.startswith('Head_Alien_') else 0
  for key in ['Face%02d'%i for i in range(1,10)]+['WidthWide','WidthNarrow','JawWide','JawNarrow','ChinRound','ChinSquare']:shape(o,key,lambda p,i,k=key:head_warp(p,k,jaw_offset))
  if o.name.startswith('Hair_Alien_'):
   for j in range(1,10):shape(o,f'AlienFit{j:02d}',lambda p,idx,j=j:alien_fit(p,j))
 else:
  for j in range(1,8):shape(o,'Body%02d'%j,lambda p,i,j=j:body_warp(p,j))
 if o.name in alien_specs:
  alien_detail_shapes(o)
  continue
 if o.name.startswith('Head_'):
  # Material membership selects original authored eye/brow topology. No duplicate source eyes remain.
  eyeids={v for p in o.data.polygons if o.data.materials[p.material_index] in [M['Eye'],M['Ivory']] for v in p.vertices}
  browids={v for p in o.data.polygons if o.data.materials[p.material_index]==M['Brow'] for v in p.vertices}
  isalien=o.name=='Head_Alien';cz=1.765 if isalien else 1.697;cx=.08 if isalien else .043
  def eye_warp(p,idx,j):
   q=p.copy()
   if idx not in eyeids or p.z<cz-(.09 if isalien else .032):return q
   sign=1 if p.x>0 else -1;center=Vector((sign*cx,p.y,cz));d=q-center
   if j==1:d.z*=1.16
   if j==2:d.z*=.73
   if j==3:d.x*=1.12
   if j==4:d.x*=.85
   if j==5:d.z+=sign*d.x*.16
   if j==6:d.z-=sign*d.x*.16
   if j==7:center.x+=sign*.004
   return center+d
  for j in range(1,8):shape(o,'Eye%02d'%j,lambda p,i,j=j:eye_warp(p,i,j))
  def brow_warp(p,idx,j):
   q=p.copy()
   if idx in browids:
    if j==1:q.z+=.004
    if j==2:q.z-=.004
    if j==3:q.z+=((abs(p.x)-.035)*.18)
    if j==4:q.z-=((abs(p.x)-.035)*.18)
    if j==5:
     bz=1.835 if isalien else 1.709;q.z=bz+(q.z-bz)*1.3
   return q
  for j in range(1,6):shape(o,'Brow%02d'%j,lambda p,i,j=j:brow_warp(p,i,j))
  for j in range(1,6):
   def nose(p,idx,j=j):
    q=p.copy();w=math.exp(-((p.x/.030)**2+((p.z-(1.69 if not isalien else 1.71))/.032)**2))*clamp((-p.y-.125)/.025)
    if j==1:q.y-=.009*w
    if j==2:q.y+=.007*w
    if j==3:q.x*=1+.14*w
    if j==4:q.x*=1-.12*w
    if j==5:q.z+=.005*w
    return q
   shape(o,'Nose%02d'%j,nose)
  for j in range(1,6):
   def mouth(p,idx,j=j):
    if o.name in ['Head_Alien','Head_Female']:return mouth_warp(p,j,1.642 if isalien else 1.646,.033 if isalien else .028)
    q=p.copy();w=math.exp(-((p.x/.060)**2+((p.z-1.633)/.018)**2))*clamp((-p.y-.12)/.035)
    if j==1:q.x*=1+.1*w
    if j==2:q.x*=1-.1*w
    if j==3:q.z+=.004*w*abs(p.x)/.05
    if j==4:q.z-=.004*w*abs(p.x)/.05
    if j==5:q.y-=.004*w
    return q
   shape(o,'Mouth%02d'%j,mouth)

(ROOT/'QA/ChinHair').mkdir(parents=True,exist_ok=True)
(ROOT/'QA/ChinHair/mouth-topology.json').write_text(json.dumps(mouth_surface_report,indent=2),encoding='utf-8')

# Build fitted paint and masks after all head keys exist, preserving their surface binding.
exec(compile((ROOT/'Tools/face_details.py').read_text(encoding='utf-8'),'face_details.py','exec'))

# Regress the actual short-crop defect: cast through the whole crown from inside the
# head, including additive face/width/jaw extremes, and require hair outside skin.
def evaluated_vertices(o,keys):
 basis=o.data.shape_keys.key_blocks['Basis']
 return [v.co+sum((o.data.shape_keys.key_blocks[k].data[v.index].co-basis.data[v.index].co for k in keys),Vector()) for v in o.data.vertices]
crop=bpy.data.objects['Hair_Human_10'];clearances=[];samples=0
skin_faces=[p for p in human.data.polygons if human.data.materials[p.material_index]==M['Skin']
 and all(human.data.vertices[i].co.z+.28*human.data.vertices[i].co.y>1.721 for i in p.vertices)]
for face in range(10):
 for extreme in ['', 'Wide', 'Narrow']:
  keys=([f'Face{face:02d}'] if face else [])+(['Width'+extreme,'Jaw'+extreme] if extreme else [])
  headverts=evaluated_vertices(human,keys);hairverts=evaluated_vertices(crop,keys)
  tree=BVHTree.FromPolygons(hairverts,[list(p.vertices) for p in crop.data.polygons],all_triangles=False)
  for p in skin_faces:
   # Triangle centroids sample interior coverage, not just silhouette vertices.
   for k in range(1,len(p.vertices)-1):
    point=(headverts[p.vertices[0]]+headverts[p.vertices[k]]+headverts[p.vertices[k+1]])/3
    origin=Vector((0,-.046,1.701));ray=point-origin
    hit,normal,index,distance=tree.ray_cast(origin,ray.normalized(),1)
    assert hit is not None,('Uncovered scalp',keys,point)
    clearance=distance-ray.length;assert clearance>.001,('Hair penetrates scalp',keys,clearance,point)
    clearances.append(clearance);samples+=1
qa=ROOT/'QA';qa.mkdir(exist_ok=True)
(qa/'hair-scalp-verification.json').write_text(json.dumps({'cases':30,'raySamples':samples,'minimumClearanceMeters':min(clearances),'status':'PASS','legacyCropId':10,'humanHairOptions':33,'newStyles':21},indent=2),encoding='utf-8')

# Snapshot/disconnect before touching connected bones: avoids the V2.0 finger regression.
bpy.context.view_layer.objects.active=rig;bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
rest={b.name:(b.head.copy(),b.tail.copy(),b.roll,b.use_connect) for b in rig.data.edit_bones}
for b in rig.data.edit_bones:b.use_connect=False
for b in rig.data.edit_bones:
 start,end,roll,connected=rest[b.name]
 for side,anchor in anchors.items():
  if b.name.endswith(side) and b.name.startswith(('Hand','Thumb','Index','Middle','Ring','Pinky')):start=anchor+(start-anchor)*.78;end=anchor+(end-anchor)*.78
 b.head=start;b.tail=end;b.roll=roll
for side in ['.L','.R']:
 old=rig.data.edit_bones['Thumb3'+side];end=old.tail.copy();old.tail=old.head.lerp(end,.6)
 tip=rig.data.edit_bones.new('Thumb4'+side);tip.head=old.tail;tip.tail=end;tip.parent=old;tip.use_connect=True
for b in rig.data.edit_bones:
 if b.name in rest and rest[b.name][3] and b.parent and (b.head-b.parent.tail).length<1e-5:b.use_connect=True
 if b.name.startswith(('Index','Middle','Ring','Pinky')):
  assert b.length>.012 and (b.tail.x-b.head.x)*(1 if b.name.endswith('.L') else -1)>0,b.name
bpy.ops.object.mode_set(mode='OBJECT')
for o in list(bpy.data.objects):
 if o!=rig and o not in made:bpy.data.objects.remove(o,do_unlink=True)
rig.hide_set(False);rig.hide_viewport=False;rig.hide_render=False
for o in made:
 o.hide_set(False);o.hide_render=False;o.hide_viewport=False
 for v in o.data.vertices:assert len(v.groups)>0,(o.name,v.index,'unweighted')
 if o.name.startswith(('Legs_','Feet_')):
  assert min(v.co.x for v in o.data.vertices)<-.04 and max(v.co.x for v in o.data.vertices)>.04,(o.name,'missing mirrored limb')
bpy.ops.object.select_all(action='SELECT');bpy.context.view_layer.objects.active=rig
OUT.parent.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(OUT),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',use_armature_deform_only=False,use_mesh_modifiers=False)
manifest={'source':'Quaternius Ultimate Modular Men + Ultimate Modular Women + Universal Base Characters / CC0; ASTRA derivatives','rigBones':len(rig.data.bones),'modules':[]}
for o in made:
 manifest['modules'].append({'name':o.name,'vertices':len(o.data.vertices),'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'shapes':[k.name for k in o.data.shape_keys.key_blocks]})
 visible=o.name in ['Head_Human','Hair_Human_01','Body_00','Legs_00','Feet_00']
 o.hide_set(not visible);o.hide_render=not visible
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Source/Quaternius_Astra_Modular.blend'))
(ROOT/'Source/modular_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('MODULAR_EXPORT_OK',len(made),'modules',len(rig.data.bones),'bones')

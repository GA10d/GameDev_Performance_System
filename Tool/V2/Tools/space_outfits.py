"""Append-only CC0 Quaternius wardrobe derivatives. Executed by build_modular.py.

All additions are joined to the three skinned modules, use the existing bones,
and receive the same bounded Body shape keys in the parent build script.
Coordinates: metres, T pose, front = -Y. No commercial game meshes are used.
"""
from mathutils.kdtree import KDTree

SPACE_OUTFITS=[
 (10,'舱外加压服','SpaceSuit','SpaceSuit','SpaceSuit'),
 (11,'轨道驾驶服','SpaceSuit','Suit','Suit'),
 (12,'星表测绘服','Adventurer','Worker','Worker'),
 (13,'采矿动力服','SpaceSuit','Swat','Worker'),
 (14,'废船拆解服','Worker','Worker','Worker'),
 (15,'生化隔离服','SpaceSuit','SpaceSuit','SpaceSuit'),
 (16,'低温勘探服','SpaceSuit','SpaceSuit','SpaceSuit'),
 (17,'热区防护服','SpaceSuit','SpaceSuit','SpaceSuit'),
 (18,'舰队安保服','Swat','Swat','Swat'),
 (19,'轨道医疗服','Suit','Worker','Suit'),
 (20,'舰桥礼勤服','Suit','Suit','Suit'),
 (21,'远航行商服','King','Worker','Worker'),
]

def space_mesh(name,vertices,faces,material,bone):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
 o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bind(o,name,material,bone)
 bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
 return o

def space_panel(name,center,size,material='Suit',bone='Torso',cut=.2):
 # Extruded octagonal plate: silhouette, chamfered corners and real side walls.
 x,y,z=center;w,d,h=size;cx=w*cut;cz=h*cut
 outline=[(-w/2+cx,-h/2),(w/2-cx,-h/2),(w/2,-h/2+cz),(w/2,h/2-cz),
          (w/2-cx,h/2),(-w/2+cx,h/2),(-w/2,h/2-cz),(-w/2,-h/2+cz)]
 vs=[(x+u,y+t*d/2,z+v) for t in [-1,1] for u,v in outline]
 fs=[tuple(reversed(range(8))),tuple(range(8,16))]+[(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)]
 return space_mesh(name,vs,fs,material,bone)

def space_band(name,center,rx,ry,depth,material,bone,axis='Z',thickness=.012):
 # Hollow polygonal cuff, never a solid disk through the joint.
 vs=[];n=12
 for inner in [False,True]:
  for level in [-1,1]:
   for i in range(n):
    a=math.tau*i/n;u=(rx-(thickness if inner else 0))*math.cos(a);v=(ry-(thickness if inner else 0))*math.sin(a)
    p=Vector((u,v,level*depth/2)) if axis=='Z' else Vector((level*depth/2,u,v))
    vs.append(p+Vector(center))
 fs=[]
 for i in range(n):
  j=(i+1)%n
  fs.extend([(i,j,j+n,i+n),(i+2*n,i+3*n,j+3*n,j+2*n),(i,i+2*n,j+2*n,j),(i+n,j+n,j+3*n,i+3*n)])
 return space_mesh(name,vs,fs,material,bone)

def space_weights(o,base):
 # Transfer the nearest original skin weights to a shaped garment panel.
 # Keeps long panels on their own leg and makes the chest follow spine flexion.
 tree=KDTree(len(base.data.vertices))
 for v in base.data.vertices:tree.insert(base.matrix_world@v.co,v.index)
 tree.balance();o.vertex_groups.clear()
 for group in base.vertex_groups:o.vertex_groups.new(name=group.name)
 for v in o.data.vertices:
  _,idx,_=tree.find(o.matrix_world@v.co)
  for g in base.data.vertices[idx].groups:
   if g.weight>0:o.vertex_groups[g.group].add([v.index],g.weight,'REPLACE')
 return o

def space_coat_panel(side,front,legbase,material):
 # Four separate tapered panels with front/back splits; no cloth simulation.
 y=-.145 if front else .064
 vs=[(side*.025,y,1.06),(side*.176,y,1.06),(side*.235,y+(-.018 if front else .02),.69),(side*.055,y+(-.018 if front else .02),.69)]
 vs+= [(x,y+(.012 if front else -.012),z) for x,y,z in vs]
 faces=[(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)]
 return space_weights(space_mesh('Split coat hem',vs,faces,material,'Hips'),legbase)

space_manifest=[]
for sid,label,body_src,leg_src,foot_src in SPACE_OUTFITS:
 body=duplicate(body_src+'_Body',f'Body_{sid:02d}')
 legs=duplicate(leg_src+'_Legs',f'Legs_{sid:02d}')
 feet=duplicate(foot_src+'_Feet',f'Feet_{sid:02d}')
 upper=[body];lower=[legs];boots=[feet]
 # Reassign existing fabric panels, not the hand/neck skin. Broad colour fields
 # remain editable using the existing suit/accent palette.
 for o in [body,legs,feet]:
  for slot in o.material_slots:
   if sid in [11,15,16,18,19,20,21] and slot.material==M['Ivory']:slot.material=M['Suit']
   if sid==17 and slot.material==M['Suit']:slot.material=M['Ivory']
   if sid==19 and slot.material==M['Suit']:slot.material=M['Ivory']
 # Shape the ORIGINAL garment with a taper to zero at the waist, wrist and ankle.
 # These fixed interfaces keep independently selected modules mixable.
 puff={10:.024,13:.016,15:.012,16:.030,17:.009,18:.010}.get(sid,0)
 for v in body.data.vertices:
  x,y,z=v.co
  if abs(x)<.19 and 1.075<z<1.48:
   t=min(1,(z-1.075)/.10,(1.48-z)/.06);v.co.y+=(1 if y>-.05 else -1)*puff*t
  elif .20<abs(x)<.53:
   t=min(1,(abs(x)-.20)/.05,(.53-abs(x))/.07)
   v.co.y+=(y+.06)*puff*4*t;v.co.z+=(z-1.432)*puff*4*t
 for v in legs.data.vertices:
  x,y,z=v.co
  if .30<z<.93:
   t=min(1,(z-.30)/.1,(.93-z)/.10);v.co.y+=(1 if y>-.06 else -1)*puff*.45*t
 # The original SpaceSuit/Swat trousers end above low shoes. Extend only the
 # lower cuff, and extend new boot shafts, so the three selectors can mix.
 hem=min(v.co.z for v in legs.data.vertices)
 for v in legs.data.vertices:
  if v.co.z<.32:v.co.z=.125+(v.co.z-hem)*(.32-.125)/(.32-hem)
 shaft=max(v.co.z for v in feet.data.vertices)
 for v in feet.data.vertices:
  if v.co.z>.09:v.co.z=.09+(v.co.z-.09)*(.275-.09)/(shaft-.09)

 surfaces={id(parts):BVHTree.FromPolygons([base.matrix_world@v.co for v in base.data.vertices],[list(p.vertices) for p in base.data.polygons]) for parts,base in [(upper,body),(lower,legs),(boots,feet)]}

 def panel(center,size,mat='Suit',bone='Torso',target=upper):
  x,y,z=center;w,d,h=size;fitted=False
  if y<-.10 and (target is not upper or abs(x)<.19):
   hit,_,_,_=surfaces[id(target)].ray_cast(Vector((x,-.55,z)),Vector((0,1,0)),1)
   if hit is not None:
    front=min(y-d/2,hit.y-.012);back=hit.y+.007
    center=(x,(front+back)/2,z);size=(w,back-front,h);fitted=True
  o=space_panel(label,center,size,mat,bone)
  if fitted:space_weights(o,body if target is upper else legs if target is lower else feet)
  target.append(o);return o
 def collar(mat='Dark',height=.027,radius=.105):
  upper.append(space_band('Pressure collar',(0,-.043,1.515),radius,radius*.82,height,mat,'Chest'))
 def shoulder(side,mat='Suit',large=False):
  bone='UpperArm.'+('L' if side>0 else 'R')
  upper.append(orb('Faceted shoulder',(side*.233,-.06,1.446),(.10 if large else .080,.103 if large else .080,.106 if large else .083),mat,bone,segments=8,rings=6))
 def cuff(side,mat='Dark',x=.50):
  radius=.062 if body_src=='SpaceSuit' else .046
  upper.append(space_band('Sleeve cuff',(side*x,-.078,1.432),radius,radius-.003,.033,mat,'LowerArm.'+('L' if side>0 else 'R'),'X'))
 def console(x=0,z=1.34,width=.145):
  panel((x,-.224,z),(width,.052,.143),'Dark')
  panel((x,-.253,z+.025),(width*.75,.01,.038),'Accent')
  for dx in [-.027,.027]:panel((x+dx,-.254,z-.036),(.018,.012,.024),'Ivory')
 def knee(side,mat='Dark',heavy=False):
  panel((side*.121,-.164,.52),(.133 if heavy else .105,.035,.15 if heavy else .115),mat,'LowerLeg.'+('L' if side>0 else 'R'),lower)
 def pocket(side,z=.80):
  panel((side*.145,-.172,z),(.098,.052,.13),'Suit','UpperLeg.'+('L' if side>0 else 'R'),lower)
  panel((side*.145,-.204,z+.042),(.088,.014,.028),'Accent','UpperLeg.'+('L' if side>0 else 'R'),lower)
 def gaiter(side,mat='Dark',high=.24):
  boots.append(space_band('Ankle seal',(side*.13,-.056,high),.072,.078,.035,mat,'LowerLeg.'+('L' if side>0 else 'R')))
 def toe(side,mat='Ivory'):
  panel((side*.132,-.237,.055),(.124,.084,.08),mat,'Foot.'+('L' if side>0 else 'R'),boots)

 if sid==10:
  collar('Ivory');console()
  for s in [-1,1]:
   shoulder(s,'Ivory',True);cuff(s,'Accent');knee(s,'Ivory');toe(s);gaiter(s,'Accent')
   panel((s*.09,-.184,1.12),(.075,.055,.05),'Accent','Abdomen')
 elif sid==11:
  collar('Dark',.022,.096)
  for s in [-1,1]:
   # Harness is segmented across spine bones rather than one rigid chest slab.
   for z in [1.20,1.32,1.43]:panel((s*.095,-.172,z),(.035,.023,.104),'Dark','Chest' if z>1.40 else 'Torso')
   panel((s*.095,-.19,1.39),(.054,.015,.035),'Ivory');cuff(s);gaiter(s,'Accent',.155)
  panel((0,-.176,1.10),(.225,.025,.045),'Dark','Abdomen')
  console(.055,1.27,.092);pocket(-1)
 elif sid==12:
  shoulder(1,'Accent');collar('Suit',.024,.102)
  console(-.06,1.31,.10)
  panel((.102,-.17,1.41),(.075,.03,.046),'Ivory','Chest')
  for s in [-1,1]:pocket(s);knee(s,'Dark');gaiter(s,'Accent');cuff(s,'Accent')
  panel((-.25,-.128,1.458),(.035,.04,.05),'Accent','UpperArm.R')
 elif sid==13:
  collar('Dark');console(0,1.34,.17)
  for s in [-1,1]:
   shoulder(s,'Accent',True);cuff(s,'Dark',.47);knee(s,'Ivory',True);toe(s,'Accent');gaiter(s)
   panel((s*.23,-.175,1.447),(.10,.045,.145),'Dark','UpperArm.'+('L' if s>0 else 'R'))
   panel((s*.121,-.167,.32),(.096,.035,.16),'Accent','LowerLeg.'+('L' if s>0 else 'R'),lower)
 elif sid==14:
  shoulder(-1,'Ivory',True);cuff(-1,'Accent');cuff(1,'Dark')
  for z,x in [(1.41,-.11),(1.31,-.01),(1.21,.09)]:panel((x,-.178,z),(.070,.033,.11),'Dark')
  panel((.10,-.196,1.24),(.080,.041,.12),'Accent')
  knee(-1,'Accent',True);knee(1);pocket(1);toe(-1,'Dark');toe(1,'Dark')
 elif sid==15:
  collar('Accent',.042,.105)
  for z in [1.17,1.29,1.41]:panel((0,-.197,z),(.027,.025,.104),'Accent','Chest' if z>1.4 else 'Torso')
  for s in [-1,1]:
   cuff(s,'Accent');gaiter(s,'Accent');
   upper.append(orb('Chest filter',(s*.105,-.193,1.35),(.044,.038,.044),'Ivory','Torso',segments=8,rings=4))
  panel((0,-.231,1.43),(.062,.012,.035),'Dark','Chest')
 elif sid==16:
  collar('Ivory',.058,.115)
  for s in [-1,1]:
   shoulder(s,'Suit',True);cuff(s,'Ivory');pocket(s);knee(s,'Dark');gaiter(s,'Ivory');toe(s,'Dark')
   for z in [1.21,1.30,1.39]:panel((s*.096,-.208,z),(.136,.053,.072),'Suit')
  panel((0,-.209,1.28),(.025,.018,.23),'Accent')
 elif sid==17:
  collar('Dark',.025,.104)
  for s in [-1,1]:
   shoulder(s,'Ivory');cuff(s,'Dark');knee(s,'Ivory');toe(s,'Dark');gaiter(s)
   panel((s*.086,-.206,1.32),(.14,.042,.20),'Dark')
   for z in [1.255,1.30,1.345,1.39]:panel((s*.086,-.234,z),(.13,.02,.021),'Ivory')
  panel((0,-.218,1.15),(.12,.03,.056),'Accent','Abdomen')
 elif sid==18:
  collar('Dark',.029,.107)
  for s in [-1,1]:
   shoulder(s,'Suit',True);knee(s,'Suit',True);cuff(s,'Dark');gaiter(s);toe(s,'Dark')
   panel((s*.087,-.19,1.36),(.15,.056,.19),'Suit')
   panel((s*.087,-.225,1.405),(.125,.014,.027),'Accent')
  panel((0,-.178,1.13),(.19,.035,.09),'Dark','Abdomen')
 elif sid==19:
  collar('Ivory',.021,.098)
  for s in [-1,1]:
   cuff(s,'Accent');gaiter(s,'Ivory',.154);pocket(s)
   panel((s*.10,-.172,1.21),(.08,.047,.11),'Ivory')
  # Original triangular rescue insignia; not a protected red-cross emblem.
  panel((.092,-.176,1.416),(.06,.022,.063),'Accent','Chest')
  for s in [-1,1]:upper.append(space_coat_panel(s,True,legs,'Ivory'))
 elif sid==20:
  for s in [-1,1]:
   panel((s*.21,-.064,1.494),(.105,.096,.027),'Accent','UpperArm.'+('L' if s>0 else 'R'))
   cuff(s,'Accent');gaiter(s,'Dark',.15)
  for z in [1.20,1.30,1.40]:panel((.015,-.165,z),(.014,.013,.017),'Ivory')
  panel((-.096,-.165,1.412),(.066,.02,.035),'Accent','Chest')
  panel((0,-.165,1.09),(.25,.028,.028),'Dark','Abdomen')
 elif sid==21:
  collar('Suit',.032,.11)
  for s in [-1,1]:
   upper.extend([space_coat_panel(s,True,legs,'Suit'),space_coat_panel(s,False,legs,'Suit')])
   panel((s*.13,-.185,1.15),(.096,.055,.12),'Dark','Abdomen');cuff(s,'Accent');pocket(s);gaiter(s)
  console(-.07,1.36,.083)
 # Fit each rear identifier to the actual cloth surface and transfer its skin
 # weights; a guessed Y plane left floating strips when the spine bent.
 tree=BVHTree.FromPolygons([body.matrix_world@v.co for v in body.data.vertices],[list(p.vertices) for p in body.data.polygons])
 for z in ([1.35,1.40] if sid in [11,20,21] else [1.28,1.34,1.40]):
  hit,_,_,_=tree.ray_cast(Vector((0,.5,z)),Vector((0,-1,0)),.8)
  if hit is not None:
   o=panel((0,hit.y+.003,z),(.15 if sid!=13 else .18,.016,.018),'Accent');space_weights(o,body)
 for collection,name in [(upper,'Body'),(lower,'Legs'),(boots,'Feet')]:
  join(collection,f'{name}_{sid:02d}')
 space_manifest.append(dict(id=sid,name=label,source=[body_src,leg_src,foot_src]))
(ROOT/'QA/space-outfits-source.json').write_text(json.dumps(space_manifest,ensure_ascii=False,indent=2),encoding='utf-8')

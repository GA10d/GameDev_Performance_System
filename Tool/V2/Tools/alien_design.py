"""Distinct alien anatomy and crown silhouettes, authored as weighted low-poly meshes.
All designs are original abstractions; no meshes from reference games are imported.
"""
alien_specs={}
alien_profiles={
 0:('灰裔',1,1,0,0),1:('阔吻鳃族',1.32,.92,-.045,0),
 2:('裂颚甲族',1.0,1,.015,.025),3:('穹壳独眼',1.12,.94,-.015,0),
 4:('横翼四目',1.70,.85,-.035,0),5:('菌伞胞族',1.75,.82,.035,.01),
 6:('晶面硅族',.96,1.12,.075,0),7:('喙面翼族',.95,1,.015,.025),
 8:('纵颅深潜者',1,1.20,.105,.075),9:('花萼共生体',1.50,.95,.025,0)
}
def alien_fit(p,i):
 q=p.copy();name,sx,sz,dz,dy=alien_profiles[i]
 q.x*=sx;q.y+=dy;q.z=1.9+(q.z-1.9)*sz+dz;return q

def mesh_part(name,verts,faces,material='Skin'):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
 o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
 return bind(o,name,material)

def shell(name,rings,segments=12,material='Skin'):
 # Continuous cross-sections give a coherent skull/neck, rather than a pile of spheres.
 verts=[];faces=[]
 for z,rx,ry,cy in rings:
  for k in range(segments):
   a=math.tau*k/segments;verts.append((math.sin(a)*rx,cy-math.cos(a)*ry,z))
 for j in range(len(rings)-1):
  for k in range(segments):a=j*segments+k;b=j*segments+(k+1)%segments;faces.append((a,b,b+segments,a+segments))
 faces.extend([tuple(reversed(range(segments))),tuple(range((len(rings)-1)*segments,len(rings)*segments))])
 return mesh_part(name,verts,faces,material)

def tube(name,points,radii,material='Skin',sides=8):
 verts=[];faces=[]
 for j,point in enumerate(points):
  p=Vector(point);d=(Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])).normalized()
  u=d.cross(Vector((0,1,0)))
  if u.length<.01:u=d.cross(Vector((1,0,0)))
  u.normalize();v=d.cross(u).normalized()
  for k in range(sides):a=math.tau*k/sides;verts.append(p+radii[j]*(u*math.cos(a)+v*math.sin(a)))
 for j in range(len(points)-1):
  for k in range(sides):a=j*sides+k;b=j*sides+(k+1)%sides;faces.append((a,b,b+sides,a+sides))
 faces.extend([tuple(reversed(range(sides))),tuple(range((len(points)-1)*sides,len(points)*sides))])
 return mesh_part(name,verts,faces,material)

def plate(name,outline,thickness=.012,material='Skin'):
 # Explicit front/back and rim; the center ridge makes membrane/petal volumes readable.
 n=len(outline);center=sum((Vector(v) for v in outline),Vector())/n
 verts=[Vector(p) for p in outline]+[Vector(p)+Vector((0,thickness,0)) for p in outline]
 verts.extend([center+Vector((0,-thickness*.65,0)),center+Vector((0,thickness,0))]);faces=[]
 for k in range(n):j=(k+1)%n;faces.extend([(2*n,k,j),(2*n+1,n+j,n+k),(k,n+k,n+j,j)])
 return mesh_part(name,verts,faces,material)

def tag(o,name):
 g=o.vertex_groups.new(name=name);g.add(list(range(len(o.data.vertices))),1,'REPLACE');return o

def make_eye(parts,spec,center,scale,brow=True):
 index=len(spec['eyes']);spec['eyes'].append(center)
 x,y,z=center;rx,ry,rz=scale
 parts.append(orb('Eye socket',(x,y+.010,z),(rx*1.19,ry*1.04,rz*1.20),'SkinShade',segments=12,rings=6))
 parts.append(tag(orb('Optic',(x,y-.008,z),scale,'Eye',segments=12,rings=6),'DetailEye'+str(index)))
 parts.append(tag(orb('Optic glint',(x-rx*.18,y-ry-.008,z+rz*.18),(rx*.18,.004,rz*.15),'Ivory',segments=8,rings=4),'DetailEye'+str(index)))
 if brow:
  parts.append(tag(orb('Supraorbital plate',(x,y+.002,z+rz*.94),(rx*1.21,.016,.013),'Skin',segments=8,rings=4),'DetailBrow'))

def mouth(parts,loc,scale=(.043,.009,.006)):
 # Integrate the seam into the actual muzzle or skull instead of a hovering orb.
 target=next((p for p in parts if p.name.startswith('Broad muzzle')),parts[0])
 geometry_mouth(target,scale[0],loc[2],min(.005,scale[2]*.65))
 spec['mouth']=(loc[2],scale[0])

profiles={
 1:[(1.51,.049,.053,-.012),(1.60,.071,.068,-.035),(1.66,.143,.127,-.049),(1.73,.185,.143,-.025),(1.81,.186,.134,-.008),(1.885,.120,.10,.005),(1.91,.044,.04,.012)],
 2:[(1.51,.049,.055,-.012),(1.63,.076,.083,-.020),(1.72,.117,.117,-.010),(1.80,.135,.145,.012),(1.885,.100,.165,.035),(1.96,.035,.078,.075)],
 3:[(1.51,.049,.055,-.012),(1.62,.085,.077,-.022),(1.69,.143,.116,-.026),(1.79,.160,.141,-.010),(1.88,.132,.124,0),(1.925,.060,.057,.010)],
 4:[(1.51,.049,.055,-.012),(1.63,.061,.065,-.02),(1.72,.110,.095,-.019),(1.765,.260,.116,-.01),(1.815,.285,.116,-.002),(1.855,.22,.088,.01),(1.88,.074,.050,.015)],
 5:[(1.51,.049,.055,-.012),(1.63,.062,.074,-.023),(1.73,.083,.085,-.010),(1.80,.090,.085,.005)],
 6:[(1.51,.049,.054,-.012),(1.63,.072,.073,-.01),(1.715,.145,.116,.006),(1.825,.155,.131,.015),(1.94,.076,.095,.021),(2.025,.012,.016,.025)],
 7:[(1.51,.049,.055,-.012),(1.63,.071,.079,-.008),(1.73,.103,.10,.008),(1.835,.111,.123,.028),(1.91,.081,.140,.065),(1.945,.025,.078,.090)],
 8:[(1.51,.049,.055,-.012),(1.635,.075,.070,-.02),(1.75,.120,.13,-.002),(1.855,.140,.19,.036),(1.975,.122,.205,.090),(2.075,.063,.142,.14),(2.10,.018,.042,.16)],
 9:[(1.51,.049,.055,-.012),(1.64,.055,.070,-.03),(1.75,.085,.086,-.020),(1.84,.079,.07,-.007),(1.895,.021,.025,.005)]
}
def jaw_support_rings(rings):
 # The original alien shells had one long neck-to-cheek quad strip. Add support
 # rings on that same surface so a local rounded/square chin can bend continuously
 # instead of pulling only the two distant end rings.
 result=list(rings)
 for z in [1.58,1.60,1.62,1.65,1.68,1.70]:
  if any(abs(r[0]-z)<1e-5 for r in result):continue
  for a,b in zip(rings,rings[1:]):
   if a[0]<z<b[0]:
    t=(z-a[0])/(b[0]-a[0]);result.append(tuple([z]+[a[k]+t*(b[k]-a[k]) for k in range(1,4)]));break
 return sorted(result)

for i in range(1,10):
 spec={'eyes':[],'nose':(0,-.15,1.72)};parts=[shell('Continuous cranium',jaw_support_rings(profiles[i]),8 if i==6 else 16)]
 if i==1:
  parts.append(tag(orb('Broad muzzle',(0,-.146,1.687),(.128,.074,.043),'Skin',segments=12,rings=6),'DetailNose'))
  for s in [-1,1]:
   make_eye(parts,spec,(s*.128,-.130,1.808),(.044,.027,.044))
   parts.append(orb('Nostril',(s*.050,-.209,1.711),(.013,.005,.006),'SkinShade',segments=8,rings=4))
   for z in [1.70,1.733,1.765]:parts.append(plate('Gill groove',[(s*.155,-.094,z),(s*.183,-.038,z+.014),(s*.182,-.032,z+.019),(s*.153,-.09,z+.006)],.004,'SkinShade'))
  mouth(parts,(0,-.215,1.674),(.084,.004,.007))
 if i==2:
  for s in [-1,1]:
   make_eye(parts,spec,(s*.082,-.134,1.805),(.041,.021,.020))
   for height in [1.674,1.718]:
    jaw=tube('Split mandible',[(s*.078,-.095,height+.035),(s*.095,-.181,height),(s*.052,-.218,height-.030),(s*.029,-.19,height-.043)],[.030,.027,.017,.006],'SkinShade')
    parts.append(tag(jaw,'DetailMouth'))
  parts.append(tag(plate('Keel nose',[(0,-.172,1.86),(-.025,-.17,1.72),(0,-.205,1.695),(.025,-.17,1.72)],.013,'Skin'),'DetailNose'))
  mouth(parts,(0,-.144,1.683),(.029,.018,.05))
 if i==3:
  make_eye(parts,spec,(0,-.150,1.80),(.083,.033,.060))
  parts.append(tag(plate('Central olfactory crest',[(-.018,-.153,1.72),(0,-.186,1.67),(.018,-.153,1.72)],.012),'DetailNose'))
  mouth(parts,(0,-.143,1.653),(.055,.009,.006))
 if i==4:
  for s in [-1,1]:
   make_eye(parts,spec,(s*.216,-.085,1.802),(.029,.019,.025),False)
   make_eye(parts,spec,(s*.138,-.108,1.790),(.023,.016,.029),False)
  parts.append(tag(plate('Broad central ridge',[(-.08,-.120,1.83),(0,-.146,1.863),(.08,-.120,1.83),(0,-.147,1.805)],.014),'DetailBrow'))
  parts.append(tag(orb('Nasal pore',(0,-.12,1.75),(.015,.011,.012),'SkinShade',segments=8,rings=4),'DetailNose'))
  mouth(parts,(0,-.109,1.676),(.044,.008,.006))
 if i==5:
  parts.append(shell('Fungal canopy',[(1.798,.248,.159,.014),(1.823,.26,.17,.014),(1.906,.185,.141,.018),(1.946,.062,.065,.02)],16,'Skin'))
  for k in range(12):
   a=math.tau*k/12;parts.append(tube('Radial gill',[(math.sin(a)*.060,.014+math.cos(a)*.045,1.795),(math.sin(a)*.237,.014+math.cos(a)*.148,1.805)],[.008,.006],'Accent',sides=5))
  for x in [-.045,.045]:make_eye(parts,spec,(x,-.091,1.739),(.024,.014,.030),False)
  parts.append(tag(orb('Breathing pore',(0,-.096,1.699),(.011,.006,.009),'SkinShade',segments=8,rings=4),'DetailNose'))
  mouth(parts,(0,-.092,1.668),(.026,.006,.004))
 if i==6:
  for s in [-1,1]:make_eye(parts,spec,(s*.061,-.094,1.813),(.016,.012,.046),False)
  parts.append(tag(plate('Crystal brow',[(-.113,-.093,1.874),(0,-.144,1.895),(.113,-.093,1.874),(0,-.14,1.856)],.013,'Accent'),'DetailBrow'))
  parts.append(tag(plate('Prismatic nose',[(0,-.152,1.856),(-.025,-.124,1.729),(0,-.173,1.708),(.025,-.124,1.729)],.016,'SkinShade'),'DetailNose'))
  mouth(parts,(0,-.108,1.67),(.035,.006,.005))
 if i==7:
  for s in [-1,1]:make_eye(parts,spec,(s*.080,-.080,1.816),(.028,.022,.029))
  beak=mesh_part('Solid upper beak',[(-.060,-.081,1.763),(.060,-.081,1.763),(0,-.306,1.687),(0,-.12,1.813),(-.041,-.085,1.694),(.041,-.085,1.694)],[(0,1,3),(0,3,2),(3,1,2),(0,2,4),(2,1,5),(4,2,5),(0,4,5,1)],'SkinShade')
  parts.append(tag(beak,'DetailNose'))
  parts.append(tag(plate('Lower beak',[(-.041,-.09,1.692),(0,-.285,1.677),(.041,-.09,1.692),(0,-.109,1.651)],.014,'Accent'),'DetailMouth'))
 if i==8:
  for s in [-1,1]:
   make_eye(parts,spec,(s*.076,-.118,1.794),(.043,.020,.027))
   for z in [1.668,1.698,1.728]:parts.append(plate('Vent',[(s*.062,-.092,z),(s*.091,-.071,z+.008),(s*.09,-.073,z+.014),(s*.06,-.095,z+.006)],.004,'SkinShade'))
  parts.append(tag(plate('Olfactory ridge',[(0,-.153,1.864),(-.016,-.141,1.742),(0,-.174,1.710),(.016,-.141,1.742)],.012),'DetailNose'))
  mouth(parts,(0,-.106,1.659),(.039,.005,.006))
 if i==9:
  for k in range(7):
   a=math.tau*k/7;u=Vector((math.sin(a),0,math.cos(a)));v=Vector((u.z,0,-u.x));c=Vector((0,.015,1.777))
   outline=[c+u*.049-v*.023,c+u*.185-v*.070+Vector((0,.004,0)),c+u*.251+Vector((0,.018,0)),c+u*.185+v*.07+Vector((0,.004,0)),c+u*.049+v*.023]
   parts.append(plate('Calyx lobe',outline,.026,'Skin' if k%2 else 'Accent'))
  for x in [-.037,.037]:make_eye(parts,spec,(x,-.109,1.768),(.021,.013,.033),False)
  make_eye(parts,spec,(0,-.087,1.836),(.014,.010,.022),False)
  parts.append(tag(orb('Scent organ',(0,-.115,1.716),(.013,.015,.018),'Accent',segments=8,rings=4),'DetailNose'))
  mouth(parts,(0,-.095,1.676),(.023,.005,.006))
 name=f'Head_Alien_{i:02d}';o=join(parts,name);alien_specs[name]=spec

# Each crown changes the silhouette language, not merely the count of spikes.
for i in range(1,12):
 p=[]
 if i==1:
  for s in [-1,1]:
   root=(s*.105,.012,1.80);tips=[(s*.24,.015,1.91),(s*.345,.03,2.04),(s*.325,.10,1.84),(s*.245,.11,1.724)]
   p.append(plate('Broad gill fan',[root]+tips,.016,'Accent'))
   for tip in tips:p.append(tube('Fan ray',[root,tip],[.013,.005],'Skin'))
 if i==2:
  for s in [-1,1]:p.append(tube('Curved ram horn',[(s*.105,.017,1.82),(s*.20,.025,1.955),(s*.295,.06,1.952),(s*.33,.072,1.847),(s*.26,.02,1.784),(s*.219,-.01,1.825)],[.035,.045,.042,.030,.021,.003],'SkinShade'))
 if i==3:
  outline=[(0,-.09,1.88),(0,-.055,2.215),(0,.10,2.285),(0,.31,2.015),(0,.20,1.79)]
  # A sagittal sail, with real thickness across X.
  sail=plate('Sail',[(y,0,z) for x,y,z in outline],.034,'Accent')
  for v in sail.data.vertices:v.co.x,v.co.y=-v.co.y,v.co.x
  p.append(sail)
 if i==4:
  points=[(.225*math.sin(-2.0+k*4.0/16),.028,2.06+.225*math.cos(-2.0+k*4.0/16)) for k in range(17)]
  p.append(tube('Lunar arch',points,[.025]*17,'Skin'))
  for s in [-1,1]:p.append(tube('Arch root',[(s*.12,.025,1.835),(s*.20,.03,1.965)],[.03,.025],'Skin'))
 if i==5:
  for s in [-1,1]:
   p.append(tube('Hanging cephalic lobe',[(s*.08,.038,1.91),(s*.155,.12,1.89),(s*.185,.145,1.78),(s*.165,.12,1.64),(s*.22,.16,1.605)],[.037,.05,.043,.024,.004],'Skin'))
   p.append(tube('Lobe stripe',[(s*.115,.065,1.928),(s*.19,.115,1.78),(s*.172,.095,1.675)],[.012,.009,.003],'Accent',sides=6))
 if i==6:
  p.append(shell('Umbrella crown',[(1.925,.273,.19,.034),(1.95,.29,.20,.034),(2.05,.19,.144,.033),(2.115,.05,.049,.025)],16,'Accent'))
  for a in [math.tau*k/12 for k in range(12)]:p.append(tube('Lamella',[(.075*math.sin(a),.025+.07*math.cos(a),1.906),(.26*math.sin(a),.034+.18*math.cos(a),1.93)],[.009,.006],'SkinShade',sides=5))
 if i==7:
  for x,y,z,h,w in [(-.07,.016,1.865,.27,.045),(.026,.029,1.89,.36,.055),(.112,.065,1.85,.19,.048),(-.13,.10,1.81,.15,.038)]:
   p.append(shell('Crystal spire',[(z,w*.6,w*.65,y),(z+h*.26,w,w,y),(z+h*.74,w*.76,w*.74,y+.022),(z+h,.001,.001,y+.025)],6,'Accent'))
   for v in p[-1].data.vertices:v.co.x+=x
 if i==8:
  for s in [-1,1]:
   points=[(s*.075,-.014,1.89),(s*.12,-.04,2.05),(s*.245,-.075,2.14),(s*.31,-.11,2.105)]
   p.append(tube('Sensory antenna',points,[.022,.018,.011,.006],'Skin'))
   p.append(orb('Sensory bulb',points[-1],(.028,.022,.030),'Accent',segments=10,rings=6))
 if i==9:
  p.append(tube('Carapace dorsal root',[(0,.025,1.80),(0,-.015,1.89),(0,.033,1.95),(0,.10,2.005),(0,.20,2.01)],[.034,.032,.030,.028,.022],'SkinShade'))
  for j,(z,w,y) in enumerate([(1.892,.145,-.015),(1.95,.17,.033),(2.005,.14,.10),(2.01,.095,.20)]):
   p.append(plate('Overlapping carapace',[(-w,y,z-.04),(-w*.6,y-.045,z+.055),(0,y-.05,z+.093),(w*.6,y-.045,z+.055),(w,y,z-.04),(0,y+.055,z-.055)],.035,'Skin' if j%2 else 'SkinShade'))
 if i==10:
  for k in range(7):
   a=math.tau*k/7;u=Vector((math.sin(a),.4*math.cos(a),max(.10,math.cos(a))));u.normalize();c=Vector((0,.027,1.906));v=Vector((math.cos(a),.15,-math.sin(a)*.4)).normalized()
   p.append(plate('Leaf crown',[c-v*.025,c+u*.17-v*.072,c+u*.32,c+u*.17+v*.072,c+v*.025],.02,'Skin' if k%2 else 'Accent'))
 if i==11:
  p.append(shell('Transverse bony bridge',[(1.865,.1,.055,.020),(1.97,.16,.070,.018),(2.035,.355,.081,.017),(2.09,.33,.081,.011),(2.14,.23,.047,.012)],12,'SkinShade'))
  for s in [-1,1]:p.append(plate('Bridge edge',[(s*.08,-.069,2.03),(s*.345,-.065,2.03),(s*.32,-.07,2.095),(s*.10,-.071,2.09)],.008,'Accent'))
 join(p,f'Hair_Alien_{i:02d}')

(ROOT/'Source/alien_design_manifest.json').write_text(json.dumps({'heads':{str(k):v[0] for k,v in alien_profiles.items()},'crests':['none','gill fan','ram horn','sagittal sail','lunar arch','hanging lobes','fungal umbrella','crystal spires','sensory antennae','carapace','leaf crown','bony bridge']},ensure_ascii=False,indent=2),encoding='utf-8')

def alien_detail_shapes(o):
 spec=alien_specs[o.name]
 def group_ids(prefix):
  return {v.index for v in o.data.vertices if any(o.vertex_groups[g.group].name.startswith(prefix) for g in v.groups)}
 eyes=[group_ids('DetailEye'+str(i)) for i in range(len(spec['eyes']))]
 brows=group_ids('DetailBrow');noses=group_ids('DetailNose');mouths=group_ids('DetailMouth')
 def eye_warp(p,idx,j):
  q=p.copy()
  for anchor,ids in zip(spec['eyes'],eyes):
   if idx not in ids:continue
   c=Vector(anchor);d=q-c
   if j==1:d.z*=1.12
   if j==2:d.z*=.78
   if j==3:d.x*=1.12
   if j==4:d.x*=.85
   if j==5:d.z+=d.x*.18*(1 if c.x>=0 else -1)
   if j==6:d.z-=d.x*.18*(1 if c.x>=0 else -1)
   if j==7:d.y-=.004
   return c+d
  return q
 for j in range(1,8):shape(o,f'Eye{j:02d}',lambda p,idx,j=j:eye_warp(p,idx,j))
 for prefix,ids in [('Brow',brows),('Nose',noses),('Mouth',mouths)]:
  center=sum((o.data.vertices[k].co for k in ids),Vector())/max(1,len(ids))
  for j in range(1,6):
   def detail(p,idx,j=j,ids=ids,center=center):
    if prefix=='Mouth' and 'mouth' in spec:
     z,width=spec['mouth'];return mouth_warp(p,j,z,width)
    if idx not in ids:return p
    d=p-center
    if j==1:d.z*=1.12
    if j==2:d.z*=.85
    if j==3:d.x*=1.10
    if j==4:d.x*=.87
    if j==5:d.y-=.006
    return center+d
   shape(o,f'{prefix}{j:02d}',detail)

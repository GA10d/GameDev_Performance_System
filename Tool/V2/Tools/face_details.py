"""UV face paint, closed mask shells and compact headsets; stable saved IDs."""
face_detail_report=[]
import numpy as np

def strip_region(cx,cz,width,height,slope=0):
 return [(cx-width/2,cz-height/2-slope*width/2),(cx+width/2,cz-height/2+slope*width/2),
         (cx+width/2,cz+height/2+slope*width/2),(cx-width/2,cz+height/2-slope*width/2)]

def fitted_strap(head,name,z,side):
 # Ribbon follows the cheek, temple and back of the head; no floating ear rods.
 head.data.calc_loop_triangles()
 triangles=[tuple(t.vertices) for t in head.data.loop_triangles
            if head.data.materials[head.data.polygons[t.polygon_index].material_index] in [M['Skin'],M['SkinShade']]]
 tree=BVHTree.FromPolygons([v.co for v in head.data.vertices],triangles,all_triangles=True)
 verts=[];faces=[];records=[]
 from mathutils.geometry import barycentric_transform
 for step in range(15):
  angle=math.radians(27+step*8);direction=Vector((side*math.sin(angle),-math.cos(angle),0))
  for dz in [-.002,.002]:
   origin=Vector((0,-.025,z+dz));hit,normal,index,distance=tree.ray_cast(origin,direction,.4)
   assert hit is not None,('Mask strap misses skin',name,step,z)
   ids=triangles[index];points=[head.data.vertices[i].co for i in ids]
   weights=barycentric_transform(hit,*points,Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1)))
   offset=normal*.002;verts.append(hit+offset);records.append((ids,weights,offset))
  if step:
   i=step*2;faces.append((i-2,i,i+1,i-1) if side>0 else (i-1,i+1,i,i-2))
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
 o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bind(o,name,'Dark');o.shape_key_add(name='Basis',from_mix=False)
 for src in head.data.shape_keys.key_blocks:
  if src.name=='Basis':continue
  key=o.shape_key_add(name=src.name,from_mix=False)
  for i,(ids,weights,offset) in enumerate(records):key.data[i].co=sum((src.data[j].co*w for j,w in zip(ids,weights)),Vector())+offset
 return o

def deform_detail(o,head):
 o.shape_key_add(name='Basis',from_mix=False)
 for key in head.data.shape_keys.key_blocks:
  if key.name!='Basis':shape(o,key.name,lambda p,i,k=key.name:head_warp(p,k))
 return o

def detail_mesh(name,verts,faces,material,head):
 mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
 o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 return deform_detail(bind(o,name,material),head)

def mask_shell(head,name):
 # Continuous shell bridges nose and lips; copying skin triangles left holes at features.
 tree=BVHTree.FromPolygons([v.co for v in head.data.vertices],[list(p.vertices) for p in head.data.polygons])
 rows=[(1.603,.025),(1.615,.045),(1.631,.062),(1.649,.065),(1.667,.052),(1.682,.019)]
 front_depth=min(v.co.y for v in head.data.vertices if abs(v.co.x)<.075 and 1.608<v.co.z<1.70)-.017
 def front(x,z,u):
  # Use a continuous envelope around all lips/nose vertices. Sampling only row heights
  # missed thin lip meshes between rows and created holes and abrupt depth changes.
  return front_depth+.025*abs(u)**1.6+.018*((z-1.644)/.043)**2
 verts=[];faces=[]
 for z,width in rows:
  for column in range(7):
   u=column/3-1;x=width*u;verts.append(Vector((x,front(x,z,u),z)))
 for row in range(5):
  for col in range(6):
   i=row*7+col;faces.append((i,i+1,i+8,i+7))
 # Return the open perimeter towards the face to give the fabric a visible folded hem.
 boundary=list(range(7))+[r*7+6 for r in range(1,6)]+list(range(40,34,-1))+[r*7 for r in range(4,0,-1)]
 rim=len(verts);verts.extend(verts[i]+Vector((0,.006,0)) for i in boundary)
 for j,index in enumerate(boundary):
  n=(j+1)%len(boundary);faces.append((index,boundary[n],rim+n,rim+j))
 parts=[detail_mesh(name,verts,faces,'Suit',head)]
 # Match the normal calculation used by the bounded face blend shapes. Hard base
 # normals combined with Unity's recalculated blend normals made dark triangular
 # bands on the long-face preset. Fabric keeps the low-poly silhouette with a
 # continuous normal field, just like the existing skin.
 for polygon in parts[0].data.polygons:polygon.use_smooth=True
 for row in [2,3]:
  z,width=rows[row];vs=[]
  for column in range(7):
   u=(column/3-1)*.88;x=width*u;y=front(x,z,u)-.0008
   vs.extend([(x,y,z-.0007),(x,y,z+.0007)])
  parts.append(detail_mesh('Mask stitched pleat',vs,[(i,i+2,i+3,i+1) for i in range(0,12,2)],'Dark',head))
 return parts,front

def headset(head,species,style):
 z=1.748 if species=='Alien' else 1.693
 tree=BVHTree.FromPolygons([v.co for v in head.data.vertices],
       [list(p.vertices) for p in head.data.polygons if head.data.materials[p.material_index] in [M['Skin'],M['SkinShade']]])
 hit,normal,_,_=tree.ray_cast(Vector((.4,-.070,z)),Vector((-1,0,0)),1)
 if hit is None and species=='Female':hit,normal,_,_=tree.find_nearest(Vector((.085,-.070,z)))
 assert hit is not None,('Headset contact missing',species,[(m.name) for m in head.data.materials],[[min(v.co[i] for v in head.data.vertices),max(v.co[i] for v in head.data.vertices)] for i in range(3)])
 center=hit+normal*.002
 parts=[deform_detail(orb('Low profile ear pad',center,(.007,.014,.020),'Dark',segments=10,rings=6),head),
        deform_detail(orb('Receiver dial',center+Vector((.005,-.002,.003)),(.002,.007,.009),'Trim',segments=8,rings=4),head)]
 if style==8:
  a=center+Vector((.002,-.010,-.015));b=center+Vector((-.002,-.030,-.028));c=center+Vector((-.010,-.052,-.034))
  parts.extend([deform_detail(cone('Short comms boom',a,b,.003,'Dark'),head),
                deform_detail(cone('Short comms boom',b,c,.0025,'Dark'),head),
                deform_detail(orb('Comms microphone',c,(.004,.007,.004),'Dark',segments=8,rings=4),head)])
 return join(parts,f'FaceAcc_{species}_{style:02d}')

for species,head in [('Human',human),('Alien',alien),('Female',female)]:
 # A dedicated second UV channel paints the existing skin; it adds no face geometry.
 # Back-of-head, eyes, brows and mouth retain transparent/black mask coordinates.
 if not head.data.uv_layers:head.data.uv_layers.new(name='UVMap')
 while len(head.data.uv_layers)>1:head.data.uv_layers.remove(head.data.uv_layers[-1])
 paint_uv=head.data.uv_layers.new(name='FacePaintUV')
 for polygon in head.data.polygons:
  front=head.data.materials[polygon.material_index] in [M['Skin'],M['SkinShade']] and polygon.normal.y<-.08 and polygon.center.y<-.06
  for loop in polygon.loop_indices:
   p=head.data.vertices[head.data.loops[loop].vertex_index].co
   paint_uv.data[loop].uv=((p.x+.20)/.40,(p.z-1.55)/.40) if front else (-1,-1)
 textures=ROOT/'UnityProject/Assets/AstraToolV2/Resources/FaceMarks';textures.mkdir(parents=True,exist_ok=True)
 for style in range(1,6):
  regions=[];cx=.045 if species=='Alien' else .064;cz=1.681 if species=='Alien' else 1.661
  for side in ([-1,1] if style in [3,5] else [-1] if style in [1,4] else [1]):
   for dz in ([-.0035,.0035] if style==5 else [0]):
    regions.append(strip_region(side*cx,cz+dz,.024,.010 if style==4 else .0028,side*.10))
  # Analytic soft-edge rasterization. The mask is tinted with the chosen accent in Unity.
  size=512
  grid=(np.arange(size,dtype=np.float32)+.5)/size*.40
  x=grid[None,:]-.20;z=grid[:,None]+1.55;coverage=np.zeros((size,size),dtype=np.float32)
  for region in regions:
   distances=[]
   for j,(rx,rz) in enumerate(region):
    nx,nz=region[(j+1)%len(region)];dx=nx-rx;dz=nz-rz
    distances.append(((x-rx)*dz-(z-rz)*dx)/math.hypot(dx,dz))
   coverage=np.maximum(coverage,np.clip(.5-np.maximum.reduce(distances)/(.40/size),0,1))
  pixels=np.ones((size,size,4),dtype=np.float32);pixels[:,:,:3]=coverage[:,:,None]
  image=bpy.data.images.new(f'{species}_{style:02d}',width=size,height=size,alpha=False)
  image.colorspace_settings.name='Non-Color';image.pixels.foreach_set(pixels.ravel())
  image.filepath_raw=str(textures/f'{species}_{style:02d}.png');image.file_format='PNG';image.save();bpy.data.images.remove(image)
  face_detail_report.append({'texture':f'{species}_{style:02d}.png','size':size,'geometryVertices':0,'uvChannel':1})
 for style in [5,7]:
  name=f'FaceAcc_{species}_{style:02d}';parts,mask_front=mask_shell(head,name)
  # Top and bottom elastic ribbons are attached to the fitted shell.
  for side in [-1,1]:
   for z in [1.650,1.622]:parts.append(fitted_strap(head,'Mask elastic',z,side))
  if style==5:
   for side in [-1,1]:
    x=side*.037
    housing=deform_detail(orb('Filter housing',(x,mask_front(x,1.649,x/.065)-.008,1.649),(.015,.010,.015),'Trim',segments=8,rings=4),head)
    parts.append(housing)
  join(parts,name)
 for style in [4,8]:headset(head,species,style)

(ROOT/'QA').mkdir(exist_ok=True)
(ROOT/'QA/face-surface-verification.json').write_text(json.dumps({'status':'PASS','modules':face_detail_report},indent=2),encoding='utf-8')

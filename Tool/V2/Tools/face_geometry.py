"""Continuous lower-face sculpt fields and topology-bound mouth seams (no mouth UV).

Executed by build_modular before modules and blend shapes are generated.
"""
mouth_surface_report=[]

def smooth01(t):
 t=max(0,min(1,t));return t*t*(3-2*t)

def chin_warp(p,kind):
 q=p.copy();x,y,z=p
 # Fade on both the neck and upper cheeks. Spatial fields also deform hair and
 # facewear, so every module keeps the same interface at additive slider extremes.
 lower=smooth01((z-1.540)/.025)*(1-smooth01((z-1.595)/.074))
 front=max(smooth01((-y-.075)/.045),.55*smooth01((z-1.58)/.035))
 w=lower*front
 if kind=='ChinRound':
  q.x+=w*(x*.14+.007*math.tanh(x/.024))
  q.z+=.008*w*math.exp(-(x/.030)**2)
  q.y-=.0025*w
 elif kind=='ChinSquare':
  q.x+=w*(x*.22+.015*math.tanh(x/.024))
  # Broaden the lower jaw and gently lower its corners, retaining a rounded
  # center and the existing cheek planes instead of stamping a rectangular box.
  q.z+=w*(.004*math.exp(-(x/.028)**2)-.007*min(1,(abs(x)/.055)**2))
  q.y-=.003*w
 return q

def geometry_mouth(head,width,z,height=.0032):
 """Cut a tapered lip seam into the skin itself; its edges are shared with skin.

 The mouth is a shallow sculpted groove with a dark material, not a decal or a
 separate surface. BMesh edge splits retain weights and all following sculpt
 keys move both sides of each common edge together.
 """
 mesh=head.data;bm=bmesh.new();bm.from_mesh(mesh)
 skin={i for i,m in enumerate(mesh.materials) if m in [M['Skin'],M['SkinShade']]}
 if M['Eye'] not in list(mesh.materials):mesh.materials.append(M['Eye'])
 dark=list(mesh.materials).index(M['Eye'])
 outline=[(-width,z),(-width*.60,z+height),(width*.60,z+height),(width,z),(width*.60,z-height),(-width*.60,z-height)]
 # Cut through skin faces only. Adjacent faces share the new edge vertices.
 for (ax,az),(bx,bz) in zip(outline,outline[1:]+outline[:1]):
  faces=[f for f in bm.faces if f.material_index in skin]
  geom=list(set(faces)|{e for f in faces for e in f.edges}|{v for f in faces for v in f.verts})
  bmesh.ops.bisect_plane(bm,geom=geom,dist=1e-7,plane_co=(ax,0,az),plane_no=(bz-az,0,ax-bx))
 bm.normal_update();seams=[]
 for f in bm.faces:
  c=f.calc_center_median()
  if f.material_index not in skin or f.normal.y>-.10 or c.y>-.06:continue
  inside=all((c.x-a)*(d-b)-(c.z-b)*(c2-a)>=-1e-8 for (a,b),(c2,d) in zip(outline,outline[1:]+outline[:1]))
  if inside:f.material_index=dark;seams.append(f)
 assert seams,('No integrated mouth surface',head.name)
 for v in bm.verts:
  if v.co.y>-.06:continue
  dx=abs(v.co.x)/width;dz=abs(v.co.z-z)/height
  if dx<1 and dz<2.8:
   # Depress the groove, raise a tiny continuous lip on either side.
   w=(1-dx*dx)**2
   v.co.y+=w*(.0014*math.exp(-dz*dz*2)-.0018*math.exp(-((dz-1.8)/.65)**2))
 mouth_edges={e for f in seams for e in f.edges}
 assert all(e.is_manifold for e in mouth_edges),('Mouth seam detached',head.name)
 mouth_surface_report.append({'head':head.name,'faces':len(seams),'sharedEdges':len(mouth_edges),'attached':True})
 # Triangulate generated n-gons deterministically to avoid exporter fan artifacts.
 bmesh.ops.triangulate(bm,faces=[f for f in bm.faces if len(f.verts)>4])
 bm.to_mesh(mesh);bm.free();mesh.update()
 return head

def mouth_warp(p,j,z=1.638,width=.045):
 q=p.copy();w=math.exp(-((p.x/(width*1.6))**4+((p.z-z)/.023)**4))
 w*=1-smooth01((p.y+.070)/.045)
 if j==1:q.x*=1+.25*w
 if j==2:q.x*=1-.23*w
 if j==3:q.z+=.005*w*(abs(p.x)/width)**1.4
 if j==4:q.z-=.005*w*(abs(p.x)/width)**1.4
 if j==5:q.z=z+(q.z-z)*(1+.42*w);q.y-=.002*w
 return q

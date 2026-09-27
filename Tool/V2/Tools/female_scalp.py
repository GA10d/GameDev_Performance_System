"""Close the authored female head's omitted scalp with a welded cranial patch.

The pack removes the scalp beneath hair. Reuse its exact boundary and skin weights,
preserving the original face; do not hide an incomplete head beneath a second shell.
"""
bm=bmesh.new();bm.from_mesh(female.data)
skin_slot=next(i for i,m in enumerate(female.data.materials) if m==M['Skin'])
edges={e for e in bm.edges if e.is_boundary and e.link_faces[0].material_index==skin_slot}
loops=[]
while edges:
 edge=edges.pop();chain=[edge.verts[0],edge.verts[1]]
 while True:
  candidates=[e for e in chain[-1].link_edges if e in edges]
  if not candidates:break
  edge=candidates[0];edges.remove(edge);chain.append(edge.other_vert(chain[-1]))
 if chain[-1]==chain[0]:chain.pop()
 loops.append(chain)
boundary=next(loop for loop in loops if min(v.co.z for v in loop)>1.60 and max(v.co.y for v in loop)>.02 and max(v.co.x for v in loop)-min(v.co.x for v in loop)>.15)
count=len(boundary);previous=boundary;origin=Vector((0,-.048,1.705));radius=Vector((.094,.103,.106));pole=Vector((0,.20,.98)).normalized()
layer=bm.verts.layers.deform.verify();head_group=female.vertex_groups.get('Head') or female.vertex_groups.new(name='Head')
def vertex(p):
 v=bm.verts.new(p);v[layer][head_group.index]=1;return v
def face(vs):
 f=bm.faces.new(vs);f.material_index=skin_slot;f.smooth=True
for step in range(1,6):
 t=step/6;current=[]
 for v in boundary:
  delta=v.co-origin;direction=Vector((delta.x/radius.x,delta.y/radius.y,delta.z/radius.z)).normalized()
  radial=direction.lerp(pole,t).normalized()
  ellipsoid=origin+Vector((radial.x*radius.x,radial.y*radius.y,radial.z*radius.z))
  # Ease from exact authored boundary to the convex scalp; no seam or duplicate rim.
  base=origin+Vector((direction.x*radius.x,direction.y*radius.y,direction.z*radius.z))
  current.append(vertex(ellipsoid+(v.co-base)*(1-t)**2))
 for i in range(count):j=(i+1)%count;face((previous[i],previous[j],current[j],current[i]))
 previous=current
tip=vertex(origin+Vector((pole.x*radius.x,pole.y*radius.y,pole.z*radius.z)))
for i in range(count):face((previous[i],previous[(i+1)%count],tip))
bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
assert all(not e.is_boundary for v in boundary for e in v.link_edges),'Scalp rim is not welded'
bm.to_mesh(female.data);bm.free()
(ROOT/'QA/WomenAlien/female-scalp.json').write_text(json.dumps({'status':'PASS','authoredBoundaryVertices':count,'weldedRim':True,'addedPatchVertices':count*5+1,'originalFacePreserved':True},indent=2),encoding='utf-8')

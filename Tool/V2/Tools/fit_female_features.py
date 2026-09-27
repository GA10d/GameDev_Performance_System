"""Seat the authored female nose/lip overlay into the underlying face.

The original glTF uses an open skin-colored feature overlay. Its coplanar rim
creates tiny forehead/philtrum lines; the lower tab intersects a geometric mouth.
Keep the nose sculpt, bury its rim, and retire only the hidden lower lip tab.
"""
bm=bmesh.new();bm.from_mesh(female.data);remaining=set(bm.verts);components=[]
while remaining:
 stack=[remaining.pop()];component=[]
 while stack:
  v=stack.pop();component.append(v)
  for e in v.link_edges:
   n=e.other_vert(v)
   if n in remaining:remaining.remove(n);stack.append(n)
 if all(female.data.materials[f.material_index]==M['Skin'] for v in component for f in v.link_faces):components.append(component)
assert len(components)==2,'Female source skin topology changed; inspect before fitting'
body=max(components,key=len);feature=min(components,key=len)
bm.verts.index_update();verts=[v.co.copy() for v in bm.verts]
faces={f for v in body for f in v.link_faces}
tree=BVHTree.FromPolygons(verts,[[v.index for v in f.verts] for f in faces])
for v in feature:
 hit,_,_,_=tree.ray_cast(Vector((v.co.x,-.5,v.co.z)),Vector((0,1,0)),1)
 assert hit is not None,'Female feature no longer overlays skin'
 w=1-smooth01((v.co.z-1.653)/.012)
 if any(e.is_boundary for e in v.link_edges):w=1
 v.co.y=v.co.y*(1-w)+(hit.y+.004)*w
# The source lower-lip tab is superseded by the fitted modeled seam. Remove its
# hidden faces rather than letting mouth/width keys pull it back through the skin.
feature_faces=list({f for v in feature for f in v.link_faces})
geom=list(set(feature_faces)|{e for f in feature_faces for e in f.edges}|set(feature))
bmesh.ops.bisect_plane(bm,geom=geom,dist=1e-7,plane_co=(0,0,1.663),plane_no=(0,0,1),clear_inner=True)
bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
bm.to_mesh(female.data);bm.free()

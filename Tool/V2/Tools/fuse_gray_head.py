"""Fuse the gray alien's skin before blend shapes: one closed, continuous shell."""
skin_parts=[alien,orb('Dome',(0,-.015,1.827),(.141,.129,.13))]
for side in [-1,1]:skin_parts.append(orb('Orbital skin',(side*.08,-.147,1.765),(.059,.051,.075)))
alien=join(skin_parts,'Head_Alien')
bpy.ops.object.select_all(action='DESELECT');alien.select_set(True);bpy.context.view_layer.objects.active=alien
# Remeshing only skin avoids melting the deliberately separate eyes and brows.
for mod in list(alien.modifiers):alien.modifiers.remove(mod)
remesh=alien.modifiers.new('Fuse cranium and face','REMESH');remesh.mode='VOXEL';remesh.voxel_size=.0035;remesh.use_smooth_shade=True
bpy.ops.object.modifier_apply(modifier=remesh.name)
smooth=alien.modifiers.new('Relax skull transition','SMOOTH');smooth.factor=1.1;smooth.iterations=10
bpy.ops.object.modifier_apply(modifier=smooth.name)
decimate=alien.modifiers.new('Low poly unified skin','DECIMATE');decimate.ratio=min(1,1700/max(1,len(alien.data.polygons)))
bpy.ops.object.modifier_apply(modifier=decimate.name)
alien.data.materials.clear();alien.data.materials.append(M['Skin'])
for p in alien.data.polygons:p.material_index=0;p.use_smooth=True
alien.vertex_groups.clear();neck=alien.vertex_groups.new(name='Neck');head=alien.vertex_groups.new(name='Head')
for v in alien.data.vertices:
 weight=max(0,min(1,(v.co.z-1.52)/.068))
 if weight:head.add([v.index],weight,'REPLACE')
 if weight<1:neck.add([v.index],1-weight,'REPLACE')
alien.modifiers.new('Quaternius skin','ARMATURE').object=rig
bm=bmesh.new();bm.from_mesh(alien.data);remaining=set(bm.verts);components=[]
while remaining:
 stack=[remaining.pop()];size=0
 while stack:
  v=stack.pop();size+=1
  for edge in v.link_edges:
   neighbor=edge.other_vert(v)
   if neighbor in remaining:remaining.remove(neighbor);stack.append(neighbor)
 components.append(size)
assert len(components)==1,('Disconnected gray skin',components)
assert all(e.is_manifold for e in bm.edges),'Gray skin is not closed'
bm.free()
(ROOT/'QA/WomenAlien').mkdir(parents=True,exist_ok=True)
(ROOT/'QA/WomenAlien/gray-topology.json').write_text(json.dumps({'status':'PASS','skinComponents':1,'closedManifold':True,'vertices':len(alien.data.vertices),'polygons':len(alien.data.polygons),'mouthGeometry':0},indent=2),encoding='utf-8')

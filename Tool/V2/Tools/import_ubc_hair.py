"""Import the actual CC0 Universal Base Characters Standard meshes into the shared rig.
Executed by build_modular.py after the human scalp helpers are defined.
"""
from mathutils import Matrix
UBC=ROOT/'Source/Downloads/UniversalBaseCharacters/Universal Base Characters[Standard]/Hairstyles/Rigged to Head Bone/FBX (Unity)'
ubc_sources={22:('Hair_Long',True),23:('Hair_Long',True),27:('Hair_Buns',True),30:('Hair_SimpleParted',False),31:('Hair_Buzzed',False),32:('Hair_BuzzedFemale',True)}
ubc_manifest=[]
for style,(source_name,female) in ubc_sources.items():
 old=bpy.data.objects.get(f'Hair_Human_{style:02d}')
 if old is not None:
  made.remove(old);bpy.data.objects.remove(old,do_unlink=True)
 before=set(bpy.data.objects);bpy.ops.import_scene.fbx(filepath=str(UBC/(source_name+'.fbx')))
 imported=set(bpy.data.objects)-before;o=next(o for o in imported if o.type=='MESH')
 original_vertices=len(o.data.vertices);original_triangles=sum(len(p.vertices)-2 for p in o.data.polygons)
 transform=o.matrix_world.copy()
 for v in o.data.vertices:
  p=transform@v.co;p.x*=-1.25;p.y=-p.y*1.13-.047;p.z+=.067 if female else .025
  if source_name=='Hair_Long' and p.z<1.775:
   # Keep the source's separate sculpted locks and irregular tips; only adapt length.
   p.z=1.775+(p.z-1.775)*(.72 if style==22 else 1.10)
  if p.z>1.73:
   center=Vector((0,-.046,1.701));ray=p-center
   hit,normal,index,distance=head_tree.ray_cast(center,ray.normalized(),.5)
   if hit is not None and ray.length<distance+.006:p=center+ray.normalized()*(distance+.006)
  v.co=p
 o.parent=None;o.matrix_world=Matrix.Identity(4)
 for mod in list(o.modifiers):o.modifiers.remove(mod)
 o.vertex_groups.clear()
 for other in imported:
  if other!=o:bpy.data.objects.remove(other,do_unlink=True)
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
 bind(o,f'Hair_Human_{style:02d}','Hair')
 # Preserve source strands/topology. The shared ASTRA material provides the palette.
 for poly in o.data.polygons:poly.use_smooth=True
 ubc_manifest.append({'id':style,'source':source_name+'.fbx','sourceVertices':original_vertices,'sourceTriangles':original_triangles,'vertices':len(o.data.vertices),'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'topologyPreserved':True})
(ROOT/'Source/ubc_hair_manifest.json').write_text(json.dumps(ubc_manifest,indent=2),encoding='utf-8')

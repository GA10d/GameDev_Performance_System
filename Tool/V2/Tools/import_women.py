"""CC0 Quaternius women: authored modules fitted offline to the common rest rig.

Imported glTF leaf bone lengths are display hints, not anatomical lengths. Retarget
joint positions with minimal rotation and longitudinal stretch only on real chains.
All runtime characters keep the existing 51-bone Humanoid and UAL animations.
"""
from mathutils import Matrix
women_names=['Adventurer','Casual','Formal','Medieval','Punk','SciFi','Soldier','Suit','Witch','Worker']
women_report=[]

def plain(n):return n.split('.')[0]

def import_woman(index,name):
 before=set(bpy.data.objects)
 bpy.ops.import_scene.gltf(filepath=str(ROOT/'Source/Downloads/UltimateModularWomen'/f'{name}.gltf'))
 imported=set(bpy.data.objects)-before
 src_rig=next(o for o in imported if o.type=='ARMATURE')
 src={b.name:src_rig.matrix_world@b.head_local for b in src_rig.data.bones}
 dst={b.name:b.head_local.copy() for b in rig.data.bones}
 def mapped(n):
  if n=='Body':return 'Hips'
  if n.startswith('Wrist'):return n.replace('Wrist','Hand')
  if n.endswith(('.L','.R')) and n[:-2] in ['Index1','Middle1','Ring1','Pinky1','Thumb1']:return 'Hand'+n[-2:]
  return n if n in dst else 'Root'
 def move(p,n):
  dest=mapped(n)
  # Metacarpals are collapsed into the existing hand, preserving their authored span.
  if dest.startswith('Hand'):
   return p+dst[dest]-src['Wrist'+dest[-2:]]
  if n=='Body':return p+dst['Hips']-src['Hips']
  children={'Hips':'Abdomen','Abdomen':'Torso','Torso':'Chest','Chest':'Neck','Neck':'Head'}
  side=n[-2:]
  for a,b in [('Shoulder','UpperArm'),('UpperArm','LowerArm'),('LowerArm','Wrist'),('UpperLeg','LowerLeg'),('LowerLeg','Foot')]:
   if n.startswith(a):children[n]=b+side
  child=children.get(n)
  if child and child in src:
   a=src[child]-src[n];b=dst[mapped(child)]-dst[dest]
   q=p-src[n];axis=a.normalized();q+=axis*q.dot(axis)*(b.length/a.length-1)
   return dst[dest]+a.rotation_difference(b)@q
  return p+dst[dest]-src[n]

 def module(part,out,selection=None):
  matches=[o for o in imported if o.type=='MESH' and o.name.split('.')[0].endswith('_'+part)]
  assert matches,(name,part,[o.name for o in imported if o.type=='MESH'])
  original=matches[0]
  o=original.copy();o.data=original.data.copy();bpy.context.collection.objects.link(o);o.name=out
  world=o.matrix_world.copy();o.parent=None;o.matrix_world=Matrix.Identity(4);o.animation_data_clear();o.modifiers.clear()
  for v in o.data.vertices:v.co=world@v.co
  bm=bmesh.new();bm.from_mesh(o.data)
  if selection:
   bad=[p for p in bm.faces if not selection(plain(o.data.materials[p.material_index].name),[v.co for v in p.verts])]
   bmesh.ops.delete(bm,geom=bad,context='FACES')
  bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
  # glTF splits vertices at hard normals; welding restores the author's actual topology.
  bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=0.00001)
  bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
  weights=[]
  for v in o.data.vertices:
   groups=[(o.vertex_groups[g.group].name,g.weight) for g in v.groups if g.weight>1e-6]
   total=sum(w for _,w in groups)
   assert total>0,(name,part,v.index,'unweighted source')
   v.co=sum((move(v.co,n)*w/total for n,w in groups),Vector())
   result={}
   for n,w in groups:result[mapped(n)]=result.get(mapped(n),0)+w/total
   weights.append(result)
  o.vertex_groups.clear()
  for n in sorted({n for w in weights for n in w}):
   group=o.vertex_groups.new(name=n)
   for i,w in enumerate(weights):
    if n in w:group.add([i],w[n],'REPLACE')
  for slot in o.material_slots:
   n=plain(slot.material.name)
   key='Skin' if n.startswith('Skin') else 'Hair' if n.startswith('Hair') else 'Dark' if n in ['Black','DarkBrown','Grey','Brown'] else 'Ivory' if n in ['White','Beige'] else 'Trim' if n in ['Gold','Worker_Yellow','Metal'] else 'Suit'
   slot.material=M[key]
  o.parent=rig;o.modifiers.new('Common Humanoid skin','ARMATURE').object=rig;made.append(o)
  return o

 for part,prefix in [('Body','Body'),('Legs','Legs'),('Feet','Feet')]:module(part,f'{prefix}_{22+index:02d}')
 # Keep authored hair/headwear, but remove source facial features and neck trim.
 def hair_region(mat,vs):
  if mat in ['Skin','Brown','Black']:return False
  # The eyebrow strips are independent components in the source. Their narrow
  # coordinate box excludes the long fringe and temple locks.
  if all(1.695<v.z<1.716 and -.174<v.y<-.139 and .016<abs(v.x)<.075 for v in vs):return False
  return True
 hair=module('Head',f'Hair_Female_{index+1:02d}',hair_region)
 for slot in hair.material_slots:
  if slot.material not in [M['Trim'],M['Dark']]:slot.material=M['Hair']
 if name=='Casual':
  global female
  female=module('Head','Head_Female',lambda m,vs:m in ['Skin','Hair_Brown','Brown'])
  # The base head has original authored skin, eye and brow topology.
  for slot in female.material_slots:
   if slot.material==M['Hair']:slot.material=M['Brow']
   elif slot.material==M['Dark']:slot.material=M['Eye']
  # Keep original skin, nose and lip sculpt. A fitted geometric seam is added below.
 women_report.append({'source':name+'.gltf','outfitId':22+index,'hairId':index+1,'rig':'shared Humanoid'})
 for o in imported:bpy.data.objects.remove(o,do_unlink=True)

for index,name in enumerate(women_names):import_woman(index,name)
exec(compile((ROOT/'Tools/female_scalp.py').read_text(encoding='utf-8'),'female_scalp.py','exec'))
exec(compile((ROOT/'Tools/fit_female_features.py').read_text(encoding='utf-8'),'fit_female_features.py','exec'))

# Preserve authored buried roots. Projecting every inner root outward produced
# the rectangular fringe and temple splinters reported in the creator. Only the
# original scalp shell receives a small outward clearance; loose locks remain
# embedded in the skull as authored.
scalp_tree=BVHTree.FromPolygons([v.co for v in female.data.vertices],
 [list(p.vertices) for p in female.data.polygons if female.data.materials[p.material_index]==M['Skin']])
for o in made:
 if not o.name.startswith('Hair_Female_'):continue
 bm=bmesh.new();bm.from_mesh(o.data)
 fit=bm.verts.layers.float.new('ScalpFit');remaining=set(bm.verts)
 while remaining:
  stack=[remaining.pop()];component=[]
  while stack:
   v=stack.pop();component.append(v)
   for e in v.link_edges:
    other=e.other_vert(v)
    if other in remaining:remaining.remove(other);stack.append(other)
  # Authored scalp shells have a long open hairline; individual locks are closed.
  boundary=[v for v in component if any(e.is_boundary for e in v.link_edges)]
  if len(boundary)>20:
   for v in component:v[fit]=1
 # Finish component traversal before topology changes invalidate BMesh handles.
 edges=[e for e in bm.edges if all(v[fit]>.5 for v in e.verts)]
 if edges:bmesh.ops.subdivide_edges(bm,edges=edges,cuts=1,use_grid_fill=True)
 fit=bm.verts.layers.float.get('ScalpFit');center=Vector((0,-.048,1.705))
 for v in bm.verts:
  if v[fit]<.5:continue
  ray=v.co-center
  hit,_,_,distance=scalp_tree.ray_cast(center,ray.normalized(),.5)
  if hit is not None and ray.length<distance+.004:
   v.co=center+ray.normalized()*(distance+.004)
 bm.to_mesh(o.data);bm.free()

# Same small modeled seam as the male creator, fitted to the authored female lips.
tree=BVHTree.FromPolygons([v.co for v in female.data.vertices],
 [list(p.vertices) for p in female.data.polygons if female.data.materials[p.material_index]==M['Skin']])
lip=orb('Female_mouth',(0,-.16,1.646),(.028,.003,.0035),'Eye',segments=12,rings=4)
for v in lip.data.vertices:
 hit,_,_,_=tree.ray_cast(Vector((v.co.x,-.5,v.co.z)),Vector((0,1,0)),1)
 assert hit is not None,'Female mouth lost skin contact'
 v.co.y=hit.y+(v.co.y+.16)+.001
female=join([female,lip],'Head_Female')

# Shared human headgear fitted to the female skull (same head joint anchor).
for srcid,dstid in [(8,11),(9,12),(11,13)]:
 original=bpy.data.objects[f'Hair_Human_{srcid:02d}']
 o=original.copy();o.data=original.data.copy();bpy.context.collection.objects.link(o);o.name=f'Hair_Female_{dstid:02d}'
 # Shape-key normals are recalculated in Unity; matching smooth base normals avoids
 # false dark triangles on rigid hat panels at the widest face/jaw settings.
 for polygon in o.data.polygons:polygon.use_smooth=True
 made.append(o)

(ROOT/'QA/WomenAlien').mkdir(parents=True,exist_ok=True)
(ROOT/'QA/WomenAlien/women-import.json').write_text(json.dumps(women_report,indent=2),encoding='utf-8')

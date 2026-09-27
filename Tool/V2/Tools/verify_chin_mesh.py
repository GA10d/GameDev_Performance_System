"""Verify actual exported sculpt continuity and collect measurable chin deltas."""
import bpy,bmesh,json,pathlib,math
from mathutils import Vector
ROOT=pathlib.Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Source/Quaternius_Astra_Modular.blend'))
reports=[]
for o in bpy.data.objects:
 if not o.name.startswith('Head_'):continue
 base=o.data.shape_keys.key_blocks['Basis'];keys=o.data.shape_keys.key_blocks
 skin={p.material_index for p in o.data.polygons if o.data.materials[p.material_index].name in ['Q_Skin','Q_SkinShade']}
 ids={i for p in o.data.polygons if p.material_index in skin for i in p.vertices}
 offset=.040 if o.name.startswith('Head_Alien_') else 0
 jaw=[i for i in ids if base.data[i].co.y<.01 and 1.56+offset<base.data[i].co.z<1.66+offset]
 assert jaw,('No jaw surface',o.name)
 report={'head':o.name,'jawVertexCount':len(jaw),'variants':{}}
 for k in ['ChinRound','ChinSquare']:
  offsets=[(keys[k].data[i].co-base.data[i].co).length for i in jaw]
  assert max(offsets)>.003,('Chin not perceptible',o.name,k)
  report['variants'][k]={'maximumDisplacementMeters':max(offsets)}
 # All shared vertex indices remain shared for every blend shape, including the
 # skin/geometry-mouth boundary. Check finite coordinates and flipped local faces
 # across all new jaw variants plus both supported slider extremes.
 o.data.calc_loop_triangles();flips=0;checks=0
 for key in ['ChinRound','ChinSquare']:
  for extreme in ['Wide','Narrow']:
   coordinates=[v.co+(keys[key].data[v.index].co-v.co)+(keys['Width'+extreme].data[v.index].co-v.co)+(keys['Jaw'+extreme].data[v.index].co-v.co) for v in o.data.vertices]
   assert all(math.isfinite(c) for p in coordinates for c in p)
   for tri in o.data.loop_triangles:
    if not set(tri.vertices)&set(jaw):continue
    a,b,c=[base.data[i].co for i in tri.vertices];normal=(b-a).cross(c-a)
    a,b,c=[coordinates[i] for i in tri.vertices];new=(b-a).cross(c-a)
    if normal.length<1e-10:continue
    checks+=1
    if new.length<1e-10 or new.dot(normal)<0:
     flips+=1;print('TRIANGLE_REVIEW',o.name,key,extreme,list(tri.vertices),[list(base.data[i].co) for i in tri.vertices],normal.length,new.length,new.dot(normal))
 report['triangleChecks']=checks;report['flippedTriangles']=flips
 assert flips==0,report
 reports.append(report)
(ROOT/'QA/ChinHair/mesh-verification.json').write_text(json.dumps({'status':'PASS','heads':reports},indent=2))
print('CHIN_MESH_PASS',len(reports),sum(r['triangleChecks'] for r in reports))

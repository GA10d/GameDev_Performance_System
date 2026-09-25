"""Run in Blender: --python batch_prepare.py -- --manifest assets.json --output Prepared.
Local, manifest-driven conversion. Originals are never overwritten. No network downloader.
"""
import argparse, bpy, hashlib, json, sys
from pathlib import Path

def main():
    args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
    p=argparse.ArgumentParser();p.add_argument('--manifest',required=True);p.add_argument('--output',required=True);p.add_argument('--allow-skinned-decimation',action='store_true')
    options=p.parse_args(args);manifest=Path(options.manifest).resolve();output=Path(options.output).resolve();output.mkdir(parents=True,exist_ok=True)
    entries=json.loads(manifest.read_text(encoding='utf-8-sig'));reports=[]
    for entry in entries:
        source=(manifest.parent/entry['path']).resolve()
        if not entry.get('license') or not entry.get('author') or not entry.get('source_url'):
            raise ValueError('Asset provenance is incomplete: '+str(source))
        if not source.is_file():raise FileNotFoundError(source)
        if output==source.parent or output in source.parents:raise ValueError('Output must be separate from the input directory')
        name=entry['id']
        if not name or any(c not in 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-' for c in name):raise ValueError('Use a safe unique ASCII id')
        asset_output=output/name
        if asset_output.exists():raise FileExistsError('Refusing to overwrite previous conversion: '+str(asset_output))
        asset_output.mkdir()
        bpy.ops.wm.read_factory_settings(use_empty=True)
        suffix=source.suffix.lower()
        if suffix=='.blend':bpy.ops.wm.open_mainfile(filepath=str(source))
        elif suffix=='.fbx':bpy.ops.import_scene.fbx(filepath=str(source))
        elif suffix=='.obj':bpy.ops.wm.obj_import(filepath=str(source))
        elif suffix in ['.gltf','.glb']:bpy.ops.import_scene.gltf(filepath=str(source))
        else:raise ValueError('Unsupported input: '+suffix)
        report=dict(entry);report['sha256']=hashlib.sha256(source.read_bytes()).hexdigest();report['meshes']=[]
        for ob in list(bpy.context.scene.objects):
            if ob.type in ['LIGHT','CAMERA']:
                bpy.data.objects.remove(ob,do_unlink=True);continue
            if ob.type!='MESH':continue
            before=sum(len(f.vertices)-2 for f in ob.data.polygons);notes=[]
            ratio=max(.05,min(1,float(entry.get('ratio',1))))
            skinned=any(m.type=='ARMATURE' for m in ob.modifiers)
            if ratio<1 and not ob.data.shape_keys and (not skinned or options.allow_skinned_decimation):
                bpy.context.view_layer.objects.active=ob
                mod=ob.modifiers.new('Preparation decimate','DECIMATE');mod.ratio=ratio;mod.use_collapse_triangulate=True
                bpy.ops.object.modifier_apply(modifier=mod.name)
                if skinned:notes.append('Skinned decimation opted in: manually inspect deformation and weight seams.')
            elif ratio<1:notes.append('Decimation skipped: shape keys or protected skinned mesh.')
            if entry.get('flat_normals',False):
                for f in ob.data.polygons:f.use_smooth=False
            after=sum(len(f.vertices)-2 for f in ob.data.polygons)
            report['meshes'].append({'name':ob.name,'triangles_before':before,'triangles_after':after,'material_slots':len(ob.data.materials),'skinned':skinned,'notes':notes})
        bpy.ops.object.select_all(action='SELECT')
        bpy.ops.wm.save_as_mainfile(filepath=str(asset_output/(name+'.blend')))
        bpy.ops.export_scene.fbx(filepath=str(asset_output/(name+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE','EMPTY'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
        report['notes']='No auto-retopology, texture baking, rig retarget or artistic approval. Animation export is off; keep original animation files.'
        (asset_output/'audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8');reports.append(report)
    (output/'batch_report.json').write_text(json.dumps(reports,ensure_ascii=False,indent=2),encoding='utf8')
    print('PREPARED',len(reports),'assets')

if __name__=='__main__':main()

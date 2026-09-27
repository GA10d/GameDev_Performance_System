using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceToolV2;
public static class ToolV2QA
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static void Verify(ToolPackage package)
    {
        var failures=ToolValidation.Package(package);Require(failures.Count==0,string.Join("\n",failures));
        var lines=new List<string>();int poses=0,vertices=0;
        Require(package.library.characters.Count(c=>c.look.creatorEnabled)>=32,"32 creator presets missing");
        var catalog=package.library.characters[0].catalog;
        Require(catalog.clips.All(c=>c.humanMotion),"Non-Humanoid animation found");
        foreach(var character in package.library.characters)
        {
            var actor=ToolCharacterView.Create(character,null);var motion=actor.GetComponent<ToolMotion>();
            var bones=actor.GetComponentsInChildren<Transform>(true);var scales=bones.Select(b=>b.localScale).ToArray();
            var renderers=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).ToArray();
            Require(renderers.Length>=4,"Incomplete modules "+character.id);
            if(character.look.creatorEnabled)
            {
                Require(renderers.Count(r=>r.name.StartsWith("Head_"))==1,"Duplicate or missing head");
                Require(renderers.Count(r=>r.name.StartsWith("Body_"))==1,"Duplicate or missing body");
                Require(renderers.Count(r=>r.name.StartsWith("Legs_"))==1,"Duplicate or missing legs");
                Require(renderers.Count(r=>r.name.StartsWith("Feet_"))==1,"Duplicate or missing feet");
                foreach(var r in renderers.Where(r=>r.name.StartsWith("Legs_")||r.name.StartsWith("Feet_")))
                    Require(r.sharedMesh.vertices.Min(v=>v.x)<-.0001f&&r.sharedMesh.vertices.Max(v=>v.x)>.0001f,"Missing mirrored limb: "+r.name);
                Require(renderers.First(r=>r.name.StartsWith("Head_")).sharedMesh.blendShapeCount>=35,"Face shapes not imported");
            }
            var baked=new Mesh();
            foreach(var clip in catalog.clips)
            foreach(float fraction in new[]{.05f,.42f,.8f})
            {
                motion.Sample(clip.name,clip.length*fraction);poses++;
                for(int i=0;i<bones.Length;i++)Require((bones[i].localScale-scales[i]).sqrMagnitude<.00001f,"Bone scaling regression "+bones[i].name);
                foreach(var renderer in renderers)
                {
                    renderer.BakeMesh(baked,true);
                    foreach(var v in baked.vertices)
                    {
                        Vector3 world=renderer.transform.TransformPoint(v);vertices++;
                        if(float.IsNaN(world.x)||float.IsInfinity(world.x)||float.IsNaN(world.y)||float.IsNaN(world.z))throw new Exception("Invalid skinned vertex "+character.id+" "+clip.name);
                        if(world.sqrMagnitude>=36)throw new Exception("Exploding mesh "+character.id+" "+renderer.name+" "+clip.name+" "+world);
                    }
                }
            }
            motion.Sample("Idle_Loop",.3f);
            Vector3 head1=motion.Animator.GetBoneTransform(HumanBodyBones.Head).position;
            motion.Sample("Dance_Loop",.7f);motion.Sample("Idle_Loop",.3f);
            Require((head1-motion.Animator.GetBoneTransform(HumanBodyBones.Head).position).magnitude<.001f,"Timeline seek is not deterministic");
            UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(actor);
            lines.Add("PASS "+character.id+": modules, skinning, fixed bone scales, "+catalog.clips.Length+" real clips × 3 samples, seek replay");
            Debug.Log("V2_POSES_PASS "+character.id);
        }
        // Hair IDs remain append-only: legacy short crop is still 10, full helmet is still 11.
        Require(ToolCreatorOptions.HumanHair[10]=="短寸"&&ToolCreatorOptions.HumanHair[11]=="航行头盔","Legacy hair IDs changed");
        foreach(var species in new[]{ToolSpecies.Human,ToolSpecies.StandingAlien})
        {
            // The hat-category zero is a remove-hat command, not a second bald hairstyle.
            var ids=Enumerable.Range(0,ToolCreatorOptions.HairCategories(species).Length).SelectMany(i=>ToolCreatorOptions.HairIds(species,i).Where(id=>!(species==ToolSpecies.Human&&i==3&&id==0))).OrderBy(i=>i).ToArray();
            Require(ids.SequenceEqual(Enumerable.Range(0,ToolCreatorOptions.Hair(species).Length)),"Hair menu missing or duplicate IDs");
            var look=new ToolCharacterLook{hairStyle=ToolCreatorOptions.Hair(species).Length-1};ToolCreatorOptions.Normalize(look,species);
            Require(look.hairStyle==ToolCreatorOptions.Hair(species).Length-1,"New hair clamped to old range");
        }
        // Exercise every selectable module and all bounded facial/body extremes, independently of presets.
        foreach(var species in new[]{ToolSpecies.Human,ToolSpecies.StandingAlien})
        {
            var c=ToolV2CreatorBuild.NewCharacter(package,species);var actor=ToolCharacterView.Create(c,null);
            for(int i=0;i<ToolCreatorOptions.Hair(species).Length;i++)
            {
                c.look.hairStyle=i;c.look.outfitStyle=i%ToolCreatorOptions.Outfits.Length;c.look.legStyle=(i+2)%ToolCreatorOptions.Outfits.Length;c.look.footStyle=(i+4)%ToolCreatorOptions.Outfits.Length;
                c.look.faceAccessory=i%10;c.look.gearAccessory=i%10;c.look.facialMark=i%6;
                c.look.faceShape=i%10;c.look.eyeShape=i%8;c.look.browShape=i%6;c.look.noseShape=i%6;c.look.mouthShape=i%6;c.look.bodyType=i%8;
                c.look.faceWidth=i%2==0?.92f:1.08f;c.look.jawWidth=i%2==0?-1:1;c.look.bodyScale=i%2==0?.9f:1.1f;ToolCharacterView.ApplyLook(actor,c);
                var active=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).ToArray();
                Require(i==0||active.Any(r=>r.name=="Hair_"+(species==ToolSpecies.Human?"Human":"Alien")+"_"+i.ToString("00")),"Missing hair module");
                foreach(var renderer in active)Require(renderer.sharedMaterials.All(m=>m&&m.shader.name=="Astra/ToolV2/Character"),"Material did not use ASTRA shader");
                foreach(string action in new[]{"Idle_Talking_Loop","Fixing_Kneeling","Sitting_Talking_Loop","Hit_Head","Dance_Loop"})
                {
                    actor.GetComponent<ToolMotion>().Sample(action,.6f);
                    var boundsCache=actor.GetComponent<ToolPortraitBounds>();if(!boundsCache)boundsCache=actor.AddComponent<ToolPortraitBounds>();
                    Require(boundsCache.TryGetBounds(out Bounds portrait),"Portrait bounds missing");
                    var head=actor.GetComponent<ToolMotion>().Animator.GetBoneTransform(HumanBodyBones.Head);
                    Require((portrait.center-head.position).magnitude<.6f&&portrait.size.sqrMagnitude<3&&portrait.size.y>.1f,"Portrait scale/seek regression: "+species+" hair "+i+" "+action+" "+portrait);
                }
            }
            UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(c);
        }
        // Prefab export/reload restores property blocks and retains serialized skin/shape choices.
        var preset=package.library.characters.First(c=>c.look.creatorEnabled);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ToolV2Build.Root+"/Characters/Prefabs/"+preset.id+".prefab");
        Require(prefab&&prefab.GetComponent<ToolCharacterInstance>().preset==preset,"Prefab preset reference lost");
        var instance=UnityEngine.Object.Instantiate(prefab);ToolCharacterView.ApplyLook(instance,preset);
        Require(instance.GetComponent<ToolMotion>().catalog==catalog,"Prefab motion catalog lost");UnityEngine.Object.DestroyImmediate(instance);
        var bad=ScriptableObject.CreateInstance<ToolUnit>();bad.duration=4;bad.actions.Add(new ToolActionSpan{actorId="missing",actionId="Walk_Loop",duration=2});
        Require(ToolValidation.Unit(bad,package.library).Any(e=>e.Contains("未被人物轨")),"Missing actor coverage accepted");UnityEngine.Object.DestroyImmediate(bad);
        var state=new Dictionary<string,int>{{"credits",1}};var pay=package.graph.Node("A").unit.dialogue.Last().choices[0];
        Require(!ToolLogic.Test(pay.condition,state),"Choice affordability regression");state["credits"]=3;ToolLogic.Apply(pay.effects,state);Require(state["credits"]==1,"Choice resource regression");
        Require(ToolLogic.Exit(package.graph.Node("A"),"silence")=="C","Silence route regression");
        // Save the same draft twice: keep GUID, reject duplicate IDs, then remove only this generated test record.
        var savedCharacters=package.library.characters;var draft=ToolV2CreatorBuild.NewCharacter(package,ToolSpecies.Human);draft.id="qa_"+Guid.NewGuid().ToString("N");
        ToolCharacter saved=null;string savedPath=null,prefabPath=ToolV2Build.Root+"/Characters/Prefabs/"+draft.id+".prefab";
        try
        {
            saved=ToolV2CreatorBuild.SaveCharacter(draft,package);savedPath=AssetDatabase.GetAssetPath(saved);string guid=AssetDatabase.AssetPathToGUID(savedPath);
            draft.look.hairStyle=29;draft.look.faceShape=9;draft.look.skin=new Color(.3f,.2f,.17f);ToolV2CreatorBuild.SaveCharacter(draft,package,saved);
            Require(saved.look.hairStyle==29&&saved.look.faceShape==9&&saved.look.skin==draft.look.skin&&AssetDatabase.AssetPathToGUID(savedPath)==guid,"Saved draft did not update in place");
            bool rejected=false;try{ToolV2CreatorBuild.SaveCharacter(draft,package);}catch(ArgumentException){rejected=true;}Require(rejected,"Duplicate character ID accepted");
        }
        finally
        {
            package.library.characters=savedCharacters;EditorUtility.SetDirty(package.library);AssetDatabase.SaveAssets();
            if(savedPath!=null)AssetDatabase.DeleteAsset(savedPath);AssetDatabase.DeleteAsset(prefabPath);UnityEngine.Object.DestroyImmediate(draft);
        }
        lines.Add("PASS creator save/update keeps GUID, color and face; duplicate IDs rejected; temporary QA assets removed");
        lines.Add("PASS all registered modular clothing/hair selectable; 45 combined extrema; all hair categories/IDs; exported prefab references; graph and choice checks");
        lines.Add("TOTAL poses="+poses+" vertices="+vertices+" clips="+catalog.clips.Length);
        string folder=Path.GetFullPath(Application.dataPath+"/../../QA");Directory.CreateDirectory(folder);File.WriteAllLines(folder+"/tool-v2-verification.txt",lines);
        Debug.Log("TOOL_V2_VERIFY_OK poses="+poses+" vertices="+vertices);
    }
}

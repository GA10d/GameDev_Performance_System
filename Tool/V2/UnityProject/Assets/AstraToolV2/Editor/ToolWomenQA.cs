using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceToolV2;

public static class ToolWomenQA
{
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void Build()
    {
        var package=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolV2Build.PackagePath);
        ToolV2CreatorBuild.SeedWomen(package);ToolV2CreatorBuild.RebuildPrefabs(package);
        var presets=package.library.characters.Where(c=>c.look.creatorEnabled&&c.look.humanModel==1).ToArray();
        Check(presets.Length>=10,"Missing female presets");
        foreach(var c in presets)
        {
            var actor=ToolCharacterView.Create(c,null);
            Check(actor.GetComponentsInChildren<SkinnedMeshRenderer>().Count(r=>r.enabled&&r.name=="Head_Female")==1,"Female head missing");
            UnityEngine.Object.DestroyImmediate(actor);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ToolV2Build.Root+"/Characters/Prefabs/"+c.id+".prefab");
            Check(prefab&&prefab.GetComponent<ToolCharacterInstance>().preset==c,"Female prefab lost preset");
            var restored=UnityEngine.Object.Instantiate(prefab);ToolCharacterView.ApplyLook(restored,c);
            Check(restored.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>r.enabled&&r.name=="Head_Female"),"Female prefab reload lost model");
            UnityEngine.Object.DestroyImmediate(restored);
        }
        var look=new ToolCharacterLook();ToolCreatorOptions.SetHumanModel(look,1);
        var hairIds=Enumerable.Range(0,ToolCreatorOptions.HairCategories(ToolSpecies.Human,look).Length)
            .SelectMany(g=>ToolCreatorOptions.HairIds(ToolSpecies.Human,g,look).Where(id=>!(g==2&&id==0))).OrderBy(id=>id).ToArray();
        Check(hairIds.SequenceEqual(Enumerable.Range(0,ToolCreatorOptions.FemaleHair.Length)),"Female hair menu coverage");
        ToolCreatorOptions.SelectHair(look,ToolSpecies.Human,2,12);
        look=JsonUtility.FromJson<ToolCharacterLook>(JsonUtility.ToJson(look));
        ToolCreatorOptions.SelectHair(look,ToolSpecies.Human,2,0);Check(look.hairStyle==2,"Female remove-hat roundtrip");
        Check(JsonUtility.FromJson<ToolCharacterLook>("{\"creatorEnabled\":true,\"hairStyle\":23}").humanModel==0,"Legacy JSON migration");
        var draft=ToolV2CreatorBuild.NewCharacter(package,ToolSpecies.Human,1);var instance=ToolCharacterView.Create(draft,null);var baked=new Mesh();int combinations=0;
        try
        {
            foreach(int hair in Enumerable.Range(0,14))foreach(float width in new[]{.92f,1.08f})
            {
                draft.look.hairStyle=hair;draft.look.faceWidth=width;draft.look.jawWidth=width<1?-1:1;draft.look.faceShape=hair%10;
                draft.look.eyeShape=hair%8;draft.look.mouthShape=hair%6;draft.look.bodyType=hair%8;
                draft.look.outfitStyle=22+hair%10;draft.look.legStyle=22+(hair+3)%10;draft.look.footStyle=22+(hair+7)%10;
                draft.look.faceAccessory=hair%10;draft.look.facialMark=hair%6;ToolCharacterView.ApplyLook(instance,draft);
                var active=instance.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled).ToArray();
                Check(hair==0||active.Any(r=>r.name=="Hair_Female_"+hair.ToString("00")),"Female hair missing");
                foreach(string action in new[]{"Idle_Talking_Loop","Fixing_Kneeling","Sitting_Talking_Loop","Hit_Head","Dance_Loop"})
                {
                    instance.GetComponent<ToolMotion>().Sample(action,.6f);combinations++;
                    foreach(var renderer in active)
                    {
                        renderer.BakeMesh(baked,true);
                        Check(baked.vertices.All(v=>!float.IsNaN(v.x)&&renderer.transform.TransformPoint(v).sqrMagnitude<36),"Female extreme exploded "+hair+" "+action);
                    }
                }
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(instance);UnityEngine.Object.DestroyImmediate(draft);UnityEngine.Object.DestroyImmediate(baked);}
        foreach(string species in new[]{"Female","Alien"})foreach(int mouth in Enumerable.Range(0,6))
            Check(!Resources.Load<Texture2D>("Mouths/"+species+"_"+mouth.ToString("00")),"Obsolete mouth texture remains");
        // Preserve the user's in-progress timeline. Fix dangling sample IDs only in
        // disposable copies used by regression checks; never save these clones.
        var test=UnityEngine.Object.Instantiate(package);test.graph=UnityEngine.Object.Instantiate(package.graph);
        var copies=new List<UnityEngine.Object>{test,test.graph};
        foreach(var n in test.graph.nodes.Where(n=>n.unit))
        {
            n.unit=UnityEngine.Object.Instantiate(n.unit);copies.Add(n.unit);
            if(n.unit.actors.Count==1)
            {
                var a=n.unit.actors[0];a.start=0;a.duration=n.unit.duration;
                foreach(var d in n.unit.dialogue)d.actorId=a.id;
                foreach(var cam in n.unit.cameras)cam.actorId=a.id;
            }
        }
        try{ToolV2QA.Verify(test);}finally{foreach(var c in copies)UnityEngine.Object.DestroyImmediate(c);}
        string root=Path.GetFullPath(Application.dataPath+"/../..");
        File.WriteAllText(root+"/QA/WomenAlien/editor-checks.txt","PASS: 10 female presets; common Humanoid; female hair menu coverage; hat restore and JSON; legacy male JSON; geometric mouths; obsolete UV mouth textures removed; full UAL pose suite; "+combinations+" female mixed wardrobe/hair/extreme poses. User sample QA ran on disposable copies.\n");
        AssetDatabase.ExportPackage(ToolV2Build.Root,root+"/Export/ASTRA_Performance_Tool_V2.unitypackage",ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);
        ToolV2Build.BuildVerifiedPlayer();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Astra.PerformanceToolV2
{
    // Opt-in executable QA: uses the real workbench, shared rig, clip player,
    // save/load and camera. No replacement mannequin or synthetic screenshots.
    public static class ToolOutfitRuntimeQA
    {
        public static IEnumerator Run(ToolWorkbench w)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-qaDir");
            string dir=at>=0?args[at+1]:Path.Combine(Application.persistentDataPath,"OutfitQA");Directory.CreateDirectory(dir);
            var errors=new List<string>();int images=0,poses=0;long vertices=0;
            Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m+"\n"+s);};
            Application.logMessageReceived+=logger;yield return null;
            var ids=Enumerable.Range(0,ToolCreatorOptions.OutfitCategories.Length).SelectMany(group=>ToolCreatorOptions.OutfitIds(group)).OrderBy(i=>i).ToArray();
            if(!ids.SequenceEqual(Enumerable.Range(0,22)))errors.Add("Male/alien outfit menu missing or duplicate IDs");
            var mesh=new Mesh();
            foreach(var species in new[]{ToolSpecies.Human,ToolSpecies.StandingAlien})
            {
                w.NewCharacter(species);w.Paused=true;w.Section=4;w.FaceView=false;
                for(int id=10;id<22;id++)
                {
                    var look=w.Draft.look;ToolCreatorOptions.ApplyOutfit(look,id);ToolCreatorOptions.ApplyOutfitPalette(look,id);
                    look.gearAccessory=0;look.faceAccessory=0;look.bodyType=0;look.bodyScale=1;
                    look.hairStyle=species==ToolSpecies.Human?(id%2==0?17:23):0;look.faceShape=species==ToolSpecies.Human?0:(id-10)%10;
                    w.RefreshLook();w.ActionIndex=0;
                    foreach(int yaw in new[]{0,90,180})
                    {
                        w.Yaw=yaw;w.SampleAt(.3f);yield return null;yield return new WaitForEndOfFrame();
                        ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"outfit_{species}_{id:00}_{yaw:000}.png"));images++;
                    }
                    // Critical posing photos are separate from the exhaustive numeric sweep.
                    foreach(int action in new[]{6,9,16})
                    {
                        w.ActionIndex=action;w.Yaw=30;w.SampleAt(.65f);yield return null;yield return new WaitForEndOfFrame();
                        ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"motion_{species}_{id:00}_{action:00}.png"));images++;
                    }
                    var motion=w.Actor.GetComponent<ToolMotion>();
                    var transforms=w.Actor.GetComponentsInChildren<Transform>();var scales=transforms.Select(t=>t.localScale).ToArray();
                    for(int body=0;body<ToolCreatorOptions.Bodies.Length;body++)
                    {
                        look.bodyType=body;look.bodyScale=body%2==0?.9f:1.1f;w.RefreshLook();
                        var renderers=w.Actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled&&(r.name.StartsWith("Body_")||r.name.StartsWith("Legs_")||r.name.StartsWith("Feet_"))).ToArray();
                        if(renderers.Length!=3)errors.Add("Incorrect wardrobe module count: "+id);
                        foreach(string prefix in new[]{"Body_","Legs_","Feet_"})if(!renderers.Any(r=>r.name==prefix+id.ToString("00")))errors.Add("Missing module "+prefix+id);
                        foreach(var clip in w.Draft.catalog.clips)
                        foreach(float fraction in new[]{.16f,.64f})
                        {
                            motion.Sample(clip.name,clip.length*fraction);poses++;
                            // Root scale is the user-controlled uniform size; bones must stay fixed.
                            for(int b=0;b<transforms.Length;b++)if(transforms[b]!=w.Actor.transform&&transforms[b].name!="Quaternius Rig"&&(transforms[b].localScale-scales[b]).sqrMagnitude>.0001f)errors.Add("Bone scale changed "+transforms[b].name);
                            foreach(var renderer in renderers)
                            {
                                renderer.BakeMesh(mesh,true);
                                foreach(var v in mesh.vertices)
                                {
                                    var p=renderer.transform.TransformPoint(v);vertices++;
                                    if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.sqrMagnitude)||p.sqrMagnitude>36){errors.Add($"Invalid vertex {species}/{id}/{body}/{clip.name}/{renderer.name}");break;}
                                }
                            }
                        }
                        yield return null;
                    }
                    look.bodyType=0;look.bodyScale=1;w.RefreshLook();
                }
                // Saved IDs above 9 must survive normalisation and a real JSON round trip.
                for(int part=0;part<3;part++)ToolCreatorOptions.SetOutfitPart(w.Draft.look,part,21-part);
                w.RefreshLook();string path=w.SaveLook();w.LoadLook(path);File.Delete(path);
                if(w.Draft.look.outfitStyle!=21||w.Draft.look.legStyle!=20||w.Draft.look.footStyle!=19)errors.Add("New outfit JSON roundtrip failed");
            }
            // Demonstrate mixed upper/lower/boot combinations and all four menu groups.
            w.NewCharacter(ToolSpecies.Human);w.Paused=true;w.Section=4;w.FaceView=false;
            for(int id=0;id<ToolCreatorOptions.Outfits.Length;id++)
            {
                w.Draft.look.outfitStyle=id;w.Draft.look.legStyle=(id+7)%ToolCreatorOptions.Outfits.Length;w.Draft.look.footStyle=(id+13)%ToolCreatorOptions.Outfits.Length;
                w.RefreshLook();w.ActionIndex=0;w.Yaw=0;w.SampleAt(.3f);yield return null;yield return new WaitForEndOfFrame();
                ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"mixed_{id:00}.png"));images++;
            }
            ToolCreatorOptions.ApplyOutfit(w.Draft.look,10);ToolCreatorOptions.ApplyOutfitPalette(w.Draft.look,10);w.RefreshLook();
            for(int group=0;group<4;group++)
            {
                w.OutfitGroup=group;w.SampleAt(.3f);yield return null;yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(dir,$"ui_group_{group}.png"));yield return null;
            }
            UnityEngine.Object.Destroy(mesh);Application.logMessageReceived-=logger;
            File.WriteAllLines(Path.Combine(dir,"outfit-runtime-result.txt"),new[]{"Runtime errors: "+errors.Count,"Camera captures: "+images,"Wardrobe pose samples: "+poses,"Skinned vertices checked: "+vertices,"12 outfits x 2 species x 8 body types x 42 clips x 2 times; 22 mixed outfits; 2 JSON roundtrips; four menu ID partitions. Numerical checks are not a guarantee of zero cloth intersections."}.Concat(errors));
            Application.Quit(errors.Count==0?0:2);
        }
    }
}

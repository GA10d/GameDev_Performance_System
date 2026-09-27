using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Astra.PerformanceToolV2
{
    public static class ToolAlienRuntimeQA
    {
        public static IEnumerator Run(ToolWorkbench workbench)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-qaDir");
            string dir=at>=0?args[at+1]:Path.Combine(Application.persistentDataPath,"AlienQA");Directory.CreateDirectory(dir);
            var errors=new List<string>();Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m+"\n"+s);};
            Application.logMessageReceived+=logger;int captures=0;
            yield return null;workbench.NewCharacter(ToolSpecies.StandingAlien);workbench.Paused=true;
            workbench.Draft.look.skin=new Color(.39f,.54f,.47f);workbench.Draft.look.accent=new Color(.62f,.43f,.25f);workbench.Draft.look.eyes=new Color(.035f,.055f,.045f);
            workbench.Draft.look.hair=workbench.Draft.look.skin;
            workbench.Draft.look.suit=new Color(.28f,.34f,.29f);workbench.Draft.look.faceAccessory=0;workbench.Draft.look.facialMark=0;
            for(int face=0;face<10;face++)
            {
                workbench.Draft.look.faceShape=face;workbench.Draft.look.hairStyle=0;workbench.Section=1;workbench.RefreshLook();
                var heads=workbench.Actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Head_")).ToArray();
                string expected=face==0?"Head_Alien":"Head_Alien_"+face.ToString("00");
                if(heads.Length!=1||heads[0].name!=expected)errors.Add("Wrong anatomical head: "+face);
                foreach(int yaw in new[]{0,70,180})
                {
                    workbench.Yaw=yaw;workbench.FaceView=true;workbench.SampleAt(.4f);yield return null;yield return new WaitForEndOfFrame();
                    CheckHead(workbench,errors,$"head {face} yaw {yaw}");
                    ToolRuntimeQA.CaptureCamera(workbench.lens,Path.Combine(dir,$"head_{face:00}_{yaw:000}.png"));captures++;
                }
                for(int crest=0;crest<12;crest++)
                {
                    workbench.Draft.look.hairStyle=crest;workbench.Section=2;workbench.RefreshLook();workbench.Yaw=25;workbench.SampleAt(.4f);
                    yield return null;yield return new WaitForEndOfFrame();
                    CheckHead(workbench,errors,$"face {face} crest {crest}");
                    ToolRuntimeQA.CaptureCamera(workbench.lens,Path.Combine(dir,$"combo_{face:00}_{crest:00}.png"));captures++;
                    if(face==0)
                    foreach(int yaw in new[]{0,70,180})
                    {
                        workbench.Yaw=yaw;workbench.SampleAt(.4f);yield return null;yield return new WaitForEndOfFrame();
                        CheckHead(workbench,errors,$"crest {crest} yaw {yaw}");
                        ToolRuntimeQA.CaptureCamera(workbench.lens,Path.Combine(dir,$"crest_{crest:00}_{yaw:000}.png"));captures++;
                    }
                }
            }
            for(int face=0;face<10;face++)
            foreach(float width in new[]{.92f,1.08f})
            foreach(int action in new[]{6,9,16})
            {
                var l=workbench.Draft.look;l.faceShape=face;l.hairStyle=(face+action)%12;l.faceWidth=width;l.jawWidth=width<1?-1:1;l.bodyScale=width<1?.9f:1.1f;
                workbench.RefreshLook();workbench.FaceView=true;workbench.ActionIndex=action;workbench.Yaw=60;workbench.SampleAt(.6f);
                yield return null;yield return new WaitForEndOfFrame();CheckHead(workbench,errors,$"extreme {face} {action} {width}");
                ToolRuntimeQA.CaptureCamera(workbench.lens,Path.Combine(dir,$"extreme_{face:00}_{action:00}_{width:F2}.png"));captures++;
            }
            workbench.Draft.look.faceShape=9;workbench.Draft.look.hairStyle=11;workbench.RefreshLook();
            string path=workbench.SaveLook();workbench.LoadLook(path);File.Delete(path);
            if(workbench.Draft.look.faceShape!=9||workbench.Draft.look.hairStyle!=11)errors.Add("Alien head/crown JSON roundtrip mismatch");
            workbench.NewCharacter(ToolSpecies.Human);workbench.Paused=true;
            foreach(int hair in new[]{22,23,27,30,31,32})
            {
                workbench.Draft.look.hairStyle=hair;workbench.Section=2;workbench.RefreshLook();workbench.ActionIndex=0;
                foreach(int yaw in new[]{0,70,180})
                {
                    workbench.Yaw=yaw;workbench.FaceView=true;workbench.SampleAt(.4f);yield return null;yield return new WaitForEndOfFrame();
                    CheckHead(workbench,errors,$"UBC {hair} yaw {yaw}");
                    ToolRuntimeQA.CaptureCamera(workbench.lens,Path.Combine(dir,$"ubc_{hair:00}_{yaw:000}.png"));captures++;
                }
                foreach(int action in new[]{6,9,16})
                {
                    workbench.ActionIndex=action;workbench.Yaw=70;workbench.SampleAt(.6f);yield return null;yield return new WaitForEndOfFrame();
                    CheckHead(workbench,errors,$"UBC {hair} action {action}");
                    ToolRuntimeQA.CaptureCamera(workbench.lens,Path.Combine(dir,$"ubc_motion_{hair:00}_{action:00}.png"));captures++;
                }
            }
            workbench.Draft.look.hairStyle=32;path=workbench.SaveLook();workbench.LoadLook(path);File.Delete(path);
            if(workbench.Draft.look.hairStyle!=32)errors.Add("UBC hairstyle 32 JSON roundtrip mismatch");
            // Render actual OnGUI menus and the back/full-body orbit, not only camera textures.
            workbench.NewCharacter(ToolSpecies.StandingAlien);workbench.Paused=true;workbench.Draft.look.faceShape=4;
            for(int group=0;group<3;group++)
            {
                workbench.Draft.look.hairStyle=new[]{1,4,8}[group];workbench.Section=2;workbench.Subsection=group;
                workbench.RefreshLook();workbench.Yaw=20;workbench.SampleAt(.4f);
                yield return null;yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(dir,$"ui_alien_crowns_{group}.png"));captures++;yield return null;
            }
            workbench.Section=1;workbench.SampleAt(.4f);yield return null;yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"ui_alien_anatomy.png"));captures++;yield return null;
            workbench.NewCharacter(ToolSpecies.Human);workbench.Paused=true;workbench.Draft.look.hairStyle=23;
            workbench.Section=2;workbench.RefreshLook();workbench.SampleAt(.4f);yield return null;yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"ui_human_long_hair.png"));captures++;yield return null;
            workbench.FaceView=false;
            foreach(int yaw in new[]{0,90,180,270})
            {
                workbench.Yaw=yaw;workbench.SampleAt(.4f);yield return null;yield return new WaitForEndOfFrame();
                CheckHead(workbench,errors,$"full body yaw {yaw}");
                ToolRuntimeQA.CaptureCamera(workbench.lens,Path.Combine(dir,$"full_body_{yaw:000}.png"));captures++;
            }
            Application.logMessageReceived-=logger;
            File.WriteAllLines(Path.Combine(dir,"alien-runtime-result.txt"),new[]{"Runtime errors: "+errors.Count,"Captured views: "+captures,"10 anatomical heads x 3 angles; 12 crowns x 3 angles; all 120 head/crown pairs; 60 extreme action views; 36 UBC hair views; 5 actual menu screens; 4 full-body angles; alien and human JSON roundtrips; portrait corner bounds and stage occlusion regression"}.Concat(errors));
            Application.Quit(errors.Count==0?0:2);
        }
        static void CheckHead(ToolWorkbench workbench,List<string> errors,string label)
        {
            var head=workbench.Actor.GetComponent<ToolMotion>().Animator.GetBoneTransform(HumanBodyBones.Head);
            var p=workbench.lens.WorldToViewportPoint(head.position);
            if(p.z<=0||p.x<0||p.x>1||p.y<0||p.y>1)errors.Add("Head outside frame: "+label+" "+p);
            var cache=workbench.Actor.GetComponent<ToolPortraitBounds>();
            if(cache&&cache.TryGetBounds(out Bounds bounds))
            {
                if(workbench.FaceView)
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                {
                    var point=workbench.lens.WorldToViewportPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));
                    if(point.z<=0||point.x<0||point.x>1||point.y<0||point.y>1)errors.Add("Portrait corner clipped: "+label+" "+point);
                }
                var stage=workbench.Actor.GetComponentInParent<ToolStage>();
                var vector=bounds.center-workbench.lens.transform.position;var ray=new Ray(workbench.lens.transform.position,vector.normalized);
                foreach(var r in stage.GetComponentsInChildren<MeshRenderer>())
                    if(r.enabled&&r.bounds.IntersectRay(ray,out float entry)&&entry<vector.magnitude-.01f)
                        errors.Add("Stage occludes portrait: "+label+" / "+r.name);
            }
        }
    }
}

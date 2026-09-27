using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Astra.PerformanceToolV2
{
    public static class ToolHairRuntimeQA
    {
        public static IEnumerator Run(ToolWorkbench workbench)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-qaDir");
            string dir=at>=0?args[at+1]:Path.Combine(Application.persistentDataPath,"HairQA");Directory.CreateDirectory(dir);
            var errors=new List<string>();Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m+"\n"+s);};
            Application.logMessageReceived+=logger;
            yield return null;workbench.NewCharacter(ToolSpecies.Human);workbench.Paused=true;workbench.Section=2;
            workbench.Draft.look.hair=new Color(.17f,.13f,.11f);
            foreach(int style in new[]{10,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29})
            {
                workbench.Draft.look.hairStyle=style;workbench.RefreshLook();workbench.FaceView=true;workbench.ActionIndex=0;
                foreach(int yaw in new[]{0,65,180})
                {
                    workbench.Yaw=yaw;workbench.SampleAt(.4f);yield return null;yield return new WaitForEndOfFrame();
                    ToolRuntimeQA.CaptureCamera(workbench.lens,Path.Combine(dir,$"hair_{style:00}_{yaw:000}.png"));
                }
            }
            workbench.Draft.look.hairStyle=10;
            foreach(int face in new[]{0,1,2,5,7,9})
            foreach(float width in new[]{.92f,1.08f})
            {
                workbench.Draft.look.faceShape=face;workbench.Draft.look.faceWidth=width;workbench.Draft.look.jawWidth=width<1?-1:1;
                workbench.RefreshLook();workbench.Yaw=0;workbench.SampleAt(.4f);yield return null;yield return new WaitForEndOfFrame();
                var head=workbench.Actor.GetComponent<ToolMotion>().Animator.GetBoneTransform(HumanBodyBones.Head);
                Vector3 target=head.position+Vector3.up*.09f;
                workbench.lens.transform.position=target+new Vector3(.08f,.52f,-.64f);workbench.lens.transform.LookAt(target);
                ToolRuntimeQA.CaptureCamera(workbench.lens,Path.Combine(dir,$"crop_face_{face}_{width:F2}.png"));
            }
            workbench.Draft.look.hairStyle=29;
            string json=workbench.SaveLook();workbench.LoadLook(json);
            if(workbench.Draft.look.hairStyle!=29)errors.Add("New hair JSON roundtrip truncated ID 29");
            File.Delete(json); // Remove only the temporary record created by this QA run.
            workbench.Draft.look.faceWidth=1;workbench.Draft.look.jawWidth=0;
            foreach(int style in new[]{10,22,23,24,25,26,27})
            foreach(int action in new[]{1,6,9,14,16})
            {
                workbench.Draft.look.hairStyle=style;workbench.RefreshLook();workbench.ActionIndex=action;workbench.Yaw=65;workbench.FaceView=true;
                workbench.SampleAt(.6f);yield return null;yield return new WaitForEndOfFrame();
                var head=workbench.Actor.GetComponent<ToolMotion>().Animator.GetBoneTransform(HumanBodyBones.Head);
                var screen=workbench.lens.WorldToViewportPoint(head.position);
                if(screen.z<=0||screen.x<0||screen.x>1||screen.y<0||screen.y>1)errors.Add($"Head outside portrait after seek: hair {style}, action {action}, viewport {screen}");
                ToolRuntimeQA.CaptureCamera(workbench.lens,Path.Combine(dir,$"motion_{style:00}_{action:00}.png"));
            }
            Application.logMessageReceived-=logger;
            File.WriteAllLines(Path.Combine(dir,"hair-runtime-result.txt"),new[]{"Runtime errors: "+errors.Count,"19 styles x 3 angles; 12 short-crop face/extreme top views; 35 action views; new hair ID 29 JSON roundtrip"});
            if(errors.Count>0)File.WriteAllLines(Path.Combine(dir,"errors.txt"),errors);
            else if(File.Exists(Path.Combine(dir,"errors.txt")))File.Delete(Path.Combine(dir,"errors.txt"));
            Application.Quit(errors.Count==0?0:2);
        }
    }
}

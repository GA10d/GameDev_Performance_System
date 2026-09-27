using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astra.PerformanceToolV2
{
    public static class ToolChinRuntimeQA
    {
        public static IEnumerator Run(ToolWorkbench w)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-qaDir");
            string dir=at>=0?args[at+1]:Path.Combine(Application.persistentDataPath,"ChinQA");Directory.CreateDirectory(dir);
            var errors=new List<string>();int pictures=0;
            Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m+"\n"+s);};
            Application.logMessageReceived+=logger;yield return null;
            Action<int> family=n=>{w.NewCharacter(n<2?ToolSpecies.Human:ToolSpecies.StandingAlien,n==1?1:0);w.Paused=true;w.FaceView=true;w.Draft.look.hairStyle=0;w.Draft.look.faceShape=n<2?0:n-2;w.ActionIndex=Array.FindIndex(w.package.library.actions,a=>a.id=="Idle_Loop");};
            foreach(int n in Enumerable.Range(0,12))
            {
                family(n);
                foreach(int chin in Enumerable.Range(0,3))foreach(int yaw in new[]{0,75})
                {
                    w.Draft.look.chinStyle=chin;w.Yaw=yaw;w.RefreshLook();w.SampleAt(.4f);
                    yield return null;yield return new WaitForEndOfFrame();
                    ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"chin_{n:00}_{chin}_{yaw}.png"));pictures++;
                }
            }
            family(1);
            foreach(int hair in Enumerable.Range(1,10))foreach(int yaw in new[]{0,-65,65})
            {
                w.Draft.look.hairStyle=hair;w.Yaw=yaw;w.RefreshLook();w.SampleAt(.4f);
                yield return null;yield return new WaitForEndOfFrame();
                ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"hair_{hair:00}_{yaw}.png"));pictures++;
            }
            foreach(int hair in new[]{2,8})foreach(int sign in new[]{-1,1})foreach(int yaw in new[]{-40,40})
            {
                w.Draft.look.hairStyle=hair;w.Draft.look.faceWidth=1+sign*.08f;w.Draft.look.jawWidth=sign;
                w.Draft.look.faceShape=sign>0?7:9;w.Draft.look.chinStyle=sign>0?2:1;
                w.Yaw=yaw;w.RefreshLook();w.SampleAt(.4f);yield return null;yield return new WaitForEndOfFrame();
                ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"hair_extreme_{hair}_{sign}_{yaw}.png"));pictures++;
            }
            foreach(int n in new[]{0,1,2})
            {
                family(n);
                foreach(int mouth in Enumerable.Range(0,6))foreach(int yaw in new[]{0,85})
                {
                    w.Draft.look.mouthShape=mouth;w.Draft.look.chinStyle=2;w.Draft.look.faceWidth=1.08f;w.Draft.look.jawWidth=1;
                    w.Yaw=yaw;w.RefreshLook();w.SampleAt(.4f);yield return null;yield return new WaitForEndOfFrame();
                    ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"mouth_{n}_{mouth}_{yaw}.png"));pictures++;
                }
            }
            foreach(int n in new[]{0,1,2,3,4,9})
            {
                family(n);w.Draft.look.chinStyle=2;w.Draft.look.mouthShape=5;w.Draft.look.faceWidth=1.08f;w.Draft.look.jawWidth=1;
                if(n==1)w.Draft.look.hairStyle=2;
                foreach(string action in new[]{"Fixing_Kneeling","Dance_Loop"})
                {
                    w.FaceView=action!="Dance_Loop";w.Yaw=60;w.RefreshLook();w.ActionIndex=Array.FindIndex(w.package.library.actions,a=>a.id==action);
                    w.SampleAt(w.package.library.actions[w.ActionIndex].length*.45f);yield return null;yield return new WaitForEndOfFrame();
                    ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"action_{n}_{action}.png"));pictures++;
                }
            }
            Application.logMessageReceived-=logger;
            File.WriteAllLines(Path.Combine(dir,"result.txt"),new[]{"Runtime errors: "+errors.Count,"Camera frames: "+pictures,"Chin silhouettes / hairline both sides / mouth profiles / combined slider extremes / kneeling and dance. No persistent character files written."}.Concat(errors));
            Application.Quit(errors.Count==0?0:2);
        }
    }
}

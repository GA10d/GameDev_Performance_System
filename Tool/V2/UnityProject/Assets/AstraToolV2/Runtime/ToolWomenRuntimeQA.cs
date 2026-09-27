using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astra.PerformanceToolV2
{
    public static class ToolWomenRuntimeQA
    {
        public static IEnumerator Run(ToolWorkbench w)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-qaDir");
            string dir=at>=0?args[at+1]:Path.Combine(Application.persistentDataPath,"WomenQA");Directory.CreateDirectory(dir);
            var errors=new List<string>();int pictures=0;
            Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m+"\n"+s);};
            Application.logMessageReceived+=logger;yield return null;w.Paused=true;
            foreach(int i in Enumerable.Range(0,10))
            {
                w.LoadPreset(w.package.library.Character("creator_f"+i.ToString("00")));w.Paused=true;
                foreach(var shot in new[]{"front","side","back","full","bow","dance"})
                {
                    w.FaceView=shot!="full"&&shot!="dance";w.Yaw=shot=="side"?70:shot=="back"||shot=="bow"?150:0;
                    string action=shot=="bow"?"Fixing_Kneeling":shot=="dance"?"Dance_Loop":"Idle_Loop";
                    w.ActionIndex=Array.FindIndex(w.package.library.actions,a=>a.id==action);w.SampleAt(shot=="bow"?w.package.library.actions[w.ActionIndex].length*.45f:.6f);
                    yield return null;yield return new WaitForEndOfFrame();
                    ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"female_{i:00}_{shot}.png"));pictures++;
                }
            }
            w.NewCharacter(ToolSpecies.Human,1);w.Draft.look.hairStyle=0;w.Paused=true;w.FaceView=true;
            foreach(int yaw in new[]{0,70,150,180})
            {
                w.Yaw=yaw;w.RefreshLook();w.SampleAt(.4f);yield return null;yield return new WaitForEndOfFrame();
                ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"female_bald_{yaw}.png"));pictures++;
            }
            w.Draft.look.hairStyle=2;w.Draft.look.faceShape=9;w.Draft.look.faceWidth=1.08f;w.Draft.look.jawWidth=1;w.Draft.look.facialMark=5;
            string json=w.SaveLook();w.LoadLook(json);
            if(w.Draft.look.humanModel!=1||w.Draft.look.hairStyle!=2||w.Draft.look.faceShape!=9)errors.Add("Female JSON roundtrip failed");
            w.Section=2;w.Yaw=55;w.RefreshLook();w.SampleAt(.4f);yield return null;yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"female_workbench.png"));pictures++;
            foreach(int hair in new[]{11,12,13})foreach(int yaw in new[]{0,70})
            {
                w.Draft.look.hairStyle=hair;w.Yaw=yaw;w.RefreshLook();w.SampleAt(.4f);yield return null;yield return new WaitForEndOfFrame();
                ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"female_hat_{hair}_{yaw}.png"));pictures++;
            }
            w.Draft.look.hairStyle=0;w.Draft.look.faceShape=0;w.Draft.look.faceWidth=1;w.Draft.look.jawWidth=0;
            foreach(int accessory in new[]{1,3,4,5,7,8})
            {
                w.Draft.look.faceAccessory=accessory;w.Yaw=70;w.RefreshLook();w.SampleAt(.4f);yield return null;yield return new WaitForEndOfFrame();
                ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"female_accessory_{accessory}.png"));pictures++;
            }
            w.NewCharacter(ToolSpecies.StandingAlien);w.Paused=true;w.FaceView=true;w.Draft.look.hairStyle=0;w.Draft.look.faceAccessory=0;
            foreach(int mouth in Enumerable.Range(0,6))foreach(int yaw in new[]{0,70,150})foreach(var action in new[]{"Idle_Loop","Fixing_Kneeling"})
            {
                w.Draft.look.mouthShape=mouth;w.RefreshLook();w.Yaw=yaw;w.ActionIndex=Array.FindIndex(w.package.library.actions,a=>a.id==action);w.SampleAt(action=="Fixing_Kneeling"?w.package.library.actions[w.ActionIndex].length*.45f:.6f);
                yield return null;yield return new WaitForEndOfFrame();
                ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"gray_{mouth}_{yaw}_{action}.png"));pictures++;
            }
            yield return null;Application.logMessageReceived-=logger;
            File.WriteAllLines(Path.Combine(dir,"result.txt"),new[]{"Runtime errors: "+errors.Count,"Screenshots: "+pictures,"10 women: front/side/back/full/bow/dance; female bald 4 angles; JSON roundtrip; gray 6 mouths x 3 angles x 2 actions."}.Concat(errors));
            Application.Quit(errors.Count==0?0:2);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Astra.Performance
{
    public sealed class PerformanceCapture : MonoBehaviour
    {
        public PerformanceDirector director;
        string output;
        readonly List<string> checks=new List<string>();
        int errors;
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--performance-qa");if(index<0)yield break;
            output=index+1<args.Length?args[index+1]:Path.Combine(Application.dataPath,"../../QA/runtime");Directory.CreateDirectory(output);
            Application.logMessageReceived+=Log;
            yield return new WaitForSecondsRealtime(1);
            Check(director.Session!=null&&director.Error==null,"runtime session constructed");
            if(director.Session==null){File.WriteAllLines(Path.Combine(output,"runtime-verification.txt"),checks);Application.Quit(1);yield break;}
            Check(director.actors.All(a=>a.IsBound),"all cast rig adapters bound");
            Check(director.Feed.IsCreated()&&director.remoteCamera.targetTexture==director.Feed,"remote camera renders into owned RT");
            director.Session.Auto=false;
            director.StartChannel(0);yield return new WaitForSecondsRealtime(1.8f);director.Session.Advance();yield return new WaitForSecondsRealtime(.5f);
            yield return Capture("01_lin_signal");
            var actor=director.actors[0];var p=actor.Head.position;float t=director.Session.BeatTime;
            director.TogglePause();yield return new WaitForSecondsRealtime(.5f);
            Check(Mathf.Abs(t-director.Session.BeatTime)<.001f&&(actor.Head.position-p).sqrMagnitude<.000001f,"pause freezes actor and sequence");
            Check(director.Sound.Paused&&!director.Session.IsSpeaking,"pause silences speech and ambient");director.TogglePause();
            director.Session.Start("lin03");yield return new WaitForSecondsRealtime(2.2f);director.Session.Advance();yield return Capture("02_stillness");
            director.StartChannel(1);yield return new WaitForSecondsRealtime(1.4f);director.Session.Advance();yield return Capture("03_he_point");
            director.StartChannel(2);yield return new WaitForSecondsRealtime(1.9f);director.Session.Advance();yield return Capture("04_yu_guarded");
            director.Laboratory=true;director.FullBody=true;yield return new WaitForSecondsRealtime(1.2f);yield return Capture("05_rig_inspector");director.Laboratory=false;director.FullBody=false;
            director.Session.Start("reply");director.Session.Advance();yield return new WaitForSecondsRealtime(.8f);Check(director.Session.Phase==SessionPhase.Choice,"live choice reached");yield return Capture("06_reply_choice");
            Check(director.Session.Choose(1)&&director.Session.PendingReply.Contains("姓名"),"live log branch selected");director.Session.Advance();yield return new WaitForSecondsRealtime(1.2f);director.Session.Advance();Check(director.Session.Phase==SessionPhase.Complete,"live selected branch completes");yield return Capture("07_queued_reply");
            director.StartChannel(0);yield return new WaitForSecondsRealtime(1.5f);director.Session.Advance();director.ToggleStyle();yield return Capture("08_clear_comparison");Check(!director.remoteGrade.stylized&&!director.cabinGrade.stylized,"style switch reaches both cameras");director.ToggleStyle();
            // Matched-frame comparison: freeze only the presentation clock, retain the live GUI.
            director.enabled=false;foreach(var a in director.actors)a.enabled=false;
            yield return Capture("09_style_retro_matched");director.ToggleStyle();yield return Capture("10_style_clear_matched");director.ToggleStyle();
            director.enabled=true;foreach(var a in director.actors)a.enabled=true;
            director.Interrupt();Check(director.Session.Phase==SessionPhase.Cancelled&&director.actors.All(a=>a.MouthAmount==0),"interrupt clears mouth and session");director.StartChannel(0);Check(director.Session.PendingReply=="","replay clears reply");
            Check(director.actors.All(a=>a.GetComponentsInChildren<Renderer>(true).All(r=>r.sharedMaterials.All(m=>m&&m.shader&&m.shader.isSupported))),"actor shaders supported");
            Check(errors==0,"no runtime error or exception");File.WriteAllLines(Path.Combine(output,"runtime-verification.txt"),checks);
            File.WriteAllText(Path.Combine(output,"environment.txt"),SystemInfo.graphicsDeviceName+"\n"+SystemInfo.operatingSystem+"\n"+Screen.width+" x "+Screen.height+"\nUnity "+Application.unityVersion);
            Application.logMessageReceived-=Log;Application.Quit(checks.Any(x=>x.StartsWith("FAIL"))?1:0);
        }
        void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        void Check(bool ok,string name){checks.Add((ok?"PASS: ":"FAIL: ")+name);if(!ok)Debug.LogWarning("QA FAILURE: "+name);}
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
            var colors=image.GetPixels32();int bright=0;foreach(var c in colors)if(c.r+c.g+c.b>90)bright++;
            Check(bright>colors.Length*.08f,name+" rendered nonblack frame");Destroy(image);
        }
        public void CaptureManual(){StartCoroutine(Manual());}
        IEnumerator Manual()
        {
            yield return new WaitForEndOfFrame();string dir=Path.Combine(Application.persistentDataPath,"Screenshots");Directory.CreateDirectory(dir);
            var tex=ScreenCapture.CaptureScreenshotAsTexture();string path=Path.Combine(dir,DateTime.Now.ToString("yyyyMMdd_HHmmss")+".png");File.WriteAllBytes(path,tex.EncodeToPNG());Destroy(tex);Debug.Log("Screenshot: "+path);
        }
    }
}

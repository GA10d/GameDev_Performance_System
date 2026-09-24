using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Astra.PerformanceV2
{
    public sealed class ArchiveQA:MonoBehaviour
    {
        public ArchiveDirector director;
        readonly List<string> checks=new List<string>();
        string output;int errors;
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int idx=Array.IndexOf(args,"--archive-qa");if(idx<0)yield break;
            output=idx+1<args.Length?args[idx+1]:Path.GetFullPath(Application.dataPath+"/../../QA/runtime");Directory.CreateDirectory(output);
            Application.logMessageReceived+=Log;
            yield return new WaitForSecondsRealtime(1.2f);
            var d=director;
            Check(d.Error==null,"sequence validated at runtime");Check(d.actors.Length==4&&d.actors.Select(a=>a.species).Distinct().Count()==3,"four actors across three species");
            Check(d.sequence.clips.Length==43&&d.sequence.clips.All(c=>c.humanMotion),"43 real Quaternius humanoid clips imported (includes T pose)");
            Check(d.sequence.beats.Select(b=>b.shot).Distinct().Count()==9,"story exercises nine shot types");
            d.Seek(1);yield return new WaitForSecondsRealtime(1.8f);
            float time=d.BeatTime;var head=d.actors[0].Head.position;var cam=d.cameraRig.lens.transform.position;
            d.TogglePause();yield return new WaitForSecondsRealtime(.4f);
            Check(Mathf.Abs(d.BeatTime-time)<.00001f&&(head-d.actors[0].Head.position).sqrMagnitude<.0000001f&&(cam-d.cameraRig.lens.transform.position).sqrMagnitude<.0000001f,"pause freezes sequence, retargeted skeleton and camera");d.TogglePause();
            d.SetGallery(true);
            for(int i=0;i<4;i++)
            {
                d.SelectActor(i);var a=d.actors[i];Check(a.Ready,a.name+" valid humanoid avatar and graph");
                d.SelectAction(1);yield return new WaitForSecondsRealtime(.6f);
                var hand=a.Animator.GetBoneTransform(HumanBodyBones.RightHand);var before=hand.position;yield return new WaitForSecondsRealtime(.5f);
                Check(Vector3.Distance(before,hand.position)>.005f,a.name+" imported talking clip changes hand pose");
                for(int n=0;n<d.sequence.galleryActions.Length;n++)
                {
                    d.SelectAction(n);yield return new WaitForSecondsRealtime(.37f);
                    bool finite=a.GetComponentsInChildren<SkinnedMeshRenderer>().All(r=>Finite(r.bounds.center)&&Finite(r.bounds.size));
                    Check(finite&&a.Head.position.y>0&&a.LiveClips<=4,a.name+" / "+d.sequence.galleryActions[n]+" evaluates without invalid skinning");
                }
            }
            d.SelectActor(1);d.SelectAction(1);
            for(int i=0;i<40;i++){d.SelectAction(i%d.sequence.galleryActions.Length);yield return null;}
            Check(d.actors.All(a=>a.LiveClips<=4),"rapid action interruption keeps graph bounded");
            d.SelectAction(1);yield return new WaitForSecondsRealtime(.5f);
            foreach(ShotKind s in Enum.GetValues(typeof(ShotKind)))
            {d.SelectShot(s);yield return new WaitForSecondsRealtime(1.55f);Check(Finite(d.cameraRig.lens.transform.position)&&d.cameraRig.Transition>.99f,"camera settles / "+s);}
            int[] beats={0,1,2,3,4,5,7,8,9,11,13,14};
            string[] names={"01_establishing","02_human_talking","03_alien_close","04_over_shoulder","05_orc_low_angle","06_repair","07_relay_insert","08_two_shot","09_human_close","10_orc_seated","11_tracking","12_final_close"};
            for(int i=0;i<beats.Length;i++)
            {d.Seek(beats[i]);yield return new WaitForSecondsRealtime(beats[i]==5?2.7f:1.8f);yield return Capture(names[i]);}
            d.SetGallery(true);d.SelectActor(3);d.SelectAction(1);d.SelectShot(ShotKind.Medium);yield return new WaitForSecondsRealtime(1.9f);yield return Capture("13_gallery_voss");
            Check(d.actors.Where((a,i)=>i!=3).All(a=>a.GetComponentsInChildren<Renderer>().All(r=>!r.enabled)),"solo gallery shots hide other actors to prevent occlusion");
            d.SelectActor(1);d.SelectShot(ShotKind.Close);yield return new WaitForSecondsRealtime(1.9f);d.TogglePause();yield return Capture("14_alien_retro");d.grade.Stylized=false;yield return Capture("15_alien_clear");d.grade.Stylized=true;d.TogglePause();
            d.SelectActor(2);d.SelectAction(8);d.SelectShot(ShotKind.Tracking);yield return new WaitForSecondsRealtime(1.9f);yield return Capture("16_seat_full_body");
            d.ToggleReducedMotion();Check(d.cameraRig.ReducedMotion&&d.actors.All(a=>a.ReducedMotion),"reduced motion reaches camera and all actors");d.ToggleReducedMotion();
            d.Seek(d.sequence.beats.Length-1);Check(d.actors.All(a=>a.GetComponentsInChildren<Renderer>().All(r=>r.enabled)),"return to archive restores cast visibility");yield return new WaitForSecondsRealtime(d.Beat.duration+.2f);Check(d.Ended,"archive reaches end");d.Seek(0);Check(!d.Ended&&d.Index==0&&d.BeatTime<.1f,"replay resets completion");
            Check(d.actors.All(a=>a.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>m&&m.shader.isSupported))),"character shaders supported");
            Check(d.actors.All(a=>a.GetComponentsInChildren<SkinnedMeshRenderer>().All(r=>r.updateWhenOffscreen)),"posed mesh bounds update for seated heads and facial accessories");
            d.ToggleReducedMotion();d.Seek(9);yield return new WaitForSecondsRealtime(.5f);
            var stableLens=d.cameraRig.lens.transform.position;var stableRotation=d.cameraRig.lens.transform.rotation;
            yield return new WaitForSecondsRealtime(.8f);
            Check(Vector3.Distance(stableLens,d.cameraRig.lens.transform.position)<.0001f&&Quaternion.Angle(stableRotation,d.cameraRig.lens.transform.rotation)<.001f,"portrait framing is independent of animated head bones");d.ToggleReducedMotion();
            d.Seek(13);yield return new WaitForSecondsRealtime(d.Beat.duration+.15f);
            Check(d.Index==14&&Vector3.Distance(d.actors[0].transform.position,d.sequence.beats[13].end)<.001f,"walking endpoint survives automatic cut into final portrait");
            d.Seek(14);Check(Vector3.Distance(d.actors[0].transform.position,d.sequence.beats[13].end)<.001f,"manual seek reconstructs prior blocking");
            Check(errors==0,"no runtime exceptions or errors");
            File.WriteAllLines(Path.Combine(output,"runtime-verification.txt"),checks);
            File.WriteAllText(Path.Combine(output,"environment.txt"),SystemInfo.graphicsDeviceName+"\n"+SystemInfo.operatingSystem+"\n"+Screen.width+" x "+Screen.height+"\nUnity "+Application.unityVersion);
            Application.logMessageReceived-=Log;Application.Quit(checks.Any(c=>c.StartsWith("FAIL"))?1:0);
        }
        static bool Finite(Vector3 v)=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z);
        void Check(bool ok,string name){checks.Add((ok?"PASS: ":"FAIL: ")+name);if(!ok)Debug.LogWarning("QA "+name);}
        void Log(string m,string st,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),tex.EncodeToPNG());
            int count=0,n=0;
            for(int y=Screen.height/3;y<Screen.height*3/4;y+=4)for(int x=Screen.width/5;x<Screen.width*4/5;x+=4){var c=tex.GetPixel(x,y);n++;if(c.r+c.g+c.b>.19f)count++;}
            Check(count>n*.12f,name+" contains rendered scene pixels");Destroy(tex);
        }
        public void CaptureManual(){StartCoroutine(Manual());}
        IEnumerator Manual()
        {yield return new WaitForEndOfFrame();string p=Path.Combine(Application.persistentDataPath,"Screenshots");Directory.CreateDirectory(p);var t=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(p,DateTime.Now.ToString("yyyyMMdd_HHmmss")+".png"),t.EncodeToPNG());Destroy(t);}
    }
}

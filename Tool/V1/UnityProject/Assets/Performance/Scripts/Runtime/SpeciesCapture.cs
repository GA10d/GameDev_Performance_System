using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astra.Performance
{
    // Opt-in player-side QA. No external editor or render substitutes for these captures.
    public sealed class SpeciesCapture:MonoBehaviour
    {
        public PerformanceDirector director;
        readonly List<string> checks=new List<string>();
        readonly List<string> frames=new List<string>();
        string output;int errors;
        void Check(bool okay,string name){checks.Add((okay?"PASS: ":"FAIL: ")+name);}
        void Log(string msg,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int idx=Array.IndexOf(args,"--species-qa");if(idx<0)yield break;
            output=args[idx+1];Directory.CreateDirectory(output);Application.logMessageReceived+=Log;
            yield return new WaitForSecondsRealtime(1);
            var d=director;Check(d.Error==null&&d.actors.Length==6,"six actors and valid session");
            Check(d.actors.All(a=>a.IsBound),"all six V1 semantic rigs bind");
            Check(Mathf.Abs(d.remoteCamera.aspect-1240f/500)<.001f,"feed aspect matches visible window without cropping");
            d.Session.Auto=false;
            for(int i=0;i<6;i++)
            {
                d.FullBody=false;d.StartChannel(i);yield return new WaitForSecondsRealtime(1.1f);d.Session.Advance();
                Check(d.Session.Beat.actor==(ActorId)i&&d.actors.Count(a=>a.gameObject.activeSelf)==1,"channel "+i+" activates exactly its subject");
                yield return Capture("actor_"+i+"_portrait");
                d.FullBody=true;yield return new WaitForSecondsRealtime(.4f);yield return Capture("actor_"+i+"_full");
                var renderer=d.actors[i].GetComponentInChildren<SkinnedMeshRenderer>();
                Bounds bounds=renderer.bounds;bool fits=true;
                for(int corner=0;corner<8;corner++)
                {
                    Vector3 p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                    Vector3 v=d.remoteCamera.WorldToViewportPoint(p);fits&=v.x>-.04f&&v.x<1.04f&&v.y>-.04f&&v.y<1.04f&&v.z>0;
                }
                Check(fits,"full-body framing contains posed bounds / "+i);
            }
            d.FullBody=false;
            foreach(var beat in d.sequence.beats.Where(b=>(int)b.actor>=3&&!b.systemLine))
            {
                d.Session.Start(beat.id);
                for(int stage=0;stage<4;stage++){yield return new WaitForSecondsRealtime(.75f);yield return Capture(beat.id+"_"+stage);}
            }
            d.StartChannel(5);d.Session.Advance();var crawler=d.actors[5].GetComponent<CrawlerPerformance>();
            float maxError=0,minY=10;bool moving=false,supported=true;var initial=crawler.Feet;
            for(int n=0;n<24;n++)
            {
                yield return new WaitForSecondsRealtime(.25f);maxError=Mathf.Max(maxError,crawler.MaxContactError);minY=Mathf.Min(minY,crawler.FootHeights.Min());
                moving|=crawler.Feet.Where((p,i)=>Vector3.Distance(p,initial[i])>.035f).Any();
                supported&=crawler.FootHeights.Count(y=>y<.058f)>=3;
                yield return Capture("crawl_"+n.ToString("D2"));
            }
            Check(moving,"crawler changes limb contacts through a complete gait cycle");
            Check(supported,"crawler retains at least three floor contacts throughout sampled gait");
            Check(maxError<.025f,"crawler IK reaches targets (max error "+maxError.ToString("F4")+" m)");
            Check(minY>.005f,"crawler endpoints remain above floor (minimum "+minY.ToString("F4")+" m)");
            d.TogglePause();var feet=crawler.Feet;var camera=d.remoteCamera.transform.position;float time=d.Session.BeatTime;
            yield return new WaitForSecondsRealtime(.3f);
            Check(crawler.Feet.Where((p,i)=>Vector3.Distance(p,feet[i])>.0001f).Count()==0&&Mathf.Abs(d.Session.BeatTime-time)<.0001f&&Vector3.Distance(camera,d.remoteCamera.transform.position)<.0001f,"pause freezes crawler, camera and captions");
            yield return Capture("crawler_paused");d.TogglePause();d.ToggleCrawl();yield return new WaitForSecondsRealtime(1);
            feet=crawler.Feet;yield return new WaitForSecondsRealtime(.6f);Check(!d.Crawling&&crawler.Feet.Where((p,i)=>Vector3.Distance(p,feet[i])>.015f).Count()==0,"stop mode returns four supports to rest");yield return Capture("crawler_rest");
            d.ToggleReducedMotion();yield return new WaitForSecondsRealtime(.3f);Check(crawler.MaxContactError<.025f,"reduced-motion crawler retains contact solution");d.ToggleReducedMotion();
            d.ToggleCrawl();d.ToggleStyle();yield return Capture("crawler_clear");d.ToggleStyle();
            for(int i=3;i<6;i++)
            {
                d.StartChannel(i);d.Session.Auto=true;
                for(int n=0;n<400&&d.Session.Phase!=SessionPhase.Complete;n++){d.Session.Tick(.1f);if(d.Session.Phase==SessionPhase.Ready)d.Session.Advance();}
                Check(d.Session.Phase==SessionPhase.Complete,"species channel completes / "+i);
                d.Session.Auto=false;
            }
            d.StartChannel(0);Check(d.Session.Beat.id=="lin01","original human archive remains accessible");
            Check(errors==0,"no runtime errors or exceptions");
            File.WriteAllLines(Path.Combine(output,"checks.txt"),checks);File.WriteAllLines(Path.Combine(output,"frames.txt"),frames);
            File.WriteAllText(Path.Combine(output,"environment.txt"),SystemInfo.graphicsDeviceName+"\n"+Screen.width+" x "+Screen.height+"\nUnity "+Application.unityVersion);
            Application.logMessageReceived-=Log;Application.Quit(checks.Any(x=>x.StartsWith("FAIL"))?1:0);
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(output,name+".png"),tex.EncodeToPNG());frames.Add(name+".png");
            int pixels=0;for(int y=Screen.height/4;y<Screen.height*2/3;y+=8)for(int x=Screen.width/4;x<Screen.width*3/4;x+=8){var c=tex.GetPixel(x,y);if(c.r+c.g+c.b>.12f)pixels++;}
            Check(pixels>80,"visible rendered frame / "+name);Destroy(tex);
        }
    }
}

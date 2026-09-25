using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Astra.Performance;

public static class PerformanceVerification
{
    static List<string> checks;
    static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL: "+message);checks.Add("PASS: "+message);}
    static PerformanceBeat B(string id,string text="你好，世界。",string next="")=>new PerformanceBeat{id=id,text=text,lead=.2f,charactersPerSecond=18,hold=.4f,next=next};
    static void Tick(PerformanceSession s,float seconds,float step=1f/60){for(float t=0;t<seconds;t+=step)s.Tick(Math.Min(step,seconds-t));}
    [MenuItem("Astra Performance/Verify core")]
    public static void Run()
    {
        checks=new List<string>();
        var one=B("one");var s=new PerformanceSession(new[]{one});int pulses=0,entries=0;
        s.Revealed+=_=>pulses++;s.Entered+=_=>entries++;
        s.Start("one");Check(s.Phase==SessionPhase.Lead && entries==1,"start enters exactly one beat");
        s.Tick(.1f);Check(s.VisibleText=="","lead does not reveal text early");
        s.Paused=true;float clock=s.BeatTime;s.Tick(5);s.Advance();Check(s.BeatTime==clock && s.VisibleText=="","pause freezes clock and ignores advance");s.Paused=false;
        s.Advance();Check(s.VisibleText==one.text && s.Phase==SessionPhase.Hold,"first advance completes text only");Check(pulses==0 && entries==1,"skip emits no burst of sound or duplicate cue");
        s.Advance();Check(s.Phase==SessionPhase.Hold,"rapid second advance cannot bypass required hold");
        Tick(s,.5f);Check(s.Phase==SessionPhase.Ready,"hold reaches ready");s.Advance();Check(s.Phase==SessionPhase.Complete,"terminal node completes");
        var choice=B("choice");choice.choices=new[]{new ReplyChoice{text="name",next="endA",payload="NAME"},new ReplyChoice{text="log",next="endB",payload="LOG"}};
        s=new PerformanceSession(new[]{choice,B("endA"),B("endB")}){Auto=true};s.Start("choice");Tick(s,20);
        Check(s.Phase==SessionPhase.Choice,"automatic playback waits for choice indefinitely");Check(!s.Choose(-1)&&!s.Choose(7),"out of range choice is ignored");
        s.Paused=true;Check(!s.Choose(0),"paused choice is ignored");s.Paused=false;
        Check(s.Choose(0)&&s.Beat.id=="endA"&&s.PendingReply=="NAME","name reply branch routes correctly");Check(!s.Choose(1),"a choice cannot be submitted twice");
        s.Start("choice");s.Advance();Tick(s,.5f);Check(s.Choose(1)&&s.Beat.id=="endB"&&s.PendingReply=="LOG","log reply branch routes correctly");
        s.Cancel();Check(s.Phase==SessionPhase.Cancelled&&s.Beat==null&&s.VisibleText==""&&s.PendingReply=="","cancel releases session state");
        s.Start("choice");Check(s.PendingReply==""&&s.Phase==SessionPhase.Lead,"replay clears previous reply");
        var unicode=B("unicode","A😀中e\u0301");s=new PerformanceSession(new[]{unicode});s.Start("unicode");Tick(s,2);
        Check(s.VisibleElements==4&&s.VisibleText==unicode.text,"unicode surrogate pairs and combining marks remain intact");
        var a=new PerformanceSession(new[]{B("fps")});var b=new PerformanceSession(new[]{B("fps")});a.Start("fps");b.Start("fps");Tick(a,1.4f,1f/30);Tick(b,1.4f,1f/144);
        Check(a.VisibleText==b.VisibleText&&a.Phase==b.Phase,"typing is stable across 30 and 144 Hz");
        s=new PerformanceSession(new[]{B("lag")});s.Start("lag");s.Tick(5);Check(s.VisibleText=="你好，世界。"&&s.Phase==SessionPhase.Ready,"large frame catches up without dropping characters");
        bool rejected=false;try{new PerformanceSession(new[]{B("same"),B("same")});}catch(ArgumentException){rejected=true;}Check(rejected,"duplicate IDs rejected");
        rejected=false;try{new PerformanceSession(new[]{B("x","text","missing")});}catch(ArgumentException){rejected=true;}Check(rejected,"dangling edges rejected");
        var bad=B("bad");bad.charactersPerSecond=0;rejected=false;try{new PerformanceSession(new[]{bad});}catch(ArgumentException){rejected=true;}Check(rejected,"zero text speed rejected");
        bad=B("nan");bad.hold=float.NaN;rejected=false;try{new PerformanceSession(new[]{bad});}catch(ArgumentException){rejected=true;}Check(rejected,"nonfinite timing rejected");
        var seq=AssetDatabase.LoadAssetAtPath<PerformanceSequence>("Assets/Performance/Data/SignalArchive.asset");
        Check(seq && MonoScript.FromScriptableObject(seq) && MonoScript.FromScriptableObject(seq).GetClass()==typeof(PerformanceSequence),"serialized sequence has a valid Unity script binding");
        s=new PerformanceSession(seq.beats);s.Start(seq.entryId);var visited=new HashSet<string>();
        for(int i=0;i<5000&&s.Phase!=SessionPhase.Complete;i++){visited.Add(s.Beat.id);s.Tick(.1f);if(s.Phase==SessionPhase.Ready)s.Advance();if(s.Phase==SessionPhase.Choice)s.Choose(0);}
        Check(s.Phase==SessionPhase.Complete&&visited.Contains("lin03")&&visited.Contains("he01")&&visited.Contains("yu01")&&visited.Contains("end_name"),"shipping sequence traverses all three cast members and completes");
        Check(seq.beats.All(x=>x.text.Length<=54),"shipping subtitles fit the two-line writing budget");
        Check(UnityEngine.Object.FindObjectsOfType<ActorPerformance>().All(x=>x.IsBound),"all shipping rigs contain required bones");
        var director=UnityEngine.Object.FindObjectOfType<PerformanceDirector>();
        Check(director.actors.All(x=>Vector3.Dot(x.transform.forward,(director.remoteCamera.transform.position-x.transform.position).normalized)>.7f),"FBX characters face the archive camera");
        var root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));Directory.CreateDirectory(Path.Combine(root,"QA"));
        File.WriteAllLines(Path.Combine(root,"QA/core-verification.txt"),checks);Debug.Log("CORE_VERIFIED "+checks.Count+" checks");
    }
}

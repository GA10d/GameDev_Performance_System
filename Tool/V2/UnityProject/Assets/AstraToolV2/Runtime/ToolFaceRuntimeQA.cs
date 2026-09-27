using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astra.PerformanceToolV2
{
    public static class ToolFaceRuntimeQA
    {
        public static IEnumerator Run(ToolWorkbench w)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-qaDir");
            string dir=at>=0?args[at+1]:Path.Combine(Application.persistentDataPath,"FaceQA");Directory.CreateDirectory(dir);
            var errors=new List<string>();int pictures=0,looks=0;
            Application.LogCallback logger=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m+"\n"+s);};
            Application.logMessageReceived+=logger;
            yield return null;w.Paused=true;w.FaceView=true;
            foreach(int id in new[]{3,4,7,9,15})
            {
                w.LoadPreset(w.package.library.Character("creator_h"+id.ToString("00")));w.Paused=true;w.FaceView=true;
                foreach(int yaw in new[]{0,65,-65})
                {
                    w.Yaw=yaw;w.SampleAt(.4f);yield return null;yield return new WaitForEndOfFrame();
                    ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"preset_{id:00}_{yaw}.png"));pictures++;
                }
            }
            foreach(var species in new[]{ToolSpecies.Human,ToolSpecies.StandingAlien})
            {
                w.NewCharacter(species);w.Paused=true;w.FaceView=true;w.ActionIndex=0;w.Draft.look.hairStyle=0;w.Draft.look.faceAccessory=0;
                w.Draft.look.accent=new Color(.55f,.12f,.09f);
                foreach(int mark in new[]{0,1,2,3,4,5})
                foreach(int yaw in new[]{0,65,180})
                {
                    w.Draft.look.facialMark=mark;w.RefreshLook();w.Yaw=yaw;w.SampleAt(.4f);
                    yield return null;yield return new WaitForEndOfFrame();
                    ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"paint_{species}_{mark}_{yaw}.png"));pictures++;
                }
                for(int face=0;face<(species==ToolSpecies.Human?10:1);face++)
                foreach(float width in new[]{.92f,1.08f})
                foreach(int mark in new[]{0,1,2,3,4,5})
                {
                    w.Draft.look.faceShape=face;w.Draft.look.faceWidth=width;w.Draft.look.jawWidth=width<1?-1:1;w.Draft.look.facialMark=mark;w.RefreshLook();looks++;
                    if(w.Actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r=>r.name.StartsWith("Mark_")))errors.Add("Unexpected face paint mesh");
                    foreach(var r in w.Actor.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled&&r.name.StartsWith("Head_")))
                    for(int slot=0;slot<r.sharedMaterials.Length;slot++)
                    {
                        if(!r.sharedMaterials[slot].name.StartsWith("Q_Skin"))continue;
                        var block=new MaterialPropertyBlock();r.GetPropertyBlock(block,slot);
                        if((block.GetFloat("_FaceMarkEnabled")>.5f)!=(mark>0))errors.Add("Face paint toggle mismatch");
                    }
                }
                w.Draft.look.faceShape=0;w.Draft.look.faceWidth=1;w.Draft.look.jawWidth=0;
                foreach(int mask in new[]{5,7})foreach(int yaw in new[]{0,65})foreach(int action in new[]{0,1,6})
                {
                    w.Draft.look.faceAccessory=mask;w.Draft.look.facialMark=0;w.RefreshLook();w.ActionIndex=action;w.Yaw=yaw;w.SampleAt(.6f);
                    yield return null;yield return new WaitForEndOfFrame();
                    ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"mask_{species}_{mask}_{yaw}_{action}.png"));pictures++;
                }
                foreach(int headset in new[]{4,8})foreach(int yaw in new[]{0,65,-65})
                {
                    w.Draft.look.faceAccessory=headset;w.RefreshLook();w.ActionIndex=1;w.Yaw=yaw;w.SampleAt(.6f);
                    yield return null;yield return new WaitForEndOfFrame();
                    ToolRuntimeQA.CaptureCamera(w.lens,Path.Combine(dir,$"headset_{species}_{headset}_{yaw}.png"));pictures++;
                }
            }
            // Exercise the real runtime clock beyond nine seconds without editing user's assets.
            var package=UnityEngine.Object.Instantiate(w.package);var graph=ScriptableObject.CreateInstance<ToolGraph>();var unit=ScriptableObject.CreateInstance<ToolUnit>();
            unit.id="qa_long";unit.duration=3600;
            unit.actors.Add(new ToolActorSpan{id="speaker",character=w.package.library.characters.First(c=>c.look.creatorEnabled),duration=3600});
            unit.actions.Add(new ToolActionSpan{actorId="speaker",actionId="Idle_Talking_Loop",duration=3600});
            unit.dialogue.Add(new ToolDialogueSpan{actorId="speaker",start=120,text="长演出播放检查",hold=2});
            unit.scenes.Add(new ToolSceneSpan{sceneId="archive",duration=3600});
            unit.cameras.Add(new ToolCameraSpan{actorId="speaker",shot=ToolShot.Medium,duration=3600});
            graph.entryNode="long";graph.nodes.Add(new ToolGraphNode{id="long",kind=ToolNodeKind.Unit,unit=unit,edges=new List<ToolGraphEdge>{new ToolGraphEdge{slot="next",target="end"}}});
            graph.nodes.Add(new ToolGraphNode{id="end",kind=ToolNodeKind.End});package.graph=graph;
            var player=new GameObject("Long unit QA").AddComponent<ToolPlayer>();player.enabled=false;player.autoPlay=false;player.package=package;player.lens=w.lens;player.Begin();
            foreach(float time in new[]{10,30,120.5f,3599})
            {player.Seek(time);if(Mathf.Abs(player.CurrentTime-time)>.01f||player.CurrentNodeId!="long")errors.Add("Runtime seek clipped at "+time);}
            player.Seek(120.5f);if(string.IsNullOrEmpty(player.CurrentSubtitle))errors.Add("Late subtitle missing");
            player.Stop();UnityEngine.Object.Destroy(player.gameObject);UnityEngine.Object.Destroy(unit);UnityEngine.Object.Destroy(graph);UnityEngine.Object.Destroy(package);
            Application.logMessageReceived-=logger;
            File.WriteAllLines(Path.Combine(dir,"result.txt"),new[]{"Runtime errors: "+errors.Count,"Screenshots: "+pictures,"Face/width/paint states: "+looks,"Runtime seek: 10 / 30 / 120.5 / 3599 seconds; late subtitle at 120.5 seconds"}.Concat(errors));
            Application.Quit(errors.Count==0?0:2);
        }
    }
}

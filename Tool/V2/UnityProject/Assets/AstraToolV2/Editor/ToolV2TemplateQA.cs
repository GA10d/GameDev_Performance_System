using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceToolV2;

public static class ToolV2TemplateQA
{
    static readonly List<string> lines=new List<string>();
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);lines.Add("PASS "+message);}
    public static void Run()
    {
        try
        {
            if(!Environment.GetCommandLineArgs().Contains("-authoringQA"))throw new Exception("Run in the isolated QA copy");
            var lib=AssetDatabase.LoadAssetAtPath<ToolLibrary>(ToolV2Build.Root+"/Samples/Library.asset");
            var sample=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolV2Build.PackagePath);
            string baseline=EditorJsonUtility.ToJson(lib)+EditorJsonUtility.ToJson(sample.graph);
            foreach(ToolPerformanceTemplate type in Enum.GetValues(typeof(ToolPerformanceTemplate)))
            {
                var config=ToolV2Templates.Defaults(type,lib);var package=ToolV2Templates.Create(lib,config);
                Check(ToolValidation.Package(package).Count==0,type+": generated graph and timelines validate");
                Check(!AssetDatabase.Contains(package)&&!AssetDatabase.Contains(package.graph)&&ToolV2Authoring.Units(package).All(u=>!AssetDatabase.Contains(u)),type+": creation remains an unsaved draft");
                Check(AllReachable(package.graph),type+": all nodes reachable, no disconnected template content");
                if(type!=ToolPerformanceTemplate.Blank)
                {
                    Check(ToolV2Authoring.Units(package).All(u=>u.actors.All(a=>a.start==0&&a.duration==u.duration)&&u.scenes.Single().duration==u.duration),type+": actors and scene cover full dialogue");
                    Check(ToolV2Authoring.Units(package).All(u=>u.cameras.First().start==0&&Math.Abs(u.cameras.Last().start+u.cameras.Last().duration-u.duration)<.001f),type+": camera coverage reaches the end");
                }
                var saved=ToolV2Authoring.SaveDraft(package,"Assets/TemplateQA",type.ToString());
                Check(ToolValidation.Package(saved).Count==0&&AssetDatabase.Contains(saved),type+": template can be saved as editable assets");
            }
            var question=ToolV2Templates.Defaults(ToolPerformanceTemplate.Question,lib);question.replySeconds=75;
            var q=ToolV2Templates.Create(lib,question);var intro=q.graph.Node("opening").unit;var choice=intro.dialogue.Last();
            Check(choice.kind==ToolDialogueKind.Choice&&choice.choices.Length==2&&choice.patienceSeconds==75&&q.initialPatience>=75,"Configured reply time is not shortened by initial patience");
            Check(q.graph.Node("opening").edges.Select(e=>e.slot).OrderBy(s=>s).SequenceEqual(new[]{"accept","decline","silence"}),"Both replies and timeout are wired");
            Check(q.graph.nodes.Count(n=>n.kind==ToolNodeKind.End)==3,"Interactive branches have distinct endings");
            Check(intro.actions.Last().actionId=="Idle_Loop"&&intro.actions.Last().start==choice.start,"Speaker waits in idle while player chooses");
            var longText=ToolV2Templates.Defaults(ToolPerformanceTemplate.Broadcast,lib);longText.opening=new string('长',1800);longText.sceneId=lib.scenes.Last().id;longText.speaker=ToolV2Templates.Characters(lib).Last();
            var longer=ToolV2Templates.Create(lib,longText);var unit=longer.graph.Node("opening").unit;
            Check(unit.duration>100&&ToolValidation.Package(longer).Count==0,"Long dialogue extends all tracks without clipping");
            Check(unit.actors[0].character==longText.speaker&&unit.scenes[0].sceneId==longText.sceneId&&unit.dialogue[0].text==longText.opening,"Selected character, scene and text are preserved");
            var dual=ToolV2Templates.Defaults(ToolPerformanceTemplate.Conversation,lib);var dialogue=ToolV2Templates.Create(lib,dual);
            var units=ToolV2Authoring.Units(dialogue);
            Check(units[0].dialogue[0].actorId=="speaker"&&units[1].dialogue[0].actorId=="partner","Two-person dialogue switches the actual speaker");
            Check(units.All(u=>u.actors[0].character==dual.speaker&&u.actors[1].character==dual.partner&&u.cameras.Last().shot==ToolShot.OverShoulder),"Two-person staging keeps both actors and alternates camera targets");
            var duplicate=dual.Copy();duplicate.partner=duplicate.speaker;Check(ToolV2Templates.Validate(lib,duplicate).Count>0,"Duplicate conversation participants rejected");
            duplicate=question.Copy();duplicate.opening=" ";Check(ToolV2Templates.Validate(lib,duplicate).Count>0,"Blank required dialogue rejected before generation");
            duplicate=question.Copy();duplicate.replySeconds=float.NaN;Check(ToolV2Templates.Validate(lib,duplicate).Count>0,"Invalid reply duration rejected");
            var independent=ToolV2Templates.Create(lib,question);independent.graph.Node("opening").unit.dialogue[0].text="独立修改";
            Check(q.graph.Node("opening").unit.dialogue[0].text!="独立修改","Generated performances never share editable unit data");
            Check(EditorJsonUtility.ToJson(lib)+EditorJsonUtility.ToJson(sample.graph)==baseline,"Existing library and source graph unchanged");
            var savedQ=ToolV2Authoring.SaveDraft(q,"Assets/TemplateQA","QuestionExport");ToolV2MeetingExport.Export(savedQ,Path.GetFullPath("../TemplateExport-QA"),"template_question");
            Check(File.Exists("../TemplateExport-QA/Assets/AstraToolV2/Resources/Performances/template_question.asset"),"Template uses the existing game export pipeline");
            ToolV2Authoring.AdoptSaved(savedQ);ToolV2Authoring.NewPerformance();
            Check(ToolV2Authoring.Current==savedQ,"Opening the template chooser does not replace the current performance");
            foreach(var window in Resources.FindObjectsOfTypeAll<ToolV2TemplateWindow>())window.Close();
            Check(ToolV2Authoring.Current==savedQ,"Cancelling the chooser preserves current work");
            bool blocked=false;try{ToolV2Authoring.CreateFromTemplate(lib,duplicate);}catch(ArgumentException){blocked=true;}
            Check(blocked&&ToolV2Authoring.Current==savedQ,"Invalid form input cannot replace current work");
            ToolV2Authoring.CreateFromTemplate(lib,dual);
            Check(ToolV2Authoring.IsDraft(ToolV2Authoring.Current)&&ToolV2Authoring.Unit.actors.Count==2,"Create action activates the configured template in the timeline");
            foreach(var window in Resources.FindObjectsOfTypeAll<ToolV2TimelineWindow>())window.Close();
            ToolV2Authoring.AdoptSaved(savedQ);
            lines.Add("RESULT PASS checks="+lines.Count);Directory.CreateDirectory("../QA");File.WriteAllLines("../QA/template-verification.txt",lines);Debug.Log("ASTRA_TEMPLATE_QA_SUCCEEDED");
        }
        catch(Exception e){Directory.CreateDirectory("../QA");lines.Add("FAIL "+e);File.WriteAllLines("../QA/template-verification.txt",lines);Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static bool AllReachable(ToolGraph graph)
    {
        var visited=new HashSet<string>();var todo=new Stack<string>();todo.Push(graph.entryNode);
        while(todo.Count>0){string id=todo.Pop();if(!visited.Add(id))continue;var node=graph.Node(id);if(node==null)return false;foreach(var e in node.edges)todo.Push(e.target);}
        return visited.Count==graph.nodes.Count;
    }
}

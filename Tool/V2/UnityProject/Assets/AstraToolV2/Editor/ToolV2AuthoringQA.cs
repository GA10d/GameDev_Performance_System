using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceToolV2;
using Object=UnityEngine.Object;

public static class ToolV2AuthoringQA
{
    static readonly List<string> lines=new List<string>();
    static void Check(bool value,string label){if(!value)throw new Exception(label);lines.Add("PASS "+label);}
    public static void Run()
    {
        try
        {
            if(!Environment.GetCommandLineArgs().Contains("-authoringQA"))throw new InvalidOperationException("请在隔离副本使用 -authoringQA 执行验收。");
            if(File.Exists(ToolV2Authoring.RecoveryPath))File.Delete(ToolV2Authoring.RecoveryPath);
            if(File.Exists(ToolV2Authoring.RecoveryPath+".bak"))File.Delete(ToolV2Authoring.RecoveryPath+".bak");
            var library=AssetDatabase.LoadAssetAtPath<ToolLibrary>(ToolV2Build.Root+"/Samples/Library.asset");
            var sample=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolV2Build.PackagePath);
            string original=EditorJsonUtility.ToJson(sample.graph),libOriginal=EditorJsonUtility.ToJson(library);
            var draft=ToolV2Authoring.CreateDraft(library);
            Check(!AssetDatabase.Contains(draft)&&!AssetDatabase.Contains(draft.graph)&&ToolV2Authoring.Units(draft).All(u=>!AssetDatabase.Contains(u)),"New performance creates no authored asset files");
            Check(ToolValidation.Package(draft).Count==0,"New draft has valid entry, unit and End connections");
            var first=draft.graph.Node(draft.graph.entryNode);var second=ToolV2Authoring.AddUnit(draft,first);
            Check(ToolLogic.Exit(first,"next")==second.id&&draft.graph.Node(ToolLogic.Exit(second,"next")).kind==ToolNodeKind.End,"New unit automatically inserts into simple sequence");
            Check(first.unit!=second.unit&&first.id!=second.id,"Each new unit is independent and uniquely named");
            first.unit.actors.Add(new ToolActorSpan{id="actor0",character=library.characters.First(c=>c),start=0,duration=8});
            first.unit.actions.Add(new ToolActionSpan{actorId="actor0",actionId="Idle_Talking_Loop",start=0,duration=8});
            first.unit.dialogue.Add(new ToolDialogueSpan{actorId="actor0",text="草稿恢复与导出测试",start=.3f,charactersPerSecond=15,hold=1});
            first.unit.scenes.Add(new ToolSceneSpan{sceneId="archive",start=0,duration=8});
            first.unit.cameras.Add(new ToolCameraSpan{actorId="actor0",shot=ToolShot.Medium,start=0,duration=8});
            draft.name="Draft QA";draft.initialStats=new[]{new ToolStat{key="credits",value=7}};
            string snapshot="Library/AuthoringQA-snapshot.asset";
            ToolV2Authoring.WriteSnapshot(draft,snapshot);
            var recovered=ToolV2Authoring.ReadSnapshot(snapshot);
            Check(recovered!=draft&&recovered.graph!=draft.graph&&recovered.graph.Node(first.id).unit!=first.unit,"Recovery rebuilds separate draft objects");
            Check(recovered.library==library&&recovered.graph.Node(first.id).unit.actors[0].character==first.unit.actors[0].character,"Recovery preserves library and character asset references");
            Check(recovered.initialStats[0].value==7&&recovered.graph.Node(first.id).unit.dialogue[0].text=="草稿恢复与导出测试"&&ToolValidation.Package(recovered).Count==0,"Recovery preserves dialogue, timeline, variables and graph links");
            var branch=new ToolGraphNode{id="reply",kind=ToolNodeKind.GlobalChoice,prompt="是否继续？",patienceSeconds=15,choices=new[]{new ToolChoice{id="yes",text="继续"},new ToolChoice{id="no",text="结束"}}};
            string end=ToolLogic.Exit(second,"next");
            branch.edges.Add(new ToolGraphEdge{slot="yes",target=end});branch.edges.Add(new ToolGraphEdge{slot="no",target=end});branch.edges.Add(new ToolGraphEdge{slot="silence",target=end});
            draft.graph.nodes.Add(branch);second.edges[0].target=branch.id;
            var extra=ToolV2Authoring.AddUnit(draft,branch);
            Check(branch.edges.Count==3&&branch.edges.All(e=>e.target==end),"Adding a unit never overwrites authored choices");
            draft.graph.nodes.Remove(extra);
            var saved=ToolV2Authoring.SaveDraft(draft,"Assets/AuthoringQA","Meeting");
            Check(AssetDatabase.Contains(saved)&&AssetDatabase.Contains(saved.graph)&&ToolV2Authoring.Units(saved).All(AssetDatabase.Contains),"Save creates package, graph and every unit automatically");
            Check(saved.graph.Node(first.id).unit!=first.unit&&!AssetDatabase.Contains(draft),"Save keeps the original draft intact");
            Check(ToolValidation.Package(saved).Count==0&&saved.library==library,"Saved package validates and shares the original resource library");
            var another=ToolV2Authoring.SaveDraft(draft,"Assets/AuthoringQA","Meeting");
            Check(AssetDatabase.GetAssetPath(saved)!=AssetDatabase.GetAssetPath(another),"Repeated name creates a new folder instead of overwriting");
            bool rejected=false;try{ToolV2Authoring.SaveDraft(draft,"Assets/../../outside","Invalid");}catch(ArgumentException){rejected=true;}
            Check(rejected,"Save rejects destinations outside Assets");
            var duplicate=ToolV2Authoring.CloneDraft(saved);duplicate.graph.Node(first.id).unit.dialogue[0].text="独立副本";
            Check(saved.graph.Node(first.id).unit.dialogue[0].text!="独立副本","Copy as new performance isolates units and graph");
            var appended=ToolV2Authoring.AddUnit(saved,saved.graph.Node(first.id));AssetDatabase.SaveAssets();
            Check(AssetDatabase.Contains(appended.unit)&&ToolValidation.Package(saved).Count==0,"Adding a unit to an existing package automatically saves its asset");
            string export=Path.GetFullPath("../Export-QA");ToolV2MeetingExport.Export(saved,export,"authoring_qa");
            Check(File.Exists(export+"/Assets/AstraToolV2/Resources/Performances/authoring_qa.asset"),"Game export creates the ID-addressable package");
            Check(File.Exists(export+"/"+AssetDatabase.GetAssetPath(saved.graph))&&File.Exists(export+"/"+AssetDatabase.GetAssetPath(saved.graph)+".meta"),"Game export includes graph and reference GUIDs");
            Check(File.Exists(export+"/Assets/AstraToolV2/Runtime/ToolPlayer.cs")&&File.Exists(export+"/meeting-export.txt"),"Game export includes player and manifest");
            Check(EditorJsonUtility.ToJson(sample.graph)==original&&EditorJsonUtility.ToJson(library)==libOriginal,"Original user graph and library remain untouched");
            ToolV2Authoring.SelectPackage(saved);ToolV2TimelineWindow.Open();ToolV2GraphWindow.Open();
            var timeline=EditorWindow.GetWindow<ToolV2TimelineWindow>();var graph=EditorWindow.GetWindow<ToolV2GraphWindow>();
            Check(Field<ToolPackage>(timeline,"package")==saved&&Field<ToolPackage>(graph,"package")==saved,"Both editor windows use the selected performance");
            ToolV2Authoring.SelectUnit(appended.unit);
            Check(Field<ToolUnit>(timeline,"unit")==appended.unit&&Field<ToolGraphNode>(graph,"selected").unit==appended.unit,"Selecting a unit synchronizes timeline and graph");
            timeline.Close();graph.Close();
            ToolV2Authoring.WriteSnapshot(duplicate,ToolV2Authoring.RecoveryPath);
            Check(ToolV2Authoring.ReadSnapshot(ToolV2Authoring.RecoveryPath).graph.Node(first.id).unit.dialogue[0].text=="独立副本","Unsaved draft remains recoverable across an editor restart");
            lines.Add("RESULT PASS checks="+lines.Count);
            Directory.CreateDirectory("../QA");File.WriteAllLines("../QA/authoring-verification.txt",lines);Debug.Log("ASTRA_AUTHORING_QA_SUCCEEDED "+lines.Count);
        }
        catch(Exception e){lines.Add("FAIL "+e);Directory.CreateDirectory("../QA");File.WriteAllLines("../QA/authoring-verification.txt",lines);Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static T Field<T>(object value,string name)=>(T)value.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(value);
}

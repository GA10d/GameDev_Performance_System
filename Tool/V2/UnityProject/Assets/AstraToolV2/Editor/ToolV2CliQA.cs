using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceToolV2;

public static class ToolV2CliQA
{
    static readonly List<string> Checks=new List<string>();
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);Checks.Add("PASS "+text);}
    static void Guard(){if(!Environment.GetCommandLineArgs().Contains("-cliQA"))throw new Exception("Use -cliQA in an isolated project");}
    static ToolV2Cli.Response Call(ToolV2Cli.Request r){var response=ToolV2Cli.Execute(r);Check(response.ok,r.command+": "+response.message);return response;}
    static ToolV2Cli.Spec Copy(ToolV2Cli.Spec s)=>JsonUtility.FromJson<ToolV2Cli.Spec>(JsonUtility.ToJson(s));
    static ToolV2Cli.Request SpecRequest(string command,ToolV2Cli.Spec spec)=>new ToolV2Cli.Request{command=command,specJson=JsonUtility.ToJson(spec)};
    public static void Run()
    {
        Guard();int exit=0;
        try
        {
            Call(new ToolV2Cli.Request{command="catalog"});
            foreach(string type in Enum.GetNames(typeof(ToolPerformanceTemplate)))
            {
                var created=Call(new ToolV2Cli.Request{command="template",template=type,name="CLI "+type});
                var s=JsonUtility.FromJson<ToolV2Cli.Spec>(created.dataJson);Call(SpecRequest("validate",s));
                var p=ToolV2Cli.Build(s);Check(JsonUtility.ToJson(ToolV2Cli.Describe(p))==JsonUtility.ToJson(s),type+" lossless JSON roundtrip");ToolV2Cli.Dispose(p);
            }
            var spec=JsonUtility.FromJson<ToolV2Cli.Spec>(Call(new ToolV2Cli.Request{command="template",template="Question",name="中文支线"}).dataJson);
            foreach(string route in new[]{"accept","decline","silence"})
            {
                var r=SpecRequest("simulate",spec);r.route=new[]{route};
                var response=Call(r);Check(response.dataJson.Contains(route=="accept"?"accepted":route=="decline"?"declined":"unanswered"),"correct ending: "+route);
            }
            var wrong=Copy(spec);wrong.units[0].actors[0].id="actor0";
            var bad=ToolV2Cli.Execute(SpecRequest("validate",wrong));
            Check(!bad.ok&&bad.issues.Any(i=>i.code=="ACTOR_NOT_FOUND"&&i.path.EndsWith("/actions/0/actorId")),"screenshot actor0/speaker mismatch gives exact path");
            wrong=Copy(spec);wrong.units[0].actors[0].duration=1;
            Check(ToolV2Cli.Execute(SpecRequest("validate",wrong)).issues.Any(i=>i.code=="ACTOR_COVERAGE"),"actor timing coverage distinct from missing ID");
            wrong=Copy(spec);wrong.nodes[0].edges[0].target="missing";
            Check(!ToolV2Cli.Execute(SpecRequest("validate",wrong)).ok,"dangling edge rejected");
            wrong=Copy(spec);wrong.nodes=wrong.nodes.Where(n=>n.id!="accepted").ToArray();wrong.nodes.First(n=>n.id=="accept_reply").edges[0].target="accept_reply";
            Check(ToolV2Cli.Execute(SpecRequest("validate",wrong)).issues.Any(i=>i.code=="NO_END_PATH"),"dead cycle rejected");
            wrong=Copy(spec);wrong.nodes=wrong.nodes.Concat(new[]{new ToolV2Cli.Node{id="unused",kind=ToolNodeKind.End}}).ToArray();
            Check(ToolV2Cli.Execute(SpecRequest("validate",wrong)).issues.Any(i=>i.code=="UNREACHABLE_NODE"&&i.severity=="warning"),"unreachable node warning");
            wrong=Copy(spec);wrong.units[0].dialogue[1].choices[0].hasCondition=true;wrong.units[0].dialogue[1].choices[0].condition.value=2;
            var blocked=SpecRequest("simulate",wrong);blocked.route=new[]{"accept"};Check(!ToolV2Cli.Execute(blocked).ok,"disabled choice rejected");
            wrong.initialStats=new[]{new ToolStat{key="credits",value=3}};wrong.units[0].dialogue[1].choices[0].effects=new[]{new ToolEffect{key="credits",delta=-2}};
            blocked.specJson=JsonUtility.ToJson(wrong);Check(Call(blocked).dataJson.Contains("\"value\": 1"),"choice resource effects applied");
            var extra=SpecRequest("simulate",spec);extra.route=new[]{"accept","decline"};Check(!ToolV2Cli.Execute(extra).ok,"unused route rejected");
            extra.route=new[]{"accept"};extra.waitSeconds=30;Check(!ToolV2Cli.Execute(extra).ok,"late choice rejected");
            // Condition and GlobalChoice use the same runtime ToolLogic and global patience budget.
            var full=Copy(spec);
            full.nodes=full.nodes.Concat(new[]{
                new ToolV2Cli.Node{id="gate",kind=ToolNodeKind.Condition,condition=new ToolCondition{key="credits",value=2},edges=new[]{new ToolGraphEdge{slot="yes",target="global"},new ToolGraphEdge{slot="no",target="opening"}}},
                new ToolV2Cli.Node{id="global",kind=ToolNodeKind.GlobalChoice,patienceSeconds=5,choices=new[]{new ToolChoice{id="go",text="继续",effects=new[]{new ToolEffect{key="credits",delta=-2}}}},edges=new[]{new ToolGraphEdge{slot="go",target="opening"},new ToolGraphEdge{slot="silence",target="unanswered"}}}
            }).ToArray();full.entryNode="gate";full.initialStats=new[]{new ToolStat{key="credits",value=3}};
            var chain=SpecRequest("simulate",full);chain.route=new[]{"go","accept"};chain.waitSeconds=1;
            Check(Call(chain).dataJson.Contains("global/go"),"condition yes and global selection route");
            chain.route=new[]{"silence"};Check(Call(chain).dataJson.Contains("unanswered"),"global timeout route");
            full.initialStats[0].value=0;chain.specJson=JsonUtility.ToJson(full);chain.route=new[]{"decline"};Check(Call(chain).dataJson.Contains("declined"),"condition no route");
            string folder="Assets/CliQA/Run_"+Guid.NewGuid().ToString("N");
            var apply=SpecRequest("apply",spec);apply.destination=folder;
            var sample=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolV2Build.PackagePath);
            EditorUtility.SetDirty(sample);Call(apply);Check(EditorUtility.IsDirty(sample),"apply does not save unrelated dirty assets");
            Check(!ToolV2Cli.Execute(apply).ok,"existing destination cannot be overwritten");
            apply.destination="Assets/../Outside";Check(!ToolV2Cli.Execute(apply).ok,"asset path escape rejected");
            var inspected=Call(new ToolV2Cli.Request{command="inspect",package=folder+"/Performance.asset"});
            Check(inspected.dataJson==JsonUtility.ToJson(spec,true),"saved package JSON matches original including Chinese text");
            string output=Path.GetFullPath("../CliQAExport_"+Guid.NewGuid().ToString("N"));
            var export=new ToolV2Cli.Request{command="export",package=folder+"/Performance.asset",output=output,performanceId="cli_qa"};
            Call(export);Check(EditorUtility.IsDirty(sample),"export does not save unrelated dirty assets");
            Check(File.Exists(output+"/Assets/AstraToolV2/Resources/Performances/cli_qa.asset.meta"),"export alias and meta exist");
            Check(File.Exists(output+"/"+folder+"/Graph.asset"),"export includes graph dependencies");
            Check(!ToolV2Cli.Execute(export).ok,"nonempty export destination rejected");
            export.output=Application.dataPath;Check(!ToolV2Cli.Execute(export).ok,"export into project rejected");
            var current=ToolV2Authoring.Current;Call(new ToolV2Cli.Request{command="template",template="Broadcast",name="Detached"});Check(ToolV2Authoring.Current==current,"CLI does not switch active authoring session");
            EditorUtility.ClearDirty(sample);
        }
        catch(Exception e){exit=1;Checks.Add("FAIL "+e);}
        File.WriteAllLines(Path.GetFullPath("../cli-verification.txt"),Checks);EditorApplication.Exit(exit);
    }
    public static void ServeForTests()
    {
        Guard();double until=EditorApplication.timeSinceStartup+180;
        EditorApplication.update+=()=>{if(EditorApplication.timeSinceStartup>until||File.Exists("Library/AstraCli/qa-stop"))EditorApplication.Exit(0);};
    }
}

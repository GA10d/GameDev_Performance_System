using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceToolV2;

// Exports a real ToolPackage and its referenced assets, with the same GUIDs/runtime.
public static class ToolV2MeetingExport
{
    const string Root="Assets/AstraToolV2";
    const string Sample=Root+"/Samples/Meeting/RelayMeeting.asset";
    [MenuItem("Astra Performance Tool V2/Export selected package for cabin meeting")]
    public static void ExportSelected()
    {
        var package=Selection.activeObject as ToolPackage;
        if(!package)throw new InvalidOperationException("请先选中 ToolPackage 资产");
        string folder=EditorUtility.OpenFolderPanel("导出到文件夹，再将其中 Assets 合并到舱室 UnityProject",Path.GetFullPath("../Export"),"");
        if(!string.IsNullOrEmpty(folder))Export(package,folder,package.name.ToLowerInvariant());
    }
    public static void ExportSample()
    {
        var args=Environment.GetCommandLineArgs();
        string folder=Argument(args,"-meetingOutput",Path.GetFullPath("../Export/Meeting"));
        string path=Argument(args,"-meetingPackage",Sample);
        string id=Argument(args,"-meetingId","relay");
        if(path==Sample&&!AssetDatabase.LoadAssetAtPath<ToolPackage>(path))CreateSample();
        var package=AssetDatabase.LoadAssetAtPath<ToolPackage>(path);
        Export(package,folder,id);
    }
    static string Argument(string[] args,string key,string fallback)
    {int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:fallback;}
    public static void Export(ToolPackage package,string folder,string id,bool saveAllAssets=true)
    {
        if(!Regex.IsMatch(id,"^[A-Za-z0-9_-]{1,64}$"))throw new ArgumentException("演出包 ID 只支持英文、数字、下划线、短横线");
        var errors=ToolValidation.Package(package);
        if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
        if(!package.graph.nodes.Any(n=>n.kind==ToolNodeKind.End))throw new InvalidOperationException("演出树必须包含 End 节点");
        if(saveAllAssets)AssetDatabase.SaveAssets();
        var paths=new HashSet<string>(AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(package),true));
        foreach(var file in Directory.GetFiles(Root+"/Runtime","*.cs"))
            if(!file.Contains("QA")&&!file.EndsWith("ToolWorkbench.cs"))paths.Add(file.Replace('\\','/'));
        foreach(var directory in new[]{Root+"/Resources",Root+"/Shaders"})
            foreach(var file in Directory.GetFiles(directory,"*",SearchOption.AllDirectories))
                if(!file.EndsWith(".meta"))paths.Add(file.Replace('\\','/'));
        foreach(var path in paths)
        {
            if(!path.StartsWith("Assets/")||!File.Exists(path))continue;
            string target=Path.Combine(folder,path);Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.Copy(path,target,true);if(File.Exists(path+".meta"))File.Copy(path+".meta",target+".meta",true);
        }
        // A stable Resources address independent of the authored package's folder/name.
        string alias=Path.Combine(folder,"Assets/AstraToolV2/Resources/Performances/"+id+".asset");
        Directory.CreateDirectory(Path.GetDirectoryName(alias));File.Copy(AssetDatabase.GetAssetPath(package),alias,true);
        if(!File.Exists(alias+".meta"))File.WriteAllText(alias+".meta","fileFormatVersion: 2\nguid: "+Guid.NewGuid().ToString("N")+"\n");
        File.WriteAllText(Path.Combine(folder,"meeting-export.txt"),"Package: "+AssetDatabase.GetAssetPath(package)+"\nPerformance ID: "+id+"\nUnity: "+Application.unityVersion+"\nExported files: "+paths.Count);
        Debug.Log("ASTRA_MEETING_EXPORT_SUCCEEDED "+folder);
    }
    static ToolGraphEdge Edge(string slot,string target)=>new ToolGraphEdge{slot=slot,target=target};
    static ToolUnit Unit(string id,ToolCharacter character,string text,float duration)
    {
        var unit=ScriptableObject.CreateInstance<ToolUnit>();unit.id=id;unit.duration=duration;
        unit.actors.Add(new ToolActorSpan{id="speaker",character=character,start=0,duration=duration});
        unit.actions.Add(new ToolActionSpan{actorId="speaker",actionId="Idle_Talking_Loop",start=0,duration=duration});
        unit.dialogue.Add(new ToolDialogueSpan{actorId="speaker",text=text,start=.3f,charactersPerSecond=15,hold=1});
        unit.scenes.Add(new ToolSceneSpan{sceneId="archive",start=0,duration=duration});
        unit.cameras.Add(new ToolCameraSpan{actorId="speaker",shot=ToolShot.Medium,start=0,duration=duration});
        AssetDatabase.CreateAsset(unit,Root+"/Samples/Meeting/"+id+".asset");return unit;
    }
    // Created once; future exports preserve edits made with the existing timeline/graph editors.
    static void CreateSample()
    {
        Directory.CreateDirectory(Root+"/Samples/Meeting");AssetDatabase.Refresh();
        var original=AssetDatabase.LoadAssetAtPath<ToolPackage>(Root+"/Samples/AstraSample.asset");
        var human=original.library.characters.First(c=>c&&c.species==ToolSpecies.Human);
        var alien=original.library.characters.First(c=>c&&c.species==ToolSpecies.StandingAlien);
        var intro=Unit("Meeting_Opening",human,"探测舱 04，这里是中继站。信号确认，准备交接地球档案。",19);
        intro.dialogue.Add(new ToolDialogueSpan{actorId="speaker",kind=ToolDialogueKind.Choice,start=5,patienceSeconds=14,text="中继电源不足。要消耗两张维修券恢复链路吗？",choices=new[]{
            new ToolChoice{id="repair",text="使用维修券，恢复中继",hasCondition=true,condition=new ToolCondition{key="credits",compare=ToolCompare.GreaterOrEqual,value=2},effects=new[]{new ToolEffect{key="credits",delta=-2}}},
            new ToolChoice{id="inspect",text="请值班员检查备用线路"}}});
        EditorUtility.SetDirty(intro);
        var repair=Unit("Meeting_Repair",human,"主链路已恢复。谢谢你的支援，档案可以完整传输了。",7);
        var contact=Unit("Meeting_Contact",alien,"我是备用站值班员。已接通低功耗线路，档案仍然可以传输。",7);
        var graph=ScriptableObject.CreateInstance<ToolGraph>();graph.entryNode="opening";
        graph.nodes.Add(new ToolGraphNode{id="opening",unit=intro,position=new Vector2(30,100),edges=new[]{Edge("repair","power"),Edge("inspect","contact"),Edge("silence","contact")}.ToList()});
        graph.nodes.Add(new ToolGraphNode{id="power",kind=ToolNodeKind.Condition,position=new Vector2(330,20),condition=new ToolCondition{key="power",compare=ToolCompare.GreaterOrEqual,value=1},edges=new[]{Edge("yes","repair"),Edge("no","contact")}.ToList()});
        graph.nodes.Add(new ToolGraphNode{id="repair",unit=repair,position=new Vector2(630,20),edges=new[]{Edge("next","archive")}.ToList()});
        graph.nodes.Add(new ToolGraphNode{id="contact",unit=contact,position=new Vector2(630,250),edges=new[]{Edge("next","archive")}.ToList()});
        graph.nodes.Add(new ToolGraphNode{id="archive",kind=ToolNodeKind.GlobalChoice,position=new Vector2(930,100),prompt="档案传输完成。如何保存这份记录？",patienceSeconds=18,choices=new[]{new ToolChoice{id="publish",text="公开记录"},new ToolChoice{id="seal",text="封存记录"}},edges=new[]{Edge("publish","published"),Edge("seal","sealed"),Edge("silence","deferred")}.ToList()});
        int i=0;foreach(string ending in new[]{"published","sealed","deferred"})graph.nodes.Add(new ToolGraphNode{id=ending,kind=ToolNodeKind.End,position=new Vector2(1250,20+140*i++)});
        AssetDatabase.CreateAsset(graph,Root+"/Samples/Meeting/MeetingGraph.asset");
        var package=ScriptableObject.CreateInstance<ToolPackage>();package.library=original.library;package.graph=graph;package.initialPatience=40;
        package.initialStats=new[]{new ToolStat{key="credits",value=3},new ToolStat{key="power",value=1}};
        AssetDatabase.CreateAsset(package,Sample);AssetDatabase.SaveAssets();
    }
}

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceToolV2;
using Object = UnityEngine.Object;

// File transport only: no socket, shell execution, or changes to the active authoring session.
[InitializeOnLoad]
public static class ToolV2Cli
{
    [Serializable] public class Request
    {
        public int version=1;
        public string id,command,token,specJson,package,destination,output,performanceId;
        public string library="Assets/AstraToolV2/Samples/Library.asset";
        public string template="Question",name="AI Performance",speaker,partner,paramsJson;
        public string[] route=Array.Empty<string>();
        public float waitSeconds;
    }
    [Serializable] public class Issue
    {
        public string code,path,message,severity="error";
        public Issue(string c,string p,string m,string s="error"){code=c;path=p;message=m;severity=s;}
    }
    [Serializable] public class Response
    {
        public int version=1;
        public bool ok;
        public string id,code,message,dataJson;
        public List<Issue> issues=new List<Issue>();
    }
    [Serializable] public class Spec
    {
        public int schemaVersion=1;
        public string name,library,entryNode;
        public int initialPatience=30;
        public ToolStat[] initialStats=Array.Empty<ToolStat>();
        public Unit[] units=Array.Empty<Unit>();
        public Node[] nodes=Array.Empty<Node>();
    }
    [Serializable] public class Actor
    {
        public string id,character;
        public float start,duration;
    }
    [Serializable] public class Unit
    {
        public string id;
        public float duration;
        public Actor[] actors=Array.Empty<Actor>();
        public ToolActionSpan[] actions=Array.Empty<ToolActionSpan>();
        public ToolDialogueSpan[] dialogue=Array.Empty<ToolDialogueSpan>();
        public ToolSceneSpan[] scenes=Array.Empty<ToolSceneSpan>();
        public ToolCameraSpan[] cameras=Array.Empty<ToolCameraSpan>();
    }
    [Serializable] public class Node
    {
        public string id,unit,prompt;
        public ToolNodeKind kind;
        public Vector2 position;
        public ToolCondition condition=new ToolCondition();
        public float patienceSeconds=6;
        public ToolChoice[] choices=Array.Empty<ToolChoice>();
        public ToolGraphEdge[] edges=Array.Empty<ToolGraphEdge>();
    }
    [Serializable] class CharacterInfo { public string id,name,path,species; }
    [Serializable] class Catalog
    {
        public string library;
        public CharacterInfo[] characters;
        public ToolAction[] actions;
        public ToolScenePreset[] scenes;
        public string[] shots=Enum.GetNames(typeof(ToolShot)),effects=Enum.GetNames(typeof(ToolFx));
    }
    [Serializable] class Saved { public string package,destination,performanceId; }
    [Serializable] class Trace
    {
        public string ending;
        public List<string> visited=new List<string>(),selections=new List<string>();
        public ToolStat[] stats;
        public string mode="logic-only";
    }
    [Serializable] class Heartbeat { public int version=1,pid; public string token,project; }
    static readonly string Queue=Path.GetFullPath("Library/AstraCli");
    static readonly string Token=Guid.NewGuid().ToString("N");
    static double nextPoll;
    static ToolV2Cli(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<nextPoll)return;
        nextPoll=EditorApplication.timeSinceStartup+1;
        try
        {
            Directory.CreateDirectory(Queue);
            Atomic(Path.Combine(Queue,"heartbeat.json"),JsonUtility.ToJson(new Heartbeat{pid=Process.GetCurrentProcess().Id,token=Token,project=Path.GetFullPath(".")}));
            foreach(string path in Directory.GetFiles(Queue,"*.request.json").OrderBy(File.GetCreationTimeUtc).Take(1))
            {
                var request=JsonUtility.FromJson<Request>(File.ReadAllText(path,Encoding.UTF8));
                string key=Path.GetFileName(path).Replace(".request.json","");
                if(request==null||request.id!=key||request.token!=Token) {File.Move(path,path+".rejected");continue;}
                // Claim before execution. A client can safely cancel only an unclaimed request.
                File.Move(path,path+".running");
                Atomic(Path.Combine(Queue,key+".response.json"),JsonUtility.ToJson(Execute(request),true));
                File.Delete(path+".running");
            }
        }
        catch(Exception e){UnityEngine.Debug.LogWarning("ASTRA_CLI_TRANSPORT: "+e.Message);}
    }
    public static void Batch()
    {
        var args=Environment.GetCommandLineArgs();
        int index=Array.IndexOf(args,"-astraRequest");
        if(index<0||index+1>=args.Length){EditorApplication.Exit(2);return;}
        string path=Path.GetFullPath(args[index+1]);Response response;
        try{response=Execute(JsonUtility.FromJson<Request>(File.ReadAllText(path,Encoding.UTF8)));}
        catch(Exception e){response=new Response{code="REQUEST_INVALID",message=e.Message};}
        Atomic(path+".response.json",JsonUtility.ToJson(response,true));
        EditorApplication.Exit(response.ok?0:2);
    }
    static void Atomic(string path,string content)
    {
        string temp=path+".tmp";File.WriteAllText(temp,content,new UTF8Encoding(false));
        if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);
    }
    static T Asset<T>(string path) where T:Object
    {
        if(string.IsNullOrWhiteSpace(path)||!path.StartsWith("Assets/",StringComparison.Ordinal)||path.Contains("..")||path.Contains('\\'))throw new ArgumentException("Use an Assets/ path: "+path);
        var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(!asset)throw new ArgumentException("Asset not found: "+path);return asset;
    }
    static ToolCharacter Character(ToolLibrary library,string reference)
    {
        var matches=(library.characters??Array.Empty<ToolCharacter>()).Where(c=>c&&(AssetDatabase.GetAssetPath(c)==reference||c.id==reference)).Distinct().ToArray();
        if(matches.Length!=1)throw new ArgumentException("Character missing or ambiguous; use catalog asset path: "+reference);
        return matches[0];
    }
    public static Response Execute(Request request)
    {
        var response=new Response{id=request?.id};ToolPackage temporary=null;
        try
        {
            if(request==null||request.version!=1)throw new ArgumentException("Unsupported request version");
            object result=null;
            switch(request.command)
            {
                case "catalog":
                    var library=Asset<ToolLibrary>(request.library);
                    result=new Catalog{library=request.library,characters=library.characters.Where(c=>c).Select(c=>new CharacterInfo{id=c.id,name=c.displayName,path=AssetDatabase.GetAssetPath(c),species=c.species.ToString()}).ToArray(),actions=library.actions,scenes=library.scenes};break;
                case "template":
                    var lib=Asset<ToolLibrary>(request.library);
                    if(!Enum.TryParse(request.template,false,out ToolPerformanceTemplate type)||!Enum.IsDefined(typeof(ToolPerformanceTemplate),type))throw new ArgumentException("Unknown template");
                    var setup=ToolV2Templates.Defaults(type,lib);
                    if(!string.IsNullOrEmpty(request.paramsJson))JsonUtility.FromJsonOverwrite(request.paramsJson,setup);
                    setup.template=type;setup.name=request.name;
                    if(!string.IsNullOrEmpty(request.speaker))setup.speaker=Character(lib,request.speaker);
                    if(!string.IsNullOrEmpty(request.partner))setup.partner=Character(lib,request.partner);
                    temporary=ToolV2Templates.Create(lib,setup);result=Describe(temporary);break;
                case "inspect":result=Describe(Asset<ToolPackage>(request.package));break;
                case "validate":case "apply":case "simulate":case "export":
                    ToolPackage package;
                    if(!string.IsNullOrEmpty(request.specJson)){temporary=Build(JsonUtility.FromJson<Spec>(request.specJson));package=temporary;}
                    else package=Asset<ToolPackage>(request.package);
                    response.issues=Validate(package);
                    if(response.issues.Any(i=>i.severity=="error")){response.code="VALIDATION_FAILED";response.message="Fix reported fields before generating assets or exporting.";return response;}
                    if(request.command=="validate")result=Describe(package);
                    if(request.command=="simulate")result=Simulate(package,request.route,request.waitSeconds);
                    if(request.command=="apply")
                    {
                        if(!temporary)throw new ArgumentException("apply requires a spec");
                        string dest=NewAssetFolder(request.destination);
                        var saved=SaveNew(package,dest);
                        result=new Saved{package=AssetDatabase.GetAssetPath(saved),destination=dest};
                    }
                    if(request.command=="export")
                    {
                        if(temporary)throw new ArgumentException("Export requires a saved package; apply first.");
                        // Loading shaders can mark compilation caches dirty; their source is already on disk.
                        var dirty=AssetDatabase.GetDependencies(request.package,true).SelectMany(AssetDatabase.LoadAllAssetsAtPath).Where(o=>!(o is Shader)&&!(o is MonoScript)&&EditorUtility.IsDirty(o)).ToArray();
                        if(dirty.Length>0)throw new InvalidOperationException("Save this package and its dependencies in Unity before export: "+string.Join(", ",dirty.Select(o=>AssetDatabase.GetAssetPath(o)+" ("+o.GetType().Name+")")));
                        string output=Path.GetFullPath(request.output??"");
                        string project=Path.GetFullPath(".");
                        if(IsWithin(output,project)||IsWithin(project,output))throw new ArgumentException("Export destination must be outside the Unity project and its ancestors.");
                        if(Directory.Exists(output)&&Directory.EnumerateFileSystemEntries(output).Any())throw new ArgumentException("Export destination must be new or empty; use a new version folder.");
                        ToolV2MeetingExport.Export(package,output,request.performanceId,false);
                        result=new Saved{package=request.package,destination=output,performanceId=request.performanceId};
                    }
                    break;
                default:throw new ArgumentException("Unknown command: "+request.command);
            }
            response.ok=true;response.code="OK";response.message="Completed";response.dataJson=JsonUtility.ToJson(result,true);
        }
        catch(Exception e){response.code=e is ArgumentException?"INVALID_ARGUMENT":"OPERATION_FAILED";response.message=e.Message;}
        finally{Dispose(temporary);}
        return response;
    }
    static bool IsWithin(string child,string parent)
    {
        child=Path.GetFullPath(child).Replace('\\','/').TrimEnd('/');parent=Path.GetFullPath(parent).Replace('\\','/').TrimEnd('/');
        return child.Equals(parent,StringComparison.OrdinalIgnoreCase)||child.StartsWith(parent+"/",StringComparison.OrdinalIgnoreCase);
    }
    static ToolPackage SaveNew(ToolPackage source,string folder)
    {
        var copy=ToolV2Authoring.CloneDraft(source);copy.name=source.name;copy.graph.name=source.name+"_Graph";
        try
        {
            Directory.CreateDirectory(folder+"/Units");AssetDatabase.Refresh();
            int index=0;
            foreach(var unit in ToolV2Authoring.Units(copy))
            {
                unit.hideFlags=HideFlags.None;AssetDatabase.CreateAsset(unit,folder+"/Units/Unit_"+(++index)+".asset");AssetDatabase.SaveAssetIfDirty(unit);
            }
            copy.graph.hideFlags=HideFlags.None;AssetDatabase.CreateAsset(copy.graph,folder+"/Graph.asset");AssetDatabase.SaveAssetIfDirty(copy.graph);
            copy.hideFlags=HideFlags.None;AssetDatabase.CreateAsset(copy,folder+"/Performance.asset");
            copy.name=source.name;EditorUtility.SetDirty(copy);AssetDatabase.SaveAssetIfDirty(copy);
            return copy;
        }
        catch
        {
            // The folder was verified absent before this operation; never delete a pre-existing asset.
            AssetDatabase.DeleteAsset(folder);Dispose(copy);throw;
        }
    }
    static string NewAssetFolder(string destination)
    {
        if(string.IsNullOrWhiteSpace(destination)||!destination.StartsWith("Assets/",StringComparison.Ordinal)||destination.Contains('\\'))throw new ArgumentException("destination must be Assets/.../NewVersion");
        string full=Path.GetFullPath(destination);
        if(!IsWithin(full,Application.dataPath)||destination.Split('/').Any(p=>p==".."||p=="."||string.IsNullOrWhiteSpace(p)))throw new ArgumentException("Invalid asset destination");
        if(Directory.Exists(full)||File.Exists(full)||File.Exists(full+".meta"))throw new ArgumentException("Destination already exists; choose a new version folder.");
        return destination.TrimEnd('/');
    }
    public static Spec Describe(ToolPackage package)
    {
        var units=ToolV2Authoring.Units(package);
        if(units.Select(u=>u.id).Distinct().Count()!=units.Length)throw new ArgumentException("Distinct units have duplicate IDs; rename them in Unity before inspect.");
        return new Spec{name=package.name,library=AssetDatabase.GetAssetPath(package.library),entryNode=package.graph.entryNode,initialPatience=package.initialPatience,initialStats=package.initialStats,
            units=units.Select(u=>new Unit{id=u.id,duration=u.duration,actors=u.actors.Select(a=>new Actor{id=a.id,character=AssetDatabase.GetAssetPath(a.character),start=a.start,duration=a.duration}).ToArray(),actions=u.actions.ToArray(),dialogue=u.dialogue.ToArray(),scenes=u.scenes.ToArray(),cameras=u.cameras.ToArray()}).ToArray(),
            nodes=package.graph.nodes.Select(n=>new Node{id=n.id,kind=n.kind,unit=n.unit?n.unit.id:null,position=n.position,condition=n.condition,prompt=n.prompt,patienceSeconds=n.patienceSeconds,choices=n.choices,edges=n.edges.ToArray()}).ToArray()};
    }
    public static ToolPackage Build(Spec spec)
    {
        if(spec==null||spec.schemaVersion!=1||string.IsNullOrWhiteSpace(spec.name))throw new ArgumentException("Invalid spec version or name");
        var p=ScriptableObject.CreateInstance<ToolPackage>();p.hideFlags=HideFlags.DontSave;
        var made=new List<ToolUnit>();
        try
        {
            p.name=spec.name;p.library=Asset<ToolLibrary>(spec.library);p.initialPatience=spec.initialPatience;p.initialStats=spec.initialStats;
            p.graph=ScriptableObject.CreateInstance<ToolGraph>();p.graph.hideFlags=HideFlags.DontSave;p.graph.entryNode=spec.entryNode;
            var units=new Dictionary<string,ToolUnit>();
            foreach(var source in spec.units)
            {
                if(string.IsNullOrWhiteSpace(source.id)||units.ContainsKey(source.id))throw new ArgumentException("Duplicate/empty unit ID: "+source.id);
                var u=ScriptableObject.CreateInstance<ToolUnit>();u.hideFlags=HideFlags.DontSave;made.Add(u);u.id=source.id;u.name=source.id;u.duration=source.duration;
                u.actors=source.actors.Select(a=>new ToolActorSpan{id=a.id,character=Character(p.library,a.character),start=a.start,duration=a.duration}).ToList();
                u.actions=source.actions.ToList();u.dialogue=source.dialogue.ToList();u.scenes=source.scenes.ToList();u.cameras=source.cameras.ToList();units.Add(u.id,u);
            }
            foreach(var n in spec.nodes)
            {
                ToolUnit unit=null;
                if(n.kind==ToolNodeKind.Unit&&!units.TryGetValue(n.unit??"",out unit))throw new ArgumentException("nodes/"+n.id+"/unit: unknown unit "+n.unit);
                p.graph.nodes.Add(new ToolGraphNode{id=n.id,kind=n.kind,unit=unit,position=n.position,condition=n.condition,prompt=n.prompt,patienceSeconds=n.patienceSeconds,choices=n.choices,edges=n.edges.ToList()});
            }
            if(made.Any(u=>!p.graph.nodes.Any(n=>n.unit==u)))throw new ArgumentException("Unreferenced units would be lost; connect or remove them.");
            return p;
        }
        catch{foreach(var u in made)Object.DestroyImmediate(u);if(p.graph)Object.DestroyImmediate(p.graph);Object.DestroyImmediate(p);throw;}
    }
    public static void Dispose(ToolPackage p)
    {
        if(!p||AssetDatabase.Contains(p))return;
        foreach(var unit in ToolV2Authoring.Units(p))if(unit&&!AssetDatabase.Contains(unit))Object.DestroyImmediate(unit);
        if(p.graph&&!AssetDatabase.Contains(p.graph))Object.DestroyImmediate(p.graph);Object.DestroyImmediate(p);
    }
    public static List<Issue> Validate(ToolPackage p)
    {
        var issues=new List<Issue>();
        foreach(string error in ToolValidation.Package(p))issues.Add(new Issue("UNITY_VALIDATION","/",error));
        if(!p||!p.graph||!p.library)return issues;
        foreach(var u in ToolV2Authoring.Units(p))
        {
            string root="/units/"+u.id;
            foreach(var group in u.actors.GroupBy(a=>a.id))
            {
                var spans=group.OrderBy(a=>a.start).ToArray();
                if(string.IsNullOrWhiteSpace(group.Key))issues.Add(new Issue("ACTOR_ID_EMPTY",root+"/actors","Actor ID is empty"));
                for(int i=1;i<spans.Length;i++)if(spans.Take(i).Any(a=>a.start+a.duration>spans[i].start+.001f))issues.Add(new Issue("ACTOR_OVERLAP",root+"/actors/"+group.Key,"Same actor ID has overlapping spans"));
            }
            Action<string,string,float,float> binding=(id,path,start,duration)=>
            {
                var actors=u.actors.Where(a=>a.id==id).ToArray();
                if(actors.Length==0)issues.Add(new Issue("ACTOR_NOT_FOUND",path,"Referenced actor '"+id+"' does not exist. Available: "+string.Join(",",u.actors.Select(a=>a.id))));
                else if(!actors.Any(a=>a.start<=start+.001f&&a.start+a.duration>=start+duration-.001f))issues.Add(new Issue("ACTOR_COVERAGE",path,"Actor must cover ["+start+", "+(start+duration)+"] seconds"));
            };
            for(int i=0;i<u.actions.Count;i++)binding(u.actions[i].actorId,root+"/actions/"+i+"/actorId",u.actions[i].start,u.actions[i].duration);
            for(int i=0;i<u.dialogue.Count;i++)if(!string.IsNullOrEmpty(u.dialogue[i].actorId))binding(u.dialogue[i].actorId,root+"/dialogue/"+i+"/actorId",u.dialogue[i].start,u.dialogue[i].Duration);
            for(int i=0;i<u.cameras.Count;i++)
            {
                var c=u.cameras[i];if(!string.IsNullOrEmpty(c.actorId))binding(c.actorId,root+"/cameras/"+i+"/actorId",c.start,c.duration);
                if(!string.IsNullOrEmpty(c.partnerId))binding(c.partnerId,root+"/cameras/"+i+"/partnerId",c.start,c.duration);
                if((c.shot==ToolShot.TwoShot||c.shot==ToolShot.OverShoulder)&&(string.IsNullOrEmpty(c.partnerId)||c.actorId==c.partnerId))issues.Add(new Issue("CAMERA_PARTNER",root+"/cameras/"+i,"Two-person shot requires distinct subject and partner"));
            }
        }
        var graph=p.graph;
        var reachable=new HashSet<string>();var queue=new Queue<string>();queue.Enqueue(graph.entryNode??"");
        while(queue.Count>0){var id=queue.Dequeue();if(!reachable.Add(id))continue;var n=graph.Node(id);if(n==null||n.kind==ToolNodeKind.End)continue;foreach(var e in n.edges)queue.Enqueue(e.target);}
        var ends=new HashSet<string>(graph.nodes.Where(n=>n.kind==ToolNodeKind.End).Select(n=>n.id));
        bool changed;do{changed=false;foreach(var n in graph.nodes)if(n.edges.Any(e=>ends.Contains(e.target)))changed|=ends.Add(n.id);}while(changed);
        foreach(var n in graph.nodes)
        {
            if(!reachable.Contains(n.id))issues.Add(new Issue("UNREACHABLE_NODE","/nodes/"+n.id,"Not reachable from entry", "warning"));
            else if(!ends.Contains(n.id))issues.Add(new Issue("NO_END_PATH","/nodes/"+n.id,"No structural path to End"));
            if(n.kind==ToolNodeKind.End&&n.edges.Count>0)issues.Add(new Issue("END_HAS_EDGES","/nodes/"+n.id,"End nodes must not have outgoing edges"));
            if(n.kind==ToolNodeKind.GlobalChoice&&(!ToolTimelineEditing.Finite(n.patienceSeconds)||n.patienceSeconds<=0))issues.Add(new Issue("INVALID_TIMEOUT","/nodes/"+n.id,"Timeout must be positive"));
        }
        return issues;
    }
    public static object Simulate(ToolPackage p,string[] route,float wait)
    {
        if(wait<0||!ToolTimelineEditing.Finite(wait))throw new ArgumentException("waitSeconds must be finite and non-negative");
        var trace=new Trace();var state=p.initialStats.ToDictionary(s=>s.key,s=>s.value);state["patience"]=p.initialPatience;
        string id=p.graph.entryNode;int choiceIndex=0;route=route??Array.Empty<string>();
        for(int step=0;step<128;step++)
        {
            var n=p.graph.Node(id);trace.visited.Add(id);
            if(n.kind==ToolNodeKind.End)
            {
                if(choiceIndex!=route.Length)throw new ArgumentException("Unused route choices: "+(route.Length-choiceIndex));
                trace.ending=id;trace.stats=state.Select(kv=>new ToolStat{key=kv.Key,value=kv.Value}).ToArray();return trace;
            }
            string slot="next";
            if(n.kind==ToolNodeKind.Condition)slot=ToolLogic.Test(n.condition,state)?"yes":"no";
            else
            {
                var dialogue=n.unit?n.unit.dialogue.OrderBy(d=>d.start).LastOrDefault():null;
                bool choosing=n.kind==ToolNodeKind.GlobalChoice||dialogue!=null&&dialogue.kind==ToolDialogueKind.Choice;
                if(choosing)
                {
                    slot=choiceIndex<route.Length?route[choiceIndex++]:"silence";
                    float timeout=n.kind==ToolNodeKind.GlobalChoice?Mathf.Min(n.patienceSeconds,Mathf.Max(0,state["patience"])):dialogue.patienceSeconds;
                    float elapsed=slot=="silence"?timeout:wait;
                    if(slot!="silence"&&elapsed>=timeout)throw new ArgumentException("Choice timed out at "+id+"; use silence or a shorter wait.");
                    if(slot!="silence")
                    {
                        var options=n.kind==ToolNodeKind.GlobalChoice?n.choices:dialogue.choices;
                        var option=ToolLogic.Choice(options,slot);
                        if(option==null||option.hasCondition&&!ToolLogic.Test(option.condition,state))throw new ArgumentException("Choice missing or disabled: "+id+"/"+slot);
                        ToolLogic.Apply(option.effects,state);
                    }
                    state["patience"]=Mathf.Max(0,state["patience"]-Mathf.CeilToInt(elapsed));trace.selections.Add(id+"/"+slot);
                }
            }
            id=ToolLogic.Exit(n,slot);
        }
        throw new InvalidOperationException("Runtime transition limit (128) exceeded; check cycles/route.");
    }
}

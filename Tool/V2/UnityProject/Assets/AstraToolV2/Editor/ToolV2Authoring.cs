using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Astra.PerformanceToolV2;
using Object = UnityEngine.Object;

// One authoring session shared by the timeline and graph. Drafts need no Assets files.
[InitializeOnLoad]
public static class ToolV2Authoring
{
    public const string DefaultFolder="Assets/AstraToolV2/Performances";
    public const string RecoveryPath="Library/AstraPerformanceDraft.asset";
    static ToolPackage current;
    static ToolUnit currentUnit;
    static bool initialized;
    static double nextBackup;
    public static event Action Changed;
    public static ToolPackage Current { get { Ensure(); return current; } }
    public static ToolUnit Unit { get { Ensure(); return currentUnit; } }
    public static bool IsDraft(ToolPackage p)=>p&&!AssetDatabase.Contains(p);
    static ToolV2Authoring()
    {
        AssemblyReloadEvents.beforeAssemblyReload+=Backup;
        EditorApplication.quitting+=Backup;
        EditorApplication.update+=()=>{if(!EditorApplication.isPlayingOrWillChangePlaymode&&EditorApplication.timeSinceStartup>nextBackup){nextBackup=EditorApplication.timeSinceStartup+3;Backup();}};
    }
    static void Ensure()
    {
        if(initialized)return;initialized=true;
        foreach(string recovery in new[]{RecoveryPath,RecoveryPath+".bak"})
        {
            if(!File.Exists(recovery))continue;
            try { current=ReadSnapshot(recovery);break; }
            catch(Exception e){Debug.LogWarning("演出草稿恢复失败，缓存文件仍保留："+e.Message);}
        }
        if(!current)current=AssetDatabase.LoadAssetAtPath<ToolPackage>(SessionState.GetString("Astra.Authoring.Package",ToolV2Build.PackagePath));
        currentUnit=Units(current).FirstOrDefault();
    }
    public static ToolUnit[] Units(ToolPackage p)=>p&&p.graph?p.graph.nodes.Where(n=>n.unit).Select(n=>n.unit).Distinct().ToArray():Array.Empty<ToolUnit>();
    static void Activate(ToolPackage p,ToolUnit u=null)
    {
        var previous=current;
        current=p;currentUnit=u?u:Units(p).FirstOrDefault();initialized=true;
        SessionState.SetString("Astra.Authoring.Package",p?AssetDatabase.GetAssetPath(p):"");
        Changed?.Invoke();Backup();
        if(previous&&previous!=p&&IsDraft(previous))
        {
            foreach(var old in Units(previous))if(!AssetDatabase.Contains(old))Object.DestroyImmediate(old);
            if(previous.graph&&!AssetDatabase.Contains(previous.graph))Object.DestroyImmediate(previous.graph);
            Object.DestroyImmediate(previous);
        }
    }
    public static void SelectUnit(ToolUnit u){if(currentUnit==u)return;currentUnit=u;Changed?.Invoke();}
    static void LeaveDraft(Action next)
    {
        if(!IsDraft(Current)){next();return;}
        int result=EditorUtility.DisplayDialogComplex("当前演出还未保存","保存当前草稿后继续，或者放弃这份草稿？","保存并继续","取消","放弃草稿");
        if(result==0)ToolV2PublishWindow.Open(false,()=>next());
        if(result==2){ClearBackup();next();}
    }
    public static void SelectPackage(ToolPackage p)
    {if(!p||p==Current)return;LeaveDraft(()=>Activate(p));}
    [MenuItem("Astra Performance Tool V2/New performance")]
    public static void NewPerformance()
    {
        var library=Current?Current.library:AssetDatabase.LoadAssetAtPath<ToolLibrary>(ToolV2Build.Root+"/Samples/Library.asset");
        if(!library){EditorUtility.DisplayDialog("缺少素材库","请先打开一个带素材库的现有演出包。","确定");return;}
        ToolV2TemplateWindow.Open(library);
    }
    public static void CreateFromTemplate(ToolLibrary library,ToolTemplateSettings settings,Action created=null)
    {
        var errors=ToolV2Templates.Validate(library,settings);
        if(errors.Count>0)throw new ArgumentException(string.Join("\n",errors));
        LeaveDraft(()=>{Activate(ToolV2Templates.Create(library,settings));ToolV2TimelineWindow.Open();created?.Invoke();});
    }
    public static ToolPackage CreateDraft(ToolLibrary library)
    {
        var p=ScriptableObject.CreateInstance<ToolPackage>();p.name="未命名演出";p.library=library;p.initialPatience=30;
        p.graph=ScriptableObject.CreateInstance<ToolGraph>();p.graph.name="演出树草稿";
        p.hideFlags=HideFlags.DontSave;p.graph.hideFlags=HideFlags.DontSave;
        p.graph.nodes.Add(new ToolGraphNode{id="end",kind=ToolNodeKind.End,position=new Vector2(470,100)});
        var n=AddUnit(p,null);p.graph.entryNode=n.id;return p;
    }
    public static ToolPackage CloneDraft(ToolPackage source)
    {
        var p=Object.Instantiate(source);p.name=source.name+"_副本";
        p.graph=Object.Instantiate(source.graph);p.graph.name=p.name+"_Graph";
        var mapping=new Dictionary<ToolUnit,ToolUnit>();
        foreach(var n in p.graph.nodes)if(n.unit)
        {if(!mapping.TryGetValue(n.unit,out var u)){u=Object.Instantiate(n.unit);u.name=n.unit.name;mapping.Add(n.unit,u);}n.unit=u;}
        p.hideFlags=HideFlags.DontSave;p.graph.hideFlags=HideFlags.DontSave;
        foreach(var u in mapping.Values)u.hideFlags=HideFlags.DontSave;
        return p;
    }
    public static void DuplicatePerformance()
    {if(!Current||!Current.graph)return;LeaveDraft(()=>Activate(CloneDraft(Current)));}
    public static string UniqueNodeId(ToolGraph graph,string prefix)
    {int i=1;string id;do{id=prefix+"_"+i++;}while(graph.nodes.Any(n=>n.id==id));return id;}
    public static ToolGraphNode AddUnit(ToolPackage p,ToolGraphNode after)
    {
        if(!p||!p.graph)throw new InvalidOperationException("请先新建或打开演出。");
        var graph=p.graph;Undo.RecordObject(graph,"新增演出单元");
        var unit=ScriptableObject.CreateInstance<ToolUnit>();unit.id=UniqueNodeId(graph,"unit");unit.name=unit.id;
        if(AssetDatabase.Contains(p))
        {
            string folder=Path.GetDirectoryName(AssetDatabase.GetAssetPath(p)).Replace('\\','/')+"/Units";
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            AssetDatabase.CreateAsset(unit,AssetDatabase.GenerateUniqueAssetPath(folder+"/"+unit.id+".asset"));
        }
        if(!AssetDatabase.Contains(unit))unit.hideFlags=HideFlags.DontSave;
        var node=new ToolGraphNode{id=unit.id,kind=ToolNodeKind.Unit,unit=unit,position=after!=null?after.position+new Vector2(270,120):new Vector2(60,100+graph.nodes.Count*35)};
        var end=graph.nodes.FirstOrDefault(n=>n.kind==ToolNodeKind.End);
        if(end==null){end=new ToolGraphNode{id=UniqueNodeId(graph,"end"),kind=ToolNodeKind.End,position=node.position+new Vector2(540,0)};graph.nodes.Add(end);}
        var continuation=after?.edges.FirstOrDefault(e=>e.slot=="next");
        node.edges.Add(new ToolGraphEdge{slot="next",target=continuation!=null?continuation.target:end.id});
        if(continuation!=null)continuation.target=node.id;
        graph.nodes.Add(node);if(string.IsNullOrEmpty(graph.entryNode))graph.entryNode=node.id;
        EditorUtility.SetDirty(graph);return node;
    }
    public static ToolGraphNode AddCurrentUnit(ToolGraphNode after=null)
    {
        if(after==null&&Current&&Current.graph)after=Current.graph.nodes.FirstOrDefault(n=>n.unit==Unit);
        var node=AddUnit(Current,after);SelectUnit(node.unit);Backup();return node;
    }
    public static void Save()
    {
        if(IsDraft(Current)){ToolV2PublishWindow.Open(false);return;}
        AssetDatabase.SaveAssets();
        if(Current)EditorGUIUtility.PingObject(Current);
    }
    // Clone first, then write into a new unique directory. Source assets and draft stay intact on failure.
    public static ToolPackage SaveDraft(ToolPackage draft,string parent,string name)
    {
        if(!IsDraft(draft)||!draft.graph)throw new InvalidOperationException("没有可保存的演出草稿。");
        parent=parent.Replace('\\','/').TrimEnd('/');
        string full=Path.GetFullPath(parent),assets=Path.GetFullPath(Application.dataPath);
        if(!(parent=="Assets"||parent.StartsWith("Assets/",StringComparison.Ordinal))||!(full==assets||full.StartsWith(assets+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("保存位置必须在当前工程的 Assets 中。");
        if(string.IsNullOrWhiteSpace(name)||name=="."||name==".."||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0)throw new ArgumentException("演出名称不能包含路径或文件名保留字符。");
        Directory.CreateDirectory(parent);AssetDatabase.Refresh();
        string folder=AssetDatabase.GenerateUniqueAssetPath(parent+"/"+name);
        var copy=CloneDraft(draft);copy.name=name;copy.graph.name=name+"_Graph";
        try
        {
            Directory.CreateDirectory(folder+"/Units");AssetDatabase.Refresh();
            copy.hideFlags=HideFlags.None;copy.graph.hideFlags=HideFlags.None;
            int i=1;foreach(var unit in Units(copy)){unit.hideFlags=HideFlags.None;unit.name="Unit_"+i;AssetDatabase.CreateAsset(unit,folder+"/Units/Unit_"+i+".asset");i++;}
            AssetDatabase.CreateAsset(copy.graph,folder+"/"+name+"_Graph.asset");
            AssetDatabase.CreateAsset(copy,folder+"/"+name+".asset");AssetDatabase.SaveAssets();return copy;
        }
        catch{AssetDatabase.DeleteAsset(folder);throw;}
    }
    public static void AdoptSaved(ToolPackage saved){ClearBackup();Activate(saved);EditorGUIUtility.PingObject(saved);}
    public static void WriteSnapshot(ToolPackage p,string path)
    {
        if(!p||!p.graph)throw new InvalidOperationException("演出草稿已失效，无法创建恢复快照。");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var objects=new List<Object>{p,p.graph};objects.AddRange(Units(p));
        string temp=path+".tmp";
        InternalEditorUtility.SaveToSerializedFileAndForget(objects.ToArray(),temp,true);
        if(File.Exists(path))File.Copy(path,path+".bak",true);
        File.Copy(temp,path,true);File.Delete(temp);
    }
    public static ToolPackage ReadSnapshot(string path)
    {
        var objects=InternalEditorUtility.LoadSerializedFileAndForget(path);
        var result=objects.OfType<ToolPackage>().FirstOrDefault();
        if(!result||!result.graph||!result.library)throw new InvalidDataException("草稿缺少演出树或素材库。");
        foreach(var obj in objects)if(obj&&!AssetDatabase.Contains(obj))obj.hideFlags=HideFlags.DontSave;
        return result;
    }
    public static void Backup()
    {
        if(!initialized||!IsDraft(current)||EditorApplication.isPlaying)return;
        try{WriteSnapshot(current,RecoveryPath);}catch(Exception e){Debug.LogWarning("草稿缓存写入失败，请点击保存："+e.Message);}
    }
    static void ClearBackup(){if(File.Exists(RecoveryPath))File.Delete(RecoveryPath);if(File.Exists(RecoveryPath+".bak"))File.Delete(RecoveryPath+".bak");}
    public static void Toolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        if(GUILayout.Button("新建演出",EditorStyles.toolbarButton,GUILayout.Width(72)))NewPerformance();
        var next=(ToolPackage)EditorGUILayout.ObjectField(Current,typeof(ToolPackage),false,GUILayout.Width(225));
        if(next!=Current&&next)SelectPackage(next);
        GUILayout.Label(IsDraft(Current)?"草稿 · 自动恢复":"已保存演出",EditorStyles.miniLabel,GUILayout.Width(100));
        using(new EditorGUI.DisabledScope(!Current))
        {
            if(GUILayout.Button("保存",EditorStyles.toolbarButton,GUILayout.Width(50)))Save();
            if(GUILayout.Button("导出到游戏",EditorStyles.toolbarButton,GUILayout.Width(90)))ToolV2PublishWindow.Open(true);
            if(GUILayout.Button("复制为新演出",EditorStyles.toolbarButton,GUILayout.Width(98)))DuplicatePerformance();
        }
        GUILayout.FlexibleSpace();GUILayout.EndHorizontal();
    }
}

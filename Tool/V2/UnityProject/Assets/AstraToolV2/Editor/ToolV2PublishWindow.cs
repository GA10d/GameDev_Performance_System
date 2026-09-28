using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceToolV2;

public sealed class ToolV2PublishWindow : EditorWindow
{
    ToolPackage package;
    string performanceName,performanceId,projectFolder,output,error;
    bool export;
    Action afterSave;
    public static void Open(bool export,Action afterSave=null)
    {
        var w=GetWindow<ToolV2PublishWindow>(true,export?"导出到游戏":"保存演出",true);
        w.package=ToolV2Authoring.Current;w.export=export;w.afterSave=afterSave;
        w.performanceName=w.package.name=="未命名演出"?"MyPerformance":w.package.name;
        w.performanceId=Regex.Replace(w.performanceName.ToLowerInvariant(),"[^a-z0-9_-]","_").Trim('_');
        if(string.IsNullOrEmpty(w.performanceId))w.performanceId="my_performance";
        w.projectFolder=ToolV2Authoring.DefaultFolder;
        string key=SettingsKey(w.package);
        if(!ToolV2Authoring.IsDraft(w.package))w.performanceId=EditorPrefs.GetString(key+".Id",w.performanceId);
        w.output=EditorPrefs.GetString(key+".Output",Path.GetFullPath("../Export/Meeting-"+w.performanceId));w.error=null;
        w.minSize=new Vector2(600,340);w.maxSize=new Vector2(900,560);w.Show();
    }
    static string SettingsKey(ToolPackage value)=>"Astra.PerformanceExport."+Application.dataPath+"."+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
    void OnGUI()
    {
        if(!package){EditorGUILayout.HelpBox("演出已关闭，请重新打开保存窗口。",MessageType.Info);return;}
        GUILayout.Space(12);bool draft=ToolV2Authoring.IsDraft(package);
        EditorGUILayout.LabelField(export?"将当前演出打包给游戏":"保存当前演出",EditorStyles.boldLabel);
        if(draft)
        {
            EditorGUILayout.HelpBox("保存时自动生成演出包、演出树和所有单元文件。无需逐个创建资产。",MessageType.Info);
            performanceName=EditorGUILayout.TextField("演出名称",performanceName);
            EditorGUILayout.BeginHorizontal();projectFolder=EditorGUILayout.TextField("保存到工程目录",projectFolder);
            if(GUILayout.Button("选择",GUILayout.Width(52)))
            {
                string folder=EditorUtility.OpenFolderPanel("选择当前工程 Assets 内的目录",Application.dataPath,"");
                if(!string.IsNullOrEmpty(folder))projectFolder=FileUtil.GetProjectRelativePath(folder);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("同名演出会创建新目录，不覆盖已有演出。",EditorStyles.wordWrappedMiniLabel);
        }
        else EditorGUILayout.LabelField("当前演出",AssetDatabase.GetAssetPath(package),EditorStyles.wordWrappedLabel);
        if(export)
        {
            GUILayout.Space(12);performanceId=EditorGUILayout.TextField("演出包 ID",performanceId);
            EditorGUILayout.HelpBox("在游戏的星图事件中填写同一个演出包 ID。仅支持英文、数字、短横线和下划线。",MessageType.None);
            EditorGUILayout.BeginHorizontal();output=EditorGUILayout.TextField("导出目录",output);
            if(GUILayout.Button("选择",GUILayout.Width(52))){string folder=EditorUtility.OpenFolderPanel("选择导出目录",Path.GetFullPath("../Export"),"");if(!string.IsNullOrEmpty(folder))output=folder;}
            EditorGUILayout.EndHorizontal();
        }
        GUILayout.FlexibleSpace();if(!string.IsNullOrEmpty(error))EditorGUILayout.HelpBox(error,MessageType.Error);
        EditorGUILayout.BeginHorizontal();GUILayout.FlexibleSpace();
        if(GUILayout.Button("取消",GUILayout.Width(90),GUILayout.Height(30)))Close();
        if(GUILayout.Button(export?(draft?"保存并导出":"导出") : "保存",GUILayout.Width(135),GUILayout.Height(30)))Publish();
        EditorGUILayout.EndHorizontal();GUILayout.Space(12);
    }
    void Publish()
    {
        try
        {
            if(export)
            {
                if(!Regex.IsMatch(performanceId??"","^[A-Za-z0-9_-]{1,64}$"))throw new ArgumentException("请填写有效的演出包 ID。");
                if(string.IsNullOrWhiteSpace(output))throw new ArgumentException("请选择导出目录。");
                var errors=ToolValidation.Package(package);
                if(package.graph&&!package.graph.nodes.Any(n=>n.kind==ToolNodeKind.End))errors.Add("演出树必须有结束节点。");
                if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors.Take(12)));
                string destination=Path.GetFullPath(output).TrimEnd(Path.DirectorySeparatorChar);
                string assets=Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar);
                if(destination==assets||destination.StartsWith(assets+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||assets.StartsWith(destination+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("导出目录请选择独立文件夹，不能是当前工程、Assets 或其上级目录。");
                if(File.Exists(Path.Combine(destination,"meeting-export.txt"))&&!EditorUtility.DisplayDialog("更新已有导出","该目录已有导出文件，继续将更新其中同名文件。","更新","取消"))return;
            }
            if(ToolV2Authoring.IsDraft(package)){package=ToolV2Authoring.SaveDraft(package,projectFolder,performanceName.Trim());ToolV2Authoring.AdoptSaved(package);}
            else AssetDatabase.SaveAssets();
            if(export)
            {
                ToolV2MeetingExport.Export(package,output,performanceId);
                string key=SettingsKey(package);EditorPrefs.SetString(key+".Id",performanceId);EditorPrefs.SetString(key+".Output",Path.GetFullPath(output));
                EditorUtility.DisplayDialog("导出完成","演出包 ID："+performanceId+"\n\n"+Path.GetFullPath(output)+"\n\n将其中整个 Assets 文件夹合并到游戏 Unity 工程，并重新构建游戏。星图事件填写上面的 ID。","确定");
            }
            var callback=afterSave;Close();callback?.Invoke();
        }
        catch(Exception e){error=e.Message;}
    }
}

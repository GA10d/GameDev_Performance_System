using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Astra.PerformanceToolV2;

// Snapshot the selected draft for play mode; never rewrite the sample scene or package.
[InitializeOnLoad]
public static class ToolV2AuthoringPreview
{
    const string Snapshot="Library/AstraPerformancePreview.asset", Key="Astra.Authoring.Preview";
    [Serializable] sealed class SceneRecord { public string path; public bool loaded,active; }
    [Serializable] sealed class Setup { public SceneRecord[] scenes; }
    static ToolV2AuthoringPreview()
    {
        SceneManager.sceneLoaded+=OnSceneLoaded;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key,false))
            {
                SessionState.SetBool(Key,false);
                var setup=JsonUtility.FromJson<Setup>(SessionState.GetString(Key+".Scenes","{}"));
                if(setup?.scenes!=null&&setup.scenes.Length>0)
                {
                    var records=new System.Collections.Generic.List<SceneSetup>();
                    foreach(var s in setup.scenes)if(!string.IsNullOrEmpty(s.path))records.Add(new SceneSetup{path=s.path,isLoaded=s.loaded,isActive=s.active});
                    if(records.Count>0)EditorSceneManager.RestoreSceneManagerSetup(records.ToArray());
                    else EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Single);
                }
            }
        };
    }
    public static void Play(ToolPackage package)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        ToolV2Authoring.WriteSnapshot(package,Snapshot);
        var scenes=EditorSceneManager.GetSceneManagerSetup();var records=new SceneRecord[scenes.Length];
        for(int i=0;i<scenes.Length;i++)records[i]=new SceneRecord{path=scenes[i].path,loaded=scenes[i].isLoaded,active=scenes[i].isActive};
        SessionState.SetString(Key+".Scenes",JsonUtility.ToJson(new Setup{scenes=records}));
        SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene(ToolV2Build.Root+"/Scenes/ToolPreview.unity");
        EditorApplication.isPlaying=true;
    }
    static void OnSceneLoaded(Scene scene,LoadSceneMode mode)
    {
        if(!Application.isPlaying||!SessionState.GetBool(Key,false)||!File.Exists(Snapshot))return;
        foreach(var workbench in UnityEngine.Object.FindObjectsOfType<ToolWorkbench>())workbench.enabled=false;
        var player=UnityEngine.Object.FindObjectOfType<ToolPlayer>();
        if(!player)return;
        player.package=ToolV2Authoring.ReadSnapshot(Snapshot);player.autoPlay=true;player.showGUI=true;
        RenderSettings.ambientLight=new Color(.47f,.51f,.47f);
    }
}

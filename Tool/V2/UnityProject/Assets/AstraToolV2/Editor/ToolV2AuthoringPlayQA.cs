using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Astra.PerformanceToolV2;

[InitializeOnLoad]
public static class ToolV2AuthoringPlayQA
{
    const string Key="Astra.Authoring.PlayQA";
    static double deadline;
    static int phase;
    static ToolV2AuthoringPlayQA()
    {
        EditorApplication.update+=Update;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){deadline=EditorApplication.timeSinceStartup+45;phase=1;}
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall+=()=>
                {
                    if(!SessionState.GetBool(Key+".Completed",false)){Fail("Preview stopped without reaching End");return;}
                    if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/AuthoringQA/OriginalScene.unity"){Fail("Original editor scene was not restored");return;}
                    File.AppendAllText("../QA/authoring-preview.txt","PASS Original editor scene restored\nRESULT PASS\n");
                    SessionState.SetBool(Key,false);Debug.Log("ASTRA_AUTHORING_PREVIEW_QA_SUCCEEDED");EditorApplication.Exit(0);
                };
            }
        };
    }
    public static void Run()
    {
        if(!Environment.GetCommandLineArgs().Contains("-authoringQA"))throw new Exception("Use isolated QA project");
        var recovered=ToolV2Authoring.Current;
        if(!ToolV2Authoring.IsDraft(recovered)||recovered.graph.nodes.Where(n=>n.unit).First().unit.dialogue[0].text!="独立副本")throw new Exception("Unsaved session did not recover after restart");
        Directory.CreateDirectory("../QA");File.WriteAllText("../QA/authoring-preview.txt","PASS Draft restored in a fresh Unity process\n");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/AuthoringQA/OriginalScene.unity");
        SessionState.SetBool(Key,true);SessionState.SetBool(Key+".Completed",false);
        deadline=EditorApplication.timeSinceStartup+60;ToolV2AuthoringPreview.Play(recovered);
    }
    static void Update()
    {
        if(!SessionState.GetBool(Key,false))return;
        if(deadline>0&&EditorApplication.timeSinceStartup>deadline){Fail("Play mode timed out");return;}
        if(!EditorApplication.isPlaying||phase==0)return;
        var player=UnityEngine.Object.FindObjectOfType<ToolPlayer>();
        if(!player||string.IsNullOrEmpty(player.CurrentNodeId))return;
        if(phase==1)
        {
            if(player.package.graph.nodes.Where(n=>n.unit).First().unit.dialogue[0].text!="独立副本"){Fail("Preview played default sample instead of current draft");return;}
            var workbench=UnityEngine.Object.FindObjectOfType<ToolWorkbench>();
            if(workbench&&workbench.enabled){Fail("Character workbench interferes with performance preview");return;}
            player.externalClock=true;phase=2;
            File.AppendAllText("../QA/authoring-preview.txt","PASS Actual player started the selected unsaved draft\nPASS Character workbench disabled without editing the source scene\n");
        }
        if(phase!=2)return;
        if(player.Failure!=null){Fail(player.Failure);return;}
        if(player.IsChoosing)player.Choose("yes");else player.Tick(.5f);
        if(player.IsFinished)
        {
            phase=3;SessionState.SetBool(Key+".Completed",true);
            File.AppendAllText("../QA/authoring-preview.txt","PASS Draft timeline, choice and End play to completion\n");
            EditorApplication.isPlaying=false;
        }
    }
    static void Fail(string reason){SessionState.SetBool(Key,false);File.AppendAllText("../QA/authoring-preview.txt","FAIL "+reason+"\n");Debug.LogError(reason);EditorApplication.Exit(1);}
}

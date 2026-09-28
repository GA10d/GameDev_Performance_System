using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Astra.PerformanceToolV2;

[InitializeOnLoad]
public static class ToolV2TemplatePlayQA
{
    const string Key="Astra.Template.PlayQA";
    static readonly ToolPerformanceTemplate[] Cases={ToolPerformanceTemplate.SingleCall,ToolPerformanceTemplate.Conversation,ToolPerformanceTemplate.Question,ToolPerformanceTemplate.Question,ToolPerformanceTemplate.Question,ToolPerformanceTemplate.Broadcast};
    static readonly string[] Endings={"end","end","accepted","declined","unanswered","end"};
    static int index;
    static bool ready,captured,replyCaptured,stopping;
    static double deadline,nextTick;
    static ToolV2TemplatePlayQA()
    {
        EditorApplication.update+=Update;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){deadline=EditorApplication.timeSinceStartup+100;ready=true;}
            if(state==PlayModeStateChange.EnteredEditMode)EditorApplication.delayCall+=()=>
            {
                if(!SessionState.GetBool(Key+".Done",false)){Fail("Playback did not finish every case");return;}
                SessionState.SetBool(Key,false);File.AppendAllText("../QA/template-playback.txt","RESULT PASS cases=6\n");Debug.Log("ASTRA_TEMPLATE_PLAY_QA_SUCCEEDED");EditorApplication.Exit(0);
            };
        };
    }
    public static void Run()
    {
        if(!Environment.GetCommandLineArgs().Contains("-authoringQA"))throw new Exception("Run in isolated QA project");
        Directory.CreateDirectory("../QA/TemplateFrames");File.WriteAllText("../QA/template-playback.txt","");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/TemplateQA/OriginalScene.unity");
        var library=AssetDatabase.LoadAssetAtPath<ToolLibrary>(ToolV2Build.Root+"/Samples/Library.asset");
        SessionState.SetBool(Key,true);SessionState.SetBool(Key+".Done",false);deadline=EditorApplication.timeSinceStartup+120;
        ToolV2AuthoringPreview.Play(ToolV2Templates.Create(library,ToolV2Templates.Defaults(Cases[0],library)));
    }
    static void Update()
    {
        if(!SessionState.GetBool(Key,false))return;
        if(deadline>0&&EditorApplication.timeSinceStartup>deadline){Fail("Template playback timed out");return;}
        if(!ready||!EditorApplication.isPlaying||stopping||EditorApplication.timeSinceStartup<nextTick)return;
        nextTick=EditorApplication.timeSinceStartup+.04;
        var player=UnityEngine.Object.FindObjectOfType<ToolPlayer>();if(!player||string.IsNullOrEmpty(player.CurrentNodeId))return;
        player.externalClock=true;
        if(player.Failure!=null){Fail(player.Failure);return;}
        if(!captured&&player.CurrentTime>=1.5f){Capture(player,Cases[index]+"_"+index);captured=true;}
        if(index==1&&!replyCaptured&&player.CurrentNodeId=="reply"&&player.CurrentTime>=1.5f){Capture(player,"Conversation_second_speaker");replyCaptured=true;}
        if(player.IsChoosing&&index!=4)player.Choose(index==2?"accept":"decline");else player.Tick(.25f);
        if(!player.IsFinished)return;
        if(player.CurrentNodeId!=Endings[index]){Fail("Wrong ending: "+player.CurrentNodeId);return;}
        File.AppendAllText("../QA/template-playback.txt","PASS "+Cases[index]+" -> "+Endings[index]+"\n");
        if(++index==Cases.Length){stopping=true;SessionState.SetBool(Key+".Done",true);EditorApplication.isPlaying=false;return;}
        var library=player.package.library;player.package=ToolV2Templates.Create(library,ToolV2Templates.Defaults(Cases[index],library));player.Begin();captured=false;
    }
    static void Capture(ToolPlayer player,string name)
    {
        var texture=new RenderTexture(960,540,24);texture.Create();var previous=player.lens.targetTexture;var active=RenderTexture.active;
        var pixels=new Texture2D(960,540,TextureFormat.RGB24,false);
        try {player.lens.targetTexture=texture;player.lens.Render();RenderTexture.active=texture;pixels.ReadPixels(new Rect(0,0,960,540),0,0);pixels.Apply();File.WriteAllBytes("../QA/TemplateFrames/"+name+".png",pixels.EncodeToPNG());}
        finally {player.lens.targetTexture=previous;RenderTexture.active=active;texture.Release();UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(pixels);}
    }
    static void Fail(string message){SessionState.SetBool(Key,false);File.AppendAllText("../QA/template-playback.txt","FAIL "+message+"\n");Debug.LogError(message);EditorApplication.Exit(1);}
}

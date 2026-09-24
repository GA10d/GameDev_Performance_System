using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using Astra.Performance;

public static class PerformanceBuild
{
    const string Root="Assets/Performance";
    static Dictionary<string,Material> materials;
    [MenuItem("Astra Performance/Rebuild scene")]
    public static void CreateScene()
    {
        Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Scenes");Directory.CreateDirectory(Root+"/Data");AssetDatabase.Refresh();
        foreach(var path in Directory.GetFiles(Root+"/Textures","*.png"))
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.sRGBTexture=!path.Contains("Roughness")&&!path.Contains("NormalGL");
            if(path.Contains("NormalGL"))ti.textureType=TextureImporterType.NormalMap;
            ti.SaveAndReimport();
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        PlayerSettings.companyName="Project Astra";PlayerSettings.productName="ASTRA - Performance Study";
        PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);
        QualitySettings.vSyncCount=1;QualitySettings.antiAliasing=0;QualitySettings.pixelLightCount=4;
        QualitySettings.shadowDistance=8;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.30f,.34f,.27f);
        RenderSettings.ambientEquatorColor=new Color(.16f,.18f,.14f);RenderSettings.ambientGroundColor=new Color(.10f,.09f,.075f);RenderSettings.skybox=null;RenderSettings.fog=false;
        materials=new Dictionary<string,Material>();
        var cabinPath=Root+"/Models/Astra_Cabin.fbx";
        Import(cabinPath,false);
        var cabin=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(cabinPath));cabin.name="Astra source cabin (read-only source copy)";
        foreach(var r in cabin.GetComponentsInChildren<Renderer>())
            r.sharedMaterials=r.sharedMaterials.Select(m=>CabinMaterial(m?m.name:"HullPaint")).ToArray();
        var camera=new GameObject("Cabin view").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,1.28f,0);camera.fieldOfView=75;camera.nearClipPlane=.03f;camera.farClipPlane=12;camera.cullingMask=~(1<<8);
        camera.backgroundColor=new Color(.015f,.02f,.017f);camera.clearFlags=CameraClearFlags.SolidColor;camera.gameObject.AddComponent<AudioListener>();
        var cg=camera.gameObject.AddComponent<SignalRenderer>();cg.gradeShader=Shader.Find("Hidden/Astra/Performance/Grade");
        LightAt("Cabin warm",new Vector3(.0f,2.05f,.2f),new Color(1,.87f,.64f),1.8f,4,~(1<<8));
        LightAt("Cabin bounce",new Vector3(-.55f,1.2f,0),new Color(.61f,.73f,.66f),.55f,3,~(1<<8));
        var controls=new GameObject("Performance runtime");var director=controls.AddComponent<PerformanceDirector>();
        director.cabinCamera=camera;director.cabinGrade=cg;director.sequence=MakeSequence();
        director.screenMaterial=CabinMaterial("Screen");
        var remote=new GameObject("Remote archive camera").AddComponent<Camera>();remote.transform.position=new Vector3(30,1.42f,-2.48f);remote.transform.LookAt(new Vector3(30,1.36f,0));
        remote.fieldOfView=30;remote.aspect=2;remote.cullingMask=1<<8;remote.nearClipPlane=.03f;remote.farClipPlane=12;remote.clearFlags=CameraClearFlags.SolidColor;remote.backgroundColor=new Color(.08f,.115f,.09f);remote.depth=-2;
        var rg=remote.gameObject.AddComponent<SignalRenderer>();rg.gradeShader=cg.gradeShader;rg.signal=true;
        director.remoteCamera=remote;director.remoteGrade=rg;
        director.remoteKey=LightAt("Remote tungsten",new Vector3(29.15f,2.2f,-1.1f),new Color(1,.82f,.61f),1.75f,5,1<<8);
        director.remoteFill=LightAt("Remote screen fill",new Vector3(30.7f,1.45f,-1.8f),new Color(.52f,.70f,.64f),1.0f,5,1<<8);
        LightAt("Remote edge",new Vector3(30.7f,2, .45f),new Color(.74f,.69f,.44f),1.4f,3,1<<8);
        var ids=new[]{"Lin047","He112","Yu203"};var actors=new List<ActorPerformance>();
        for(int i=0;i<3;i++)
        {
            string path=Root+"/Models/"+ids[i]+".fbx";Import(path,true);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));go.name=ids[i];go.transform.position=new Vector3(30,0,0);go.transform.rotation=Quaternion.Euler(0,180,0);
            foreach(var t in go.GetComponentsInChildren<Transform>())t.gameObject.layer=8;
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>ActorMaterial(m,i)).ToArray();
            var a=go.AddComponent<ActorPerformance>();a.actorId=(ActorId)i;a.phaseSeed=i+.7f;actors.Add(a);
            a.Bind();
            Debug.Log("RIG "+ids[i]+" head="+a.Head.position+" local="+a.Head.localRotation.eulerAngles+" up="+a.Head.up+" forward="+a.Head.forward);
        }
        director.actors=actors.ToArray();
        RemoteSet();
        controls.AddComponent<PerformanceView>().director=director;
        controls.AddComponent<PerformanceCapture>().director=director;
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"/Scenes/Performance.unity");AssetDatabase.SaveAssets();
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"/Scenes/Performance.unity",true)};
    }
    static void Import(string path,bool rig)
    {
        var i=(ModelImporter)AssetImporter.GetAtPath(path);if(i==null)throw new Exception("Missing "+path);
        i.importCameras=false;i.importLights=false;i.importAnimation=false;i.preserveHierarchy=true;i.isReadable=true;
        if(rig)i.animationType=ModelImporterAnimationType.Generic;
        i.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;i.SaveAndReimport();
    }
    static Material SaveMat(string name,Material m)
    {
        m.name=name;string path=Root+"/Materials/"+name+".mat";var old=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(old){EditorUtility.CopySerialized(m,old);UnityEngine.Object.DestroyImmediate(m);m=old;}
        else AssetDatabase.CreateAsset(m,path);
        materials[name]=m;return m;
    }
    static Material CabinMaterial(string n)
    {
        if(materials.TryGetValue(n,out var existing))return existing;
        Color c=n.Contains("Brass")?new Color(.40f,.29f,.14f):n.Contains("Red")?new Color(.31f,.065f,.042f):n.Contains("Rubber")?new Color(.025f,.03f,.023f):n.Contains("Dark")?new Color(.10f,.12f,.10f):n.Contains("Paper")?new Color(.64f,.57f,.39f):n.Contains("Canvas")?new Color(.24f,.205f,.13f):new Color(.29f,.33f,.30f);
        Material m;
        if(n.Contains("Lamp")||n=="Screen")
        {m=new Material(Shader.Find("Unlit/Texture"));if(n!="Screen"){m=new Material(Shader.Find("Unlit/Color"));m.color=n.Contains("Green")?new Color(.40f,.66f,.28f):n.Contains("Red")?new Color(.7f,.15f,.06f):new Color(.83f,.66f,.32f);}}
        else
        {
            m=new Material(Shader.Find("Astra/WornMetal"));m.color=c;m.SetFloat("_Glossiness",.18f);m.SetFloat("_Metallic",.15f);m.SetFloat("_Wear",.16f);
            if(n=="HullPaint")
            {
                m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/NavalPaint_Albedo.png"));m.SetFloat("_TextureStrength",.88f);m.SetFloat("_TextureScale",1.0f);
                m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/PaintedMetal012_2K-PNG_NormalGL.png"));
                m.SetTexture("_RoughnessMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/PaintedMetal012_2K-PNG_Roughness.png"));m.SetFloat("_DetailStrength",.78f);m.SetFloat("_BumpScale",.28f);
            }
        }
        return SaveMat(n,m);
    }
    static Material ActorMaterial(Material original,int index)
    {
        string baseName=original?original.name:"Suit";string key="Actor"+index+"_"+baseName;
        if(materials.TryGetValue(key,out var m))return m;
        m=new Material(Shader.Find("Astra/Performance/Character"));m.color=original?original.color:Color.gray;
        if(baseName=="Suit")m.color=new[]{new Color(.57f,.55f,.43f),new Color(.32f,.39f,.30f),new Color(.37f,.19f,.15f)}[index];
        if(baseName=="SuitDark")m.color=new[]{new Color(.27f,.29f,.23f),new Color(.22f,.26f,.20f),new Color(.24f,.24f,.19f)}[index];
        if(baseName=="Skin")m.color=new[]{new Color(.61f,.48f,.36f),new Color(.43f,.32f,.23f),new Color(.66f,.53f,.41f)}[index];
        m.SetFloat("_Bands",5);return SaveMat(key,m);
    }
    static Light LightAt(string n,Vector3 p,Color c,float intensity,float range,int mask)
    {var l=new GameObject(n).AddComponent<Light>();l.transform.position=p;l.type=LightType.Point;l.color=c;l.intensity=intensity;l.range=range;l.cullingMask=mask;l.shadows=LightShadows.None;return l;}
    static void Box(string n,Vector3 p,Vector3 size,Material material)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=n;go.layer=8;go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());}
    static void RemoteSet()
    {
        for(int i=-2;i<=2;i++)
        {
            Box("Repaired rear wall",new Vector3(30+i*.61f,1.2f,.78f),new Vector3(.58f,2.4f,.07f),CabinMaterial("HullPaint"));
            for(int j=0;j<4;j++)Box("Panel fastener",new Vector3(30+i*.61f+.25f,.25f+j*.63f,.727f),new Vector3(.034f,.034f,.026f),CabinMaterial("Brass"));
        }
        Box("Floor",new Vector3(30,-.06f,.0f),new Vector3(4,.1f,3),CabinMaterial("DarkSteel"));
        Box("Pipe left",new Vector3(29.22f,1.15f,.60f),new Vector3(.058f,2.4f,.07f),CabinMaterial("Brass"));
        Box("Pipe right",new Vector3(30.77f,1.15f,.61f),new Vector3(.045f,2.4f,.07f),CabinMaterial("DarkSteel"));
        Box("Electrical box",new Vector3(30.65f,1.28f,.56f),new Vector3(.3f,.49f,.16f),CabinMaterial("DarkSteel"));
        Box("Status lamp",new Vector3(30.65f,1.37f,.463f),new Vector3(.19f,.055f,.018f),CabinMaterial("LampGreen"));
        for(int j=0;j<5;j++)Box("Vent",new Vector3(29.45f,.9f+j*.075f,.69f),new Vector3(.27f,.021f,.03f),CabinMaterial("DarkSteel"));
        Box("Warning label",new Vector3(29.5f,1.70f,.72f),new Vector3(.34f,.15f,.01f),CabinMaterial("Paper"));
        for(int j=0;j<4;j++)Box("Printed label",new Vector3(29.5f,1.665f+j*.023f,.711f),new Vector3(.27f,.008f,.008f),CabinMaterial("DarkSteel"));
    }
    static PerformanceSequence MakeSequence()
    {
        var p=Root+"/Data/SignalArchive.asset";var s=AssetDatabase.LoadAssetAtPath<PerformanceSequence>(p);
        if(s)return s; // Writer edits survive rebuilding scene and executable.
        s=ScriptableObject.CreateInstance<PerformanceSequence>();s.entryId="lin01";
        s.beats=new[]{
            Beat("lin01",ActorId.Lin,"如果这段影像到了，你的风扇应该还在响。",PoseId.Tired,GazeId.Panel,GestureId.None,"lin02"),
            Beat("lin02",ActorId.Lin,"我的停过一次。我用配给券垫住了轴承。",PoseId.Rest,GazeId.Lens,GestureId.Explain,"lin03"),
            Beat("lin03",ActorId.Lin,"系统把它记作：消耗品自行修复。",PoseId.Tired,GazeId.Down,GestureId.None,"lin04",1.8f,.7f,true),
            Beat("lin04",ActorId.Lin,"如果你也听见那种声音，先别关掉它。听听这份旧记录。",PoseId.Guarded,GazeId.Lens,GestureId.Chest,"he01"),
            Beat("he01",ActorId.He,"编号写反了。先拧蓝线下面那颗螺钉。",PoseId.Assertive,GazeId.Panel,GestureId.Point,"he02"),
            Beat("he02",ActorId.He,"别笑。我也花了七年，才发现说明书装倒了。",PoseId.Rest,GazeId.Lens,GestureId.Nod,"he03"),
            Beat("he03",ActorId.He,"有个人传给我另一个名字。不是零件的名字。",PoseId.Tired,GazeId.Away,GestureId.None,"yu01",1.1f,.35f),
            Beat("yu01",ActorId.Yu,"别告诉它，你还记得自己的名字。",PoseId.Guarded,GazeId.Away,GestureId.No,"yu02",1.2f,.65f),
            Beat("yu02",ActorId.Yu,"我把我的写在了舱壁背面。那里不在检查清单上。",PoseId.Guarded,GazeId.Lens,GestureId.Chest,"reply",.8f,.3f),
            Beat("reply",ActorId.Yu,"轮到你了。你想留下什么？",PoseId.Rest,GazeId.Lens,GestureId.None,"",.6f,.7f),
            Beat("end_name",ActorId.Lin,"〔影像结束〕回信已排队：一个名字，和一段风扇的声音。",PoseId.Rest,GazeId.Lens,GestureId.None,"",1.0f,.35f),
            Beat("end_log",ActorId.He,"〔影像结束〕回信已排队：维修记录。姓名字段留空。",PoseId.Rest,GazeId.Panel,GestureId.None,"",1.0f,.2f)
        };
        s.beats.First(b=>b.id=="reply").choices=new[]{new ReplyChoice{text="留下我的名字",next="end_name",payload="我的名字还在。风扇也还在。"},new ReplyChoice{text="只发送维修记录",next="end_log",payload="047 型轴承修复方法；姓名字段留空。"}};
        s.beats.First(b=>b.id=="lin03").alarm=true;
        foreach(var end in s.beats.Where(b=>b.id.StartsWith("end_")))end.systemLine=true;
        AssetDatabase.CreateAsset(s,p);return s;
    }
    static PerformanceBeat Beat(string id,ActorId actor,string text,PoseId pose,GazeId gaze,GestureId gesture,string next,float hold=.7f,float close=0,bool still=false)
    {return new PerformanceBeat{id=id,actor=actor,text=text,pose=pose,gaze=gaze,gesture=gesture,next=next,hold=hold,closeUp=close,stillness=still};}
    [MenuItem("Astra Performance/Build Windows + verify")]
    public static void Build()
    {
        CreateScene();PerformanceVerification.Run();
        var root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));Directory.CreateDirectory(Path.Combine(root,"Build"));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Root+"/Scenes/Performance.unity"},locationPathName=Path.Combine(root,"Build/ASTRA Performance.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
        Debug.Log("PERFORMANCE_BUILD_OK "+report.summary.totalSize);
    }
}

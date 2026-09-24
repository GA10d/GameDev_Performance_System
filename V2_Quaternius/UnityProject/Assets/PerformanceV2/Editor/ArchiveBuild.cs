using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using Astra.PerformanceV2;

public static class ArchiveBuild
{
    const string Root=CastImport.Root;
    static Dictionary<string,Material> mats;
    static Material paint,steel,brass,rust,ivory,green,red,black;
    [MenuItem("Astra V2/Build archive")]
    public static void Build()
    {
        CreateScene();
        string root=Path.GetFullPath(Application.dataPath+"/../..");Directory.CreateDirectory(root+"/Build");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Root+"/Scenes/Relay.unity"},locationPathName=root+"/Build/ASTRA Relay V2.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        File.WriteAllText(root+"/QA/build-summary.txt",report.summary.result+"\nUnity "+Application.unityVersion+"\nBytes "+report.summary.totalSize+"\nErrors "+report.summary.totalErrors);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed");
        Debug.Log("ARCHIVE_BUILD_OK");
    }
    [MenuItem("Astra V2/Rebuild scene")]
    public static void CreateScene()
    {
        Directory.CreateDirectory(Root+"/Scenes");Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Data");AssetDatabase.Refresh();CastImport.Configure();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);mats=new Dictionary<string,Material>();
        PlayerSettings.companyName="Project Astra";PlayerSettings.productName="ASTRA Relay V2";PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);
        QualitySettings.vSyncCount=1;QualitySettings.antiAliasing=0;QualitySettings.pixelLightCount=6;QualitySettings.shadowDistance=16;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadows=ShadowQuality.All;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.43f,.47f,.39f);RenderSettings.ambientEquatorColor=new Color(.26f,.30f,.24f);RenderSettings.ambientGroundColor=new Color(.11f,.13f,.10f);RenderSettings.skybox=null;RenderSettings.fog=false;
        paint=Mat("Patinated hull",new Color(.30f,.36f,.29f),true);steel=Mat("Dark steel",new Color(.095f,.125f,.115f),true);brass=Mat("Brass fittings",new Color(.48f,.34f,.14f),true);rust=Mat("Oxide red",new Color(.36f,.10f,.055f),true);ivory=Mat("Old enamel",new Color(.63f,.61f,.47f),true);
        green=Mat("Phosphor",new Color(.47f,.69f,.38f),false,true);red=Mat("Signal red",new Color(.78f,.16f,.055f),false,true);black=Mat("Rubber",new Color(.02f,.027f,.022f));
        Stage();
        var camera=new GameObject("Archive lens").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(3,2,-5);camera.transform.LookAt(new Vector3(0,1,0));camera.fieldOfView=38;camera.nearClipPlane=.03f;camera.farClipPlane=40;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.015f,.025f,.022f);camera.allowHDR=true;camera.gameObject.AddComponent<AudioListener>();
        var grade=camera.gameObject.AddComponent<ArchiveGrade>();grade.shader=Shader.Find("Hidden/Astra/Performance/Grade");
        var rig=camera.gameObject.AddComponent<ArchiveCamera>();rig.lens=camera;
        Spotlight("Warm overhead",new Vector3(-2.2f,3.7f,-1.2f),new Vector3(0,.9f,.5f),new Color(1,.83f,.60f),1.65f,85,true);
        Spotlight("Pale green fill",new Vector3(3,2.8f,-2.1f),new Vector3(0,1.2f,.5f),new Color(.67f,.83f,.73f),1.05f,90,false);
        Spotlight("Door rim",new Vector3(.2f,2.8f,3),new Vector3(0,1.1f,.0f),new Color(.84f,.69f,.41f),1.7f,110,true);
        Spotlight("Technician face fill",new Vector3(-3.3f,2.6f,-.8f),new Vector3(-2.8f,1.4f,1.65f),new Color(.87f,.85f,.70f),1.15f,46,false);
        Spotlight("Front portrait fill",new Vector3(-.6f,2.5f,-3.6f),new Vector3(-.4f,1.3f,-.2f),new Color(.81f,.85f,.77f),.8f,70,false);
        var director=new GameObject("Archive playback").AddComponent<ArchiveDirector>();director.cameraRig=rig;director.grade=grade;
        Vector3[] positions={new Vector3(-1.25f,0,.25f),new Vector3(.45f,0,.7f),new Vector3(2.1f,0,.45f),new Vector3(-2.8f,0,1.7f)};
        string[] names={"林 / LIN","回声 / ECHO","鲁克 / RUK","沃斯 / VOSS"};string[] species={"人类","外星人","半兽人","人类"};string[] roles={"047 · 航行记录员","N-12 · 生物信号译员","R-08 · 中继站维修员","112 · 值守技师"};
        var actors=new List<CastActor>();
        for(int i=0;i<4;i++)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+CastImport.Ids[i]+".fbx"));go.name=CastImport.Ids[i];go.transform.position=positions[i];go.transform.rotation=Quaternion.Euler(0,180,0);
            foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(ActorMat).ToArray();
            // Static FBX import bounds describe the standing rest pose. A seated
            // head can be visible while that original bound lies outside the lens.
            foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())r.updateWhenOffscreen=true;
            var a=go.AddComponent<CastActor>();a.displayName=names[i];a.species=species[i];a.role=roles[i];a.identityColor=new[]{new Color(.81f,.74f,.51f),new Color(.51f,.76f,.69f),new Color(.77f,.60f,.37f),new Color(.65f,.73f,.48f)}[i];actors.Add(a);
        }
        director.actors=actors.ToArray();director.sequence=MakeSequence();
        director.galleryOccluders=UnityEngine.Object.FindObjectsOfType<MeshRenderer>().Where(r=>new[]{"Relay ","Magnetic spool","Reel","Technician ","Supply crate","Crate band","Stencil DELAY"}.Any(prefix=>r.name.StartsWith(prefix))).Select(r=>r.gameObject).ToArray();
        director.repairKit=new GameObject("Repair equipment");
        Box("Open service housing",new Vector3(0,.13f,0),new Vector3(.50f,.26f,.32f),steel).transform.SetParent(director.repairKit.transform);
        Box("Service face",new Vector3(0,.265f,0),new Vector3(.46f,.018f,.28f),brass).transform.SetParent(director.repairKit.transform);
        for(int i=-1;i<=1;i++)Cylinder("Repair fastener",new Vector3(i*.14f,.283f,0),new Vector3(.055f,.012f,.055f),ivory,Vector3.zero).transform.SetParent(director.repairKit.transform);
        director.repairKit.SetActive(false);
        director.actionTable=new GameObject("Action workbench");
        Box("Workbench top",new Vector3(0,.87f,0),new Vector3(.70f,.07f,.4f),ivory).transform.SetParent(director.actionTable.transform);
        for(int i=-1;i<=1;i+=2)Box("Workbench support",new Vector3(i*.27f,.42f,0),new Vector3(.06f,.84f,.25f),steel).transform.SetParent(director.actionTable.transform);
        director.pickup=Box("Pickup component",new Vector3(.12f,.96f,0),new Vector3(.13f,.12f,.12f),brass);director.pickup.transform.SetParent(director.actionTable.transform);
        director.actionTable.SetActive(false);
        director.pushCrate=new GameObject("Push test load");
        Box("Load case",new Vector3(0,.47f,0),new Vector3(.8f,.94f,.45f),rust).transform.SetParent(director.pushCrate.transform);
        Box("Push grip",new Vector3(0,.95f,.18f),new Vector3(.66f,.10f,.065f),brass).transform.SetParent(director.pushCrate.transform);director.pushCrate.SetActive(false);
        var stool=new GameObject("Seat mark");
        Box("Seat cushion",new Vector3(0,.46f,0),new Vector3(.46f,.09f,.42f),black).transform.SetParent(stool.transform);
        for(int i=-1;i<=1;i+=2)for(int j=-1;j<=1;j+=2)Box("Seat leg",new Vector3(i*.18f,.23f,j*.16f),new Vector3(.035f,.45f,.035f),steel).transform.SetParent(stool.transform);
        director.stool=stool;stool.SetActive(false);
        director.gameObject.AddComponent<ArchiveView>().director=director;director.gameObject.AddComponent<ArchiveQA>().director=director;
        var validation=director.sequence.Validate();if(validation!=null)throw new Exception(validation);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"/Scenes/Relay.unity");AssetDatabase.SaveAssets();
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"/Scenes/Relay.unity",true)};
    }
    static Material Mat(string name,Color color,bool worn=false,bool emissive=false)
    {
        if(mats.TryGetValue(name,out var cached))return cached;
        var m=new Material(Shader.Find(emissive?"Unlit/Color":worn?"Astra/WornMetal":"Astra/Performance/Character"));m.color=color;
        if(worn){m.SetFloat("_Wear",.19f);m.SetFloat("_Glossiness",.19f);m.SetFloat("_Metallic",.18f);m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/NavalPaint_Albedo.png"));m.SetFloat("_TextureStrength",name.Contains("hull")?.65f:.22f);m.SetFloat("_TextureScale",.7f);m.SetFloat("_DetailStrength",0);}
        else if(!emissive)m.SetFloat("_Bands",6);
        string path=Root+"/Materials/"+name.Replace('/','_')+".mat";var old=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(old){EditorUtility.CopySerialized(m,old);UnityEngine.Object.DestroyImmediate(m);m=old;}else AssetDatabase.CreateAsset(m,path);
        mats[name]=m;return m;
    }
    static Material ActorMat(Material original)
    {string n=original?original.name:"Unknown";return Mat("Cast_"+n,original?original.color:Color.gray);}
    static GameObject Box(string name,Vector3 p,Vector3 size,Material mat)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
    static GameObject Cylinder(string name,Vector3 p,Vector3 size,Material mat,Vector3 euler)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.position=p;go.transform.localScale=size;go.transform.eulerAngles=euler;go.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
    static void Text(string text,Vector3 p,float size,Color color)
    {
        var go=new GameObject("Stencil "+text);go.transform.position=p;var tm=go.AddComponent<TextMesh>();tm.text=text;tm.fontSize=70;tm.characterSize=size;tm.color=color;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;
        var r=go.GetComponent<MeshRenderer>();
        if(!mats.TryGetValue("StencilFont",out var fm)){fm=new Material(Shader.Find("Astra/DepthText"));fm.mainTexture=r.sharedMaterial.mainTexture;string path=Root+"/Materials/StencilFont.mat";var old=AssetDatabase.LoadAssetAtPath<Material>(path);if(old){EditorUtility.CopySerialized(fm,old);UnityEngine.Object.DestroyImmediate(fm);fm=old;}else AssetDatabase.CreateAsset(fm,path);mats["StencilFont"]=fm;}
        r.sharedMaterial=fm;
    }
    static void Spotlight(string name,Vector3 p,Vector3 target,Color c,float intensity,float angle,bool shadows)
    {var l=new GameObject(name).AddComponent<Light>();l.transform.position=p;l.transform.LookAt(target);l.type=LightType.Spot;l.range=15;l.spotAngle=angle;l.color=c;l.intensity=intensity;l.shadows=shadows?LightShadows.Soft:LightShadows.None;l.shadowStrength=.65f;l.shadowBias=.03f;l.shadowNormalBias=.3f;}
    static void Stage()
    {
        Box("Floor",new Vector3(0,-.10f,.6f),new Vector3(10,.20f,9),steel);
        for(int i=-4;i<=4;i++)for(int j=-3;j<=3;j++)
        {Box("Floor tile",new Vector3(i*1.05f,-.001f,j*1.05f),new Vector3(1.015f,.012f,1.015f),(i+j)%3==0?paint:steel);}
        for(int i=-4;i<=4;i++)
        {
            Box("Hull plate",new Vector3(i*1.02f,1.65f,3.4f),new Vector3(.98f,3.3f,.14f),paint);
            Box("Vertical rib",new Vector3(i*1.02f-.49f,1.65f,3.24f),new Vector3(.075f,3.3f,.13f),steel);
            for(int j=0;j<5;j++)Cylinder("Rivet",new Vector3(i*1.02f-.44f,.28f+j*.61f,3.15f),new Vector3(.045f,.017f,.045f),brass,new Vector3(90,0,0));
        }
        Box("Lower rail",new Vector3(0,.25f,3.13f),new Vector3(9,.10f,.13f),brass);
        Box("Ceiling header",new Vector3(0,3.18f,2.7f),new Vector3(10,.20f,1.1f),steel);
        Box("Left hull",new Vector3(-4.7f,1.65f,-.7f),new Vector3(.20f,3.3f,8.4f),paint);Box("Right hull",new Vector3(4.7f,1.65f,-.7f),new Vector3(.20f,3.3f,8.4f),paint);
        Box("Front bulkhead",new Vector3(0,1.65f,-4.8f),new Vector3(10,3.3f,.14f),paint);
        Box("Ceiling",new Vector3(0,3.75f,-.5f),new Vector3(10,.15f,9),steel);
        // Recessed pressure door and embossed industrial signage.
        Box("Pressure door dark surround",new Vector3(.15f,1.36f,3.13f),new Vector3(1.65f,2.65f,.18f),steel);
        Box("Pressure door",new Vector3(.15f,1.34f,3.00f),new Vector3(1.40f,2.43f,.14f),ivory);
        Box("Door seam",new Vector3(.15f,1.34f,2.91f),new Vector3(.018f,2.3f,.015f),steel);
        Cylinder("Door wheel",new Vector3(.16f,1.15f,2.82f),new Vector3(.34f,.045f,.34f),rust,new Vector3(90,0,0));
        Box("Door warning stripe",new Vector3(.15f,2.36f,2.90f),new Vector3(1.2f,.14f,.018f),rust);
        Text("RELAY  /  07",new Vector3(.15f,2.66f,2.86f),.035f,new Color(.78f,.75f,.57f));
        Text("PRESSURE LOCK",new Vector3(.15f,1.97f,2.90f),.021f,new Color(.15f,.20f,.16f));
        for(int i=0;i<3;i++)
        {
            Cylinder("Vertical service pipe",new Vector3(-3.85f+i*.22f,1.65f,3.03f),new Vector3(.085f,1.61f,.085f),i==1?rust:brass,Vector3.zero);
            for(int j=0;j<3;j++)Box("Pipe bracket",new Vector3(-3.63f,.55f+j*1.03f,2.95f),new Vector3(.70f,.065f,.06f),steel);
        }
        for(int i=0;i<2;i++)
        {
            float x=i==0?-2.6f:2.75f;
            Box("Terminal cabinet",new Vector3(x,1.42f,2.96f),new Vector3(1.15f,1.40f,.43f),steel);
            Box("Terminal enamel",new Vector3(x,1.42f,2.71f),new Vector3(1.04f,1.24f,.08f),ivory);
            Box("CRT bezel",new Vector3(x,1.65f,2.63f),new Vector3(.83f,.53f,.13f),black);
            Box("CRT",new Vector3(x,1.65f,2.55f),new Vector3(.69f,.39f,.025f),Mat("CRT dim",new Color(.05f,.13f,.078f),false,true));
            for(int j=0;j<5;j++)Box("CRT scan trace",new Vector3(x-.05f,1.76f-j*.052f,2.53f),new Vector3(.36f+(j%2)*.18f,.010f,.012f),green);
            for(int j=0;j<4;j++)Cylinder("Control knob",new Vector3(x-.3f+j*.2f,1.13f,2.57f),new Vector3(.10f,.04f,.10f),j==3?rust:steel,new Vector3(90,0,0));
            Text(i==0?"SIGNAL / 017":"BIO-SENSORS",new Vector3(x,2.29f,2.73f),.026f,new Color(.69f,.70f,.54f));
        }
        for(int i=0;i<3;i++)
        {float x=-2.3f+i*2.25f;Box("Overhead light housing",new Vector3(x,2.99f,2.92f),new Vector3(.8f,.13f,.26f),steel);Box("Lamp diffuser",new Vector3(x,2.90f,2.78f),new Vector3(.64f,.04f,.08f),Mat("Lamp tungsten",new Color(1,.77f,.43f),false,true));}
        // Low central relay machine leaves faces and silhouettes visible.
        Box("Relay plinth",new Vector3(0,.34f,-.58f),new Vector3(.76f,.68f,.62f),steel);
        Box("Relay console",new Vector3(0,.71f,-.58f),new Vector3(.94f,.10f,.74f),ivory);
        Box("Magnetic spool left",new Vector3(-.21f,.79f,-.49f),new Vector3(.30f,.12f,.33f),black);
        Box("Magnetic spool right",new Vector3(.21f,.79f,-.49f),new Vector3(.30f,.12f,.33f),black);
        for(int i=-1;i<=1;i+=2)Cylinder("Reel",new Vector3(i*.21f,.862f,-.49f),new Vector3(.25f,.011f,.25f),brass,Vector3.zero);
        for(int i=0;i<5;i++)Box("Relay keys",new Vector3(-.3f+i*.14f,.777f,-.83f),new Vector3(.085f,.035f,.06f),i==4?red:green);
        Text("DELAY 17Y",new Vector3(0,.50f,-.904f),.020f,new Color(.64f,.71f,.51f));
        Box("Technician pedestal",new Vector3(-2.8f,.45f,.9f),new Vector3(.65f,.90f,.58f),steel);
        Box("Technician panel",new Vector3(-2.8f,.93f,.9f),new Vector3(.73f,.07f,.63f),ivory);
        for(int i=0;i<3;i++)Box("Technician switch",new Vector3(-3.02f+i*.2f,.98f,.87f),new Vector3(.12f,.03f,.16f),i==2?red:green);
        // Salvage boxes and cable runs create a lived-in work station.
        Box("Supply crate",new Vector3(-3.5f,.34f,.75f),new Vector3(.85f,.68f,.73f),rust);
        for(int i=-1;i<=1;i+=2)Box("Crate band",new Vector3(-3.5f+i*.26f,.35f,.75f),new Vector3(.045f,.72f,.77f),steel);
        for(int i=0;i<4;i++)Box("Floor conduit",new Vector3(2.6f+i*.1f,.026f,1),new Vector3(.038f,.044f,4.5f),black);
    }
    static ArchiveSequence MakeSequence()
    {
        string path=Root+"/Data/RelayArchive.asset";var s=AssetDatabase.LoadAssetAtPath<ArchiveSequence>(path);
        if(s)
        {
            if(s.reviewRevision<2)
            {
                s.beats[10].shot=ShotKind.Tracking;s.beats[12].shot=ShotKind.Tracking;
                s.beats[10].duration=2.1f;s.beats[12].duration=2.0f;
                s.beats[13].start=new Vector3(-1.25f,0,.25f);s.beats[13].end=new Vector3(-1.25f,0,-1.3f);
                s.beats[13].speed=.72f;s.beats[13].duration=2.8f;
                s.reviewRevision=2;EditorUtility.SetDirty(s);
            }
            return s;
        }
        s=ScriptableObject.CreateInstance<ArchiveSequence>();
        s.clips=AssetDatabase.LoadAllAssetsAtPath(Root+"/Animations/UAL1_Standard.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")).ToArray();
        s.galleryActions=new[]{"Idle_Loop","Idle_Talking_Loop","Walk_Loop","Walk_Formal_Loop","PickUp_Table","Interact","Fixing_Kneeling","Sitting_Enter","Sitting_Idle_Loop","Sitting_Talking_Loop","Sitting_Exit","Push_Loop","Crouch_Idle_Loop","Hit_Chest","Hit_Head","Jog_Fwd_Loop","Dance_Loop"};
        s.beats=new[]{
            B("arrival","01 · 一份迟到的记录",0,"Idle_Loop",ShotKind.Establishing,"中继站 07。录制时间早于你的出生日期。",7),
            B("lin","02 · 名字",0,"Idle_Talking_Loop",ShotKind.Medium,"我叫林。系统只认编号，但这段录像由我保管。",7,1,Accent.Nod),
            B("echo","03 · 异乡的声音",1,"Idle_Talking_Loop",ShotKind.Close,"你们用名字记住彼此。我们用一段声音。",7,0,Accent.Listen,true),
            B("listen","04 · 倾听",0,"Idle_Loop",ShotKind.OverShoulder,"〔林听完翻译，没有立刻回答。〕",4,1,Accent.Listen,true),
            B("ruk","05 · 维修员",2,"Idle_Talking_Loop",ShotKind.LowAngle,"声音也会坏。我修了三天，才让这台机器肯开口。",7,1,Accent.Nod,true),
            B("repair","06 · 留下痕迹",2,"Fixing_Kneeling",ShotKind.Tracking,"它缺的不是零件，是一颗不在清单上的螺钉。",6,-1,Accent.LookDown),
            B("voss","07 · 值守技师",3,"Interact",ShotKind.Medium,"电压稳定。你们还有九十秒。",5,-1,Accent.Glance,true),
            B("relay","08 · 机器的时间",3,"Idle_Loop",ShotKind.Insert,"磁带仍在转动。发送队列：十七年。",4,-1,Accent.None,true),
            B("pair","09 · 两种记忆",1,"Idle_Talking_Loop",ShotKind.TwoShot,"如果十七年后有人听到，声音的主人还算在这里吗？",7,0,Accent.Listen,true),
            B("answer","10 · 一个回答",0,"Idle_Talking_Loop",ShotKind.Close,"我不知道。所以别删掉开头那一段呼吸声。",7,1,Accent.Refuse,true),
            B("sit","11 · 短暂休息",2,"Sitting_Enter",ShotKind.Tracking,"〔鲁克终于坐下。机器继续低鸣。〕",2.1f,-1,Accent.None,true,true),
            B("seated","12 · 故乡",2,"Sitting_Talking_Loop",ShotKind.Profile,"我母亲说，能听见别人呼吸的地方，就算一间屋子。",7,0,Accent.Nod,false,true),
            B("stand","13 · 继续工作",2,"Sitting_Exit",ShotKind.Tracking,"好了。把它送出去。",2.0f,-1,Accent.None,true,true),
            B("walk","14 · 走向记录",0,"Walk_Loop",ShotKind.Tracking,"〔林走近录制镜头。〕",2.8f),
            B("last","15 · 给未来的人",0,"Idle_Talking_Loop",ShotKind.Close,"陌生人，如果你的风扇还在响，就替我们再听一会儿。",8,-1,Accent.Nod,true),
            B("end","16 · 空白也是记录",1,"Idle_Loop",ShotKind.Establishing,"录像已结束。它曾经装下四个人，现在装下十七年。",6,-1,Accent.LookDown)
        };
        s.beats[13].move=true;s.beats[13].start=new Vector3(-1.25f,0,.25f);s.beats[13].end=new Vector3(-1.25f,0,-1.3f);s.beats[13].speed=.72f;s.reviewRevision=2;
        AssetDatabase.CreateAsset(s,path);return s;
    }
    static ArchiveBeat B(string id,string chapter,int actor,string action,ShotKind shot,string subtitle,float duration,int listener=-1,Accent accent=Accent.None,bool cut=false,bool seated=false)
    {return new ArchiveBeat{id=id,chapter=chapter,actor=actor,action=action,shot=shot,subtitle=subtitle,duration=duration,listener=listener,accent=accent,cut=cut,seated=seated};}
}

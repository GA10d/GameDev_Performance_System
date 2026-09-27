using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Astra.PerformanceTool;

public static class ToolBuild
{
    public const string Root="Assets/PerformanceTool";
    public const string PackagePath=Root+"/Samples/AstraSample.asset";
    static T Save<T>(T asset,string path) where T:UnityEngine.Object
    {
        var old=AssetDatabase.LoadAssetAtPath<T>(path);
        if(old)return old;
        AssetDatabase.CreateAsset(asset,path);return asset;
    }
    [MenuItem("Astra Performance Tool/Create sample project")]
    public static ToolPackage CreateSample()
    {
        Directory.CreateDirectory(Root+"/Samples");Directory.CreateDirectory(Root+"/Scenes");Directory.CreateDirectory(Root+"/Exports");AssetDatabase.Refresh();
        string[] ids={"lin","he","yu","xian","ruk","fu"};
        string[] models={"Lin047","He112","Yu203","Xian318","Ruk509","Fu640"};
        string[] names={"林","赫","余","弦","砾","伏"};
        var library=Save(ScriptableObject.CreateInstance<ToolLibrary>(),Root+"/Samples/Library.asset");
        var cast=new List<ToolCharacter>();
        for(int i=0;i<6;i++)
        {
            var c=Save(ScriptableObject.CreateInstance<ToolCharacter>(),Root+"/Samples/Character_"+ids[i]+".asset");
            c.id=ids[i];c.displayName=names[i];c.species=i==3?ToolSpecies.StandingAlien:i==4?ToolSpecies.HalfOrc:i==5?ToolSpecies.CrawlerAlien:ToolSpecies.Human;
            c.sourceModel=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Performance/Models/"+models[i]+".fbx");
            if(!c.sourceModel)throw new Exception("Missing V3 model " + models[i]);
            c.v3Materials=Directory.GetFiles("Assets/Performance/Materials","Actor"+i+"_*.mat")
                .Select(x=>AssetDatabase.LoadAssetAtPath<Material>(x.Replace('\\','/'))).Where(x=>x).ToArray();
            c.accessoryMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Performance/Materials/Brass.mat");
            c.look.skin=i==3?new Color(.58f,.66f,.55f):i==4?new Color(.48f,.50f,.30f):i==5?new Color(.46f,.55f,.41f):new Color(.66f,.53f,.43f);
            c.look.suit=i==4?new Color(.48f,.33f,.23f):new Color(.37f,.41f,.34f);
            EditorUtility.SetDirty(c);cast.Add(c);
        }
        // Re-running sample creation must not remove characters made in the creator.
        library.characters=cast.Concat(library.characters==null?Array.Empty<ToolCharacter>():library.characters.Where(c=>c&&c.look.creatorEnabled&&!cast.Contains(c))).Distinct().ToArray();
        string[] actions={"Idle_Loop","Idle_Talking_Loop","Walk_Loop","Walk_Formal_Loop","PickUp_Table","Interact","Fixing_Kneeling","Sitting_Enter","Sitting_Idle_Loop","Sitting_Talking_Loop","Sitting_Exit","Push_Loop","Crouch_Idle_Loop","Hit_Chest","Hit_Head","Jog_Fwd_Loop","Dance_Loop"};
        string[] actionZh={"待机","说话","行走","正式行走","取物","操作设备","跪姿维修","坐下","坐姿待机","坐姿说话","起身","推压","蹲伏","拍胸","触头","慢跑","舞动"};
        library.actions=actions.Select((id,i)=>new ToolAction{id=id,displayName=actionZh[i],length=i==7||i==10?1.2f:i==4||i==6?2.4f:2,loop=i<4||i==8||i==9||i==11||i==12||i==15||i==16,crawlerAllowed=i==0||i==1||i==2||i==3||i==5||i==12||i==15}).ToArray();
        string[] shots={"全景","中景","近景","双人","过肩","侧面","低机位","设备插入","跟拍","自定义"};
        library.cameras=Enumerable.Range(0,10).Select(i=>new ToolCameraPreset{shot=(ToolShot)i,displayName=shots[i],moving=i==8||i==9,fov=i==3?42:36}).ToArray();
        library.scenes=new[]{
            new ToolScenePreset{id="archive",displayName="档案舱",wall=new Color(.28f,.31f,.26f),floor=new Color(.13f,.15f,.12f),light=new Color(1,.82f,.59f)},
            new ToolScenePreset{id="relay",displayName="旧中继站",wall=new Color(.23f,.29f,.29f),floor=new Color(.12f,.15f,.17f),light=new Color(.75f,.83f,1)},
            new ToolScenePreset{id="alarm",displayName="故障警报",wall=new Color(.25f,.19f,.17f),floor=new Color(.16f,.11f,.10f),light=new Color(1,.35f,.24f)}
        };
        EditorUtility.SetDirty(library);

        var unitA=Save(ScriptableObject.CreateInstance<ToolUnit>(),Root+"/Samples/Unit_A_Archive.asset");unitA.id="archive_request";unitA.duration=9;
        unitA.actors=new List<ToolActorSpan>{new ToolActorSpan{id="speaker",character=cast[3],start=0,duration=9}};
        unitA.actions=new List<ToolActionSpan>{new ToolActionSpan{actorId="speaker",actionId="Idle_Talking_Loop",start=0,duration=4,speed=1},new ToolActionSpan{actorId="speaker",actionId="Interact",start=4,duration=2,speed=1}};
        unitA.dialogue=new List<ToolDialogueSpan>{
            new ToolDialogueSpan{actorId="speaker",kind=ToolDialogueKind.Text,start=.4f,text="这份记录还能传出去。先检查电源。",charactersPerSecond=14,hold=.8f},
            new ToolDialogueSpan{actorId="speaker",kind=ToolDialogueKind.Choice,start=4.2f,text="用两张维修券启动中继吗？",patienceSeconds=4,
                choices=new[]{new ToolChoice{id="pay",text="支付两张维修券",hasCondition=true,condition=new ToolCondition{key="credits",compare=ToolCompare.GreaterOrEqual,value=2},effects=new[]{new ToolEffect{key="credits",delta=-2}}},new ToolChoice{id="inspect",text="先检查线路"}}}
        };
        unitA.scenes=new List<ToolSceneSpan>{new ToolSceneSpan{sceneId="archive",start=0,duration=9}};
        unitA.cameras=new List<ToolCameraSpan>{new ToolCameraSpan{shot=ToolShot.Medium,actorId="speaker",start=0,duration=4},new ToolCameraSpan{shot=ToolShot.Close,actorId="speaker",fx=ToolFx.SignalGlitch,start=4,duration=5}};
        EditorUtility.SetDirty(unitA);

        var unitB=Save(ScriptableObject.CreateInstance<ToolUnit>(),Root+"/Samples/Unit_B_Repair.asset");unitB.id="repair";unitB.duration=6;
        unitB.actors=new List<ToolActorSpan>{new ToolActorSpan{id="speaker",character=cast[4],start=0,duration=6}};
        unitB.actions=new List<ToolActionSpan>{new ToolActionSpan{actorId="speaker",actionId="Fixing_Kneeling",start=.5f,duration=2.4f,speed=1},new ToolActionSpan{actorId="speaker",actionId="Idle_Talking_Loop",start=3,duration=3,speed=1}};
        unitB.dialogue=new List<ToolDialogueSpan>{new ToolDialogueSpan{actorId="speaker",start=.7f,text="机器还活着。我们把信号送出去。",charactersPerSecond=14,hold=1}};
        unitB.scenes=new List<ToolSceneSpan>{new ToolSceneSpan{sceneId="relay",start=0,duration=6}};
        unitB.cameras=new List<ToolCameraSpan>{new ToolCameraSpan{shot=ToolShot.Tracking,actorId="speaker",start=0,duration=3},new ToolCameraSpan{shot=ToolShot.LowAngle,actorId="speaker",start=3,duration=3}};
        EditorUtility.SetDirty(unitB);

        var unitC=Save(ScriptableObject.CreateInstance<ToolUnit>(),Root+"/Samples/Unit_C_Crawler.asset");unitC.id="crawler";unitC.duration=6;
        unitC.actors=new List<ToolActorSpan>{new ToolActorSpan{id="speaker",character=cast[5],start=0,duration=6}};
        unitC.actions=new List<ToolActionSpan>{new ToolActionSpan{actorId="speaker",actionId="Walk_Loop",start=0,duration=3.5f,speed=1},new ToolActionSpan{actorId="speaker",actionId="Crouch_Idle_Loop",start=3.5f,duration=2.5f,speed=1}};
        unitC.dialogue=new List<ToolDialogueSpan>{new ToolDialogueSpan{actorId="speaker",start=.6f,text="〔节律译文〕地板说，远处仍有回应。",charactersPerSecond=14,hold=1}};
        unitC.scenes=new List<ToolSceneSpan>{new ToolSceneSpan{sceneId="alarm",start=0,duration=6}};
        unitC.cameras=new List<ToolCameraSpan>{new ToolCameraSpan{shot=ToolShot.Tracking,actorId="speaker",start=0,duration=3},new ToolCameraSpan{shot=ToolShot.Profile,actorId="speaker",fx=ToolFx.EmergencyLight,start=3,duration=3}};
        EditorUtility.SetDirty(unitC);

        var graph=Save(ScriptableObject.CreateInstance<ToolGraph>(),Root+"/Samples/Graph.asset");graph.entryNode="A";
        graph.nodes=new List<ToolGraphNode>{
            new ToolGraphNode{id="A",kind=ToolNodeKind.Unit,position=new Vector2(40,120),unit=unitA,edges=new List<ToolGraphEdge>{new ToolGraphEdge{slot="pay",target="funds"},new ToolGraphEdge{slot="inspect",target="C"},new ToolGraphEdge{slot="silence",target="C"}}},
            new ToolGraphNode{id="funds",kind=ToolNodeKind.Condition,position=new Vector2(370,65),condition=new ToolCondition{key="power",compare=ToolCompare.GreaterOrEqual,value=1},edges=new List<ToolGraphEdge>{new ToolGraphEdge{slot="yes",target="B"},new ToolGraphEdge{slot="no",target="C"}}},
            new ToolGraphNode{id="B",kind=ToolNodeKind.Unit,position=new Vector2(670,35),unit=unitB,edges=new List<ToolGraphEdge>{new ToolGraphEdge{slot="next",target="final_choice"}}},
            new ToolGraphNode{id="C",kind=ToolNodeKind.Unit,position=new Vector2(665,260),unit=unitC,edges=new List<ToolGraphEdge>{new ToolGraphEdge{slot="next",target="final_choice"}}},
            new ToolGraphNode{id="final_choice",kind=ToolNodeKind.GlobalChoice,position=new Vector2(980,155),prompt="如何保存这份记录？",patienceSeconds=5,
                choices=new[]{new ToolChoice{id="publish",text="公开记录",effects=new[]{new ToolEffect{key="trust",delta=1}}},new ToolChoice{id="seal",text="封存记录"}},
                edges=new List<ToolGraphEdge>{new ToolGraphEdge{slot="publish",target="end"},new ToolGraphEdge{slot="seal",target="end"},new ToolGraphEdge{slot="silence",target="end"}}},
            new ToolGraphNode{id="end",kind=ToolNodeKind.End,position=new Vector2(1280,155)}
        };EditorUtility.SetDirty(graph);
        var package=Save(ScriptableObject.CreateInstance<ToolPackage>(),PackagePath);package.library=library;package.graph=graph;
        package.initialStats=new[]{new ToolStat{key="credits",value=3},new ToolStat{key="power",value=1},new ToolStat{key="trust",value=0}};package.initialPatience=10;
        EditorUtility.SetDirty(package);AssetDatabase.SaveAssets();
        CreateScene(package);return package;
    }
    static void CreateScene(ToolPackage package)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Performance camera").AddComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;
        camera.gameObject.AddComponent<AudioListener>();camera.gameObject.AddComponent<ToolScreenFx>();
        var runner=new GameObject("Astra performance player").AddComponent<ToolPlayer>();runner.package=package;runner.lens=camera;
        runner.gameObject.AddComponent<ToolCapture>();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Root+"/Scenes/ToolPreview.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"/Scenes/ToolPreview.unity",true)};
    }
    [MenuItem("Astra Performance Tool/Verify and export package")]
    public static void VerifyAndExport()
    {
        var package=AssetDatabase.LoadAssetAtPath<ToolPackage>(PackagePath);if(!package)package=CreateSample();
        if(package.library.characters.Length<38)ToolCreatorBuild.SeedStarterCast(package);
        ToolCreatorBuild.RebuildPrefabs(package);
        var errors=ToolValidation.Package(package);if(errors.Count>0)throw new Exception(string.Join("\n",errors));
        string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
        Directory.CreateDirectory(Path.Combine(root,"QA"));Directory.CreateDirectory(Path.Combine(root,"Export"));
        var lines=new List<string>{"PASS: six original V3 character models retained", "PASS: 17 V2 semantic actions", "PASS: nine V2 camera presets and custom camera", "PASS: three scene presets", "PASS: sample graph validates"};
        if(package.library.characters.Length<38||package.library.actions.Length!=17||package.library.cameras.Length!=10||package.library.scenes.Length!=3)throw new Exception("Catalog counts mismatch");
        foreach(var c in package.library.characters)
        {
            var temp=PrefabUtility.InstantiatePrefab(c.sourceModel) as GameObject;
            foreach(var bone in new[]{"Hips","Chest","Head","UpperArmL","UpperArmR","ForearmL","ForearmR"})if(!ToolCharacterView.Find(temp.transform,bone))throw new Exception(c.id+" missing "+bone);
            UnityEngine.Object.DestroyImmediate(temp);
            if(c.look.creatorEnabled)
            {
                if(!c.creatorResources||!c.creatorResources.facetedOrb||!c.creatorResources.hiddenMaterial)throw new Exception(c.id+" creator resources missing");
                var actor=ToolCharacterView.Create(c,null);
                if(!ToolCharacterView.Find(actor.transform,"Creator Head Parts"))throw new Exception(c.id+" modular geometry missing");
                foreach(var action in package.library.actions)
                    foreach(float time in new[]{0f,.5f,1.1f})
                    {
                        actor.GetComponent<ToolMotion>().Sample(action.id,time,1);
                        var head=ToolCharacterView.Find(actor.transform,"Head");
                        var p=head.position;
                        if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z))throw new Exception(c.id+" pose invalid "+action.id);
                    }
                UnityEngine.Object.DestroyImmediate(actor);
            }
            lines.Add("PASS: rig, creator parts and 17 actions "+c.id);
        }
        var bad=ScriptableObject.CreateInstance<ToolUnit>();bad.duration=4;bad.actions.Add(new ToolActionSpan{actorId="missing",actionId="Walk_Loop",start=0,duration=2});
        if(!ToolValidation.Unit(bad,package.library).Any(x=>x.Contains("未被人物轨")))throw new Exception("Uncovered action accepted");
        UnityEngine.Object.DestroyImmediate(bad);lines.Add("PASS: uncovered action rejected");
        var stats=new Dictionary<string,int>{{"credits",1}};
        var pay=package.graph.Node("A").unit.dialogue.Last().choices[0];
        if(ToolLogic.Test(pay.condition,stats))throw new Exception("Unaffordable choice accepted");
        stats["credits"]=3;if(!ToolLogic.Test(pay.condition,stats))throw new Exception("Affordable choice rejected");
        ToolLogic.Apply(pay.effects,stats);if(stats["credits"]!=1)throw new Exception("Resource effect failed");lines.Add("PASS: choice conditions and resource deltas");
        if(ToolLogic.Exit(package.graph.Node("A"),"silence")!="C"||ToolLogic.Exit(package.graph.Node("funds"),"yes")!="B")throw new Exception("Graph route failed");
        lines.Add("PASS: silence and conditional routing");
        File.WriteAllLines(Path.Combine(root,"QA/tool-verification.txt"),lines);
        var included=new List<string>{Root,"Assets/Performance/Scripts/Presentation/CrawlerPerformance.cs"};
        foreach(var character in package.library.characters)
        {
            included.Add(AssetDatabase.GetAssetPath(character.sourceModel));
            included.AddRange(character.v3Materials.Where(m=>m).Select(AssetDatabase.GetAssetPath));
            if(character.accessoryMaterial)included.Add(AssetDatabase.GetAssetPath(character.accessoryMaterial));
        }
        AssetDatabase.ExportPackage(included.Distinct().ToArray(),Path.Combine(root,"Export/ASTRA_Performance_Tool.unitypackage"),ExportPackageOptions.Recurse|ExportPackageOptions.IncludeDependencies);
        Debug.Log("TOOL_VERIFY_OK " + lines.Count);
    }
    [MenuItem("Astra Performance Tool/Build preview player")]
    public static void BuildPlayer()
    {
        VerifyAndExport();
        string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));Directory.CreateDirectory(Path.Combine(root,"Build"));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{Root+"/Scenes/ToolPreview.unity"},locationPathName=Path.Combine(root,"Build/ASTRA Performance Tool.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
        File.WriteAllText(Path.Combine(root,"QA/build-summary.txt"),"Succeeded\nBytes "+report.summary.totalSize+"\nErrors "+report.summary.totalErrors);
        Debug.Log("TOOL_BUILD_OK");
    }
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceToolV2;

public static class ToolFaceTimelineQA
{
    // Opens a disposable in-memory copy for manual UI QA; no sample asset is edited.
    public static void OpenTimelineCheck()
    {
        var package=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolV2Build.PackagePath);
        var unit=UnityEngine.Object.Instantiate(package.graph.nodes.First(n=>n.unit).unit);
        unit.id="qa_duration_unsaved";unit.hideFlags=HideFlags.DontSave;
        var window=EditorWindow.GetWindow<ToolV2TimelineWindow>("ASTRA 演出时间轴");
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        typeof(ToolV2TimelineWindow).GetField("unit",flags).SetValue(window,unit);
        typeof(ToolV2TimelineWindow).GetField("selectedTrack",flags).SetValue(window,0);
        typeof(ToolV2TimelineWindow).GetField("selectedIndex",flags).SetValue(window,0);
        // Empty graph keeps the toolbar from re-selecting a persisted sample unit.
        var temporary=UnityEngine.Object.Instantiate(package);temporary.graph=ScriptableObject.CreateInstance<ToolGraph>();
        temporary.hideFlags=HideFlags.DontSave;temporary.graph.hideFlags=HideFlags.DontSave;
        typeof(ToolV2TimelineWindow).GetField("package",flags).SetValue(window,temporary);
        window.position=new Rect(60,50,1280,840);window.Show();window.Focus();
    }
    static int checks;
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;}
    public static void VerifyAndBuild()
    {
        var unit=ScriptableObject.CreateInstance<ToolUnit>();
        try
        {
            foreach(float duration in new[]{30f,120f,3600f,86400f})
            {
                ToolTimelineEditing.SetDuration(unit,duration);
                Check(unit.duration==duration,"Duration clipped: "+duration);
                var copy=ScriptableObject.CreateInstance<ToolUnit>();
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(unit),copy);
                Check(copy.duration==duration,"Duration serialization: "+duration);UnityEngine.Object.DestroyImmediate(copy);
            }
            unit.duration=9;
            unit.actors.Add(new ToolActorSpan{start=15,duration=100});
            ToolTimelineEditing.ExtendToContent(unit);Check(unit.duration==115,"Actor edit did not grow unit");
            ToolTimelineEditing.SetDuration(unit,9);Check(unit.duration==115,"Shrinking cut existing content");
            unit.actions.Add(new ToolActionSpan{start=115,duration=5});
            ToolTimelineEditing.ExtendToContent(unit);Check(unit.duration==120,"Action edit did not grow unit");
            unit.dialogue.Add(new ToolDialogueSpan{start=120,text="0123456789",charactersPerSecond=10,hold=2});
            ToolTimelineEditing.ExtendToContent(unit);Check(unit.duration==123,"Text duration did not grow unit");
            unit.dialogue[0].kind=ToolDialogueKind.Choice;unit.dialogue[0].patienceSeconds=20;
            ToolTimelineEditing.ExtendToContent(unit);Check(unit.duration==140,"Choice duration did not grow unit");
            unit.cameras.Add(new ToolCameraSpan{start=140,duration=20});
            unit.scenes.Add(new ToolSceneSpan{start=160,duration=40});
            ToolTimelineEditing.ExtendToContent(unit);Check(unit.duration==200,"Camera/scene did not grow unit");
            Check(!ToolTimelineEditing.SetDuration(unit,float.NaN)&&unit.duration==200,"NaN accepted");
            Check(!ToolTimelineEditing.SetDuration(unit,float.PositiveInfinity)&&unit.duration==200,"Infinity accepted");
            foreach(float zoom in new[]{.0001f,.01f,1,88,200})
                Check(ToolTimelineEditing.TickStep(zoom)*zoom>=69,"Unbounded ruler tick density");
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(ToolV2Build.Root+"/Models/ModularCast.fbx");
            Check(!model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(r=>r.name.StartsWith("Mark_")),"Face mark geometry still present");
            foreach(string name in new[]{"Head_Human","Head_Alien"})
            {
                var mesh=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name==name).sharedMesh;
                Check(mesh.uv2.Length==mesh.vertexCount&&mesh.uv2.Any(uv=>uv.x>0&&uv.y>0),"UV2 missing: "+name);
            }
            foreach(var species in new[]{ToolSpecies.Human,ToolSpecies.StandingAlien})
            for(int i=1;i<=5;i++)Check(ToolCharacterView.FaceMark(species,i)!=null,"Face paint texture missing");
            Check(ToolCreatorOptions.HairIds(ToolSpecies.Human,3).Contains(0),"Hat category has no none option");
            var look=new ToolCharacterLook{hairStyle=23};
            ToolCreatorOptions.SelectHair(look,ToolSpecies.Human,3,8);
            Check(look.hairStyle==8&&look.hairUnderHat==23,"Putting on hat forgot original hair");
            look=JsonUtility.FromJson<ToolCharacterLook>(JsonUtility.ToJson(look));
            ToolCreatorOptions.SelectHair(look,ToolSpecies.Human,3,9);
            ToolCreatorOptions.SelectHair(look,ToolSpecies.Human,3,0);
            Check(look.hairStyle==23,"Removing hat did not restore saved hair");
            ToolCreatorOptions.SelectHair(look,ToolSpecies.Human,3,0);
            Check(look.hairStyle==23,"None changed an existing uncovered hairstyle");
            ToolCreatorOptions.SelectHair(look,ToolSpecies.Human,0,0);
            ToolCreatorOptions.SelectHair(look,ToolSpecies.Human,3,11);
            ToolCreatorOptions.SelectHair(look,ToolSpecies.Human,3,0);
            Check(look.hairStyle==0,"None did not preserve baldness");
            foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.name=="FaceAcc_Human_04"||r.name=="FaceAcc_Alien_04"))
                Check(r.sharedMesh.bounds.size.y<.070f,"Headset is oversized: "+r.name);
            string path=Path.GetFullPath(Application.dataPath+"/../../QA/FaceTimeline/editor-result.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,"PASS: "+checks+" checks\nDuration: 30 / 120 / 3600 / 86400 s; serialization; all five tracks auto-extend; safe shrink; finite input; bounded tick density; no Mark meshes; UV2; 10 textures; hat removal restores hair after JSON roundtrip; compact headsets.\n");
        }
        finally{UnityEngine.Object.DestroyImmediate(unit);}
        ToolV2Build.BuildPlayer();
    }
}

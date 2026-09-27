using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceToolV2;

public static class ToolChinQA
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Build(){Verify();ToolWomenQA.Build();}
    public static void Verify()
    {
        var package=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolV2Build.PackagePath);
        Check(JsonUtility.FromJson<ToolCharacterLook>("{\"faceShape\":2}").chinStyle==0,"Legacy chin changed");
        int cases=0;var baked=new Mesh();
        foreach(int family in Enumerable.Range(0,12))
        {
            var c=ToolV2CreatorBuild.NewCharacter(package,family<2?ToolSpecies.Human:ToolSpecies.StandingAlien,family==1?1:0);
            c.look.hairStyle=0;c.look.faceShape=family<2?0:family-2;
            var actor=ToolCharacterView.Create(c,null);
            try
            {
                foreach(int chin in Enumerable.Range(0,3))foreach(float extreme in new[]{-1f,1f})
                {
                    c.look.chinStyle=chin;c.look.faceWidth=1+extreme*.08f;c.look.jawWidth=extreme;
                    c.look.mouthShape=chin==1?2:5;ToolCharacterView.ApplyLook(actor,c);
                    var head=actor.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.enabled&&r.name.StartsWith("Head_"));
                    foreach(string key in new[]{"ChinRound","ChinSquare"})
                    {
                        int index=Enumerable.Range(0,head.sharedMesh.blendShapeCount).Single(i=>head.sharedMesh.GetBlendShapeName(i).EndsWith(key));
                        Check(head.GetBlendShapeWeight(index)==((key=="ChinRound"?1:2)==chin?100:0),"Chin selector failed");
                    }
                    foreach(string action in new[]{"Idle_Talking_Loop","Fixing_Kneeling","Hit_Head","Dance_Loop"})
                    {
                        actor.GetComponent<ToolMotion>().Sample(action,.6f);head.BakeMesh(baked,true);cases++;
                        Check(baked.vertices.All(v=>!float.IsNaN(v.x)&&head.transform.TransformPoint(v).sqrMagnitude<36),"Invalid chin deformation "+family);
                    }
                    var loaded=JsonUtility.FromJson<ToolCharacterLook>(JsonUtility.ToJson(c.look));
                    Check(loaded.chinStyle==chin&&loaded.mouthShape==c.look.mouthShape,"Chin JSON roundtrip lost state");
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(c);}
        }
        UnityEngine.Object.DestroyImmediate(baked);
        Check(Shader.Find("Astra/ToolV2/Character").FindPropertyIndex("_MouthTex")<0,"Mouth UV shader remains");
        Check(!AssetDatabase.IsValidFolder(ToolV2Build.Root+"/Resources/Mouths"),"Obsolete mouth textures still bundled");
        string root=Path.GetFullPath(Application.dataPath+"/../..");Directory.CreateDirectory(root+"/QA/ChinHair");
        File.WriteAllText(root+"/QA/ChinHair/editor-checks.txt","PASS: "+cases+" chin/extreme/action poses; male, female, ten alien anatomies; legacy JSON default and new JSON roundtrip; no mouth texture shader/resources.\n");
    }
}

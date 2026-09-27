using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceTool;

public static class ToolCreatorBuild
{
    const string Creator="Assets/PerformanceTool/Creator";
    const string Characters="Assets/PerformanceTool/Characters";
    public static ToolCreatorResources EnsureResources()
    {
        if(!AssetDatabase.IsValidFolder(Creator))AssetDatabase.CreateFolder("Assets/PerformanceTool","Creator");
        string path=Creator+"/CreatorResources.asset";
        var res=AssetDatabase.LoadAssetAtPath<ToolCreatorResources>(path);
        if(res)return res;
        res=ScriptableObject.CreateInstance<ToolCreatorResources>();AssetDatabase.CreateAsset(res,path);
        res.facetedOrb=UVOrb("Faceted orb",8,5);
        res.diamond=Flat("Diamond",new[]{new Vector3(0,.5f,0),new Vector3(.5f,0,0),new Vector3(0,0,.5f),new Vector3(-.5f,0,0),new Vector3(0,0,-.5f),new Vector3(0,-.5f,0)},new[]{0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1});
        res.wedge=Flat("Wedge",new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(0,.5f,-.5f),new Vector3(0,.5f,.5f)},new[]{0,1,4,2,5,3,0,4,5,0,5,2,1,3,5,1,5,4,0,2,3,0,3,1});
        res.box=Flat("Box",new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)},new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7});
        foreach(var mesh in new[]{res.facetedOrb,res.diamond,res.wedge,res.box})AssetDatabase.AddObjectToAsset(mesh,res);
        res.solidMaterial=new Material(Shader.Find("Standard")){name="Creator faceted solid"};res.solidMaterial.SetFloat("_Glossiness",0);res.solidMaterial.SetFloat("_Metallic",0);
        var hiddenShader=Shader.Find("Astra/PerformanceTool/Invisible");if(!hiddenShader)throw new Exception("Creator Invisible.shader did not import");
        res.hiddenMaterial=new Material(hiddenShader){name="Creator hidden source parts"};
        AssetDatabase.AddObjectToAsset(res.solidMaterial,res);AssetDatabase.AddObjectToAsset(res.hiddenMaterial,res);
        EditorUtility.SetDirty(res);AssetDatabase.SaveAssets();return res;
    }
    static Mesh Flat(string name,Vector3[] points,int[] triangles)
    {
        var vertices=new Vector3[triangles.Length];var normals=new Vector3[triangles.Length];
        for(int i=0;i<triangles.Length;i+=3)
        {
            Vector3 a=points[triangles[i]],b=points[triangles[i+1]],c=points[triangles[i+2]];
            var normal=Vector3.Cross(b-a,c-a).normalized;
            for(int j=0;j<3;j++){vertices[i+j]=points[triangles[i+j]];normals[i+j]=normal;}
        }
        var mesh=new Mesh{name=name,vertices=vertices,normals=normals,triangles=Enumerable.Range(0,triangles.Length).ToArray()};mesh.RecalculateBounds();return mesh;
    }
    static Mesh UVOrb(string name,int sides,int rings)
    {
        var vertices=new List<Vector3>();var indices=new List<int>();
        for(int y=0;y<rings;y++)for(int x=0;x<sides;x++)
        {
            float a=x*Mathf.PI*2/sides,b=(x+1)*Mathf.PI*2/sides;
            float t=y*Mathf.PI/rings,u=(y+1)*Mathf.PI/rings;
            Vector3 P(float az,float el)=>new Vector3(Mathf.Cos(az)*Mathf.Sin(el),Mathf.Cos(el),Mathf.Sin(az)*Mathf.Sin(el))*.5f;
            var p0=P(a,t);var p1=P(b,t);var p2=P(a,u);var p3=P(b,u);
            vertices.AddRange(new[]{p0,p2,p1,p1,p2,p3});
        }
        for(int i=0;i<vertices.Count;i++)indices.Add(i);
        var mesh=new Mesh{name=name,vertices=vertices.ToArray(),triangles=indices.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    public static ToolCharacter NewCharacter(ToolPackage package,ToolSpecies species)
    {
        var source=package.library.characters.FirstOrDefault(c=>c&&c.id==(species==ToolSpecies.Human?"lin":"xian"));
        if(!source)throw new Exception("V3 source character not found: "+species+"; library="+string.Join(",",package.library.characters.Select(c=>c?c.id:"null")));
        var c=ScriptableObject.CreateInstance<ToolCharacter>();
        c.id=(species==ToolSpecies.Human?"human_":"alien_")+Guid.NewGuid().ToString("N").Substring(0,8);
        c.displayName=species==ToolSpecies.Human?"新建人类":"新建外星人";
        c.species=species;c.sourceModel=source.sourceModel;c.v3Materials=source.v3Materials;c.accessoryMaterial=source.accessoryMaterial;
        c.creatorResources=EnsureResources();c.look.creatorEnabled=true;
        c.look.skin=ToolCreatorOptions.Skin(species)[0];c.look.hair=ToolCreatorOptions.HairColors[0];
        c.look.suit=ToolCreatorOptions.SuitColors[0];c.look.eyes=ToolCreatorOptions.EyeColors[0];c.look.accent=ToolCreatorOptions.AccentColors[0];
        return c;
    }
    public static void SetBase(ToolCharacter character,ToolPackage package,int baseIndex)
    {
        string id=character.species==ToolSpecies.Human?new[]{"lin","he","yu"}[Mathf.Clamp(baseIndex,0,2)]:"xian";
        var source=package.library.characters.First(c=>c&&c.id==id);
        character.sourceModel=source.sourceModel;character.v3Materials=source.v3Materials;
    }
    public static int BaseIndex(ToolCharacter character,ToolPackage package)
    {
        if(character.species!=ToolSpecies.Human)return 0;
        string[] ids={"lin","he","yu"};
        for(int i=0;i<ids.Length;i++)if(character.sourceModel==package.library.characters.First(c=>c.id==ids[i]).sourceModel)return i;
        return 0;
    }
    public static void SaveCharacter(ToolCharacter character,ToolPackage package)
    {
        if(!character||!package||!package.library)throw new Exception("Missing character or library");
        if(character.species!=ToolSpecies.Human&&character.species!=ToolSpecies.StandingAlien)throw new Exception("Creator only supports human and standing alien");
        if(string.IsNullOrWhiteSpace(character.id)||string.IsNullOrWhiteSpace(character.displayName))throw new Exception("角色 ID 和名字不能为空");
        if(package.library.characters.Any(c=>c&&c!=character&&c.id==character.id))throw new Exception("角色 ID 已存在: "+character.id);
        character.creatorResources=EnsureResources();character.look.creatorEnabled=true;
        if(!AssetDatabase.IsValidFolder(Characters))AssetDatabase.CreateFolder("Assets/PerformanceTool","Characters");
        if(!AssetDatabase.IsValidFolder(Characters+"/Prefabs"))AssetDatabase.CreateFolder(Characters,"Prefabs");
        string path=AssetDatabase.GetAssetPath(character);
        if(string.IsNullOrEmpty(path))
        {
            string safe=string.Concat(character.id.Select(ch=>char.IsLetterOrDigit(ch)||ch=='_'||ch=='-'?ch:'_'));
            path=AssetDatabase.GenerateUniqueAssetPath(Characters+"/Character_"+safe+".asset");AssetDatabase.CreateAsset(character,path);
        }
        var list=package.library.characters.Where(c=>c).ToList();if(!list.Contains(character)){list.Add(character);package.library.characters=list.ToArray();}
        string prefab=Characters+"/Prefabs/"+Path.GetFileNameWithoutExtension(path)+".prefab";
        var actor=ToolCharacterView.Create(character,null,false);
        actor.AddComponent<ToolCharacterInstance>().preset=character;
        PrefabUtility.SaveAsPrefabAsset(actor,prefab);UnityEngine.Object.DestroyImmediate(actor);
        EditorUtility.SetDirty(character);EditorUtility.SetDirty(package.library);AssetDatabase.SaveAssets();
    }
    [MenuItem("Astra Performance Tool/Seed creator cast")]
    public static void SeedMenu(){var package=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolBuild.PackagePath);if(!package)package=ToolBuild.CreateSample();SeedStarterCast(package);}
    [MenuItem("Astra Performance Tool/Rebuild creator prefabs")]
    public static void RebuildPrefabsMenu()
    {
        var package=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolBuild.PackagePath);
        if(!package)throw new Exception("Missing sample package");
        RebuildPrefabs(package);
    }
    public static void RebuildPrefabs(ToolPackage package)
    {
        foreach(var character in package.library.characters.Where(c=>c&&c.look.creatorEnabled).ToArray())SaveCharacter(character,package);
    }
    public static void InspectAnchors()
    {
        var package=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolBuild.PackagePath);
        foreach(string id in new[]{"lin","he","xian"})
        {
            var source=package.library.characters.First(c=>c.id==id);
            var actor=ToolCharacterView.Create(source,null,false);
            var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();
            var mesh=new Mesh();skin.BakeMesh(mesh);
            for(int i=0;i<mesh.subMeshCount;i++)
            {
                string name=skin.sharedMaterials[i]?skin.sharedMaterials[i].name:"null";
                if(!new[]{"Eye","Iris","Glass","Metal","Lip","Hair","Skin"}.Any(name.Contains))continue;
                var vertices=mesh.vertices;var triangles=mesh.GetTriangles(i);
                Vector3 min=new Vector3(99,99,99),max=new Vector3(-99,-99,-99);
                foreach(int index in triangles)
                {
                    Vector3 p=skin.transform.TransformPoint(vertices[index]);
                    min=Vector3.Min(min,p);max=Vector3.Max(max,p);
                }
                Debug.Log("ANCHOR "+id+" "+name+" y="+min.y.ToString("F3")+".."+max.y.ToString("F3")+" z="+min.z.ToString("F3")+".."+max.z.ToString("F3"));
            }
            UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(actor);
        }
    }
    public static void SeedStarterCast(ToolPackage package)
    {
        EnsureResources();
        string[] humanNames={"巡检员","档案员","信号员","医疗员","回收者","驾驶员","守夜人","焊接师","观察员","调度员","船员","证人","研究员","学徒","维修主管","失联者"};
        string[] alienNames={"静默观测者","深港来客","长颅译员","孢纹使者","空舱旅者","光环记录者","双角侦察者","旧域访客","绿脊工匠","晶冠导航者","侧翼巡游者","四瞳译者","背鳍哨兵","远星遗民","信标守护者","三脊信使"};
        for(int species=0;species<2;species++)for(int i=0;i<16;i++)
        {
            var type=species==0?ToolSpecies.Human:ToolSpecies.StandingAlien;
            string id=(species==0?"creator_h":"creator_a")+(i+1).ToString("00");
            if(package.library.characters.Any(c=>c&&c.id==id))continue;
            var c=NewCharacter(package,type);c.id=id;c.displayName=(species==0?humanNames:alienNames)[i];
            ToolCreatorOptions.Randomize(c.look,type,new System.Random(7403+species*419+i*37));
            c.look.hairStyle=i;c.look.faceShape=i%10;c.look.eyeShape=i%12;c.look.bodyType=i%8;c.look.outfitStyle=i%12;
            c.look.faceAccessory=i%12;c.look.gearAccessory=(i+5)%12;c.look.facialMark=i%8;
            c.look.skin=ToolCreatorOptions.Skin(type)[i];c.look.suit=ToolCreatorOptions.SuitColors[(i*5)%16];
            c.look.hair=ToolCreatorOptions.HairColors[(i*3)%16];c.look.eyes=ToolCreatorOptions.EyeColors[i%12];
            c.look.accent=ToolCreatorOptions.AccentColors[(i*7)%12];
            SetBase(c,package,species==0?i%3:0);SaveCharacter(c,package);
        }
        Debug.Log("TOOL_CREATOR_SEEDED "+package.library.characters.Length);
    }
}

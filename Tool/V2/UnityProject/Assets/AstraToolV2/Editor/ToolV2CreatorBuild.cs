using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceToolV2;

public static class ToolV2CreatorBuild
{
    const string Root=ToolV2Build.Root;
    public static ToolAvatarCatalog CreateCatalog()
    {
        string path=Root+"/Data/AvatarCatalog.asset";
        var catalog=AssetDatabase.LoadAssetAtPath<ToolAvatarCatalog>(path);
        if(catalog)return catalog;
        catalog=ScriptableObject.CreateInstance<ToolAvatarCatalog>();
        catalog.modularModel=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/ModularCast.fbx");
        catalog.clips=AssetDatabase.LoadAllAssetsAtPath(Root+"/Animations/UAL1_Standard.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")&&c.name!="A_TPose").OrderBy(c=>c.name).ToArray();
        var sources=new List<GameObject>{catalog.modularModel};
        sources.AddRange(new[]{"Lin","Echo","Ruk","Voss"}.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/DemoCast/"+n+".fbx")));
        var materials=new List<Material>();
        foreach(var source in sources)
        foreach(var m in source.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m).GroupBy(m=>m.name).Select(g=>g.First()))
        {
            if(materials.Any(x=>x.name==m.name))continue;
            var material=new Material(Shader.Find("Astra/ToolV2/Character")){name=m.name};
            material.color=m.HasProperty("_Color")?m.color:new Color(.45f,.5f,.42f);
            AssetDatabase.CreateAsset(material,Root+"/Materials/"+m.name+".mat");materials.Add(material);
        }
        catalog.materials=materials.ToArray();AssetDatabase.CreateAsset(catalog,path);return catalog;
    }
    public static List<ToolCharacter> CreateOriginalCast(ToolAvatarCatalog catalog)
    {
        var result=new List<ToolCharacter>();
        string[] ids={"Lin","Echo","Ruk","Voss"},names={"林 · 航行员","回声 · 外星信使","砾 · 半兽人维修员","沃斯 · 旧航路"};
        for(int i=0;i<ids.Length;i++)
        {
            string path=Root+"/Characters/Original_"+ids[i]+".asset";
            var c=AssetDatabase.LoadAssetAtPath<ToolCharacter>(path);
            if(!c){c=ScriptableObject.CreateInstance<ToolCharacter>();c.id=ids[i].ToLowerInvariant();c.displayName=names[i];c.species=i==1?ToolSpecies.StandingAlien:i==2?ToolSpecies.HalfOrc:ToolSpecies.Human;c.catalog=catalog;c.sourceModel=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/DemoCast/"+ids[i]+".fbx");AssetDatabase.CreateAsset(c,path);}
            result.Add(c);
        }
        return result;
    }
    public static ToolCharacter NewCharacter(ToolPackage package,ToolSpecies species,int humanModel=0)
    {
        var c=ScriptableObject.CreateInstance<ToolCharacter>();c.catalog=package.library.characters.First().catalog;
        c.id="custom_"+Guid.NewGuid().ToString("N").Substring(0,10);c.displayName=species==ToolSpecies.Human?"新建人类":"新建外星人";c.species=species;
        c.sourceModel=c.catalog.modularModel;c.look.creatorEnabled=true;c.look.hairStyle=species==ToolSpecies.Human?1:0;
        c.look.skin=ToolCreatorOptions.Skin(species)[0];if(ToolCreatorOptions.Alien(species))c.look.hair=c.look.skin;c.look.eyes=new Color(.025f,.045f,.035f);
        if(species==ToolSpecies.Human){ToolCreatorOptions.SetHumanModel(c.look,humanModel);c.displayName=humanModel==1?"新建女性角色":"新建男性角色";}
        return c;
    }
    public static int BaseIndex(ToolCharacter c,ToolPackage p)=>0;
    public static void SetBase(ToolCharacter c,ToolPackage p,int i){}
    public static ToolCharacter SaveCharacter(ToolCharacter draft,ToolPackage package,ToolCharacter existing=null)
    {
        if(string.IsNullOrWhiteSpace(draft.displayName)||!System.Text.RegularExpressions.Regex.IsMatch(draft.id??"",@"^[a-zA-Z0-9_\-]{1,64}$"))throw new ArgumentException("名字不能为空；ID 仅使用字母、数字、下划线或短横线，最多 64 字符。");
        if(package.library.characters.Any(c=>c&&c!=existing&&c!=draft&&c.id==draft.id))throw new ArgumentException("ID 已存在，请使用不同的 ID。");
        if(!draft.catalog||!draft.catalog.modularModel)throw new ArgumentException("缺少模块化模型");
        ToolCharacter saved=existing;
        if(!saved){saved=UnityEngine.Object.Instantiate(draft);saved.name=draft.id;AssetDatabase.CreateAsset(saved,AssetDatabase.GenerateUniqueAssetPath(Root+"/Characters/"+draft.id+".asset"));}
        else {Undo.RecordObject(saved,"Save V2 character");EditorUtility.CopySerialized(draft,saved);EditorUtility.SetDirty(saved);}
        if(!package.library.characters.Contains(saved)){Undo.RecordObject(package.library,"Add V2 character");package.library.characters=package.library.characters.Concat(new[]{saved}).ToArray();EditorUtility.SetDirty(package.library);}
        SavePrefab(saved);AssetDatabase.SaveAssets();return saved;
    }
    static void SavePrefab(ToolCharacter c)
    {
        Directory.CreateDirectory(Root+"/Characters/Prefabs");
        var go=ToolCharacterView.Create(c,null);var instance=go.AddComponent<ToolCharacterInstance>();instance.preset=c;
        PrefabUtility.SaveAsPrefabAsset(go,Root+"/Characters/Prefabs/"+c.id+".prefab");UnityEngine.Object.DestroyImmediate(go);
    }
    public static void RebuildPrefabs(ToolPackage package){foreach(var c in package.library.characters)if(c)SavePrefab(c);AssetDatabase.SaveAssets();}
    public static void SeedWomen(ToolPackage package)
    {
        string[] roles={"探险者","驻站员","接待员","远行者","信号技师","导航员","警卫","指挥员","仪式学者","工程师"};
        for(int i=0;i<10;i++)
        {
            string id="creator_f"+i.ToString("00");if(package.library.Character(id))continue;
            var c=NewCharacter(package,ToolSpecies.Human,1);c.id=id;c.displayName="女性 · "+roles[i];
            ToolCreatorOptions.ApplyOutfit(c.look,22+i);c.look.hairStyle=i+1;
            c.look.skin=ToolCreatorOptions.HumanSkin[(i*3)%16];c.look.hair=ToolCreatorOptions.HairColors[i%8];
            c.look.suit=ToolCreatorOptions.SuitColors[i];c.look.accent=ToolCreatorOptions.AccentColors[i];
            c.look.eyes=ToolCreatorOptions.EyeColors[i%6]*.55f;c.look.eyes.a=1;
            SaveCharacter(c,package);UnityEngine.Object.DestroyImmediate(c);
        }
    }
    public static void SeedStarterCast(ToolPackage package)
    {
        for(int species=0;species<2;species++)for(int i=0;i<16;i++)
        {
            string id="creator_"+(species==0?"h":"a")+i.ToString("00");if(package.library.Character(id))continue;
            var c=NewCharacter(package,species==0?ToolSpecies.Human:ToolSpecies.StandingAlien);
            ToolCreatorOptions.Randomize(c.look,c.species,new System.Random(300+species*80+i));c.id=id;
            string[] roles={"航行员","档案员","工程师","联络员","调查员","巡逻员","医生","引路人","驻站员","技工","传译员","旧旅客","观测员","舱务员","研究员","信使"};
            c.displayName=(species==0?"人类 · ":"外星人 · ")+roles[i];c.look.faceShape=i%10;c.look.hairStyle=i%12;c.look.outfitStyle=i%10;c.look.legStyle=i%10;c.look.footStyle=i%10;
            c.look.faceAccessory=i%5==0?0:i%10;c.look.gearAccessory=i%10;c.look.facialMark=i%6;
            if(species==0&&c.look.hairStyle>=8)c.look.faceAccessory=0;
            c.look.eyes=ToolCreatorOptions.EyeColors[i%6]*.55f;c.look.eyes.a=1;
            SaveCharacter(c,package);UnityEngine.Object.DestroyImmediate(c);
        }
    }
}

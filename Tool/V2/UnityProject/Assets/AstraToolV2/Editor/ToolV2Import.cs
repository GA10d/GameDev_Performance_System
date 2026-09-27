using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ToolV2Import
{
    public const string Root="Assets/AstraToolV2";
    public static readonly string[] Ids={"DemoCast/Lin","DemoCast/Echo","DemoCast/Ruk","DemoCast/Voss","ModularCast"};
    public static void Configure()
    {
        foreach(string id in Ids)
        {
            string path=Root+"/Models/"+id+".fbx";
            var m=(ModelImporter)AssetImporter.GetAtPath(path);
            m.animationType=ModelImporterAnimationType.Human;m.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            m.importBlendShapes=true;m.importAnimation=false;m.importCameras=false;m.importLights=false;m.isReadable=true;m.optimizeGameObjects=false;
            var d=m.humanDescription;
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            d.skeleton=model.GetComponentsInChildren<Transform>(true).Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray();
            var h=new List<HumanBone>();
            Add(h,"Hips","Hips");Add(h,"Spine","Abdomen");Add(h,"Chest","Torso");Add(h,"UpperChest","Chest");Add(h,"Neck","Neck");Add(h,"Head","Head");
            foreach(string side in new[]{"Left","Right"})
            {
                string s=side=="Left"?".L":".R";
                foreach(string pair in new[]{"Shoulder:Shoulder","UpperArm:UpperArm","LowerArm:LowerArm","Hand:Hand","UpperLeg:UpperLeg","LowerLeg:LowerLeg","Foot:Foot"})
                {var p=pair.Split(':');Add(h,side+p[0],p[1]+s);}
                foreach(string finger in new[]{"Thumb","Index","Middle","Ring","Little"})
                {string source=finger=="Little"?"Pinky":finger;Add(h,side+finger+"Proximal",source+"2"+s);Add(h,side+finger+"Intermediate",source+"3"+s);Add(h,side+finger+"Distal",source+"4"+s);}
            }
            d.human=h.ToArray();d.armStretch=.02f;d.legStretch=.02f;d.upperArmTwist=.5f;d.lowerArmTwist=.5f;d.upperLegTwist=.5f;d.lowerLegTwist=.5f;d.feetSpacing=0;
            m.humanDescription=d;m.SaveAndReimport();
        }
        var ai=(ModelImporter)AssetImporter.GetAtPath(Root+"/Animations/UAL1_Standard.fbx");
        ai.animationType=ModelImporterAnimationType.Human;ai.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        ai.bakeAxisConversion=true;ai.importAnimation=true;ai.motionNodeName="root";ai.importCameras=false;ai.importLights=false;
        var clips=ai.defaultClipAnimations;
        foreach(var c in clips){c.name=c.name.Substring(c.name.LastIndexOf('|')+1);c.loopTime=c.name.EndsWith("_Loop");c.loopPose=c.loopTime;c.lockRootRotation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=true;}
        ai.clipAnimations=clips;ai.SaveAndReimport();
        var lines=new List<string>();
        foreach(string id in Ids)
        {
            var av=AssetDatabase.LoadAllAssetsAtPath(Root+"/Models/"+id+".fbx").OfType<Avatar>().FirstOrDefault();
            lines.Add(id+": avatar="+(av?av.name:"missing")+" valid="+(av&&av.isValid)+" human="+(av&&av.isHuman));
            if(!av||!av.isValid||!av.isHuman)throw new Exception("Invalid humanoid: "+id);
        }
        foreach(var c in AssetDatabase.LoadAllAssetsAtPath(Root+"/Animations/UAL1_Standard.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview")))lines.Add(c.name+" "+c.length+"s human="+c.humanMotion);
        string root=Path.GetFullPath(Application.dataPath+"/../..");Directory.CreateDirectory(root+"/QA");File.WriteAllLines(root+"/QA/import-report.txt",lines);
        Debug.Log("CAST_IMPORT_OK "+string.Join("\n",lines));
    }
    static void Add(List<HumanBone> h,string human,string bone)
    {var id=(HumanBodyBones)Enum.Parse(typeof(HumanBodyBones),human);h.Add(new HumanBone{humanName=HumanTrait.BoneName[(int)id],boneName=bone,limit=new HumanLimit{useDefaultValues=true}});}
}

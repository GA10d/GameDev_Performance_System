using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceTool;

public sealed class ToolCharacterCreatorWindow : EditorWindow
{
    ToolPackage package;
    [SerializeField] ToolCharacter character;
    PreviewRenderUtility preview;
    GameObject actor,stageObject;
    ToolStage stage;
    Vector2 scroll;
    int section,actionIndex;
    readonly int[] subsection=new int[6];
    float yaw,zoom=2.35f;
    string notice;
    static readonly string[] Sections={"身份","面容","头部","身形","装扮","配色"};
    static readonly string[][] Subsections={
        new[]{"基本资料","V3 底模"},
        new[]{"脸型","眼睛","眉骨","鼻部","口部"},
        new[]{"发型 / 颅冠"},
        new[]{"体型","整体比例"},
        new[]{"服装","面部配饰","装备配饰","面部纹样"},
        new[]{"肤色","发色","服装色","眼色","强调色"}
    };
    [MenuItem("Astra Performance Tool/New character/Human")]
    public static void NewHuman()=>OpenNew(ToolSpecies.Human);
    [MenuItem("Astra Performance Tool/New character/Alien")]
    public static void NewAlien()=>OpenNew(ToolSpecies.StandingAlien);
    public static void OpenNew(ToolSpecies species)
    {
        var w=GetWindow<ToolCharacterCreatorWindow>("ASTRA 捏人");w.minSize=new Vector2(1050,690);
        w.package=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolBuild.PackagePath);
        if(!w.package)w.package=ToolBuild.CreateSample();
        w.ReleaseCharacter();w.character=ToolCreatorBuild.NewCharacter(w.package,species);w.section=0;w.zoom=species==ToolSpecies.StandingAlien?2.75f:2.35f;w.notice="";w.ResetPreview();w.Show();
    }
    public static void OpenEdit(ToolCharacter value)
    {
        var w=GetWindow<ToolCharacterCreatorWindow>("ASTRA 捏人");w.minSize=new Vector2(1050,690);
        w.package=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolBuild.PackagePath);
        w.ReleaseCharacter();w.character=value;w.section=0;w.zoom=value.species==ToolSpecies.StandingAlien?2.75f:2.35f;w.notice="";w.ResetPreview();w.Show();
    }
    void OnEnable(){EditorApplication.update+=Tick;if(!package)package=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolBuild.PackagePath);}
    void OnDisable(){EditorApplication.update-=Tick;DisposePreview();ReleaseCharacter();}
    void ReleaseCharacter(){if(character&&!AssetDatabase.Contains(character))DestroyImmediate(character);character=null;}
    void Tick(){if(character)Repaint();}
    void OnGUI()
    {
        if(!character){GUILayout.Label("从演出时间轴选择「新建角色 → 人类 / 外星人」。",EditorStyles.boldLabel);return;}
        Rect top=new Rect(12,10,position.width-24,53),left=new Rect(12,70,440,position.height-140),right=new Rect(464,70,position.width-476,position.height-140),footer=new Rect(12,position.height-61,position.width-24,49);
        GUI.Box(top,GUIContent.none);GUI.Box(left,GUIContent.none);GUI.Box(right,GUIContent.none);GUI.Box(footer,GUIContent.none);
        GUILayout.BeginArea(top);
        GUILayout.BeginHorizontal();GUILayout.Label(AssetDatabase.Contains(character)?"ASTRA  /  编辑角色":"ASTRA  /  新建角色",EditorStyles.boldLabel,GUILayout.Width(200));
        GUILayout.Label(character.species==ToolSpecies.Human?"人类  ·  V3 骨架":"外星人  ·  V3 骨架",EditorStyles.miniBoldLabel);
        GUILayout.FlexibleSpace();GUILayout.Label("所有部件沿用 V2 的 17 种动作",EditorStyles.miniLabel);GUILayout.EndHorizontal();
        GUILayout.EndArea();
        GUILayout.BeginArea(left);
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(102));
        GUILayout.Space(10);
        for(int i=0;i<Sections.Length;i++)
        {
            var old=GUI.backgroundColor;
            if(i==section)GUI.backgroundColor=new Color(.67f,.81f,.57f);
            if(GUILayout.Button(Sections[i],GUILayout.Height(43)))
            {
                section=i;scroll=Vector2.zero;
            }
            GUI.backgroundColor=old;
        }
        GUILayout.FlexibleSpace();
        GUILayout.Label("类别 → 子项 → 样式",EditorStyles.wordWrappedMiniLabel);
        GUILayout.EndVertical();
        GUILayout.BeginVertical();
        GUILayout.Space(10);
        GUILayout.Label("分类 / "+Sections[section],EditorStyles.boldLabel);
        int previousSubsection=subsection[section];
        subsection[section]=GUILayout.Toolbar(subsection[section],Subsections[section],GUILayout.Height(32));
        if(previousSubsection!=subsection[section])scroll=Vector2.zero;
        GUILayout.Label(Sections[section]+"  ›  "+Subsections[section][subsection[section]],EditorStyles.miniBoldLabel);
        scroll=GUILayout.BeginScrollView(scroll);
        EditorGUI.BeginChangeCheck();
        Undo.RecordObject(character,"Edit ASTRA character");
        DrawCurrentOption(character.look);
        if(EditorGUI.EndChangeCheck()){EditorUtility.SetDirty(character);ResetPreview();}
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        GUILayout.BeginArea(right);
        Rect picture=new Rect(9,9,right.width-18,right.height-83);
        DrawPreview(picture);
        GUILayout.BeginArea(new Rect(12,right.height-70,right.width-24,56));
        GUILayout.BeginHorizontal();
        GUILayout.Label("动作试演",GUILayout.Width(65));
        var actions=package.library.actions;
        actionIndex=EditorGUILayout.Popup(actionIndex,actions.Select(a=>a.displayName).ToArray(),GUILayout.Width(130));
        GUILayout.Label("旋转",GUILayout.Width(35));yaw=GUILayout.HorizontalSlider(yaw,-65,65,GUILayout.Width(105));
        GUILayout.Label("缩放",GUILayout.Width(35));zoom=GUILayout.HorizontalSlider(zoom,1.5f,3.8f,GUILayout.Width(95));
        GUILayout.EndHorizontal();GUILayout.Label("拖动滑块检查侧面；动作循环预览会显示所有部件的骨骼跟随。",EditorStyles.miniLabel);
        GUILayout.EndArea();GUILayout.EndArea();
        GUILayout.BeginArea(footer);GUILayout.BeginHorizontal();
        if(GUILayout.Button("随机组合",GUILayout.Width(110),GUILayout.Height(32))){Undo.RecordObject(character,"Randomize ASTRA character");ToolCreatorOptions.Randomize(character.look,character.species,new System.Random());EditorUtility.SetDirty(character);ResetPreview();}
        GUILayout.Space(9);GUILayout.Label(notice,EditorStyles.wordWrappedMiniLabel);GUILayout.FlexibleSpace();
        if(GUILayout.Button("保存角色与 Prefab",GUILayout.Width(160),GUILayout.Height(32)))
        {
            try{ToolCreatorBuild.SaveCharacter(character,package);notice="已保存到 Characters，现可拖到时间轴。";EditorGUIUtility.PingObject(character);}
            catch(Exception e){notice=e.Message;EditorUtility.DisplayDialog("保存失败",e.Message,"确定");}
        }
        GUILayout.EndHorizontal();GUILayout.EndArea();
    }
    void DrawCurrentOption(ToolCharacterLook look)
    {
        int sub=subsection[section];
        GUILayout.Space(9);
        if(section==0)
        {
            if(sub==0)
            {
                character.displayName=EditorGUILayout.TextField("名字",character.displayName);
                character.id=EditorGUILayout.TextField("唯一 ID",character.id);
                EditorGUILayout.HelpBox("先设置身份，再按左侧分类逐项挑选；右侧可旋转并试演动作。",MessageType.Info);
            }
            else
            {
                int old=ToolCreatorBuild.BaseIndex(character,package);
                int chosen=OptionGrid(old,character.species==ToolSpecies.Human?ToolCreatorOptions.HumanBases:ToolCreatorOptions.AlienBases);
                if(chosen!=old)ToolCreatorBuild.SetBase(character,package,chosen);
                EditorGUILayout.HelpBox("V3 底模决定原始骨架和蒙皮。其它选项只添加跟随骨骼的低模部件。",MessageType.Info);
            }
        }
        if(section==1)
        {
            if(sub==0){look.faceShape=OptionGrid(look.faceShape,ToolCreatorOptions.Faces(character.species));look.faceWidth=EditorGUILayout.Slider("脸宽微调",look.faceWidth,.85f,1.15f);}
            if(sub==1)look.eyeShape=OptionGrid(look.eyeShape,ToolCreatorOptions.Eyes(character.species));
            if(sub==2)look.browShape=OptionGrid(look.browShape,ToolCreatorOptions.Brows);
            if(sub==3)look.noseShape=OptionGrid(look.noseShape,ToolCreatorOptions.Noses);
            if(sub==4)look.mouthShape=OptionGrid(look.mouthShape,ToolCreatorOptions.Mouths);
        }
        if(section==2)look.hairStyle=OptionGrid(look.hairStyle,ToolCreatorOptions.Hair(character.species));
        if(section==3)
        {
            if(sub==0)look.bodyType=OptionGrid(look.bodyType,ToolCreatorOptions.Bodies);
            else look.bodyScale=EditorGUILayout.Slider("整体身高",look.bodyScale,.85f,1.15f);
        }
        if(section==4)
        {
            if(sub==0)look.outfitStyle=OptionGrid(look.outfitStyle,ToolCreatorOptions.Outfits);
            if(sub==1)look.faceAccessory=OptionGrid(look.faceAccessory,ToolCreatorOptions.FaceAccessories);
            if(sub==2)look.gearAccessory=OptionGrid(look.gearAccessory,ToolCreatorOptions.GearAccessories);
            if(sub==3)look.facialMark=OptionGrid(look.facialMark,ToolCreatorOptions.Marks);
        }
        if(section==5)
        {
            if(sub==0)Palette(ref look.skin,ToolCreatorOptions.Skin(character.species));
            if(sub==1)Palette(ref look.hair,ToolCreatorOptions.HairColors);
            if(sub==2)Palette(ref look.suit,ToolCreatorOptions.SuitColors);
            if(sub==3)Palette(ref look.eyes,ToolCreatorOptions.EyeColors);
            if(sub==4)Palette(ref look.accent,ToolCreatorOptions.AccentColors);
        }
    }
    static int OptionGrid(int selected,string[] options)
    {
        for(int row=0;row<(options.Length+1)/2;row++)
        {
            GUILayout.BeginHorizontal();
            for(int col=0;col<2;col++)
            {
                int i=row*2+col;if(i>=options.Length)break;
                var old=GUI.backgroundColor;
                if(i==selected)GUI.backgroundColor=new Color(.67f,.81f,.57f);
                if(GUILayout.Button(options[i],GUILayout.MinWidth(132),GUILayout.Height(36))){selected=i;GUI.changed=true;}
                GUI.backgroundColor=old;
            }
            GUILayout.EndHorizontal();
        }
        return selected;
    }
    static void Palette(ref Color value,Color[] options)
    {
        for(int row=0;row<(options.Length+3)/4;row++)
        {
            GUILayout.BeginHorizontal();
            for(int col=0;col<4;col++)
            {
                int i=row*4+col;if(i>=options.Length)break;
                Rect r=GUILayoutUtility.GetRect(62,39,GUILayout.Width(62));
                if(GUI.Button(r,GUIContent.none)){value=options[i];GUI.changed=true;}
                EditorGUI.DrawRect(new Rect(r.x+3,r.y+3,r.width-6,r.height-6),value==options[i]?new Color(.94f,.83f,.53f):new Color(.12f,.16f,.14f));
                EditorGUI.DrawRect(new Rect(r.x+6,r.y+6,r.width-12,r.height-12),options[i]);
                if(value==options[i])
                {
                    var style=new GUIStyle(EditorStyles.boldLabel){alignment=TextAnchor.MiddleCenter};
                    style.normal.textColor=(options[i].r*.299f+options[i].g*.587f+options[i].b*.114f)>.45f?Color.black:Color.white;
                    GUI.Label(new Rect(r.x+6,r.y+6,r.width-12,r.height-12),"✓",style);
                }
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.Space(9);
        value=EditorGUILayout.ColorField("自定义颜色",value);
    }
    void DrawPreview(Rect rect)
    {
        if(Event.current.type!=EventType.Repaint){GUI.Box(rect,GUIContent.none);return;}
        try
        {
            if(preview==null||!actor)CreatePreview();
            var action=package.library.actions[Mathf.Clamp(actionIndex,0,package.library.actions.Length-1)];
            actor.GetComponent<ToolMotion>().Sample(action.id,(float)(EditorApplication.timeSinceStartup%8),1);
            stage.Set(package.library.scenes[0],ToolFx.None,0);
            float radians=yaw*Mathf.Deg2Rad;
            var target=new Vector3(0,character.species==ToolSpecies.StandingAlien?1.48f:1.37f,0);
            preview.camera.transform.position=new Vector3(Mathf.Sin(radians)*zoom,target.y+.12f,-Mathf.Cos(radians)*zoom);
            preview.camera.transform.LookAt(target);
            preview.camera.fieldOfView=32;preview.camera.clearFlags=CameraClearFlags.SolidColor;
            preview.camera.backgroundColor=new Color(.035f,.054f,.046f);
            preview.BeginPreview(rect,GUIStyle.none);preview.camera.Render();var texture=preview.EndPreview();
            GUI.DrawTexture(rect,texture,ScaleMode.StretchToFill,false);
            EditorGUI.DrawRect(new Rect(rect.x+8,rect.y+8,Mathf.Min(rect.width-16,310),28),new Color(.025f,.04f,.032f,.92f));
            GUI.Label(new Rect(rect.x+12,rect.y+12,rect.width-24,22),character.displayName+"  /  "+action.displayName,EditorStyles.whiteLabel);
        }
        catch(Exception ex){GUI.Box(rect,ex.Message);}
    }
    void CreatePreview()
    {
        DisposePreview();preview=new PreviewRenderUtility();preview.camera.nearClipPlane=.03f;preview.camera.farClipPlane=30;
        stageObject=new GameObject("Creator preview stage");stage=stageObject.AddComponent<ToolStage>();stage.Build();preview.AddSingleGO(stageObject);
        actor=ToolCharacterView.Create(character,null);preview.AddSingleGO(actor);
        preview.lights[0].intensity=1.1f;preview.lights[0].transform.rotation=Quaternion.Euler(45,30,0);
    }
    void ResetPreview(){DisposePreview();Repaint();}
    void DisposePreview()
    {
        if(preview!=null){preview.Cleanup();preview=null;}
        if(actor)DestroyImmediate(actor);if(stageObject)DestroyImmediate(stageObject);
        actor=null;stageObject=null;stage=null;
    }
}

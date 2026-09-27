using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceToolV2;

public sealed class ToolV2CharacterCreatorWindow : EditorWindow
{
    ToolPackage package;
    [SerializeField] ToolCharacter character;
    ToolCharacter original;
    bool faceView=true;
    PreviewRenderUtility preview;
    GameObject actor,stageObject;
    ToolStage stage;
    Vector2 scroll;
    int section,actionIndex,outfitGroup;
    int outfitPart=-1;
    readonly int[] subsection=new int[6];
    float yaw,zoom=2.35f;
    string notice;
    static readonly string[] Sections={"身份","面容","头部","身形","装扮","配色"};
    static readonly string[][] Subsections={
        new[]{"基本资料"},
        new[]{"脸型","眼睛","眉骨","鼻部","口部","下巴"},
        new[]{"发型 / 颅冠"},
        new[]{"体型","整体比例"},
        new[]{"上装","下装","鞋靴","面饰","装备","纹样"},
        new[]{"肤色","发色","服装色","眼色","强调色"}
    };
    [MenuItem("Astra Performance Tool V2/New character/Human/Male")]
    public static void NewHuman()=>OpenNew(ToolSpecies.Human);
    [MenuItem("Astra Performance Tool V2/New character/Human/Female")]
    public static void NewFemale()=>OpenNew(ToolSpecies.Human,1);
    [MenuItem("Astra Performance Tool V2/New character/Alien")]
    public static void NewAlien()=>OpenNew(ToolSpecies.StandingAlien);
    public static void OpenNew(ToolSpecies species,int humanModel=0)
    {
        var w=GetWindow<ToolV2CharacterCreatorWindow>("ASTRA 捏人");w.minSize=new Vector2(1050,690);
        w.package=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolV2Build.PackagePath);
        if(!w.package)w.package=ToolV2Build.CreateSample();
        w.ReleaseCharacter();w.original=null;w.character=ToolV2CreatorBuild.NewCharacter(w.package,species,humanModel);w.section=0;w.outfitPart=-1;w.zoom=1.12f;w.notice="关闭窗口前请保存草稿。";w.ResetPreview();w.Show();w.EnsureVisible();
    }
    public static void OpenEdit(ToolCharacter value)
    {
        var w=GetWindow<ToolV2CharacterCreatorWindow>("ASTRA 捏人");w.minSize=new Vector2(1050,690);
        w.package=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolV2Build.PackagePath);
        w.ReleaseCharacter();w.original=value;w.character=Instantiate(value);w.section=0;w.outfitPart=-1;w.zoom=value.species==ToolSpecies.StandingAlien?2.75f:2.35f;w.notice="关闭窗口前请保存草稿。";w.ResetPreview();w.Show();w.EnsureVisible();
    }
    void EnsureVisible()
    {
        float width=Screen.currentResolution.width>0?Screen.currentResolution.width:1920,height=Screen.currentResolution.height>0?Screen.currentResolution.height:1080;
        if(position.x<0||position.y<0||position.xMax>width||position.yMax>height)
        {float w=Mathf.Min(1240,width-40),h=Mathf.Min(800,height-90);position=new Rect(Mathf.Max(10,(width-w)/2),Mathf.Max(40,(height-h)/2),w,h);}
    }
    void OnEnable(){EditorApplication.update+=Tick;Undo.undoRedoPerformed+=ResetPreview;if(!package)package=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolV2Build.PackagePath);}
    void OnDisable(){EditorApplication.update-=Tick;Undo.undoRedoPerformed-=ResetPreview;DisposePreview();ReleaseCharacter();}
    void ReleaseCharacter(){if(character&&!AssetDatabase.Contains(character))DestroyImmediate(character);character=null;}
    void Tick(){if(character)Repaint();}
    void OnGUI()
    {
        if(!character){GUILayout.Label("从演出时间轴选择「新建角色 → 人类 / 外星人」。",EditorStyles.boldLabel);return;}
        Rect top=new Rect(12,10,position.width-24,53),left=new Rect(12,70,440,position.height-140),right=new Rect(464,70,position.width-476,position.height-140),footer=new Rect(12,position.height-61,position.width-24,49);
        GUI.Box(top,GUIContent.none);GUI.Box(left,GUIContent.none);GUI.Box(right,GUIContent.none);GUI.Box(footer,GUIContent.none);
        GUILayout.BeginArea(top);
        GUILayout.BeginHorizontal();GUILayout.Label(original?"ASTRA  /  编辑角色草稿":"ASTRA  /  新建角色草稿",EditorStyles.boldLabel,GUILayout.Width(200));
        GUILayout.Label(character.species==ToolSpecies.Human?"人类  ·  Quaternius 骨架":"外星人  ·  Quaternius 骨架",EditorStyles.miniBoldLabel);
        GUILayout.FlexibleSpace();GUILayout.Label("原始蒙皮 · Humanoid · UAL 动作",EditorStyles.miniLabel);GUILayout.EndHorizontal();
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
                section=i;outfitPart=-1;if(section==2)subsection[2]=ToolCreatorOptions.HairCategory(character.species,character.look.hairStyle,character.look);scroll=Vector2.zero;faceView=section<3||section==5;
            }
            GUI.backgroundColor=old;
        }
        GUILayout.FlexibleSpace();
        GUILayout.Label("类别 → 子项 → 样式",EditorStyles.wordWrappedMiniLabel);
        GUILayout.EndVertical();
        GUILayout.BeginVertical();
        GUILayout.Space(10);
        GUILayout.Label("分类 / "+Sections[section],EditorStyles.boldLabel);
        var optionSections=section==2?ToolCreatorOptions.HairCategories(character.species,character.look):section==5?ToolCreatorOptions.ColorSections(character.species):section==1&&ToolCreatorOptions.Alien(character.species)?new[]{"生物头型","眼睛","眉骨","嗅觉","口器","下巴"}:Subsections[section];
        int previousSubsection=subsection[section];
        subsection[section]=GUILayout.SelectionGrid(subsection[section],optionSections,3,GUILayout.Height(optionSections.Length>3?64:32));
        if(previousSubsection!=subsection[section]){scroll=Vector2.zero;if(section==4)faceView=subsection[section]==3||subsection[section]==5;if(section==5)faceView=subsection[section]!=2;}
        GUILayout.Label(Sections[section]+"  ›  "+optionSections[subsection[section]],EditorStyles.miniBoldLabel);
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
        GUILayout.Label("旋转",GUILayout.Width(35));yaw=GUILayout.HorizontalSlider(yaw,-180,180,GUILayout.Width(90));
        faceView=GUILayout.Toggle(faceView,"面部特写",GUILayout.Width(80));
        GUILayout.EndHorizontal();GUILayout.Label("拖动滑块检查侧面；动作循环预览会显示所有部件的骨骼跟随。",EditorStyles.miniLabel);
        GUILayout.EndArea();GUILayout.EndArea();
        GUILayout.BeginArea(footer);GUILayout.BeginHorizontal();
        if(GUILayout.Button("随机组合",GUILayout.Width(110),GUILayout.Height(32))){Undo.RecordObject(character,"Randomize ASTRA character");ToolCreatorOptions.Randomize(character.look,character.species,new System.Random());EditorUtility.SetDirty(character);ResetPreview();}
        GUILayout.Space(9);GUILayout.Label(notice,EditorStyles.wordWrappedMiniLabel);GUILayout.FlexibleSpace();
        if(GUILayout.Button("保存角色与 Prefab",GUILayout.Width(160),GUILayout.Height(32)))
        {
            try{original=ToolV2CreatorBuild.SaveCharacter(character,package,original);notice="已保存到 Characters，现可拖到时间轴。";EditorGUIUtility.PingObject(character);}
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
                if(character.species==ToolSpecies.Human)
                {
                    GUILayout.Space(8);GUILayout.Label("人类底模",EditorStyles.boldLabel);
                    int model=GUILayout.Toolbar(look.humanModel,ToolCreatorOptions.HumanModels,GUILayout.Height(35));
                    if(model!=look.humanModel){ToolCreatorOptions.SetHumanModel(look,model);outfitPart=-1;GUI.changed=true;}
                }
                EditorGUILayout.HelpBox("先设置身份，再按左侧分类逐项挑选；右侧可旋转并试演动作。",MessageType.Info);
            }
            else
            {
                int old=ToolV2CreatorBuild.BaseIndex(character,package);
                int chosen=OptionGrid(old,character.species==ToolSpecies.Human?ToolCreatorOptions.HumanBases:ToolCreatorOptions.AlienBases);
                if(chosen!=old)ToolV2CreatorBuild.SetBase(character,package,chosen);
                EditorGUILayout.HelpBox("Quaternius 底模决定原始骨架和蒙皮。其它选项只添加跟随骨骼的低模部件。",MessageType.Info);
            }
        }
        if(section==1)
        {
            if(sub==0){look.faceShape=OptionGrid(look.faceShape,ToolCreatorOptions.Faces(character.species));look.faceWidth=EditorGUILayout.Slider("脸宽微调",look.faceWidth,.92f,1.08f);look.jawWidth=EditorGUILayout.Slider("下颌微调",look.jawWidth,-1,1);}
            if(sub==1)look.eyeShape=OptionGrid(look.eyeShape,ToolCreatorOptions.Eyes(character.species));
            if(sub==2){if(ToolCreatorOptions.HasBrows(character.species,look.faceShape))look.browShape=OptionGrid(look.browShape,ToolCreatorOptions.Brows);else EditorGUILayout.HelpBox("此生物头型没有独立眉骨，可调整眼形和颅冠。",MessageType.Info);}
            if(sub==3)look.noseShape=OptionGrid(look.noseShape,ToolCreatorOptions.Noses);
            if(sub==4)look.mouthShape=OptionGrid(look.mouthShape,ToolCreatorOptions.Mouths);
            if(sub==5)look.chinStyle=OptionGrid(look.chinStyle,ToolCreatorOptions.Chins);
        }
        if(section==2)
        {
            var ids=ToolCreatorOptions.HairIds(character.species,sub,character.look);
            int index=OptionGrid(Array.IndexOf(ids,ToolCreatorOptions.HairOption(look,character.species,sub)),ids.Select(id=>ToolCreatorOptions.HairOptionName(character.species,sub,id,character.look)).ToArray());
            if(index>=0)ToolCreatorOptions.SelectHair(look,character.species,sub,ids[index]);
        }
        if(section==3)
        {
            if(sub==0)look.bodyType=OptionGrid(look.bodyType,ToolCreatorOptions.Bodies);
            else look.bodyScale=EditorGUILayout.Slider("整体身高",look.bodyScale,.9f,1.1f);
        }
        if(section==4)
        {
            if(sub<3)
            {
                int current=ToolCreatorOptions.OutfitPart(look,sub);
                if(outfitPart!=sub){outfitPart=sub;outfitGroup=ToolCreatorOptions.OutfitCategory(current,character.look);}
                GUILayout.Label("用途分类",EditorStyles.miniBoldLabel);
                outfitGroup=OptionGrid(outfitGroup,ToolCreatorOptions.OutfitCategoriesFor(character.look));
                GUILayout.Space(10);var ids=ToolCreatorOptions.OutfitIds(outfitGroup,character.look);
                int chosen=OptionGrid(Array.IndexOf(ids,current),ids.Select(id=>ToolCreatorOptions.Outfits[id]).ToArray());
                if(chosen>=0){current=ids[chosen];ToolCreatorOptions.SetOutfitPart(look,sub,current);}
                GUILayout.Space(8);GUILayout.Label("当前部件："+ToolCreatorOptions.Outfits[current],EditorStyles.wordWrappedLabel);
                if(GUILayout.Button("整套应用：上装 / 下装 / 鞋靴",GUILayout.Height(34))){ToolCreatorOptions.ApplyOutfit(look,current);GUI.changed=true;}
                if(GUILayout.Button("使用推荐配色",GUILayout.Height(30))){ToolCreatorOptions.ApplyOutfitPalette(look,current);GUI.changed=true;}
            }
            if(sub==3){var ids=ToolCreatorOptions.FaceAccessoryIds(character.species,look.faceShape);int index=OptionGrid(Array.IndexOf(ids,look.faceAccessory),ids.Select(id=>ToolCreatorOptions.FaceAccessories[id]).ToArray());if(index>=0)look.faceAccessory=ids[index];if(ToolCreatorOptions.Exotic(character.species,look.faceShape))EditorGUILayout.HelpBox("异形五官暂支持护颈；眼镜和呼吸器适用于灰裔原型。",MessageType.Info);}
            if(sub==4)look.gearAccessory=OptionGrid(look.gearAccessory,ToolCreatorOptions.GearAccessories);
            if(sub==5){if(ToolCreatorOptions.Exotic(character.species,look.faceShape))EditorGUILayout.HelpBox("此原型使用自身鳃纹、甲板或花瓣，通过强调色修改。",MessageType.Info);else look.facialMark=OptionGrid(look.facialMark,ToolCreatorOptions.Marks);}
        }
        if(section==5)
        {
            if(sub==0)Palette(ref look.skin,ToolCreatorOptions.Skin(character.species));
            if(sub==1){Palette(ref look.hair,ToolCreatorOptions.HeadColors(character.species));if(ToolCreatorOptions.Alien(character.species))EditorGUILayout.HelpBox("颅冠骨架使用基色；鳍膜、晶簇与花瓣的局部颜色在强调色中调整。",MessageType.Info);}
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
            stage.SetPose(action.id,actor.transform.position,character.look.bodyScale);
            stage.Set(package.library.scenes[0],ToolFx.None,0);
            float radians=yaw*Mathf.Deg2Rad;
            var head=actor.GetComponent<ToolMotion>().Animator.GetBoneTransform(HumanBodyBones.Head);
            bool alien=ToolCreatorOptions.Alien(character.species);
            var target=faceView?head.position+Vector3.up*(alien?.18f:.075f)*character.look.bodyScale:new Vector3(0,.98f*character.look.bodyScale,0);
            zoom=faceView?(alien?1.12f:.98f):4.25f;
            preview.camera.transform.position=new Vector3(Mathf.Sin(radians)*zoom,target.y+.04f,-Mathf.Cos(radians)*zoom);
            preview.camera.transform.LookAt(target);
            preview.camera.fieldOfView=32;preview.camera.clearFlags=CameraClearFlags.SolidColor;
            preview.camera.aspect=rect.width/rect.height;if(faceView)ToolPortraitFraming.Apply(preview.camera,actor,yaw);
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
        stageObject=new GameObject("Creator preview stage");stage=stageObject.AddComponent<ToolStage>();stage.Build(true);preview.AddSingleGO(stageObject);
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

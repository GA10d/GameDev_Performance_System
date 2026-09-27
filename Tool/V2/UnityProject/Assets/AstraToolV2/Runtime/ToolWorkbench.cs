using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
namespace Astra.PerformanceToolV2
{
    public sealed class ToolWorkbench:MonoBehaviour
    {
        public ToolPackage package;public Camera lens;public ToolPlayer player;
        public ToolCharacter Draft=>draft;
        public GameObject Actor=>actor;
        public int Section {get=>section;set {section=value;sub=section==2&&draft?ToolCreatorOptions.HairCategory(draft.species,draft.look.hairStyle,draft.look):0;scroll=Vector2.zero;SetViewForOption();}}
        public int Subsection {get=>sub;set {sub=value;scroll=Vector2.zero;SetViewForOption();}}
        public int ActionIndex {get=>actionIndex;set {actionIndex=value;actionTime=0;}}
        public bool FaceView=true,Paused;
        public int OutfitGroup;
        public float Yaw;
        public float ActionTime=>actionTime;
        public static readonly string[] Sections={"身份","面容","头部","身形","装扮","配色"};
        public static readonly string[][] Subsections={new[]{"基本资料","预设角色"},new[]{"脸型","眼睛","眉骨","鼻部","口部","下巴"},new[]{"发型 / 颅冠"},new[]{"体型","整体比例"},new[]{"上装","下装","鞋靴","面饰","装备","纹样"},new[]{"肤色","发色","服装色","眼色","强调色"}};
        string[] CurrentSubsections=>section==2?ToolCreatorOptions.HairCategories(draft.species,draft.look):section==5?ToolCreatorOptions.ColorSections(draft.species):section==1&&ToolCreatorOptions.Alien(draft.species)?new[]{"生物头型","眼睛","眉骨","嗅觉器官","口器","下巴"}:Subsections[section];
        ToolCharacter draft;GameObject actor;ToolStage stage;RenderTexture texture;Camera uiCamera;
        Font font;int section,sub,actionIndex,presetIndex;Vector2 scroll;float actionTime;
        bool showPresets,showActions,performing,showHumanChoices;string notice="先选择类别，再挑选样式。右侧可试演动作并检查侧面。";
        GUIStyle label,small,title,button,active,field;
        readonly Color ink=new Color(.9f,.92f,.84f),muted=new Color(.64f,.72f,.65f),panel=new Color(.075f,.10f,.088f),selected=new Color(.24f,.36f,.27f);
        void SetViewForOption(){if(section==4&&sub<3&&draft)OutfitGroup=ToolCreatorOptions.OutfitCategory(ToolCreatorOptions.OutfitPart(draft.look,sub),draft.look);FaceView=section<3||section==4&&(sub==3||sub==5)||section==5&&sub!=2;}
        void Start()
        {
            font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},20);
            RenderSettings.ambientLight=new Color(.47f,.51f,.47f);
            Application.runInBackground=true;
            Application.targetFrameRate=60;lens.clearFlags=CameraClearFlags.SolidColor;lens.backgroundColor=new Color(.035f,.055f,.045f);
            uiCamera=new GameObject("Workbench UI camera").AddComponent<Camera>();uiCamera.cullingMask=0;uiCamera.clearFlags=CameraClearFlags.SolidColor;uiCamera.backgroundColor=Color.black;uiCamera.depth=10;
            stage=new GameObject("Workbench stage").AddComponent<ToolStage>();stage.Build(true);stage.Set(package.library.scenes[0],ToolFx.None,0);
            texture=new RenderTexture(1000,1000,24,RenderTextureFormat.ARGB32);texture.Create();lens.targetTexture=texture;lens.nearClipPlane=.03f;lens.farClipPlane=40;
            lens.GetComponent<ToolScreenFx>().enabled=false;NewCharacter(ToolSpecies.Human);
            if(Environment.GetCommandLineArgs().Contains("-outfitQA"))StartCoroutine(ToolOutfitRuntimeQA.Run(this));
            if(Environment.GetCommandLineArgs().Contains("-faceQA"))StartCoroutine(ToolFaceRuntimeQA.Run(this));
            if(Environment.GetCommandLineArgs().Contains("-chinQA"))StartCoroutine(ToolChinRuntimeQA.Run(this));
            if(Environment.GetCommandLineArgs().Contains("-womenQA"))StartCoroutine(ToolWomenRuntimeQA.Run(this));
            if(Environment.GetCommandLineArgs().Contains("-alienQA"))StartCoroutine(ToolAlienRuntimeQA.Run(this));
            if(Environment.GetCommandLineArgs().Contains("-hairQA"))StartCoroutine(ToolHairRuntimeQA.Run(this));
            if(Environment.GetCommandLineArgs().Contains("-toolQA"))StartCoroutine(ToolRuntimeQA.Run(this,package));
        }
        public void NewCharacter(ToolSpecies species,int humanModel=0)
        {
            if(draft)Destroy(draft);draft=ScriptableObject.CreateInstance<ToolCharacter>();draft.catalog=package.library.characters[0].catalog;draft.sourceModel=draft.catalog.modularModel;
            draft.species=species;draft.id="custom_"+Guid.NewGuid().ToString("N").Substring(0,12);draft.displayName=ToolCreatorOptions.Alien(species)?"新的外星人":"新的人类";
            draft.look.creatorEnabled=true;draft.look.hairStyle=ToolCreatorOptions.Alien(species)?0:1;draft.look.skin=ToolCreatorOptions.Skin(species)[0];draft.look.eyes=new Color(.025f,.04f,.03f);
            if(ToolCreatorOptions.Alien(species))draft.look.hair=draft.look.skin;
            if(species==ToolSpecies.Human){ToolCreatorOptions.SetHumanModel(draft.look,humanModel);draft.displayName=humanModel==1?"新的女性角色":"新的男性角色";}
            showHumanChoices=false;Section=0;Yaw=0;actionIndex=0;Rebuild();
        }
        public void LoadPreset(ToolCharacter character)
        {
            if(draft)Destroy(draft);draft=Instantiate(character);draft.id="custom_"+Guid.NewGuid().ToString("N").Substring(0,12);draft.displayName=character.displayName;actionIndex=0;Rebuild();
        }
        public void Rebuild()
        {if(actor){actor.SetActive(false);Destroy(actor);}actor=ToolCharacterView.Create(draft,stage.transform);actionTime=0;}
        public void RefreshLook(){ToolCharacterView.ApplyLook(actor,draft);}
        public void SampleAt(float time){actionTime=time;RenderActor();}
        void Update()
        {
            if(performing){if(Input.GetKeyDown(KeyCode.Escape))ExitPerformance();return;}
            if(!actor)return;if(!Paused)actionTime+=Time.deltaTime;RenderActor();
        }
        void RenderActor()
        {
            actor.GetComponent<ToolMotion>().Sample(package.library.actions[actionIndex].id,actionTime);
            stage.SetPose(package.library.actions[actionIndex].id,actor.transform.position,draft.look.bodyScale);
            var head=actor.GetComponent<ToolMotion>().Animator.GetBoneTransform(HumanBodyBones.Head);
            bool alien=ToolCreatorOptions.Alien(draft.species);
            Vector3 target=FaceView?head.position+Vector3.up*(alien?.18f:.075f)*draft.look.bodyScale:new Vector3(0,.98f*draft.look.bodyScale,0);
            float distance=FaceView?(alien?1.12f:.98f):3.95f,radians=Yaw*Mathf.Deg2Rad;
            lens.transform.position=target+new Vector3(Mathf.Sin(radians)*distance,.03f,-Mathf.Cos(radians)*distance);lens.transform.LookAt(target);lens.fieldOfView=32;
            if(FaceView)ToolPortraitFraming.Apply(lens,actor,Yaw);
        }
        void Styles()
        {
            label=new GUIStyle(GUI.skin.label){font=font,fontSize=19,alignment=TextAnchor.MiddleLeft,wordWrap=true};label.normal.textColor=ink;
            small=new GUIStyle(label){fontSize=15};small.normal.textColor=muted;
            title=new GUIStyle(label){fontSize=25,fontStyle=FontStyle.Bold};
            button=new GUIStyle(label){alignment=TextAnchor.MiddleCenter,fontSize=17};active=new GUIStyle(button){fontStyle=FontStyle.Bold};
            field=new GUIStyle(GUI.skin.textField){font=font,fontSize=18,padding=new RectOffset(10,8,9,8)};field.normal.textColor=ink;field.focused.textColor=ink;
        }
        void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        bool Button(Rect r,string text,bool on=false)
        {
            Fill(r,on?selected:new Color(.14f,.18f,.155f));
            if(on)Fill(new Rect(r.x,r.y,3,r.height),new Color(.7f,.82f,.5f));
            bool click=GUI.Button(r,GUIContent.none,GUIStyle.none);GUI.Label(r,text,on?active:button);return click;
        }
        void OnGUI()
        {
            if(!draft)return;if(label==null)Styles();
            var matrix=GUI.matrix;float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1440*scale)*.5f,(Screen.height-900*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);
            if(showHumanChoices&&Event.current.type==EventType.MouseDown&&!new Rect(873,24,159,146).Contains(Event.current.mousePosition))showHumanChoices=false;
            if(performing){if(Button(new Rect(20,15,190,40),"← 返回捏人 / Esc"))ExitPerformance();GUI.matrix=matrix;return;}
            Fill(new Rect(0,0,1440,900),new Color(.035f,.05f,.042f));
            GUI.Label(new Rect(25,15,620,40),"ASTRA  /  角色工作台  V2",title);
            GUI.Label(new Rect(25,57,600,28),"Quaternius  ·  开源蒙皮模型  /  UAL 动作试演",small);
            if(Button(new Rect(875,24,155,40),"＋ 新建人类 ▾"))showHumanChoices=!showHumanChoices;
            if(Button(new Rect(1040,24,165,40),"＋ 新建外星人"))NewCharacter(ToolSpecies.StandingAlien);
            if(Button(new Rect(1215,24,200,40),"演出试播  →"))EnterPerformance();
            Fill(new Rect(20,100,590,705),panel);Fill(new Rect(625,100,795,705),panel);
            for(int i=0;i<Sections.Length;i++)if(Button(new Rect(32,114+i*65,104,54),Sections[i],section==i))Section=i;
            GUI.Label(new Rect(34,675,102,112),"类别\n↓ 子项\n↓ 样式",small);
            GUI.Label(new Rect(154,112,435,36),Sections[section]+"  /  "+CurrentSubsections[sub],title);
            for(int i=0;i<CurrentSubsections.Length;i++)
            {
                int cols=3;float w=136;
                if(Button(new Rect(156+(i%cols)*(w+6),162+(i/cols)*42,w,35),CurrentSubsections[i],sub==i))Subsection=i;
            }
            float top=CurrentSubsections.Length>3?257:215;
            var area=new Rect(156,top,430,780-top);
            float contentHeight=section==0?(sub==1?Mathf.Max(1,Presets.Length)*53:410):section==1&&sub==0?465:section==2?Mathf.CeilToInt(ToolCreatorOptions.HairIds(draft.species,sub,draft.look).Length/2f)*54+8:section==3&&sub==1?300:section==4&&sub<3?242+Mathf.CeilToInt(ToolCreatorOptions.OutfitIds(OutfitGroup,draft.look).Length/2f)*54:section==5?(sub==1&&ToolCreatorOptions.Alien(draft.species)?420:350):320;
            scroll=GUI.BeginScrollView(area,scroll,new Rect(0,0,406,contentHeight));
            bool changed=GUI.changed;GUI.changed=false;Options();bool edited=GUI.changed;GUI.changed=changed||edited;
            GUI.EndScrollView();if(edited)RefreshLook();
            GUI.DrawTexture(new Rect(638,113,768,585),texture,ScaleMode.ScaleToFit,false);
            Fill(new Rect(650,125,410,55),new Color(.035f,.06f,.045f,.97f));
            GUI.Label(new Rect(660,126,392,28),draft.displayName,label);
            GUI.Label(new Rect(660,153,392,24),(ToolCreatorOptions.Alien(draft.species)?"外星人":ToolCreatorOptions.Female(draft.look)?"人类 · 女性":"人类 · 男性")+"   /   "+package.library.actions[actionIndex].displayName,small);
            if(Button(new Rect(650,713,130,37),FaceView?"面部特写":"全身检查"))FaceView=!FaceView;
            if(Button(new Rect(790,713,88,37),Paused?"播放":"暂停"))Paused=!Paused;
            if(Button(new Rect(888,713,210,37),"动作 · "+package.library.actions[actionIndex].displayName))showActions=!showActions;
            GUI.Label(new Rect(1110,713,50,35),"转向",small);Yaw=GUI.HorizontalSlider(new Rect(1155,727,230,25),Yaw,-180,180);
            GUI.Label(new Rect(650,760,736,32),"角色与配饰共用骨架；可切换全身视角检查坐姿、手部和衣装。",small);
            Fill(new Rect(20,822,1400,58),panel);
            if(Button(new Rect(32,833,128,36),"随机组合")){ToolCreatorOptions.Randomize(draft.look,draft.species,new System.Random());RefreshLook();}
            GUI.Label(new Rect(178,829,870,43),notice,small);
            if(Button(new Rect(1055,833,150,36),"载入已存外观"))LoadLast();
            if(Button(new Rect(1218,833,190,36),"保存外观 JSON"))SaveLook();
            if(showActions)
            {
                Fill(new Rect(883,225,310,482),new Color(.04f,.07f,.05f));
                for(int i=0;i<package.library.actions.Length;i++)if(Button(new Rect(890+(i%2)*148,234+(i/2)*50,142,42),package.library.actions[i].displayName,actionIndex==i)){ActionIndex=i;showActions=false;}
            }
            if(showHumanChoices)
            {
                Fill(new Rect(873,68,159,102),new Color(.02f,.035f,.025f));
                if(Button(new Rect(878,73,149,43),"男性"))NewCharacter(ToolSpecies.Human,0);
                if(Button(new Rect(878,121,149,43),"女性"))NewCharacter(ToolSpecies.Human,1);
            }
            GUI.matrix=matrix;
        }
        ToolCharacter[] Presets=>package.library.characters.Where(c=>c&&c.look.creatorEnabled&&c.species==draft.species&&c.look.humanModel==draft.look.humanModel).ToArray();
        void Options()
        {
            var l=draft.look;
            if(section==0&&sub==0)
            {
                GUI.Label(new Rect(0,0,405,30),"角色名称",label);draft.displayName=GUI.TextField(new Rect(0,37,405,42),draft.displayName,64,field);
                if(draft.species==ToolSpecies.Human)
                {
                    GUI.Label(new Rect(0,90,405,26),"人类底模",small);
                    for(int i=0;i<2;i++)if(Button(new Rect(i*207,122,198,40),ToolCreatorOptions.HumanModels[i],l.humanModel==i))
                    {ToolCreatorOptions.SetHumanModel(l,i);GUI.changed=true;}
                }
                GUI.Label(new Rect(0,180,405,140),"这里制作外观并试演动作。\n在 Unity 的捏人编辑器中保存角色与 Prefab 后，可直接拖入演出时间轴。",label);
                if(Button(new Rect(0,340,405,45),"选择预制角色 →"))Subsection=1;
                return;
            }
            if(section==0&&sub==1)
            {
                var presets=Presets;
                for(int i=0;i<presets.Length;i++)if(Button(new Rect(0,i*53,405,45),presets[i].displayName)){LoadPreset(presets[i]);GUI.changed=true;}return;
            }
            if(section==1)
            {
                if(sub==0){l.faceShape=Grid(l.faceShape,ToolCreatorOptions.Faces(draft.species));GUI.Label(new Rect(0,300,405,30),"脸宽微调（受限范围）",small);l.faceWidth=GUI.HorizontalSlider(new Rect(0,340,400,25),l.faceWidth,.92f,1.08f);GUI.Label(new Rect(0,380,405,30),"下颌微调",small);l.jawWidth=GUI.HorizontalSlider(new Rect(0,420,400,25),l.jawWidth,-1,1);}
                if(sub==1)l.eyeShape=Grid(l.eyeShape,ToolCreatorOptions.Eyes(draft.species));if(sub==2){if(ToolCreatorOptions.HasBrows(draft.species,l.faceShape))l.browShape=Grid(l.browShape,ToolCreatorOptions.Brows);else GUI.Label(new Rect(0,0,405,90),"此生物头型没有独立眉骨，可调整眼形和颅冠。",label);}if(sub==3)l.noseShape=Grid(l.noseShape,ToolCreatorOptions.Noses);if(sub==4)l.mouthShape=Grid(l.mouthShape,ToolCreatorOptions.Mouths);if(sub==5)l.chinStyle=Grid(l.chinStyle,ToolCreatorOptions.Chins);
            }
            if(section==2)
            {
                var ids=ToolCreatorOptions.HairIds(draft.species,sub,draft.look);
                int index=Grid(Array.IndexOf(ids,ToolCreatorOptions.HairOption(l,draft.species,sub)),ids.Select(id=>ToolCreatorOptions.HairOptionName(draft.species,sub,id,draft.look)).ToArray());
                if(index>=0)ToolCreatorOptions.SelectHair(l,draft.species,sub,ids[index]);
            }
            if(section==3){if(sub==0)l.bodyType=Grid(l.bodyType,ToolCreatorOptions.Bodies);else{GUI.Label(new Rect(0,0,405,60),"整体身高  "+l.bodyScale.ToString("F2"),label);l.bodyScale=GUI.HorizontalSlider(new Rect(0,90,400,25),l.bodyScale,.9f,1.1f);GUI.Label(new Rect(0,150,405,130),"仅整体等比缩放。骨架比例不变，避免破坏 Humanoid 动作重定向。",small);}}
            if(section==4){if(sub<3)OutfitPicker(l);if(sub==3){var ids=ToolCreatorOptions.FaceAccessoryIds(draft.species,l.faceShape);int index=Grid(Array.IndexOf(ids,l.faceAccessory),ids.Select(id=>ToolCreatorOptions.FaceAccessories[id]).ToArray());if(index>=0)l.faceAccessory=ids[index];if(ToolCreatorOptions.Exotic(draft.species,l.faceShape))GUI.Label(new Rect(0,120,405,110),"异形五官暂支持护颈。眼镜与呼吸器适用于灰裔原型。",small);}if(sub==4)l.gearAccessory=Grid(l.gearAccessory,ToolCreatorOptions.GearAccessories);if(sub==5){if(ToolCreatorOptions.Exotic(draft.species,l.faceShape))GUI.Label(new Rect(0,0,405,95),"此原型使用自身的鳃纹、甲板或花瓣配色，可在强调色中修改。",label);else l.facialMark=Grid(l.facialMark,ToolCreatorOptions.Marks);}}
            if(section==5){if(sub==0)l.skin=Palette(l.skin,ToolCreatorOptions.Skin(draft.species));if(sub==1){l.hair=Palette(l.hair,ToolCreatorOptions.HeadColors(draft.species));if(ToolCreatorOptions.Alien(draft.species))GUI.Label(new Rect(0,336,398,76),"颅冠骨架使用基色；鳍膜、晶簇与花瓣的局部颜色在“强调色”中调整。",small);}if(sub==2)l.suit=Palette(l.suit,ToolCreatorOptions.SuitColors);if(sub==3)l.eyes=Palette(l.eyes,ToolCreatorOptions.EyeColors);if(sub==4)l.accent=Palette(l.accent,ToolCreatorOptions.AccentColors);}
        }
        void OutfitPicker(ToolCharacterLook look)
        {
            GUI.Label(new Rect(0,0,405,28),"用途分类",small);
            for(int i=0;i<ToolCreatorOptions.OutfitCategoriesFor(draft.look).Length;i++)
                if(Button(new Rect((i%2)*207,34+(i/2)*43,198,36),ToolCreatorOptions.OutfitCategoriesFor(draft.look)[i],OutfitGroup==i)){OutfitGroup=i;scroll=Vector2.zero;}
            int current=ToolCreatorOptions.OutfitPart(look,sub);var ids=ToolCreatorOptions.OutfitIds(OutfitGroup,draft.look);
            for(int i=0;i<ids.Length;i++)
                if(Button(new Rect((i%2)*207,130+(i/2)*54,198,45),ToolCreatorOptions.Outfits[ids[i]],current==ids[i])){current=ids[i];ToolCreatorOptions.SetOutfitPart(look,sub,current);GUI.changed=true;}
            float y=136+Mathf.CeilToInt(ids.Length/2f)*54;
            GUI.Label(new Rect(0,y,405,28),"当前部件："+ToolCreatorOptions.Outfits[current],small);
            if(Button(new Rect(0,y+38,198,42),"整套应用")){ToolCreatorOptions.ApplyOutfit(look,current);GUI.changed=true;notice="已应用 "+ToolCreatorOptions.Outfits[current]+" 的上装、下装与鞋靴。";}
            if(Button(new Rect(207,y+38,198,42),"使用推荐配色")){ToolCreatorOptions.ApplyOutfitPalette(look,current);GUI.changed=true;}
        }
        int Grid(int current,string[] options){for(int i=0;i<options.Length;i++)if(Button(new Rect((i%2)*207,(i/2)*54,198,45),options[i],current==i)){current=i;GUI.changed=true;}return current;}
        Color Palette(Color color,Color[] colors)
        {
            for(int i=0;i<colors.Length;i++){Rect r=new Rect((i%4)*103,(i/4)*84,92,72);bool on=(color-colors[i]).maxColorComponent<.002f&&(colors[i]-color).maxColorComponent<.002f;Fill(r,on?ink:new Color(.18f,.22f,.19f));Fill(new Rect(r.x+4,r.y+4,84,45),colors[i]);GUI.Label(new Rect(r.x+5,r.y+47,82,24),(on?"✓ ":"")+"色号 "+(i+1),small);if(GUI.Button(r,GUIContent.none,GUIStyle.none)){color=colors[i];GUI.changed=true;}}return color;
        }
        [Serializable] public class LookFile {public int version=2;public ToolSpecies species;public string displayName;public ToolCharacterLook look;}
        public string SaveLook()
        {
            string folder=Path.Combine(Application.persistentDataPath,"Characters");Directory.CreateDirectory(folder);string path=Path.Combine(folder,draft.id+".json");
            File.WriteAllText(path,JsonUtility.ToJson(new LookFile{species=draft.species,displayName=draft.displayName,look=draft.look},true));notice="已保存："+Path.GetFileName(path)+"（程序数据目录 / Characters）";return path;
        }
        public void LoadLook(string path)
        {
            var file=JsonUtility.FromJson<LookFile>(File.ReadAllText(path));if(file.version!=2||file.look==null||(file.species!=ToolSpecies.Human&&file.species!=ToolSpecies.StandingAlien))throw new InvalidDataException("仅支持 V2 人类 / 外星人外观文件");
            NewCharacter(file.species);draft.displayName=file.displayName;draft.look=file.look;Section=0;RefreshLook();notice="已载入外观";
        }
        void LoadLast(){try{string dir=Path.Combine(Application.persistentDataPath,"Characters");string file=Directory.Exists(dir)?Directory.GetFiles(dir,"*.json").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault():null;if(file==null){notice="还没有保存的外观。";return;}LoadLook(file);}catch(Exception e){notice=e.Message;}}
        public void EnterPerformance(){performing=true;uiCamera.enabled=false;stage.gameObject.SetActive(false);lens.targetTexture=null;player.enabled=true;player.Begin();}
        public void ExitPerformance(){performing=false;uiCamera.enabled=true;player.Stop();player.enabled=false;stage.gameObject.SetActive(true);lens.targetTexture=texture;lens.GetComponent<ToolScreenFx>().enabled=false;}
        void OnDestroy(){if(texture){texture.Release();Destroy(texture);}if(draft)Destroy(draft);if(font)Destroy(font);}
    }
}

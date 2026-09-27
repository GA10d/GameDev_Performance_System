using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceToolV2;

public sealed class ToolV2TimelineWindow : EditorWindow
{
    ToolPackage package;
    ToolUnit unit;
    ToolCharacter selectedCharacter;
    PreviewRenderUtility preview;
    GameObject previewActor,previewStage;
    ToolStage stage;
    string previewActorId;
    string previewContext;
    readonly Dictionary<string,GameObject> companionActors=new Dictionary<string,GameObject>();
    float playhead, pixelsPerSecond=88;
    bool playing;
    double lastTime;
    int tab,selectedTrack=-1,selectedIndex=-1,dragTrack=-1,dragIndex=-1;
    bool resizeClip;
    string hoveredAction;
    string pendingDragKind;
    object pendingDragValue;
    Vector2 libraryScroll,inspectorScroll,timelineScroll;
    string characterSearch="";
    readonly bool[] speciesOpen={true,true,false,false};
    readonly bool[,] tierOpen={{true,false,true},{true,false,true},{true,false,true},{true,false,true}};
    const float Left=205,Right=292,Row=42,LabelWidth=88;
    static readonly string[] Tabs={"人物","动作","台词","镜头","场景"};
    static readonly string[] Tracks={"人物","动作","台词","镜头","场景"};
    static readonly string[] SpeciesNames={"人类","直立外星人","半兽人","四足外星人"};
    static readonly string[] TierNames={"Demo V2 原角色","捏人预设","自建角色"};
    [MenuItem("Astra Performance Tool V2/Open timeline %#t")]
    public static void Open(){var w=GetWindow<ToolV2TimelineWindow>("ASTRA 演出时间轴");w.minSize=new Vector2(1100,720);w.Show();}
    void OnEnable()
    {
        wantsMouseMove=true;
        package=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolV2Build.PackagePath);
        if(package&&package.graph&&package.graph.nodes.Count>0)unit=package.graph.nodes.FirstOrDefault(n=>n.unit!=null)?.unit;
        EditorApplication.update+=Tick;
    }
    void OnDisable(){EditorApplication.update-=Tick;DisposePreview();}
    void OnFocus(){ResetPreview();}
    void Tick()
    {
        double now=EditorApplication.timeSinceStartup;
        if(playing&&unit){playhead+=Mathf.Clamp((float)(now-lastTime),0,.1f);if(playhead>unit.duration){playhead=0;playing=false;}Repaint();}
        else if(!string.IsNullOrEmpty(hoveredAction))Repaint();
        lastTime=now;
    }
    void OnGUI()
    {
        if(!package){if(GUILayout.Button("创建示例工程",GUILayout.Height(45))){package=ToolV2Build.CreateSample();unit=package.graph.nodes[0].unit;}return;}
        Toolbar();
        float upper=Mathf.Max(300,position.height-330);
        Rect lib=new Rect(0,32,Left,upper-32),viewer=new Rect(Left,32,position.width-Left-Right,upper-32),inspect=new Rect(position.width-Right,32,Right,upper-32);
        GUI.Box(lib,GUIContent.none);GUI.Box(viewer,GUIContent.none);GUI.Box(inspect,GUIContent.none);
        GUILayout.BeginArea(lib);Library();GUILayout.EndArea();
        GUILayout.BeginArea(viewer);DrawPreview(new Rect(8,8,viewer.width-16,viewer.height-40));
        EditorGUI.DrawRect(new Rect(8,viewer.height-32,viewer.width-16,24),new Color(.12f,.17f,.15f));
        GUI.Label(new Rect(16,viewer.height-29,viewer.width-32,20),"实时预览  ·  拖动下方时间线定位  ·  悬停动作可试演",EditorStyles.whiteLabel);GUILayout.EndArea();
        GUILayout.BeginArea(inspect);Inspector();GUILayout.EndArea();
        DrawTimeline(new Rect(0,upper,position.width,position.height-upper));
    }
    void Toolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        package=(ToolPackage)EditorGUILayout.ObjectField(package,typeof(ToolPackage),false,GUILayout.Width(230));
        if(package&&package.graph)
        {
            var units=package.graph.nodes.Where(n=>n.unit).Select(n=>n.unit).Distinct().ToArray();
            int current=Mathf.Max(0,Array.IndexOf(units,unit));
            int next=EditorGUILayout.Popup(current,units.Select(u=>u.id).ToArray(),GUILayout.Width(160));
            if(next>=0&&next<units.Length&&unit!=units[next]){unit=units[next];playhead=0;selectedIndex=-1;ResetPreview();}
        }
        if(GUILayout.Button(playing?"暂停":"播放",EditorStyles.toolbarButton,GUILayout.Width(55)))playing=!playing;
        if(GUILayout.Button("演出树",EditorStyles.toolbarButton,GUILayout.Width(65)))ToolV2GraphWindow.Open();
        if(GUILayout.Button("验证",EditorStyles.toolbarButton,GUILayout.Width(55)))ShowValidation();
        if(GUILayout.Button("导出演出单元",EditorStyles.toolbarButton,GUILayout.Width(96)))ExportUnit();
        if(GUILayout.Button("运行预览",EditorStyles.toolbarButton,GUILayout.Width(75)))OpenGamePreview();
        if(GUILayout.Button("导出 Unity 包",EditorStyles.toolbarButton,GUILayout.Width(100))){ToolV2Build.VerifyAndExport();EditorUtility.DisplayDialog("导出完成","Tool/Export/ASTRA_Performance_Tool_V2.unitypackage 已生成。","确定");}
        GUILayout.FlexibleSpace();GUILayout.Label(unit?playhead.ToString("F2")+" / "+unit.duration.ToString("F2")+" s":"",GUILayout.Width(100));
        GUILayout.EndHorizontal();
    }
    void Library()
    {
        hoveredAction=null;
        GUILayout.Label("素材库",EditorStyles.boldLabel);
        tab=GUILayout.Toolbar(tab,Tabs);
        if(tab==0)
        {
            if(GUILayout.Button("＋ 新建角色",GUILayout.Height(34)))
            {
                var menu=new GenericMenu();
                menu.AddItem(new GUIContent("人类/男性"),false,()=>ToolV2CharacterCreatorWindow.OpenNew(ToolSpecies.Human,0));
                menu.AddItem(new GUIContent("人类/女性"),false,()=>ToolV2CharacterCreatorWindow.OpenNew(ToolSpecies.Human,1));
                menu.AddItem(new GUIContent("外星人"),false,()=>ToolV2CharacterCreatorWindow.OpenNew(ToolSpecies.StandingAlien));
                menu.ShowAsContext();
            }
            GUILayout.BeginHorizontal();
            string nextSearch=GUILayout.TextField(characterSearch,GUI.skin.FindStyle("ToolbarSeachTextField")??EditorStyles.textField);
            if(GUILayout.Button("×",GUILayout.Width(25)))nextSearch="";
            if(nextSearch!=characterSearch){characterSearch=nextSearch;libraryScroll=Vector2.zero;selectedCharacter=null;ResetPreview();}
            GUILayout.EndHorizontal();
        }
        libraryScroll=GUILayout.BeginScrollView(libraryScroll);
        if(tab==0)
        {
            GUILayout.Space(5);
            int visible=0;
            for(int i=0;i<SpeciesNames.Length;i++)visible+=DrawSpecies((ToolSpecies)i);
            if(visible==0)GUILayout.Label("没有匹配的角色。",EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(9);GUILayout.Label("按物种展开，再展开原角色、预设或自建角色。拖拽角色到人物轨。",EditorStyles.wordWrappedMiniLabel);
        }
        if(tab==1)
        {
            foreach(var action in package.library.actions)
            {
                Rect r=GUILayoutUtility.GetRect(180,31);
                LibraryCard(r,new GUIContent(action.displayName+"  ▷",action.id+" · "+action.length.ToString("F1")+" 秒"));
                if(r.Contains(Event.current.mousePosition)) hoveredAction=action.id;
                StartDrag(r,"action",action.id);
            }
            if(Event.current.type==EventType.MouseMove&&!new Rect(0,50,Left,position.height).Contains(Event.current.mousePosition))hoveredAction=null;
        }
        if(tab==2)
        {
            DragButton("纯文本 · 打字机","text",null);
            DragButton("选择 · 含沉默","choice",null);
            GUILayout.Label("文本长度和显示速度决定片段时长。选择片段必须位于台词轨末尾。",EditorStyles.wordWrappedMiniLabel);
        }
        if(tab==3)
        {
            foreach(var c in package.library.cameras)DragButton(c.displayName+(c.moving?"  ⟿":""),"camera",c.shot);
            GUILayout.Space(8);GUILayout.Label("选中镜头后可调整坐标、视野和画面效果。",EditorStyles.wordWrappedMiniLabel);
        }
        if(tab==4)foreach(var scene in package.library.scenes)DragButton(scene.displayName,"scene",scene.id);
        GUILayout.EndScrollView();
        if(Event.current.type==EventType.MouseDrag&&pendingDragKind!=null)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData("AstraKind",pendingDragKind);
            DragAndDrop.SetGenericData("AstraValue",pendingDragValue);
            DragAndDrop.StartDrag("ASTRA " + pendingDragKind);
            pendingDragKind=null;pendingDragValue=null;Event.current.Use();
        }
        if(Event.current.type==EventType.MouseUp){pendingDragKind=null;pendingDragValue=null;}
    }
    int DrawSpecies(ToolSpecies species)
    {
        var all=package.library.characters.Where(c=>c&&c.species==species).ToArray();
        var matching=all.Where(c=>string.IsNullOrEmpty(characterSearch)||
            c.displayName.IndexOf(characterSearch,StringComparison.OrdinalIgnoreCase)>=0||
            c.id.IndexOf(characterSearch,StringComparison.OrdinalIgnoreCase)>=0).ToArray();
        if(matching.Length==0)return 0;
        int group=(int)species;
        bool expanded=string.IsNullOrEmpty(characterSearch)?speciesOpen[group]:true;
        bool next=EditorGUILayout.Foldout(expanded,SpeciesNames[group]+"  ("+matching.Length+")",true);
        if(string.IsNullOrEmpty(characterSearch))speciesOpen[group]=next;
        if(!next)return matching.Length;
        for(int tier=0;tier<TierNames.Length;tier++)
        {
            var choices=matching.Where(c=>CharacterTier(c)==tier).ToArray();
            if(choices.Length==0)continue;
            GUILayout.BeginHorizontal();GUILayout.Space(12);
            GUILayout.BeginVertical();
            bool tierExpanded=string.IsNullOrEmpty(characterSearch)?tierOpen[group,tier]:true;
            bool nextTier=EditorGUILayout.Foldout(tierExpanded,TierNames[tier]+"  ("+choices.Length+")",true);
            if(string.IsNullOrEmpty(characterSearch))tierOpen[group,tier]=nextTier;
            if(nextTier)foreach(var c in choices)DrawCharacterRow(c);
            GUILayout.EndVertical();GUILayout.EndHorizontal();
        }
        return matching.Length;
    }
    static int CharacterTier(ToolCharacter c)
    {
        if(!c.look.creatorEnabled)return 0;
        return c.id.StartsWith("creator_h",StringComparison.Ordinal)||c.id.StartsWith("creator_a",StringComparison.Ordinal)||c.id.StartsWith("creator_f",StringComparison.Ordinal)?1:2;
    }
    void DrawCharacterRow(ToolCharacter c)
    {
        Rect r=GUILayoutUtility.GetRect(165,30);
        if(selectedCharacter==c)
        {
            EditorGUI.DrawRect(r,new Color(.27f,.39f,.31f));
            GUI.Label(new Rect(r.x+7,r.y+4,r.width-10,r.height-7),c.displayName,EditorStyles.whiteLabel);
        }
        else LibraryCard(r,new GUIContent(c.displayName));
        if(Event.current.type==EventType.MouseDown&&r.Contains(Event.current.mousePosition))
        {selectedCharacter=c;ResetPreview();}
        StartDrag(r,"actor",c);
    }
    void DragButton(string label,string kind,object value)
    {
        Rect r=GUILayoutUtility.GetRect(180,36);LibraryCard(r,new GUIContent(label));StartDrag(r,kind,value);
    }
    static void LibraryCard(Rect rect,GUIContent label)
    {
        EditorGUI.DrawRect(new Rect(rect.x,rect.y+1,rect.width,rect.height-2),new Color(.17f,.20f,.18f));
        GUI.Label(new Rect(rect.x+7,rect.y+5,rect.width-14,rect.height-10),label,EditorStyles.whiteLabel);
    }
    void StartDrag(Rect rect,string kind,object value)
    {
        if(Event.current.type==EventType.MouseDown&&rect.Contains(Event.current.mousePosition))
        {
            pendingDragKind=kind;pendingDragValue=value;Event.current.Use();
        }
    }
    void DrawPreview(Rect rect)
    {
        if(Event.current.type!=EventType.Repaint){GUI.Box(rect,GUIContent.none);return;}
        if(!unit){GUI.Box(rect,"选择演出单元");return;}
        var activeActors=unit.actors.Where(a=>a.start<=playhead&&a.start+a.duration>playhead&&a.character).ToArray();
        var currentCamera=unit.cameras.LastOrDefault(c=>c.start<=playhead&&c.start+c.duration>playhead);
        var actorSpan=activeActors.FirstOrDefault(a=>currentCamera!=null&&a.id==currentCamera.actorId)??activeActors.FirstOrDefault();
        var actor=selectedCharacter?selectedCharacter:actorSpan==null?null:actorSpan.character;
        if(!actor){GUI.Box(rect,"当前没有人物");return;}
        try
        {
            string context=selectedCharacter?"solo/"+actor.GetInstanceID():string.Join("|",activeActors.Select(a=>a.id+"/"+a.character.GetInstanceID()));
            if(preview==null||previewActor==null||previewActorId!=actor.id||previewContext!=context){CreatePreview(actor);previewContext=context;}
            float time=!string.IsNullOrEmpty(hoveredAction)?(float)(EditorApplication.timeSinceStartup%6):playhead;
            var action=unit.actions.LastOrDefault(a=>actorSpan!=null&&a.actorId==actorSpan.id&&a.start<=playhead&&a.start+a.duration>playhead);
            string id=string.IsNullOrEmpty(hoveredAction)?action==null?"Idle_Loop":action.actionId:hoveredAction;
            previewActor.GetComponent<ToolMotion>().Sample(id,string.IsNullOrEmpty(hoveredAction)?action==null?time:time-action.start:time,action==null?1:action.speed);
            if(!selectedCharacter)
            {
                for(int i=0;i<activeActors.Length;i++)
                {
                    var span=activeActors[i];GameObject liveActor;
                    if(span==actorSpan)liveActor=previewActor;
                    else if(!companionActors.TryGetValue(span.id,out liveActor))
                    {liveActor=ToolCharacterView.Create(span.character,null);preview.AddSingleGO(liveActor);companionActors[span.id]=liveActor;}
                    liveActor.transform.position=new Vector3((i-(activeActors.Length-1)*.5f)*1.7f,0,0);
                    if(span!=actorSpan||string.IsNullOrEmpty(hoveredAction))liveActor.GetComponent<ToolMotion>().SampleTimeline(unit,span.id,playhead);
                }
            }
            var scene=unit.scenes.LastOrDefault(s=>s.start<=playhead&&s.start+s.duration>playhead);
            var camera=unit.cameras.LastOrDefault(c=>c.start<=playhead&&c.start+c.duration>playhead);
            stage.Set(package.library.Scene(scene==null?"archive":scene.sceneId),camera==null?ToolFx.None:camera.fx,time);
            GameObject partner=null;if(camera!=null&&!string.IsNullOrEmpty(camera.partnerId))companionActors.TryGetValue(camera.partnerId,out partner);
            previewActor.transform.rotation=Quaternion.identity;foreach(var companion in companionActors.Values)companion.transform.rotation=Quaternion.identity;
            ToolBlocking.Apply(camera,previewActor.transform,partner?partner.transform:null);
            ToolCameraMath.Apply(preview.camera,camera,previewActor.transform,partner?partner.transform:null,actor.species==ToolSpecies.CrawlerAlien,actor.species==ToolSpecies.StandingAlien,time);
            preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.035f,.055f,.044f);
            preview.BeginPreview(rect,GUIStyle.none);preview.camera.Render();var texture=preview.EndPreview();
            GUI.DrawTexture(rect,texture,ScaleMode.StretchToFill,false);
            EditorGUI.DrawRect(new Rect(rect.x+8,rect.y+8,Mathf.Min(rect.width-16,320),29),new Color(.025f,.04f,.032f,.92f));
            GUI.Label(new Rect(rect.x+12,rect.y+10,rect.width-24,25),"ASTRA / "+actor.displayName+"   "+id,EditorStyles.whiteLabel);
        }
        catch(Exception ex){GUI.Box(rect,ex.Message);}
    }
    void CreatePreview(ToolCharacter character)
    {
        DisposePreview();preview=new PreviewRenderUtility();preview.camera.nearClipPlane=.03f;preview.camera.farClipPlane=30;
        previewStage=new GameObject("Tool preview stage");stage=previewStage.AddComponent<ToolStage>();stage.Build();preview.AddSingleGO(previewStage);
        previewActor=ToolCharacterView.Create(character,null);preview.AddSingleGO(previewActor);previewActorId=character.id;
        preview.lights[0].intensity=1.1f;preview.lights[0].transform.rotation=Quaternion.Euler(45,30,0);
    }
    void ResetPreview(){DisposePreview();Repaint();}
    void DisposePreview()
    {
        if(preview!=null){preview.Cleanup();preview=null;}
        if(previewActor)DestroyImmediate(previewActor);if(previewStage)DestroyImmediate(previewStage);
        foreach(var companion in companionActors.Values)if(companion)DestroyImmediate(companion);companionActors.Clear();previewContext=null;
        previewActor=null;previewStage=null;stage=null;previewActorId=null;
    }
    void DrawTimeline(Rect area)
    {
        GUI.Box(area,GUIContent.none);
        if(!unit)return;
        Rect title=new Rect(area.x+12,area.y+5,area.width-24,25);
        GUILayout.BeginArea(title);GUILayout.BeginHorizontal();
        GUILayout.Label("时间轴",EditorStyles.boldLabel,GUILayout.Width(50));
        GUILayout.Label("单元时长（秒）",GUILayout.Width(91));
        EditorGUI.BeginChangeCheck();
        float requested=EditorGUILayout.DelayedFloatField(unit.duration,GUILayout.Width(85));
        if(EditorGUI.EndChangeCheck())SetUnitDuration(requested);
        if(GUILayout.Button("＋30秒",GUILayout.Width(64)))SetUnitDuration(unit.duration+30);
        if(GUILayout.Button("匹配内容",GUILayout.Width(68)))SetUnitDuration(ToolTimelineEditing.ContentEnd(unit));
        GUILayout.Space(12);GUILayout.Label("缩放",GUILayout.Width(32));
        // Logarithmic zoom remains useful for seconds, minutes and hours.
        float zoom=Mathf.Log10(Mathf.Max(.000001f,pixelsPerSecond));
        float nextZoom=GUILayout.HorizontalSlider(zoom,Mathf.Min(-4,zoom),Mathf.Max(2.3f,zoom),GUILayout.Width(100));
        if(!Mathf.Approximately(zoom,nextZoom))pixelsPerSecond=Mathf.Pow(10,nextZoom);
        if(GUILayout.Button("全览",GUILayout.Width(48))){pixelsPerSecond=Mathf.Max(.000001f,(area.width-LabelWidth-80)/Mathf.Max(.1f,unit.duration));timelineScroll.x=0;}
        GUILayout.Space(10);GUILayout.Label("定位秒",GUILayout.Width(44));
        EditorGUI.BeginChangeCheck();float seek=EditorGUILayout.DelayedFloatField(playhead,GUILayout.Width(80));
        if(EditorGUI.EndChangeCheck()&&ToolTimelineEditing.Finite(seek))
        {playhead=Mathf.Clamp(seek,0,unit.duration);selectedCharacter=null;timelineScroll.x=Mathf.Max(0,playhead*pixelsPerSecond-(area.width-LabelWidth)*.35f);Repaint();}
        GUILayout.FlexibleSpace();GUILayout.Label("片段越界自动延长",EditorStyles.miniLabel);
        GUILayout.EndHorizontal();GUILayout.EndArea();
        float tail=Mathf.Max(5,(area.width-LabelWidth)/pixelsPerSecond*.25f);
        float contentWidth=Mathf.Max(area.width-LabelWidth-25,(unit.duration+tail)*pixelsPerSecond+30);
        Rect viewport=new Rect(area.x+4,area.y+34,area.width-8,area.height-40);
        timelineScroll=GUI.BeginScrollView(viewport,timelineScroll,new Rect(0,0,contentWidth+LabelWidth,Row*6+12));
        float x0=LabelWidth;
        float step=ToolTimelineEditing.TickStep(pixelsPerSecond);
        double first=Math.Floor(Math.Max(0,(timelineScroll.x-x0)/pixelsPerSecond)/step)*step;
        double last=Math.Min(unit.duration+tail,(timelineScroll.x+viewport.width-x0)/pixelsPerSecond+step);
        for(double second=first;second<=last;second+=step)
        {
            float x=x0+(float)second*pixelsPerSecond;EditorGUI.DrawRect(new Rect(x,0,1,Row*6),new Color(.28f,.32f,.30f));
            GUI.Label(new Rect(x+3,0,80,19),second.ToString("0.##")+"s",EditorStyles.miniLabel);
        }
        for(int track=0;track<5;track++)
        {
            float y=Row*(track+1);EditorGUI.DrawRect(new Rect(0,y,contentWidth+LabelWidth,Row-2),track%2==0?new Color(.17f,.20f,.18f):new Color(.13f,.16f,.14f));
            GUI.Label(new Rect(7,y+10,72,21),Tracks[track],EditorStyles.whiteBoldLabel);
            int count=Count(track);
            for(int i=0;i<count;i++)
            {
                GetClip(track,i,out float start,out float duration,out string label);
                Rect clip=new Rect(x0+start*pixelsPerSecond,y+6,Mathf.Max(2,duration*pixelsPerSecond),Row-13);
                Color color=track==0?new Color(.39f,.56f,.38f):track==1?new Color(.53f,.42f,.30f):track==2?new Color(.56f,.36f,.32f):track==3?new Color(.38f,.50f,.61f):new Color(.36f,.46f,.44f);
                bool selected=selectedTrack==track&&selectedIndex==i;
                EditorGUI.DrawRect(clip,new Color(.015f,.02f,.018f));
                float border=clip.width>=8?2:1;
                if(clip.width>border*2)EditorGUI.DrawRect(new Rect(clip.x+border,clip.y+2,clip.width-border*2,clip.height-4),color);
                if(selected&&clip.width>6)EditorGUI.DrawRect(new Rect(clip.x+2,clip.y+2,clip.width-4,2),new Color(.96f,.84f,.52f));
                if(clip.width>28)GUI.Label(new Rect(clip.x+5,clip.y+3,clip.width-10,clip.height-4),label,selected?EditorStyles.whiteBoldLabel:EditorStyles.whiteLabel);
                EditorGUIUtility.AddCursorRect(new Rect(clip.xMax-8,clip.y,8,clip.height),MouseCursor.ResizeHorizontal);
                ClipMouse(clip,track,i,x0);
            }
            DropOnTrack(new Rect(x0,y,contentWidth,Row),track,x0);
        }
        float px=x0+playhead*pixelsPerSecond;EditorGUI.DrawRect(new Rect(px,0,2,Row*6),new Color(.94f,.81f,.48f));
        float endX=x0+unit.duration*pixelsPerSecond;
        EditorGUI.DrawRect(new Rect(endX,0,2,Row*6),new Color(.74f,.47f,.35f));
        GUI.Label(new Rect(endX+4,21,180,19),"末尾 / 拖入素材可延长",EditorStyles.miniLabel);
        var ruler=new Rect(x0,0,contentWidth,Row);
        if(Event.current.type==EventType.MouseDown&&ruler.Contains(Event.current.mousePosition)){playhead=Mathf.Clamp((Event.current.mousePosition.x-x0)/pixelsPerSecond,0,unit.duration);selectedCharacter=null;selectedIndex=-1;selectedTrack=-1;Event.current.Use();Repaint();}
        GUI.EndScrollView();
        if(dragTrack>=0&&Event.current.type==EventType.MouseUp){dragTrack=-1;dragIndex=-1;Event.current.Use();}
    }
    void SetUnitDuration(float requested)
    {
        if(!ToolTimelineEditing.Finite(requested)){ShowNotification(new GUIContent("时长需要有效数字"));return;}
        Undo.RecordObject(unit,"Change unit duration");
        if(ToolTimelineEditing.SetDuration(unit,requested))EditorUtility.SetDirty(unit);
        if(requested<ToolTimelineEditing.ContentEnd(unit))ShowNotification(new GUIContent("保留现有片段：先缩短或删除末尾片段，再缩短单元"));
        playhead=Mathf.Clamp(playhead,0,unit.duration);Repaint();
    }
    int Count(int track)=>track==0?unit.actors.Count:track==1?unit.actions.Count:track==2?unit.dialogue.Count:track==3?unit.cameras.Count:unit.scenes.Count;
    void GetClip(int track,int index,out float start,out float duration,out string label)
    {
        start=0;duration=1;label="";
        if(track==0){var x=unit.actors[index];start=x.start;duration=x.duration;label=(x.character?x.character.displayName:"缺少角色")+" / "+x.id;}
        if(track==1){var x=unit.actions[index];start=x.start;duration=x.duration;label=x.actionId;}
        if(track==2){var x=unit.dialogue[index];start=x.start;duration=x.Duration;label=x.kind==ToolDialogueKind.Choice?"选择 / "+x.text:"台词 / "+x.text;}
        if(track==3){var x=unit.cameras[index];start=x.start;duration=x.duration;label=x.shot+" / "+x.fx;}
        if(track==4){var x=unit.scenes[index];start=x.start;duration=x.duration;label=x.sceneId;}
    }
    void ClipMouse(Rect rect,int track,int index,float origin)
    {
        var e=Event.current;
        if(e.type==EventType.MouseDown&&rect.Contains(e.mousePosition))
        {
            selectedTrack=track;selectedIndex=index;selectedCharacter=null;dragTrack=track;dragIndex=index;resizeClip=e.mousePosition.x>rect.xMax-10;
            e.Use();Repaint();
        }
        if(e.type==EventType.MouseDrag&&dragTrack==track&&dragIndex==index)
        {
            Undo.RecordObject(unit,"Move performance clip");float delta=e.delta.x/pixelsPerSecond;
            if(track==0){var x=unit.actors[index];if(resizeClip)x.duration=Mathf.Max(.1f,x.duration+delta);else x.start=Mathf.Max(0,x.start+delta);}
            if(track==1){var x=unit.actions[index];if(resizeClip)x.duration=Mathf.Max(.1f,x.duration+delta);else x.start=Mathf.Max(0,x.start+delta);}
            if(track==2){var x=unit.dialogue[index];if(resizeClip){if(x.kind==ToolDialogueKind.Choice)x.patienceSeconds=Mathf.Max(.1f,x.patienceSeconds+delta);else x.hold=Mathf.Max(0,x.hold+delta);}else x.start=Mathf.Max(0,x.start+delta);}
            if(track==3){var x=unit.cameras[index];if(resizeClip)x.duration=Mathf.Max(.1f,x.duration+delta);else x.start=Mathf.Max(0,x.start+delta);}
            if(track==4){var x=unit.scenes[index];if(resizeClip)x.duration=Mathf.Max(.1f,x.duration+delta);else x.start=Mathf.Max(0,x.start+delta);}
            ToolTimelineEditing.ExtendToContent(unit);EditorUtility.SetDirty(unit);e.Use();Repaint();
        }
    }
    void DropOnTrack(Rect rect,int track,float origin)
    {
        var e=Event.current;if(!rect.Contains(e.mousePosition))return;
        string kind=DragAndDrop.GetGenericData("AstraKind") as string;
        int expected=kind=="actor"?0:kind=="action"?1:kind=="text"||kind=="choice"?2:kind=="camera"?3:kind=="scene"?4:-1;
        if(expected!=track)return;
        if(e.type==EventType.DragUpdated){DragAndDrop.visualMode=DragAndDropVisualMode.Copy;e.Use();}
        if(e.type!=EventType.DragPerform)return;
        DragAndDrop.AcceptDrag();float time=Mathf.Max(0,(e.mousePosition.x-origin)/pixelsPerSecond);
        object value=DragAndDrop.GetGenericData("AstraValue");Undo.RecordObject(unit,"Add performance clip");
        if(track==0)unit.actors.Add(new ToolActorSpan{id="actor"+unit.actors.Count,character=value as ToolCharacter,start=time,duration=4});
        if(track==1)
        {
            string action=value as string;var actor=unit.actors.FirstOrDefault(a=>a.start<=time&&a.start+a.duration>time);
            if(actor!=null){var preset=package.library.Action(action);unit.actions.Add(new ToolActionSpan{actorId=actor.id,actionId=action,start=time,duration=Mathf.Min(preset.length,actor.start+actor.duration-time)});}
            else EditorUtility.DisplayDialog("动作未放置","先在人物轨放置覆盖该时间的角色。","确定");
        }
        if(track==2)unit.dialogue.Add(new ToolDialogueSpan{start=time,actorId=unit.actors.FirstOrDefault()?.id??"",kind=kind=="choice"?ToolDialogueKind.Choice:ToolDialogueKind.Text,text=kind=="choice"?"你如何回应？":"新的台词",choices=kind=="choice"?new[]{new ToolChoice{id="yes",text="确认"}}:Array.Empty<ToolChoice>()});
        if(track==3)unit.cameras.Add(new ToolCameraSpan{shot=(ToolShot)value,actorId=unit.actors.FirstOrDefault()?.id??"",start=time,duration=3});
        if(track==4)unit.scenes.Add(new ToolSceneSpan{sceneId=value as string,start=time,duration=4});
        ToolTimelineEditing.ExtendToContent(unit);EditorUtility.SetDirty(unit);DragAndDrop.SetGenericData("AstraKind",null);e.Use();Repaint();
    }
    void Inspector()
    {
        GUILayout.Label("检查与属性",EditorStyles.boldLabel);
        inspectorScroll=GUILayout.BeginScrollView(inspectorScroll);
        if(selectedCharacter)
        {
            GUILayout.Label("角色外观 / "+selectedCharacter.displayName,EditorStyles.boldLabel);
            if(selectedCharacter.look.creatorEnabled)
            {
                if(GUILayout.Button("打开捏人编辑器",GUILayout.Height(30)))ToolV2CharacterCreatorWindow.OpenEdit(selectedCharacter);
                if(GUILayout.Button("导出人物 Prefab"))ExportCharacter(selectedCharacter);
                EditorGUILayout.HelpBox("该角色使用可组合部件。请在捏人编辑器中调整外形、服装和配饰。",MessageType.Info);
                GUILayout.EndScrollView();return;
            }
            EditorGUILayout.HelpBox("Demo V2 原角色保留原始外观。新建角色使用独立的 Quaternius 模块化模型。",MessageType.Info);
            if(GUILayout.Button("导出人物 Prefab"))ExportCharacter(selectedCharacter);
        }
        else if(unit&&selectedIndex>=0&&selectedIndex<Count(selectedTrack))ClipInspector();
        else
        {
            unit=(ToolUnit)EditorGUILayout.ObjectField("演出单元",unit,typeof(ToolUnit),false);
            if(unit){Undo.RecordObject(unit,"Edit unit");unit.id=EditorGUILayout.TextField("ID",unit.id);EditorGUI.BeginChangeCheck();float duration=EditorGUILayout.DelayedFloatField("时长（秒）",unit.duration);if(EditorGUI.EndChangeCheck())SetUnitDuration(duration);EditorUtility.SetDirty(unit);}
            GUILayout.Label("拖拽左侧材料到下方对应轨道。选中片段可编辑属性；拖动右缘可改时长。",EditorStyles.wordWrappedMiniLabel);
        }
        GUILayout.Space(14);
        if(unit)
        {
            var errors=ToolValidation.Unit(unit,package.library);
            GUILayout.Label("即时校验  " + (errors.Count==0?"通过":"问题 "+errors.Count),EditorStyles.boldLabel);
            foreach(var err in errors.Take(8))EditorGUILayout.HelpBox(err,MessageType.Warning);
        }
        GUILayout.EndScrollView();
    }
    void ClipInspector()
    {
        Undo.RecordObject(unit,"Edit timeline clip");
        if(selectedTrack==0)
        {
            var x=unit.actors[selectedIndex];x.id=EditorGUILayout.TextField("轨 ID",x.id);x.character=(ToolCharacter)EditorGUILayout.ObjectField("角色",x.character,typeof(ToolCharacter),false);x.start=EditorGUILayout.FloatField("开始",x.start);x.duration=EditorGUILayout.FloatField("时长",x.duration);
        }
        if(selectedTrack==1)
        {
            var x=unit.actions[selectedIndex];x.actorId=EditorGUILayout.TextField("人物轨",x.actorId);x.actionId=EditorGUILayout.TextField("动作",x.actionId);x.start=EditorGUILayout.FloatField("开始",x.start);x.duration=EditorGUILayout.FloatField("时长",x.duration);x.speed=EditorGUILayout.Slider("倍速",x.speed,.25f,3);
            var actor=unit.actors.FirstOrDefault(a=>a.id==x.actorId);var preset=package.library.Action(x.actionId);
            if(actor!=null&&actor.character&&actor.character.species==ToolSpecies.CrawlerAlien&&preset!=null&&!preset.crawlerAllowed)EditorGUILayout.HelpBox("爬行体对此动作使用静止替代。",MessageType.Info);
        }
        if(selectedTrack==2)
        {
            var x=unit.dialogue[selectedIndex];x.kind=(ToolDialogueKind)EditorGUILayout.EnumPopup("形式",x.kind);x.actorId=EditorGUILayout.TextField("人物轨",x.actorId);
            x.start=EditorGUILayout.FloatField("开始",x.start);x.text=EditorGUILayout.TextArea(x.text,GUILayout.MinHeight(65));
            if(x.kind==ToolDialogueKind.Text){x.charactersPerSecond=EditorGUILayout.Slider("字/秒",x.charactersPerSecond,4,60);x.hold=EditorGUILayout.FloatField("停留",x.hold);}
            else
            {
                x.patienceSeconds=EditorGUILayout.FloatField("耐心秒数",x.patienceSeconds);
                for(int i=0;i<x.choices.Length;i++)
                {
                    var choice=x.choices[i];GUILayout.Label("选项 "+(i+1),EditorStyles.boldLabel);
                    choice.id=EditorGUILayout.TextField("出口",choice.id);choice.text=EditorGUILayout.TextField("文字",choice.text);
                    choice.hasCondition=EditorGUILayout.Toggle("有条件",choice.hasCondition);
                    if(choice.hasCondition){choice.condition.key=EditorGUILayout.TextField("资源",choice.condition.key);choice.condition.compare=(ToolCompare)EditorGUILayout.EnumPopup(choice.condition.compare);choice.condition.value=EditorGUILayout.IntField("阈值",choice.condition.value);}
                    if(choice.effects==null)choice.effects=Array.Empty<ToolEffect>();
                    for(int j=0;j<choice.effects.Length;j++)
                    {
                        GUILayout.BeginHorizontal();
                        choice.effects[j].key=EditorGUILayout.TextField("资源增减",choice.effects[j].key);
                        choice.effects[j].delta=EditorGUILayout.IntField(choice.effects[j].delta,GUILayout.Width(50));
                        if(GUILayout.Button("×",GUILayout.Width(24)))choice.effects=choice.effects.Where((_,k)=>k!=j).ToArray();
                        GUILayout.EndHorizontal();
                    }
                    if(GUILayout.Button("+ 资源增减"))choice.effects=choice.effects.Concat(new[]{new ToolEffect()}).ToArray();
                    if(GUILayout.Button("删除选项")){x.choices=x.choices.Where((_,j)=>j!=i).ToArray();break;}
                }
                if(GUILayout.Button("添加选项"))x.choices=x.choices.Concat(new[]{new ToolChoice{id="option"+x.choices.Length}}).ToArray();
                GUILayout.Label("沉默出口自动存在；在演出树中连接。",EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.LabelField("自动时长",x.Duration.ToString("F2")+" 秒");
        }
        if(selectedTrack==3)
        {
            var x=unit.cameras[selectedIndex];x.shot=(ToolShot)EditorGUILayout.EnumPopup("镜头",x.shot);x.fx=(ToolFx)EditorGUILayout.EnumPopup("画面效果",x.fx);
            x.actorId=EditorGUILayout.TextField("主体",x.actorId);x.partnerId=EditorGUILayout.TextField("陪体",x.partnerId);x.start=EditorGUILayout.FloatField("开始",x.start);x.duration=EditorGUILayout.FloatField("时长",x.duration);
            if(x.shot==ToolShot.Custom)
            {
                x.customPosition=EditorGUILayout.Vector3Field("相对位置",x.customPosition);x.customEuler=EditorGUILayout.Vector3Field("旋转",x.customEuler);x.customFov=EditorGUILayout.Slider("视野",x.customFov,20,75);
                if(GUILayout.Button("保存当前 Scene 视图镜头")&&SceneView.lastActiveSceneView!=null)
                {
                    var c=SceneView.lastActiveSceneView.camera;x.customPosition=c.transform.position;x.customEuler=c.transform.eulerAngles;x.customFov=c.fieldOfView;
                }
            }
        }
        if(selectedTrack==4){var x=unit.scenes[selectedIndex];x.sceneId=EditorGUILayout.TextField("场景 ID",x.sceneId);x.start=EditorGUILayout.FloatField("开始",x.start);x.duration=EditorGUILayout.FloatField("时长",x.duration);}
        ToolTimelineEditing.ExtendToContent(unit);EditorUtility.SetDirty(unit);
        if(GUILayout.Button("删除选中片段"))
        {
            if(selectedTrack==0)unit.actors.RemoveAt(selectedIndex);if(selectedTrack==1)unit.actions.RemoveAt(selectedIndex);
            if(selectedTrack==2)unit.dialogue.RemoveAt(selectedIndex);if(selectedTrack==3)unit.cameras.RemoveAt(selectedIndex);if(selectedTrack==4)unit.scenes.RemoveAt(selectedIndex);
            selectedIndex=-1;
        }
    }
    void ExportCharacter(ToolCharacter character)
    {
        string path=EditorUtility.SaveFilePanelInProject("导出 V2 人物","Character_"+character.id,"prefab","选择导出目录",ToolV2Build.Root+"/Exports");
        if(string.IsNullOrEmpty(path))return;
        var go=ToolCharacterView.Create(character,null);go.AddComponent<ToolCharacterInstance>().preset=character;
        PrefabUtility.SaveAsPrefabAsset(go,path);DestroyImmediate(go);AssetDatabase.SaveAssets();
    }
    void ExportUnit()
    {
        if(!unit)return;
        var errors=ToolValidation.Unit(unit,package.library);
        if(errors.Count>0){EditorUtility.DisplayDialog("无法导出",string.Join("\n",errors.Take(12)),"确定");return;}
        string path=EditorUtility.SaveFilePanelInProject("导出演出单元",unit.id,"asset","选择演出单元保存位置",ToolV2Build.Root+"/Exports");
        if(string.IsNullOrEmpty(path))return;
        var copy=Instantiate(unit);copy.name=unit.id;
        AssetDatabase.CreateAsset(copy,path);AssetDatabase.SaveAssets();EditorGUIUtility.PingObject(copy);
    }
    void OpenGamePreview()
    {
        if(ToolValidation.Package(package).Count>0){ShowValidation();return;}
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ToolV2Build.Root+"/Scenes/ToolPreview.unity");
        EditorApplication.isPlaying=true;
    }
    void ShowValidation()
    {
        var errors=ToolValidation.Package(package);
        EditorUtility.DisplayDialog("演出校验",errors.Count==0?"所有已连接演出单元通过校验。":string.Join("\n",errors.Take(14)),"确定");
    }
}

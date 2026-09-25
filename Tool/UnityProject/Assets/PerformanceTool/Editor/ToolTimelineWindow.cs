using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceTool;

public sealed class ToolTimelineWindow : EditorWindow
{
    ToolPackage package;
    ToolUnit unit;
    ToolCharacter selectedCharacter;
    PreviewRenderUtility preview;
    GameObject previewActor,previewStage;
    ToolStage stage;
    string previewActorId;
    float playhead, pixelsPerSecond=88;
    bool playing;
    double lastTime;
    int tab,selectedTrack=-1,selectedIndex=-1,dragTrack=-1,dragIndex=-1;
    bool resizeClip;
    string hoveredAction;
    string pendingDragKind;
    object pendingDragValue;
    Vector2 libraryScroll,inspectorScroll,timelineScroll;
    const float Left=205,Right=292,Row=42,LabelWidth=88;
    static readonly string[] Tabs={"人物","动作","台词","镜头","场景"};
    static readonly string[] Tracks={"人物","动作","台词","镜头","场景"};
    [MenuItem("Astra Performance Tool/Open timeline %#t")]
    public static void Open(){var w=GetWindow<ToolTimelineWindow>("ASTRA 演出时间轴");w.minSize=new Vector2(1100,720);w.Show();}
    void OnEnable()
    {
        package=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolBuild.PackagePath);
        if(package&&package.graph&&package.graph.nodes.Count>0)unit=package.graph.nodes.FirstOrDefault(n=>n.unit!=null)?.unit;
        EditorApplication.update+=Tick;
    }
    void OnDisable(){EditorApplication.update-=Tick;DisposePreview();}
    void Tick()
    {
        double now=EditorApplication.timeSinceStartup;
        if(playing&&unit){playhead+=Mathf.Clamp((float)(now-lastTime),0,.1f);if(playhead>unit.duration){playhead=0;playing=false;}Repaint();}
        else if(!string.IsNullOrEmpty(hoveredAction))Repaint();
        lastTime=now;
    }
    void OnGUI()
    {
        if(!package){if(GUILayout.Button("创建示例工程",GUILayout.Height(45))){package=ToolBuild.CreateSample();unit=package.graph.nodes[0].unit;}return;}
        Toolbar();
        float upper=Mathf.Max(300,position.height-330);
        Rect lib=new Rect(0,32,Left,upper-32),viewer=new Rect(Left,32,position.width-Left-Right,upper-32),inspect=new Rect(position.width-Right,32,Right,upper-32);
        GUI.Box(lib,GUIContent.none);GUI.Box(viewer,GUIContent.none);GUI.Box(inspect,GUIContent.none);
        GUILayout.BeginArea(lib);Library();GUILayout.EndArea();
        GUILayout.BeginArea(viewer);DrawPreview(new Rect(8,8,viewer.width-16,viewer.height-40));GUI.Label(new Rect(16,viewer.height-29,viewer.width-32,20),"实时预览  ·  拖动下方时间线定位  ·  悬停动作可试演");GUILayout.EndArea();
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
        if(GUILayout.Button("演出树",EditorStyles.toolbarButton,GUILayout.Width(65)))ToolGraphWindow.Open();
        if(GUILayout.Button("验证",EditorStyles.toolbarButton,GUILayout.Width(55)))ShowValidation();
        if(GUILayout.Button("导出演出单元",EditorStyles.toolbarButton,GUILayout.Width(96)))ExportUnit();
        if(GUILayout.Button("运行预览",EditorStyles.toolbarButton,GUILayout.Width(75)))OpenGamePreview();
        if(GUILayout.Button("导出 Unity 包",EditorStyles.toolbarButton,GUILayout.Width(100))){ToolBuild.VerifyAndExport();EditorUtility.DisplayDialog("导出完成","Tool/Export/ASTRA_Performance_Tool.unitypackage 已生成。","确定");}
        GUILayout.FlexibleSpace();GUILayout.Label(unit?playhead.ToString("F2")+" / "+unit.duration.ToString("F2")+" s":"",GUILayout.Width(100));
        GUILayout.EndHorizontal();
    }
    void Library()
    {
        GUILayout.Label("素材库",EditorStyles.boldLabel);
        tab=GUILayout.Toolbar(tab,Tabs);
        libraryScroll=GUILayout.BeginScrollView(libraryScroll);
        if(tab==0)
        {
            foreach(var c in package.library.characters)
            {
                Rect r=GUILayoutUtility.GetRect(180,36);
                GUI.Box(r,c.displayName+"  /  "+c.species);
                if(Event.current.type==EventType.MouseDown&&r.Contains(Event.current.mousePosition)){selectedCharacter=c;ResetPreview();}
                StartDrag(r,"actor",c);
            }
            GUILayout.Space(10);GUILayout.Label("选中角色后在右侧修改外观并导出人物 Prefab。",EditorStyles.wordWrappedMiniLabel);
        }
        if(tab==1)
        {
            foreach(var action in package.library.actions)
            {
                Rect r=GUILayoutUtility.GetRect(180,31);
                GUI.Box(r,new GUIContent(action.displayName+"  ▷",action.id+" · "+action.length.ToString("F1")+" 秒"));
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
    void DragButton(string label,string kind,object value)
    {
        Rect r=GUILayoutUtility.GetRect(180,36);GUI.Box(r,label);StartDrag(r,kind,value);
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
        var actorSpan=unit.actors.LastOrDefault(a=>a.start<=playhead&&a.start+a.duration>playhead);
        var actor=selectedCharacter?selectedCharacter:actorSpan==null?null:actorSpan.character;
        if(!actor){GUI.Box(rect,"当前没有人物");return;}
        try
        {
            if(preview==null||previewActor==null||previewActorId!=actor.id)CreatePreview(actor);
            float time=!string.IsNullOrEmpty(hoveredAction)?(float)(EditorApplication.timeSinceStartup%6):playhead;
            var action=unit.actions.LastOrDefault(a=>actorSpan!=null&&a.actorId==actorSpan.id&&a.start<=playhead&&a.start+a.duration>playhead);
            string id=string.IsNullOrEmpty(hoveredAction)?action==null?"Idle_Loop":action.actionId:hoveredAction;
            previewActor.GetComponent<ToolMotion>().Sample(id,string.IsNullOrEmpty(hoveredAction)?action==null?time:time-action.start:time,action==null?1:action.speed);
            var scene=unit.scenes.LastOrDefault(s=>s.start<=playhead&&s.start+s.duration>playhead);
            var camera=unit.cameras.LastOrDefault(c=>c.start<=playhead&&c.start+c.duration>playhead);
            stage.Set(package.library.Scene(scene==null?"archive":scene.sceneId),camera==null?ToolFx.None:camera.fx,time);
            ToolCameraMath.Apply(preview.camera,camera,previewActor.transform,null,actor.species==ToolSpecies.CrawlerAlien,time);
            preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.035f,.055f,.044f);
            preview.BeginPreview(rect,GUIStyle.none);preview.camera.Render();var texture=preview.EndPreview();
            GUI.DrawTexture(rect,texture,ScaleMode.StretchToFill,false);
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
        previewActor=null;previewStage=null;stage=null;previewActorId=null;
    }
    void DrawTimeline(Rect area)
    {
        GUI.Box(area,GUIContent.none);
        if(!unit)return;
        Rect title=new Rect(area.x+12,area.y+5,area.width-24,25);
        GUI.Label(title,"时间轴  /  "+unit.id,EditorStyles.boldLabel);
        float contentWidth=Mathf.Max(area.width-LabelWidth-25,unit.duration*pixelsPerSecond+60);
        Rect viewport=new Rect(area.x+4,area.y+34,area.width-8,area.height-40);
        timelineScroll=GUI.BeginScrollView(viewport,timelineScroll,new Rect(0,0,contentWidth+LabelWidth,Row*6+12));
        float x0=LabelWidth;
        for(int second=0;second<=Mathf.CeilToInt(unit.duration);second++)
        {
            float x=x0+second*pixelsPerSecond;EditorGUI.DrawRect(new Rect(x,0,1,Row*6),new Color(.28f,.32f,.30f));
            GUI.Label(new Rect(x+3,0,50,19),second+"s",EditorStyles.miniLabel);
        }
        for(int track=0;track<5;track++)
        {
            float y=Row*(track+1);EditorGUI.DrawRect(new Rect(0,y,contentWidth+LabelWidth,Row-2),track%2==0?new Color(.17f,.20f,.18f):new Color(.13f,.16f,.14f));
            GUI.Label(new Rect(7,y+10,72,21),Tracks[track],EditorStyles.boldLabel);
            int count=Count(track);
            for(int i=0;i<count;i++)
            {
                GetClip(track,i,out float start,out float duration,out string label);
                Rect clip=new Rect(x0+start*pixelsPerSecond,y+6,Mathf.Max(20,duration*pixelsPerSecond),Row-13);
                Color color=track==0?new Color(.39f,.56f,.38f):track==1?new Color(.53f,.42f,.30f):track==2?new Color(.56f,.36f,.32f):track==3?new Color(.38f,.50f,.61f):new Color(.36f,.46f,.44f);
                EditorGUI.DrawRect(clip,color);GUI.Label(new Rect(clip.x+5,clip.y+3,clip.width-10,clip.height-4),label,selectedTrack==track&&selectedIndex==i?EditorStyles.whiteBoldLabel:EditorStyles.whiteLabel);
                EditorGUIUtility.AddCursorRect(new Rect(clip.xMax-8,clip.y,8,clip.height),MouseCursor.ResizeHorizontal);
                ClipMouse(clip,track,i,x0);
            }
            DropOnTrack(new Rect(x0,y,contentWidth,Row),track,x0);
        }
        float px=x0+playhead*pixelsPerSecond;EditorGUI.DrawRect(new Rect(px,0,2,Row*6),new Color(.94f,.81f,.48f));
        var ruler=new Rect(x0,0,contentWidth,Row);
        if(Event.current.type==EventType.MouseDown&&ruler.Contains(Event.current.mousePosition)){playhead=Mathf.Clamp((Event.current.mousePosition.x-x0)/pixelsPerSecond,0,unit.duration);selectedCharacter=null;Event.current.Use();Repaint();}
        GUI.EndScrollView();
        if(dragTrack>=0&&Event.current.type==EventType.MouseUp){dragTrack=-1;dragIndex=-1;Event.current.Use();}
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
            if(track==2){var x=unit.dialogue[index];if(resizeClip)x.hold=Mathf.Max(0,x.hold+delta);else x.start=Mathf.Max(0,x.start+delta);}
            if(track==3){var x=unit.cameras[index];if(resizeClip)x.duration=Mathf.Max(.1f,x.duration+delta);else x.start=Mathf.Max(0,x.start+delta);}
            if(track==4){var x=unit.scenes[index];if(resizeClip)x.duration=Mathf.Max(.1f,x.duration+delta);else x.start=Mathf.Max(0,x.start+delta);}
            EditorUtility.SetDirty(unit);e.Use();Repaint();
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
        DragAndDrop.AcceptDrag();float time=Mathf.Clamp((e.mousePosition.x-origin)/pixelsPerSecond,0,unit.duration-.1f);
        object value=DragAndDrop.GetGenericData("AstraValue");Undo.RecordObject(unit,"Add performance clip");
        if(track==0)unit.actors.Add(new ToolActorSpan{id="actor"+unit.actors.Count,character=value as ToolCharacter,start=time,duration=Mathf.Min(4,unit.duration-time)});
        if(track==1)
        {
            string action=value as string;var actor=unit.actors.FirstOrDefault(a=>a.start<=time&&a.start+a.duration>time);
            if(actor!=null){var preset=package.library.Action(action);unit.actions.Add(new ToolActionSpan{actorId=actor.id,actionId=action,start=time,duration=Mathf.Min(preset.length,actor.start+actor.duration-time)});}
            else EditorUtility.DisplayDialog("动作未放置","先在人物轨放置覆盖该时间的角色。","确定");
        }
        if(track==2)unit.dialogue.Add(new ToolDialogueSpan{start=time,actorId=unit.actors.FirstOrDefault()?.id??"",kind=kind=="choice"?ToolDialogueKind.Choice:ToolDialogueKind.Text,text=kind=="choice"?"你如何回应？":"新的台词",choices=kind=="choice"?new[]{new ToolChoice{id="yes",text="确认"}}:Array.Empty<ToolChoice>()});
        if(track==3)unit.cameras.Add(new ToolCameraSpan{shot=(ToolShot)value,actorId=unit.actors.FirstOrDefault()?.id??"",start=time,duration=Mathf.Min(3,unit.duration-time)});
        if(track==4)unit.scenes.Add(new ToolSceneSpan{sceneId=value as string,start=time,duration=Mathf.Min(4,unit.duration-time)});
        EditorUtility.SetDirty(unit);DragAndDrop.SetGenericData("AstraKind",null);e.Use();Repaint();
    }
    void Inspector()
    {
        GUILayout.Label("检查与属性",EditorStyles.boldLabel);
        inspectorScroll=GUILayout.BeginScrollView(inspectorScroll);
        if(selectedCharacter)
        {
            GUILayout.Label("角色外观 / "+selectedCharacter.displayName,EditorStyles.boldLabel);
            var look=selectedCharacter.look;EditorGUI.BeginChangeCheck();
            look.faceWidth=EditorGUILayout.Slider("面部宽度",look.faceWidth,.75f,1.25f);
            look.bodyScale=EditorGUILayout.Slider("体型",look.bodyScale,.8f,1.25f);
            look.skin=EditorGUILayout.ColorField("肤色",look.skin);look.suit=EditorGUILayout.ColorField("服装",look.suit);look.hair=EditorGUILayout.ColorField("发色",look.hair);
            look.hairOrHeadgear=EditorGUILayout.Popup("发型/头饰",look.hairOrHeadgear,new[]{"原始","工帽","顶冠"});
            look.accessory=EditorGUILayout.Popup("配饰",look.accessory,new[]{"无","接收器","工牌"});
            if(EditorGUI.EndChangeCheck()){Undo.RecordObject(selectedCharacter,"Edit character look");EditorUtility.SetDirty(selectedCharacter);ResetPreview();}
            if(GUILayout.Button("导出人物 Prefab"))ExportCharacter(selectedCharacter);
        }
        else if(unit&&selectedIndex>=0&&selectedIndex<Count(selectedTrack))ClipInspector();
        else
        {
            unit=(ToolUnit)EditorGUILayout.ObjectField("演出单元",unit,typeof(ToolUnit),false);
            if(unit){Undo.RecordObject(unit,"Edit unit");unit.id=EditorGUILayout.TextField("ID",unit.id);unit.duration=EditorGUILayout.FloatField("时长",unit.duration);EditorUtility.SetDirty(unit);}
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
        EditorUtility.SetDirty(unit);
        if(GUILayout.Button("删除选中片段"))
        {
            if(selectedTrack==0)unit.actors.RemoveAt(selectedIndex);if(selectedTrack==1)unit.actions.RemoveAt(selectedIndex);
            if(selectedTrack==2)unit.dialogue.RemoveAt(selectedIndex);if(selectedTrack==3)unit.cameras.RemoveAt(selectedIndex);if(selectedTrack==4)unit.scenes.RemoveAt(selectedIndex);
            selectedIndex=-1;
        }
    }
    void ExportCharacter(ToolCharacter character)
    {
        string path=EditorUtility.SaveFilePanelInProject("导出 V3 人物","Character_"+character.id,"prefab","选择导出目录",ToolBuild.Root+"/Exports");
        if(string.IsNullOrEmpty(path))return;
        var go=ToolCharacterView.Create(character,null,false);go.AddComponent<ToolCharacterInstance>().preset=character;
        PrefabUtility.SaveAsPrefabAsset(go,path);DestroyImmediate(go);AssetDatabase.SaveAssets();
    }
    void ExportUnit()
    {
        if(!unit)return;
        var errors=ToolValidation.Unit(unit,package.library);
        if(errors.Count>0){EditorUtility.DisplayDialog("无法导出",string.Join("\n",errors.Take(12)),"确定");return;}
        string path=EditorUtility.SaveFilePanelInProject("导出演出单元",unit.id,"asset","选择演出单元保存位置",ToolBuild.Root+"/Exports");
        if(string.IsNullOrEmpty(path))return;
        var copy=Instantiate(unit);copy.name=unit.id;
        AssetDatabase.CreateAsset(copy,path);AssetDatabase.SaveAssets();EditorGUIUtility.PingObject(copy);
    }
    void OpenGamePreview()
    {
        if(ToolValidation.Package(package).Count>0){ShowValidation();return;}
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ToolBuild.Root+"/Scenes/ToolPreview.unity");
        EditorApplication.isPlaying=true;
    }
    void ShowValidation()
    {
        var errors=ToolValidation.Package(package);
        EditorUtility.DisplayDialog("演出校验",errors.Count==0?"所有已连接演出单元通过校验。":string.Join("\n",errors.Take(14)),"确定");
    }
}

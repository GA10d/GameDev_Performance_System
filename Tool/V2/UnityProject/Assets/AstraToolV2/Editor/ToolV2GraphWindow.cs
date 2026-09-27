using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceToolV2;

public sealed class ToolV2GraphWindow : EditorWindow
{
    ToolPackage package;
    ToolGraph graph;
    ToolGraphNode selected,dragging,pending;
    string pendingSlot;
    Vector2 scroll,inspectorScroll;
    const float NodeWidth=215,NodeTop=54,NodePort=23,InspectorWidth=300;
    [MenuItem("Astra Performance Tool V2/Open graph %#g")]
    public static void Open(){var w=GetWindow<ToolV2GraphWindow>("ASTRA 演出树");w.minSize=new Vector2(1050,650);w.Show();}
    void OnEnable(){package=AssetDatabase.LoadAssetAtPath<ToolPackage>(ToolV2Build.PackagePath);graph=package?package.graph:null;}
    void OnGUI()
    {
        Toolbar();if(!graph)return;
        Rect canvas=new Rect(0,30,position.width-InspectorWidth,position.height-30);
        Rect panel=new Rect(position.width-InspectorWidth,30,InspectorWidth,position.height-30);
        GUI.Box(canvas,GUIContent.none);GUI.Box(panel,GUIContent.none);
        DrawCanvas(canvas);GUILayout.BeginArea(panel);Inspector();GUILayout.EndArea();
    }
    void Toolbar()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);
        package=(ToolPackage)EditorGUILayout.ObjectField(package,typeof(ToolPackage),false,GUILayout.Width(230));
        graph=package?package.graph:null;
        if(GUILayout.Button("+ 演出单元",EditorStyles.toolbarButton))Add(ToolNodeKind.Unit);
        if(GUILayout.Button("+ 判断",EditorStyles.toolbarButton))Add(ToolNodeKind.Condition);
        if(GUILayout.Button("+ 全局选择",EditorStyles.toolbarButton))Add(ToolNodeKind.GlobalChoice);
        if(GUILayout.Button("+ 结束",EditorStyles.toolbarButton))Add(ToolNodeKind.End);
        if(GUILayout.Button("时间轴",EditorStyles.toolbarButton))ToolV2TimelineWindow.Open();
        if(GUILayout.Button("校验",EditorStyles.toolbarButton))
        {
            var errors=ToolValidation.Package(package);EditorUtility.DisplayDialog("演出树校验",errors.Count==0?"通过":string.Join("\n",errors.Take(18)),"确定");
        }
        GUILayout.FlexibleSpace();GUILayout.Label(pending!=null?"连接 "+pending.id+" / "+pendingSlot+" → 点击目标节点":"拖动节点；点出口后点目标节点连接",EditorStyles.miniLabel);
        GUILayout.EndHorizontal();
    }
    void Add(ToolNodeKind kind)
    {
        if(!graph)return;Undo.RecordObject(graph,"Add performance node");
        var node=new ToolGraphNode{id=kind.ToString().ToLower()+"_"+graph.nodes.Count,kind=kind,position=new Vector2(scroll.x+70,scroll.y+80+graph.nodes.Count*25)};
        if(kind==ToolNodeKind.GlobalChoice)node.choices=new[]{new ToolChoice{id="option0",text="确认"}};
        graph.nodes.Add(node);selected=node;EditorUtility.SetDirty(graph);
    }
    void DrawCanvas(Rect area)
    {
        Rect view=new Rect(area.x+4,area.y+4,area.width-8,area.height-8);
        scroll=GUI.BeginScrollView(view,scroll,new Rect(0,0,Mathf.Max(1700,area.width),Mathf.Max(1100,area.height)));
        DrawGrid(new Rect(0,0,1700,1100));
        Handles.BeginGUI();
        foreach(var n in graph.nodes)
        foreach(var edge in n.edges)
        {
            var target=graph.Node(edge.target);if(target==null)continue;
            var slots=Slots(n);int index=Array.IndexOf(slots,edge.slot);if(index<0)continue;
            Vector3 from=new Vector3(n.position.x+NodeWidth,n.position.y+NodeTop+index*NodePort+NodePort/2f,0);
            Vector3 to=new Vector3(target.position.x,target.position.y+24,0);
            Handles.DrawBezier(from,to,from+Vector3.right*75,to+Vector3.left*75,new Color(.58f,.72f,.55f),null,3);
        }
        Handles.EndGUI();
        foreach(var n in graph.nodes)DrawNode(n);
        if(Event.current.type==EventType.DragUpdated&&DragAndDrop.objectReferences.Any(x=>x is ToolUnit)){DragAndDrop.visualMode=DragAndDropVisualMode.Copy;Event.current.Use();}
        if(Event.current.type==EventType.DragPerform)
        {
            var unit=DragAndDrop.objectReferences.OfType<ToolUnit>().FirstOrDefault();
            if(unit!=null){DragAndDrop.AcceptDrag();Add(ToolNodeKind.Unit);selected.unit=unit;selected.position=Event.current.mousePosition;EditorUtility.SetDirty(graph);Event.current.Use();}
        }
        GUI.EndScrollView();
        if(Event.current.type==EventType.MouseUp)dragging=null;
    }
    void DrawGrid(Rect area)
    {
        for(int x=0;x<area.width;x+=40)EditorGUI.DrawRect(new Rect(x,0,1,area.height),new Color(.18f,.23f,.20f));
        for(int y=0;y<area.height;y+=40)EditorGUI.DrawRect(new Rect(0,y,area.width,1),new Color(.18f,.23f,.20f));
    }
    void DrawNode(ToolGraphNode n)
    {
        string[] slots=Slots(n);float height=NodeTop+Mathf.Max(1,slots.Length)*NodePort+7;
        Rect box=new Rect(n.position.x,n.position.y,NodeWidth,height);
        Color fill=n==selected?new Color(.30f,.39f,.32f):n.kind==ToolNodeKind.Condition?new Color(.38f,.31f,.23f):n.kind==ToolNodeKind.GlobalChoice?new Color(.34f,.28f,.31f):new Color(.24f,.29f,.27f);
        EditorGUI.DrawRect(box,fill);
        GUI.Label(new Rect(box.x+9,box.y+5,box.width-18,23),n.id+"  ·  "+KindName(n.kind),EditorStyles.whiteBoldLabel);
        GUI.Label(new Rect(box.x+9,box.y+27,box.width-18,20),n.kind==ToolNodeKind.Unit?n.unit?n.unit.id:"未选择单元":n.kind==ToolNodeKind.Condition?n.condition.key+" "+n.condition.compare+" "+n.condition.value:n.kind==ToolNodeKind.GlobalChoice?n.prompt:"结束",EditorStyles.whiteMiniLabel);
        if(graph.entryNode==n.id)GUI.Label(new Rect(box.x+3,box.y-20,80,18),"入口 ▶",EditorStyles.boldLabel);
        for(int i=0;i<slots.Length;i++)
        {
            Rect port=new Rect(box.x+NodeWidth-20,box.y+NodeTop+i*NodePort,19,19);
            EditorGUI.DrawRect(port,pending==n&&pendingSlot==slots[i]?Color.yellow:new Color(.65f,.78f,.53f));
            GUI.Label(new Rect(box.x+8,port.y,NodeWidth-34,19),slots[i]+" → "+(ToolLogic.Exit(n,slots[i])??"未连接"),EditorStyles.whiteMiniLabel);
            if(Event.current.type==EventType.MouseDown&&port.Contains(Event.current.mousePosition))
            {pending=n;pendingSlot=slots[i];selected=n;Event.current.Use();Repaint();return;}
        }
        var e=Event.current;
        if(e.type==EventType.MouseDown&&box.Contains(e.mousePosition))
        {
            if(pending!=null&&pending!=n)
            {
                Undo.RecordObject(graph,"Connect performance nodes");var edge=pending.edges.FirstOrDefault(x=>x.slot==pendingSlot);
                if(edge==null)pending.edges.Add(new ToolGraphEdge{slot=pendingSlot,target=n.id});else edge.target=n.id;
                EditorUtility.SetDirty(graph);pending=null;pendingSlot=null;
            }
            else {selected=n;dragging=n;}
            e.Use();Repaint();
        }
        if(e.type==EventType.MouseDrag&&dragging==n)
        {Undo.RecordObject(graph,"Move performance node");n.position+=e.delta;EditorUtility.SetDirty(graph);e.Use();Repaint();}
    }
    static string KindName(ToolNodeKind kind)=>kind==ToolNodeKind.Unit?"演出":kind==ToolNodeKind.Condition?"判断":kind==ToolNodeKind.GlobalChoice?"全局选择":"终点";
    static string[] Slots(ToolGraphNode node)
    {
        if(node.kind==ToolNodeKind.End)return Array.Empty<string>();
        if(node.kind==ToolNodeKind.Condition)return new[]{"yes","no"};
        if(node.kind==ToolNodeKind.GlobalChoice)return (node.choices??Array.Empty<ToolChoice>()).Select(c=>c.id).Concat(new[]{"silence"}).ToArray();
        if(node.unit==null)return new[]{"next"};
        var last=node.unit.dialogue.OrderBy(d=>d.start).LastOrDefault();
        return last!=null&&last.kind==ToolDialogueKind.Choice?(last.choices??Array.Empty<ToolChoice>()).Select(c=>c.id).Concat(new[]{"silence"}).ToArray():new[]{"next"};
    }
    void Inspector()
    {
        GUILayout.Label("演出树属性",EditorStyles.boldLabel);
        inspectorScroll=GUILayout.BeginScrollView(inspectorScroll);
        if(package!=null)
        {
            Undo.RecordObject(package,"Edit performance resources");
            GUILayout.Label("全局资源初值",EditorStyles.boldLabel);
            if(package.initialStats==null)package.initialStats=Array.Empty<ToolStat>();
            for(int i=0;i<package.initialStats.Length;i++)
            {
                GUILayout.BeginHorizontal();
                package.initialStats[i].key=EditorGUILayout.TextField(package.initialStats[i].key);
                package.initialStats[i].value=EditorGUILayout.IntField(package.initialStats[i].value,GUILayout.Width(55));
                if(GUILayout.Button("×",GUILayout.Width(24)))package.initialStats=package.initialStats.Where((_,j)=>j!=i).ToArray();
                GUILayout.EndHorizontal();
            }
            if(GUILayout.Button("+ 新资源"))package.initialStats=package.initialStats.Concat(new[]{new ToolStat{key="resource"+package.initialStats.Length}}).ToArray();
            package.initialPatience=EditorGUILayout.IntField("初始耐心",package.initialPatience);
            EditorUtility.SetDirty(package);GUILayout.Space(12);
        }
        if(selected==null){GUILayout.Label("选择节点查看属性。点出口，再点目标节点连接。",EditorStyles.wordWrappedMiniLabel);GUILayout.EndScrollView();return;}
        Undo.RecordObject(graph,"Edit performance graph");
        string oldId=selected.id;
        string newId=EditorGUILayout.TextField("节点 ID",oldId);
        if(!string.IsNullOrWhiteSpace(newId)&&newId!=oldId&&!graph.nodes.Any(n=>n!=selected&&n.id==newId))
        {
            selected.id=newId;if(graph.entryNode==oldId)graph.entryNode=newId;
            foreach(var n in graph.nodes)foreach(var edge in n.edges)if(edge.target==oldId)edge.target=newId;
        }
        if(GUILayout.Button("设为入口"))graph.entryNode=selected.id;
        if(selected.kind==ToolNodeKind.Unit)selected.unit=(ToolUnit)EditorGUILayout.ObjectField("演出单元",selected.unit,typeof(ToolUnit),false);
        if(selected.kind==ToolNodeKind.Condition)
        {
            selected.condition.key=EditorGUILayout.TextField("全局变量",selected.condition.key);
            selected.condition.compare=(ToolCompare)EditorGUILayout.EnumPopup("判断",selected.condition.compare);
            selected.condition.value=EditorGUILayout.IntField("阈值",selected.condition.value);
        }
        if(selected.kind==ToolNodeKind.GlobalChoice)
        {
            selected.prompt=EditorGUILayout.TextField("提示",selected.prompt);
            selected.patienceSeconds=EditorGUILayout.FloatField("耐心秒数",selected.patienceSeconds);
            for(int i=0;i<selected.choices.Length;i++)
            {
                var option=selected.choices[i];GUILayout.Space(8);GUILayout.Label("选项 "+(i+1),EditorStyles.boldLabel);
                option.id=EditorGUILayout.TextField("出口",option.id);option.text=EditorGUILayout.TextField("显示",option.text);
                option.hasCondition=EditorGUILayout.Toggle("有条件",option.hasCondition);
                if(option.hasCondition){option.condition.key=EditorGUILayout.TextField("变量",option.condition.key);option.condition.compare=(ToolCompare)EditorGUILayout.EnumPopup(option.condition.compare);option.condition.value=EditorGUILayout.IntField("阈值",option.condition.value);}
                Effects(option);
                if(GUILayout.Button("删除选项")){selected.choices=selected.choices.Where((_,j)=>j!=i).ToArray();break;}
            }
            if(GUILayout.Button("添加选项"))selected.choices=selected.choices.Concat(new[]{new ToolChoice{id="option"+selected.choices.Length}}).ToArray();
            GUILayout.Label("沉默出口始终存在；耐心耗尽自动选择。",EditorStyles.wordWrappedMiniLabel);
        }
        GUILayout.Space(13);GUILayout.Label("出口",EditorStyles.boldLabel);
        foreach(string slot in Slots(selected))
        {
            string target=ToolLogic.Exit(selected,slot);
            GUILayout.Label(slot+"  →  "+(target??"点击右侧端口连接"),EditorStyles.miniLabel);
        }
        if(GUILayout.Button("清除该节点连接"))selected.edges.Clear();
        if(GUILayout.Button("删除节点"))
        {
            graph.nodes.Remove(selected);foreach(var n in graph.nodes)n.edges.RemoveAll(e=>e.target==selected.id);
            selected=null;pending=null;
        }
        EditorUtility.SetDirty(graph);GUILayout.EndScrollView();
    }
    static void Effects(ToolChoice option)
    {
        if(option.effects==null)option.effects=Array.Empty<ToolEffect>();
        for(int i=0;i<option.effects.Length;i++)
        {
            GUILayout.BeginHorizontal();option.effects[i].key=EditorGUILayout.TextField(option.effects[i].key);
            option.effects[i].delta=EditorGUILayout.IntField(option.effects[i].delta,GUILayout.Width(48));
            if(GUILayout.Button("×",GUILayout.Width(25))){option.effects=option.effects.Where((_,j)=>j!=i).ToArray();GUILayout.EndHorizontal();break;}
            GUILayout.EndHorizontal();
        }
        if(GUILayout.Button("+ 资源增减"))option.effects=option.effects.Concat(new[]{new ToolEffect()}).ToArray();
    }
}

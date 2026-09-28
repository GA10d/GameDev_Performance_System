using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Astra.PerformanceToolV2;

public sealed class ToolV2TemplateWindow : EditorWindow
{
    [SerializeField] ToolLibrary library;
    [SerializeField] ToolTemplateSettings[] settings;
    [SerializeField] int selected;
    Vector2 scroll;
    string error;
    public static void Open(ToolLibrary library)
    {
        var window=GetWindow<ToolV2TemplateWindow>(true,"新建演出 · 选择模板",true);
        if(window.library!=library||window.settings==null){window.library=library;window.ResetSettings();}
        window.minSize=new Vector2(700,660);window.Show();
    }
    void ResetSettings(){settings=Enum.GetValues(typeof(ToolPerformanceTemplate)).Cast<ToolPerformanceTemplate>().Select(t=>ToolV2Templates.Defaults(t,library)).ToArray();error=null;}
    void OnGUI()
    {
        if(settings==null||settings.Length!=5)ResetSettings();
        GUILayout.Space(10);EditorGUILayout.LabelField("从一个可直接试演的模板开始",EditorStyles.boldLabel);
        EditorGUILayout.LabelField("选择角色并填写台词，自动准备动作、镜头、场景和演出树。生成后仍可自由编辑。",EditorStyles.wordWrappedLabel);
        GUILayout.Space(8);
        var cards=new GUIStyle(GUI.skin.button){wordWrap=true,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(12,8,8,8),fixedHeight=54};
        string[] labels=ToolV2Templates.Names.Select((name,i)=>name+"\n"+ToolV2Templates.Descriptions[i]).ToArray();
        int next=GUILayout.SelectionGrid(selected,labels,2,cards);
        if(next!=selected){selected=next;scroll=Vector2.zero;error=null;GUI.FocusControl(null);}
        GUILayout.Space(8);scroll=EditorGUILayout.BeginScrollView(scroll);
        var setup=settings[selected];
        setup.name=EditorGUILayout.TextField("演出名称",setup.name);
        var nextLibrary=(ToolLibrary)EditorGUILayout.ObjectField("素材库",library,typeof(ToolLibrary),false);
        if(nextLibrary!=library)
        {
            library=nextLibrary;var cast=ToolV2Templates.Characters(library);
            foreach(var item in settings)
            {
                var defaults=ToolV2Templates.Defaults(item.template,library);
                if(!cast.Contains(item.speaker))item.speaker=defaults.speaker;
                if(!cast.Contains(item.partner))item.partner=defaults.partner;
                if(library?.scenes==null||!library.scenes.Any(s=>s.id==item.sceneId))item.sceneId=defaults.sceneId;
            }
            error=null;GUIUtility.ExitGUI();
        }
        bool blank=setup.template==ToolPerformanceTemplate.Blank;
        if(!blank)
        {
            var characters=ToolV2Templates.Characters(library);
            setup.speaker=Character("主讲人",setup.speaker,characters);
            if(setup.template==ToolPerformanceTemplate.Conversation)setup.partner=Character("对话角色",setup.partner,characters);
            var scenes=library?.scenes??Array.Empty<ToolScenePreset>();int scene=Array.FindIndex(scenes,s=>s.id==setup.sceneId);
            int chosen=EditorGUILayout.Popup("场景",scene,scenes.Select(s=>s.displayName).ToArray());if(chosen>=0&&chosen<scenes.Length)setup.sceneId=scenes[chosen].id;
            GUILayout.Space(6);
            setup.opening=Words(setup.template==ToolPerformanceTemplate.Broadcast?"播报内容":setup.template==ToolPerformanceTemplate.Conversation?"主讲人的台词":"开场台词",setup.opening);
            if(setup.template==ToolPerformanceTemplate.SingleCall)setup.reply=Words("结束语",setup.reply);
            if(setup.template==ToolPerformanceTemplate.Conversation)setup.reply=Words("对话角色的台词",setup.reply);
            if(setup.template==ToolPerformanceTemplate.Question)
            {
                setup.question=EditorGUILayout.TextField("向玩家提问",setup.question);
                setup.replySeconds=EditorGUILayout.Slider("等待回复（秒）",setup.replySeconds,1,120);
                setup.acceptLabel=EditorGUILayout.TextField("选项一",setup.acceptLabel);setup.acceptReply=Words("选择选项一后的回应",setup.acceptReply);
                setup.declineLabel=EditorGUILayout.TextField("选项二",setup.declineLabel);setup.declineReply=Words("选择选项二后的回应",setup.declineReply);
                setup.silenceReply=Words("超时未选择时的回应",setup.silenceReply);
                EditorGUILayout.HelpBox("两个选项和超时分支都会自动连接回应与结束节点。倒计时从提问出现后开始。",MessageType.None);
            }
            EditorGUILayout.HelpBox("角色与场景覆盖整段演出；片段时长按台词长度自动计算。当前模板生成字幕与角色动作，不生成配音。",MessageType.None);
        }
        else EditorGUILayout.HelpBox("自动准备一个空单元、入口和结束节点，适合自行搭建时间轴。",MessageType.Info);
        EditorGUILayout.EndScrollView();
        var issues=ToolV2Templates.Validate(library,setup);
        if(!string.IsNullOrEmpty(error))EditorGUILayout.HelpBox(error,MessageType.Error);
        else if(issues.Count>0)EditorGUILayout.HelpBox(string.Join("\n",issues),MessageType.Warning);
        GUILayout.Space(6);GUILayout.BeginHorizontal();
        if(GUILayout.Button("取消",GUILayout.Width(88),GUILayout.Height(32)))Close();
        GUILayout.FlexibleSpace();GUILayout.Label("生成草稿，保存或导出时再创建文件",EditorStyles.miniLabel);
        using(new EditorGUI.DisabledScope(issues.Count>0))if(GUILayout.Button("创建演出",GUILayout.Width(130),GUILayout.Height(32)))
        {
            try{ToolV2Authoring.CreateFromTemplate(library,setup.Copy(),()=>{if(this)Close();});}catch(Exception e){error=e.Message;}
        }
        GUILayout.EndHorizontal();GUILayout.Space(10);
    }
    static ToolCharacter Character(string label,ToolCharacter selected,ToolCharacter[] choices)
    {
        int index=Array.IndexOf(choices,selected);
        int next=EditorGUILayout.Popup(label,index,choices.Select(c=>c.displayName).ToArray());
        return next>=0&&next<choices.Length?choices[next]:null;
    }
    static string Words(string label,string value)
    {EditorGUILayout.LabelField(label);return EditorGUILayout.TextArea(value??"",GUILayout.MinHeight(48));}
}

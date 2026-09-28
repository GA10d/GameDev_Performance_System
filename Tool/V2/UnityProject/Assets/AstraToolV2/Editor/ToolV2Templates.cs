using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Astra.PerformanceToolV2;

public enum ToolPerformanceTemplate { SingleCall, Conversation, Question, Broadcast, Blank }

[Serializable]
public sealed class ToolTemplateSettings
{
    public ToolPerformanceTemplate template;
    public string name,sceneId,opening,reply,question,acceptLabel,declineLabel,acceptReply,declineReply,silenceReply;
    public ToolCharacter speaker,partner;
    public float replySeconds=15;
    public ToolTemplateSettings Copy()=>(ToolTemplateSettings)MemberwiseClone();
}

// Templates produce ordinary editable draft data, with no authored asset files or shared-data edits.
public static class ToolV2Templates
{
    public static readonly string[] Names={"单人来电","双人对话","交互问答","纯播报","空白演出"};
    public static readonly string[] Descriptions={"一位主讲人，开场与结束语","两位角色轮流说话，自动切换镜头","开场、两个选项、三条回应与结局","一段播报，播放完成自动结束","从空时间轴开始自由编排"};
    public static ToolCharacter[] Characters(ToolLibrary library)=>library?(library.characters??Array.Empty<ToolCharacter>()).Where(c=>c&&c.species!=ToolSpecies.CrawlerAlien).Distinct().ToArray():Array.Empty<ToolCharacter>();
    public static ToolTemplateSettings Defaults(ToolPerformanceTemplate type,ToolLibrary library)
    {
        var cast=Characters(library);
        return new ToolTemplateSettings{
            template=type,name=Names[(int)type],speaker=cast.FirstOrDefault(),partner=cast.Skip(1).FirstOrDefault(),
            sceneId=library?.scenes?.FirstOrDefault(s=>s.id=="archive")?.id??library?.scenes?.FirstOrDefault()?.id,
            opening=type==ToolPerformanceTemplate.Broadcast?"航行通告：前方航道正在维护，请保持当前航向，等待下一次导航更新。":type==ToolPerformanceTemplate.Conversation?"前方信号已经稳定。请确认中继站的接收状态。":"探测舱04，这里是前哨站。我们已接通视频链路，准备发送本次任务简报。",
            reply=type==ToolPerformanceTemplate.Conversation?"中继站收到，接收状态正常。我们会继续监测这片星域。":"本次简报发送完毕。祝航行顺利，前哨站结束通话。",
            question="是否接收前哨站简报？",acceptLabel="接收简报",declineLabel="暂不接收",
            acceptReply="收到，简报已经发送。请按计划继续航行。",declineReply="收到，我们将保留简报，等待下一次联络。",
            silenceReply="暂未收到你的回应，本次会议结束。"};
    }
    static string SpeakingAction(ToolLibrary library)=>library.Action("Idle_Talking_Loop")!=null?"Idle_Talking_Loop":library.Action("Idle_Loop")!=null?"Idle_Loop":null;
    public static List<string> Validate(ToolLibrary library,ToolTemplateSettings s)
    {
        var errors=new List<string>();
        if(s==null){errors.Add("请选择演出模板。");return errors;}
        if(!Enum.IsDefined(typeof(ToolPerformanceTemplate),s.template))errors.Add("未知演出模板。");
        if(string.IsNullOrWhiteSpace(s.name))errors.Add("请填写演出名称。");
        if(!library){errors.Add("请选择素材库。");return errors;}
        if(s.template==ToolPerformanceTemplate.Blank)return errors;
        var cast=Characters(library);
        if(!s.speaker||!cast.Contains(s.speaker))errors.Add("请选择素材库中的主讲人。");
        if(library.scenes==null||!library.scenes.Any(x=>x!=null&&x.id==s.sceneId))errors.Add("请选择可用的场景。");
        if(library.actions==null||SpeakingAction(library)==null)errors.Add("素材库需要说话或待机动作。");
        if(string.IsNullOrWhiteSpace(s.opening))errors.Add("请填写开场台词或播报内容。");
        if(s.template==ToolPerformanceTemplate.SingleCall||s.template==ToolPerformanceTemplate.Conversation)
            if(string.IsNullOrWhiteSpace(s.reply))errors.Add("请填写第二段台词。");
        if(s.template==ToolPerformanceTemplate.Conversation)
        {
            if(!s.partner||!cast.Contains(s.partner)||s.partner==s.speaker)errors.Add("双人对话需要两位不同的角色。");
            if(library.actions!=null&&library.Action("Idle_Loop")==null)errors.Add("双人对话需要待机动作供倾听者使用。");
        }
        if(s.template==ToolPerformanceTemplate.Question)
        {
            if(new[]{s.question,s.acceptLabel,s.declineLabel,s.acceptReply,s.declineReply,s.silenceReply}.Any(string.IsNullOrWhiteSpace))errors.Add("请填完整提问、两个选项和三种回应。");
            if(!ToolTimelineEditing.Finite(s.replySeconds)||s.replySeconds<1||s.replySeconds>120)errors.Add("等待回复时间应为 1–120 秒。");
        }
        return errors;
    }
    public static ToolPackage Create(ToolLibrary library,ToolTemplateSettings s)
    {
        var errors=Validate(library,s);if(errors.Count>0)throw new ArgumentException(string.Join("\n",errors));
        if(s.template==ToolPerformanceTemplate.Blank){var blank=ToolV2Authoring.CreateDraft(library);blank.name=s.name.Trim();return blank;}
        var package=ScriptableObject.CreateInstance<ToolPackage>();package.name=s.name.Trim();package.hideFlags=HideFlags.DontSave;package.library=library;
        package.initialPatience=s.template==ToolPerformanceTemplate.Question?Mathf.CeilToInt(s.replySeconds)+1:30;
        var graph=ScriptableObject.CreateInstance<ToolGraph>();graph.name=package.name+"_Graph";graph.hideFlags=HideFlags.DontSave;package.graph=graph;graph.entryNode="opening";
        bool dual=s.template==ToolPerformanceTemplate.Conversation;
        var opening=Unit(package,"opening",s.opening,s,dual,false,new Vector2(40,140));
        if(s.template==ToolPerformanceTemplate.Question)
        {
            var choice=new ToolDialogueSpan{kind=ToolDialogueKind.Choice,actorId="speaker",start=opening.unit.dialogue[0].start+opening.unit.dialogue[0].Duration+.3f,text=s.question.Trim(),patienceSeconds=s.replySeconds,
                choices=new[]{new ToolChoice{id="accept",text=s.acceptLabel.Trim()},new ToolChoice{id="decline",text=s.declineLabel.Trim()}}};
            opening.unit.dialogue.Add(choice);Extend(opening.unit,choice.start+choice.Duration+.2f);
            opening.unit.actions[0].duration=choice.start;
            opening.unit.actions.Add(new ToolActionSpan{actorId="speaker",actionId=library.Action("Idle_Loop")!=null?"Idle_Loop":SpeakingAction(library),start=choice.start,duration=opening.unit.duration-choice.start});
            var yes=Unit(package,"accept_reply",s.acceptReply,s,false,false,new Vector2(350,50));
            var no=Unit(package,"decline_reply",s.declineReply,s,false,false,new Vector2(350,250));
            var silence=Unit(package,"silence_reply",s.silenceReply,s,false,false,new Vector2(350,450));
            Link(opening,"accept",yes.id);Link(opening,"decline",no.id);Link(opening,"silence",silence.id);
            Finish(graph,yes,"accepted",new Vector2(670,50));Finish(graph,no,"declined",new Vector2(670,250));Finish(graph,silence,"unanswered",new Vector2(670,450));
        }
        else if(s.template==ToolPerformanceTemplate.Broadcast)Finish(graph,opening,"end",new Vector2(380,140));
        else
        {
            var reply=Unit(package,"reply",s.reply,s,dual,dual,new Vector2(360,140));Link(opening,"next",reply.id);Finish(graph,reply,"end",new Vector2(680,140));
        }
        return package;
    }
    static ToolGraphNode Unit(ToolPackage package,string id,string words,ToolTemplateSettings settings,bool dual,bool partnerSpeaks,Vector2 position)
    {
        var unit=ScriptableObject.CreateInstance<ToolUnit>();unit.name=id;unit.id=id;unit.hideFlags=HideFlags.DontSave;
        string subject=partnerSpeaks?"partner":"speaker",other=partnerSpeaks?"speaker":"partner";
        var dialogue=new ToolDialogueSpan{actorId=subject,start=.4f,text=words.Trim(),charactersPerSecond=18,hold=1.2f};
        unit.duration=Mathf.Max(4,dialogue.start+dialogue.Duration+.4f);unit.dialogue.Add(dialogue);
        unit.actors.Add(new ToolActorSpan{id="speaker",character=settings.speaker,duration=unit.duration});
        if(dual)unit.actors.Add(new ToolActorSpan{id="partner",character=settings.partner,duration=unit.duration});
        foreach(var actor in unit.actors)unit.actions.Add(new ToolActionSpan{actorId=actor.id,actionId=actor.id==subject?SpeakingAction(package.library):"Idle_Loop",duration=unit.duration});
        unit.scenes.Add(new ToolSceneSpan{sceneId=settings.sceneId,duration=unit.duration});
        if(dual)
        {
            unit.cameras.Add(new ToolCameraSpan{actorId=subject,partnerId=other,shot=ToolShot.TwoShot,duration=1});
            unit.cameras.Add(new ToolCameraSpan{actorId=subject,partnerId=other,shot=ToolShot.OverShoulder,start=1,duration=unit.duration-1});
        }
        else unit.cameras.Add(new ToolCameraSpan{actorId=subject,shot=ToolShot.Medium,duration=unit.duration});
        var node=new ToolGraphNode{id=id,kind=ToolNodeKind.Unit,unit=unit,position=position};package.graph.nodes.Add(node);return node;
    }
    static void Extend(ToolUnit unit,float duration)
    {
        unit.duration=duration;
        foreach(var actor in unit.actors)actor.duration=duration;
        foreach(var action in unit.actions)action.duration=duration;
        foreach(var scene in unit.scenes)scene.duration=duration;
        foreach(var camera in unit.cameras)camera.duration=duration-camera.start;
    }
    static void Link(ToolGraphNode source,string slot,string target)=>source.edges.Add(new ToolGraphEdge{slot=slot,target=target});
    static void Finish(ToolGraph graph,ToolGraphNode source,string id,Vector2 position)
    {graph.nodes.Add(new ToolGraphNode{id=id,kind=ToolNodeKind.End,position=position});Link(source,"next",id);}
}

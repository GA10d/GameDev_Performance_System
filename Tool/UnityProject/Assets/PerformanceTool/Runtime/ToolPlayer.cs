using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Astra.PerformanceTool
{
    public sealed class ToolPlayer : MonoBehaviour
    {
        public ToolPackage package;
        public bool autoPlay = true;
        public Camera lens;
        public string CurrentNodeId => node==null?"":node.id;
        public float CurrentTime => clock;
        public string CurrentSubtitle => subtitle;
        public Dictionary<string,int> Stats = new Dictionary<string,int>();
        readonly Dictionary<string,GameObject> live = new Dictionary<string,GameObject>();
        ToolGraphNode node;
        ToolStage stage;
        float clock,waiting,patienceStart;
        string subtitle="",speaker="";
        ToolDialogueSpan activeDialogue;
        bool paused,finished;
        int transitions;
        void Start(){if(autoPlay)Begin();}
        public void Begin()
        {
            if(!package||!package.graph||!package.library){Debug.LogError("Tool package is missing");return;}
            Stats.Clear();foreach(var stat in package.initialStats)Stats[stat.key]=stat.value;
            Stats["patience"]=package.initialPatience;
            if(!lens)lens=Camera.main;if(!lens)lens=new GameObject("Tool camera").AddComponent<Camera>();
            lens.clearFlags=CameraClearFlags.SolidColor;lens.backgroundColor=new Color(.035f,.055f,.045f);
            if(!lens.GetComponent<ToolScreenFx>())lens.gameObject.AddComponent<ToolScreenFx>();
            if(!stage){var root=new GameObject("Tool stage");stage=root.AddComponent<ToolStage>();}
            if(!stage.IsBuilt)stage.Build();
            transitions=0;finished=false;paused=false;Enter(package.graph.entryNode);
        }
        void Enter(string id)
        {
            if(++transitions>128){Debug.LogError("Tool graph exceeded transition limit");finished=true;return;}
            foreach(var actor in live.Values)if(actor){actor.SetActive(false);Destroy(actor);}
            live.Clear();
            node=package.graph.Node(id);clock=0;waiting=0;activeDialogue=null;subtitle="";speaker="";
            if(node==null){finished=true;return;}
            if(node.kind==ToolNodeKind.Condition)
            {
                Enter(ToolLogic.Exit(node,ToolLogic.Test(node.condition,Stats)?"yes":"no"));return;
            }
            if(node.kind==ToolNodeKind.End){finished=true;return;}
            if(node.kind==ToolNodeKind.GlobalChoice){waiting=Mathf.Min(node.patienceSeconds,Mathf.Max(0,Stats["patience"]));patienceStart=waiting;subtitle=node.prompt;return;}
            RenderUnit(0);
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Space))paused=!paused;
            if(node==null||finished||paused)return;
            float dt=Time.deltaTime;
            if(node.kind==ToolNodeKind.GlobalChoice)
            {
                waiting-=dt;if(waiting<=0)Choose("silence");return;
            }
            if(node.unit==null){finished=true;return;}
            clock+=dt;RenderUnit(clock);
            if(activeDialogue!=null&&activeDialogue.kind==ToolDialogueKind.Choice)
            {
                float start=activeDialogue.start+activeDialogue.Duration;
                waiting=Mathf.Max(0,start-clock);
                if(waiting<=0){Choose("silence");return;}
            }
            else if(clock>=node.unit.duration)Enter(ToolLogic.Exit(node,"next"));
        }
        public void Seek(float time) {if(node!=null&&node.kind==ToolNodeKind.Unit&&node.unit){clock=Mathf.Clamp(time,0,node.unit.duration);RenderUnit(clock);}}
        public void Choose(string slot)
        {
            if(node==null||finished)return;
            ToolChoice choice=null;
            if(node.kind==ToolNodeKind.GlobalChoice)choice=ToolLogic.Choice(node.choices,slot);
            else if(activeDialogue!=null&&activeDialogue.kind==ToolDialogueKind.Choice)choice=ToolLogic.Choice(activeDialogue.choices,slot);
            else return;
            if(choice!=null)
            {
                if(choice.hasCondition&&!ToolLogic.Test(choice.condition,Stats))return;
                ToolLogic.Apply(choice.effects,Stats);
            }
            if(slot!="silence"&&choice==null)return;
            int cost=Mathf.CeilToInt(node.kind==ToolNodeKind.GlobalChoice?patienceStart-waiting:clock-activeDialogue.start);
            Stats["patience"]=Mathf.Max(0,Stats["patience"]-Mathf.Max(0,cost));
            Enter(ToolLogic.Exit(node,slot));
        }
        void RenderUnit(float time)
        {
            var unit=node.unit;
            var activeActors=unit.actors.Where(a=>a.start<=time&&a.start+a.duration>time).ToArray();
            var ids=new HashSet<string>(activeActors.Select(a=>a.id));
            foreach(var old in live.Keys.ToArray())if(!ids.Contains(old)){Destroy(live[old]);live.Remove(old);}
            for(int i=0;i<activeActors.Length;i++)
            {
                var a=activeActors[i];if(!a.character)continue;
                if(!live.TryGetValue(a.id,out var go)||!go){go=ToolCharacterView.Create(a.character,stage.transform);live[a.id]=go;}
                go.transform.localPosition=new Vector3((i-(activeActors.Length-1)*.5f)*1.7f,0,0);
                var action=unit.actions.LastOrDefault(x=>x.actorId==a.id&&x.start<=time&&x.start+x.duration>time);
                go.GetComponent<ToolMotion>().Sample(action==null?"Idle_Loop":action.actionId,action==null?time:time-action.start,action==null?1:action.speed);
            }
            var scene=unit.scenes.LastOrDefault(s=>s.start<=time&&s.start+s.duration>time);
            var camera=unit.cameras.LastOrDefault(c=>c.start<=time&&c.start+c.duration>time);
            var currentActor=camera!=null?camera.actorId:activeActors.Length>0?activeActors[0].id:null;
            live.TryGetValue(currentActor??"",out var subject);
            live.TryGetValue(camera==null?"":camera.partnerId??"",out var partner);
            bool crawler=activeActors.Any(a=>a.id==currentActor&&a.character&&a.character.species==ToolSpecies.CrawlerAlien);
            ToolCameraMath.Apply(lens,camera,subject?subject.transform:null,partner?partner.transform:null,crawler,time);
            var fx=camera==null?ToolFx.None:camera.fx;lens.GetComponent<ToolScreenFx>().effect=fx;
            stage.Set(package.library.Scene(scene==null?"archive":scene.sceneId),fx,time);
            activeDialogue=unit.dialogue.LastOrDefault(d=>d.start<=time&&(d.kind==ToolDialogueKind.Choice||d.start+d.Duration>time));
            if(activeDialogue==null){subtitle="";speaker="";return;}
            var actor=unit.actors.FirstOrDefault(a=>a.id==activeDialogue.actorId);
            speaker=actor!=null&&actor.character?actor.character.displayName:"系统";
            if(activeDialogue.kind==ToolDialogueKind.Text)
            {
                int count=Mathf.Clamp(Mathf.FloorToInt((time-activeDialogue.start)*activeDialogue.charactersPerSecond),0,activeDialogue.text.Length);
                subtitle=activeDialogue.text.Substring(0,count);
            }
            else subtitle=activeDialogue.text;
        }
        void OnGUI()
        {
            if(!package||node==null)return;
            var box=new GUIStyle(GUI.skin.box){fontSize=18,alignment=TextAnchor.MiddleLeft,wordWrap=true};
            box.normal.textColor=new Color(.89f,.9f,.77f);
            var button=new GUIStyle(GUI.skin.button){fontSize=16};
            GUI.Box(new Rect(20,Screen.height-160,Screen.width-40,140),"",box);
            GUI.Label(new Rect(40,Screen.height-153,Screen.width-80,26),"ASTRA / "+node.id+"     "+speaker,box);
            GUI.Label(new Rect(40,Screen.height-119,Screen.width-80,46),subtitle,box);
            bool choosing=node.kind==ToolNodeKind.GlobalChoice||activeDialogue!=null&&activeDialogue.kind==ToolDialogueKind.Choice;
            if(choosing&&!finished)
            {
                var choices=node.kind==ToolNodeKind.GlobalChoice?node.choices:activeDialogue.choices;
                for(int i=0;i<choices.Length;i++)
                {
                    var c=choices[i];bool enabled=!c.hasCondition||ToolLogic.Test(c.condition,Stats);
                    GUI.enabled=enabled;if(GUI.Button(new Rect(40+i*235,Screen.height-66,225,36),c.text,button))Choose(c.id);
                }
                GUI.enabled=true;
                GUI.Label(new Rect(Screen.width-255,Screen.height-65,225,36),"沉默倒计时 "+Mathf.CeilToInt(waiting)+"s",box);
            }
            if(finished&&GUI.Button(new Rect(Screen.width-150,20,130,35),"重新播放",button))Begin();
        }
    }
}

using UnityEngine;

namespace Astra.Performance
{
    public sealed class PerformanceView : MonoBehaviour
    {
        public PerformanceDirector director;
        Font chinese,mono;
        GUIStyle label,small,body,title,button,captionStyle;
        readonly Color cream=new Color(.83f,.81f,.67f),green=new Color(.62f,.76f,.52f),muted=new Color(.46f,.52f,.43f),dark=new Color(.055f,.077f,.065f,.96f);
        public Rect FeedRect=>new Rect(100,143,1240,500);
        bool initialized;
        void Init()
        {
            chinese=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei UI","Microsoft YaHei","Noto Sans CJK SC","Arial"},24);
            mono=Font.CreateDynamicFontFromOSFont(new[]{"Consolas","Courier New"},20);
            label=new GUIStyle(GUI.skin.label){font=chinese,fontSize=18,normal={textColor=cream},padding=new RectOffset(0,0,0,0)};
            small=new GUIStyle(label){font=mono,fontSize=15,normal={textColor=muted}};
            body=new GUIStyle(label){fontSize=25,wordWrap=true,normal={textColor=new Color(.89f,.88f,.76f)}};
            title=new GUIStyle(label){font=mono,fontSize=39,fontStyle=FontStyle.Bold};
            button=new GUIStyle(label){fontSize=17,alignment=TextAnchor.MiddleCenter,normal={textColor=cream}};
            captionStyle=new GUIStyle(small){fontSize=13};initialized=true;
        }
        static void Fill(Rect r,Color c){Color old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        void Text(Rect r,string s,GUIStyle style){GUI.Label(r,s,style);}
        bool Button(Rect r,string s,bool active=false)
        {
            bool hover=r.Contains(Event.current.mousePosition);
            Fill(r,active?new Color(.22f,.29f,.19f,.95f):hover?new Color(.20f,.24f,.19f,.98f):new Color(.085f,.115f,.09f,.94f));
            Fill(new Rect(r.x,r.yMax-1,r.width,1),active?green:muted*.6f);
            return GUI.Button(r,s,button);
        }
        void OnGUI()
        {
            if(!initialized)Init();
            float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);
            float ox=(Screen.width-1440*scale)*.5f,oy=(Screen.height-900*scale)*.5f;
            GUI.matrix=Matrix4x4.TRS(new Vector3(ox,oy,0),Quaternion.identity,new Vector3(scale,scale,1));
            var session=director.Session;
            Fill(new Rect(60,24,1320,852),new Color(.04f,.057f,.047f,.79f));
            Fill(new Rect(60,24,4,64),green);
            Text(new Rect(88,37,260,55),"A S T R A",title);
            Text(new Rect(369,40,600,30),"先遣观测计划   /   信号档案",label);
            Text(new Rect(369,69,700,22),"HUMAN TELEMETRY  •  RECEIVE / STORE / FORWARD",captionStyle);
            Text(new Rect(1078,42,278,25),"CACHE  /  03 PACKETS",small);
            Text(new Rect(1078,68,278,23),"单程延迟  17 年",new GUIStyle(label){fontSize=15,normal={textColor=green}});
            Fill(new Rect(88,99,1264,1),muted*.55f);
            string[] tabs={"01   林 / 047 · 维修员","02   赫 / 112 · 旧航路","03   余 / 203 · 未登记"};
            for(int i=0;i<3;i++)if(Button(new Rect(100+i*313,109,303,30),tabs[i],director.Channel==i))director.StartChannel(i);
            if(Button(new Rect(1050,109,290,30),"A  自动播放  "+(session!=null && session.Auto?"开":"关"),session!=null&&session.Auto))session.Auto=!session.Auto;
            Rect video=FeedRect;
            Fill(new Rect(video.x-2,video.y-2,video.width+4,video.height+4),new Color(.3f,.33f,.25f));
            if(director.Feed)GUI.DrawTexture(video,director.Feed,ScaleMode.ScaleAndCrop,false);
            if(session==null || director.Error!=null){Text(new Rect(130,220,1050,160),director.Error??"正在建立通信缓存……",body);return;}
            var beat=session.Beat;
            string id=beat==null?"---":new[]{"047","112","203"}[(int)beat.actor];
            string name=beat==null?"缓存结束":new[]{"林","赫","余"}[(int)beat.actor];
            Fill(new Rect(116,158,212,54),new Color(.035f,.059f,.044f,.84f));
            Text(new Rect(130,167,195,23),"●  REC / PROBE "+id,new GUIStyle(small){normal={textColor=green}});
            Text(new Rect(130,188,195,18),"已缓存影像 · 非实时",new GUIStyle(label){fontSize=12,normal={textColor=cream}});
            Text(new Rect(1138,165,190,22),"SIGNAL  074 / 100",captionStyle);
            Text(new Rect(118,608,750,22),"ASTRA LINK //  生物观测单元  " + id + "   /   ARCHIVE 19.04",captionStyle);
            for(int i=0;i<20;i++)Fill(new Rect(1168+i*7,615-Mathf.Abs(Mathf.Sin((session.IsSpeaking?session.BeatTime:0)*12+i*.72f))*15,3,16),new Color(.53f,.67f,.43f,.7f));
            if((beat!=null && beat.systemLine) || session.Phase==SessionPhase.Complete || session.Phase==SessionPhase.Cancelled)
            {
                Fill(video,new Color(.028f,.048f,.036f,.97f));
                Text(new Rect(490,306,610,55),"END OF TRANSMISSION",title);
                Text(new Rect(551,370,650,35),"影像播放完毕  /  没有新的实时信号",label);
                Text(new Rect(560,419,550,30),"STORE  →  QUEUE  →  17 YEARS",small);
                name="系统";id="QUEUE";
            }
            if(director.IsPaused)
            {
                Fill(video,new Color(.025f,.04f,.03f,.60f));
                Text(new Rect(520,315,500,50),"传输暂停",new GUIStyle(title){font=chinese,fontSize=36});
                if(Button(new Rect(577,384,286,45),"P / Esc  继续播放"))director.TogglePause();
            }
            Fill(new Rect(100,657,1240,139),dark);
            Fill(new Rect(100,657,3,139),green);
            Text(new Rect(121,670,140,25),name+"  /  "+id,new GUIStyle(label){fontSize=18,normal={textColor=green}});
            Text(new Rect(280,670,950,22),session.Phase==SessionPhase.Choice?"选择你的回信；回复会加入发送队列。":session.Phase==SessionPhase.Complete?"回信已加入队列。下一次回应，也许要等三十四年。":"",new GUIStyle(label){fontSize=15,normal={textColor=muted}});
            if(session.Phase==SessionPhase.Cancelled)Text(new Rect(122,711,1150,72),"通信已中断。按 R 重新接收缓存。",body);
            else if(session.Phase==SessionPhase.Complete)Text(new Rect(122,711,1150,72),"待发送："+(string.IsNullOrEmpty(session.PendingReply)?"尚未撰写回信":session.PendingReply),body);
            else Text(new Rect(122,707,1160,78),session.VisibleText,body);
            if(session.Phase==SessionPhase.Choice)
            {
                for(int i=0;i<beat.choices.Length;i++)if(Button(new Rect(110+i*615,810,600,47),(i+1)+"  "+beat.choices[i].text))session.Choose(i);
            }
            else
            {
                if(Button(new Rect(100,810,335,43),session.Phase==SessionPhase.Complete?"R  重播通信":"Space  "+(session.Phase==SessionPhase.Lead||session.Phase==SessionPhase.Typing?"显示整句":session.Phase==SessionPhase.Hold?"等待片刻…":"继续")))
                {if(session.Phase==SessionPhase.Complete)director.StartChannel(director.Channel);else session.Advance();}
                if(Button(new Rect(448,810,149,43),"P  暂停"))director.TogglePause();
                if(Button(new Rect(610,810,227,43),"V  "+(director.Stylized?"复古画面":"清晰画面")))director.ToggleStyle();
                if(Button(new Rect(850,810,194,43),"M  "+(director.Sound.Muted?"声音关闭":"声音开启")))director.Sound.SetMuted(!director.Sound.Muted);
                if(Button(new Rect(1057,810,283,43),"F1  演出检查",director.Laboratory))director.Laboratory=!director.Laboratory;
            }
            Text(new Rect(102,859,1150,18),"R 重播    1–3 选择通道    F12 截图    影像来自各自的单人探测舱",new GUIStyle(label){fontSize=11,normal={textColor=muted}});
            if(director.Laboratory)DrawLab(session,beat);
            GUI.matrix=Matrix4x4.identity;
        }
        void DrawLab(PerformanceSession s,PerformanceBeat b)
        {
            Rect r=new Rect(1010,227,309,346);Fill(r,new Color(.025f,.04f,.033f,.97f));
            Text(new Rect(1028,242,279,30),"PERFORMANCE / INSPECT",small);
            string desc="状态  "+s.Phase+"\n节拍  "+(b?.id??"-")+"\n姿态  "+(b?.pose.ToString()??"-")+"\n视线  "+(b?.gaze.ToString()??"-")+"\n手势  "+(b?.gesture.ToString()??"-")+"\n时钟  "+s.BeatTime.ToString("F2")+" s";
            Text(new Rect(1028,279,265,159),desc,new GUIStyle(label){fontSize=16});
            if(Button(new Rect(1028,440,272,32),director.FullBody?"查看胸像":"查看全身 / 固定足底"))director.FullBody=!director.FullBody;
            if(Button(new Rect(1028,481,272,32),director.ReducedMotion?"恢复微动作":"减弱动态",director.ReducedMotion))director.ToggleReducedMotion();
            if(Button(new Rect(1028,522,272,32),"中断会话 / 检查清理"))director.Interrupt();
        }
        void OnDestroy(){if(chinese)Destroy(chinese);if(mono)Destroy(mono);}
    }
}

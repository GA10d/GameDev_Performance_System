using UnityEngine;

namespace Astra.PerformanceV2
{
    public sealed class ArchiveView:MonoBehaviour
    {
        public ArchiveDirector director;
        Font chinese,mono;GUIStyle small,body,title,label,button;
        readonly Color ivory=new Color(.86f,.84f,.71f),muted=new Color(.49f,.56f,.47f),amber=new Color(.81f,.60f,.30f),dark=new Color(.022f,.035f,.03f,.96f);
        static readonly string[] ActionsZh={"待机 / 呼吸","站立说话","走路","正式步行","取桌上物件","操作设备","跪姿维修","坐下","坐姿待机","坐姿说话","起身","推移重物","蹲姿待机","胸口受击","头部受击","小跑","舞蹈 / 极限姿态"};
        void Setup()
        {
            if(small!=null)return;
            chinese=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},22);mono=Font.CreateDynamicFontFromOSFont(new[]{"Consolas","Arial"},16);
            label=new GUIStyle(GUI.skin.label){font=chinese,fontSize=16,normal={textColor=ivory},wordWrap=false};
            small=new GUIStyle(label){font=mono,fontSize=13,normal={textColor=muted}};
            title=new GUIStyle(label){font=mono,fontSize=30,normal={textColor=ivory}};
            body=new GUIStyle(label){fontSize=24,wordWrap=true};
            button=new GUIStyle(label){fontSize=15,alignment=TextAnchor.MiddleCenter};
        }
        void Fill(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
        void Text(Rect r,string s,GUIStyle style){GUI.Label(r,s,style);}
        bool Button(Rect r,string s,bool active=false)
        {
            bool hover=r.Contains(Event.current.mousePosition);Fill(r,active?new Color(.24f,.29f,.21f):hover?new Color(.17f,.21f,.17f):new Color(.07f,.095f,.075f));
            if(active)Fill(new Rect(r.x,r.y,3,r.height),amber);GUI.Label(r,s,button);return GUI.Button(r,GUIContent.none,GUIStyle.none);
        }
        void OnGUI()
        {
            Setup();var d=director;if(!d||d.Error!=null){GUI.Label(new Rect(30,30,1000,100),d?d.Error:"No director");return;}
            if(!d.Hud)return;
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);float ox=(Screen.width-1600*scale)/2,oy=(Screen.height-900*scale)/2;
            GUI.matrix=Matrix4x4.TRS(new Vector3(ox,oy,0),Quaternion.identity,new Vector3(scale,scale,1));
            if(ox>0){Fill(new Rect(-ox/scale,-oy/scale,ox/scale,Screen.height/scale),dark);Fill(new Rect(1600,-oy/scale,ox/scale,Screen.height/scale),dark);}
            Fill(new Rect(-ox/scale,-oy/scale,Screen.width/scale,109+oy/scale),dark);
            Fill(new Rect(0,107,1600,2),new Color(.32f,.37f,.28f));
            Text(new Rect(40,19,600,40),"ASTRA / THE RELAY",title);
            Text(new Rect(42,65,960,24),"ARCHIVE 017  /  旧中继站录像  /  单向延迟 17 年",label);
            Text(new Rect(1155,24,410,22),"QUATERNIUS CAST STUDY   /   V.02.1",small);
            Text(new Rect(1172,60,400,23),d.Gallery?"角色 · 动作 · 镜头试验台":"历史记录回放 · 非实时连线",new GUIStyle(label){normal={textColor=amber}});
            // Thin frame keeps the picture dominant; labels sit outside faces.
            Fill(new Rect(0,109,16,566),dark);Fill(new Rect(1584,109,16,566),dark);
            Fill(new Rect(30,127,330,31),new Color(.025f,.045f,.03f,.8f));
            Text(new Rect(41,131,350,24),ArchiveCamera.Names[(int)d.cameraRig.Kind],new GUIStyle(label){fontSize=14});
            Text(new Rect(1260,133,310,20),"REC  /  "+(d.Gallery?d.GalleryTime:d.BeatTime).ToString("00.0")+" s",small);
            var a=d.actors[d.Focus];
            Fill(new Rect(0,675,1600,225+oy/scale),dark);
            Fill(new Rect(40,701,3,79),a.identityColor);
            Text(new Rect(58,699,260,28),a.displayName+" / "+a.species,new GUIStyle(label){fontSize=20,normal={textColor=a.identityColor}});
            Text(new Rect(58,735,267,34),a.role,new GUIStyle(label){fontSize=13,normal={textColor=muted}});
            Text(new Rect(344,696,1175,81),d.Ended?"录像结束。四个人的声音，抵达时只剩下这一份记录。":d.VisibleText,body);
            if(!d.Gallery)
            {
                float w=1520f/d.sequence.beats.Length;
                for(int i=0;i<d.sequence.beats.Length;i++)
                {
                    var r=new Rect(40+i*w,796,w-3,8);Fill(r,i<d.Index?amber:i==d.Index?ivory:new Color(.18f,.23f,.18f));
                    if(GUI.Button(new Rect(r.x,784,w-3,26),GUIContent.none,GUIStyle.none))d.Seek(i);
                }
                Text(new Rect(42,812,490,26),(d.Index+1).ToString("00")+" / "+d.sequence.beats.Length+"   "+d.Beat.chapter,label);
                if(Button(new Rect(546,821,152,39),"← 上一镜"))d.Seek(Mathf.Max(0,d.Index-1));
                if(Button(new Rect(706,821,162,39),d.Paused?"Space  继续":"Space  暂停"))d.TogglePause();
                if(Button(new Rect(876,821,152,39),"下一镜 →"))d.Seek(Mathf.Min(d.sequence.beats.Length-1,d.Index+1));
                if(Button(new Rect(1036,821,163,39),"R  重播录像")){d.Seek(0);if(d.Paused)d.TogglePause();}
                if(Button(new Rect(1207,821,353,39),"Tab  打开角色 / 动作 / 镜头试验台"))d.SetGallery(true);
            }
            else DrawGallery();
            Text(new Rect(42,875,1500,18),"1–4 人物     Tab 模式     Q / E 动作     C 镜头     V 清晰 / 复古     H 隐藏界面     F12 截图     Esc 暂停",new GUIStyle(small){fontSize=12});
            if(d.Paused)Text(new Rect(850,65,285,24),"已暂停 · Space 继续",new GUIStyle(label){normal={textColor=amber}});
            if(d.Gallery)DrawControls();
            GUI.matrix=Matrix4x4.identity;
        }
        void DrawGallery()
        {
            var d=director;for(int i=0;i<4;i++)if(Button(new Rect(40+i*224,801,215,48),(i+1)+"  "+d.actors[i].displayName+" · "+d.actors[i].species,d.Focus==i))d.SelectActor(i);
            if(Button(new Rect(950,804,180,43),d.Paused?"继续动作":"暂停动作"))d.TogglePause();
            if(Button(new Rect(1140,804,188,43),d.ReducedMotion?"轻微动态":"正常动态",d.ReducedMotion))d.ToggleReducedMotion();
            if(Button(new Rect(1340,804,220,43),"返回故事录像"))d.SetGallery(false);
        }
        void DrawControls()
        {
            var d=director;
            Fill(new Rect(1200,109,384,566),new Color(.024f,.042f,.033f,1));
            Text(new Rect(1243,191,290,26),"PERFORMANCE DESK",small);
            Text(new Rect(1243,221,285,30),"动作 / "+(d.ActionIndex+1)+" of "+d.sequence.galleryActions.Length,label);
            if(Button(new Rect(1242,254,39,41),"‹"))d.SelectAction((d.ActionIndex+d.sequence.galleryActions.Length-1)%d.sequence.galleryActions.Length);
            Text(new Rect(1290,263,221,28),ActionsZh[d.ActionIndex],new GUIStyle(label){fontSize=16});
            if(Button(new Rect(1499,254,39,41),"›"))d.SelectAction((d.ActionIndex+1)%d.sequence.galleryActions.Length);
            Text(new Rect(1245,304,289,24),d.sequence.galleryActions[d.ActionIndex],new GUIStyle(small){fontSize=11});
            for(int i=0;i<9;i++)if(Button(new Rect(1242+(i%2)*151,344+(i/2)*42,145,35),ArchiveCamera.Names[i].Split('·')[0],(int)d.cameraRig.Kind==i))d.SelectShot((ShotKind)i);
            if(Button(new Rect(1242,571,92,35),"0.5 ×",d.PlaybackSpeed==.5f))d.PlaybackSpeed=.5f;
            if(Button(new Rect(1342,571,92,35),"1 ×",d.PlaybackSpeed==1))d.PlaybackSpeed=1;
            if(Button(new Rect(1442,571,96,35),"1.5 ×",d.PlaybackSpeed==1.5f))d.PlaybackSpeed=1.5f;
            Text(new Rect(1245,621,291,24),"CC0 mesh + humanoid animation",new GUIStyle(small){fontSize=11});
        }
        void OnDestroy(){if(chinese)Destroy(chinese);if(mono)Destroy(mono);}
    }
}

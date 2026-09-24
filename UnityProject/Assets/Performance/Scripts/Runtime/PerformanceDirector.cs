using System;
using System.Linq;
using UnityEngine;

namespace Astra.Performance
{
    public sealed class PerformanceDirector : MonoBehaviour
    {
        public PerformanceSequence sequence;
        public ActorPerformance[] actors;
        public Camera remoteCamera;
        public Camera cabinCamera;
        public Light remoteKey;
        public Light remoteFill;
        public Material screenMaterial;
        public SignalRenderer cabinGrade,remoteGrade;
        public PerformanceSession Session { get; private set; }
        public RenderTexture Feed { get; private set; }
        public PerformanceAudio Sound { get; private set; }
        public bool ReducedMotion { get; private set; }
        public bool Stylized { get; private set; }=true;
        public bool Laboratory { get; set; }
        public bool FullBody { get; set; }
        public string Error {get;private set;}
        public int Channel {get;private set;}
        float cameraBlend, lightBlend;
        bool focused=true;
        bool userPaused;
        readonly MaterialPropertyBlock styleProperties=new MaterialPropertyBlock();
        public bool IsPaused=>userPaused || !focused;
        public event Action CueChanged;

        void Awake()
        {
            Sound=gameObject.AddComponent<PerformanceAudio>();
            Feed=new RenderTexture(864,432,24,RenderTextureFormat.ARGB32){name="Remote communication",filterMode=FilterMode.Point};Feed.Create();
            remoteCamera.targetTexture=Feed;
            if(screenMaterial)screenMaterial.mainTexture=Feed;
            try
            {
                foreach(var actor in actors)actor.Bind();
                Session=new PerformanceSession(sequence.beats);
                Session.Entered+=OnEntered;
                Session.Revealed+=OnRevealed;
                Session.Ended+=OnEnded;
                StartChannel(0);
            }
            catch(Exception ex){Error=ex.Message;Debug.LogException(ex);}
        }
        public void StartChannel(int channel)
        {
            if(Session==null)return;
            Channel=Mathf.Clamp(channel,0,2);userPaused=false;
            Sound.StopVoice();foreach(var a in actors)a.ResetPerformance();
            Session.Start(new[]{sequence.entryId,"he01","yu01"}[Channel]);
            UpdatePause();
        }
        void OnEntered(PerformanceBeat beat)
        {
            if(!beat.systemLine)Channel=(int)beat.actor;
            Sound.StopVoice();
            foreach(var a in actors)
            {
                bool active=a.actorId==beat.actor;a.gameObject.SetActive(active);
                a.Speaking=false;if(active)a.Apply(beat);
            }
            CueChanged?.Invoke();
        }
        void OnRevealed(string ch)
        {
            if(!Session.IsSpeaking || Session.Beat.systemLine || char.IsPunctuation(ch,0) || string.IsNullOrWhiteSpace(ch))return;
            actors[(int)Session.Beat.actor].Pulse();Sound.Pulse(Session.Beat.actor,ch);
        }
        void OnEnded(){Sound.StopVoice();foreach(var a in actors)a.ResetPerformance();}
        void Update()
        {
            if(Session==null)return;
            if(Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) Session.Advance();
            if(Input.GetKeyDown(KeyCode.P)||Input.GetKeyDown(KeyCode.Escape))TogglePause();
            if(Input.GetKeyDown(KeyCode.R))StartChannel(Channel);
            if(Input.GetKeyDown(KeyCode.A))Session.Auto=!Session.Auto;
            if(Input.GetKeyDown(KeyCode.F1))Laboratory=!Laboratory;
            if(Input.GetKeyDown(KeyCode.V))ToggleStyle();
            if(Input.GetKeyDown(KeyCode.M))Sound.SetMuted(!Sound.Muted);
            if(Input.GetKeyDown(KeyCode.Alpha1))ChooseOrChannel(0);
            if(Input.GetKeyDown(KeyCode.Alpha2))ChooseOrChannel(1);
            if(Input.GetKeyDown(KeyCode.Alpha3) && Session.Phase!=SessionPhase.Choice)StartChannel(2);
            if(Input.GetKeyDown(KeyCode.F12))GetComponent<PerformanceCapture>().CaptureManual();
            Session.Tick(Time.unscaledDeltaTime);
            foreach(var a in actors){a.Speaking=Session.IsSpeaking && !Session.Beat.systemLine && a.actorId==Session.Beat?.actor;a.Paused=IsPaused;}
            if(IsPaused || Session.Beat==null)return;
            var beat=Session.Beat;
            float blend=1-Mathf.Exp(-Time.unscaledDeltaTime*2.2f);
            cameraBlend=Mathf.Lerp(cameraBlend,ReducedMotion?0:beat.closeUp,blend);
            lightBlend=Mathf.Lerp(lightBlend,beat.alarm?1:0,blend);
            remoteCamera.transform.position=new Vector3(30,FullBody?1.03f:1.42f,FullBody?-3.65f:Mathf.Lerp(-2.48f,-1.88f,cameraBlend));
            remoteCamera.transform.LookAt(new Vector3(30,FullBody?.93f:1.36f,0));
            remoteCamera.fieldOfView=FullBody?33:30;
            remoteKey.color=Color.Lerp(new Color(1,.82f,.61f),new Color(1,.44f,.25f),lightBlend*.62f);
            remoteFill.color=Color.Lerp(new Color(.52f,.70f,.64f),new Color(.67f,.42f,.30f),lightBlend*.4f);
        }
        void ChooseOrChannel(int n){if(Session.Phase==SessionPhase.Choice)Session.Choose(n);else StartChannel(n);}
        public void TogglePause(){userPaused=!userPaused;UpdatePause();}
        void OnApplicationFocus(bool hasFocus){focused=Environment.GetCommandLineArgs().Contains("--performance-qa") || hasFocus;if(Session!=null)UpdatePause();}
        void UpdatePause(){Session.Paused=IsPaused;Sound.SetPaused(IsPaused);foreach(var a in actors)a.Paused=IsPaused;}
        public void ToggleStyle()
        {
            Stylized=!Stylized;cabinGrade.stylized=remoteGrade.stylized=Stylized;
            Feed.filterMode=Stylized?FilterMode.Point:FilterMode.Bilinear;
            foreach(var r in actors.SelectMany(a=>a.GetComponentsInChildren<Renderer>(true)))
            {
                r.GetPropertyBlock(styleProperties);styleProperties.SetFloat("_Stylized",Stylized?1:0);r.SetPropertyBlock(styleProperties);
            }
        }
        public void ToggleReducedMotion(){ReducedMotion=!ReducedMotion;foreach(var a in actors)a.ReducedMotion=ReducedMotion;}
        public void Interrupt()
        {
            Session.Cancel();userPaused=false;UpdatePause();cameraBlend=0;lightBlend=0;
            remoteKey.color=new Color(1,.82f,.61f);remoteFill.color=new Color(.52f,.70f,.64f);
            remoteCamera.transform.position=new Vector3(30,1.42f,-2.48f);remoteCamera.transform.LookAt(new Vector3(30,1.36f,0));remoteCamera.fieldOfView=30;
        }
        void OnDestroy()
        {
            if(Session!=null){Session.Entered-=OnEntered;Session.Revealed-=OnRevealed;Session.Ended-=OnEnded;}
            if(remoteCamera)remoteCamera.targetTexture=null;
            if(Feed){Feed.Release();Destroy(Feed);}
        }
    }
}

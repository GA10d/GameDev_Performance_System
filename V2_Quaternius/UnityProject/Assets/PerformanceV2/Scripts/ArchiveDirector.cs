using UnityEngine;

namespace Astra.PerformanceV2
{
    public sealed class ArchiveDirector:MonoBehaviour
    {
        public ArchiveSequence sequence;
        public CastActor[] actors;
        public ArchiveCamera cameraRig;
        public ArchiveGrade grade;
        public GameObject stool;
        public GameObject[] galleryOccluders;
        public GameObject repairKit,actionTable,pushCrate,pickup;
        public bool Paused {get;private set;}
        public bool Gallery {get;private set;}
        public bool Ended {get;private set;}
        public bool ReducedMotion {get;private set;}
        public bool Hud=true;
        public int Index {get;private set;}
        public int Focus {get;private set;}
        public int ActionIndex {get;private set;}
        public float BeatTime {get;private set;}
        public float GalleryTime {get;private set;}
        public float PlaybackSpeed=1;
        [System.NonSerialized] public float AuditStep=-1;
        public string Error {get;private set;}
        public ArchiveBeat Beat=>sequence.beats[Index];
        public string VisibleText
        {
            get
            {
                if(Gallery)return "动作试演 · Q / E 切换，Space 暂停；可单独比较体型与镜头。";
                // Complete subtitles remain available while scrubbing or paused.
                return BeatTime>=Beat.lead||Paused?Beat.subtitle:"";
            }
        }
        void Start()
        {
            Error=sequence.Validate();if(Error!=null){Debug.LogError(Error);enabled=false;return;}
            foreach(var a in actors)a.Initialize();Seek(0);
        }
        void Update()
        {
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            cameraRig.lens.pixelRect=new Rect((Screen.width-1600*scale)/2+16*scale,(Screen.height-900*scale)/2+225*scale,(Gallery?1184:1568)*scale,566*scale);
            if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.P)||Input.GetKeyDown(KeyCode.Escape))TogglePause();
            if(Input.GetKeyDown(KeyCode.Tab))SetGallery(!Gallery);
            if(Input.GetKeyDown(KeyCode.R)){Seek(0);Paused=false;}
            if(Input.GetKeyDown(KeyCode.RightArrow))Seek(Mathf.Min(Index+1,sequence.beats.Length-1));
            if(Input.GetKeyDown(KeyCode.LeftArrow))Seek(Mathf.Max(Index-1,0));
            if(Input.GetKeyDown(KeyCode.V))grade.Stylized=!grade.Stylized;
            if(Input.GetKeyDown(KeyCode.H))Hud=!Hud;
            if(Input.GetKeyDown(KeyCode.F12))GetComponent<ArchiveQA>().CaptureManual();
            for(int i=0;i<4;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))){SetGallery(true);SelectActor(i);}
            if(Gallery&&Input.GetKeyDown(KeyCode.Q))SelectAction((ActionIndex+sequence.galleryActions.Length-1)%sequence.galleryActions.Length);
            if(Gallery&&Input.GetKeyDown(KeyCode.E))SelectAction((ActionIndex+1)%sequence.galleryActions.Length);
            if(Gallery&&Input.GetKeyDown(KeyCode.C))SelectShot((ShotKind)(((int)cameraRig.Kind+1)%9));
            float dt=Paused?0:(AuditStep>0?AuditStep:Mathf.Min(Time.unscaledDeltaTime,.05f))*PlaybackSpeed;
            if(!Gallery&&!Ended)
            {
                BeatTime+=dt;
                if(Beat.move)actors[Focus].transform.position=Vector3.Lerp(Beat.start,Beat.end,Mathf.Clamp01(BeatTime/Beat.duration));
                if(BeatTime>=Beat.duration){if(Index+1<sequence.beats.Length)EnterBeat(Index+1,false);else Ended=true;}
            }
            if(Gallery)GalleryTime+=dt;
            foreach(var a in actors)a.Tick(dt);
            if(Gallery&&sequence.galleryActions[ActionIndex]=="PickUp_Table"&&GalleryTime>.35f)
            {pickup.transform.SetParent(actors[Focus].Animator.GetBoneTransform(HumanBodyBones.RightHand),false);pickup.transform.localPosition=Vector3.zero;pickup.transform.localRotation=Quaternion.identity;}
            cameraRig.Tick(dt);
        }
        public void Seek(int index){EnterBeat(Mathf.Clamp(index,0,sequence.beats.Length-1),true);}
        void EnterBeat(int index,bool reset)
        {
            Gallery=false;Ended=false;Index=index;BeatTime=0;Focus=Beat.actor;cameraRig.Gallery=false;
            cameraRig.ClosingWide=index==sequence.beats.Length-1;
            ResetPickup();
            foreach(var prop in galleryOccluders)prop.SetActive(true);
            actionTable.SetActive(false);pushCrate.SetActive(false);repairKit.SetActive(index==5);
            if(index==5)repairKit.transform.position=actors[2].Home+new Vector3(0,0,-.58f);
            if(reset)
            {
                foreach(var a in actors){a.ResetMark();a.Play(sequence.Clip("Idle_Loop"),1,true);}
                // Reconstruct prior blocking for arbitrary timeline seeks.
                for(int n=0;n<index;n++)if(sequence.beats[n].move)
                {var b=sequence.beats[n];actors[b.actor].transform.position=b.end;}
            }
            foreach(var a in actors)
            {
                a.Expression=Accent.None;a.GazeWeight=.35f;a.GazeTarget=actors[Focus].transform.position+Vector3.up*1.6f;
                if(a!=actors[Focus])
                {
                    a.Play(sequence.Clip("Idle_Loop"));
                    Face(a,actors[Focus].transform.position,22);
                }
            }
            var current=actors[Focus];current.Play(sequence.Clip(Beat.action),Beat.speed,reset);current.Expression=Beat.accent;
            current.TargetYaw=current.HomeYaw;
            if(Beat.listener>=0&&!Beat.seated)Face(current,actors[Beat.listener].transform.position,55);
            current.GazeWeight=Beat.listener>=0?.75f:.05f;
            current.GazeTarget=Beat.listener>=0?actors[Beat.listener].transform.position+Vector3.up*1.6f:current.transform.position+Vector3.back*2+Vector3.up*1.6f;
            if(Beat.move){current.transform.position=Beat.start;current.TargetYaw=Quaternion.LookRotation(Beat.end-Beat.start).eulerAngles.y;}
            if(reset)current.transform.rotation=Quaternion.Euler(0,current.TargetYaw,0);
            SetupSeat(Beat.seated);
            cameraRig.Seated=Beat.action=="Sitting_Talking_Loop"||Beat.action=="Sitting_Idle_Loop";
            cameraRig.SetShot(Beat.shot,current,Beat.listener>=0?actors[Beat.listener]:null,true);
            UpdateVisibility();
        }
        public void TogglePause(){Paused=!Paused;}
        public void ToggleReducedMotion(){ReducedMotion=!ReducedMotion;cameraRig.ReducedMotion=ReducedMotion;foreach(var a in actors)a.ReducedMotion=ReducedMotion;}
        public void SetGallery(bool value)
        {
            if(value==Gallery)return;
            if(!value){Seek(Index);PlaybackSpeed=1;return;}
            Gallery=true;Ended=false;GalleryTime=0;Paused=false;cameraRig.Gallery=true;
            foreach(var prop in galleryOccluders)prop.SetActive(false);
            SelectActor(Focus);
        }
        public void SelectActor(int index)
        {
            Focus=Mathf.Clamp(index,0,actors.Length-1);
            SelectAction(ActionIndex);SelectShot(ShotKind.Tracking);
        }
        public void SelectAction(int index)
        {
            ActionIndex=Mathf.Clamp(index,0,sequence.galleryActions.Length-1);GalleryTime=0;
            ResetPickup();
            foreach(var a in actors){a.ResetMark();a.Play(sequence.Clip("Idle_Loop"),1,true);a.GazeWeight=0;}
            actors[Focus].Play(sequence.Clip(sequence.galleryActions[ActionIndex]),1,true);
            cameraRig.Seated=sequence.galleryActions[ActionIndex]=="Sitting_Idle_Loop"||sequence.galleryActions[ActionIndex]=="Sitting_Talking_Loop";
            SelectShot(cameraRig.Kind);
        }
        public void SelectShot(ShotKind shot)
        {
            if(Gallery)
            {
                foreach(var a in actors)a.ResetMark();
                bool group=shot==ShotKind.Establishing||shot==ShotKind.Insert;
                actors[Focus].transform.position=group?actors[Focus].Home:new Vector3(0,0,.3f);
                if(shot==ShotKind.TwoShot||shot==ShotKind.OverShoulder)
                {
                    var other=actors[(Focus+1)%actors.Length];
                    actors[Focus].transform.position=new Vector3(-.8f,0,.3f);other.transform.position=new Vector3(.8f,0,.55f);
                    actors[Focus].TargetYaw=110;other.TargetYaw=250;
                    actors[Focus].transform.rotation=Quaternion.Euler(0,110,0);other.transform.rotation=Quaternion.Euler(0,250,0);
                }
                string action=sequence.galleryActions[ActionIndex];
                SetupSeat(action.StartsWith("Sitting"));
                repairKit.SetActive(action=="Fixing_Kneeling");
                repairKit.transform.position=actors[Focus].transform.position+new Vector3(0,0,-.58f);
                actionTable.SetActive(action=="PickUp_Table"||action=="Interact");
                actionTable.transform.position=actors[Focus].transform.position+new Vector3(.08f,0,-.55f);
                pushCrate.SetActive(action=="Push_Loop");pushCrate.transform.position=actors[Focus].transform.position+new Vector3(0,0,-.75f);
                // Insert is a machine shot, so restore its subject in this mode.
                foreach(var prop in galleryOccluders)prop.SetActive(shot==ShotKind.Insert&&(prop.name.StartsWith("Relay")||prop.name.StartsWith("Magnetic")||prop.name.StartsWith("Reel")||prop.name.StartsWith("Stencil DELAY")));
            }
            cameraRig.SetShot(shot,actors[Focus],actors[(Focus+1)%actors.Length],true);UpdateVisibility();
        }
        void UpdateVisibility()
        {
            bool all=cameraRig.Kind==ShotKind.Establishing;
            bool pair=cameraRig.Kind==ShotKind.TwoShot||cameraRig.Kind==ShotKind.OverShoulder;
            foreach(var actor in actors)foreach(var r in actor.GetComponentsInChildren<Renderer>())r.enabled=!Gallery||all||actor==actors[Focus]||(pair&&actor==actors[(Focus+1)%actors.Length]);
        }
        void ResetPickup(){pickup.transform.SetParent(actionTable.transform,false);pickup.transform.localPosition=new Vector3(.12f,.96f,0);pickup.transform.localRotation=Quaternion.identity;}
        void SetupSeat(bool enabledSeat)
        {
            stool.SetActive(enabledSeat);
            if(enabledSeat){stool.transform.position=actors[Focus].transform.position-Vector3.back*.11f;stool.transform.rotation=Quaternion.identity;}
        }
        static void Face(CastActor actor,Vector3 target,float limit)
        {
            Vector3 dir=target-actor.transform.position;
            if(dir.sqrMagnitude<.01f)return;
            float yaw=Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg;
            actor.TargetYaw=Mathf.MoveTowardsAngle(actor.HomeYaw,yaw,limit);
        }
    }
}

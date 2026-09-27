using System;
using System.Linq;
using UnityEngine;

namespace Astra.Performance
{
    // A replaceable adapter: production Animator/Playables can implement the same cue contract.
    public sealed class ActorPerformance : MonoBehaviour
    {
        public ActorId actorId;
        public float phaseSeed;
        Transform chest,head,armL,armR,foreL,foreR,jaw,lid;
        Quaternion[] rests;
        Vector3 chestRest,jawRest,lidRest;
        int lidVerticalAxis;
        PerformanceBeat cue;
        CrawlerPerformance crawler;
        float clock,gestureStart=-10,voiceUntil;
        public bool Paused { get; set; }
        public bool ReducedMotion { get; set; }
        public bool Speaking { get; set; }
        public float MouthAmount { get; private set; }
        public float GazeYaw { get; private set; }
        public int CueCount { get; private set; }
        public bool IsBound => head && chest && armL && armR && foreL && foreR && jaw && lid;
        public Transform Head => head;

        void Awake() { Bind(); }
        public void Bind()
        {
            if(rests!=null) return;
            var all=GetComponentsInChildren<Transform>(true);
            Func<string,Transform> find=n=>all.FirstOrDefault(t=>t.name==n);
            chest=find("Chest");head=find("Head");armL=find("UpperArmL");armR=find("UpperArmR");
            foreL=find("ForearmL");foreR=find("ForearmR");jaw=find("Jaw");lid=find("Lid");
            if(!IsBound) throw new InvalidOperationException("Missing required rig bone on " + name);
            crawler=GetComponent<CrawlerPerformance>();if(crawler)crawler.Bind();
            rests=new[]{chest.localRotation,head.localRotation,armL.localRotation,armR.localRotation,foreL.localRotation,foreR.localRotation};
            chestRest=chest.localPosition;jawRest=jaw.localPosition;lidRest=lid.localScale;
            Vector3 vertical=lid.InverseTransformDirection(Vector3.up);
            lidVerticalAxis=Mathf.Abs(vertical.x)>Mathf.Abs(vertical.y)?0:1;
            if(Mathf.Abs(vertical.z)>Mathf.Abs(vertical[lidVerticalAxis]))lidVerticalAxis=2;
        }
        public void Apply(PerformanceBeat beat)
        {
            Bind();cue=beat;CueCount++;gestureStart=clock+beat.lead+.12f;voiceUntil=0;
        }
        public void Pulse() { voiceUntil=clock+.09f; }
        public void ResetPerformance()
        {
            if(rests==null) return;
            cue=null;Speaking=false;MouthAmount=0;clock=0;gestureStart=-10;voiceUntil=0;
            chest.localRotation=rests[0];head.localRotation=rests[1];armL.localRotation=rests[2];armR.localRotation=rests[3];foreL.localRotation=rests[4];foreR.localRotation=rests[5];
            chest.localPosition=chestRest;jaw.localPosition=jawRest;SetLid(false);
            if(crawler)crawler.ResetPose();
        }
        void LateUpdate() { Evaluate(Time.unscaledDeltaTime); }
        public void Evaluate(float dt)
        {
            if(Paused || rests==null) return;
            clock+=Mathf.Max(0,dt);float seed=phaseSeed*1.73f;
            float micro=ReducedMotion || (cue!=null && cue.stillness) ? 0 : 1;
            PoseId pose=cue==null?PoseId.Rest:cue.pose;
            float lean=pose==PoseId.Tired?5:pose==PoseId.Guarded?3:pose==PoseId.Assertive?-2:0;
            float sway=Mathf.Sin(clock*.71f+seed)*.7f*micro;
            chest.localPosition=chestRest; // Feet and pelvis never bob with breathing.
            if(!crawler)Rotate(chest,rests[0],new Vector3(lean+Mathf.Sin(clock*1.6f+seed)*.45f*micro,0,sway),dt);
            GazeId gaze=cue==null?GazeId.Lens:cue.gaze;
            float yaw=gaze==GazeId.Panel?-23:gaze==GazeId.Away?24:0;
            float pitch=gaze==GazeId.Down?12:gaze==GazeId.Panel?5:0;
            float gt=clock-gestureStart;
            float envelope=gt>0 && gt<2.0f ? Mathf.Sin(Mathf.PI*Mathf.Clamp01(gt/2f)) : 0;
            GestureId gesture=cue==null?GestureId.None:cue.gesture;
            float nod=(gesture==GestureId.Nod ? Mathf.Sin(gt*7)*4*envelope:0);
            float shake=(gesture==GestureId.No ? Mathf.Sin(gt*9)*8*envelope:0);
            GazeYaw=Mathf.Lerp(GazeYaw,yaw,1-Mathf.Exp(-dt*5));
            Rotate(head,rests[1],new Vector3(pitch+nod+Mathf.Sin(clock*.9f+seed)*.45f*micro,GazeYaw+shake,-lean*.22f),dt);
            float guarded=pose==PoseId.Guarded?1:0;
            Vector3 al=new Vector3(-3-guarded*12,0,5), ar=new Vector3(4-guarded*14,0,-7);
            Vector3 fl=new Vector3(-12-guarded*36,0,0),fr=new Vector3(-8-guarded*40,0,0);
            if(gesture==GestureId.Explain) { ar+=new Vector3(-27,0,-15)*envelope;fr.x-=40*envelope; }
            if(gesture==GestureId.Point) { al+=new Vector3(-48,-15,13)*envelope;fl.x-=29*envelope; }
            if(gesture==GestureId.Chest) { ar+=new Vector3(-30,-15,28)*envelope;fr.x-=73*envelope; }
            if(crawler)crawler.Evaluate(dt,ReducedMotion,cue!=null&&cue.stillness);
            else {Rotate(armL,rests[2],al,dt);Rotate(armR,rests[3],ar,dt);Rotate(foreL,rests[4],fl,dt);Rotate(foreR,rests[5],fr,dt);}
            MouthAmount=Speaking && clock<voiceUntil ? (.45f+.45f*(Mathf.Sin(clock*31)>0?1:0)) : 0;
            jaw.localPosition=jawRest+jaw.parent.InverseTransformVector(Vector3.down*.015f*MouthAmount);
            float blinkPhase=Mathf.Repeat(clock+seed,3.9f+phaseSeed*.43f);
            bool blink=blinkPhase<.12f;
            SetLid(blink);
        }
        void SetLid(bool closed)
        {
            Vector3 scale=lidRest;scale[lidVerticalAxis]*=closed?1:.015f;lid.localScale=scale;
        }
        void Rotate(Transform t,Quaternion rest,Vector3 euler,float dt)
        {
            // Model-space semantic rotations are conjugated into the imported parent basis.
            Quaternion worldDelta=transform.rotation*Quaternion.Euler(euler)*Quaternion.Inverse(transform.rotation);
            Quaternion desired=Quaternion.Inverse(t.parent.rotation)*worldDelta*t.parent.rotation*rest;
            t.localRotation=Quaternion.Slerp(t.localRotation,desired,1-Mathf.Exp(-dt*7));
        }
    }
}

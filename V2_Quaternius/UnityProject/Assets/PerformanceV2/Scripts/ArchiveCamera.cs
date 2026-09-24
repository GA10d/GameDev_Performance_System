using UnityEngine;

namespace Astra.PerformanceV2
{
    public sealed class ArchiveCamera : MonoBehaviour
    {
        public Camera lens;
        public bool ReducedMotion,Gallery,Seated,ClosingWide;
        public ShotKind Kind {get;private set;}
        public float Transition {get;private set;}
        Vector3 fromPos,fromTarget,targetPos,targetLook,currentLook,mark;
        float fromFov,targetFov,clock,duration,poseOffset;
        CastActor actor,partner;
        public static readonly string[] Names={"全景 · 建立空间","中景 · 人物交流","近景 · 表情与停顿","双人 · 关系构图","过肩 · 倾听视点","侧面 · 轮廓","低机位 · 压迫感","插入 · 中继设备","全身 · 动作与接触"};
        public void SetShot(ShotKind kind,CastActor subject,CastActor listener,bool cut)
        {
            var oldActor=actor;var oldKind=Kind;
            actor=subject;partner=listener;Kind=kind;clock=0;mark=actor.transform.position;
            poseOffset=PoseTarget();
            fromPos=lens.transform.position;fromTarget=currentLook;fromFov=lens.fieldOfView;
            Compute();
            // Distant setups use an editorial cut; they must not fly through the cast.
            duration=!cut&&oldActor==actor&&oldKind==kind&&Vector3.Distance(fromPos,targetPos)<.7f?.65f:0;
            Transition=duration==0?1:0;Apply(Transition);
        }
        void Compute()
        {
            Vector3 p=Kind==ShotKind.Tracking?actor.transform.position:mark;
            float head=actor.StandingHead+poseOffset;
            targetFov=32;
            switch(Kind)
            {
                case ShotKind.Establishing:
                    if(ClosingWide&&!Gallery){targetPos=new Vector3(-.8f,2f,-4.45f);targetLook=new Vector3(-.3f,.80f,.4f);targetFov=42;}
                    else {targetPos=new Vector3(.4f,1.9f,-4.45f);targetLook=new Vector3(-.3f,1.06f,.8f);targetFov=35;}break;
                case ShotKind.Medium:
                    targetPos=p+new Vector3(.20f,head+.10f,-2.55f);targetLook=p+Vector3.up*(head-.24f);targetFov=31;break;
                case ShotKind.Close:
                    bool alien=actor.species=="外星人";
                    bool activePose=Gallery&&actor.Action!="Idle_Loop"&&actor.Action!="Idle_Talking_Loop";
                    if(activePose){targetPos=p+new Vector3(.05f,head+.1f,-2f);targetLook=p+Vector3.up*(head+.04f);targetFov=39;}
                    else {targetPos=p+new Vector3(.05f,head+.01f,-1.27f);targetLook=p+Vector3.up*(head+(alien?.015f:-.13f));targetFov=alien?39:34;}
                    break;
                case ShotKind.TwoShot:
                    var q=partner?partner.transform.position:p+Vector3.left*1.6f;
                    targetFov=40;
                    float distance=Mathf.Max(2.0f,(Mathf.Abs(p.x-q.x)+1.05f)/(2*Mathf.Tan(targetFov*Mathf.Deg2Rad*.5f)*lens.aspect))+Mathf.Abs(p.z-q.z)*.5f;
                    targetLook=(p+q)*.5f+Vector3.up*1.42f;targetPos=targetLook+new Vector3(0,.08f,-distance);break;
                case ShotKind.OverShoulder:
                    var other=partner?partner.transform.position:p+Vector3.right*1.6f;
                    var toward=(p-other).normalized;
                    targetPos=other-toward*.45f+new Vector3(0,1.55f,-.44f);targetLook=p+Vector3.up*(head-.12f);targetFov=32;break;
                case ShotKind.Profile:
                    targetPos=p+new Vector3(-1.1f,head+.04f,-2.05f);targetLook=p+Vector3.up*(head-.23f);targetFov=30;break;
                case ShotKind.LowAngle:
                    targetPos=p+new Vector3(.2f,.92f,-2.1f);targetLook=p+Vector3.up*(head-.24f);targetFov=33;break;
                case ShotKind.Insert:
                    targetPos=new Vector3(.35f,1.25f,-2.1f);targetLook=new Vector3(0,.70f,-.58f);targetFov=36;break;
                default:
                    bool repairing=actor.Action=="Fixing_Kneeling";
                    targetPos=p+new Vector3(.38f,1.38f,-3.6f);targetLook=p+Vector3.up*(repairing?.82f:.99f);targetFov=repairing?40:36;break;
            }
        }
        public void Tick(float dt)
        {
            if(dt<=0)return;clock+=dt;
            poseOffset=Mathf.Lerp(poseOffset,PoseTarget(),1-Mathf.Exp(-dt/0.14f));Compute();
            Transition=duration==0?1:Mathf.Clamp01(clock/duration);Apply(Mathf.SmoothStep(0,1,Transition));
        }
        float PoseTarget()
        {
            // Dialogue shots have a fixed anchor. Only posture-changing gallery
            // clips receive a damped height correction, to keep a kneeling face
            // in frame when the user explicitly selects a close lens.
            if(!Gallery)return Seated?-.52f:0;
            string action=actor.Action??"";
            if(action!="Idle_Loop"&&action!="Idle_Talking_Loop")
                return Mathf.Clamp(actor.Head.position.y-actor.transform.position.y-(actor.StandingHead-.10f),-.9f,.2f);
            return 0;
        }
        void Apply(float t)
        {
            Vector3 end=targetPos;
            if(!ReducedMotion&&!Gallery&&Kind==ShotKind.Close)end+=(targetLook-targetPos).normalized*(.055f*Mathf.SmoothStep(0,1,clock/7));
            lens.transform.position=Vector3.Lerp(fromPos,end,t);currentLook=Vector3.Lerp(fromTarget,targetLook,t);
            lens.transform.rotation=Quaternion.LookRotation(currentLook-lens.transform.position);lens.fieldOfView=Mathf.Lerp(fromFov,targetFov,t);
        }
    }
}

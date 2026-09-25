using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Astra.PerformanceV2
{
    [RequireComponent(typeof(Animator))]
    public sealed class CastActor : MonoBehaviour
    {
        public string displayName,species,role;
        public Color identityColor;
        Animator animator;
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        readonly AnimationClipPlayable[] players=new AnimationClipPlayable[4];
        readonly float[] startWeights=new float[4];
        readonly bool[] loops=new bool[4];
        readonly float[] durations=new float[4];
        int target;
        float blend=1,elapsed;
        Transform head;
        Quaternion headBase;
        public Vector3 Home {get;private set;}
        public float HomeYaw {get;private set;}
        public float StandingHead {get;private set;}
        public float TargetYaw;
        public string Action {get;private set;}
        public float ActionTime {get;private set;}
        public Accent Expression;
        public Vector3 GazeTarget;
        public float GazeWeight;
        public bool ReducedMotion;
        public Transform Head=>head;
        public Animator Animator=>animator;
        public bool Ready=>animator&&animator.avatar&&animator.avatar.isValid&&animator.avatar.isHuman&&graph.IsValid();
        public int LiveClips {get {int n=0;foreach(var p in players)if(p.IsValid())n++;return n;}}
        public void Initialize()
        {
            if(graph.IsValid())return;
            Home=transform.position;HomeYaw=transform.eulerAngles.y;
            animator=GetComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            head=animator.GetBoneTransform(HumanBodyBones.Head);
            StandingHead=head.position.y-transform.position.y;TargetYaw=HomeYaw;
            graph=PlayableGraph.Create(name+" performance");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            mixer=AnimationMixerPlayable.Create(graph,4);
            var output=AnimationPlayableOutput.Create(graph,"Humanoid pose",animator);output.SetSourcePlayable(mixer);graph.Play();
        }
        public void Play(AnimationClip clip,float speed=1,bool immediate=false)
        {
            if(!clip)return;
            if(Action==clip.name&&!immediate)return;
            Initialize();int slot=-1;float min=2;
            if(immediate)for(int i=0;i<4;i++){if(players[i].IsValid()){graph.Disconnect(mixer,i);graph.DestroyPlayable(players[i]);}mixer.SetInputWeight(i,0);}
            for(int i=0;i<4;i++){float w=mixer.GetInputWeight(i);if(!players[i].IsValid()){slot=i;break;}if(w<min){min=w;slot=i;}}
            if(players[slot].IsValid()){graph.Disconnect(mixer,slot);graph.DestroyPlayable(players[slot]);}
            float sum=0;
            for(int i=0;i<4;i++){startWeights[i]=i==slot?0:mixer.GetInputWeight(i);sum+=startWeights[i];}
            for(int i=0;i<4;i++){startWeights[i]=sum>.001f?startWeights[i]/sum:0;mixer.SetInputWeight(i,startWeights[i]);}
            players[slot]=AnimationClipPlayable.Create(graph,clip);players[slot].SetApplyFootIK(true);players[slot].SetSpeed(speed);
            graph.Connect(players[slot],0,mixer,slot);loops[slot]=clip.isLooping;durations[slot]=clip.length;
            target=slot;blend=sum>.001f?0:1;Action=clip.name;ActionTime=0;
            mixer.SetInputWeight(slot,blend);graph.Evaluate(0);if(head)headBase=head.localRotation;
        }
        public void Tick(float dt)
        {
            if(!graph.IsValid()||dt<=0)return;
            elapsed+=dt;ActionTime+=dt;
            transform.rotation=Quaternion.Euler(0,Mathf.MoveTowardsAngle(transform.eulerAngles.y,TargetYaw,65*dt),0);
            // Remove the previous additive head correction before sampling the next animated pose.
            if(head)head.localRotation=headBase;
            blend=Mathf.Min(1,blend+dt/.32f);float t=blend*blend*(3-2*blend);
            for(int i=0;i<4;i++)
            {
                if(!players[i].IsValid())continue;
                mixer.SetInputWeight(i,i==target?t:startWeights[i]*(1-t));
                double time=players[i].GetTime();
                if(loops[i]&&durations[i]>0&&time>durations[i])players[i].SetTime(time%durations[i]);
                else if(!loops[i]&&time>durations[i]){players[i].SetTime(durations[i]);players[i].SetSpeed(0);}
                if(i!=target&&blend>=1){graph.Disconnect(mixer,i);graph.DestroyPlayable(players[i]);}
            }
            graph.Evaluate(dt);
            if(head)
            {
                headBase=head.localRotation;
                Vector3 direction=transform.InverseTransformDirection(GazeTarget-head.position);
                float yaw=Mathf.Clamp(Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg,-38,38)*GazeWeight;
                float pitch=Mathf.Clamp(-Mathf.Atan2(direction.y,new Vector2(direction.x,direction.z).magnitude)*Mathf.Rad2Deg,-13,17)*GazeWeight;
                float a=ReducedMotion?.3f:1;
                float pulse=Mathf.Sin(Mathf.Clamp01(ActionTime/2)*Mathf.PI);
                if(Expression==Accent.Nod)pitch+=Mathf.Sin(ActionTime*6)*7*pulse*a;
                if(Expression==Accent.Refuse)yaw+=Mathf.Sin(ActionTime*5)*10*pulse*a;
                if(Expression==Accent.LookDown)pitch+=12;
                if(Expression==Accent.Glance)yaw+=18*Mathf.Sin(Mathf.Clamp01(ActionTime/3)*Mathf.PI);
                float roll=Expression==Accent.Listen?7:Mathf.Sin(elapsed*.8f)*.45f*a;
                head.rotation=Quaternion.AngleAxis(yaw,Vector3.up)*Quaternion.AngleAxis(pitch,transform.right)*head.rotation*Quaternion.Euler(0,roll,0);
            }
        }
        public void ResetMark(){transform.position=Home;TargetYaw=HomeYaw;transform.rotation=Quaternion.Euler(0,HomeYaw,0);Expression=Accent.None;}
        void OnDestroy(){if(graph.IsValid())graph.Destroy();}
    }
}

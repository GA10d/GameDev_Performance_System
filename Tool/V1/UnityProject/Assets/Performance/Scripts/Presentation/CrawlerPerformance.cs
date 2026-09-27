using System;
using System.Linq;
using UnityEngine;

namespace Astra.Performance
{
    // Independent morphology adapter. The common director still owns pause/cue time.
    public sealed class CrawlerPerformance:MonoBehaviour
    {
        sealed class Leg
        {
            public Transform upper,lower,end;
            public Vector3 anchor,pole;
            public Quaternion endRotation;
            public float a,b,phase;
            public Vector3 planted,stepStart,stepGoal;
            public bool swinging;
        }
        Leg[] legs;
        Transform hips,chest,tail1,tail2;
        Vector3 hipRest;
        Transform[] bones;
        Quaternion[] rotations;
        float clock;
        public bool Locomotion=true;
        public float MaxContactError {get;private set;}
        public float[] FootHeights {get;private set;}=new float[4];
        public Vector3[] Feet=>legs.Select(l=>l.end.position).ToArray();
        public void Bind()
        {
            if(legs!=null)return;
            var all=GetComponentsInChildren<Transform>(true);
            Func<string,Transform> find=n=>all.First(t=>t.name==n);
            hips=find("Hips");chest=find("Chest");tail1=find("Tail1");tail2=find("Tail2");hipRest=hips.localPosition;
            legs=new Leg[4];
            string[] us={"UpperArmL","UpperArmR","ThighL","ThighR"},ls={"ForearmL","ForearmR","ShinL","ShinR"},es={"HandL","HandR","FootL","FootR"};
            float[] phases={0,.5f,.75f,.25f};
            for(int i=0;i<4;i++)
            {
                var l=new Leg{upper=find(us[i]),lower=find(ls[i]),end=find(es[i]),phase=phases[i]};
                l.anchor=transform.InverseTransformPoint(l.end.position);
                l.pole=transform.InverseTransformDirection(l.lower.position-(l.upper.position+l.end.position)*.5f);
                l.a=Vector3.Distance(l.upper.position,l.lower.position);l.b=Vector3.Distance(l.lower.position,l.end.position);
                l.endRotation=Quaternion.Inverse(transform.rotation)*l.end.rotation;l.planted=l.anchor;legs[i]=l;
            }
            bones=new[]{hips,chest,tail1,tail2}.Concat(legs.SelectMany(l=>new[]{l.upper,l.lower,l.end})).ToArray();
            rotations=bones.Select(t=>t.localRotation).ToArray();
        }
        public void ResetPose()
        {
            Bind();clock=0;hips.localPosition=hipRest;
            for(int i=0;i<bones.Length;i++)bones[i].localRotation=rotations[i];MaxContactError=0;
            foreach(var l in legs){l.planted=l.anchor;l.swinging=false;}
        }
        public void Evaluate(float dt,bool reduced,bool still)
        {
            Bind();clock+=Mathf.Max(0,dt);
            for(int i=0;i<bones.Length;i++)bones[i].localRotation=rotations[i];
            bool moving=Locomotion&&!reduced&&!still;
            // A short, bounded crawl inside the archive bay. Four staggered swings,
            // each shorter than a quarter cycle, retain at least three supports.
            float travel=moving?Mathf.Sin(clock*.68f)*.105f:0;
            Vector3 shift=transform.forward*travel+Vector3.up*(reduced||still?0:Mathf.Sin(clock*1.3f)*.007f);
            hips.localPosition=hipRest+hips.parent.InverseTransformVector(shift);
            tail1.localRotation=rotations[2]*Quaternion.Euler(0,0,reduced||still?0:Mathf.Sin(clock*.9f)*5);
            tail2.localRotation=rotations[3]*Quaternion.Euler(0,0,reduced||still?0:Mathf.Sin(clock*.9f-1)*8);
            MaxContactError=0;
            for(int i=0;i<4;i++)
            {
                var l=legs[i];Vector3 target=l.planted;
                if(moving)
                {
                    float phase=Mathf.Repeat(clock/3.8f+l.phase,1);
                    if(phase<.20f)
                    {
                        if(!l.swinging){l.stepStart=l.planted;l.stepGoal=l.anchor+Vector3.forward*(travel+Mathf.Cos(clock*.68f)*.07f);l.swinging=true;}
                        target=Vector3.Lerp(l.stepStart,l.stepGoal,Mathf.SmoothStep(0,1,phase/.20f));
                        target.y+=Mathf.Sin(Mathf.PI*phase/.20f)*.085f;
                    }
                    else if(l.swinging){l.planted=l.stepGoal;target=l.planted;l.swinging=false;}
                }
                else {l.swinging=false;l.planted=Vector3.Lerp(l.planted,l.anchor,1-Mathf.Exp(-dt*4));target=l.planted;}
                Vector3 world=transform.TransformPoint(target);Solve(l,world);
                l.end.rotation=transform.rotation*l.endRotation;
                MaxContactError=Mathf.Max(MaxContactError,Vector3.Distance(l.end.position,world));FootHeights[i]=l.end.position.y;
            }
        }
        void Solve(Leg l,Vector3 target)
        {
            Vector3 delta=target-l.upper.position;float dist=Mathf.Clamp(delta.magnitude,.001f,l.a+l.b-.0001f);Vector3 dir=delta.normalized;
            float along=(l.a*l.a-l.b*l.b+dist*dist)/(2*dist);float height=Mathf.Sqrt(Mathf.Max(0,l.a*l.a-along*along));
            Vector3 bend=Vector3.ProjectOnPlane(transform.TransformDirection(l.pole),dir).normalized;
            Vector3 knee=l.upper.position+dir*along+bend*height;
            l.upper.rotation=Quaternion.FromToRotation(l.lower.position-l.upper.position,knee-l.upper.position)*l.upper.rotation;
            l.lower.rotation=Quaternion.FromToRotation(l.end.position-l.lower.position,target-l.lower.position)*l.lower.rotation;
        }
    }
}

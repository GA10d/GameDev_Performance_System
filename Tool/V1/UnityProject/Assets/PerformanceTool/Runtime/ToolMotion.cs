using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Astra.PerformanceTool
{
    public sealed class ToolMotion : MonoBehaviour
    {
        Transform[] bones;
        Quaternion[] rotations;
        Vector3[] positions;
        ToolSpecies species;
        public void Bind(ToolSpecies type)
        {
            species=type;
            string[] names={"Hips","Chest","Head","UpperArmL","UpperArmR","ForearmL","ForearmR","ThighL","ThighR","ShinL","ShinR"};
            bones=names.Select(n=>ToolCharacterView.Find(transform,n)).ToArray();
            rotations=bones.Select(b=>b?b.localRotation:Quaternion.identity).ToArray();
            positions=bones.Select(b=>b?b.localPosition:Vector3.zero).ToArray();
        }
        public void Sample(string action,float time,float speed)
        {
            if(bones==null)Bind(ToolSpecies.Human);
            for(int i=0;i<bones.Length;i++) if(bones[i]) {bones[i].localRotation=rotations[i];bones[i].localPosition=positions[i];}
            float t=Mathf.Max(0,time)*Mathf.Max(.1f,speed), wave=Mathf.Sin(t*5), breath=Mathf.Sin(t*1.7f);
            if(species==ToolSpecies.CrawlerAlien)
            {
                // The V3 crawler's authored IK contact rig remains the locomotion source.
                var crawler=GetComponent<Astra.Performance.CrawlerPerformance>();
                if(!crawler)crawler=gameObject.AddComponent<Astra.Performance.CrawlerPerformance>();
                crawler.ResetPose();crawler.Locomotion=action=="Walk_Loop"||action=="Walk_Formal_Loop"||action=="Jog_Fwd_Loop";
                crawler.Evaluate(t,false,action=="Crouch_Idle_Loop");
                return;
            }
            Rotate(1,new Vector3(breath*.5f,0,0));
            switch(action)
            {
                case "Idle_Talking_Loop": Rotate(2,new Vector3(Mathf.Sin(t*2)*2,Mathf.Sin(t*1.3f)*3,0));Rotate(4,new Vector3(-23,0,-12));Rotate(6,new Vector3(-30,0,0));break;
                case "Walk_Loop": case "Walk_Formal_Loop": case "Jog_Fwd_Loop":
                    float amount=action=="Jog_Fwd_Loop"?29:action=="Walk_Formal_Loop"?13:21;
                    Rotate(7,new Vector3(wave*amount,0,0));Rotate(8,new Vector3(-wave*amount,0,0));
                    Rotate(3,new Vector3(-wave*amount*.7f,0,0));Rotate(4,new Vector3(wave*amount*.7f,0,0));
                    Move(0,new Vector3(0,Mathf.Abs(wave)*.025f,0));break;
                case "PickUp_Table": case "Fixing_Kneeling":
                    Rotate(0,new Vector3(0,0,0));Move(0,new Vector3(0,action=="Fixing_Kneeling"?-.40f:-.19f,0));
                    Rotate(1,new Vector3(25,0,0));Rotate(3,new Vector3(-40,0,12));Rotate(4,new Vector3(-43,0,-12));
                    Rotate(7,new Vector3(35,0,0));Rotate(8,new Vector3(35,0,0));break;
                case "Interact": Rotate(4,new Vector3(-47,0,-15));Rotate(6,new Vector3(-28,0,0));break;
                case "Sitting_Enter": case "Sitting_Idle_Loop": case "Sitting_Talking_Loop": case "Sitting_Exit":
                    float blend=(action=="Sitting_Enter")?Mathf.Clamp01(t/1.1f):(action=="Sitting_Exit")?1-Mathf.Clamp01(t/1.1f):1;
                    Move(0,new Vector3(0,-.51f*blend,.08f*blend));Rotate(7,new Vector3(70*blend,0,0));Rotate(8,new Vector3(70*blend,0,0));
                    if(action=="Sitting_Talking_Loop")Rotate(4,new Vector3(-25,0,-11));break;
                case "Push_Loop": Rotate(1,new Vector3(12,0,0));Rotate(3,new Vector3(-62,0,9));Rotate(4,new Vector3(-62,0,-9));break;
                case "Crouch_Idle_Loop": Move(0,new Vector3(0,-.37f,0));Rotate(7,new Vector3(41,0,0));Rotate(8,new Vector3(41,0,0));break;
                case "Hit_Chest": Rotate(4,new Vector3(-44,0,30));Rotate(6,new Vector3(-65,0,0));break;
                case "Hit_Head": Rotate(4,new Vector3(-68,0,22));Rotate(6,new Vector3(-82,0,0));break;
                case "Dance_Loop": Rotate(1,new Vector3(0,0,Mathf.Sin(t*3)*11));Rotate(3,new Vector3(-35,0,Mathf.Sin(t*3)*18));Rotate(4,new Vector3(-35,0,-Mathf.Sin(t*3)*18));Rotate(7,new Vector3(Mathf.Sin(t*3)*16,0,0));Rotate(8,new Vector3(-Mathf.Sin(t*3)*16,0,0));break;
            }
        }
        void Rotate(int index,Vector3 euler)
        {
            var b=bones[index];if(!b)return;
            Quaternion worldDelta=transform.rotation*Quaternion.Euler(euler)*Quaternion.Inverse(transform.rotation);
            b.localRotation=Quaternion.Inverse(b.parent.rotation)*worldDelta*b.parent.rotation*rotations[index];
        }
        void Move(int index,Vector3 delta)
        {
            var b=bones[index];if(b)b.localPosition=positions[index]+b.parent.InverseTransformVector(delta);
        }
    }
}

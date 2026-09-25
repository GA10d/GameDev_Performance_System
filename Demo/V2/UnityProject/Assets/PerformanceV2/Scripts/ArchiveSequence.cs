using System;
using UnityEngine;

namespace Astra.PerformanceV2
{
    public enum ShotKind { Establishing, Medium, Close, TwoShot, OverShoulder, Profile, LowAngle, Insert, Tracking }
    public enum Accent { None, Nod, Refuse, Listen, LookDown, Glance }
    [Serializable] public sealed class ArchiveBeat
    {
        public string id,chapter,subtitle,action="Idle_Loop";
        [Range(0,3)] public int actor;
        public int listener=-1;
        public ShotKind shot;
        public Accent accent;
        public float duration=7,lead=.6f,speed=1;
        public bool cut,move,seated;
        public Vector3 start,end;
    }
    [CreateAssetMenu(menuName="Astra/Archive sequence")]
    public sealed class ArchiveSequence : ScriptableObject
    {
        public int reviewRevision;
        public ArchiveBeat[] beats;
        public AnimationClip[] clips;
        public string[] galleryActions;
        public AnimationClip Clip(string name)
        {foreach(var c in clips)if(c&&c.name==name)return c;return null;}
        public string Validate()
        {
            if(beats==null||beats.Length==0)return "Empty sequence";
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var b in beats){if(!ids.Add(b.id))return "Duplicate beat: "+b.id;if(b.duration<=0||b.actor<0||b.actor>3||!Clip(b.action))return "Invalid beat: "+b.id;}
            foreach(var action in galleryActions)if(!Clip(action))return "Missing action: "+action;
            return null;
        }
    }
}

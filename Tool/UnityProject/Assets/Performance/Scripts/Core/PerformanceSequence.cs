using System;
using UnityEngine;

namespace Astra.Performance
{
    public enum ActorId { Lin, He, Yu, Xian, Ruk, Fu }
    public enum PoseId { Rest, Tired, Guarded, Assertive }
    public enum GazeId { Lens, Panel, Down, Away }
    public enum GestureId { None, Explain, Point, Chest, No, Nod }
    public enum SessionPhase { Inactive, Lead, Typing, Hold, Ready, Choice, Complete, Cancelled }

    [Serializable] public sealed class ReplyChoice
    {
        public string text;
        public string next;
        public string payload;
    }
    [Serializable] public sealed class PerformanceBeat
    {
        public string id;
        public ActorId actor;
        [TextArea(2,4)] public string text;
        public PoseId pose;
        public GazeId gaze;
        public GestureId gesture;
        [Min(0)] public float lead = .35f;
        [Min(1)] public float charactersPerSecond = 18;
        [Min(0)] public float hold = .65f;
        [Range(0,1)] public float closeUp;
        public bool alarm;
        public bool stillness;
        public bool systemLine;
        public string next;
        public ReplyChoice[] choices = Array.Empty<ReplyChoice>();
    }
    [CreateAssetMenu(menuName="Astra/Performance/Sequence")]
    public sealed class PerformanceSequence : ScriptableObject
    {
        public string entryId;
        public PerformanceBeat[] beats;
    }
}

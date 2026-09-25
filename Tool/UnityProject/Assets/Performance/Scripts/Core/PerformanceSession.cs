using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Astra.Performance
{
    // One clock, one owner. No Input, coroutines, transforms, AudioSource or Time here.
    public sealed class PerformanceSession
    {
        readonly Dictionary<string,PerformanceBeat> beats = new Dictionary<string,PerformanceBeat>();
        readonly List<string> elements = new List<string>();
        readonly StringBuilder visible = new StringBuilder();
        float remaining, readElapsed;
        int cursor;
        public PerformanceBeat Beat { get; private set; }
        public SessionPhase Phase { get; private set; } = SessionPhase.Inactive;
        public bool Paused { get; set; }
        public bool Auto { get; set; }
        public string VisibleText { get; private set; } = "";
        public string PendingReply { get; private set; } = "";
        public int VisibleElements => cursor;
        public float BeatTime { get; private set; }
        public int Generation { get; private set; }
        public bool IsSpeaking => !Paused && Phase == SessionPhase.Typing;
        public event Action<PerformanceBeat> Entered;
        public event Action<string> Revealed;
        public event Action Ended;

        public PerformanceSession(IEnumerable<PerformanceBeat> data)
        {
            foreach (var b in data)
            {
                if (b == null || string.IsNullOrWhiteSpace(b.id) || beats.ContainsKey(b.id))
                    throw new ArgumentException("Beat IDs must be present and unique.");
                if (b.charactersPerSecond <= 0 || !Finite(b.charactersPerSecond) || !Finite(b.lead) || !Finite(b.hold) || b.lead < 0 || b.hold < 0)
                    throw new ArgumentException("Invalid timing for " + b.id);
                beats.Add(b.id,b);
            }
            foreach (var b in beats.Values)
            {
                ValidateTarget(b.next);
                if (b.choices == null) throw new ArgumentException("choices must not be null: " + b.id);
                foreach (var c in b.choices)
                {
                    if (c == null || string.IsNullOrWhiteSpace(c.text)) throw new ArgumentException("Invalid choice: " + b.id);
                    ValidateTarget(c.next);
                }
            }
        }
        void ValidateTarget(string id)
        {
            if (!string.IsNullOrEmpty(id) && !beats.ContainsKey(id)) throw new ArgumentException("Missing next beat: " + id);
        }
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
        public void Start(string id)
        {
            if (!beats.ContainsKey(id)) throw new ArgumentException("Missing entry: " + id);
            Generation++; PendingReply=""; Paused=false; Enter(id);
        }
        void Enter(string id)
        {
            if (string.IsNullOrEmpty(id)) { Phase=SessionPhase.Complete; Ended?.Invoke(); return; }
            Beat=beats[id]; Phase=SessionPhase.Lead; remaining=Beat.lead;
            BeatTime=0; readElapsed=0; cursor=0; visible.Clear(); VisibleText=""; elements.Clear();
            var e=StringInfo.GetTextElementEnumerator(Beat.text ?? "");
            while(e.MoveNext()) elements.Add((string)e.Current);
            Entered?.Invoke(Beat);
        }
        public void Cancel()
        {
            Generation++; Phase=SessionPhase.Cancelled; Paused=false; Beat=null;
            PendingReply=""; VisibleText=""; visible.Clear(); Ended?.Invoke();
        }
        public void Tick(float dt)
        {
            if(Paused || dt<=0 || float.IsNaN(dt) || float.IsInfinity(dt) || Beat==null || Phase==SessionPhase.Complete || Phase==SessionPhase.Cancelled) return;
            BeatTime+=dt; readElapsed+=dt;
            // Retain overshoot across timed states. A large frame must not lose a cue.
            for(int guard=0; guard<10000; guard++)
            {
                if(Phase==SessionPhase.Ready)
                {
                    if(Auto && readElapsed >= Beat.lead+Math.Max(2.0f,elements.Count*.12f)+Beat.hold) Enter(Beat.next);
                    return;
                }
                if(Phase==SessionPhase.Choice) return;
                if(dt<remaining) { remaining-=dt; return; }
                dt-=remaining; remaining=0;
                if(Phase==SessionPhase.Lead) { Phase=SessionPhase.Typing; }
                else if(Phase==SessionPhase.Typing)
                {
                    if(cursor<elements.Count)
                    {
                        string ch=elements[cursor++];visible.Append(ch);VisibleText=visible.ToString();
                        Revealed?.Invoke(ch);
                        remaining=1f/Beat.charactersPerSecond+PunctuationDelay(ch);
                    }
                    else { Phase=SessionPhase.Hold;remaining=Beat.hold; }
                }
                else if(Phase==SessionPhase.Hold)
                { Phase=Beat.choices.Length>0?SessionPhase.Choice:SessionPhase.Ready; }
                else return;
            }
            throw new InvalidOperationException("Sequence update exceeded safe transition budget.");
        }
        static float PunctuationDelay(string ch)
        {
            if("。！？!?…".Contains(ch)) return .28f;
            return "，、,：:；;".Contains(ch) ? .14f : 0;
        }
        // First press reveals text; later press advances. No replayed sound/cues on skip.
        public void Advance()
        {
            if(Paused || Beat==null) return;
            if(Phase==SessionPhase.Lead || Phase==SessionPhase.Typing)
            {
                VisibleText=Beat.text ?? "";cursor=elements.Count;
                Phase=SessionPhase.Hold;remaining=Beat.hold;
            }
            else if(Phase==SessionPhase.Ready) Enter(Beat.next);
        }
        public bool Choose(int index)
        {
            if(Paused || Phase!=SessionPhase.Choice || index<0 || index>=Beat.choices.Length) return false;
            var choice=Beat.choices[index];PendingReply=choice.payload;Enter(choice.next);return true;
        }
    }
}

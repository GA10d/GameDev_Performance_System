using UnityEngine;

namespace Astra.Performance
{
    public sealed class PerformanceAudio : MonoBehaviour
    {
        AudioSource voice,room;
        AudioClip[] clips;
        float lastPulse=-1;
        public bool Muted { get; private set; }
        public bool Paused { get; private set; }
        void Awake()
        {
            voice=gameObject.AddComponent<AudioSource>();voice.playOnAwake=false;voice.spatialBlend=0;voice.volume=.09f;
            room=gameObject.AddComponent<AudioSource>();room.playOnAwake=false;room.spatialBlend=0;room.volume=.065f;room.loop=true;
            clips=new[]{Tone(142),Tone(105),Tone(180)};
            int count=44100*4;float[] data=new float[count];var random=new System.Random(47);
            for(int i=0;i<count;i++) {float t=i/44100f;data[i]=.15f*Mathf.Sin(t*2*Mathf.PI*62)+.05f*Mathf.Sin(t*2*Mathf.PI*124)+.025f*((float)random.NextDouble()*2-1);}
            room.clip=AudioClip.Create("Original ventilation",count,1,44100,false);room.clip.SetData(data,0);room.Play();
        }
        static AudioClip Tone(float frequency)
        {
            int n=2646;float[] data=new float[n];
            for(int i=0;i<n;i++){float t=i/44100f;float env=Mathf.Sin(Mathf.PI*i/n);data[i]=env*env*(Mathf.Sin(t*frequency*6.283f)+.2f*Mathf.Sin(t*frequency*12.566f))*.32f;}
            var c=AudioClip.Create("Original syllable "+frequency,n,1,44100,false);c.SetData(data,0);return c;
        }
        public void Pulse(ActorId id,string ch)
        {
            if(Muted || Paused || string.IsNullOrWhiteSpace(ch) || char.IsPunctuation(ch,0) || Time.unscaledTime-lastPulse<.065f) return;
            lastPulse=Time.unscaledTime;voice.clip=clips[(int)id];voice.Play();
        }
        public void SetMuted(bool value) {Muted=value;AudioListener.volume=value?0:1;}
        public void SetPaused(bool value)
        {
            Paused=value;
            if(value){voice.Pause();room.Pause();}else{voice.UnPause();room.UnPause();}
        }
        public void StopVoice(){voice.Stop();}
        void OnDestroy()
        {
            if(clips!=null)foreach(var c in clips)Destroy(c);
            if(room && room.clip)Destroy(room.clip);
            AudioListener.volume=1;
        }
    }
}

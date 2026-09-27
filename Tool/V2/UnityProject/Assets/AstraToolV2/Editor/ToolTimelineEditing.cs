using System;
using Astra.PerformanceToolV2;
using UnityEngine;

// Duration is content-driven. No demo-length cap is applied to authoring or playback.
public static class ToolTimelineEditing
{
    public static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    static float End(float current,float start,float duration)
    {
        float end=start+duration;
        return Finite(end)&&start>=0&&duration>0?Mathf.Max(current,end):current;
    }
    public static float ContentEnd(ToolUnit unit)
    {
        float end=.1f;
        foreach(var x in unit.actors)end=End(end,x.start,x.duration);
        foreach(var x in unit.actions)end=End(end,x.start,x.duration);
        foreach(var x in unit.dialogue)end=End(end,x.start,x.Duration);
        foreach(var x in unit.cameras)end=End(end,x.start,x.duration);
        foreach(var x in unit.scenes)end=End(end,x.start,x.duration);
        return end;
    }
    public static bool SetDuration(ToolUnit unit,float requested)
    {
        if(!unit||!Finite(requested))return false;
        float duration=Mathf.Max(.1f,ContentEnd(unit),requested);
        if(unit.duration==duration)return false;
        unit.duration=duration;return true;
    }
    public static bool ExtendToContent(ToolUnit unit)=>SetDuration(unit,unit.duration);
    public static float TickStep(float pixelsPerSecond)
    {
        float wanted=70f/Mathf.Max(.000001f,pixelsPerSecond);
        float magnitude=Mathf.Pow(10,Mathf.Floor(Mathf.Log10(wanted)));
        float fraction=wanted/magnitude;
        return (fraction<=1?1:fraction<=2?2:fraction<=5?5:10)*magnitude;
    }
}

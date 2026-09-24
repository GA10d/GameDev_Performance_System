using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Astra.PerformanceV2
{
    // Captures the rendered player through continuous playback, including cuts.
    // The CSV is diagnostic evidence, never a substitute for reviewing the images.
    public sealed class VisualAudit : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Attach()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--visual-audit")>=0)
                FindObjectOfType<ArchiveDirector>().gameObject.AddComponent<VisualAudit>();
        }
        readonly List<string> rows=new List<string>();
        string output; int number;
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--visual-audit");output=args[i+1];Directory.CreateDirectory(output);
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            yield return null;
            var d=GetComponent<ArchiveDirector>();d.AuditStep=1f/30;d.Seek(0);
            rows.Add("file,mode,beat,actor,action,time,camera_x,camera_y,camera_z,aspect,head_x,head_y,leftfoot_x,leftfoot_y,rightfoot_x,rightfoot_y,hips_y,root_x,root_z");
            if(Array.IndexOf(args,"--audit-matrix")>=0)
            {
                d.SetGallery(true);d.AuditStep=1f/30;
                for(int a=0;a<4;a++)for(int n=0;n<d.sequence.galleryActions.Length;n++)
                {
                    d.SelectActor(a);d.SelectAction(n);
                    float sample=Mathf.Max(.15f,d.sequence.Clip(d.sequence.galleryActions[n]).length*.5f);
                    for(int f=0;f<sample*30;f++)yield return new WaitForEndOfFrame();
                    d.TogglePause();
                    foreach(ShotKind shot in Enum.GetValues(typeof(ShotKind)))
                    {d.SelectShot(shot);yield return new WaitForEndOfFrame();yield return new WaitForEndOfFrame();Capture(d,"matrix_"+a+"_"+n.ToString("00")+"_"+(int)shot);}
                    d.TogglePause();
                }
                File.WriteAllLines(Path.Combine(output,"frames.csv"),rows);File.WriteAllText(Path.Combine(output,"complete.txt"),number+" rendered matrix frames captured");Application.Quit();yield break;
            }
            int frame=0,last=-1;
            while(!d.Ended)
            {
                yield return new WaitForEndOfFrame();
                if(d.Index!=last||frame%8==0){Capture(d,"story_"+d.Index.ToString("00"));last=d.Index;}
                frame++;
            }
            Capture(d,"story_end");
            if(Array.IndexOf(args,"--audit-gallery")>=0)
            {
                d.SetGallery(true);
                for(int a=0;a<4;a++)for(int n=0;n<d.sequence.galleryActions.Length;n++)
                {
                    d.SelectActor(a);d.SelectAction(n);d.SelectShot(ShotKind.Tracking);
                    float duration=Mathf.Max(1.4f,d.sequence.Clip(d.sequence.galleryActions[n]).length+.45f);
                    for(int f=0;f<Mathf.CeilToInt(duration*30);f++)
                    {yield return new WaitForEndOfFrame();if(f%10==0)Capture(d,"gallery_"+a+"_"+n.ToString("00"));}
                }
                for(int a=0;a<4;a++)
                {
                    d.SelectActor(a);d.SelectAction(1);
                    foreach(ShotKind shot in Enum.GetValues(typeof(ShotKind)))
                    {
                        d.SelectShot(shot);
                        for(int f=0;f<36;f++){yield return new WaitForEndOfFrame();if(f%15==0)Capture(d,"lens_"+a+"_"+(int)shot);}
                    }
                }
            }
            File.WriteAllLines(Path.Combine(output,"frames.csv"),rows);File.WriteAllText(Path.Combine(output,"complete.txt"),number+" rendered frames captured");
            Debug.Log("VISUAL_AUDIT_COMPLETE "+number);Application.Quit();
        }
        void Capture(ArchiveDirector d,string group)
        {
            string file=group+"_"+(number++).ToString("D5")+".jpg";
            var t=ScreenCapture.CaptureScreenshotAsTexture();
            int visible=0;for(int y=t.height/3;y<t.height*2/3;y+=40)for(int x=t.width/5;x<t.width*4/5;x+=40)if(t.GetPixel(x,y).grayscale>.025f)visible++;
            if(visible<10){File.WriteAllText(Path.Combine(output,"capture-failed.txt"),"Window is not rendering; do not accept black screenshots.");Debug.LogError("VISUAL_AUDIT_BLACK_FRAME");Application.Quit(2);return;}
            File.WriteAllBytes(Path.Combine(output,file),t.EncodeToJPG(86));Destroy(t);
            var c=d.cameraRig.lens;var a=d.actors[d.Focus];var cp=c.transform.position;
            var head=c.WorldToViewportPoint(a.Head.position);
            var lf=c.WorldToViewportPoint(a.Animator.GetBoneTransform(HumanBodyBones.LeftFoot).position);
            var rf=c.WorldToViewportPoint(a.Animator.GetBoneTransform(HumanBodyBones.RightFoot).position);
            var hip=a.Animator.GetBoneTransform(HumanBodyBones.Hips).position;
            var p=a.transform.position;
            rows.Add(string.Join(",",file,d.Gallery?"gallery":"story",d.Index,d.Focus,a.Action,(d.Gallery?d.GalleryTime:d.BeatTime).ToString("F3",System.Globalization.CultureInfo.InvariantCulture),cp.x,cp.y,cp.z,c.aspect,head.x,head.y,lf.x,lf.y,rf.x,rf.y,hip.y,p.x,p.z));
        }
    }
}

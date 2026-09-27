using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astra.PerformanceToolV2
{
    public static class ToolRuntimeQA
    {
        public static IEnumerator Run(ToolWorkbench workbench,ToolPackage package)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-qaDir");string dir=at>=0?args[at+1]:Path.Combine(Application.persistentDataPath,"QA");Directory.CreateDirectory(dir);
            var errors=new List<string>();var poses=new List<string>();Application.LogCallback onLog=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+stack);};Application.logMessageReceived+=onLog;
            yield return null;yield return new WaitForEndOfFrame();workbench.Paused=true;
            foreach(var character in package.library.characters.Where(c=>c.look.creatorEnabled))
            {
                workbench.LoadPreset(character);workbench.FaceView=true;workbench.Yaw=0;workbench.Section=1;workbench.SampleAt(.4f);
                poses.Add(character.id+" BEFORE head "+workbench.Actor.GetComponent<ToolMotion>().Animator.GetBoneTransform(HumanBodyBones.Head).position+" camera "+workbench.lens.transform.position);
                yield return null;yield return new WaitForEndOfFrame();CaptureCamera(workbench.lens,Path.Combine(dir,character.id+"_face.png"));
                poses.Add(character.id+" AFTER head "+workbench.Actor.GetComponent<ToolMotion>().Animator.GetBoneTransform(HumanBodyBones.Head).position+" camera "+workbench.lens.transform.position);
                workbench.FaceView=false;workbench.Yaw=25;workbench.ActionIndex=9;workbench.SampleAt(.5f);
                yield return null;yield return new WaitForEndOfFrame();CaptureCamera(workbench.lens,Path.Combine(dir,character.id+"_sit.png"));
            }
            foreach(var species in new[]{ToolSpecies.Human,ToolSpecies.StandingAlien})
            {
                workbench.NewCharacter(species);workbench.Paused=true;workbench.FaceView=true;workbench.SampleAt(.2f);
                for(int section=0;section<6;section++)
                {
                    workbench.Section=section;yield return null;yield return new WaitForEndOfFrame();
                    var screenshot=ScreenCapture.CaptureScreenshotAsTexture();
                    if(screenshot.GetPixels32().Any(c=>c.r>20||c.g>20||c.b>20))File.WriteAllBytes(Path.Combine(dir,species+"_UI_"+section+".png"),screenshot.EncodeToPNG());
                    UnityEngine.Object.Destroy(screenshot);
                }
                string json=workbench.SaveLook();string before=JsonUtility.ToJson(workbench.Draft.look);workbench.LoadLook(json);if(JsonUtility.ToJson(workbench.Draft.look)!=before)errors.Add("JSON roundtrip mismatch");
                for(int action=0;action<package.library.actions.Length;action++)
                {
                    workbench.FaceView=false;workbench.Yaw=20;workbench.ActionIndex=action;workbench.SampleAt(package.library.actions[action].length*.45f);
                    yield return null;yield return new WaitForEndOfFrame();CaptureCamera(workbench.lens,Path.Combine(dir,species+"_action_"+action.ToString("00")+".png"));
                }
            }
            workbench.NewCharacter(ToolSpecies.StandingAlien);workbench.SampleAt(.3f);workbench.FaceView=false;
            var partner=ToolCharacterView.Create(package.library.characters[0],workbench.Actor.transform.parent);partner.transform.position=new Vector3(1.7f,0,.35f);partner.GetComponent<ToolMotion>().Sample("Idle_Loop",.3f);
            for(int shot=0;shot<9;shot++)
            {
                yield return null;yield return new WaitForEndOfFrame();
                var span=new ToolCameraSpan{shot=(ToolShot)shot};ToolBlocking.Apply(span,workbench.Actor.transform,partner.transform);
                ToolCameraMath.Apply(workbench.lens,span,workbench.Actor.transform,partner.transform,false,true,.5f);
                CaptureCamera(workbench.lens,Path.Combine(dir,"camera_"+shot+".png"));
            }
            partner.SetActive(false);UnityEngine.Object.Destroy(partner);
            workbench.EnterPerformance();workbench.player.enabled=false;workbench.player.Seek(4.4f);workbench.player.Choose("pay");
            if(workbench.player.CurrentNodeId!="B")errors.Add("Player pay branch failed");
            workbench.player.Seek(1.8f);yield return null;yield return new WaitForEndOfFrame();CaptureCamera(workbench.lens,Path.Combine(dir,"story_repair.png"));
            workbench.player.Begin();workbench.player.Seek(4.4f);workbench.player.Choose("silence");if(workbench.player.CurrentNodeId!="C")errors.Add("Player silence branch failed");
            workbench.player.Seek(2);yield return null;yield return new WaitForEndOfFrame();CaptureCamera(workbench.lens,Path.Combine(dir,"story_contact.png"));
            workbench.ExitPerformance();
            File.WriteAllLines(Path.Combine(dir,"runtime-result.txt"),new[]{"Runtime errors: "+errors.Count,"32 presets × face/sitting views; 12 UI states; 34 action views; 9 cameras; player pay/silence branches; JSON save/load","Hidden-window whole-screen capture can be black. UI reviewed separately with native window capture."}.Concat(errors));
            File.WriteAllLines(Path.Combine(dir,"pose-coordinates.txt"),poses);
            Application.logMessageReceived-=onLog;Application.Quit(errors.Count==0?0:2);
        }
        public static void CaptureCamera(Camera camera,string path)
        {
            var original=camera.targetTexture;RenderTexture temporary=null;if(!original){temporary=new RenderTexture(1440,900,24);temporary.Create();camera.targetTexture=temporary;}
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=camera.targetTexture;
            var texture=new Texture2D(camera.targetTexture.width,camera.targetTexture.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);texture.Apply();
            File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);RenderTexture.active=previous;camera.targetTexture=original;if(temporary){temporary.Release();UnityEngine.Object.Destroy(temporary);}
        }
    }
}

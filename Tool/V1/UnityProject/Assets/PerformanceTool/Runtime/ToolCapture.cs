using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;

namespace Astra.PerformanceTool
{
    // Only runs when the preview executable receives --tool-qa-dir=<absolute path>.
    public sealed class ToolCapture : MonoBehaviour
    {
        IEnumerator Start()
        {
            string creatorArgument=Environment.GetCommandLineArgs().FirstOrDefault(x=>x.StartsWith("--tool-creator-qa-dir=",StringComparison.Ordinal));
            if(creatorArgument!=null)
            {
                yield return CreatorCapture(creatorArgument.Substring("--tool-creator-qa-dir=".Length).Trim('"'));
                yield break;
            }
            string argument=Environment.GetCommandLineArgs().FirstOrDefault(x=>x.StartsWith("--tool-qa-dir=",StringComparison.Ordinal));
            if(argument==null)yield break;
            string root=argument.Substring("--tool-qa-dir=".Length).Trim('"');
            Directory.CreateDirectory(root);
            yield return null;
            var player=GetComponent<ToolPlayer>();
            if(!player){Debug.LogError("Tool QA: missing player");Application.Quit(2);yield break;}
            player.Begin();
            player.Seek(1.5f);yield return Capture(root,"01_A_medium.png");
            player.Seek(4.5f);yield return Capture(root,"02_A_choice_close.png");
            player.Choose("pay");
            if(player.CurrentNodeId!="B"||player.Stats["credits"]!=1)throw new InvalidOperationException("Paid branch or credit deduction failed");
            player.Seek(.8f);yield return Capture(root,"03_B_tracking.png");
            player.Seek(3.6f);yield return Capture(root,"04_B_low_angle.png");
            player.Begin();player.Seek(4.5f);player.Choose("inspect");
            if(player.CurrentNodeId!="C")throw new InvalidOperationException("Inspect branch failed");
            player.Seek(.9f);yield return Capture(root,"05_C_crawler_tracking.png");
            player.Seek(3.8f);yield return Capture(root,"06_C_crawler_profile.png");
            player.Begin();
            var unit=player.package.graph.Node("A").unit;
            var original=unit.cameras.ToArray();
            string cameraActorId=unit.actors.FirstOrDefault()?.id??"speaker";
            for(int i=0;i<9;i++)
            {
                unit.cameras.Clear();
                unit.cameras.Add(new ToolCameraSpan{shot=(ToolShot)i,actorId=cameraActorId,start=0,duration=unit.duration});
                player.Seek(1.5f);
                yield return Capture(root,string.Format("shot_{0:D2}_{1}.png",i,(ToolShot)i));
            }
            unit.cameras.Clear();unit.cameras.AddRange(original);
            File.WriteAllText(Path.Combine(root,"capture-complete.txt"),"15 camera images captured\nFinal node: "+player.CurrentNodeId+"\n");
            Application.Quit(0);
        }
        IEnumerator CreatorCapture(string root)
        {
            Directory.CreateDirectory(root);yield return null;
            var player=GetComponent<ToolPlayer>();
            if(!player||!player.package){Debug.LogError("Creator QA: missing player");Application.Quit(2);yield break;}
            var unit=player.package.graph.Node("A").unit;
            var original=unit.actors[0].character;
            var cameras=unit.cameras.ToArray();
            unit.cameras.Clear();unit.cameras.Add(new ToolCameraSpan{shot=ToolShot.Close,actorId=unit.actors[0].id,start=0,duration=unit.duration});
            var characters=player.package.library.characters.Where(c=>c&&c.look.creatorEnabled).ToArray();
            foreach(var character in characters)
            {
                unit.actors[0].character=character;
                player.Begin();player.Seek(1.5f);
                yield return Capture(root,character.id+"_front.png");
                unit.cameras[0].shot=ToolShot.Profile;player.Seek(1.5f);
                yield return Capture(root,character.id+"_profile.png");
                unit.cameras[0].shot=ToolShot.Close;
            }
            unit.actors[0].character=original;unit.cameras.Clear();unit.cameras.AddRange(cameras);
            File.WriteAllText(Path.Combine(root,"capture-complete.txt"),characters.Length+" creator characters, front and profile captured\n");
            Application.Quit(0);
        }
        IEnumerator Capture(string root,string name)
        {
            yield return new WaitForEndOfFrame();
            var camera=GetComponent<ToolPlayer>().lens;
            const int width=1280,height=720;
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);
            var priorTarget=camera.targetTexture;
            var priorActive=RenderTexture.active;
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var texture=new Texture2D(width,height,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,width,height),0,0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(root,name),texture.EncodeToPNG());
            camera.targetTexture=priorTarget;RenderTexture.active=priorActive;
            Destroy(texture);Destroy(target);
            yield return null;
        }
    }
}

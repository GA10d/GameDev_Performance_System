using UnityEngine;

namespace Astra.Performance
{
    [RequireComponent(typeof(Camera))]
    public sealed class SignalRenderer : MonoBehaviour
    {
        public Shader gradeShader;
        public bool stylized=true;
        public bool signal;
        Material grade;
        void OnEnable(){if(gradeShader)grade=new Material(gradeShader);}
        void OnRenderImage(RenderTexture source,RenderTexture destination)
        {
            if(!grade || !stylized){Graphics.Blit(source,destination);return;}
            grade.SetFloat("_Strength",1);grade.SetFloat("_Signal",signal?1:0);
            // Remote camera is already low resolution. Cabin is downsampled after rendering.
            if(signal){Graphics.Blit(source,destination,grade);return;}
            var small=RenderTexture.GetTemporary(Mathf.Max(1,source.width/2),Mathf.Max(1,source.height/2),0,source.format);
            small.filterMode=FilterMode.Point;
            Graphics.Blit(source,small,grade);Graphics.Blit(small,destination);RenderTexture.ReleaseTemporary(small);
        }
        void OnDisable(){if(grade)Destroy(grade);}
    }
}

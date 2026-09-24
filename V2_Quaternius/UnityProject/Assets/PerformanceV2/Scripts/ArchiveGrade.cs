using UnityEngine;
namespace Astra.PerformanceV2
{
    [RequireComponent(typeof(Camera))]
    public sealed class ArchiveGrade:MonoBehaviour
    {
        public Shader shader;public bool Stylized=true;
        Material mat;
        void OnEnable(){mat=new Material(shader);}
        void OnRenderImage(RenderTexture src,RenderTexture dest)
        {
            if(!Stylized||!mat){Graphics.Blit(src,dest);return;}
            var small=RenderTexture.GetTemporary(Mathf.Max(1,src.width/2),Mathf.Max(1,src.height/2),0,src.format);small.filterMode=FilterMode.Point;
            mat.SetFloat("_Strength",.8f);mat.SetFloat("_Signal",0);Graphics.Blit(src,small,mat);Graphics.Blit(small,dest);RenderTexture.ReleaseTemporary(small);
        }
        void OnDisable(){if(mat)Destroy(mat);}
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Astra.PerformanceTool
{
    public sealed class ToolStage : MonoBehaviour
    {
        Material wallMaterial,floorMaterial,metalMaterial;
        Light key;
        public bool IsBuilt => wallMaterial && floorMaterial && key;
        public void Build()
        {
            foreach(Transform child in transform)DisposeObject(child.gameObject);
            wallMaterial=Mat(new Color(.29f,.32f,.28f));floorMaterial=Mat(new Color(.14f,.17f,.14f));metalMaterial=Mat(new Color(.12f,.14f,.12f));
            Box("floor",new Vector3(0,-.075f,.1f),new Vector3(9,.15f,7),floorMaterial);
            Box("back wall",new Vector3(0,1.65f,1.5f),new Vector3(9,3.3f,.12f),wallMaterial);
            Box("left side",new Vector3(-4.4f,1.65f,-.2f),new Vector3(.1f,3.3f,3.6f),wallMaterial);
            Box("right side",new Vector3(4.4f,1.65f,-.2f),new Vector3(.1f,3.3f,3.6f),wallMaterial);
            for(int i=-6;i<=6;i++)Box("panel seam",new Vector3(i*.62f,1.6f,1.425f),new Vector3(.018f,3.2f,.02f),metalMaterial);
            Box("relay cabinet",new Vector3(2.8f,.8f,1.12f),new Vector3(.65f,1.45f,.5f),metalMaterial);
            Box("status light",new Vector3(2.8f,1.27f,.86f),new Vector3(.28f,.05f,.025f),Mat(new Color(.48f,.72f,.29f),true));
            var light=new GameObject("stage key light");light.transform.SetParent(transform,false);light.transform.localPosition=new Vector3(-1.2f,2.9f,-2.2f);
            light.transform.LookAt(transform.TransformPoint(new Vector3(0,1.25f,0)));
            key=light.AddComponent<Light>();key.type=LightType.Spot;key.range=9;key.spotAngle=105;key.intensity=2.25f;
            var fill=new GameObject("stage fill light");fill.transform.SetParent(transform,false);fill.transform.localPosition=new Vector3(2.2f,1.6f,-2.0f);
            var f=fill.AddComponent<Light>();f.type=LightType.Point;f.range=7;f.intensity=.85f;f.color=new Color(.70f,.82f,.76f);
            var bounce=new GameObject("stage front bounce");bounce.transform.SetParent(transform,false);
            bounce.transform.localRotation=Quaternion.Euler(30,-25,0);
            var b=bounce.AddComponent<Light>();b.type=LightType.Directional;b.intensity=.22f;b.color=new Color(.80f,.86f,.82f);
        }
        public void Set(ToolScenePreset scene,ToolFx fx,float time)
        {
            if(scene==null)return;
            wallMaterial.color=scene.wall;floorMaterial.color=scene.floor;
            key.color=fx==ToolFx.EmergencyLight?Color.Lerp(scene.light,new Color(1,.10f,.05f),.65f+.3f*Mathf.Sin(time*9)):scene.light;
        }
        void Box(string name,Vector3 p,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(transform,false);
            go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
            DisposeObject(go.GetComponent<Collider>());
        }
        static Material Mat(Color color,bool emissive=false)
        {
            var shader=Shader.Find("Standard");
            if(!shader)throw new InvalidOperationException("Standard shader is missing from player build");
            var m=new Material(shader);m.color=color;
            if(emissive){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.5f);}
            return m;
        }
        static void DisposeObject(UnityEngine.Object o) {if(!o)return;if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
    }
}

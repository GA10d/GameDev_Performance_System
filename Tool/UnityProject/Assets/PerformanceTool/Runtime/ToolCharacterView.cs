using System;
using System.Linq;
using UnityEngine;

namespace Astra.PerformanceTool
{
    public static class ToolCharacterView
    {
        public static GameObject Create(ToolCharacter preset, Transform parent, bool addMotion = true)
        {
            if (!preset || !preset.sourceModel) return null;
            var actor = UnityEngine.Object.Instantiate(preset.sourceModel, parent);
            actor.name = preset.displayName + " / " + preset.id;
            actor.transform.localPosition = Vector3.zero;
            actor.transform.localRotation = Quaternion.Euler(0,180,0);
            ApplyLook(actor, preset);
            if (addMotion) actor.AddComponent<ToolMotion>().Bind(preset.species);
            return actor;
        }
        public static void ApplyLook(GameObject actor, ToolCharacter preset)
        {
            var look = preset.look;
            actor.transform.localScale = Vector3.one * look.bodyScale;
            var head = Find(actor.transform,"Head");
            if (head) head.localScale = new Vector3(look.faceWidth,1,1);
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i=0;i<materials.Length;i++)
                {
                    string name = materials[i] ? materials[i].name.Replace(" (Instance)","") : "";
                    string sourceName = name;
                    if (sourceName.StartsWith("Actor") && sourceName.Contains("_")) sourceName = sourceName.Substring(sourceName.IndexOf('_')+1);
                    var replacement = preset.v3Materials.FirstOrDefault(m => m && m.name.EndsWith("_"+sourceName));
                    if (replacement) materials[i] = replacement;
                }
                renderer.sharedMaterials = materials;
                for (int i=0;i<materials.Length;i++)
                {
                    if (!materials[i]) continue;
                    string name = materials[i].name;
                    Color tint = Color.white;
                    if (name.Contains("Skin") || name.Contains("Alien") || name.Contains("Orc") || name.Contains("Crawler")) tint = look.skin;
                    else if (name.Contains("Suit") || name.Contains("Plate")) tint = look.suit;
                    else if (name.Contains("Hair")) tint = look.hair;
                    else continue;
                    // MaterialPropertyBlock avoids changing V3's shared source materials.
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block,i);
                    block.SetColor("_Color",tint);
                    renderer.SetPropertyBlock(block,i);
                }
            }
            if (!head) return;
            var oldHeadgear = head.Find("Tool headgear");
            if(oldHeadgear)Remove(oldHeadgear.gameObject);
            var chest = Find(actor.transform,"Chest");
            var oldAccessory=chest?chest.Find("Tool accessory"):null;
            if(oldAccessory)Remove(oldAccessory.gameObject);
            if (look.hairOrHeadgear > 0)
            {
                Primitive("Tool headgear",head,new Vector3(0,.11f,0),
                    look.hairOrHeadgear==1?new Vector3(.25f,.08f,.22f):new Vector3(.16f,.16f,.12f),preset.accessoryMaterial);
            }
            if (look.accessory > 0)
            {
                if (chest)
                {
                    Primitive("Tool accessory",chest,new Vector3(.13f,.02f,.08f),
                        look.accessory==1?new Vector3(.09f,.13f,.05f):new Vector3(.17f,.05f,.05f),preset.accessoryMaterial);
                }
            }
        }
        static void Remove(GameObject go) {if(Application.isPlaying)UnityEngine.Object.Destroy(go);else UnityEngine.Object.DestroyImmediate(go);}
        static GameObject Primitive(string name,Transform parent,Vector3 localPosition,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=localPosition;go.transform.localScale=scale;
            var collider=go.GetComponent<Collider>();if(collider)UnityEngine.Object.DestroyImmediate(collider);
            if(material)go.GetComponent<Renderer>().sharedMaterial=material;
            return go;
        }
        public static Transform Find(Transform root,string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name==name) return t;
            return null;
        }
    }

    // V2's action catalogue is interpreted as deterministic poses for V3's named bones.
    // This keeps scrubbing and game playback identical, including the four-legged model.

}

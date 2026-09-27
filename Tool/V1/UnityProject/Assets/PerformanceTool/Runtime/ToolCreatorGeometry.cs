using UnityEngine;

namespace Astra.PerformanceTool
{
    // Additive parts follow the original V3 bones. The FBX skin, bind poses and V2 motion stay intact.
    public static class ToolCreatorGeometry
    {
        static readonly Vector2[] FaceScale={new Vector2(1,1),new Vector2(1.09f,.94f),new Vector2(1.13f,1),new Vector2(.89f,1.07f),new Vector2(1.06f,1.02f),new Vector2(1.11f,.97f),new Vector2(.87f,1),new Vector2(.93f,1.12f),new Vector2(1.02f,.98f),new Vector2(.96f,1.05f)};
        static readonly Vector2[] BodyScale={new Vector2(1,1),new Vector2(.88f,1),new Vector2(1.13f,1),new Vector2(1.11f,.90f),new Vector2(.92f,1.10f),new Vector2(1.17f,1.02f),new Vector2(.86f,.96f),new Vector2(.98f,1.12f)};
        static GameObject Part(string name, Mesh mesh, Transform joint, Transform root, Vector3 point, Vector3 scale, Color color, Material material, Vector3 angles=default)
        {
            if(!mesh||!material||!joint)return null;
            var go=new GameObject(name);
            // The FBX bone chain has import scale; only the explicit creator shape scale is relevant here.
            var factor=joint.parent.localScale;
            var pivot=root.InverseTransformPoint(joint.position);
            go.transform.position=root.TransformPoint(pivot+Vector3.Scale(point-pivot,factor));
            go.transform.rotation=root.rotation*Quaternion.Euler(angles);
            go.transform.localScale=Vector3.Scale(scale,factor);
            go.transform.SetParent(joint,true);
            var filter=go.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            var block=new MaterialPropertyBlock();block.SetColor("_Color",color);renderer.SetPropertyBlock(block);
            return go;
        }
        static void Remove(Transform parent,string name)
        {
            if(!parent)return;var old=parent.Find(name);if(!old)return;
            old.gameObject.SetActive(false);
            if(Application.isPlaying)Object.Destroy(old.gameObject);else Object.DestroyImmediate(old.gameObject);
        }
        public static void Apply(GameObject actor,ToolCharacter preset)
        {
            var resources=preset.creatorResources;
            if(!resources||!resources.solidMaterial)return;
            var look=preset.look;bool alien=ToolCreatorOptions.Alien(preset.species);
            var head=ToolCharacterView.Find(actor.transform,"Head");
            var chest=ToolCharacterView.Find(actor.transform,"Chest");
            if(!head||!chest)return;
            Remove(head,"Creator Head Parts");Remove(chest,"Creator Chest Parts");
            var face=FaceScale[Mathf.Clamp(look.faceShape,0,9)];
            head.localScale=new Vector3(face.x*look.faceWidth,face.y,1);
            var body=BodyScale[Mathf.Clamp(look.bodyType,0,7)];
            chest.localScale=new Vector3(body.x,body.y,body.x*.98f);
            var h=new GameObject("Creator Head Parts").transform;h.SetParent(head,false);
            var c=new GameObject("Creator Chest Parts").transform;c.SetParent(chest,false);
            float cy=alien?1.77f:1.565f, front=alien?.095f:.112f, eyeY=cy+(alien?.043f:.035f);
            var skin=look.skin;var shadow=Color.Lerp(skin,Color.black,.32f);
            var light=Color.Lerp(skin,Color.white,.24f);var dark=Color.Lerp(look.hair,Color.black,.35f);
            var white=alien?Color.Lerp(look.eyes,Color.white,.2f):new Color(.78f,.75f,.65f);
            var metal=new Color(.18f,.20f,.18f);var accent=look.accent;
            void Add(string n,Mesh m,Transform j,Vector3 p,Vector3 s,Color color,Vector3 a=default)
                =>Part(n,m,j,actor.transform,p,s,color,resources.solidMaterial,a);
            // Face shape adds a readable silhouette over the V3 base mesh.
            int fs=Mathf.Clamp(look.faceShape,0,9);
            if(fs==1||fs==4||fs==8){for(int side=-1;side<=1;side+=2)Add("Cheek",resources.facetedOrb,h,new Vector3(side*.101f,cy-.052f,front-.036f),new Vector3(.057f,.042f,.044f),skin);}
            if(fs==2||fs==5||fs==9)Add("Jaw",resources.wedge,h,new Vector3(0,cy-.118f,front-.048f),new Vector3(.19f,.045f,.09f),shadow);
            if(fs==3||fs==6)Add("Chin",resources.diamond,h,new Vector3(0,cy-.126f,front-.019f),new Vector3(.073f,.048f,.045f),skin);
            if(fs==4||fs==8||fs==9)Add("Brow ridge",resources.wedge,h,new Vector3(0,cy+.081f,front-.025f),new Vector3(.18f,.034f,.058f),light);
            // Eye geometry is deliberately outside the original face. All options change proportions or structure.
            int eye=Mathf.Clamp(look.eyeShape,0,11);int count=alien&&eye==3?4:2;
            float spacing=(eye==6?.071f:eye==7?.044f:.066f);
            for(int i=0;i<count;i++)
            {
                float x=count==4?(i-1.5f)*.050f:(i==0?-spacing:spacing);
                float y=eyeY+(!alien&&eye==6?-.03f:0)+(count==4&&(i==0||i==3)?-.044f:0);
                float width=eye==1?.050f:eye==2||eye==9?.036f:eye==5?.044f:.046f;
                float height=eye==1?.043f:eye==2||eye==9?.021f:eye==4?.029f:.031f;
                if(alien){width*=1.17f;height*=1.16f;}
                float tilt=eye==3||eye==8?(x<0?-15:15):eye==4?(x<0?11:-11):0;
                Add("Eye rim",resources.facetedOrb,h,new Vector3(x,y,front+.002f),new Vector3(width*1.12f,height*1.15f,.021f),shadow,new Vector3(0,0,tilt));
                Add("Eye",eye==5?resources.diamond:resources.facetedOrb,h,new Vector3(x,y,front+.021f),new Vector3(width,height,.013f),white,new Vector3(0,0,tilt));
                if(eye!=11)
                {
                    float pupil=eye==10?.016f:eye==11?.004f:.010f;
                    Add("Pupil",eye==8||eye==5?resources.diamond:resources.facetedOrb,h,new Vector3(x+(eye==3?.004f:0),y,front+.033f),new Vector3(pupil,eye==1||eye==8?pupil*1.45f:pupil,.006f),look.eyes);
                }
                if(eye==9||eye==10)Add("Eyelid",resources.wedge,h,new Vector3(x,y+height*.83f,front+.026f),new Vector3(width*1.9f,.009f,.012f),skin);
            }
            int brow=Mathf.Clamp(look.browShape,0,7);
            if(brow!=7)for(int side=-1;side<=1;side+=2)
                Add("Brow",resources.wedge,h,new Vector3(side*spacing,eyeY+.061f-(brow==5?.011f:0),front+.021f),
                    new Vector3(brow==3?.081f:.067f,brow==3?.022f:.014f,.013f),alien?shadow:look.hair,new Vector3(0,0,side*(brow==2?18:brow==4?-18:brow==6?11:0)));
            int nose=Mathf.Clamp(look.noseShape,0,7);
            if(nose==7||alien)
            {
                for(int side=-1;side<=1;side+=2)Add("Breathing slit",resources.wedge,h,new Vector3(side*.026f,cy-.029f,front+.019f),new Vector3(.019f,.008f,.008f),shadow);
            }
            else Add("Nose",nose==6?resources.diamond:resources.wedge,h,new Vector3(0,cy-.026f,front+.012f),
                new Vector3(nose==2?.064f:nose==1?.032f:.043f,nose==4?.054f:nose==3?.021f:.036f,.033f),light);
            int mouth=Mathf.Clamp(look.mouthShape,0,7);
            if(mouth!=7)Add("Mouth",resources.wedge,h,new Vector3(0,cy-.083f,front+.023f),
                new Vector3(mouth==2?.105f:.077f,mouth==5?.018f:.008f,.008f),mouth==5?Color.Lerp(skin,look.accent,.35f):shadow,new Vector3(0,0,mouth==3?7:mouth==4?-7:0));
            // 16 distinct hair / alien crown silhouettes.
            int hair=Mathf.Clamp(look.hairStyle,0,15);
            float top=alien?1.986f:1.675f;
            if(hair>0)
            {
                if(!alien)
                {
                    if(hair==1)Add("Buzz cut",resources.facetedOrb,h,new Vector3(0,top+.005f,-.003f),new Vector3(.28f,.07f,.20f),look.hair);
                    else
                    {
                        if(hair!=7&&hair!=9&&hair!=10)Add("Hair cap",resources.facetedOrb,h,new Vector3(0,top+.025f,-.003f),new Vector3(.17f,.068f,.14f),look.hair);
                        int tufts=hair==6?7:hair==11?5:hair==12?4:hair==15?2:3;
                        for(int i=0;i<tufts;i++)
                        {
                            float x=(i-(tufts-1)*.5f)*(.23f/Mathf.Max(1,tufts-1));
                            float height=hair==7?.16f:hair==10?.14f:hair==12?.07f:hair==6?.115f:.085f;
                            if(hair==2||hair==13)height*=1+(x<0?.6f:-.25f);
                            Add("Hair lock",hair==6||hair==11?resources.diamond:hair==7||hair==10?resources.wedge:resources.box,h,new Vector3(x,top+height*.40f,hair==4||hair==13?.09f:0),
                                new Vector3(.055f,height,.065f),look.hair,new Vector3(hair==4?27:0,0,hair==2?15:0));
                        }
                    }
                    if(hair==8||hair==9||hair==11)for(int side=-1;side<=1;side+=2)Add("Long side hair",resources.wedge,h,new Vector3(side*.125f,top-.115f,.010f),new Vector3(.047f,.19f,.075f),look.hair);
                    if(hair==9||hair==10)Add("Back hair",resources.facetedOrb,h,new Vector3(0,top-.075f,-.105f),new Vector3(.071f,.115f,.065f),look.hair);
                    if(hair==14)Add("Hat hair brim",resources.box,h,new Vector3(0,top+.034f,.068f),new Vector3(.30f,.023f,.13f),dark);
                }
                else
                {
                    int spikes=hair==2||hair==10?2:hair==13?6:hair==15?3:hair==5?4:hair==8?5:hair==6?5:1;
                    for(int i=0;i<spikes;i++)
                    {
                        float x=(i-(spikes-1)*.5f)*(.23f/Mathf.Max(1,spikes-1));
                        float length=hair==12||hair==10?.19f:hair==9?.17f:hair==11?.07f:.12f;
                        float z=hair==3||hair==14?-.07f:hair==4?.08f:0;
                        Add("Crown segment",hair==13?resources.diamond:resources.wedge,h,new Vector3(x,top+length*.36f,z),new Vector3(spikes>3?.045f:.07f,length,.063f),
                            hair==13?accent:Color.Lerp(skin,accent,.45f),new Vector3(hair==3?25:0,0,spikes==2?(i==0?-22:22):0));
                    }
                    if(hair==7||hair==8)for(int side=-1;side<=1;side+=2)Add("Side fin",resources.wedge,h,new Vector3(side*.137f,top-.08f,0),new Vector3(.06f,.14f,.09f),accent,new Vector3(0,0,side*28));
                    if(hair==5)for(int side=-1;side<=1;side+=2)Add("Antenna",resources.diamond,h,new Vector3(side*.12f,top+.05f,.05f),new Vector3(.029f,.17f,.029f),accent,new Vector3(0,0,side*-35));
                }
            }
            // Outfit details leave the deforming source body exposed and follow the torso.
            int outfit=Mathf.Clamp(look.outfitStyle,0,11);
            float chestY=alien?1.29f:1.18f;
            if(outfit>0)
            {
                if(outfit==1||outfit==7||outfit==11)Add("Vest",resources.box,c,new Vector3(0,chestY,.105f),new Vector3(.33f,.30f,.035f),Color.Lerp(look.suit,metal,.35f));
                if(outfit==2||outfit==10)Add("Apron",resources.box,c,new Vector3(0,chestY-.10f,.118f),new Vector3(.24f,.30f,.027f),accent);
                if(outfit==3||outfit==8)Add("Lapels",resources.wedge,c,new Vector3(0,chestY+.09f,.123f),new Vector3(.20f,.10f,.030f),light);
                if(outfit==4||outfit==9)Add("Medical panel",resources.box,c,new Vector3(.095f,chestY+.03f,.132f),new Vector3(.10f,.15f,.024f),new Color(.62f,.68f,.60f));
                if(outfit==5||outfit==6||outfit==11)for(int side=-1;side<=1;side+=2)Add("Harness",resources.box,c,new Vector3(side*.105f,chestY,.134f),new Vector3(.035f,.34f,.025f),accent,new Vector3(0,0,side*11));
                if(outfit==6||outfit==7||outfit==11)for(int side=-1;side<=1;side+=2)Add("Shoulder plate",resources.wedge,c,new Vector3(side*.20f,chestY+.13f,.055f),new Vector3(.12f,.055f,.12f),metal);
                if(outfit==8||outfit==9||outfit==10)Add("Collar",resources.facetedOrb,c,new Vector3(0,chestY+.18f,.025f),new Vector3(.20f,.047f,.14f),dark);
            }
            int fa=Mathf.Clamp(look.faceAccessory,0,11);
            if(fa>0)
            {
                if(fa==1||fa==10)for(int side=-1;side<=1;side+=2)
                {
                    float x=side*spacing;
                    for(int edge=-1;edge<=1;edge+=2)
                    {
                        Add("Goggle horizontal rim",resources.box,h,new Vector3(x,eyeY+edge*.033f,front+.047f),new Vector3(.093f,.009f,.016f),fa==10?accent:metal);
                        Add("Goggle vertical rim",resources.box,h,new Vector3(x+edge*.047f,eyeY,front+.047f),new Vector3(.009f,.064f,.016f),fa==10?accent:metal);
                    }
                }
                if(fa==2||fa==10)for(int edge=-1;edge<=1;edge+=2)Add("Monocle arc",resources.box,h,new Vector3(-spacing+edge*.039f,eyeY,front+.054f),new Vector3(.008f,.069f,.014f),accent);
                if(fa==3||fa==4||fa==11)Add("Face guard",resources.box,h,new Vector3(0,cy-.07f,front+.055f),new Vector3(fa==11?.16f:.20f,fa==4?.11f:.075f,.030f),metal);
                if(fa==5)Add("Headlamp",resources.box,h,new Vector3(0,top+.025f,.13f),new Vector3(.08f,.046f,.032f),accent);
                if(fa==6)Add("Receiver",resources.box,h,new Vector3(.155f,cy,.015f),new Vector3(.035f,.095f,.062f),accent);
                if(fa==7||fa==9)Add("Face patch",resources.wedge,h,new Vector3(.09f,cy-.055f,front+.022f),new Vector3(.057f,.028f,.010f),accent);
                if(fa==8)Add("Headband",resources.box,h,new Vector3(0,top-.05f,.11f),new Vector3(.26f,.023f,.025f),accent);
            }
            int gear=Mathf.Clamp(look.gearAccessory,0,11);
            if(gear>0)
            {
                Vector3 p=new Vector3(gear%2==0?.14f:-.13f,chestY+.035f,.155f);
                Mesh m=gear==2||gear==5||gear==10?resources.box:gear==8||gear==11?resources.diamond:resources.wedge;
                Add("Gear "+gear,m,c,p,new Vector3(gear==9?.13f:.085f,gear==3?.04f:.11f,.029f),gear==1||gear==3?accent:metal);
                if(gear==4||gear==8||gear==11)Add("Gear light",resources.diamond,c,p+new Vector3(0,.01f,.024f),new Vector3(.030f,.030f,.009f),accent);
            }
            int mark=Mathf.Clamp(look.facialMark,0,7);
            if(mark>0)
            {
                int stripes=mark==4?2:1;
                for(int i=0;i<stripes;i++)Add("Face mark",resources.wedge,h,new Vector3(mark==2?.09f:mark==3||mark==6?0:-.09f+(i*.18f),mark==3?cy+.102f:mark==7?cy-.10f:cy-.052f,front+.029f),new Vector3(.045f,.008f,.008f),accent,new Vector3(0,0,mark==1?-28:mark==2?28:0));
            }
        }
    }
}

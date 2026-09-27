using System;
using UnityEngine;

namespace Astra.PerformanceTool
{
    public static class ToolCreatorOptions
    {
        public static readonly string[] HumanBases={"林式·瘦长","赫式·结实","余式·轻盈"};
        public static readonly string[] AlienBases={"弦式·长颅"};
        public static readonly string[] HumanFaces={"原型","圆颊","方颌","尖颌","高颧","宽额","窄脸","长脸","柔和","棱角"};
        public static readonly string[] AlienFaces={"原型","细长","宽颅","楔形","高冠","窄颊","厚颌","短颅","棱脊","弧形"};
        public static readonly string[] HumanEyes={"杏眼","圆眼","细眼","上扬","下垂","深眶","宽距","近距","方瞳","双层眼睑","单目镜式","小瞳"};
        public static readonly string[] AlienEyes={"椭圆","竖瞳","狭缝","四眼","复眼","菱形","宽距","近距","上扬","深眶","亮环","无瞳"};
        public static readonly string[] HumanHair={"无发","寸头","偏分","短碎发","厚刘海","后梳","卷簇","莫西干","长侧发","低马尾","顶髻","编束","平顶","斜刘海","帽檐发","侧削"};
        public static readonly string[] AlienHair={"无冠","颅脊","双角","背鳍","额冠","触须","分节冠","侧翼","环冠","顶刺","弧角","短鳍","长鳍","晶簇","后冠","三脊"};
        public static readonly string[] Brows={"自然","平直","锐角","浓重","上挑","低压","断眉","无眉"};
        public static readonly string[] Noses={"原型","窄鼻","宽鼻","短鼻","高鼻梁","扁鼻","棱鼻","呼吸孔"};
        public static readonly string[] Mouths={"自然","细线","宽口","下弯","上扬","厚唇","呼吸缝","无口"};
        public static readonly string[] Bodies={"标准","纤细","宽肩","矮壮","修长","厚重","窄肩","高挑"};
        public static readonly string[] Outfits={"工作制服","防护背心","维修围裙","档案员","医务层","驾驶服","侦察束带","警戒甲片","正式外套","通信员","补丁装","轻型战术"};
        public static readonly string[] FaceAccessories={"无","护目镜","单目镜","面罩","呼吸器","头灯","耳侧接收器","眼下贴片","额带","面部刻痕","镜片","下颌护片"};
        public static readonly string[] GearAccessories={"无","工牌","无线电","肩章","工具包","胸前终端","袖标","腰带扣","探针","护肩","资料匣","信号环"};
        public static readonly string[] Marks={"无","左颊疤","右颊疤","额纹","双颊纹","眼下线","鼻梁纹","下颌纹"};
        public static readonly Color[] HumanSkin={
            C(0.78f,.62f,.49f),C(.68f,.52f,.40f),C(.58f,.42f,.31f),C(.46f,.32f,.24f),
            C(.35f,.23f,.18f),C(.25f,.17f,.14f),C(.69f,.57f,.47f),C(.52f,.40f,.34f),
            C(.78f,.68f,.57f),C(.63f,.48f,.37f),C(.43f,.31f,.25f),C(.31f,.22f,.18f),
            C(.72f,.58f,.48f),C(.56f,.43f,.35f),C(.41f,.30f,.24f),C(.30f,.22f,.19f)
        };
        public static readonly Color[] AlienSkin={
            C(.47f,.58f,.49f),C(.35f,.48f,.49f),C(.53f,.55f,.43f),C(.39f,.45f,.61f),
            C(.59f,.50f,.48f),C(.42f,.53f,.43f),C(.66f,.62f,.48f),C(.34f,.50f,.57f),
            C(.56f,.59f,.56f),C(.38f,.40f,.45f),C(.61f,.55f,.62f),C(.51f,.65f,.54f),
            C(.52f,.42f,.45f),C(.42f,.60f,.58f),C(.63f,.56f,.42f),C(.44f,.48f,.37f)
        };
        public static readonly Color[] HairColors={
            C(.08f,.09f,.08f),C(.17f,.13f,.11f),C(.32f,.22f,.16f),C(.48f,.36f,.24f),
            C(.60f,.52f,.36f),C(.67f,.65f,.56f),C(.38f,.39f,.38f),C(.72f,.72f,.65f),
            C(.43f,.20f,.15f),C(.29f,.31f,.24f),C(.25f,.38f,.38f),C(.43f,.39f,.49f),
            C(.46f,.49f,.34f),C(.19f,.27f,.31f),C(.53f,.43f,.40f),C(.31f,.24f,.30f)
        };
        public static readonly Color[] SuitColors={
            C(.35f,.40f,.33f),C(.29f,.36f,.39f),C(.43f,.39f,.30f),C(.32f,.33f,.31f),
            C(.42f,.30f,.27f),C(.36f,.43f,.40f),C(.44f,.42f,.37f),C(.27f,.31f,.27f),
            C(.48f,.46f,.34f),C(.33f,.36f,.44f),C(.39f,.30f,.35f),C(.41f,.42f,.44f),
            C(.47f,.35f,.29f),C(.28f,.39f,.34f),C(.40f,.45f,.35f),C(.32f,.35f,.38f)
        };
        public static readonly Color[] EyeColors={
            C(.15f,.23f,.18f),C(.25f,.37f,.31f),C(.31f,.27f,.18f),C(.24f,.31f,.42f),
            C(.38f,.31f,.29f),C(.09f,.14f,.13f),C(.48f,.40f,.21f),C(.26f,.39f,.40f),
            C(.39f,.45f,.33f),C(.32f,.26f,.40f),C(.55f,.49f,.36f),C(.48f,.55f,.53f)
        };
        public static readonly Color[] AccentColors={
            C(.61f,.50f,.30f),C(.60f,.37f,.28f),C(.42f,.57f,.38f),C(.43f,.55f,.58f),
            C(.59f,.58f,.47f),C(.40f,.44f,.50f),C(.66f,.49f,.39f),C(.39f,.48f,.33f),
            C(.58f,.56f,.35f),C(.51f,.40f,.46f),C(.44f,.54f,.52f),C(.49f,.44f,.32f)
        };
        static Color C(float r,float g,float b)=>new Color(r,g,b,1);
        public static bool Alien(ToolSpecies species)=>species==ToolSpecies.StandingAlien;
        public static string[] Faces(ToolSpecies species)=>Alien(species)?AlienFaces:HumanFaces;
        public static string[] Eyes(ToolSpecies species)=>Alien(species)?AlienEyes:HumanEyes;
        public static string[] Hair(ToolSpecies species)=>Alien(species)?AlienHair:HumanHair;
        public static Color[] Skin(ToolSpecies species)=>Alien(species)?AlienSkin:HumanSkin;
        public static void Randomize(ToolCharacterLook look,ToolSpecies species,System.Random random)
        {
            look.creatorEnabled=true;
            look.faceShape=random.Next(Faces(species).Length);look.eyeShape=random.Next(Eyes(species).Length);
            look.browShape=random.Next(Brows.Length);look.noseShape=random.Next(Noses.Length);
            look.mouthShape=random.Next(Mouths.Length);look.hairStyle=random.Next(Hair(species).Length);
            look.bodyType=random.Next(Bodies.Length);look.outfitStyle=random.Next(Outfits.Length);
            look.faceAccessory=random.Next(FaceAccessories.Length);look.gearAccessory=random.Next(GearAccessories.Length);
            look.facialMark=random.Next(Marks.Length);look.skin=Skin(species)[random.Next(Skin(species).Length)];
            look.hair=HairColors[random.Next(HairColors.Length)];look.suit=SuitColors[random.Next(SuitColors.Length)];
            look.eyes=EyeColors[random.Next(EyeColors.Length)];look.accent=AccentColors[random.Next(AccentColors.Length)];
            look.faceWidth=1;look.bodyScale=1;
        }
    }
}

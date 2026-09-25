Shader "Astra/Performance/Character"
{
    Properties
    {
        _Color ("Palette tint", Color)=(.5,.5,.4,1)
        _Bands ("Lighting bands", Range(2,8))=5
        _Stylized ("Stylization", Range(0,1))=1
        _SurfaceScale ("Material grain", Float)=65
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Banded fullforwardshadows addshadow
        #pragma target 3.0
        #include "UnityPBSLighting.cginc"
        half4 _Color; half _Bands, _Stylized; float _SurfaceScale;
        struct Input { float3 worldPos; };
        half4 LightingBanded(SurfaceOutput s, half3 lightDir, half atten)
        {
            half ndl=saturate(dot(s.Normal,lightDir));
            half stepped=floor(ndl*(_Bands-1)+.5)/max(1,_Bands-1);
            half diffuse=lerp(ndl,stepped,_Stylized);
            return half4(s.Albedo*_LightColor0.rgb*diffuse*atten,s.Alpha);
        }
        void surf(Input IN,inout SurfaceOutput o)
        {
            float3 p=floor(IN.worldPos*_SurfaceScale);
            half grain=frac(sin(dot(p,float3(12.31,41.23,72.19)))*413.5);
            o.Albedo=_Color.rgb*(.97+grain*.06*_Stylized);
            o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}

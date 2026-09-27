Shader "Hidden/Astra/Performance/Grade"
{
 Properties { _MainTex("Source",2D)="white"{} }
 SubShader
 {
  Cull Off ZWrite Off ZTest Always
  Pass
  {
   CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   float4 _MainTex_TexelSize;
   float _Strength,_Signal;
   float bayer(float2 p)
   {
    int x=(int)fmod(p.x,4),y=(int)fmod(p.y,4);
    float v=0;
    if(y==0) v=x==0?0:x==1?8:x==2?2:10;
    if(y==1) v=x==0?12:x==1?4:x==2?14:6;
    if(y==2) v=x==0?3:x==1?11:x==2?1:9;
    if(y==3) v=x==0?15:x==1?7:x==2?13:5;
    return (v+.5)/16-.5;
   }
   fixed4 frag(v2f_img i):SV_Target
   {
    float3 col=tex2D(_MainTex,i.uv).rgb;
    // Quantize display color, then return to linear for the output conversion.
    float3 srgb=LinearToGammaSpace(col);
    float2 pixel=floor(i.uv*_MainTex_TexelSize.zw);
    float noise=bayer(pixel)*.40;
    float3 q=floor(saturate(srgb)*24+noise+.5)/24;
    srgb=lerp(srgb,q,_Strength);
    float scan=1-.027*_Signal*(fmod(pixel.y,2));
    float2 v=i.uv*2-1;
    float vig=1-.13*_Signal*dot(v,v);
    return float4(GammaToLinearSpace(max(0,srgb*scan*vig)),1);
   }
   ENDCG
  }
 }
}

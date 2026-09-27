Shader "Astra/PerformanceTool/Invisible"
{
    SubShader { Tags { "Queue"="Geometry" "RenderType"="Opaque" } Pass { ColorMask 0 ZWrite Off Cull Off } }
}

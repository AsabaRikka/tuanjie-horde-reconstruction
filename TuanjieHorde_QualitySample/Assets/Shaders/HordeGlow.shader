Shader "Horde/Glow"
{
 Properties { _BaseColor("Colour",Color)=(1,1,1,1) _BaseMap("Particle",2D)="white"{} }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Tags {"LightMode"="SRPDefaultUnlit"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor;float4 _BaseMap_ST;
   CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 c:COLOR;};
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.c=i.c;return o;}
   half4 frag(V i):SV_Target{return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*i.c*_BaseColor;}
   ENDHLSL
  }
 }
}

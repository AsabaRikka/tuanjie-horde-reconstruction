Shader "Horde/World Text"
{
 Properties { _MainTex("Font atlas",2D)="white" {} }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
  Pass
  {
   Tags { "LightMode"="SRPDefaultUnlit" }
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   struct A { float4 p:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
   struct V { float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
   V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.color=i.color;return o;}
   half4 frag(V i):SV_Target{return half4(i.color.rgb,i.color.a*SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a);}
   ENDHLSL
  }
 }
}

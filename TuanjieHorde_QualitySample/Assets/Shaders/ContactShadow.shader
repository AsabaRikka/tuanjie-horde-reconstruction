Shader "Horde/Contact Shadow"
{
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20" "RenderType"="Transparent"}
  Pass
  {
   Blend DstColor Zero
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
   V vert(A i){UNITY_SETUP_INSTANCE_ID(i);V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv*2-1;return o;}
   half4 frag(V i):SV_Target{float shade=pow(saturate(1-dot(i.uv,i.uv)),2)*.37;return half4(1-shade,1-shade*.94,1-shade*.86,1);}
   ENDHLSL
  }
 }
}

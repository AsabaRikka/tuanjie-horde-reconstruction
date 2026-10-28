Shader "Horde/Gate Glass"
{
 Properties { _BaseColor("Glass colour",Color)=(.08,.42,1,.24) }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Tags {"LightMode"="SRPDefaultUnlit"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseColor;
   CBUFFER_END
   float4 vert(float4 p:POSITION):SV_POSITION{return TransformObjectToHClip(p.xyz);}
   half4 frag():SV_Target{return _BaseColor;}
   ENDHLSL
  }
 }
}

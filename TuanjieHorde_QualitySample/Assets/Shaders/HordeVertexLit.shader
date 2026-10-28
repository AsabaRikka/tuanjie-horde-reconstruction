Shader "Horde/Vertex Lit"
{
 Properties { _Tint("Tint",Color)=(1,1,1,1) _Flash("Impact flash",Range(0,1))=0 _Metallic("Metallic",Range(0,1))=0 _VertexColors("Use vertex colours",Range(0,1))=1 _SurfaceMap("Surface colour",2D)="white"{} _SurfaceTexture("Use texture",Float)=0 _TextureScale("World tiling",Float)=1 _SurfaceGrain("Grain",Float)=0 _Unlit("Baked light",Float)=0 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass
  {
   Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _Tint;float _Flash;float _Metallic;float _VertexColors;float _SurfaceTexture,_TextureScale,_SurfaceGrain,_Unlit;float _MetalHeight;
   CBUFFER_END
   TEXTURE2D(_SurfaceMap);SAMPLER(sampler_SurfaceMap);
   struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct Varyings {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 positionWS:TEXCOORD1;float4 color:COLOR;float fog:TEXCOORD2;float3 local:TEXCOORD3;UNITY_VERTEX_INPUT_INSTANCE_ID};
   Varyings vert(Attributes i){Varyings o;UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);VertexPositionInputs p=GetVertexPositionInputs(i.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.local=i.positionOS.xyz;o.color=i.color;o.fog=ComputeFogFactor(p.positionCS.z);return o;}
   half4 frag(Varyings i):SV_Target {
    UNITY_SETUP_INSTANCE_ID(i);float3 n=normalize(i.normalWS);Light l=GetMainLight(TransformWorldToShadowCoord(i.positionWS));float diff=saturate(dot(n,l.direction));
    float3 c=lerp(float3(1,1,1),i.color.rgb,_VertexColors)*_Tint.rgb;
    c*=lerp(float3(1,1,1),SAMPLE_TEXTURE2D(_SurfaceMap,sampler_SurfaceMap,i.positionWS.xz*_TextureScale).rgb,_SurfaceTexture);
    float grain=frac(sin(dot(floor(i.positionWS.xz*180),float2(12.9898,78.233)))*43758.5453)-.5;c*=1+grain*_SurfaceGrain;
    float3 light=SampleSH(n)*.85+l.color*diff*lerp(.13,1,l.shadowAttenuation);
    float3 eye=GetWorldSpaceNormalizeViewDir(i.positionWS),h=normalize(l.direction+eye),reflection=reflect(-eye,n);
    float spec=pow(saturate(dot(n,h)),lerp(24,95,_Metallic))*lerp(.04,1.0,_Metallic)*l.shadowAttenuation;
    float sky=smoothstep(-.25,.65,reflection.y);float3 environment=lerp(float3(.16,.11,.055),float3(1.10,1.15,1.25),sky);
    float fresnel=.35+.65*pow(1-saturate(dot(n,eye)),5);
    float3 col=c*light*(1-_Metallic*.20)+environment*lerp(c,float3(1,1,.75),.16)*_Metallic*fresnel+spec*lerp(float3(1,1,1),c,.35)*l.color;
    float heightShade=lerp(.53,1.10,smoothstep(0,6.2,i.positionWS.y));
    col*=lerp(1,heightShade,_Metallic);
    float skyStrip=pow(saturate(1-abs(reflection.y-.38)*1.9),6);
    col+=_Metallic*(c*.17*saturate(n.y)+float3(1.0,.94,.48)*skyStrip*.22);
    if(_Metallic>.8){
      float top=smoothstep(.4,.96,n.y);float height=saturate(i.local.y/max(.8,_MetalHeight)+.5);
      float3 goldFace=lerp(float3(.24,.145,.003),float3(.70,.60,.014),height);
      float3 goldTop=float3(1.55,1.60,.42);
      float shade=lerp(.46,1,l.shadowAttenuation);
      float3 painted=lerp(goldFace,goldTop,top)*shade;
      float edge=pow(1-saturate(dot(n,eye)),4)*.16;
      col=lerp(col,painted,.79)+float3(.55,.48,.18)*edge;
    }
    col=lerp(col,c,_Unlit);col=lerp(col,float3(1.8,2.5,3.2),_Flash);return half4(MixFog(col,i.fog),1);
   }
   ENDHLSL
  }
  Pass
  {
   Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
   float3 _LightDirection;
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
   float4 vert(A i):SV_POSITION {UNITY_SETUP_INSTANCE_ID(i);float3 p=TransformObjectToWorld(i.positionOS.xyz);float3 n=TransformObjectToWorldNormal(i.normalOS);float4 c=TransformWorldToHClip(ApplyShadowBias(p,n,_LightDirection));
   #if UNITY_REVERSED_Z
   c.z=min(c.z,UNITY_NEAR_CLIP_VALUE);
   #else
   c.z=max(c.z,UNITY_NEAR_CLIP_VALUE);
   #endif
   return c;}
   half4 frag():SV_Target{return 0;}
   ENDHLSL
  }
 }
}

Shader "Horde/IP Actor"
{
 Properties {
 _BaseMap("Base colour",2D)="white"{} _NormalMap("Normal",2D)="bump"{} _RoughMap("Roughness",2D)="white"{} _MetalMap("Metallic",2D)="black"{}
 _Tint("Tint",Color)=(1,1,1,1) _Role("Role 0 enemy 1 boss 2 vehicle",Float)=0 _Textured("Textured",Float)=1 _NormalStrength("Normal detail",Float)=0.35
 _Anim("Stride move recoil attack",Vector)=(0,0,0,0) _Pose("Duck flash dissolve",Vector)=(0,0,0,0) _Emission("Emission",Float)=0 _Variation("Variation",Vector)=(1,1,1,0) _CrowdAmbient("Crowd ambient",Float)=1
 }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _Tint;float _Role,_Textured,_NormalStrength,_Emission,_CrowdAmbient;
 CBUFFER_END
 TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
 TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap);
 TEXTURE2D(_RoughMap);SAMPLER(sampler_RoughMap);
 TEXTURE2D(_MetalMap);SAMPLER(sampler_MetalMap);
 UNITY_INSTANCING_BUFFER_START(PerActor)
 UNITY_DEFINE_INSTANCED_PROP(float4,_Anim)
 UNITY_DEFINE_INSTANCED_PROP(float4,_Pose)
 UNITY_DEFINE_INSTANCED_PROP(float4,_Variation)
 UNITY_INSTANCING_BUFFER_END(PerActor)
 struct A {float4 p:POSITION;float3 n:NORMAL;float4 tangent:TANGENT;float4 color:COLOR;float2 uv:TEXCOORD0;float2 part:TEXCOORD1;float3 pivot:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID};
 float3 rx(float3 p,float a){float s,c;sincos(a,s,c);return float3(p.x,c*p.y-s*p.z,s*p.y+c*p.z);}
 float3 rz(float3 p,float a){float s,c;sincos(a,s,c);return float3(c*p.x-s*p.y,s*p.x+c*p.y,p.z);}
 float3 ry(float3 p,float a){float s,c;sincos(a,s,c);return float3(c*p.x+s*p.z,p.y,-s*p.x+c*p.z);}
 void PoseVertex(inout float3 p,inout float3 n,inout float3 tangent,float2 region,float3 pivot)
 {
   if(_Role>2.5)return;
   float part=region.x;
   float4 a=UNITY_ACCESS_INSTANCED_PROP(PerActor,_Anim);float4 f=UNITY_ACCESS_INSTANCED_PROP(PerActor,_Pose);
   float phase=a.x,move=a.y,kick=a.z,attack=a.w;float sn=sin(phase);float side=p.x<0?-1:1;
   if(_Role>1.5){
     if(part>4.5 && part<5.5){p=pivot+rx(p-pivot,-phase);n=rx(n,-phase);tangent=rx(tangent,-phase);}
     if(part>5.5)p.z-=kick*.15;
     float chassis=1-smoothstep(.52,.94,p.y);p=lerp(p,ry(p,f.w),chassis);n=normalize(lerp(n,ry(n,f.w),chassis));p.y+=abs(f.w)*.06*chassis;
     float tilt=-kick*.035;p=rx(p,tilt);n=rx(n,tilt);tangent=rx(tangent,tilt);p.y+=abs(sn)*move*.024;return;
   }
   float boss=step(.5,_Role),hip=lerp(.48,.64,boss),shoulder=lerp(.96,1.13,boss),width=lerp(.255,.26,boss);
   float angle=0;float3 origin=0;
   if(part>3.5){angle=sin(phase+(part>4.5?3.141593:0))*move*lerp(.32,.27,boss);origin=float3(side*.115,hip,0);}
   else if(part>1.5){angle=-sin(phase+(part>2.5?3.141593:0))*move*.13;origin=float3(part>2.5?width:-width,shoulder,0);
     float wind=saturate(-attack),strike=saturate(attack);angle+=attack<0?-wind*2.15:lerp(-2.15,-.65,strike)*step(.001,strike);
   }
   if(part>1.5){angle*=region.y;p=origin+rx(p-origin,angle);n=rx(n,angle);tangent=rx(tangent,angle);}
   if(part<.5){p.z+=sin(phase+p.x*6)*move*.025*saturate((.7-p.y)*3);}
   float bend=f.x*.92+saturate(attack)*1.08-saturate(-attack)*.18;
   float weight=smoothstep(hip-.12,hip+.13,p.y);float3 local=p-float3(0,hip,0);
   p=lerp(p,float3(0,hip,0)+rx(local,bend),weight);n=normalize(lerp(n,rx(n,bend),weight));tangent=normalize(lerp(tangent,rx(tangent,bend),weight));
   p.y-=f.x*.34+saturate(attack)*.22;p.y+=abs(sn)*move*lerp(.02,.013,boss);
 }
 ENDHLSL
 Pass {
 Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_instancing
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #pragma multi_compile_fog
 struct V {float4 p:SV_POSITION;float3 ws:TEXCOORD0;float3 n:TEXCOORD1;float4 tangent:TEXCOORD2;float2 uv:TEXCOORD3;float4 color:COLOR;float fog:TEXCOORD4;UNITY_VERTEX_INPUT_INSTANCE_ID};
 V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);PoseVertex(i.p.xyz,i.n,i.tangent.xyz,i.part,i.pivot);VertexPositionInputs p=GetVertexPositionInputs(i.p.xyz);o.p=p.positionCS;o.ws=p.positionWS;o.n=TransformObjectToWorldNormal(i.n);o.tangent=float4(TransformObjectToWorldDir(i.tangent.xyz),i.tangent.w*GetOddNegativeScale());o.uv=i.uv;o.color=i.color;o.fog=ComputeFogFactor(p.positionCS.z);return o;}
 half4 frag(V i):SV_Target {
 UNITY_SETUP_INSTANCE_ID(i);float4 pose=UNITY_ACCESS_INSTANCED_PROP(PerActor,_Pose);
 clip(frac(sin(dot(floor(i.p.xy),float2(12.9898,78.233)))*43758.5453)-pose.z);
 float3 base=lerp(i.color.rgb,SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb,_Textured)*_Tint.rgb;
 base*=UNITY_ACCESS_INSTANCED_PROP(PerActor,_Variation).rgb;
 if(_Role<.5){float lum=dot(base,float3(.2126,.7152,.0722));base=lerp(lum.xxx,base,.76);}
 float3 n=normalize(i.n);
 if(_Textured>.5){float3 t=normalize(i.tangent.xyz),b=cross(n,t)*i.tangent.w;float3 tn=SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv).xyz*2-1;tn.xy*=_NormalStrength;n=normalize(t*tn.x+b*tn.y+n*max(.15,tn.z));}
 float rough=lerp(.4,SAMPLE_TEXTURE2D(_RoughMap,sampler_RoughMap,i.uv).r,_Textured);
 float metal=lerp(i.color.a,SAMPLE_TEXTURE2D(_MetalMap,sampler_MetalMap,i.uv).r,_Textured);
 if(_Role<1.5){rough=max(rough,.65);metal=min(metal,.12);}
 Light l=GetMainLight(TransformWorldToShadowCoord(i.ws));float ndl=saturate(dot(n,l.direction));float shadow=lerp(.22,1,l.shadowAttenuation);
 float3 eye=GetWorldSpaceNormalizeViewDir(i.ws);float3 h=normalize(eye+l.direction);float spec=pow(saturate(dot(n,h)),lerp(90,9,rough))*lerp(.09,.65,metal);
 float rim=pow(1-saturate(dot(n,eye)),3)*.075;
 float cavity=lerp(.65,1,smoothstep(0,.6,i.ws.y));
 float3 col=base*(SampleSH(n)*.85*_CrowdAmbient*cavity+l.color*(ndl*.88+.045)*shadow)+spec*lerp(float3(1,1,1),base,metal)*l.color*shadow+rim*float3(.22,.38,.52);
 col+=base*_Emission;col=lerp(col,float3(.9,1.6,2),pose.y*.65);return half4(MixFog(col,i.fog),1);
 }
 ENDHLSL
 }
 Pass {
 Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
 HLSLPROGRAM
 #pragma vertex shadowVert
 #pragma fragment shadowFrag
 #pragma multi_compile_instancing
 float3 _LightDirection;
 float4 shadowVert(A i):SV_POSITION{UNITY_SETUP_INSTANCE_ID(i);PoseVertex(i.p.xyz,i.n,i.tangent.xyz,i.part,i.pivot);float3 ws=TransformObjectToWorld(i.p.xyz);float4 c=TransformWorldToHClip(ApplyShadowBias(ws,TransformObjectToWorldNormal(i.n),_LightDirection));
 #if UNITY_REVERSED_Z
 c.z=min(c.z,UNITY_NEAR_CLIP_VALUE);
 #else
 c.z=max(c.z,UNITY_NEAR_CLIP_VALUE);
 #endif
 return c;}
 half4 shadowFrag():SV_Target{return 0;}
 ENDHLSL
 }
 }
}

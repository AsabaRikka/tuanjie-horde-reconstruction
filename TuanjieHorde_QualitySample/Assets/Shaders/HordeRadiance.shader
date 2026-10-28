Shader "Horde/Radiance"
{
 Properties {_BaseColor("Edge radiance",Color)=(.06,.8,2,1) _Mode("0 bolt 1 disc 2 column 3 star",Float)=0}
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+1" "RenderType"="Transparent"}
 Pass {
 Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor;float _Mode;
 CBUFFER_END
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 V vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=a.uv;o.color=a.color;return o;}
 half4 frag(V i):SV_Target{
 float2 q=i.uv*2-1;float alpha,core;
 if(_Mode<.5){
   float shape=pow(saturate(sin(i.uv.x*3.14159)),.36);float cross=abs(q.y)/max(.001,shape);
   alpha=exp(-cross*cross*6.0)*smoothstep(0,.12,i.uv.x)*smoothstep(1,.90,i.uv.x);
   core=exp(-cross*cross*38.0)*smoothstep(0,.14,i.uv.x)*smoothstep(1,.85,i.uv.x);
 }else if(_Mode<1.5){float r=length(q);alpha=pow(saturate(1-r),1.8);core=pow(saturate(1-r*1.8),2)*.65;}
 else if(_Mode<2.5){float cross=abs(q.y);alpha=pow(saturate(1-cross),2)*pow(saturate(1-i.uv.x),1.25)*smoothstep(0,.035,i.uv.x);core=pow(saturate(1-cross*4),3)*alpha*.45;}
 else {float r=length(q);alpha=(exp(-abs(q.x)*35-abs(q.y)*2)+exp(-abs(q.y)*35-abs(q.x)*2))*saturate(1-r);core=exp(-r*12);}
 float3 col=lerp(_BaseColor.rgb,float3(3.5,4.0,4.5),core)*i.color.rgb;
 return half4(col,saturate(alpha*(_Mode<.5?1.18:2.1))*_BaseColor.a*i.color.a);
 }
 ENDHLSL
 }
 }
}

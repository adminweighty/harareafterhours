Shader "Harare/Photographic Foliage"
{
 Properties { _BaseMap("Leaf colour",2D)="white"{} _AlphaMap("Leaf mask",2D)="white"{} _Cutoff("Cutoff",Range(0,1))=.4 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  Cull Off
  Pass
  {
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_AlphaMap);SAMPLER(sampler_AlphaMap);
   CBUFFER_START(UnityPerMaterial) float _Cutoff; CBUFFER_END
   struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;half3 n:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;UNITY_VERTEX_INPUT_INSTANCE_ID};
   V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(i.n);o.uv=i.uv;o.fog=ComputeFogFactor(o.p.z);return o;}
   half4 frag(V i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
   {UNITY_SETUP_INSTANCE_ID(i);clip(SAMPLE_TEXTURE2D(_AlphaMap,sampler_AlphaMap,i.uv).r-_Cutoff);half3 n=normalize(i.n)*IS_FRONT_VFACE(face,1,-1);Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));half diffuse=.25+.75*saturate(dot(n,sun.direction));half3 light=SampleSH(n)+sun.color*diffuse*lerp(.35,1,sun.shadowAttenuation);return half4(MixFog(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*light,i.fog),1);}
   ENDHLSL
  }
  Pass
  {
   Tags {"LightMode"="ShadowCaster"}
   ZWrite On ZTest LEqual ColorMask 0
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
   TEXTURE2D(_AlphaMap);SAMPLER(sampler_AlphaMap);float3 _LightDirection;
   CBUFFER_START(UnityPerMaterial) float _Cutoff; CBUFFER_END
   struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
   V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);o.p=TransformWorldToHClip(ApplyShadowBias(TransformObjectToWorld(i.p.xyz),TransformObjectToWorldNormal(i.n),_LightDirection));
   #if UNITY_REVERSED_Z
   o.p.z=min(o.p.z,UNITY_NEAR_CLIP_VALUE*o.p.w);
   #else
   o.p.z=max(o.p.z,UNITY_NEAR_CLIP_VALUE*o.p.w);
   #endif
   o.uv=i.uv;return o;}
   half4 frag(V i):SV_Target{clip(SAMPLE_TEXTURE2D(_AlphaMap,sampler_AlphaMap,i.uv).r-_Cutoff);return 0;}
   ENDHLSL
  }
 }
}

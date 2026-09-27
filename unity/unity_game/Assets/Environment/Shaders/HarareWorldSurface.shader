Shader "Harare/World Surface"
{
    Properties
    {
        _Surfaces("Reference surface array", 2DArray) = "" {}
        _Slice("Surface layer", Float) = 0
        _MetresPerTile("Metres per tile", Float) = 2
        _BaseColor("Tint", Color) = (1,1,1,1)
        _Smoothness("Smoothness", Range(0,1)) = .15
        [Toggle(_PHOTO_SURFACE)] _PhotoSurface("Photographic asphalt", Float) = 0
        _PhotoColor("Photographic colour", 2D) = "white" {}
        _PhotoNormal("Photographic normal", 2D) = "bump" {}
        _PhotoRoughness("Photographic roughness", 2D) = "white" {}
        _NormalStrength("Normal strength", Range(0,1)) = .45
        _BaseMap("Shadow pass placeholder", 2D) = "white" {}
        _Cutoff("Cutoff", Float) = .5
        _Cull("Cull", Float) = 2
        _ZWrite("ZWrite", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma require 2darray
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _PHOTO_SURFACE
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D_ARRAY(_Surfaces); SAMPLER(sampler_Surfaces);
            TEXTURE2D(_PhotoColor); SAMPLER(sampler_PhotoColor);
            TEXTURE2D(_PhotoNormal); SAMPLER(sampler_PhotoNormal);
            TEXTURE2D(_PhotoRoughness); SAMPLER(sampler_PhotoRoughness);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Slice, _MetresPerTile, _Smoothness;
                float _PhotoSurface, _NormalStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings
            {
                float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0;
                half3 normalWS:TEXCOORD1; half fog:TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings o=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p=GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS;
                o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.fog=ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 n=normalize(i.normalWS); half3 axis=abs(n);
                // Dominant-axis world projection keeps scaled road/building
                // cubes at the same human-scale texel density without new UVs.
                float2 uv=axis.y>.6 ? i.positionWS.xz : (axis.x>axis.z?i.positionWS.zy:i.positionWS.xy);
                uv/=max(.1,_MetresPerTile);
                half3 albedo=SAMPLE_TEXTURE2D_ARRAY(_Surfaces,sampler_Surfaces,uv,_Slice).rgb*_BaseColor.rgb;
                half smoothness=_Smoothness;
                #if defined(_PHOTO_SURFACE)
                    albedo=SAMPLE_TEXTURE2D(_PhotoColor,sampler_PhotoColor,uv).rgb*_BaseColor.rgb;
                    half3 detail=UnpackNormalScale(SAMPLE_TEXTURE2D(_PhotoNormal,sampler_PhotoNormal,uv),_NormalStrength);
                    // Match the world-projected UV axes, including the thin road sides.
                    half3 tangent=axis.y>.6?half3(1,0,0):(axis.x>axis.z?half3(0,0,1):half3(1,0,0));
                    half3 bitangent=axis.y>.6?half3(0,0,1):half3(0,1,0);
                    n=normalize(tangent*detail.x+bitangent*detail.y+n*detail.z);
                    smoothness=saturate(1-SAMPLE_TEXTURE2D(_PhotoRoughness,sampler_PhotoRoughness,uv).r);
                #endif
                // Low-frequency variation reduces visibly identical long runs.
                half weather=1-.035*(sin(i.positionWS.x*.31)*sin(i.positionWS.z*.23));
                InputData data=(InputData)0;
                data.positionWS=i.positionWS;data.normalWS=n;
                data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                data.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                data.bakedGI=SampleSH(n);data.shadowMask=half4(1,1,1,1);
                data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                data.vertexLighting=VertexLighting(i.positionWS,n);
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=albedo*weather;surface.alpha=1;surface.occlusion=1;
                surface.normalTS=half3(0,0,1);surface.smoothness=smoothness;
                half4 color=UniversalFragmentPBR(data,surface);
                color.rgb=MixFog(color.rgb,i.fog);return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    FallBack "Universal Render Pipeline/Lit"
}

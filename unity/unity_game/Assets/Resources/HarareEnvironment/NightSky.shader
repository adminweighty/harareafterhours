Shader "Harare/Night Sky"
{
    Properties
    {
        _StarBrightness ("Star brightness", Range(0, 2)) = 0.65
        [HideInInspector] _PreviewTime ("Preview time (-1 uses game time)", Float) = -1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _StarBrightness;
                float _PreviewTime;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.direction = input.positionOS.xyz;
                return o;
            }
            float Hash(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.direction);
                float time = _PreviewTime >= 0 ? _PreviewTime : _Time.y;
                // Sparse, world-anchored points. Derivatives soften tiny stars on phones.
                float3 grid = d * 190.0;
                float3 cell = floor(grid);
                float seed = Hash(cell);
                float radius = lerp(0.045, 0.10, seed);
                float distance = length(frac(grid) - 0.5);
                float aa = max(length(fwidth(grid)) * 0.38, 0.015);
                float stars = (1.0 - smoothstep(radius, radius + aa, distance)) * step(0.945, seed);
                stars *= smoothstep(0.04, 0.28, d.y) * _StarBrightness;
                stars *= 0.86 + 0.14 * sin(time * 1.4 + seed * 63.0);
                half3 colour = stars * half3(0.82, 0.90, 1.0);

                // One brief meteor per 18-second window, with changing direction and
                // start time. No emitters, extra lights, textures or per-frame allocation.
                float cycle = floor(time / 18.0);
                float random = frac(cycle * 0.61803399);
                float age = time - cycle * 18.0 - (3.0 + random * 5.0);
                if (age > 0.0 && age < 1.2)
                {
                    float yaw = random * 6.2831853 - 0.4;
                    float elevation = 0.36 + frac(cycle * 0.381966) * 0.28;
                    float3 start = normalize(float3(sin(yaw), elevation, cos(yaw)));
                    float3 tangent = normalize(float3(cos(yaw), -0.40, -sin(yaw)));
                    tangent = normalize(tangent - start * dot(tangent, start));
                    float3 head = normalize(start + tangent * age * 0.34);
                    float3 side = normalize(cross(start, tangent));
                    float3 along = normalize(cross(side, head));
                    float behind = -dot(d, along);
                    float across = abs(dot(d, side));
                    float width = max(fwidth(dot(d, side)) * 0.8, 0.00040);
                    float tail = saturate(1.0 - behind / 0.085);
                    float streak = (1.0 - smoothstep(width * 0.25, width, across));
                    streak *= tail * tail * step(0.0, behind) * step(0.9, dot(d, head));
                    float headGlow = 1.0 - smoothstep(0.0, width * 2.2, length(d - head));
                    float fade = smoothstep(0.0, 0.12, age) * (1.0 - smoothstep(0.65, 1.2, age));
                    colour += (streak * 1.7 + headGlow * 2.0) * fade * half3(0.80, 0.90, 1.0);
                }
                return half4(colour, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

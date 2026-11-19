Shader "SupervivenciaEnLaPaz/Cielo Altiplano"
{
    Properties
    {
        _Zenit ("Azul del cielo", Color) = (0.33, 0.58, 0.79, 1)
        _Horizonte ("Bruma del horizonte", Color) = (0.79, 0.85, 0.89, 1)
        _Luz ("Luz de las nubes", Color) = (1, 0.94, 0.84, 1)
        _Sombra ("Sombra de las nubes", Color) = (0.61, 0.72, 0.81, 1)
        _Sol ("Dirección del sol", Vector) = (-0.54, 0.57, -0.62, 0)
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            float4 _Zenit, _Horizonte, _Luz, _Sombra, _Sol;

            struct Varyings
            {
                float4 position : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings Vert(appdata_base input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.direction = input.vertex.xyz;
                return output;
            }

            float Hash(float2 cell)
            {
                float3 p = frac(float3(cell.x, cell.y, cell.x) * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float Noise(float2 samplePosition)
            {
                float2 cell = floor(samplePosition);
                float2 u = frac(samplePosition);
                u = u * u * (3.0 - 2.0 * u);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), u.x),
                            lerp(Hash(cell + float2(0, 1)), Hash(cell + 1), u.x), u.y);
            }

            float CloudDensity(float2 uv)
            {
                return 0.53 * Noise(uv) + 0.24 * Noise(uv * 2.06 + 3.7)
                     + 0.12 * Noise(uv * 4.19 - 7.1) + 0.07 * Noise(uv * 8.37 + 13.6)
                     + 0.04 * Noise(uv * 17.13 - 2.4);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 view = normalize(input.direction);
                float height = saturate(view.y);
                float3 color = lerp(_Horizonte.rgb, _Zenit.rgb, pow(height, 0.55));
                float sun = saturate(dot(view, normalize(_Sol.xyz)));
                color += (0.10 * pow(sun, 8.0) + 0.55 * smoothstep(0.9997, 0.99985, sun)) * _Luz.rgb;

                float2 cloudUV = view.xz * 1.8 / (max(view.y, 0.01) + 0.23) + float2(-2.0, 5.6);
                float density = CloudDensity(cloudUV);
                float opacity = smoothstep(0.50, 0.68, density) * smoothstep(0.015, 0.17, height);
                float litEdge = CloudDensity(cloudUV + float2(-0.18, -0.13));
                float light = saturate(0.53 + (density - litEdge) * 4.0);
                float3 clouds = lerp(_Sombra.rgb, _Luz.rgb, light);
                return half4(lerp(color, clouds, opacity), 1);
            }
            ENDCG
        }
    }
}

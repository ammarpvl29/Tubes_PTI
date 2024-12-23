Shader "Custom/SlimeLaserShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (0,1,0,1)
        _GlowColor ("Glow Color", Color) = (0,1,0,1)
        _FlowSpeed ("Flow Speed", Range(0,10)) = 1
        _NoiseScale ("Noise Scale", Range(0,50)) = 10
        _DistortionAmount ("Distortion Amount", Range(0,1)) = 0.1
        _Transparency ("Transparency", Range(0,1)) = 0.8
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _GlowColor;
            float _FlowSpeed;
            float _NoiseScale;
            float _DistortionAmount;
            float _Transparency;

            // Simple noise function
            float2 random2(float2 st)
            {
                st = float2(dot(st,float2(127.1,311.7)),
                           dot(st,float2(269.5,183.3)));
                return -1.0 + 2.0 * frac(sin(st)*43758.5453123);
            }

            // Perlin noise
            float perlinNoise(float2 st) 
            {
                float2 i = floor(st);
                float2 f = frac(st);
                
                float2 u = f*f*(3.0-2.0*f);

                return lerp(lerp(dot(random2(i + float2(0.0,0.0)), f - float2(0.0,0.0)),
                               dot(random2(i + float2(1.0,0.0)), f - float2(1.0,0.0)), u.x),
                           lerp(dot(random2(i + float2(0.0,1.0)), f - float2(0.0,1.0)),
                               dot(random2(i + float2(1.0,1.0)), f - float2(1.0,1.0)), u.x), u.y);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Time-based flow
                float2 flowUV = i.uv + float2(_Time.y * _FlowSpeed, 0);
                
                // Generate noise for distortion
                float noise = perlinNoise(flowUV * _NoiseScale);
                
                // Apply distortion to UV
                float2 distortedUV = i.uv + noise * _DistortionAmount;
                
                // Sample texture with distorted UVs
                fixed4 col = tex2D(_MainTex, distortedUV);
                
                // Add flowing effect
                float flowEffect = sin((_Time.y * _FlowSpeed + distortedUV.x) * 3.14159 * 2) * 0.5 + 0.5;
                
                // Combine colors
                fixed4 finalColor = lerp(_Color, _GlowColor, flowEffect);
                finalColor.a = col.a * _Transparency * (1 - abs(i.uv.y - 0.5) * 2); // Fade edges
                
                return finalColor;
            }
            ENDCG
        }
    }
}
Shader "Fursonify/Stars"
{
    Properties
    {
        [HDR] _Tint ("Tint", Color) = (1,1,1,1)
        _StarPower ("Star Power", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { 
            "Queue"="Geometry-99"
        }
        Blend One One
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "FursonifyIncludes.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 direction : TEXCOORD1;
                float4 color : COLOR;
            };

            fixed4 _Tint;
            float _StarPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.direction = normalize(v.vertex.xyz);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float2 uvOffset = 0.5 - i.uv;
                float dist = pow(1 - saturate(length(uvOffset * 2)), 1 / (1 - _StarPower));
                fixed4 col = dist * _Tint * i.color;
                
                // float skyIntensity = dot(float3(0.2, 0.7, 0.1), sampleEnvironment(i.direction * 10000, i.direction, 1));
                
                float zenith = dot(i.direction, float3(0, 1, 0));
                // float horizonFalloff = lerp(0, 1, smoothstep(0, 1, zenith));

                float appearanceThreshold = 1 - exp(-dot(col.rgb, float3(0.299, 0.587, 0.114)));
                appearanceThreshold *= 0.05;
                
                return col;// * smoothstep(1 - appearanceThreshold, 1, 1 - skyIntensity);// * horizonFalloff;
            }
            ENDCG
        }
    }
}

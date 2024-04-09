Shader "Tumble/Basic Outline"
{
    Properties
    {
        _EdgeWidthModifier("Edge Width Modifier", 2D) = "grey" {}

        [HDR]_EdgeColor("Edge Color", Color) = (1, 1, 1, 1)
        _EdgeSteps("Edge Steps", Range(1, 200)) = 10
        _EdgeDistance("Edge Distance", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+302"
        }
        
        GrabPass
        {
            "MagicGlassMask"
        }

        Pass
        {
            Cull Back
            ZWrite Off
            ZTest Always
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            sampler2D PreMask;
            sampler2D MagicGlassMask;
            sampler2D _EdgeWidthModifier;
            float4    _EdgeWidthModifier_ST;

            float4 _EdgeColor;
            int    _EdgeSteps;
            float  _EdgeDistance;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 localPos : TEXCOORD3;
            };
            
            float distanceToMaskEdge(float4 screenPos, float edgeDistance)
            {
                float4 mask;

                [loop]
                for (int x = 0; x <= _EdgeSteps; x++) {
                    float  ratio = (x / float(_EdgeSteps)) * UNITY_TWO_PI;
                    float4 offset = float4(cos(ratio), sin(ratio), 0.0, 0.0) * edgeDistance;
                    float2 screenUV = screenPos.xy / screenPos.w;
                    mask = tex2D(MagicGlassMask, screenUV + offset.xy);
                    if (mask.r > -0.5) return 0;
                }
                return 1;
            }

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                o.localPos = v.vertex.xyz;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Edge glow
                float4 preMask = tex2Dproj(PreMask, i.screenPos);
                float mask = tex2Dproj(MagicGlassMask, i.screenPos).r;
                float edgeWidthModifier = tex2D(_EdgeWidthModifier, i.uv * _EdgeWidthModifier_ST.xy + _EdgeWidthModifier_ST.zw * _Time[1]).r * 0.8
                + 0.2;
                float distance = distanceToMaskEdge(i.screenPos, _EdgeDistance * edgeWidthModifier);
                if(distance > 0) return preMask;
                
                return _EdgeColor;
            }
            ENDCG
        }
    }
}
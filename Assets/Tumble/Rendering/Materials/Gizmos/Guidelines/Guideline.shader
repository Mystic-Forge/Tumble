Shader "Unlit/Guideline"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        _DashSize ("Dash Size", Float) = 0.05
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Overlay"
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        CGINCLUDE
        #pragma vertex vert
        #include "UnityCG.cginc"
        
        struct appdata {
            float4 vertex : POSITION;
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            float3 pos : TEXCOORD0;
            float4 screenPos : TEXCOORD1;
            float linearDepth : TEXCOORD2;
        };

        float4 _Color;
        float _DashSize;

        v2f vert(appdata v)
        {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.pos = v.vertex.xyz;
            o.screenPos = ComputeScreenPos(o.vertex);
            return o;
        }
        ENDCG

        Pass
        {
            ZTest Greater
            ZWrite Off
            CGPROGRAM
            #pragma fragment frag

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = _Color;
                
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV));
                
                float pixelDepth = i.screenPos.w;
                float difference = abs(depth - pixelDepth);
                if(difference < 0.5) col *= 10;
                else if(abs(i.pos.y) % _DashSize < _DashSize * 0.5) discard;
                return col * 2;
            }
            ENDCG
        }

        Pass
        {
            ZTest LEqual
            CGPROGRAM
            #pragma fragment frag

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = _Color;
                return col;
            }
            ENDCG
        }
    }
}
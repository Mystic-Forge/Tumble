Shader "Tumble/Title/Additive"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        _Power ("Power", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }
        LOD 100

        Blend One One
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            float4 _Color;
            float  _Power;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                if (i.color.r < 0.001) discard;
                fixed4 col = _Color * saturate(pow(i.color.r, _Power));
                return col;
            }
            ENDCG
        }
    }
}
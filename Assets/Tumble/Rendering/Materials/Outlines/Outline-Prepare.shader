Shader "Tumble/Outline-Prepare"
{
    Properties {}
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+301"
        }

        GrabPass
        {
            "PreMask"
        }

        Pass
        {
            ZTest Off
            ZWrite Off

            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return float4(-1, 0, 0, 1);
            }
            ENDCG
        }
    }
}
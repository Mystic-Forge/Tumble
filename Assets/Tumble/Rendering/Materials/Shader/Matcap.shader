Shader "Tumble/Matcap"
{
    Properties
    {
        _Matcap ("Matcap", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _Matcap;
            float4 _TintColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                i.worldNormal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.worldPos - _WorldSpaceCameraPos);
                viewDir = reflect(viewDir, i.worldNormal);
                float2 matcapUv = mul(UNITY_MATRIX_V, viewDir).xy * 0.5 + 0.5;
                float4 col = tex2D(_Matcap, matcapUv) * _TintColor;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}

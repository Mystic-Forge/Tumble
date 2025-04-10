Shader "Tumble/Unlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Color ("Color", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
        // ZWrite 
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZWrite ("ZWrite", Float) = 1
    }
    SubShader
    {
        Cull [_Cull]
        ZTest LEqual
        ZWrite [_ZWrite]
        Blend [_SrcBlend] [_DstBlend]
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent+1"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv + _MainTex_ST.zw * _Time.y) * _Color * i.color;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col * smoothstep(0, 1, col.a);
            }
            ENDCG
        }
    }
}
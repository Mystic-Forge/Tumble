Shader "Tumble/Unlit Sky"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Geometry-100" "PreviewType"="Skybox"
        }
        Cull Front 
        ZWrite On
//        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            float4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = _Color;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

//        // Shadows
//                Pass
//        {
//            Tags {"LightMode"="ShadowCaster"}
//
//            CGPROGRAM
//            #pragma vertex vert
//            #pragma fragment frag
//            #pragma multi_compile_shadowcaster
//            #include "UnityCG.cginc"
//
//            struct v2f { 
//                V2F_SHADOW_CASTER;
//            };
//
//            v2f vert(appdata_base v)
//            {
//                v2f o;
//                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
//                return o;
//            }
//
//            float4 frag(v2f i) : SV_Target
//            {
//                SHADOW_CASTER_FRAGMENT(i)
//            }
//            ENDCG
//        }
    }
}
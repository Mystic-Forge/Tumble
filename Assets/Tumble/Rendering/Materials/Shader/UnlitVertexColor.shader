Shader "Unlit/UnlitVertexColor"
{
    Properties
    {
        [HDR] _Tint ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent"
            "Queue"="Transparent" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
//        ZWrite On
        ZTest Always
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            float4 _Tint;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = i.color * _Tint;
                return col;
            }
            ENDCG
        }
    }
}

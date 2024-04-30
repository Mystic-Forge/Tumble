Shader "Tumble/MagicGlass-Draw"
{
    Properties {}
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+301"
        }
        
        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            void vert(in float4 v : POSITION, out float4 o : SV_POSITION)
            {
                o = UnityObjectToClipPos(v);
            }

            fixed4 frag() : SV_Target
            {
                return float4(-1, 0, 0, 1);
            }
            ENDCG
        }
    }
}
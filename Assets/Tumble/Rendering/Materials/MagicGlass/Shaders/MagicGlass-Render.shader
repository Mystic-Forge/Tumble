Shader "Tumble/MagicGlass-Render"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0, 1)) = 0.5

        _EdgeWidthModifier("Edge Width Modifier", 2D) = "grey" {}

        [HDR]_EdgeColor("Edge Color", Color) = (1, 1, 1, 1)
        _EdgeSteps("Edge Steps", Range(1, 200)) = 10
        _EdgeDistance("Edge Distance", Range(0, 1)) = 0.1
        _EdgePower("Edge Power", Range(0, 1)) = 0.5
        _ReflectionIntensity("Reflection Intensity", Range(0, 1)) = 0.5

        _BlobDensity("Blob Density", Float) = 10
        [HDR]_BlobColor("Blob Color", Color) = (1, 1, 1, 1)
        _BlobDepth("Blob Depth", Float) = 0.5
        _BlobSteps("Blob Steps", Range(0, 200)) = 10
        _BlobAttenuation("Blob Attenuation", Range(0, 1)) = 0.5
        _BlobSize("Blob Size", Range(0, 1)) = 0.5

        _StarDensity("Star Density", Float) = 10
        [HDR]_StarColor("Star Color", Color) = (1, 1, 1, 1)
        _StarDepth("Star Depth", Float) = 0.5
        _StarSteps("Star Steps", Range(0, 200)) = 10
        _StarAttenuation("Star Attenuation", Range(0, 1)) = 0.5
        _StarSize("Star Size", Range(0, 1)) = 0.5

        _Roughness("Roughness", Range(0, 1)) = 0.5
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
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            sampler2D MagicGlassGrab;
            sampler2D MagicGlassMask;

            sampler2D _NormalMap;
            float4    _NormalMap_ST;
            float     _NormalStrength;

            sampler2D _EdgeWidthModifier;
            float4    _EdgeWidthModifier_ST;

            float4 _Color;
            float4 _EdgeColor;
            int    _EdgeSteps;
            float  _EdgeDistance;
            float  _EdgePower;
            float  _ReflectionIntensity;

            float  _BlobDensity;
            float4 _BlobColor;
            float  _BlobDepth;
            int    _BlobSteps;
            float  _BlobAttenuation;
            float  _BlobSize;

            float  _StarDensity;
            float4 _StarColor;
            float  _StarDepth;
            int    _StarSteps;
            float  _StarAttenuation;
            float  _StarSize;

            float     _Roughness;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                half4  tangent : TANGENT;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 localPos : TEXCOORD3;
                float3 normal : TEXCOORD4;
                float3 tangent : TEXCOORD5;
                float3 binormal : TEXCOORD6;
            };

            float3 hash33(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.xxy + p.yzz) * p.zyx);
            }

            float hash(int p)
            {
                return frac(sin(p * 0.1031) * 43758.5453);
            }

            float3 generateStarPos(float3 pos, float t)
            {
                float3 hash = hash33(pos * 4) * UNITY_TWO_PI;
                float  x = sin(hash.x + t * 1) * 0.5 + 0.5;
                float  y = cos(hash.y + t * 1) * 0.5 + 0.5;
                float  z = sin(hash.z + t * 1) * 0.5 + 0.5;
                return float3(x, y, z);
            }

            float sample3DVolume(float3 pos, float density, bool blend)
            {
                pos *= density;
                float3 cell = floor(pos);
                cell += 0.5;

                float minDist = 1;
                float t = _Time[1] * 0.5;

                for (int x = -1; x <= 1; x++) {
                    for (int y = -1; y <= 1; y++) {
                        for (int z = -1; z <= 1; z++) {
                            float3 offset = float3(x, y, z);
                            float3 p = cell + offset;
                            p += generateStarPos(cell + offset, t) * 0.5;
                            float3 delta = p - pos;
                            minDist *= min(dot(delta, delta), 1);
                        }
                    }
                }

                const float starSize = 1;
                return saturate(starSize - minDist) / starSize;
            }


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

            v2f vert(appdata v, out float4 overtex : SV_POSITION)
            {
                v2f o;
                o.localPos = v.vertex.xyz;
                overtex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(overtex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                o.normal = normalize(UnityObjectToWorldDir(v.normal));
                o.tangent = normalize(UnityObjectToWorldDir(v.tangent.xyz));
                o.binormal = normalize(cross(o.normal, o.tangent.xyz)) * v.tangent.w;
                return o;
            }

            #define hash(p)  frac(sin(dot(p, float2(11.9898, 78.233))) * 43758.5453)

            float blueNoise(float2 U)
            {
                float v = hash(U + float2(-1, 0))
                + hash(U + float2( 1, 0))
                + hash(U + float2( 0, 1))
                + hash(U + float2( 0,-1));
                return hash(U) - v / 4. + .5;
            }

            float3 BoxProjection(
                float3 direction, const float3 position,
                float4 cubemapPosition, float4 boxMin, float4 boxMax
            )
            {
                UNITY_BRANCH
                if (cubemapPosition.w > 0) {
                    float3 factors =
                    ((direction > 0 ? boxMax.xyz : boxMin.xyz) - position) / direction;
                    const float scalar = min(min(factors.x, factors.y), factors.z);
                    direction = direction * scalar + (position - cubemapPosition.xyz);
                }
                return direction;
            }

            float3 SampleEnvironment(float3 worldPos, const float mip, const float3 normal)
            {
                const float3 viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                const float3 reflectedView = reflect(-viewDir, normal);
                float3       uvw = BoxProjection(
                    reflectedView, worldPos, unity_SpecCube0_ProbePosition,
                    unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax
                );
                float3 color = UNITY_SAMPLE_TEXCUBE_LOD(
                    unity_SpecCube0, uvw, mip
                );

                color = DecodeHDR(half4(color, 1), unity_SpecCube0_HDR);

                const float blend = unity_SpecCube0_BoxMin.w;
                if (blend < 0.99999) {
                    uvw = BoxProjection(
                        reflectedView, worldPos,
                        unity_SpecCube1_ProbePosition,
                        unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax
                    );
                    float3 sample = UNITY_SAMPLE_TEXCUBE_SAMPLER_LOD(
                        unity_SpecCube1, unity_SpecCube0, uvw, mip
                    );
                    sample = DecodeHDR(half4(sample, 1), unity_SpecCube1_HDR);
                    color = lerp(sample.rgb, color, blend);
                }
                return color;
            }

            fixed4 frag(v2f i, UNITY_VPOS_TYPE screenPixel : VPOS) : SV_Target
            {
                float4 col = _Color;
                float starMask = 1;
                float2 generatedUv = i.uv;

                float4 normalColor = tex2D(_NormalMap, i.uv * _NormalMap_ST.xy + _NormalMap_ST.zw);
                float3 normalMap = UnpackNormalWithScale(normalColor, _NormalStrength);

                float3x3 TBN = float3x3(normalize(i.tangent), normalize(i.binormal), normalize(i.normal));
                TBN = transpose(TBN);

                float3 worldNormal = mul(TBN, normalMap);

                // Blobs
                float3 pos = i.localPos;
                float3 stepDir = mul((float3x3)unity_WorldToObject, normalize(i.worldPos - _WorldSpaceCameraPos)).xyz;
                float3 step = stepDir * (_BlobDepth / _BlobSteps);
                float  density = 0;
                float  influence = _BlobColor.a;
                [loop]
                for (int s = 0; s < _BlobSteps; s++) {
                    float sample = sample3DVolume(pos, _BlobDensity, true) > _BlobSize;
                    density += sample * influence;
                    if(sample > 0.5) break;
                    influence *= _BlobAttenuation;
                    pos += step;
                }
                float3 blobs = density * _BlobColor;
                col.rgb += blobs * starMask;

                // Stars
                pos = i.localPos;
                step = stepDir * (_StarDepth / _StarSteps);
                influence = _StarColor.a * saturate(1 - density);
                density = 0;
                [loop]
                for (int s = 0; s < _StarSteps; s++) {
                    float sample = sample3DVolume(pos, _StarDensity, false) > _StarSize;
                    density += sample * influence;
                    if (sample > 0.5) break;
                    influence *= _StarAttenuation;
                    pos += step;
                }
                float3 stars = density * _StarColor;
                col.rgb += stars * starMask;

                // Ambient Lighting
                float3 worldViewDir = normalize(i.worldPos - _WorldSpaceCameraPos);
                float3 reflectedView = reflect(worldViewDir, normalize(worldNormal));
                float3       uvw = BoxProjection(
                    reflectedView, i.worldPos, unity_SpecCube0_ProbePosition,
                    unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax
                );
                float3 ambientColor = UNITY_SAMPLE_TEXCUBE_LOD(
                    unity_SpecCube0, uvw, 4
                );
                
                // Diffuse lighting
                float diffuseLight = saturate(dot(worldNormal, _WorldSpaceLightPos0.xyz)); // * 0.5 + 0.5;
                
                col.rgb = ambientColor * col.rgb + col.rgb * diffuseLight;

                // Edge glow
                float mask = tex2Dproj(MagicGlassMask, i.screenPos).r;
                float edgeWidthModifier = tex2D(_EdgeWidthModifier, generatedUv * _EdgeWidthModifier_ST.xy + _EdgeWidthModifier_ST.zw * _Time[1]).r * 0.8
                + 0.2;
                float4 edgeGlow = mask.r > -0.5 ? 0 : pow(1.01 - distanceToMaskEdge(i.screenPos, _EdgeDistance * edgeWidthModifier), 1 / _EdgePower);
                edgeGlow *= _EdgeColor;
                col.rgb += edgeGlow.rgb;

                // Reflection
                float3 skyColor = SampleEnvironment(i.worldPos, 1 / _Roughness, worldNormal);
                float  fresnel = pow(dot(reflectedView, worldViewDir) * 0.5 + 0.5, 10);
                col.rgb += skyColor * fresnel * _ReflectionIntensity;

                // Specular lighting
                float spec = pow(saturate(dot(reflectedView, _WorldSpaceLightPos0.xyz)), 5000 * _Roughness) * pow(_Roughness, 5);
                col.rgb += spec;

                return col;
            }
            ENDCG
        }
    }
}
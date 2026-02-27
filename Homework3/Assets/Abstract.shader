// Upgrade NOTE: replaced 'UNITY_INSTANCE_ID' with 'UNITY_VERTEX_INPUT_INSTANCE_ID'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/Abstract"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Hue("Hue", Range(0, 20)) = 1
        _Color("Main Color", Color) = (0.5,0.5,0.5,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
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
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 posWorld : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Hue;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color.rgb = v.vertex;
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float dist = length(i.posWorld.xyz - _WorldSpaceCameraPos.xyz);
                fixed4 col = tex2D(_MainTex, i.uv);
                col.r = round(i.color.r+0.5) + floor(_Color.r + fmod((i.posWorld.x) * 0.5, 2));
                col.g = abs(round(i.color.g+0.5) + floor(_Color.g + fmod((i.posWorld.y) * 0.5, 2)));
                col.b = round(i.color.b+0.5) + floor(_Color.b + fmod((i.posWorld.z) * 0.5, 2));
                col.rgb = fmod(normalize(col.rgb+1)*_Hue, 1.001) / clamp(dist*0.1, 1, 2);
                col.a = clamp(2 - dist * 0.02, 0, 1);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}

Shader "Pathfinding/FOVRadial"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _SecondaryColor ("Secondary Color", Color) = (1,1,1,1)
        _BlendFactor ("Blend Factor", Range(0,1)) = 0
        _ObjectPos ("Owner Position", Vector) = (0,0,0,1)
        _Radius ("FOV Radius", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(fixed4,_SecondaryColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _BlendFactor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ObjectPos)
                UNITY_DEFINE_INSTANCED_PROP(float, _Radius)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v,o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
                baseColor.rgb *= baseColor.a;
                fixed4 secondaryColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SecondaryColor);
                secondaryColor.rgb *= secondaryColor.a;

                float radius = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Radius);
                float blend = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BlendFactor);

                float4 goCenter = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ObjectPos);
                float dist = distance(i.worldPos, goCenter);

                bool seen = blend >= (dist / radius);

                float4 color = seen ? secondaryColor : baseColor;

                return color;
            }
            ENDCG
        }
    }
}

Shader "Pathfinding/TrajectoryShader"
{
    Properties
	{
		_MainTex ("Cell Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		// Pass 1
		Pass
		{

		Tags
		{ 
			"Queue" = "Transparent" "LightMode" = "ForwardBase"
		}

		Stencil {
		    Ref 2
		    Comp NotEqual
		    Pass Replace
		}

		Blend SrcAlpha OneMinusSrcAlpha  

		CGPROGRAM

			//#pragma multi_compile _ SHADOWS_SCREEN
			#pragma multi_compile_fwdbase_fullshadows

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityStandardBRDF.cginc" // already includes UnityCG.cginc
			#include "AutoLight.cginc"

			fixed4 _Color;

			struct appdata_t
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 color  : COLOR;
				float2 uv     : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 normal : NORMAL;
				fixed4 color  : COLOR;
				float2 uv     : TEXCOORD0;
				float3 worldPos : TEXCOORD1;

				SHADOW_COORDS(2)
				
			};

			sampler2D _MainTex;

			v2f vert(appdata_t IN)
			{

				v2f OUT;
				OUT.pos = UnityObjectToClipPos(IN.vertex);
				OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex);
				OUT.normal = UnityObjectToWorldNormal(IN.normal);
				OUT.uv = IN.uv;
				OUT.color = IN.color * _Color;

				TRANSFER_SHADOW(OUT);

				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 c = IN.color;
				fixed shadow = SHADOW_ATTENUATION(IN);

				// setting up basic lambert lighting
				IN.normal = normalize(IN.normal);
				float3 lightDir = _WorldSpaceLightPos0.xyz;
				float3 lightColor = _LightColor0.rgb * shadow;
				c.rgb *= lightColor * saturate(dot(lightDir, IN.normal)) + UNITY_LIGHTMODEL_AMBIENT * .25;

				c.rgb *= c.a;
				return c;
			}
		ENDCG
		}

		// Pass 2
		Pass
		{

		Tags
		{ 
			"Queue" = "Transparent" "LightMode" = "ForwardAdd"
		}

		Stencil {
		    Ref 2
		    Comp NotEqual
		    Pass Replace
		}

		Blend SrcAlpha OneMinusSrcAlpha

		//Blend One One
		ZWrite Off

		CGPROGRAM
			//#pragma multi_compile _ SHADOWS_SCREEN
			#pragma multi_compile_fwdadd_fullshadows

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityStandardBRDF.cginc" // already includes UnityCG.cginc
			#include "AutoLight.cginc"

			fixed4 _Color;
			
			struct appdata_t
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 color  : COLOR;
				float2 uv     : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 normal : NORMAL;
				fixed4 color  : COLOR;
				float2 uv     : TEXCOORD0;
				float3 worldPos : TEXCOORD1;

				SHADOW_COORDS(2)
			};

			sampler2D _MainTex;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.pos = UnityObjectToClipPos(IN.vertex);
				OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex);
				OUT.normal = UnityObjectToWorldNormal(IN.normal);
				OUT.uv = IN.uv;
				OUT.color = IN.color * _Color;

				TRANSFER_SHADOW(OUT);

				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 c = IN.color;
				fixed shadow = SHADOW_ATTENUATION(IN);

				// setting up basic lambert lighting
				IN.normal = normalize(IN.normal);
				float3 lightDir = _WorldSpaceLightPos0.xyz;
				float3 lightColor = _LightColor0.rgb * shadow;
				c.rgb *= lightColor * saturate(dot(lightDir, IN.normal)) + UNITY_LIGHTMODEL_AMBIENT * .25;

				c.rgb *= c.a;
				return c;
			}
		ENDCG
		}
	}

		Fallback "Diffuse"

}

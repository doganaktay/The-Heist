Shader "Pathfinding/CellShaderLight"
{
    Properties
	{
		_MainTex ("Cell Texture", 2D) = "white" {}
		_ColorIndex("Current Color Index", Int) = 0
		_BaseColor ("Base Color", Color) = (1,1,1,1)
		_PathColor ("Path Color", Color) = (1,1,1,1)
		_SecondaryPathColor ("Secondary Path Color", Color) = (1,1,1,1)
		_HighlightColor ("Highlight Color", Color) = (1,1,1,1)
		_Speed ("Speed", Float) = 1
		_PathIndex ("Path Index", Float) = 0
		_PathCount ("Path Count", Float) = 0
		_HighlightCount ("Highlight Count", Float) = 5
	}

	SubShader
	{
		// Pass 1
		Pass
		{

		Tags
		{ 
			"LightMode" = "ForwardBase"
		}

		CGPROGRAM

			//#pragma multi_compile _ SHADOWS_SCREEN
			#pragma multi_compile_fwdbase_fullshadows

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityStandardBRDF.cginc" // already includes UnityCG.cginc
			#include "AutoLight.cginc"

			fixed4 _BaseColor;
			fixed4 _PathColor;
			fixed4 _SecondaryPathColor;
			fixed4 _HighlightColor;
			int _ColorIndex;
			float  _Speed, _RestartTime, _HighlightCount, _PathIndex, _PathCount;

			//custom methods
			fixed4 selectColor(int i)
			{
				if(i == 0)
					return _BaseColor;
				if(i == 1)
					return _PathColor;
				if(i == 2)
					return _SecondaryPathColor;
				if(i == 3)
					return _HighlightColor;

				return fixed4(0,0,0,0);
			}
			//end custom methods

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
				OUT.color = IN.color * selectColor(_ColorIndex);

				TRANSFER_SHADOW(OUT);

				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, IN.uv) * IN.color;
				fixed shadow = SHADOW_ATTENUATION(IN);

				/*
				if(_PathIndex == 0 && _ColorIndex > 1)
					return selectColor(0);

				float time = (_Time.y - _RestartTime) * _Speed;
				float count = fmod(time, _PathCount + _HighlightCount);
				float t;

				if(_PathIndex >= count - _HighlightCount && _PathIndex < count)
					t = 1 - pow((count - _PathIndex) / _HighlightCount,1.0/1.5); // taking the pow with a fractional exp for smoother blend
				else
					t = 0;


				if(_ColorIndex > 0)
				{
					fixed4 baseC = selectColor(0);
					c = lerp(baseC, c, t);
				}
				*/

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
			"LightMode" = "ForwardAdd"
		}

		Blend One One
		ZWrite Off

		CGPROGRAM
			//#pragma multi_compile _ SHADOWS_SCREEN
			#pragma multi_compile_fwdadd_fullshadows

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityStandardBRDF.cginc" // already includes UnityCG.cginc
			#include "AutoLight.cginc"

			fixed4 _BaseColor;
			fixed4 _PathColor;
			fixed4 _SecondaryPathColor;
			fixed4 _HighlightColor;
			int _ColorIndex;
			float  _Speed, _RestartTime, _HighlightCount, _PathIndex, _PathCount;

			//custom methods
			fixed4 selectColor(int i)
			{
				if(i == 0)
					return _BaseColor;
				if(i == 1)
					return _PathColor;
				if(i == 2)
					return _SecondaryPathColor;
				if(i == 3)
					return _HighlightColor;

				return fixed4(0,0,0,0);
			}
			//end custom methods

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
				OUT.color = IN.color * selectColor(_ColorIndex);

				TRANSFER_SHADOW(OUT);

				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, IN.uv) * IN.color;
				fixed shadow = SHADOW_ATTENUATION(IN);

				/*
				if(_PathIndex == 0 && _ColorIndex > 1)
					return selectColor(0);

				float time = (_Time.y - _RestartTime) * _Speed;
				float count = fmod(time, _PathCount + _HighlightCount);
				float t;

				if(_PathIndex >= count - _HighlightCount && _PathIndex < count)
					t = 1 - pow((count - _PathIndex) / _HighlightCount,1.0/1.5); // taking the pow with a fractional exp for smoother blend
				else
					t = 0;


				if(_ColorIndex > 0)
				{
					fixed4 baseC = selectColor(0);
					c = lerp(baseC, c, t);
				}
				*/

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

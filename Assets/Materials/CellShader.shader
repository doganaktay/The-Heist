Shader "Pathfinding/CellShader"
{
    Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ PIXELSNAP_ON
			#include "UnityCG.cginc"

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
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
			};

			sampler2D _MainTex;
			sampler2D _AlphaTex;
			float _AlphaSplitEnabled;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * selectColor(_ColorIndex);
				#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap (OUT.vertex);
				#endif

				return OUT;
			}


			fixed4 SampleSpriteTexture (float2 uv)
			{
				fixed4 color = tex2D (_MainTex, uv);

			#if UNITY_TEXTURE_ALPHASPLIT_ALLOWED
				if (_AlphaSplitEnabled)
					color.a = tex2D (_AlphaTex, uv).r;
			#endif //UNITY_TEXTURE_ALPHASPLIT_ALLOWED

				return color;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				if(_PathIndex == 0 && _ColorIndex > 1)
					return selectColor(0);

				float time = (_Time.y - _RestartTime) * _Speed;
				float count = fmod(time, _PathCount + _HighlightCount);
				float t;

				if(_PathIndex >= count - _HighlightCount && _PathIndex < count)
					t = 1 - (count - _PathIndex) / _HighlightCount;
				else
					t = 0;

				fixed4 c = SampleSpriteTexture (IN.texcoord) * IN.color;

				if(_ColorIndex > 0)
				{
					fixed4 baseC = selectColor(0);
					c = lerp(baseC, c, t);
				}

				c.rgb *= c.a;
				return c;
			}
		ENDCG
		}
	}
}

Shader "Pathfinding/CellShader"
{
    Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_BaseColor ("Base Color", Color) = (1,1,1,1)
		_SecondaryColor ("Secondary Color", Color) = (1,1,1,1)
		_BlendFactor ("Blend Factor", Range(0,1)) = 0
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Geometry" 
			"IgnoreProjector"="True" 
			"RenderType"="Geometry" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		
		Blend One OneMinusSrcAlpha

		Pass
		{
		CGPROGRAM
			#pragma multi_compile_instancing
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

			UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)	
				UNITY_DEFINE_INSTANCED_PROP(fixed4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(fixed4,_SecondaryColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _BlendFactor)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 colorBase : COLOR;
				fixed4 colorSecondary : COLOR1;
				float2 texcoord  : TEXCOORD0;
				float blendFactor : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			sampler2D _MainTex;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN,OUT);
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = (IN.texcoord);
				OUT.colorBase = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
				OUT.colorSecondary = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SecondaryColor);
				OUT.blendFactor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BlendFactor);

				return OUT;
			}


			fixed4 SampleSpriteTexture (float2 uv)
			{
				fixed4 color = tex2D (_MainTex, uv) + 0.5;

				return color;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
				fixed4 lerpColor = lerp(IN.colorBase, IN.colorSecondary, IN.blendFactor);
				fixed4 c = SampleSpriteTexture (IN.texcoord) * lerpColor;
				c.rgb *= c.a;
				return c;
			}
		ENDCG
		}
	}
}

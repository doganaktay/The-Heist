/*

	Smooth Voronoi Contours
	-----------------------

	Adapted from Shadertoy: https://www.shadertoy.com/view/4sdXDX

*/

Shader "Experimental/SmoothVoronoi"
{
    Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_BaseColor ("Base Color", Color) = (1,1,1,1)
		_SecondaryColor ("Secondary Color", Color) = (1,1,1,1)
		_EffectColor ("Effect Color", Color) = (1,1,1,1)
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

			// Glossy version. It's there to show that the method works with raised surfaces too.
			//#define GLOSSY

			UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)	
				UNITY_DEFINE_INSTANCED_PROP(fixed4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(fixed4,_SecondaryColor)
				UNITY_DEFINE_INSTANCED_PROP(fixed4,_EffectColor)
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
				fixed4 colorEffect : COLOR2;
				float2 texcoord  : TEXCOORD0;
				float blendFactor : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			sampler2D _MainTex;

			float2 hash22(float2 p) {
    
				// Faster, but probaly doesn't disperse things as nicely as other methods.
				float n = sin(dot(p, float2(41, 289)));
				p = frac(float2(2097152, 262144)*n);
				return cos(p*6.283 + _Time.y * 0.1)*.5;
				//return abs(frac(p+ _Time.y*.25)-.5)*2. - .5; // Snooker.
				//return abs(cos(p*6.283 + _Time.y))*.5; // Bounce.

			}

			float smoothVoronoi(float2 p, float falloff) {

				float2 ip = floor(p); p -= ip;
	
				float d = 1., res = 0.0;
	
				for(int i = -1; i <= 2; i++) {
					for(int j = -1; j <= 2; j++) {
            
						float2 b = float2(i, j);
            
						float2 v = b - p + hash22(ip + b);
            
						d = max(dot(v,v), 1e-4);
			
						res += 1.0/pow( d, falloff );
					}
				}

				return pow( 1./res, .5/falloff );
			}

			// 2D function we'll be producing the contours for. 
			float func2D(float2 p){

				float d = smoothVoronoi(p*2., 4.)*.66 + smoothVoronoi(p*6., 4.)*.34;
    
				return sqrt(d);
			}

			float smoothFrac(float x, float sf){
 
				x = frac(x); return min(x, x*(1.-x)*sf);
			}

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN,OUT);
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = (IN.texcoord);
				OUT.colorBase = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
				OUT.colorSecondary = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SecondaryColor);
				OUT.colorEffect = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EffectColor);
				OUT.blendFactor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BlendFactor);

				return OUT;
			}


			fixed4 SampleSpriteTexture (v2f IN)
			{
				fixed4 color = tex2D (_MainTex, IN.texcoord);
				return color;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(IN);
 
				float2 e = float2(0.001, 0); 
				float2 uv = IN.texcoord;

				float f = func2D(uv); // Range [0, 1]
    
				float g = length( float2(f - func2D(uv-e.xy), f - func2D(uv-e.yx)) )/(e.x);

				g = 1./max(g, 0.001);
    
				float freq = 33.; 

				// Smoothing factor. Hand picked. Ties in with the frequency above. Higher frequencies
				// require a lower value, and vice versa.
				float smoothFactor = uv.y*0.0125;
    
				#ifdef GLOSSY
				float c = smoothFrac(f*freq, g*uv.y/16.); // Range [0, 1]
				//float c = frac(f*freq); // Aliased version, for comparison.
				#else
				float c = clamp(cos(f*freq*3.14159*2.)*g*smoothFactor, 0., 1.); // Range [0, 1]
				//float c = clamp(cos(f*freq*3.14159*2.)*2., 0., 1.); // Blurry contours, for comparison.
				#endif
    
    
				// Coloring.
				//
				// Convert "c" above to the greyscale and green colors.
				//float3 col = float3(c,c,c);
				float3 col = IN.colorSecondary.rgb;
				//float3 col2 = float3(c*0.64, c, c*c*0.1);
				float3 col2 = IN.colorBase.rgb;
    
				#ifdef GLOSSY
				col = lerp(col, col2, -uv.y + clamp(frac(f*freq*0.5)*2.-1., 0., 1.0));
				#else
				col = lerp(col, col2, -uv.y + clamp(cos(f*freq*3.14159)*2., 0., 1.0));
				#endif
    
				// Color in a couple of thecontours above. Not madatory, but it's pretty simple, and an interesting 
				// way to pretty up functions. I use it all the time.
				f = f*freq;
    
				#ifdef GLOSSY
				if(f>8. && f<9.) col *= float3(1, 0, .1);
				#else
				if(f>8.5 && f<9.5) col *= float3(1, 0, .1);
				#endif 
   
    
				// Since we have the gradient related value, we may as well use it for something. In this case, we're 
				// adding a bit of highlighting. It's calculated for the contourless noise, so doesn't match up perfectly,
				// but it's good enough. Comment it out to see the texture on its own.  
				#ifdef GLOSSY
				col += g*g*g*float3(.3, .5, 1)*.25*.25*.25*.1;
				#endif 
    
				// clip all pixels with black
				clip(col.r < 0.001 && col.g < 0.001 && col.b < 0.001 ? -1 : 1);
				//col = c * float3(g*.25); // Just the function and gradient. Has a plastic wrap feel.
	
				// Done.
				return float4( sqrt(clamp(col, 0., 1.)), 1.0 );
			}
		ENDCG
		}
	}
}